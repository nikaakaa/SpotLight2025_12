using UnityEngine;

/// <summary>
/// 航线成本倍乘模块
/// 用于实现如 "所有航线成本×1.5 - 燃油成本上涨" 的效果
/// 
/// 触发时机: OnCalculateEdgeCost
/// 参数: object[0] = edgeLength (int), object[1] = IncomeModifier (引用类型)
/// </summary>
[CreateAssetMenu(fileName = "EdgeCostMultiplyModule", menuName = "Buff/Aviation/EdgeCostMultiplyModule")]
public class EdgeCostMultiplyModule : AviationBuffModule
{
    [Header("航线成本配置")]

    /// <summary>
    /// 成本乘数（1.5 = 成本增加50%）
    /// </summary>
    [Tooltip("成本乘数，1.0 = 不变")]
    public float costMultiplier = 1.5f;

    /// <summary>
    /// 最小航线长度限制（0 = 不限制）
    /// </summary>
    [Tooltip("最小航线长度，0 = 不限制")]
    public int minEdgeLength = 0;

    /// <summary>
    /// 最大航线长度限制（0 = 不限制）
    /// </summary>
    [Tooltip("最大航线长度，0 = 不限制")]
    public int maxEdgeLength = 0;

    public EdgeCostMultiplyModule()
    {
        buffCallBackType = E_BuffCallBackType.OnCalculateEdgeCost;
    }

    public override void Apply(BuffInfo buffInfo, params object[] customInfo)
    {
        if (customInfo == null || customInfo.Length < 2) return;

        // 参数解析
        int edgeLength = (int)customInfo[0];
        var modifier = customInfo[1] as IncomeModifier;

        if (modifier == null) return;

        // 检查长度限制
        if (minEdgeLength > 0 && edgeLength < minEdgeLength) return;
        if (maxEdgeLength > 0 && edgeLength > maxEdgeLength) return;

        // 应用乘数（考虑层数）
        int stackCount = buffInfo.CurStack > 0 ? buffInfo.CurStack : 1;
        float totalMultiplier = Mathf.Pow(costMultiplier, stackCount);
        modifier.Multiplier *= totalMultiplier;

        Debug.Log($"[EdgeCostMultiplyModule] 航线长度{edgeLength} 成本×{totalMultiplier:F2}");
    }
}
