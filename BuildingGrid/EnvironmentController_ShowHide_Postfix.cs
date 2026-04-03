using System.Collections.Generic;
using HarmonyLib;
using MapScaleCeo.MapSize;
using UnityEngine;

namespace MapScaleCeo.BuildingGrid;

[HarmonyPatch(typeof(EnvironmentController))]
internal static class EnvironmentController_ShowHide_Postfix
{
    // Original ShowHide: sets showUnderground = (floor < 0) and tunnel.enabled = (floor >= 0).
    // Then for each allGrids index (same Normal-map rule: only i < allGrids.Length - 3 when world size is Normal; all indices otherwise),
    // moves each grid sprite's Z to FloorManager.TERMINAL_FLOOR_SHIFT when floor > 0, else to 0.
    [HarmonyPostfix]
    [HarmonyPatch(nameof(EnvironmentController.ShowHide))]
    internal static void Postfix(EnvironmentController __instance, int floor)
    {
        if (!MapSizeHelper.FootprintIsNonDefaultVanillaSize())
        {
            return;
        }

        var extendedGrids = EnvironmentController_InitializeEnvironment_Postfix.extendedGrids;

        for (int i = 0; i < extendedGrids.Count; i++)
        {
            var grid = extendedGrids[i];
            if (grid == null)
                continue;

            grid.transform.position = grid.transform.position.SetZ((floor > 0) ? FloorManager.TERMINAL_FLOOR_SHIFT : 0f);
        }
    }
}
