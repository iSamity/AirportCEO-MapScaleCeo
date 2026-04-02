using MapScaleCeo.Config;
using UnityEngine;

namespace MapScaleCeo.MapSize;

/// <summary>
/// Vanilla footprint XY from live <see cref="GridManager.defaultGrid"/> / <see cref="GridManager.defaultGridLarge"/>.
/// </summary>
/// <remarks>
/// Optional future: if a save deserializes with worldSize.x/y zero, vanilla fills from GridManager.defaultGrid in
/// AirportData.SetAirportDataFromSerialization — add a Harmony Postfix there only if playtesting shows bad loads.
/// </remarks>
internal static class MapSizeHelper
{
    /// <summary>
    /// True when the live grid XY is neither stock Normal (700×700) nor stock Large (1050×700) from
    /// <see cref="GridManager.defaultGrid"/> / <see cref="GridManager.defaultGridLarge"/>. Independent of
    /// <see cref="AirportData.worldSizeType"/> — e.g. Large layout at 3000×3000 is custom; Normal at 1050×700 is treated as the Large default footprint.
    /// </summary>
    internal static bool FootprintIsNonDefaultVanillaSize(string caller = null, bool log = true)
    {
        var wx = GridManager.worldSizeX;
        var wy = GridManager.worldSizeY;
        if (wx <= 0 || wy <= 0)
        {
            if (log)
                Plugin.Logger.LogInfo(
                    $"[MSC] FootprintNonDefault ({caller ?? "?"}): false — grid not ready (worldSizeX={wx}, worldSizeY={wy}).");
            return false;
        }

        var n = GridManager.defaultGrid;
        var l = GridManager.defaultGridLarge;
        var isNormalStock = wx == (int)n.x && wy == (int)n.y;
        var isLargeStock = wx == (int)l.x && wy == (int)l.y;
        var custom = !isNormalStock && !isLargeStock;
        if (log)
            Plugin.Logger.LogInfo(
                $"[MSC] FootprintNonDefault ({caller ?? "?"}): {custom} — grid {wx}x{wy}; stock Normal {n.x}x{n.y}, stock Large {l.x}x{l.y}.");
        return custom;
    }

    internal static Vector2 GetVanillaFootprintXY(Enums.WorldSize worldSizeType)
    {
        var v = worldSizeType == Enums.WorldSize.Large
            ? GridManager.defaultGridLarge
            : GridManager.defaultGrid;
        return new Vector2(v.x, v.y);
    }

    /// <summary>
    /// XY for a new airport: config when set, otherwise vanilla for the selected world size type.
    /// </summary>
    internal static Vector2 GetEffectiveNewGameFootprintXY(Enums.WorldSize worldSizeType)
    {
        var entry = DefaultConfig.NewGameMapSize;
        var cfg = entry != null ? entry.Value : default;
        if (cfg.x > 0f && cfg.y > 0f)
            return cfg;

        return GetVanillaFootprintXY(worldSizeType);
    }

    /// <summary>
    /// Applies mod footprint to <paramref name="airportData"/> for new-game flows only (caller must gate on load setting).
    /// Preserves existing Z when non-zero; otherwise uses vanilla depth 5.
    /// </summary>
    internal static void ApplyNewGameWorldSizeXY(AirportData airportData)
    {
        var xy = GetEffectiveNewGameFootprintXY(airportData.worldSizeType);
        var z = airportData.worldSize.z;
        if (Mathf.Approximately(z, 0f))
        {
            var vanilla = airportData.worldSizeType == Enums.WorldSize.Large
                ? GridManager.defaultGridLarge
                : GridManager.defaultGrid;
            z = vanilla.z;
        }

        airportData.worldSize = new Vector3(xy.x, xy.y, z);
    }
}
