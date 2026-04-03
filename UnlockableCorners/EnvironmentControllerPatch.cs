using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MapScaleCeo.UnlockableCorners;


/// <summary>
/// Applies the unlockable areas scale to the unlockable areas so the unlockable areas are scaled to the map size instead of default sizes
/// So they scale correctly
/// </summary>
[HarmonyPatch(typeof(EnvironmentController), nameof(EnvironmentController.InitializeEnvironment))]
internal class EnvironmentControllerPatch
{
    [HarmonyPostfix]
    internal static void Postfix(EnvironmentController __instance)
    {
        var wx = (float)GridManager.worldSizeX;
        var wy = (float)GridManager.worldSizeY;
        if (wx <= 0f || wy <= 0f)
            return;

        var airportData = Singleton<AirportController>.Instance?.AirportData;
        if (airportData == null)
            return;

        var unlockableAreas = __instance.transform.Find("UnlockableAreas");
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
            // So this scales the background so it matches the 1/4 / 1/6 of the map size
            var background = area.Find("Background");
            if (background != null)
                background.localScale = new Vector3(sizeX, sizeY, 1f);
        }

        if (airportData.worldSizeType == Enums.WorldSize.Normal)
        {
            // Partition on num2 / num with no ±1 overlap (adjacent rects share edges only).
            SetArea("Left", 0f, 0f, wx - num2, wy - num);
            SetArea("TopLeft", 0f, num, wx - num2, wy);
            SetArea("Center", num2, 0f, wx, wy - num);
            SetArea("Top", num2, num, wx, wy);
            // Not setting topright and right because they are not used when map size is normal 
        }
        else
        {
            SetArea("Center", num2, 0f, wx - num2, wy - num);
            SetArea("Left", 0f, 0f, wx - num2 * 2f, wy - num);
            SetArea("TopLeft", 0f, num, wx - num2 * 2f, wy);
            SetArea("Top", num2, num, wx - num2, wy);
            SetArea("TopRight", num2 * 2f, num, wx, wy);
            SetArea("Right", num2 * 2f, 0f, wx, wy - num);
        }

        Plugin.Logger.LogInfo("[EnvironmentControllerPatch] UnlockableAreas scaled to map size");
    }

}
