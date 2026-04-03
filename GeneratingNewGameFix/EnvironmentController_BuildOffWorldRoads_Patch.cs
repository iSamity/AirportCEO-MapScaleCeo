using HarmonyLib;
using MapScaleCeo.MapSize;
using UnityEngine;

namespace MapScaleCeo.GeneratingNewGameFix;

/// <summary>
/// When grid XY is not stock 700×700 or 1050×700, replicate vanilla <c>BuildOffWorldRoads</c> placement using live
/// <see cref="GridManager.currentWorldSize"/> (not the contractor’s X — vanilla tunnel sits several units east of the
/// contractor / delivery site).
/// </summary>
[HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.BuildOffWorldRoads))]
internal static class EnvironmentController_BuildOffWorldRoads_Patch
{
    [HarmonyPrefix]
    private static bool BuildOffWorldRoads_Prefix(EnvironmentController __instance, bool useNewWorldSize)
    {
        if (!MapSizeHelper.FootprintIsNonDefaultVanillaSize(nameof(BuildOffWorldRoads_Prefix)))
            return true;

        var data = Singleton<AirportController>.Instance?.AirportData;
        if (data == null || !useNewWorldSize)
            return true;

        var vector = GridManager.currentWorldSize;
        var tunnelPos = new Vector2((vector.x / 2f).RoundToNearest(4f) + 2.5f, 14.5f);
        var roadStart = new Vector2((vector.x / 2f).RoundToNearest(4f) + 0.5f, 28.5f);
        if (data.worldSizeType == Enums.WorldSize.Normal)
        {
            tunnelPos = new Vector2(vector.x / 4f + 3.5f, 14.5f);
            roadStart = new Vector2(vector.x / 4f + 1.5f, 28.5f);
        }

        // +1 world unit east vs vanilla formula — fine-tune for custom footprints (not a full 4f grid step).
        const float extraWorldX = 1f;
        tunnelPos.x += extraWorldX;
        roadStart.x += extraWorldX;

        var tunnel = UnityEngine.Object.Instantiate(
                Singleton<BuildingController>.Instance.GetStructureGameObject(Enums.StructureType.WorldEntranceTunnel),
                tunnelPos,
                Quaternion.Euler(new Vector3(0f, 0f, 90f)),
                FolderController.Instance.GetFolder(Enums.StructureType.WorldEntranceTunnel))
            .GetComponent<PlaceableObject>();
        tunnel.isBuilt = true;
        tunnel.shouldNotConstruct = true;
        tunnel.ChangeToPlaced(setFolder: false);

        const float rowCount = 20f;
        var roadFolder = Traverse.Create(__instance).Field("roadFolder").GetValue<Transform>();
        if (roadFolder == null || __instance.road == null)
        {
            Plugin.Logger.LogWarning(
                "[MSC] BuildOffWorldRoads: roadFolder or road prefab null after tunnel — falling back to vanilla (possible duplicate tunnel).");
            return true;
        }

        for (var i = 0; (float)i < rowCount; i++)
        {
            for (var j = 0; j < 2; j++)
            {
                var inst = new Vector2(roadStart.x + (float)(j * 4), roadStart.y + (float)(i * 4));
                var segment = UnityEngine.Object.Instantiate(
                        __instance.road,
                        inst,
                        __instance.road.transform.rotation,
                        roadFolder)
                    .GetComponent<PlaceableRoad>();
                segment.isBuilt = true;
                segment.operationsCost = 0f;
                segment.shouldNotConstruct = true;
                segment.SetFoundationType(Enums.FoundationType.Asphalt);
                segment.ChangeToPlaced(setFolder: false);
            }
        }

        Plugin.Logger.LogInfo(
            $"[MSC] BuildOffWorldRoads: custom footprint — {data.worldSizeType} tunnel at ({tunnelPos.x:F3}, {tunnelPos.y:F3}), road lane0 at ({roadStart.x:F3}, {roadStart.y:F3}).");
        return false;
    }
}
