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
    private static int _patchCallCount = 0;
    
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
            GD.Print("[AscensionUnlock] ========================================");
            GD.Print("[AscensionUnlock] Starting patch application...");
            GD.Print("[AscensionUnlock] Searching for ascension-related types...");
            
            var patchedCount = 0;
            
            // 扩大搜索范围，包括多人游戏相关的类型
            var targetTypes = sts2Assembly.GetTypes()
                .Where(t => t.FullName != null && 
                       (t.FullName.Contains("Ascension") || 
                        t.FullName.Contains("CharacterProgress") ||
                        t.FullName.Contains("PlayerProgress") ||
                        t.FullName.Contains("Multiplayer") ||
                        t.FullName.Contains("NetGame") ||
                        t.FullName.Contains("Lobby") ||
                        t.FullName.Contains("RunConfig")))
                .ToList();
            
            GD.Print($"[AscensionUnlock] Found {targetTypes.Count} potential types");
            
            foreach (var type in targetTypes)
            {
                // 查找返回int的属性（可能是最高解锁等级）
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(int) && prop.CanRead)
                    {
                        var propName = prop.Name.ToLower();
                        if (propName.Contains("highest") || propName.Contains("max") || 
                            (propName.Contains("ascension") && (propName.Contains("level") || propName.Contains("unlock"))))
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
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (method.ReturnType == typeof(bool) && !method.IsAbstract)
                    {
                        var methodName = method.Name.ToLower();
                        var parameters = method.GetParameters();
                        
                        // 补丁接受int参数的IsUnlocked类方法
                        if ((methodName.Contains("unlock") || methodName.Contains("available") || 
                             methodName.Contains("can") || methodName.Contains("valid")) && 
                            parameters.Length >= 1 && parameters[0].ParameterType == typeof(int))
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
                
                // 查找返回int的方法（可能返回最高等级）
                foreach (var method in methods)
                {
                    if (method.ReturnType == typeof(int) && !method.IsAbstract && method.GetParameters().Length == 0)
                    {
                        var methodName = method.Name.ToLower();
                        if (methodName.Contains("highest") || methodName.Contains("max") || 
                            (methodName.Contains("ascension") && methodName.Contains("level")))
                        {
                            try
                            {
                                var postfix = typeof(UnlockPatches).GetMethod(nameof(MaxLevelPostfix), 
                                    BindingFlags.Static | BindingFlags.NonPublic);
                                harmony.Patch(method, postfix: new HarmonyMethod(postfix));
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
            
            GD.Print($"[AscensionUnlock] ========================================");
            GD.Print($"[AscensionUnlock] Patching complete! Total patches: {patchedCount}");
            GD.Print($"[AscensionUnlock] ========================================");
            
            if (patchedCount == 0)
            {
                GD.PrintErr("[AscensionUnlock] WARNING: No methods were patched!");
                GD.PrintErr("[AscensionUnlock] The game structure may have changed.");
                GD.PrintErr("[AscensionUnlock] Please check the game logs for more information.");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[AscensionUnlock] CRITICAL ERROR: {ex.Message}");
            GD.PrintErr($"[AscensionUnlock] Stack: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 前置补丁：使解锁检查总是返回true
    /// Prefix patch: Make unlock checks always return true
    /// </summary>
    private static bool IsUnlockedPrefix(ref bool __result, int level)
    {
        _patchCallCount++;
        
        // 解锁1-10级（杀戮尖塔2最高进阶10）
        if (level >= 1 && level <= 10)
        {
            __result = true;
            GD.Print($"[AscensionUnlock] #{_patchCallCount} Unlocking level {level}");
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
            _patchCallCount++;
            GD.Print($"[AscensionUnlock] #{_patchCallCount} Increasing max level from {__result} to 10");
            __result = 10;
        }
    }
    
    /// <summary>
    /// 通用前置补丁：拦截所有返回bool的方法，如果参数是int且在1-10范围内，返回true
    /// Generic prefix patch: Intercept all bool-returning methods with int parameter
    /// </summary>
    private static bool GenericBoolIntPrefix(ref bool __result, int __0)
    {
        // 如果第一个参数是1-10之间的整数，假设这是进阶等级检查
        if (__0 >= 1 && __0 <= 10)
        {
            __result = true;
            return false; // 跳过原方法
        }
        return true;
    }
    
    /// <summary>
    /// 通用后置补丁：确保所有返回int的方法至少返回10
    /// Generic postfix patch: Ensure all int-returning methods return at least 10
    /// </summary>
    private static void GenericIntPostfix(ref int __result)
    {
        // 如果返回值是0-9之间，可能是最高解锁等级，提升到10
        if (__result >= 0 && __result < 10)
        {
            __result = 10;
        }
    }
}

