using AirportCEOModLoader.Core;
using HarmonyLib;
using MapScaleCeo.Config;

namespace MapScaleCeo.WelcomeMessage;

/// <summary>
/// Queues the mod welcome via <see cref="DialogUtils.QueueDialog"/> when the update panel UI is ready,
/// matching the Airport CEO Mod Loader pattern (postfix on <see cref="UpdatePanelUI.DisplayOnlyUpdateButtons"/>).
/// </summary>
[HarmonyPatch(typeof(UpdatePanelUI), nameof(UpdatePanelUI.DisplayOnlyUpdateButtons))]
internal static class UpdatePanelUI_WelcomeMessage_Postfix
{
    static bool _welcomeQueued;

    [HarmonyPostfix]
    internal static void Postfix()
    {
        if (_welcomeQueued)
            return;

        var entry = DefaultConfig.ShowWelcomeMessage;
        if (entry == null || !entry.Value)
            return;

        _welcomeQueued = true;

        var body =
            $"Thanks for using {MyPluginInfo.PLUGIN_NAME} (v{MyPluginInfo.PLUGIN_VERSION}).\n\n" +
            "This mod adjusts map footprint, camera, building grid, and related systems for custom sizes.\n\n" +
            "Open the BepInEx config (f1) for this plugin to change map size for new games and grid options.";

        DialogUtils.QueueDialog(body);
        entry.Value = false;
    }
}
