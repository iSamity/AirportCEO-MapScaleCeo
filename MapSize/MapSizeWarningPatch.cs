using System.Collections.Generic;
using HarmonyLib;
using MapScaleCeo.Config;
using UnityEngine;

namespace MapScaleCeo.MapSize;

[HarmonyPatch(typeof(MainMenuWorldController))]
internal class MapSizeWarningPatch
{
    private static bool _skipWarningCheck;
    private static readonly List<CanvasState> _canvasesToRestore = new List<CanvasState>();

    private struct CanvasState
    {
        public Canvas Canvas;
        public int OriginalOrder;
    }

    /// <summary>
    /// Reset when main menu scene loads so the warning shows again for the next new game (e.g. second airport).
    /// </summary>
    internal static void ResetSkipWarningCheck()
    {
        _skipWarningCheck = false;
    }

    /// <summary>
    /// When true, skip <see cref="CustomCameraScreenshot.CaptureTexture"/> so the screenshot runs only after
    /// the user confirms the custom map-size dialog (same gating as showing that dialog, minus NewGame-only checks).
    /// </summary>
    internal static bool ShouldDeferScreenshotForCustomMapWarning()
    {
        if (_skipWarningCheck)
            return false;

        var mapSize = DefaultConfig.MapSize.Value;
        if (mapSize.x == 0 || mapSize.y == 0)
            return false;

        return DialogPanel.Instance != null;
    }

    [HarmonyPatch(nameof(MainMenuWorldController.LaunchAirport))]
    [HarmonyPrefix]
    internal static bool LaunchAirport_Prefix(
        MainMenuWorldController __instance,
        Enums.GameLoadSetting gameLoadSetting,
        string path,
        bool isMod)
    {
        Plugin.Logger.LogInfo($"[MapSizeWarningPatch] LaunchAirport called with gameLoadSetting={gameLoadSetting}, path={path}, isMod={isMod}");

        // Only check for new games
        if (gameLoadSetting != Enums.GameLoadSetting.NewGame)
        {
            Plugin.Logger.LogInfo("[MapSizeWarningPatch] Not a new game, skipping check");
            return true;
        }

        // Skip if we already showed the warning
        if (_skipWarningCheck)
        {
            Plugin.Logger.LogInfo("[MapSizeWarningPatch] Skipping warning check (already shown)");
            return true;
        }

        var mapSize = DefaultConfig.MapSize.Value;
        Plugin.Logger.LogInfo($"[MapSizeWarningPatch] MapSize from config: ({mapSize.x}, {mapSize.y})");

        // No custom size set, proceed normally
        if (mapSize.x == 0 || mapSize.y == 0)
        {
            Plugin.Logger.LogInfo("[MapSizeWarningPatch] MapSize is zero, proceeding normally");
            return true;
        }

        // Check if DialogPanel is available
        if (DialogPanel.Instance == null)
        {
            Plugin.Logger.LogWarning("[MapSizeWarningPatch] DialogPanel.Instance is null! Cannot show warning.");
            return true;
        }

        // Bring dialog to front by increasing canvas sorting order
        BringDialogToFront();

        Plugin.Logger.LogInfo("[MapSizeWarningPatch] Showing warning dialog...");

        // Show warning dialog for custom map size
        DialogPanel.Instance.ShowQuestionPanel(
            (result) => OnWarningResult(result, __instance, gameLoadSetting, path, isMod),
            $"Custom Map Size\n\nYou have set a custom map size of {mapSize.x} x {mapSize.y}.\n\nDo you want to continue?",
            true
        );

        return false; // Don't run original yet
    }

    private static void BringDialogToFront()
    {
        _canvasesToRestore.Clear();

        // Get ALL canvases in the DialogPanel hierarchy (including children)
        var allCanvases = DialogPanel.Instance.GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in allCanvases)
        {
            var state = new CanvasState
            {
                Canvas = canvas,
                OriginalOrder = canvas.sortingOrder
            };
            _canvasesToRestore.Add(state);
            canvas.sortingOrder = 999;
            Plugin.Logger.LogInfo($"[MapSizeWarningPatch] Set canvas '{canvas.name}' sortingOrder from {state.OriginalOrder} to 999");
        }

