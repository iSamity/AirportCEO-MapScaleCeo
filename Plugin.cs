using AirportCEOModLoader.WatermarkUtils;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MapScaleCeo.Config;
using MapScaleCeo.MapSize;

namespace MapScaleCeo;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static ConfigFile ConfigReference { get; private set; }

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        ConfigReference = base.Config;

        DefaultConfig.Setup();

        // TODO: Fix doesn't reset when the game is save, menu and create new airport
        MapSizeService.Initialize();

        SetupHarmony();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Start()
    {
        SetupModLoader();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} - Finished start");
    }


    private void SetupHarmony()
    {
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} - Setting up Harmony.");

        var harmony = new HarmonyLib.Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} - Finished up Harmony.");
    }

    private void SetupModLoader()
    {
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} - Setting up Mod Loader.");

#if DEBUG
        WatermarkUtils.Register(new WatermarkInfo(MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, false));
#else
        WatermarkUtils.Register(new WatermarkInfo("MSC", MyPluginInfo.PLUGIN_VERSION, true));
#endif
    }
}