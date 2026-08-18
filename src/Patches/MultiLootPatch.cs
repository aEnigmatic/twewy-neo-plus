using HarmonyLib;
using NeoPlus.Helpers;
using NeoPlus.Patches.Fixes;
using UnityEngine;
using Il2Cpp;
using Il2CppMaster;

namespace NeoPlus.Patches;

[HarmonyPatch]
// ReSharper disable InconsistentNaming
class MultiLootPatch {
    private static float CurrentLootBonus { get; set; } = 1f;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BattleScene), nameof(BattleScene.OnKillEnemy))]
    private static void BattleSceneOnKillEnemy(BattleScene __instance, EnemyBase killedEnemy, Character attacker, bool showPrize) {
        if (Configuration.ActiveLootMode.Value == LootMode.Original)
            return;

        var mashupDropRateBonus = GetMashupDropRateBonus();

        NeoPlus.Logger.Msg($"[{killedEnemy.GetName()}]" + (mashupDropRateBonus > 1.0 ? $" (Killer Remix: {mashupDropRateBonus:F0}x)" : ""));

        DropLogic.Execute(__instance, killedEnemy, mashupDropRateBonus);
    }

    private static float GetMashupDropRateBonus()
        => PlayerTeamManager.Instance.Mashup is {} mashup && (mashup.GetTriggerEnemyDeadSpecialAttack() || FixMashupBossBug.ForceMashUp)
               ? mashup.DropRateUpBySpecialAttack()
               : 1f;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BattleScene), nameof(BattleScene.OnStart))]
    private static void BattleSceneOnStart() {
        NeoPlus.Logger.Msg($"[BattleScene.OnStart]");
        CurrentLootBonus = LootHelper.GetLootBonus();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BattleDropItemManager), nameof(BattleDropItemManager.LotteryDrop))]
    private static bool BattleDropItemManager_LotteryDrop(ref DifficultyLevel.ELabel __result) {
        if (Configuration.ActiveLootMode.Value == LootMode.Original)
            return true;

        // do not run original logic
        __result = DifficultyLevel.ELabel.Invalid;
        return false;
    }

    private static class DropLogic {
        private static readonly char[] Difficulty = ['E', 'N', 'H', 'U'];

        private static DifficultyLevel.ELabel GetDifficulty()
            => SaveLoadController.Get<SaveDataBattle>().Difficulty;

        private static bool ShouldShowDrop(BattleScene scene)
            => scene.mSharedParam is { IsScenarioLastBattle: false, IsFinalTimeAttack: false };

        public static void Execute(BattleScene battleScene, EnemyBase killedEnemy, float lootBonus) {
            if (killedEnemy.EnemyDataMD.IsPigWithoutFuuyaGold())
                return;

            var shouldShowDrop    = ShouldShowDrop(battleScene);
            var currentDifficulty = (int) GetDifficulty();
            var totalLootBonus    = CurrentLootBonus * lootBonus;

            // roll for all difficulties 
            switch (Configuration.ActiveLootMode.Value) {
                case LootMode.MultiLoot:
                    for (var diff = currentDifficulty; diff >= 0; --diff)
                        PerformRolls(killedEnemy, diff, totalLootBonus, shouldShowDrop);
                    break;

                case LootMode.OncePerDifficulty:
                    for (var diff = currentDifficulty; diff >= 0; --diff)
                        PerformSingle(killedEnemy, diff, totalLootBonus, shouldShowDrop);
                    break;

                case LootMode.Original:
                    PerformSingle(killedEnemy, currentDifficulty, totalLootBonus, shouldShowDrop);
                    break;

                default:
                    throw new ArgumentException(nameof(Configuration.ActiveLootMode));
            }
        }

        private static void PerformSingle(EnemyBase killedEnemy, int diff, float totalLootBonus, bool shouldShowDrop) {
            // setup
            var enemyData = killedEnemy.EnemyDataMD;
            var badgeId   = enemyData.Drop[diff];
            if (badgeId == Badge.ELabel.Invalid)
                return;

            var dropChance = enemyData.DropRate[diff];
            if (dropChance <= 0.0f)
                return;

            var random = BattleDropItemManager.Instance.Random.mRandom;

            // roll
            var currentRate = dropChance * totalLootBonus;
            var success     = currentRate >= 1.0 || random.NextDouble() <= currentRate;

            // add to loot
            if (success)
                HandleBadgeDrop(killedEnemy, diff, shouldShowDrop);
        }

        private static void HandleBadgeDrop(EnemyBase killedEnemy, int diff, bool shouldShowDrop) {
            var result = BattleDropItemManager.SetDrop(killedEnemy, (DifficultyLevel.ELabel) diff);
            if (result == Badge.ELabel.Invalid) {
                NeoPlus.Logger.Warning("DropLogic.AddPinToBattle: invalid badge (max drops reached?)");
                return;
            }

            // add to battlefield
            if (shouldShowDrop)
                AddPinToBattle(killedEnemy, diff, result);
        }


        private static void PerformRolls(EnemyBase killedEnemy, int diff, float bonus, bool shouldShowDrop) {
            // setup
            var enemyData = killedEnemy.EnemyDataMD;
            var badgeId   = enemyData.Drop[diff];
            if (badgeId == Badge.ELabel.Invalid)
                return;

            var dropChance = enemyData.DropRate[diff];
            if (dropChance <= 0.0f)
                return;

            var badge  = MasterDataBase<Badge>.Get((int) badgeId);
            var random = BattleDropItemManager.Instance.Random.mRandom;

            // roll
            var rolls    = new List<float>();
            var multiMod = 1.0f;
            while (true) {
                // current drop change
                var currentRate = dropChance * bonus * multiMod;
                rolls.Add(currentRate);

                // roll
                var success = currentRate >= 1.0 || random.NextDouble() <= currentRate;
                if (!success)
                    break;

                // badge drop
                HandleBadgeDrop(killedEnemy, diff, shouldShowDrop);

                // no extra loot for piggies
                var isPig = enemyData.IsPig();
                if (isPig)
                    break;

                // reduce the chance of the same drop
                multiMod *= Math.Min(dropChance, 0.75f);
            }

            // debug output
            var badgeName = badge.GetName().Replace("・", " - ");
            var successes = rolls.Count - 1;
            var rollsStr  = string.Join(" ➜ ", rolls.Select(x => (x < 0.10 ? $"{x:P2}" : $"{x:P0}").PadLeft(8)));

            NeoPlus.Logger.Msg($"\t{Difficulty[diff]} {successes.ToString(),2}x {badgeName,-28} | {$"{dropChance:P2}",8} ➜ {rollsStr}");
        }

        private static void AddPinToBattle(EnemyBase killedEnemy, int diff, Badge.ELabel badge) {
            if (!killedEnemy)
                return;

            // scatter pin a bit
            var pos = killedEnemy.Position;
            var vec = new Vector3(
                                  pos.x + UnityEngine.Random.RandomRange(-1f, 1f),
                                  pos.y + UnityEngine.Random.RandomRange(-1f, 1f),
                                  UnityEngine.Random.RandomRange(0.5f, 1f)
                                 );

            // drop it
            PrizeFactory.ShowBadgeDropPrize(vec, diff, badge, killedEnemy.OverridePrizeShowDelay, killedEnemy.OverridePrizeMoveDelay);
        }
    }
}