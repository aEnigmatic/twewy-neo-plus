using System.Runtime.InteropServices;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MinHook;

namespace NeoPlus.Patches;

public static class NativeLootLimitPatch {
    private const int LootLimit     = 4096;
    private const int OriginalLimit = 256;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate IntPtr ArrayInitDelegate(IntPtr arrayClass, ulong length);

    private static readonly HookEngine MinHook = new HookEngine();

    private static IntPtr            _targetArrayClass;
    private static ArrayInitDelegate _original = null!;

    public static void InstallHook() {
        // find DropItemInfo[] type pointer
        var elemType = Il2CppClassPointerStore<BattleDropItemManager.DropItemInfo>.NativeClassPtr;
        var arrType  = IL2CPP.il2cpp_array_class_get(elemType, 1);
        if (arrType == IntPtr.Zero)
            throw new Exception("Failed to find array type pointer");

        _targetArrayClass = arrType;

        // find il2cpp_array_new_specific
        var targetMethod = GetIl2CppArrayNewSpecific();
        if (targetMethod == IntPtr.Zero)
            throw new Exception("Failed to find il2cpp_array_new_specific");

        nint moduleBase = NativeHelper.GetModuleHandle("GameAssembly");
        NeoPlus.Logger.Msg($"Found target method: 0x{targetMethod - moduleBase:x}");

        // hook it
        _original = MinHook.CreateHook<ArrayInitDelegate>(GetIl2CppArrayNewSpecific(), Hook);
        MinHook.EnableHook(_original);
    }

    public static void UninstallHook()
        => MinHook.DisableHooks();

    public static IntPtr GetIl2CppArrayNewSpecific() {
        var module = NativeHelper.GetModuleHandle("GameAssembly.dll");
        if (module == IntPtr.Zero)
            return IntPtr.Zero;

        // find il2cpp_array_new_specific and follow the jmp to the internal target
        var procPtr     = NativeHelper.GetProcAddress(module, "il2cpp_array_new_specific");
        var internalPtr = NativeHelper.FollowJmp(procPtr);

        return internalPtr;
    }

    private static IntPtr Hook(IntPtr arrayClass, ulong length) {
        if (arrayClass != _targetArrayClass || length != OriginalLimit)
            return _original(arrayClass, length);

        NeoPlus.Logger.Msg($"Patching max drops from {length} to {LootLimit}");
        return _original(_targetArrayClass, LootLimit);
    }
}

public static class NativeHelper {
 #pragma warning disable CA2101
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
#pragma warning restore CA2101

    public static unsafe IntPtr FollowJmp(IntPtr procPtr) {
        var instr = *(byte*) procPtr;
        if (instr != 0xE9)
            throw new Exception("Expected a direct relative jump");

        var offset = *(int*) (procPtr + 1);
        return procPtr + 5 + offset;
    }
}