using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;

namespace AscensionUnlock;

/// <summary>
/// Harmony 补丁类 - 拦截进阶难度解锁检查
/// Harmony patches to bypass ascension level unlock checks
/// </summary>
public static class UnlockPatches
{
    private static bool _initialized = false;
    
    /// <summary>
    /// 应用补丁 - 使用更精确的目标定位
    /// Apply patches - using more precise targeting
    /// </summary>
    public static void ApplyPatches(Harmony harmony)
    {
        if (_initialized) return;
        _initialized = true;
        
        try
        {
            var sts2Assembly = typeof(MegaCrit.Sts2.Core.Modding.ModInitializerAttribute).Assembly;
            GD.Print("[AscensionUnlock] Searching for ascension-related types...");
            
            var patchedCount = 0;
            
            // 只查找明确与进阶难度相关的类型
            var targetTypes = sts2Assembly.GetTypes()
                .Where(t => t.FullName != null && 
                       (t.FullName.Contains("Ascension") || 
                        t.FullName.Contains("CharacterProgress") ||
                        t.FullName.Contains("PlayerProgress")))
                .ToList();
            
            GD.Print($"[AscensionUnlock] Found {targetTypes.Count} potential types");
            
            foreach (var type in targetTypes)
            {
                GD.Print($"[AscensionUnlock] Examining type: {type.FullName}");
                
                // 查找返回int的属性（可能是最高解锁等级）
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(int) && prop.CanRead)
                    {
                        var propName = prop.Name.ToLower();
                        if (propName.Contains("highest") || propName.Contains("max") || 
                            (propName.Contains("ascension") && propName.Contains("level")))
                        {
                            var getter = prop.GetGetMethod();
                            if (getter != null && !getter.IsAbstract)
                            {
                                try
                                {
                                    var postfix = typeof(UnlockPatches).GetMethod(nameof(MaxLevelPostfix), 
                                        BindingFlags.Static | BindingFlags.NonPublic);
                                    harmony.Patch(getter, postfix: new HarmonyMethod(postfix));
                                    GD.Print($"[AscensionUnlock] ✓ Patched property: {type.Name}.{prop.Name}");
                                    patchedCount++;
                                }
                                catch (Exception ex)
                                {
                                    GD.Print($"[AscensionUnlock] ✗ Failed to patch {type.Name}.{prop.Name}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                
                // 查找返回bool的方法（可能是解锁检查）
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    if (method.ReturnType == typeof(bool) && !method.IsAbstract)
                    {
                        var methodName = method.Name.ToLower();
                        var parameters = method.GetParameters();
                        
                        // 只补丁接受int参数的IsUnlocked类方法
                        if (methodName.Contains("unlock") && parameters.Length == 1 && 
                            parameters[0].ParameterType == typeof(int))
                        {
                            try
                            {
                                var prefix = typeof(UnlockPatches).GetMethod(nameof(IsUnlockedPrefix), 
                                    BindingFlags.Static | BindingFlags.NonPublic);
                                harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                                GD.Print($"[AscensionUnlock] ✓ Patched method: {type.Name}.{method.Name}");
                                patchedCount++;
                            }
                            catch (Exception ex)
                            {
                                GD.Print($"[AscensionUnlock] ✗ Failed to patch {type.Name}.{method.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            
            GD.Print($"[AscensionUnlock] Patching complete! Total patches: {patchedCount}");
            
            if (patchedCount == 0)
            {
                GD.PrintErr("[AscensionUnlock] WARNING: No methods were patched!");
                GD.PrintErr("[AscensionUnlock] The game structure may have changed.");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[AscensionUnlock] Error: {ex.Message}");
            GD.PrintErr($"[AscensionUnlock] Stack: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 前置补丁：使解锁检查总是返回true
    /// Prefix patch: Make unlock checks always return true
    /// </summary>
    private static bool IsUnlockedPrefix(ref bool __result, int level)
    {
        // 只解锁1-10级
        if (level >= 1 && level <= 10)
        {
            __result = true;
            return false; // 跳过原方法
        }
        return true; // 执行原方法
    }
    
    /// <summary>
    /// 后置补丁：确保最高等级至少为10
    /// Postfix patch: Ensure max level is at least 10
    /// </summary>
    private static void MaxLevelPostfix(ref int __result)
    {
        if (__result < 10)
        {
            __result = 10;
        }
    }
}
