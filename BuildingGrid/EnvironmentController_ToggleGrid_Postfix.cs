using System.Collections.Generic;
using HarmonyLib;
using MapScaleCeo.Config;
using MapScaleCeo.MapSize;
using UnityEngine;

namespace MapScaleCeo.BuildingGrid;

[HarmonyPatch(typeof(EnvironmentController))]
internal static class EnvironmentController_ToggleGrid_Postfix
{
    // Original ToggleGrid: sets allGrids[i].enabled = value (Normal: skips last three when value is true).
    // For custom footprints we replace the building grid with extendedGrids, so vanilla must stay off after this runs.
    [HarmonyPostfix]
    [HarmonyPatch(nameof(EnvironmentController.ToggleGrid))]
    internal static void Postfix(EnvironmentController __instance, bool value)
    {
        if (!DefaultConfig.ImproveBuildingGrid.Value)
        {
            return;
        }

        if (!MapSizeHelper.FootprintIsNonDefaultVanillaSize())
        {
            return;
        }

        // Disable vanilla grid again because the original function is enabling it
        EnvironmentController_InitializeEnvironment_Postfix.DisableVanillaAllGrids(__instance);

        var extendedGrids = EnvironmentController_InitializeEnvironment_Postfix.extendedGrids;

        for (int i = 0; i < extendedGrids.Count; i++)
        {
            var grid = extendedGrids[i];
            if (grid == null)
                continue;
            grid.enabled = value;
        }
    }
}
