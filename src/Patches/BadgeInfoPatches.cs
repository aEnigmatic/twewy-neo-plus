using HarmonyLib;
using NeoPlus.Helpers;
using UnityEngine;
using UnityEngine.UI;
using Il2Cpp;
using Il2CppMaster;
using Il2CppUI;
using Il2CppUI.Panel;

namespace NeoPlus.Patches;

// ReSharper disable InconsistentNaming
[HarmonyPatch]
class BadgeInfoPatches {
    private const string ColorMasteredEvo = "#1FD23C";
    private const string ColorOwnedEvo    = "#1F3CD2";
    private const string ColorKnownEvo    = "#CFB53B";
    private const string ColorUnknownEvo  = "#D21F3C";

    private static int ImageWidth { get; set; } = 44;

    private static Image? getEvoImage(DescriptionPanel __instance)
        => __instance.mEvolutionImage.FirstOrDefault();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DescriptionPanel), MethodType.Constructor, typeof(IntPtr))]
    private static void DescriptionPanel_Constructor(DescriptionPanel __instance) {
        if (getEvoImage(__instance) is not {} firstImage)
            return;

        // remember image width
        ImageWidth = (int) firstImage.rectTransform.rect.width;

        // change character evo direction
        var layoutGroup = firstImage.transform.GetComponentInParent<LayoutGroup>();
        if (layoutGroup?.name != "Ico_Evolution")
            return;

        layoutGroup.padding.right  = 14;
        layoutGroup.childAlignment = TextAnchor.MiddleRight;
    }

    /// <summary>
    /// Called from BadgeEquipUI
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DescriptionPanel), nameof(DescriptionPanel.SetBadgeUIInfoDescription))]
    private static void DescriptionPanel_SetBadgeUIInfoDescription(DescriptionPanel __instance, BadgeUIInfo? info) {
        if (!Configuration.ShowBadgeInfo.Value)
            return;

        if (info?.MasterData is not {} badge)
            return;

        UpdatePanel(__instance, badge);
    }

    /// <summary>
    /// Called from ShopUI 
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DescriptionPanel), nameof(DescriptionPanel.SetBadgeDescription))]
    private static void DescriptionPanel_SetBadgeDescription(DescriptionPanel __instance, Badge? info) {
        if (!Configuration.ShowBadgeInfo.Value)
            return;

        if (info is null)
            return;

        UpdatePanel(__instance, info);
    }

    private static void UpdatePanel(DescriptionPanel panel, Badge badge) {
        // check if we have to do anything
        var hasEvolution = badge.EvolutionLevel > 1;
        if (!hasEvolution)
            return;

        // get evolution info
        var isCommonEvo    = true;
        var evolvedBadgeId = badge.EcolutionCommon;
        if (evolvedBadgeId == Badge.ELabel.Invalid) {
            var characterEvo = GetCharacterEvoInfo(badge);
            if (!characterEvo.Any())
                return;

            // assume only one possible evolution & character
            isCommonEvo = false;
            var evo = characterEvo.First();
            evolvedBadgeId = evo.BadgeId;

            panel.mEvolutionBadgeChara.Clear();
            panel.mEvolutionBadgeChara.Add(evo.Character);
        }

        // check if we know the evo
        panel.SetEvolutionText(GenerateText(badge, evolvedBadgeId, isCommonEvo), !isCommonEvo);

        // ensure text does not overlap with first image
        var layoutGroup = panel.mEvolutionText.transform.GetComponentInParent<LayoutGroup>();
        if (layoutGroup?.name != "block_Evolution")
            return;

        var numEvoImages = panel.mEvolutionBadgeChara.Count;
        layoutGroup.padding.right = 4 + numEvoImages * ImageWidth;
    }

    private static string GenerateText(Badge badge, Badge.ELabel evolvedBadgeId, bool isCommon) {
        var evolvedBadge = MasterDataBase<Badge>.Get((int) evolvedBadgeId);

        var isKnownEvo = evolvedBadge.IsKnown();
        var isOwned    = isKnownEvo && evolvedBadge.IsOwned();
        var isMastered = isKnownEvo && evolvedBadge.IsMastered();

        var insightUnlocked = SkillUtility.IsBadgeEvolution();

        if (!insightUnlocked && !isKnownEvo)
            // show "yes" or "???"
            return isCommon
                       ? Color(TextManager.GetText("Badge_Evolution01"), ColorUnknownEvo)
                       : Color(TextManager.GetText("Badge_Evolution02"), ColorUnknownEvo);

        // show full info

        string color;
        if (isMastered)
            color = ColorMasteredEvo;

        else if (isOwned)
            color = ColorOwnedEvo;

        else if (isKnownEvo)
            color = ColorKnownEvo;

        else
            color = ColorUnknownEvo;

        return $"{TextManager.GetText("Com_Lv")} {Color(badge.EvolutionLevel.ToString())} {Color(evolvedBadge.GetName(), color)}";
    }

    private record CharacterEvoInfo(Badge.ELabel BadgeId, int Character);

    private static CharacterEvoInfo[] GetCharacterEvoInfo(Badge badge) {
        var characterEvo = badge.EvolutionBadge
                                .Select((e, i) => new CharacterEvoInfo(e, (1 + i)))
                                .Where(evoInfo => evoInfo.BadgeId != Badge.ELabel.Invalid)
                                .ToArray();

        if (characterEvo.Length > 1)
            NeoPlus.Logger.Warning($"Multi evo? {string.Join(", ", characterEvo.Select(info => $"{info.Character}: {info.BadgeId}"))}");

        return characterEvo;
    }

    public static string Color(string str, string color = "#4BC8F0") => $"<color={color}>{str}</color>";
}