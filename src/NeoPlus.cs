using MelonLoader;
using NeoPlus.Patches;

[assembly: MelonGame("SQUARE ENIX", "NEO: The World Ends with You")]
[assembly: MelonInfo(typeof(NeoPlus.NeoPlus), "TWSWM", "1.0.0", "aenigmatic")]

namespace NeoPlus;

public class NeoPlus : MelonMod {
    public static MelonLogger.Instance Logger { get; private set; } = null!;

    public override void OnInitializeMelon() {
        LoggerInstance.Msg("Mod loaded");
        Logger = LoggerInstance;

        // initialize configuration
        Configuration.Init();

        // apply "manual" il2cpp patch
        PatchArrayInit();
    }

    private void PatchArrayInit() {
        try {
            NativeLootLimitPatch.InstallHook();
            LoggerInstance.Msg("Successfully applied loot limit hook");
        }
        catch (Exception e) {
            LoggerInstance.Error(e);
        }
    }
}