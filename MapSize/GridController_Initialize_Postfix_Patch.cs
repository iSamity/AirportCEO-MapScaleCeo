using HarmonyLib;
using MapScaleCeo.Config;
using UnityEngine;

namespace MapScaleCeo.MapSize;

[HarmonyPatch(typeof(GridController), nameof(GridController.Initialize))]
internal static class GridController_Initialize_Postfix_Patch
{
    [HarmonyPostfix]
    internal static void GridController_Initialize_Postfix(AirportData airportData)
    {
        var load = Singleton<SaveLoadGameDataController>.Instance?.inputGameLoadSetting;
        if (load == Enums.GameLoadSetting.ContinueGame || load == Enums.GameLoadSetting.EditorContinueGame)
        {
            MapSizeWarningPatch.AbandonPendingConfigResetAfterGridInit();
            return;
        }

        if (load != Enums.GameLoadSetting.NewGame && load != Enums.GameLoadSetting.EditorNewGame)
            return;

        if (!MapSizeWarningPatch.ConsumePendingConfigResetAfterGridInit())
            return;

        var mapSizeEntry = DefaultConfig.NewGameMapSize;
        if (mapSizeEntry == null)
            return;

        mapSizeEntry.Value = Vector2.zero;
        Plugin.Logger.LogInfo("[GridController_Initialize_Postfix_Patch] Cleared NewGameMapSize after custom new game.");
    }
}
