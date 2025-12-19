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
    /// 各节点的基础收益（应用 Buff 前）
    /// </summary>
    public Dictionary<int, float> nodeBaseIncomes;

    /// <summary>
    /// 各节点的最终收益（应用 Buff 后）
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
        nodeBaseIncomes ??= new Dictionary<int, float>();
        nodeFinalIncomes ??= new Dictionary<int, float>();
        structureMultipliers ??= new Dictionary<int, float>();
        nodeCosts ??= new Dictionary<int, float>();
        edgeCosts ??= new Dictionary<int, float>();
    }

    /// <summary>
    /// 清空数据
    /// </summary>
    public void Clear()
    {
        nodeBaseIncomes?.Clear();
        nodeFinalIncomes?.Clear();
        structureMultipliers?.Clear();
        nodeCosts?.Clear();
        edgeCosts?.Clear();
        totalIncome = 0;
        totalNodeCost = 0;
        totalEdgeCost = 0;
        assetsBefore = 0;
        assetsAfter = 0;
    }
}
