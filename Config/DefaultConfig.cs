using BepInEx.Configuration;
using MapScaleCeo;
using UnityEngine;

namespace MapScaleCeo.Config;

static class DefaultConfig
{
    internal static ConfigEntry<Vector2> NewGameMapSize { get; private set; }

    /// <summary>When true (default), custom footprints use a procedurally tiled building grid instead of vanilla sprites.</summary>
    internal static ConfigEntry<bool> ImproveBuildingGrid { get; private set; }

    public static void Setup()
    {
        NewGameMapSize = ConfigReference.Bind(
            "General",
            "Map Size (New Game)",
            new Vector2(),
            "Width and height for the next NEW airport only (0,0 = vanilla size for Normal/Large). Negative values are clamped to 0. Loaded saves always use the save file; terrain follows the live grid.");

        ImproveBuildingGrid = ConfigReference.Bind(
            "Building grid",
            "ImproveBuildingGrid",
            true,
            "On custom map sizes, replace the vanilla building grid with a full tiled grid. " +
            "Enabling this can cause performance issues on very large maps (many extra sprites).");

        ClampNewGameMapSizeIfNegative();
        NewGameMapSize.SettingChanged += (_, __) => ClampNewGameMapSizeIfNegative();
    }

    /// <summary>
    /// Config files or the runtime config UI can supply negative components; treat them as 0 so behavior matches
    /// <see cref="MapScaleCeo.MapSize.MapSizeHelper.GetEffectiveNewGameFootprintXY"/>.
    /// </summary>
    static void ClampNewGameMapSizeIfNegative()
    {
        if (NewGameMapSize == null)
            return;

        var v = NewGameMapSize.Value;
        if (v.x >= 0f && v.y >= 0f)
            return;

        NewGameMapSize.Value = new Vector2(Mathf.Max(0f, v.x), Mathf.Max(0f, v.y));
        Plugin.Logger.LogWarning(
            $"[DefaultConfig] Map Size (New Game) had negative component(s); clamped to ({NewGameMapSize.Value.x}, {NewGameMapSize.Value.y}).");
    }

    static ConfigFile ConfigReference => Plugin.ConfigReference;
}
