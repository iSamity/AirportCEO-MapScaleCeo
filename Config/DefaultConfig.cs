using BepInEx.Configuration;
using UnityEngine;

namespace MapScaleCeo.Config;

static class DefaultConfig
{
    internal static ConfigEntry<Vector2> NewGameMapSize { get; private set; }

    public static void Setup()
    {
        NewGameMapSize = ConfigReference.Bind(
            "General",
            "Map Size (New Game)",
            new Vector2(),
            "Width and height for the next NEW airport only (0,0 = vanilla size for Normal/Large). Loaded saves always use the save file; terrain follows the live grid.");
    }

    static ConfigFile ConfigReference => Plugin.ConfigReference;
}
