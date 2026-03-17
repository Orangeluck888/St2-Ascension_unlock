using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace AscensionUnlock;

/// <summary>
/// 进阶难度解锁模组 - 允许玩家直接选择任何进阶难度
/// Ascension Unlock Mod - Allows players to select any ascension level directly
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class AscensionUnlockMod
{
    private static Harmony _harmony;
    
    public static void Initialize()
    {
        try
        {
            GD.Print("========================================");
            GD.Print("[AscensionUnlock] Starting mod initialization...");
            GD.Print("[AscensionUnlock] 开始初始化模组...");
            GD.Print("========================================");
            
            // 创建 Harmony 实例用于运行时补丁
            _harmony = new Harmony("com.orangexiaoxing.ascensionunlock");
            
            // 应用所有补丁
            UnlockPatches.ApplyPatches(_harmony);
            
            GD.Print("========================================");
            GD.Print("[AscensionUnlock] ✓ Mod initialized successfully!");
            GD.Print("[AscensionUnlock] ✓ 模组初始化成功！");
            GD.Print("[AscensionUnlock] All ascension levels should now be unlocked.");
            GD.Print("[AscensionUnlock] 所有进阶难度现在应该已解锁。");
            GD.Print("========================================");
        }
        catch (Exception ex)
        {
            GD.PrintErr("========================================");
            GD.PrintErr($"[AscensionUnlock] ✗ Error during initialization: {ex.Message}");
            GD.PrintErr($"[AscensionUnlock] Stack trace: {ex.StackTrace}");
            GD.PrintErr("========================================");
        }
    }
}
