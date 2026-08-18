using System.ComponentModel;
using MelonLoader;
using UnityEngine;

namespace NeoPlus;

public enum LootMode {
    [Description("Roll once for the current difficulty.")]
    Original,

    [Description("Roll once for the current and lower difficulties.")]
    OncePerDifficulty,

    [Description("Roll until first failure.")]
    MultiLoot,
}

public static class Configuration {
    private static MelonPreferences_Category _category = null!;

    public static MelonPreferences_Entry<LootMode> ActiveLootMode { get; private set; } = null!;
    public static MelonPreferences_Entry<bool>     ShowBadgeInfo  { get; private set; } = null!;


    public static MelonPreferences_Entry<bool>  AutoSkip              { get; private set; } = null!;
    public static MelonPreferences_Entry<bool>  EnableFasterCutscenes { get; private set; } = null!;
    public static MelonPreferences_Entry<float> FasterCutscenesSpeed  { get; private set; } = null!;


    public static MelonPreferences_Entry<bool> EnableMashupFix   { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> EnableNewBadgeFix { get; private set; } = null!;

    public static MelonPreferences_Entry<KeyCode> SelectUiPrevPage  { get; private set; } = null!;
    public static MelonPreferences_Entry<KeyCode> SelectUiNextPage  { get; private set; } = null!;
    public static MelonPreferences_Entry<KeyCode> SelectUiFirstPage { get; private set; } = null!;
    public static MelonPreferences_Entry<KeyCode> SelectUiLastPage  { get; private set; } = null!;
    
    public static void Init() {
        _category = MelonPreferences.CreateCategory("TWSWM", "TWSWM");

        ActiveLootMode = _category.CreateEntry("LootMode", LootMode.MultiLoot, description: "Loot mode");
        ShowBadgeInfo  = _category.CreateEntry("ShowBadgeInfo", true, description: "Shows detailed evolution info and ownership status on badge descriptions.");

        AutoSkip              = _category.CreateEntry("AutoSkip", false, description: "Automatically switch to skip-mode in comic scenes.");
        EnableFasterCutscenes = _category.CreateEntry("EnableFasterCutscenes", true, description: "Allows skipping cutscenes faster.");
        FasterCutscenesSpeed  = _category.CreateEntry("FasterCutscenesSpeed", 100f, description: "The speed multiplier for faster cutscenes.");

        EnableMashupFix   = _category.CreateEntry("FixMashup", true, description: "Fixes the bug where Killer Remix drop rate bonus aren't applied to bosses.");
        EnableNewBadgeFix = _category.CreateEntry("FixBadgeNew", true, description: "Fixes the bug where all looted badges are being marked as new.");

        SelectUiPrevPage  = _category.CreateEntry("SelectUiPrevPage", KeyCode.PageUp);
        SelectUiNextPage  = _category.CreateEntry("SelectUiNextPage", KeyCode.PageDown);
        SelectUiFirstPage = _category.CreateEntry("SelectUiFirstPage", KeyCode.Home);
        SelectUiLastPage  = _category.CreateEntry("SelectUiLastPage", KeyCode.End);
    }
}