using HarmonyLib;
using UnityEngine;

namespace MapScaleCeo.MapSize;

/// <summary>
/// Vanilla runs InitializeCamera before StartNewGame assigns worldSize from defaultGrid; match placement to effective new-game footprint.
/// </summary>
[HarmonyPatch(typeof(CameraController), nameof(CameraController.InitializeCamera))]
internal static class CameraController_InitializeCamera_Postfix_Patch
{
    [HarmonyPostfix]
    internal static void CameraController_InitializeCamera_Postfix(CameraController __instance)
    {
        var data = Singleton<AirportController>.Instance?.AirportData;
        if (data == null)
            return;

        // Load/continue paths do not call InitializeCamera; only new-game setup does.
        var xy = MapSizeHelper.GetEffectiveNewGameFootprintXY(data.worldSizeType);
        Vector3 pos;
        if (data.worldSizeType == Enums.WorldSize.Large)
            pos = new Vector3(xy.x / 2f, xy.y / 4.5f, -250f);
        else
            pos = new Vector3(xy.x / 4f, xy.y / 4.5f, -250f);

        __instance.transform.position = pos;
    }
}
