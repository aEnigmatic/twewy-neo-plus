using HarmonyLib;
using Il2Cpp;

namespace NeoPlus.Patches;

[HarmonyPatch]
public static class StartGamePatch {
    public static event Action OnGameInit     = delegate {};
    public static event Action OnGameLoaded   = delegate {};
    public static event Action OnGameUnloaded = delegate {};

    private static bool _loaded;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveLoadController), nameof(SaveLoadController.LoadProcess))]
    private static void SaveLoadController_LoadProcess() {
        if (_loaded) {
            NeoPlus.Logger.Msg("Game unloaded");
            OnGameUnloaded?.Invoke();
            _loaded = false;
        }
        else {
            NeoPlus.Logger.Msg("Game init");
            OnGameInit?.Invoke();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FieldScene), nameof(FieldScene.OnStart))]
    private static void FieldScene_OnStart() {
        if (_loaded)
            return;

        NeoPlus.Logger.Msg("Game loaded");
        
        _loaded = true;
        OnGameLoaded?.Invoke();
    }
}