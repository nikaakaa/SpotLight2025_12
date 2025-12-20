using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 结算计算器 - 实现完整的回合结算流程
/// 
/// 结算公式：
/// - 基础收益 = 节点等级收益
/// - 节点收益 = 基础收益 × 结构倍率
/// - 总收益 = Σ(节点收益)
/// - 节点成本 = 节点等级 × 基础成本1
/// - 线路成本 = 线路长度 × 基础成本2
/// - 总成本 = Σ(节点成本) + Σ(线路成本)
/// - 净利润 = 总收益 - 总成本
/// </summary>
public class SettlementCalculator
{
    // 临时修正器（用于 Buff 回调）
    private readonly IncomeModifier nodeIncomeModifier = new IncomeModifier();
    private readonly MultiplierModifier structureMultiplierModifier = new MultiplierModifier();
    private readonly IncomeModifier nodeCostModifier = new IncomeModifier();
    private readonly IncomeModifier edgeCostModifier = new IncomeModifier();

    /// <summary>
    /// 执行完整结算流程
    /// </summary>
    /// <param name="aviationSystem">航空系统</param>
    /// <param name="structures">已检测的结构</param>
    /// <param name="playerInfo">玩家运行时数据</param>
    /// <param name="buffHandler">Buff 处理器（可选）</param>
    /// <returns>结算结果</returns>
    public SettlementResult Calculate(
        AviationSystem aviationSystem,
        AllStructures structures,
        PlayerRunTimeInfo playerInfo,
        BuffHandler buffHandler = null)
    {
        var result = new SettlementResult();
        result.Initialize();

        if (aviationSystem == null)
        {
            Debug.LogWarning("[SettlementCalculator] AviationSystem 为空");
            return result;
        }

        Debug.Log("[SettlementCalculator] 开始结算...");

        // Step 1: 计算节点基础收益
        CalculateNodeBaseIncomes(aviationSystem, result, buffHandler);

        // Step 2: 计算结构倍率并应用到节点
        ApplyStructureMultipliers(structures, result, buffHandler);

        // Step 3: 计算节点成本
        CalculateNodeCosts(aviationSystem, result, playerInfo, buffHandler);

        // Step 4: 计算航线成本
        CalculateEdgeCosts(aviationSystem, result, buffHandler);

        // Step 5: 汇总
        SummarizeResult(result);

        // Step 6: 触发结算完成回调
        buffHandler?.TriggerCustom(E_BuffCallBackType.OnSettlementComplete, result);

        Debug.Log($"[SettlementCalculator] 结算完成: 总收益={result.totalIncome}, 总成本={result.TotalCost}, 净利润={result.NetProfit}");

        return result;
    }

    #region Step 1: 节点基础收益

    /// <summary>
    /// 计算所有节点的基础收益
    /// </summary>
    private void CalculateNodeBaseIncomes(AviationSystem system, SettlementResult result, BuffHandler buffHandler)
    {
        foreach (var kvp in system.aviationNodeDict)
        {
            var node = kvp.Value;
            int nodeIndex = node.nodeIndex;

            // 基础收益 = 节点配置的收益值
            float baseIncome = node.nodeData?.NodeIncome ?? 0;

            // 获取节点等级（从配置ID推断：1001=lv1, 1002=lv2...）
            int nodeLevel = GetNodeLevel(node);

            // 重置修正器
            nodeIncomeModifier.Reset();

            // 触发 Buff 回调
            buffHandler?.TriggerCustom(E_BuffCallBackType.OnCalculateNodeBaseIncome, nodeLevel, nodeIncomeModifier);

            // 应用修正
            float finalBaseIncome = nodeIncomeModifier.Apply(baseIncome);

            result.nodeBaseIncomes[nodeIndex] = finalBaseIncome;
            result.nodeFinalIncomes[nodeIndex] = finalBaseIncome; // 暂存，后续被结构倍率修正
        }

        Debug.Log($"[SettlementCalculator] 计算节点基础收益完成: {result.nodeBaseIncomes.Count} 个节点");
    }

    #endregion

    #region Step 2: 结构倍率

    /// <summary>
    /// 计算结构倍率并应用到节点收益
    /// </summary>
    private void ApplyStructureMultipliers(AllStructures structures, SettlementResult result, BuffHandler buffHandler)
    {
        if (structures == null) return;

        int structureIndex = 0;

        // 处理每种结构类型
        foreach (var structure in structures.GetAllStructures())
        {
            // 重置修正器
            structureMultiplierModifier.Reset();

            // 获取基础倍率
            float baseMultiplier = structure.BaseMultiplier;

            // 触发 Buff 回调
            buffHandler?.TriggerCustom(E_BuffCallBackType.OnCalculateStructureMultiplier, structure.Type, structureMultiplierModifier);

            // 应用修正
            float finalMultiplier = structureMultiplierModifier.Apply(baseMultiplier);

            // 环形结构枢纽惩罚
            if (structure is RingStructure ring && !ring.MeetsHubRequirement)
            {
                finalMultiplier *= ring.HubPenaltyMultiplier;
            }

            structure.FinalMultiplier = finalMultiplier;

            // 记录结构倍率
            result.structureMultipliers[structureIndex] = finalMultiplier;

            // 应用倍率到结构中的节点
            foreach (var node in structure.Nodes)
            {
                if (result.nodeFinalIncomes.TryGetValue(node.nodeIndex, out float currentIncome))
                {
                    // 累乘（一个节点可能属于多个结构）
                    result.nodeFinalIncomes[node.nodeIndex] = currentIncome * finalMultiplier;
                }
            }

            // 计算结构总收益
            float structureTotalIncome = 0f;
            foreach (var node in structure.Nodes)
            {
                if (result.nodeFinalIncomes.TryGetValue(node.nodeIndex, out float income))
                {
                    structureTotalIncome += income;
                }
            }
            structure.TotalIncome = structureTotalIncome;

            structureIndex++;
        }

        Debug.Log($"[SettlementCalculator] 应用结构倍率完成: {structureIndex} 个结构");
    }

