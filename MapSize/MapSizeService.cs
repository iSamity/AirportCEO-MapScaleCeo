using System;
using System.Collections.Generic;
using System.Reflection;
using MapScaleCeo.Config;
using UnityEngine;

namespace MapScaleCeo.MapSize;

internal class MapSizeService
{
    /// <summary>
    /// Structure to hold grid prefab information extracted dynamically
    /// </summary>
    private struct GridInfo
    {
        public string Name;
        public Vector2 Center;
        public Vector2 Size;
        public GameObject Grid;
    }

    public static void Initialize()
    {
        var mapSize = DefaultConfig.MapSize.Value;

        if (mapSize.x == 0 || mapSize.y == 0)
        {
            return;
        }

        var customGridSize = mapSize.SetZ(5f);
        var defaultGridField = typeof(GridManager).GetField(nameof(GridManager.defaultGrid),
            BindingFlags.Static | BindingFlags.Public);
        var defaultGridLargeField = typeof(GridManager).GetField(nameof(GridManager.defaultGridLarge),
            BindingFlags.Static | BindingFlags.Public);

        if (defaultGridField != null && defaultGridLargeField != null)
        {
            defaultGridField.SetValue(null, customGridSize);
            defaultGridLargeField.SetValue(null, customGridSize);
            Plugin.Logger.LogInfo($"[MapSizeService] grid default and large overridden to: {customGridSize}");
        }
    }
}
