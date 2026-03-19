using BepInEx.Configuration;
using UnityEngine;
using MapScaleCeo.MapSize;

namespace MapScaleCeo.Config;

static class DefaultConfig
{
    internal static ConfigEntry<Vector2> MapSize { get; private set; }

    public static void Setup()
    {
        // TODO if i reset the map size, save it and open it next time it will used the set size however the unlockable areas will not be scaled to the new size
        MapSize = ConfigReference.Bind("General", "Map Size", new Vector2(), "The size of the map if empty it will use the default size, press reset to use the default size");
    }

    static ConfigFile ConfigReference => Plugin.ConfigReference;
}
