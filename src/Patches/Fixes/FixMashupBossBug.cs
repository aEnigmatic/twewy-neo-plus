using HarmonyLib;
using Il2Cpp;

namespace NeoPlus.Patches.Fixes;

[HarmonyPatch]
public static class FixMashupBossBug {
    public static bool ForceMashUp { get; set; }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerTeamManager), nameof(PlayerTeamManager.OnEndMashup))]
    private static void PlayerTeamManager_OnEndMashup() {
        if (!Configuration.EnableMashupFix.Value)
            return;

        ForceMashUp = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BattleScene), nameof(BattleScene.EntryBossEraceEffect), typeof(EnemyBase))]
    private static void BattleScene_EntryBossEraseEffect(EnemyBase target) {
        if (!Configuration.EnableMashupFix.Value)
            return;

        var mashup = PlayerTeamManager.Instance.Mashup;
        if (mashup is null || !mashup.GetTriggerEnemyDeadSpecialAttack())
            return;

        ForceMashUp = true;
    }
}