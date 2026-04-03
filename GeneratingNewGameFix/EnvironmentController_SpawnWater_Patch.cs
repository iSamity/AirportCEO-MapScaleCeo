using System.Collections.Generic;
using HarmonyLib;
using MapScaleCeo.MapSize;
using UnityEngine;

namespace MapScaleCeo.GeneratingNewGameFix;

/// <summary>
/// Keeps noise square at <c>mapSizeModifier × defaultGrid</c> (same effective size vanilla uses on X and for Y after capping), so Perlin is not stretched on tall maps.
/// A 1:1 noise-to-world loop only covers a ~¾×¾ tile window from the origin; on large custom footprints most cells never get a sample and look "skipped".
/// After generating the square map, sample it with bilinear UVs across the full <c>worldSizeX × worldSizeY</c> grid so ponds can appear everywhere.
/// Reference: AirportCeo-publicedcode <c>EnvironmentController.SpawnWater</c>.
/// </summary>
[HarmonyPatch(typeof(EnvironmentController))]
internal static class EnvironmentController_SpawnWater_Patch
{
    /// <summary>Width of the border band where ponds are less likely, as a fraction of <c>min(width,height)</c>. 0 = no edge penalty.</summary>
    private const float EdgeMarginFractionOfMinSide = 0.06f;

    /// <summary>1 = smooth ramp; higher pushes suppression toward the map edge (typical range ~0.5–2).</summary>
    private const float EdgeFalloffExponent = 1f;

    /// <summary>1 = default pond count vs random threshold; higher = more lakes (e.g. 1.25); lower = fewer.</summary>
    private const float LakeDensity = 1f;

    [HarmonyPatch("SpawnWater")]
    [HarmonyPrefix]
    private static bool SpawnWater_Prefix(EnvironmentController __instance, ref HashSet<Vector2> __result)
    {
        var load = Singleton<SaveLoadGameDataController>.Instance?.inputGameLoadSetting;
        if (load != Enums.GameLoadSetting.NewGame && load != Enums.GameLoadSetting.EditorNewGame)
        {
            Plugin.Logger.LogInfo($"[MSC] SpawnWater prefix: skip (not new game) loadSetting={load}.");
            return true;
        }

        if (!MapSizeHelper.FootprintIsNonDefaultVanillaSize(nameof(SpawnWater_Prefix)))
            return true;

        var mapSizeModifier = Traverse.Create(__instance).Field("mapSizeModifier").GetValue<float>();

        var hashSet = new HashSet<Vector2>();
        var scale = Utils.RandomRangeF(
            __instance.waterScale * (1f - __instance.waterScaleRandomizer),
            __instance.waterScale * (1f + __instance.waterScaleRandomizer));
        var depthThreshold = Utils.RandomRangeF(
            __instance.waterDepth * (1f - __instance.waterDepthRandmizier),
            __instance.waterDepth * (1f + __instance.waterDepthRandmizier));
        var lakeDensity = Mathf.Max(0.001f, LakeDensity);
        var effectiveThreshold = depthThreshold / lakeDensity;

        var wx = GridManager.worldSizeX;
        var wy = GridManager.worldSizeY;
        var minSide = Mathf.Min(wx, wy);
        var marginFrac = Mathf.Clamp01(EdgeMarginFractionOfMinSide);
        var marginTiles = marginFrac * minSide;
        if (marginTiles > minSide * 0.499f)
            marginTiles = minSide * 0.499f;
        var edgeExponent = Mathf.Clamp(EdgeFalloffExponent, 0.25f, 4f);

        var num2 = (int)(GridManager.worldSizeX *
                        (mapSizeModifier * (GridManager.defaultGrid.x / GridManager.worldSizeX)));
        var num3 = (int)(GridManager.worldSizeY *
                        (mapSizeModifier * (GridManager.defaultGrid.y / GridManager.worldSizeY)));
        var array = Utils.GenerateNoiseMap(num2, num3, scale);
        var noiseH = array.GetLength(1);
        var noiseW = array.GetLength(0);
        var denomX = wx > 1 ? wx - 1 : 1;
        var denomY = wy > 1 ? wy - 1 : 1;
        var maxNx = Mathf.Max(0, noiseW - 1);
        var maxNy = Mathf.Max(0, noiseH - 1);
        for (var iy = 0; iy < wy; iy++)
        {
            var noiseV = maxNy * (iy / (float)denomY);
            for (var ix = 0; ix < wx; ix++)
            {
                var noiseU = maxNx * (ix / (float)denomX);
                if (SampleNoiseBilinear(array, noiseU, noiseV) <= effectiveThreshold)
                    continue;

                var edgeWeight = 1f;
                if (marginTiles > 0f)
                {
                    var dEdge = Mathf.Min(ix, iy, wx - 1 - ix, wy - 1 - iy);
                    edgeWeight = Mathf.SmoothStep(0f, marginTiles, dEdge);
                    if (!Mathf.Approximately(edgeExponent, 1f))
                        edgeWeight = Mathf.Pow(Mathf.Clamp01(edgeWeight), edgeExponent);
                }

                if (edgeWeight < 1f && Utils.RandomRangeF(0f, 1f) > edgeWeight)
                    continue;

                var cell = new Vector2(ix, iy);
                Singleton<TiledObjectsManager>.Instance.AddTileable(new PondTile(cell, isPlanned: false));
                hashSet.Add(cell);
            }
        }

        __result = hashSet;
        Plugin.Logger.LogInfo(
            $"[MSC] SpawnWater prefix: applied custom — mapSizeModifier={mapSizeModifier}, noiseSize {num2}x{num3}, world {wx}x{wy} (bilinear UV), " +
            $"pondCells={hashSet.Count}, scale={scale:F5}, depthThreshold={depthThreshold:F3}→effective {effectiveThreshold:F3} (lakeDensity={lakeDensity:F2}), " +
            $"edgeMarginTiles≈{marginTiles:F1} (frac={marginFrac:F3}, exp={edgeExponent:F2}).");
        return false;
    }

    /// <summary>First index is width (j), second is height (i), matching <see cref="Utils.GenerateNoiseMap"/>.</summary>
    private static float SampleNoiseBilinear(float[,] noise, float u, float v)
    {
        var w = noise.GetLength(0);
        var h = noise.GetLength(1);
        if (w <= 0 || h <= 0)
            return 0f;

        var maxX = w - 1;
        var maxY = h - 1;
        var su = Mathf.Clamp(u, 0f, maxX);
        var sv = Mathf.Clamp(v, 0f, maxY);
        var x0 = (int)su;
        var y0 = (int)sv;
        var x1 = Mathf.Min(x0 + 1, maxX);
        var y1 = Mathf.Min(y0 + 1, maxY);
        var tx = su - x0;
        var ty = sv - y0;
        var c00 = noise[x0, y0];
        var c10 = noise[x1, y0];
        var c01 = noise[x0, y1];
        var c11 = noise[x1, y1];
        return Mathf.Lerp(Mathf.Lerp(c00, c10, tx), Mathf.Lerp(c01, c11, tx), ty);
    }
}
