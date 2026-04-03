using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MapScaleCeo.LandSize;

/// <summary>
/// Applies the terrain size to the terrain matrices so the terrain is scaled to the map size instead of default sizes
/// </summary>

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
        var wx = GridManager.worldSizeX;
        var wy = GridManager.worldSizeY;
        if (wx <= 0 || wy <= 0)
            return;

        // So the world doesn't end exactly at the border of the map
        var extraTerrainSize = 1000f;
        var terrainSize = new Vector3(wx + extraTerrainSize, wy + extraTerrainSize, 1f);
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
                field.SetValue(__instance, matrix);
            }
        }

        if (__instance.environmentOverlay != null)
        {
            __instance.environmentOverlay.transform.localScale = terrainSize;
            __instance.environmentOverlay.transform.position = worldCenter.SetZ(zOffset);
        }

        Plugin.Logger.LogInfo($"[EnvironmentControllerPatch] Terrain resized to {wx} x {wy}");
    }
}
