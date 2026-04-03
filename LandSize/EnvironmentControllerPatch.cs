using HarmonyLib;
using MapScaleCeo.Config;
using UnityEngine;

namespace MapScaleCeo.LandSize;

/// <summary>
/// Applies the terrain size to the terrain matrices so the terrain is scaled to the map size instead of default sizes
/// </summary>

[HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.InitializeEnvironment))]
internal class EnvironmentControllerPatch
{
    [HarmonyPostfix]
    internal static void Postfix(EnvironmentController __instance)
    {
        if (!DefaultConfig.ImproveGround.Value)
            return;

        var wx = GridManager.worldSizeX;
        var wy = GridManager.worldSizeY;
        if (wx <= 0 || wy <= 0)
            return;

        // So the world doesn't end exactly at the border of the map
        var extraTerrainSize = 1000f;
        var terrainSize = new Vector3(wx + extraTerrainSize, wy + extraTerrainSize, 1f);
        var worldCenter = GridManager.WorldCenter;
        const float zOffset = 0.01f;

        // Must match every terrainMatrix* field on EnvironmentController; if the game adds more, assign them here too.
        var matrix = Matrix4x4.TRS(worldCenter.SetZ(zOffset), Quaternion.identity, terrainSize);
        __instance.terrainMatrixSummer = matrix;
        __instance.terrainMatrixSpring = matrix;
        __instance.terrainMatrixWinter = matrix;
        __instance.terrainMatrixAutumn = matrix;
        __instance.terrainMatrixTropic = matrix;
        __instance.terrainMatrixDesert = matrix;

        if (__instance.environmentOverlay != null)
        {
            __instance.environmentOverlay.transform.localScale = terrainSize;
            __instance.environmentOverlay.transform.position = worldCenter.SetZ(zOffset);
        }

        Plugin.Logger.LogInfo($"[EnvironmentControllerPatch] Terrain resized to {wx} x {wy}");
    }
}