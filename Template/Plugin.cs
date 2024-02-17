using BepInEx;
using BepInEx.Logging;
using CatsArmy.patch;
using CatsArmy.service;
using HarmonyLib;
using UnityEngine;

namespace CatsArmy;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; set; }

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony harmony = new(PluginInfo.PLUGIN_GUID);
    private const GameObject Object = null;

    public TemplateService Service;

    public Plugin()
    {
        Instance = this;
    }

    private void Awake()
    {
        Service = new TemplateService();

        Log.LogInfo($"Applying patches...");
        ApplyPluginPatch();
        Log.LogInfo($"Patches applied");
        if (Object.activeSelf)
        {

        }
    }

    /// <summary>
    /// Applies the patch to the game.
    /// </summary>
    private void ApplyPluginPatch()
    {
        harmony.PatchAll(typeof(ShipLightsPatch));
        harmony.PatchAll(typeof(PlayerControllerBPatch));
    }
}
