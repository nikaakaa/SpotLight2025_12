using UnityEngine;

/// <summary>
/// 结构倍率修正模块
/// 用于实现如 "环结构倍率+0.2" 的效果
/// 
/// 触发时机: OnCalculateStructureMultiplier
/// 参数: object[0] = structureType (E_StructureType), object[1] = MultiplierModifier (引用类型)
/// </summary>
[CreateAssetMenu(fileName = "StructureMultiplierModule", menuName = "Buff/Aviation/StructureMultiplierModule")]
public class StructureMultiplierModule : AviationBuffModule
{
    [Header("结构倍率配置")]

    /// <summary>
    /// 倍率加算值（+0.2 表示倍率增加0.2）
    /// </summary>
    [Tooltip("倍率加算值")]
    public float multiplierAddValue = 0.2f;

    /// <summary>
    /// 倍率乘算值（用于 "×N" 效果）
    /// </summary>
    [Tooltip("倍率乘算值，1.0 = 不变")]
    public float multiplierMultiply = 1.0f;

    public StructureMultiplierModule()
    {
        buffCallBackType = E_BuffCallBackType.OnCalculateStructureMultiplier;
    }

    public override void Apply(BuffInfo buffInfo, params object[] customInfo)
    {
        if (customInfo == null || customInfo.Length < 2) return;

        // 参数解析
        var structureType = (E_StructureType)customInfo[0];
        var modifier = customInfo[1] as MultiplierModifier;

        if (modifier == null) return;

        // 检查结构类型是否匹配
        if (!IsStructureTypeMatch(structureType)) return;

        // 应用加算
        int stackCount = buffInfo.CurStack > 0 ? buffInfo.CurStack : 1;
        modifier.FlatBonus += multiplierAddValue * stackCount;

        // 应用乘算
        modifier.Multiplier *= multiplierMultiply;

        Debug.Log($"[StructureMultiplierModule] 结构{structureType} 倍率+{multiplierAddValue * stackCount}, ×{multiplierMultiply}");
    }
}

