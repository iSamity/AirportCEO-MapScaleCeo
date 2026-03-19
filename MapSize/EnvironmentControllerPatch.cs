using System.Reflection;
using HarmonyLib;
using MapScaleCeo.Config;
using UnityEngine;

namespace MapScaleCeo.MapSize;

[HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.InitializeEnvironment))]
internal class EnvironmentControllerPatch
{
    private static readonly string[] TerrainMatrixFields =
    [
        "terrainMatrixSummer",
        "terrainMatrixSpring",
        "terrainMatrixWinter",
        "terrainMatrixAutumn",
        "terrainMatrixTropic",
        "terrainMatrixDesert"
    ];

    [HarmonyPostfix]
    internal static void Postfix(EnvironmentController __instance)
    {
        ApplyTerrainSize(__instance);
        ApplyUnlockableAreasScale(__instance);
    }

    /// <summary>
    /// Applies the terrain size to the terrain matrices so the terrain is scaled to the map size instead of default sizes
    /// </summary>
    private static void ApplyTerrainSize(EnvironmentController instance)
    {
        var mapSize = DefaultConfig.MapSize.Value;

        if (mapSize.x == 0 || mapSize.y == 0)
        {
            return;
        }

        // So the world doesn't end exactly at the border of the map
        var extraTerrainSize = 1000f;
        var terrainSize = new Vector3(mapSize.x + extraTerrainSize, mapSize.y + extraTerrainSize, 1f);
        var worldCenter = GridManager.WorldCenter;
        const float zOffset = 0.01f;

        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var type = typeof(EnvironmentController);

        foreach (var fieldName in TerrainMatrixFields)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null)
            {
                var matrix = Matrix4x4.TRS(
                    worldCenter.SetZ(zOffset),
                    Quaternion.identity,
                    terrainSize
                );
                field.SetValue(instance, matrix);
            }
        }

        // Also adjust the environmentOverlay for Temperate climate
        if (instance.environmentOverlay != null)
        {
            instance.environmentOverlay.transform.localScale = terrainSize;
            instance.environmentOverlay.transform.position = worldCenter.SetZ(zOffset);
        }

        Plugin.Logger.LogInfo($"[EnvironmentControllerPatch] Terrain resized to {mapSize.x} x {mapSize.y}");
    }

    /// <summary>
    /// Applies the unlockable areas scale to the unlockable areas so the unlockable areas are scaled to the map size instead of default sizes
    /// So they scale correctly
    /// </summary>
    private static void ApplyUnlockableAreasScale(EnvironmentController instance)
    {
        var mapSize = DefaultConfig.MapSize.Value;
        if (mapSize.x == 0 || mapSize.y == 0)
            return;

        var wx = (float)GridManager.worldSizeX;
        var wy = (float)GridManager.worldSizeY;
        var airportData = Singleton<AirportController>.Instance?.AirportData;
        if (airportData == null)
            return;

        var unlockableAreas = instance.transform.Find("UnlockableAreas");
        if (unlockableAreas == null)
            return;

        var num = wy / 2f;
        var num2 = airportData.worldSizeType == Enums.WorldSize.Normal ? wx / 2f : wx / 3f;

        void SetArea(string areaName, float minX, float minY, float maxX, float maxY)
        {
            var area = unlockableAreas.Find(areaName);
            if (area == null) return;
            var centerX = (minX + maxX) / 2f;
            var centerY = (minY + maxY) / 2f;
            var sizeX = maxX - minX;
            var sizeY = maxY - minY;
            area.position = new Vector3(centerX, centerY, area.position.z);
            // Black overlay lives on child "Background" with Simple draw mode — size is driven by localScale, not SpriteRenderer.size on the zone root.
            var background = area.Find("Background");
            if (background != null)
                background.localScale = new Vector3(sizeX, sizeY, 1f);
        }

        if (airportData.worldSizeType == Enums.WorldSize.Normal)
        {
            SetArea("Left", 0f, 0f, wx - num2, wy - num);
            SetArea("TopLeft", 0f, num - 1f, wx - num2, wy);
            SetArea("Center", num2 - 1f, 0f, wx, wy - num);
            SetArea("Top", num2 - 1f, num - 1f, wx, wy);
            SetArea("TopRight", num2 - 1f, num - 1f, wx, wy);
            SetArea("Right", num2 - 1f, 0f, wx, wy - num);
        }
        else
        {
            SetArea("Center", num2, 0f, wx - num2, wy - num);
            SetArea("Left", 0f, 0f, wx - num2 * 2f, wy - num + 1f);
            SetArea("TopLeft", 0f, num, wx - num2 * 2f, wy);
            SetArea("Top", num2, num - 1f, wx - num2, wy);
            SetArea("TopRight", num2 * 2f - 1f, num - 1f, wx, wy);
            SetArea("Right", num2 * 2f - 1f, 0f, wx, wy - num);
        }

        Plugin.Logger.LogInfo("[EnvironmentControllerPatch] UnlockableAreas scaled to map size");
    }
}