    #endregion

    #region Step 3: 节点成本

    /// <summary>
    /// 计算所有节点的成本
    /// </summary>
    private void CalculateNodeCosts(AviationSystem system, SettlementResult result, PlayerRunTimeInfo playerInfo, BuffHandler buffHandler)
    {
        foreach (var kvp in system.aviationNodeDict)
        {
            var node = kvp.Value;
            int nodeIndex = node.nodeIndex;

            // 基础成本 = 节点等级 × 基础成本系数
            int nodeLevel = GetNodeLevel(node);
            float baseCost = nodeLevel * PlayerData.NODE_UPGRADE_BASE_COST;

            // 重置修正器
            nodeCostModifier.Reset();

            // 触发 Buff 回调
            buffHandler?.TriggerCustom(E_BuffCallBackType.OnCalculateNodeCost, nodeLevel, nodeCostModifier);

            // 应用修正
            float finalCost = nodeCostModifier.Apply(baseCost);

            result.nodeCosts[nodeIndex] = finalCost;
        }

        // 检查反盲目扩张惩罚（Buff #15）
        if (playerInfo != null && playerInfo.NodesAddedThisRound >= PlayerData.EXPANSION_PENALTY_THRESHOLD)
        {
            // 所有节点成本增加
            var keys = new List<int>(result.nodeCosts.Keys);
            foreach (var key in keys)
            {
                result.nodeCosts[key] *= PlayerData.EXPANSION_PENALTY_MULTIPLIER;
            }
            Debug.Log($"[SettlementCalculator] 触发扩张惩罚: 新增节点={playerInfo.NodesAddedThisRound}, 成本×{PlayerData.EXPANSION_PENALTY_MULTIPLIER}");
        }

        Debug.Log($"[SettlementCalculator] 计算节点成本完成: {result.nodeCosts.Count} 个节点");
    }

    #endregion

    #region Step 4: 航线成本

    /// <summary>
    /// 计算所有航线的成本
    /// </summary>
    private void CalculateEdgeCosts(AviationSystem system, SettlementResult result, BuffHandler buffHandler)
    {
        foreach (var kvp in system.aviationEdgeDict)
        {
            var edge = kvp.Value;
            int edgeIndex = edge.edgeIndex;

            // 基础成本 = 路径长度 × 每格成本
            int pathLength = edge.pathCoords?.Count ?? 0;
            float baseCost = pathLength * PlayerData.EDGE_BUILD_COST_PER_TILE;

            // 重置修正器
            edgeCostModifier.Reset();

            // 触发 Buff 回调
            buffHandler?.TriggerCustom(E_BuffCallBackType.OnCalculateEdgeCost, pathLength, edgeCostModifier);

            // 应用修正
            float finalCost = edgeCostModifier.Apply(baseCost);

            result.edgeCosts[edgeIndex] = finalCost;
        }

        Debug.Log($"[SettlementCalculator] 计算航线成本完成: {result.edgeCosts.Count} 条航线");
    }

    #endregion

    #region Step 5: 汇总

    /// <summary>
    /// 汇总结算结果
    /// </summary>
    private void SummarizeResult(SettlementResult result)
    {
        // 计算总收益
        float totalIncome = 0f;
        foreach (var kvp in result.nodeFinalIncomes)
        {
            totalIncome += kvp.Value;
        }
        result.totalIncome = (long)totalIncome;

        // 计算节点总成本
        float totalNodeCost = 0f;
        foreach (var kvp in result.nodeCosts)
        {
            totalNodeCost += kvp.Value;
        }
        result.totalNodeCost = (long)totalNodeCost;

        // 计算航线总成本
        float totalEdgeCost = 0f;
        foreach (var kvp in result.edgeCosts)
        {
            totalEdgeCost += kvp.Value;
        }
        result.totalEdgeCost = (long)totalEdgeCost;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取节点等级（从配置ID推断）
    /// </summary>
    private int GetNodeLevel(AviationNode node)
    {
        if (node?.nodeData == null) return 1;

        // 配置ID: 1001=lv1, 1002=lv2, 1003=lv3, 1004=lv4, 1005=lv5
        int configId = node.nodeData.Id;
        return configId - 1000;
    }

    #endregion
}
