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
    /// <param name="buffSystem">Buff 系统接口（必须传入实现了此接口的对象，如 playerInfo）</param>
    /// <returns>结算结果</returns>
    public SettlementResult Calculate(
        AviationSystem aviationSystem,
        AllStructures structures,
        PlayerRunTimeInfo playerInfo,
        IGameplayBuffSystem buffSystem)
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
        CalculateNodeBaseIncomes(aviationSystem, result, buffSystem);

        // Step 2: 计算结构倍率并应用到节点
        ApplyStructureMultipliers(structures, result, buffSystem);

        // Step 3: 计算节点成本
        CalculateNodeCosts(aviationSystem, result, playerInfo, buffSystem);

        // Step 4: 计算航线成本
        CalculateEdgeCosts(aviationSystem, result, buffSystem);

        // Step 5: 汇总（预先计算资产变化供动画使用）
        SummarizeResult(result, playerInfo);

        // Step 6: 触发结算完成回调
        buffSystem?.TriggerSettlementCompleteBuffs(result);

        Debug.Log($"[SettlementCalculator] 结算完成: 总收益={result.totalIncome}, 总成本={result.TotalCost}, 净利润={result.NetProfit}");

        return result;
    }

    #region Step 1: 节点基础收益

    /// <summary>
    /// 计算所有节点的基础收益
    /// </summary>
    private void CalculateNodeBaseIncomes(AviationSystem system, SettlementResult result, IGameplayBuffSystem buffSystem)
    {
        Debug.Log($"[SettlementCalculator] ========== 开始计算节点收益 ==========");

        foreach (var kvp in system.aviationNodeDict)
        {
            var node = kvp.Value;
            int nodeIndex = node.nodeIndex;

            // 原始收益 = 节点配置的收益值（未应用任何 Buff）
            float rawIncome = node.nodeData?.NodeIncome ?? 0;

            // 记录原始收益（供动画初始显示）
            result.nodeRawIncomes[nodeIndex] = rawIncome;

            // 获取节点等级（从配置ID推断：1001=lv1, 1002=lv2...）
            int nodeLevel = GetNodeLevel(node);

            // 重置修正器
            nodeIncomeModifier.Reset();

            // 触发 Buff 回调
            buffSystem?.TriggerNodeIncomeBuffs(nodeLevel, nodeIncomeModifier);

            // 记录 Buff 效果（供动画使用）
            result.nodeBuffFlatBonus[nodeIndex] = nodeIncomeModifier.FlatBonus;
            result.nodeBuffMultiplier[nodeIndex] = nodeIncomeModifier.Multiplier;

            // 记录生效的 Buff ID
            if (nodeIncomeModifier.AppliedBuffIds.Count > 0)
            {
                result.nodeAppliedBuffs[nodeIndex] = new List<int>(nodeIncomeModifier.AppliedBuffIds);
            }

            // 调试：如果有 Buff 效果，打印出来
            if (nodeIncomeModifier.FlatBonus != 0 || nodeIncomeModifier.Multiplier != 1f)
            {
                Debug.Log($"[SettlementCalculator] 节点{nodeIndex} (lv{nodeLevel}): Buff效果 FlatBonus={nodeIncomeModifier.FlatBonus}, Multiplier={nodeIncomeModifier.Multiplier}");
            }

            // 应用修正得到基础收益（Buff 后、结构倍率前）
            float incomeAfterBuff = nodeIncomeModifier.Apply(rawIncome);

            result.nodeBaseIncomes[nodeIndex] = incomeAfterBuff;
            result.nodeFinalIncomes[nodeIndex] = incomeAfterBuff; // 暂存，后续被结构倍率修正
        }

        Debug.Log($"[SettlementCalculator] 计算节点基础收益完成: {result.nodeBaseIncomes.Count} 个节点");
    }

    #endregion

    #region Step 2: 结构倍率

    /// <summary>
    /// 计算结构倍率并应用到节点收益
    /// </summary>
    /// <summary>
    /// 计算结构倍率并应用到节点收益 (改为加算逻辑)
    /// </summary>
    private void ApplyStructureMultipliers(AllStructures structures, SettlementResult result, IGameplayBuffSystem buffSystem)
    {
        if (structures == null) return;

        // 临时存储每个节点的累加倍率加成 (Base 1.0 + Bonus)
        // Key: NodeIndex, Value: Sum of (StructureMultiplier - 1.0)
        var nodeBonusMultipliers = new Dictionary<int, float>();

        int structureIndex = 0;

        // Pass 1: 计算每个结构的倍率，并累加到节点
        foreach (var structure in structures.GetAllStructures())
        {
            // 重置修正器
            structureMultiplierModifier.Reset();

            // 获取基础倍率
            float baseMultiplier = structure.BaseMultiplier;

            // 触发 Buff 回调
            buffSystem?.TriggerStructureMultiplierBuffs(structure.Type, structureMultiplierModifier);

            // 记录生效的 Buff ID
            if (structureMultiplierModifier.AppliedBuffIds.Count > 0)
            {
                result.structureAppliedBuffs[structureIndex] = new List<int>(structureMultiplierModifier.AppliedBuffIds);
            }

            // 应用修正得到该结构的最终倍率
            float finalMultiplier = structureMultiplierModifier.Apply(baseMultiplier);

            // 环形结构枢纽惩罚
            if (structure is RingStructure ring && !ring.MeetsHubRequirement)
            {
                finalMultiplier *= ring.HubPenaltyMultiplier;
            }

            structure.FinalMultiplier = finalMultiplier;

            // 记录结构倍率
            result.structureMultipliers[structureIndex] = finalMultiplier;

            // 计算此结构带来的倍率加成 (例如 1.2 -> +0.2)
            float bonus = finalMultiplier - 1.0f;

            // 累加到该结构包含的所有节点
            foreach (var node in structure.Nodes)
            {
                if (!nodeBonusMultipliers.ContainsKey(node.nodeIndex))
                {
                    nodeBonusMultipliers[node.nodeIndex] = 0f;
                }
                nodeBonusMultipliers[node.nodeIndex] += bonus;
            }

            structureIndex++;
        }

        // Pass 2: 应用总倍率到节点最终收益
        // 最终倍率 = 1.0 + Σ(StructureMultiplier - 1.0)
        // 确保遍历所有已计算基础收益的节点
        var nodeIndices = new List<int>(result.nodeBaseIncomes.Keys);
        foreach (var nodeIndex in nodeIndices)
        {
            float baseIncome = result.nodeBaseIncomes[nodeIndex];
            float totalBonus = nodeBonusMultipliers.ContainsKey(nodeIndex) ? nodeBonusMultipliers[nodeIndex] : 0f;

            // 最终倍率不能小于 0
            float totalMultiplier = Mathf.Max(0f, 1.0f + totalBonus);

            result.nodeFinalIncomes[nodeIndex] = baseIncome * totalMultiplier;

            // Debug: 如果有倍率变化，打印日志
            if (Mathf.Abs(totalMultiplier - 1.0f) > 0.001f)
            {
                // Debug.Log($"[SettlementCalculator] Node {nodeIndex}: Base={baseIncome}, TotalMult={totalMultiplier} (1+{totalBonus})");
            }
        }

        // Pass 3: 重新计算结构总收益 (用于显示)
        // 这里的 TotalIncome 定义为：该结构包含的所有节点的最终收益之和
        foreach (var structure in structures.GetAllStructures())
        {
            float structureTotalIncome = 0f;
            foreach (var node in structure.Nodes)
            {
                if (result.nodeFinalIncomes.TryGetValue(node.nodeIndex, out float income))
                {
                    structureTotalIncome += income;
                }
            }
            structure.TotalIncome = structureTotalIncome;
        }

        Debug.Log($"[SettlementCalculator] 应用结构倍率完成 (加算模式): {structureIndex} 个结构");
    }

    #endregion

    #region Step 3: 节点成本

    /// <summary>
    /// 计算所有节点的成本
    /// </summary>
    private void CalculateNodeCosts(AviationSystem system, SettlementResult result, PlayerRunTimeInfo playerInfo, IGameplayBuffSystem buffSystem)
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
            buffSystem?.TriggerNodeCostBuffs(nodeLevel, nodeCostModifier);

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
    private void CalculateEdgeCosts(AviationSystem system, SettlementResult result, IGameplayBuffSystem buffSystem)
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
            buffSystem?.TriggerEdgeCostBuffs(pathLength, edgeCostModifier);

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
    private void SummarizeResult(SettlementResult result, PlayerRunTimeInfo playerInfo = null)
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

        // 预先计算结算前后资产（供动画使用）
        if (playerInfo != null)
        {
            result.assetsBefore = playerInfo.Assets;
            result.assetsAfter = playerInfo.Assets + result.NetProfit;
        }
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
