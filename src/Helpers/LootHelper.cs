using Il2Cpp;

namespace NeoPlus.Helpers;

public static class LootHelper {
    public static float GetLootBonus() {
        var scene        = SceneBase.GetInstance<BattleScene>();
        var battleParams = ScriptableObjectManager.GetSO<BattleCommonParamScriptableObject>();

        // difficulty bonus (maxLevel - curLevel)
        float teamDropAdd = PlayerTeamManager.Instance.AddDropRate;

        // item bonus
        var difficultyMod = SaveLoadController.Get<SaveDataPlayerTeam>().DropRate;

        // chain bonus
        var numRounds  = scene.MaxRound;
        var chainBonus = Math.Min(numRounds * battleParams.DropRateOnRoundCount, battleParams.DropRateOnRoundCountMax);

        // rare noise bonus
        var noiseBonus = scene.IsRareBattle
                             ? battleParams.DropRateUpByRareBattle
                             : 1.0f;

        // total
        var totalBonus = (difficultyMod + teamDropAdd) * chainBonus * noiseBonus;
        NeoPlus.Logger.Msg("LootBonus: " + totalBonus.ToString("N1") + "x"
                                          + " = [" + difficultyMod.ToString("N1") + "x (baseMod) + " + teamDropAdd.ToString("N1") + "x (items)]"
                                          + " * " + chainBonus.ToString("F0") + " (chain bonus)"
                                          + " * " + noiseBonus.ToString("F0") + " (rare noise bonus)");

        return totalBonus;
    }
}