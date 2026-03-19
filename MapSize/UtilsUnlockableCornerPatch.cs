using HarmonyLib;
using UnityEngine;

namespace MapScaleCeo.MapSize;

[HarmonyPatch(typeof(Utils), nameof(Utils.GetUnlockableCorner))]
internal static class UtilsUnlockableCornerPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(Vector2 position, ref Enums.CornerType __result)
    {
        __result = GridManager.GetUnlockableZone(new Vector3(position.x, position.y, 0f));
        return false; // skip original (hardcoded 700x700 logic)
    }
}
