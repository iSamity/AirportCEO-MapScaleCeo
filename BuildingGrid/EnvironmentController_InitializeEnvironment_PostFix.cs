#nullable enable
using System.Collections.Generic;
using HarmonyLib;
using MapScaleCeo.MapSize;
using UnityEngine;

namespace MapScaleCeo.BuildingGrid;

class GridInfo
{
    public string Name { get; set; }
    public Vector3 Position { get; set; }
    public Vector2 Size { get; set; }

    public SpriteRenderer SpriteRenderer { get; set; }

    public GridInfo(string name, Vector3 position, Vector2 size, SpriteRenderer spriteRenderer)
    {
        Name = name;
        Position = position;
        Size = size;
        SpriteRenderer = spriteRenderer;
    }
}

[HarmonyPatch(typeof(EnvironmentController))]
internal static class EnvironmentController_InitializeEnvironment_Postfix
{
    internal static List<SpriteRenderer> extendedGrids = new List<SpriteRenderer>();
    internal static List<GridInfo> originalGrids = new List<GridInfo>();

    /// <summary>Vanilla FullGridLeft transform position (center); bottom row anchor.</summary>
    private const float FirstGridCenterX = 172.5f;

    private const float FirstGridCenterYBottom = 174.5f;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(EnvironmentController.InitializeEnvironment))]
    internal static void Postfix(EnvironmentController __instance, AirportData airportData, bool isNewGame)
    {
        extendedGrids.Clear();
        originalGrids.Clear();

        if (!MapSizeHelper.FootprintIsNonDefaultVanillaSize())
        {
            return;
        }

        // Left side
        // FullGridLeft position x:172.5 y:174.5 w:348 h:352
        // FullGridTopLeft positon x:172.5 y:525.5 w:348 h:350

        // Rigth side or center side depending on map size
        // FullGridTop position x: 523.5 y:525.5 w:354 h:350
        // FullGridTopCenter Position x:523.5 y:174.5 w:354 h:352

        // Only enabled when map is large
        // FullGridRight Position x:876.5 y:174.5 w:348 h:352
        // FullGridTopRight Position x:876.5y525.5 w:348 h:350
        // GridOverlap Position x:701.5y349..5 w:2 h:702
        var allGrids = __instance.allGrids;

        for (int i = 0; i < allGrids.Length; i++)
        {
            var grid = allGrids[i];
            if (grid == null)
                continue;

            originalGrids.Add(new GridInfo(grid.name, grid.transform.position, grid.size, grid));
        }

        DisableVanillaAllGrids(__instance);

        var fullGridLeft = FindGridLeft();
        if (fullGridLeft == null || fullGridLeft.SpriteRenderer == null)
            return;

        var worldSize = airportData.worldSize;
        BuildFullGridFromFullGridLeftOnly(fullGridLeft, worldSize.x, worldSize.y);
    }

    /// <summary>
    /// Vanilla <see cref="EnvironmentController.ToggleGrid"/> turns <see cref="EnvironmentController.allGrids"/> back on when the player shows the grid.
    /// Call this after init and from that postfix so only <see cref="extendedGrids"/> stay visible for custom footprints.
    /// </summary>
    internal static void DisableVanillaAllGrids(EnvironmentController environmentController)
    {
        var grids = environmentController.allGrids;
        if (grids == null)
            return;
        for (int i = 0; i < grids.Length; i++)
        {
            if (grids[i] != null)
                grids[i].enabled = false;
        }
    }

    private static SpriteRenderer CopyGridSpriteRenderer(SpriteRenderer grid)
    {
        Transform parent = grid.transform.parent;
        GameObject cloneGo = Object.Instantiate(grid.gameObject, parent, worldPositionStays: true);
        SpriteRenderer cloneSr = cloneGo.GetComponent<SpriteRenderer>();
        if (cloneSr == null)
            throw new System.InvalidOperationException("Cloned grid has no SpriteRenderer.");
        return cloneSr;
    }

    /// <summary>
    /// Tiles only <see cref="FullGridLeft"/> in X and Y to cover <paramref name="worldSizeX"/> by <paramref name="worldSizeY"/>.
    /// First tile centered at (172.5, 174.5).
    /// </summary>
    private static void BuildFullGridFromFullGridLeftOnly(GridInfo fullGridLeft, float worldSizeX, float worldSizeY)
    {
        float w = fullGridLeft.Size.x;
        float h = fullGridLeft.Size.y;
        if (w <= 0f || h <= 0f)
            return;

        float z = fullGridLeft.Position.z;

        int cols = Mathf.Max(1, Mathf.CeilToInt(worldSizeX / w));
        int rows = Mathf.Max(1, Mathf.CeilToInt(worldSizeY / h));

        for (int cy = 0; cy < rows; cy++)
        {
            float centerY = FirstGridCenterYBottom + cy * h;
            for (int cx = 0; cx < cols; cx++)
            {
                float centerX = FirstGridCenterX + cx * w;
                PlaceCloneAt(fullGridLeft, centerX, centerY, z);
            }
        }
    }

    private static void PlaceCloneAt(GridInfo template, float centerX, float centerY, float z)
    {
        var clone = CopyGridSpriteRenderer(template.SpriteRenderer);
        clone.transform.position = new Vector3(centerX, centerY, z);
        extendedGrids.Add(clone);
    }

    private static GridInfo? FindGridLeft()
    {
        foreach (var grid in originalGrids)
        {
            if (grid.Name == "FullGridLeft")
            {
                return grid;
            }
        }
        return null;
    }

}
