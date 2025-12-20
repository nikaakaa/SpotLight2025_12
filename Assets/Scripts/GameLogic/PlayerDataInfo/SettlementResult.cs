using System;
using System.Collections.Generic;

/// <summary>
/// 单回合结算结果（纯数据，用于结算 UI 展示和动画）
/// </summary>
[Serializable]
public class SettlementResult
{
    // ========== 收益明细 ==========

    /// <summary>
    /// 各节点的原始收益（未应用任何 Buff，用于动画初始显示）
    /// </summary>
    public Dictionary<int, float> nodeRawIncomes;

    /// <summary>
    /// 各节点的基础收益（应用 Buff 后，应用结构倍率前）
    /// </summary>
    public Dictionary<int, float> nodeBaseIncomes;

    /// <summary>
    /// 各节点的最终收益（应用 Buff 和结构倍率后）
    /// </summary>
    public Dictionary<int, float> nodeFinalIncomes;

    /// <summary>
    /// 各结构的倍率
    /// </summary>
    public Dictionary<int, float> structureMultipliers;

    /// <summary>
    /// 总收益
    /// </summary>
    public long totalIncome;

    // ========== Buff 效果明细（供动画使用）==========

    /// <summary>
    /// 各节点的 Buff 加数（FlatBonus）
    /// </summary>
    public Dictionary<int, long> nodeBuffFlatBonus;

    /// <summary>
    /// 各节点的 Buff 乘数（Multiplier，1.0 表示无变化）
    /// </summary>
    public Dictionary<int, float> nodeBuffMultiplier;

    /// <summary>
    /// 各节点实际生效的 Buff ID 列表（用于动画表现）
    /// </summary>
    public Dictionary<int, List<int>> nodeAppliedBuffs;

    /// <summary>
    /// 各结构实际生效的 Buff ID 列表（用于动画表现）
    /// </summary>
    public Dictionary<int, List<int>> structureAppliedBuffs;

    // ========== 成本明细 ==========

    /// <summary>
    /// 各节点的成本
    /// </summary>
    public Dictionary<int, float> nodeCosts;

    /// <summary>
    /// 各航线的成本
    /// </summary>
    public Dictionary<int, float> edgeCosts;

    /// <summary>
    /// 节点总成本
    /// </summary>
    public long totalNodeCost;

    /// <summary>
    /// 航线总成本
    /// </summary>
    public long totalEdgeCost;

    /// <summary>
    /// 总成本
    /// </summary>
    public long TotalCost => totalNodeCost + totalEdgeCost;

    // ========== 净收益 ==========

    /// <summary>
    /// 净收益（总收益 - 总成本）
    /// </summary>
    public long NetProfit => totalIncome - TotalCost;

    // ========== 结算前后资产 ==========

    /// <summary>
    /// 结算前资产
    /// </summary>
    public long assetsBefore;

    /// <summary>
    /// 结算后资产
    /// </summary>
    public long assetsAfter;

    /// <summary>
    /// 初始化字典
    /// </summary>
    public void Initialize()
    {
        nodeRawIncomes ??= new Dictionary<int, float>();
        nodeBaseIncomes ??= new Dictionary<int, float>();
        nodeFinalIncomes ??= new Dictionary<int, float>();
        structureMultipliers ??= new Dictionary<int, float>();
        nodeCosts ??= new Dictionary<int, float>();
        edgeCosts ??= new Dictionary<int, float>();
        nodeBuffFlatBonus ??= new Dictionary<int, long>();
        nodeBuffMultiplier ??= new Dictionary<int, float>();
        nodeAppliedBuffs ??= new Dictionary<int, List<int>>();
        structureAppliedBuffs ??= new Dictionary<int, List<int>>();
    }

    /// <summary>
    /// 清空数据
    /// </summary>
    public void Clear()
    {
        nodeRawIncomes?.Clear();
        nodeBaseIncomes?.Clear();
        nodeFinalIncomes?.Clear();
        structureMultipliers?.Clear();
        nodeCosts?.Clear();
        edgeCosts?.Clear();
        nodeBuffFlatBonus?.Clear();
        nodeBuffMultiplier?.Clear();
        nodeAppliedBuffs?.Clear();
        structureAppliedBuffs?.Clear();
        totalIncome = 0;
        totalNodeCost = 0;
        totalEdgeCost = 0;
        assetsBefore = 0;
        assetsAfter = 0;
    }
}