        // Also check parent canvases
        var parentCanvas = DialogPanel.Instance.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            // Check if we already have this canvas
            var alreadyAdded = false;
            foreach (var c in allCanvases)
            {
                if (c == parentCanvas)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
            {
                var state = new CanvasState
                {
                    Canvas = parentCanvas,
                    OriginalOrder = parentCanvas.sortingOrder
                };
                _canvasesToRestore.Add(state);
                parentCanvas.sortingOrder = 999;
                Plugin.Logger.LogInfo($"[MapSizeWarningPatch] Set parent canvas '{parentCanvas.name}' sortingOrder to 999");
            }
        }

        if (_canvasesToRestore.Count == 0)
        {
            Plugin.Logger.LogWarning("[MapSizeWarningPatch] Could not find any Canvas on DialogPanel");
        }
        else
        {
            Plugin.Logger.LogInfo($"[MapSizeWarningPatch] Brought {_canvasesToRestore.Count} canvas(es) to front");
        }
    }

    private static void RestoreDialogSortingOrder()
    {
        foreach (var state in _canvasesToRestore)
        {
            if (state.Canvas != null)
            {
                state.Canvas.sortingOrder = state.OriginalOrder;
                Plugin.Logger.LogInfo($"[MapSizeWarningPatch] Restored canvas '{state.Canvas.name}' sortingOrder to {state.OriginalOrder}");
            }
        }
        _canvasesToRestore.Clear();
    }

    private static void OnWarningResult(
        bool continueAnyway,
        MainMenuWorldController instance,
        Enums.GameLoadSetting gameLoadSetting,
        string path,
        bool isMod)
    {
        Plugin.Logger.LogInfo($"[MapSizeWarningPatch] Dialog result: continueAnyway={continueAnyway}");

        // Restore the original sorting order
        RestoreDialogSortingOrder();

        if (continueAnyway)
        {
            _skipWarningCheck = true;

            // Below two functions are the same as in the game when pressing continue
            Singleton<CustomCameraScreenshot>.Instance?.CaptureTexture();
            instance.LaunchAirport(gameLoadSetting, path, isMod);
        }
        else
        {
            Plugin.Logger.LogInfo("[MapSizeWarningPatch] User cancelled, not launching");
        }
    }

    /// <summary>
    /// Immediately hide menu panels to provide instant feedback that loading is starting
    /// </summary>
    private static void ShowLoadingFeedback(MainMenuWorldController instance)
    {
        try
        {
            // Hide all sub panels immediately
            // if (MainMenuUI.Instance != null)
            // {
            //     MainMenuUI.Instance.HideAllSubPanels();
            //     Plugin.Logger.LogInfo("[MapSizeWarningPatch] Hidden all sub panels");
            // }

            // Hide the airport creation menu panel
            if (instance.airportCreationMenuPanel != null)
            {
                instance.airportCreationMenuPanel.ShowHidePanel(false);
                Plugin.Logger.LogInfo("[MapSizeWarningPatch] Hidden airport creation menu panel");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogWarning($"[MapSizeWarningPatch] Error showing loading feedback: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(CustomCameraScreenshot), nameof(CustomCameraScreenshot.CaptureTexture))]
internal static class CustomCameraScreenshotDeferCapturePatch
{
    [HarmonyPrefix]
    internal static bool CaptureTexture_Prefix()
    {
        if (MapSizeWarningPatch.ShouldDeferScreenshotForCustomMapWarning())
        {
            Plugin.Logger.LogInfo("[MapSizeWarningPatch] Deferring CaptureTexture until dialog Continue");
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(MainMenuWorldController), "Awake")]
internal static class MapSizeWarningResetPatch
{
    [HarmonyPostfix]
    internal static void Awake_Postfix()
    {
        MapSizeWarningPatch.ResetSkipWarningCheck();
    }
}
