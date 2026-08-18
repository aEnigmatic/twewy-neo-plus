using HarmonyLib;
using Il2CppComicEvent;
using Il2CppCustomPlayables;

namespace NeoPlus.Patches;

// ReSharper disable InconsistentNaming
[HarmonyPatch]
public class ComicEventSpeedPatches {
    private static readonly float  OriginalSkipSpeed = ComicEventManager.AUTO_SPEED_SKIP;
    private static readonly double OriginalPlaySpeed = PlayableDirectorEx.DefaultSpeed;

    private static float SkipSpeed => Configuration.FasterCutscenesSpeed.Value;

    private static void SetComicEventSpeed() {
        if (!Configuration.EnableFasterCutscenes.Value)
            return;

        PlayableDirectorEx.DefaultSpeed   = SkipSpeed;
        ComicEventManager.AUTO_SPEED_SKIP = SkipSpeed;
        ComicEventManager.Instance.ApplyComicEventSpeed(SkipSpeed);
    }

    private static void ResetComicEventSpeed() {
        PlayableDirectorEx.DefaultSpeed   = OriginalPlaySpeed;
        ComicEventManager.AUTO_SPEED_SKIP = OriginalSkipSpeed;
        ComicEventManager.Instance.ApplyComicEventSpeed(OriginalPlaySpeed);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ComicEventDirector), nameof(ComicEventDirector.OnPlayBegin))]
    public static void ComicEventDirector_OnPlayBegin(ComicEventDirector __instance) {
        SetComicEventSpeed();
        
        if (!Configuration.AutoSkip.Value)
            return;

        var root = __instance.GetMostParent() ?? __instance;

        root.CustomSpeed(SkipSpeed);
        root.EnableAutomaticReproducing();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ComicEventDirector), nameof(ComicEventDirector.OnPlayEnd))]
    public static void ComicEventDirector_OnPlayEnd(ComicEventDirector __instance)
        => ResetComicEventSpeed();


    [HarmonyPrefix]
    [HarmonyPatch(typeof(ComicEventManager), nameof(ComicEventManager.CanAutoProgressEvent))]
    public static bool ComicEventManager_CanAutoProgressEvent(ComicEventManager __instance, ref bool __result) {
        __result = true;
        return false;
    }
}