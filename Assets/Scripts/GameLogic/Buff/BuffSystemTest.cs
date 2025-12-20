using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Buff 系统测试工具
/// 在编辑器和运行时验证 Buff 系统各环节是否正常工作
/// </summary>
public static class BuffSystemTest
{
    /// <summary>
    /// 运行所有测试
    /// </summary>
    public static void RunAllTests()
    {
        Debug.Log("========== Buff 系统测试开始 ==========");

        int passed = 0;
        int failed = 0;

        // 测试 1: BuffRegistry 初始化
        if (Test_BuffRegistry_Initialize())
        {
            passed++;
            Debug.Log("✓ [通过] BuffRegistry 初始化测试");
        }
        else
        {
            failed++;
            Debug.LogError("✗ [失败] BuffRegistry 初始化测试");
        }

        // 测试 2: BuffData 效果注册
        if (Test_BuffData_EffectsRegistered())
        {
            passed++;
            Debug.Log("✓ [通过] BuffData 效果注册测试");
        }
        else
        {
            failed++;
            Debug.LogError("✗ [失败] BuffData 效果注册测试");
        }

        // 测试 3: IncomeModifier 应用
        if (Test_IncomeModifier_Apply())
        {
            passed++;
            Debug.Log("✓ [通过] IncomeModifier 应用测试");
        }
        else
        {
            failed++;
            Debug.LogError("✗ [失败] IncomeModifier 应用测试");
        }

        // 测试 4: Buff 效果触发（需要运行时）
        if (Application.isPlaying)
        {
            if (Test_BuffEffect_Trigger())
            {
                passed++;
                Debug.Log("✓ [通过] Buff 效果触发测试");
            }
            else
            {
                failed++;
                Debug.LogError("✗ [失败] Buff 效果触发测试");
            }

            // 测试 5: 完整链路测试
            if (Test_FullChain_Settlement())
            {
                passed++;
                Debug.Log("✓ [通过] 完整结算链路测试");
            }
            else
            {
                failed++;
                Debug.LogError("✗ [失败] 完整结算链路测试");
            }
        }
        else
        {
            Debug.LogWarning("⚠ [跳过] Buff 效果触发测试（需要运行时）");
            Debug.LogWarning("⚠ [跳过] 完整结算链路测试（需要运行时）");
        }

        Debug.Log($"========== 测试完成: {passed} 通过, {failed} 失败 ==========");
    }

    /// <summary>
    /// 测试 1: BuffRegistry 初始化
    /// </summary>
    public static bool Test_BuffRegistry_Initialize()
    {
        BuffRegistry.Initialize();

        // 检查是否有注册的 Buff
        var allBuffs = BuffRegistry.GetAllBuffs();
        int count = 0;
        foreach (var _ in allBuffs) count++;

        if (count == 0)
        {
            Debug.LogError("  - BuffRegistry 中没有注册任何 Buff");
            return false;
        }

        Debug.Log($"  - BuffRegistry 已注册 {count} 个 Buff");

        // 检查玩家 Buff 是否存在
        var buff101 = BuffRegistry.GetById(101);
        if (buff101 == null)
        {
            Debug.LogError("  - 找不到 Buff ID=101 (lv1收益+3)");
            return false;
        }

        Debug.Log($"  - 找到 Buff ID=101: {buff101.buffName}");
        return true;
    }

    /// <summary>
    /// 测试 2: BuffData 效果注册
    /// </summary>
    public static bool Test_BuffData_EffectsRegistered()
    {
        BuffRegistry.Initialize();

        var buff101 = BuffRegistry.GetById(101);
        if (buff101 == null)
        {
            Debug.LogError("  - 找不到 Buff ID=101");
            return false;
        }

        // 检查效果是否注册
        if (buff101.Effects.Count == 0)
        {
            Debug.LogError("  - Buff ID=101 没有注册任何效果");
            return false;
        }

        Debug.Log($"  - Buff ID=101 已注册 {buff101.Effects.Count} 个效果回调");

        // 检查是否有 OnCalculateNodeBaseIncome 回调
        if (!buff101.Effects.ContainsKey(E_BuffCallBackType.OnCalculateNodeBaseIncome))
        {
            Debug.LogError("  - Buff ID=101 缺少 OnCalculateNodeBaseIncome 回调");
            return false;
        }

        Debug.Log("  - Buff ID=101 包含 OnCalculateNodeBaseIncome 回调");
        return true;
    }

    /// <summary>
    /// 测试 3: IncomeModifier 应用
    /// </summary>
    public static bool Test_IncomeModifier_Apply()
    {
        var modifier = new IncomeModifier();
        modifier.FlatBonus = 10;
        modifier.Multiplier = 1.5f;

        float result = modifier.Apply(100f);
        float expected = (100 + 10) * 1.5f; // = 165

        if (Mathf.Abs(result - expected) > 0.01f)
        {
            Debug.LogError($"  - IncomeModifier.Apply 结果错误: 期望={expected}, 实际={result}");
            return false;
        }

        Debug.Log($"  - IncomeModifier.Apply({100}) = {result} (期望: {expected})");

        // 测试重置
        modifier.Reset();
        if (modifier.FlatBonus != 0 || modifier.Multiplier != 1f)
        {
            Debug.LogError("  - IncomeModifier.Reset 失败");
            return false;
        }

        Debug.Log("  - IncomeModifier.Reset 正常");
        return true;
    }

