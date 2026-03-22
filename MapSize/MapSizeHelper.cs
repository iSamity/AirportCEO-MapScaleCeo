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
