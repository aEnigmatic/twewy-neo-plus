using HarmonyLib;
using Il2Cpp;
using Il2CppMaster;
using NeoPlus.Helpers;

namespace NeoPlus.Patches.Fixes;

// ReSharper disable InconsistentNaming
[HarmonyPatch]
public static class FixBadgeNewPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveDataBadge), nameof(SaveDataBadge.AddMyBadge))]
    private static void SaveDataBadge_AddMyBadge(SaveDataBadge __instance, Badge.ELabel id, ref bool isNew) {
        // fix all badges being marked as NEW
        if (!Configuration.EnableNewBadgeFix.Value)
            return;

        isNew = !id.IsKnown();
    }
}