    /// <summary>
    /// 测试 4: Buff 效果触发（需要运行时）
    /// </summary>
    /// <summary>
    /// 测试 4: Buff 效果触发（需要运行时）
    /// </summary>
    public static bool Test_BuffEffect_Trigger()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("  - 此测试需要在运行时执行");
            return false;
        }

        // 检查 PlayerRunTimeInfo.Current
        if (PlayerRunTimeInfo.Current == null)
        {
            Debug.LogError("  - PlayerRunTimeInfo.Current 为 NULL");
            return false;
        }

        var buffSystem = PlayerRunTimeInfo.Current.PlayerBuffSystem;
        if (buffSystem == null)
        {
            Debug.LogError("  - PlayerBuffSystem 为 NULL");
            return false;
        }

        // 清空现有 Buff
        buffSystem.ClearBuff();
        Debug.Log("  - 已清空 PlayerBuffSystem");

        // 添加测试 Buff
        bool added = buffSystem.AddBuffById(101); // lv1收益+3
        if (!added)
        {
            Debug.LogError("  - 添加 Buff ID=101 失败");
            return false;
        }

        Debug.Log("  - 已添加 Buff ID=101");

        // 手动更新一次 BuffSystem (处理添加队列)
        buffSystem.Update(0f);

        // 直接测试触发效果
        var modifier = new IncomeModifier();

        // 使用 PlayerRunTimeInfo 触发
        PlayerRunTimeInfo.Current.TriggerNodeIncomeBuffs(1, modifier); // lv1 节点

        Debug.Log($"  - 触发后 modifier: FlatBonus={modifier.FlatBonus}, Multiplier={modifier.Multiplier}");

        // lv1 收益+3 应该使 FlatBonus = 3
        if (modifier.FlatBonus != 3)
        {
            Debug.LogError($"  - Buff 效果未生效: 期望 FlatBonus=3, 实际={modifier.FlatBonus}");
            return false;
        }

        Debug.Log("  - Buff 效果正常生效");

        // 清空 Buff
        buffSystem.ClearBuff();

        return true;
    }

    /// <summary>
    /// 测试 5: 完整结算链路
    /// </summary>
    public static bool Test_FullChain_Settlement()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("  - 此测试需要在运行时执行");
            return false;
        }

        // 检查必要组件
        if (PlayerRunTimeInfo.Current == null)
        {
            Debug.LogError("  - PlayerRunTimeInfo.Current 为 NULL");
            return false;
        }

        Debug.Log("  - 检查 SettlementResult Buff 字段...");

        var result = new SettlementResult();
        result.Initialize();

        // 检查新增字段是否存在
        if (result.nodeBuffFlatBonus == null)
        {
            Debug.LogError("  - SettlementResult.nodeBuffFlatBonus 为 NULL");
            return false;
        }

        if (result.nodeBuffMultiplier == null)
        {
            Debug.LogError("  - SettlementResult.nodeBuffMultiplier 为 NULL");
            return false;
        }

        if (result.nodeRawIncomes == null)
        {
            Debug.LogError("  - SettlementResult.nodeRawIncomes 为 NULL");
            return false;
        }

        Debug.Log("  - SettlementResult Buff 字段正常");

        // 模拟记录 Buff 效果
        result.nodeBuffFlatBonus[0] = 10;
        result.nodeBuffMultiplier[0] = 1.2f;
        result.nodeRawIncomes[0] = 100f;

        Debug.Log($"  - 模拟节点0: rawIncome=100, FlatBonus=10, Multiplier=1.2");

        // 验证读取
        long flatBonus = result.nodeBuffFlatBonus.GetValueOrDefault(0, 0);
        float multiplier = result.nodeBuffMultiplier.GetValueOrDefault(0, 1f);

        if (flatBonus != 10 || Mathf.Abs(multiplier - 1.2f) > 0.01f)
        {
            Debug.LogError("  - SettlementResult 数据读写不一致");
            return false;
        }

        Debug.Log("  - SettlementResult 数据读写正常");

        return true;
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Buff System/运行所有测试")]
    private static void MenuItem_RunAllTests()
    {
        RunAllTests();
    }

    [MenuItem("Tools/Buff System/测试 1: BuffRegistry 初始化")]
    private static void MenuItem_Test1()
    {
        if (Test_BuffRegistry_Initialize())
            Debug.Log("✓ 测试通过");
        else
            Debug.LogError("✗ 测试失败");
    }

    [MenuItem("Tools/Buff System/测试 2: BuffData 效果注册")]
    private static void MenuItem_Test2()
    {
        if (Test_BuffData_EffectsRegistered())
            Debug.Log("✓ 测试通过");
        else
            Debug.LogError("✗ 测试失败");
    }

    [MenuItem("Tools/Buff System/测试 3: IncomeModifier 应用")]
    private static void MenuItem_Test3()
    {
        if (Test_IncomeModifier_Apply())
            Debug.Log("✓ 测试通过");
        else
            Debug.LogError("✗ 测试失败");
    }

    [MenuItem("Tools/Buff System/测试 4: Buff 效果触发 (需运行时)")]
    private static void MenuItem_Test4()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("此测试需要在运行时执行，请先运行游戏");
            return;
        }
        if (Test_BuffEffect_Trigger())
            Debug.Log("✓ 测试通过");
        else
            Debug.LogError("✗ 测试失败");
    }

    [MenuItem("Tools/Buff System/测试 5: 完整结算链路 (需运行时)")]
    private static void MenuItem_Test5()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("此测试需要在运行时执行，请先运行游戏");
            return;
        }
        if (Test_FullChain_Settlement())
            Debug.Log("✓ 测试通过");
        else
            Debug.LogError("✗ 测试失败");
    }
#endif
}
