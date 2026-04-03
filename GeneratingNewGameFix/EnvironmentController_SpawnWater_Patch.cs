using System.Collections.Generic;
using HarmonyLib;
using MapScaleCeo.MapSize;
using UnityEngine;

namespace MapScaleCeo.GeneratingNewGameFix;

/// <summary>
/// Caps noise resolution like vanilla does in X: <c>mapSizeModifier * defaultGrid.x</c>. Vanilla leaves Y as <c>worldSizeY * mapSizeModifier</c>,
/// which matches 700×700; on tall custom maps the noise array is much taller than wide, so Perlin advances every row too fast → horizontal stripe lakes.
/// Cap Y with <c>mapSizeModifier * defaultGrid.y</c> the same way. Pond cells outside <c>0..worldSize-1</c> are skipped (vanilla does not clip).
/// Reference: AirportCeo-publicedcode <c>EnvironmentController.SpawnWater</c>.
/// </summary>
[HarmonyPatch(typeof(EnvironmentController))]
internal static class EnvironmentController_SpawnWater_Patch
{
    [HarmonyPatch("SpawnWater")]
    [HarmonyPrefix]
    private static bool SpawnWater_Prefix(EnvironmentController __instance, ref HashSet<Vector2> __result)
    {
        var load = Singleton<SaveLoadGameDataController>.Instance?.inputGameLoadSetting;
        if (load != Enums.GameLoadSetting.NewGame && load != Enums.GameLoadSetting.EditorNewGame)
        {
            Plugin.Logger.LogInfo($"[MSC] SpawnWater prefix: skip (not new game) loadSetting={load}.");
            return true;
        }

        if (!MapSizeHelper.FootprintIsNonDefaultVanillaSize(nameof(SpawnWater_Prefix)))
            return true;

        var mapSizeModifier = Traverse.Create(__instance).Field("mapSizeModifier").GetValue<float>();

        var hashSet = new HashSet<Vector2>();
        var scale = Utils.RandomRangeF(
            __instance.waterScale * (1f - __instance.waterScaleRandomizer),
            __instance.waterScale * (1f + __instance.waterScaleRandomizer));
        var depthThreshold = Utils.RandomRangeF(
            __instance.waterDepth * (1f - __instance.waterDepthRandmizier),
            __instance.waterDepth * (1f + __instance.waterDepthRandmizier));

        var num2 = (int)(GridManager.worldSizeX *
                        (mapSizeModifier * (GridManager.defaultGrid.x / GridManager.worldSizeX)));
        var num3 = (int)(GridManager.worldSizeY *
                        (mapSizeModifier * (GridManager.defaultGrid.y / GridManager.worldSizeY)));
        var origin = new Vector2(0f - num2 / 4f, 0f - num3 / 4f);
        var array = Utils.GenerateNoiseMap(num2, num3, scale);
        var length = array.GetLength(1);
        var length2 = array.GetLength(0);
        var wx = GridManager.worldSizeX;
        var wy = GridManager.worldSizeY;
        for (var i = 0; i < length; i++)
        {
            for (var j = 0; j < length2; j++)
            {
                if (array[j, i] > depthThreshold)
                {
                    var cell = new Vector2(origin.x + j, origin.y + i).RoundVectorToInt();
                    var ix = (int)cell.x;
                    var iy = (int)cell.y;
                    if (ix < 0 || iy < 0 || ix >= wx || iy >= wy)
                        continue;

                    Singleton<TiledObjectsManager>.Instance.AddTileable(new PondTile(cell, isPlanned: false));
                    hashSet.Add(cell);
                }
            }
        }

        __result = hashSet;
        Plugin.Logger.LogInfo(
            $"[MSC] SpawnWater prefix: applied custom — mapSizeModifier={mapSizeModifier}, noiseSize {num2}x{num3}, origin=({origin.x:F1},{origin.y:F1}), " +
            $"pondCells={hashSet.Count}, scale={scale:F5}, depthThreshold={depthThreshold:F3}.");
        return false;
    }
}
