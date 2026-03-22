using HarmonyLib;
using UnityEngine;

namespace MapScaleCeo.MapSize;

[HarmonyPatch(typeof(GridController), nameof(GridController.Initialize))]
internal static class GridController_Initialize_Prefix_Patch
{
    [HarmonyPrefix]
    internal static void GridController_Initialize_Prefix(AirportData airportData)
    {
        if (airportData == null)
            return;

        var load = Singleton<SaveLoadGameDataController>.Instance?.inputGameLoadSetting;
        if (load == Enums.GameLoadSetting.ContinueGame || load == Enums.GameLoadSetting.EditorContinueGame)
            return;

        if (load != Enums.GameLoadSetting.NewGame && load != Enums.GameLoadSetting.EditorNewGame)
            return;

        MapSizeHelper.ApplyNewGameWorldSizeXY(airportData);
    }
}
