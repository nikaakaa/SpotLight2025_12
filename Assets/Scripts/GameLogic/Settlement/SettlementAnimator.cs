using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 结算动画编排器 - 控制整个结算阶段的动画流程
/// 类似小丑牌风格：地图变暗 → 节点逐个亮起 → 显示收益 → Buff跳动 → 成本扣除 → 资产更新
/// </summary>
public class SettlementAnimator : MonoBehaviour
{
    public static SettlementAnimator Instance { get; private set; }

    [Header("预制体")]
    [SerializeField] private GameObject floatingNumberPrefab;

    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;

    private AviationSystem aviationSystem;
    private readonly List<FloatingNumber> activeFloatingNumbers = new();

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 播放完整结算动画
    /// </summary>
    public async UniTask PlayAsync(
        AllStructures structures,
        SettlementResult result,
        AviationSystem system,
        CancellationToken ct = default)
    {
        aviationSystem = system;

        Log("开始结算动画");

        // 进入结算模式
        GameLogicUI.Instance?.SetSettlementMode(true);

        try
        {
            // 1. 准备阶段：地图变暗
            await PhaseDim(ct);

            // 2. 结构收益阶段（循环每个结构）
            await PhaseStructuresIncome(structures, result, ct);

            // 3. 成本阶段
            await PhaseCost(result, ct);

            // 4. 恢复阶段
            await PhaseRestore(ct);
        }
        finally
        {
            // 退出结算模式
            GameLogicUI.Instance?.SetSettlementMode(false);
            ClearFloatingNumbers();
        }

        Log("结算动画完成");
    }

    #region 阶段：准备（变暗）

    private async UniTask PhaseDim(CancellationToken ct)
    {
        Log("Phase: 准备（变暗）");

        float duration = SettlementAnimConfig.DimDuration;

        // 所有节点变暗
        foreach (var cityNode in aviationSystem.GetAllCityNodes())
        {
            cityNode.SetDim(true, duration);
        }

        // 所有边线变暗
        foreach (var edgeView in aviationSystem.GetAllEdgeViews())
        {
            edgeView.SetDim(true, duration);
        }

        await UniTask.Delay((int)(duration * 1000), cancellationToken: ct);
    }

    #endregion

    #region 阶段：结构收益

    private async UniTask PhaseStructuresIncome(AllStructures structures, SettlementResult result, CancellationToken ct)
    {
        Log("Phase: 结构收益");

        int totalStructures = structures.TotalCount;
        int processedCount = 0;

        // 按顺序结算：环 → 放射 → 单线
        // 1. 先结算环形结构（价值最高）
        Log("  结算环形结构...");
        foreach (var ring in structures.Rings)
        {
            float speedMultiplier = CalculateSpeedMultiplier(processedCount, totalStructures);
            await AnimateStructure(ring, result, processedCount, speedMultiplier, ct);
            processedCount++;
        }

        // 2. 再结算放射结构
        Log("  结算放射结构...");
        foreach (var radial in structures.Radials)
        {
            float speedMultiplier = CalculateSpeedMultiplier(processedCount, totalStructures);
            await AnimateStructure(radial, result, processedCount, speedMultiplier, ct);
            processedCount++;
        }

        // 3. 最后结算单线结构（最常见）
        Log("  结算单线结构...");
        foreach (var singleLine in structures.SingleLines)
        {
            float speedMultiplier = CalculateSpeedMultiplier(processedCount, totalStructures);
            await AnimateStructure(singleLine, result, processedCount, speedMultiplier, ct);
            processedCount++;
        }
    }

    /// <summary>
    /// 计算速度倍率（越后面越快）
    /// </summary>
    private float CalculateSpeedMultiplier(int currentIndex, int total)
    {
        if (total <= 1) return 1f;

        // 从 1.0 加速到 3.0（后面的结构播放速度是前面的3倍）
        float progress = (float)currentIndex / (total - 1);
        float speedMultiplier = 1f + progress * 2f;  // 1.0 → 3.0

        return speedMultiplier;
    }

    private async UniTask AnimateStructure(StructureBase structure, SettlementResult result, int structureIndex, float speedMultiplier, CancellationToken ct)
    {
        Log($"  结构 #{structureIndex}: {structure.Type}, {structure.Nodes.Count} 个节点, 速度x{speedMultiplier:F1}");

        // 时间缩放（速度越快，延迟越短）
        float timeScale = 1f / speedMultiplier;

        // 1. 亮起结构中的边线
        foreach (var edge in structure.Edges)
        {
            if (edge.edgeLineView != null)
            {
                edge.edgeLineView.SetTint(SettlementAnimConfig.IncomeColor, 0.2f * timeScale);
            }
        }

        // 2. 逐个亮起节点并显示【基础收益】
        var nodeFloatingNumbers = new Dictionary<int, FloatingNumber>();
        foreach (var node in structure.Nodes)
        {
            var cityNode = node.cityNodeView;
            if (cityNode == null) continue;

            // 高亮节点
            cityNode.Highlight(0.1f * timeScale);

            // 获取【基础收益】（未乘倍率）
            float baseIncome = result.nodeBaseIncomes.GetValueOrDefault(node.nodeIndex, 0);

            // 显示基础收益数字
            var floatingNumber = await CreateFloatingNumber(
                cityNode.GetFloatingNumberPosition(),
                baseIncome,
                SettlementAnimConfig.IncomeColor,
                SettlementAnimConfig.NodePopDelay * timeScale);

            if (floatingNumber != null)
            {
                nodeFloatingNumbers[node.nodeIndex] = floatingNumber;
            }

            // 节点间隔（加速）
            int nodeDelay = (int)(SettlementAnimConfig.NodePopDelay * 1000 * timeScale);
            if (nodeDelay > 0)
            {
                await UniTask.Delay(nodeDelay, cancellationToken: ct);
            }
        }

        // 3. 倍率飞向节点并更新数字
        if (result.structureMultipliers.TryGetValue(structureIndex, out float multiplier) && multiplier != 1f)
        {
            Log($"    倍率: ×{multiplier:F1}");

            // 闪烁边线
            foreach (var edge in structure.Edges)
            {
                edge.edgeLineView?.Flash(SettlementAnimConfig.MultiplierColor, 0.2f * timeScale);
            }

            // 在结构中心创建倍率数字
            if (structure.Edges.Count > 0)
            {
                var centerEdge = structure.Edges[structure.Edges.Count / 2];
                Vector3 multiplierStartPos = GetEdgeMidpoint(centerEdge);

                // 对每个节点：倍率飞向节点 → 碰撞后更新数字
                var impactTasks = new List<UniTask>();
                foreach (var node in structure.Nodes)
                {
                    if (!nodeFloatingNumbers.TryGetValue(node.nodeIndex, out var nodeNumber)) continue;
                    if (node.cityNodeView == null) continue;

                    Vector3 nodeTargetPos = node.cityNodeView.GetFloatingNumberPosition();
                    float baseIncome = result.nodeBaseIncomes.GetValueOrDefault(node.nodeIndex, 0);
                    float finalIncome = result.nodeFinalIncomes.GetValueOrDefault(node.nodeIndex, baseIncome);

                    // 启动撞击动画任务
                    impactTasks.Add(AnimateMultiplierImpact(
                        multiplierStartPos,
                        nodeTargetPos,
                        nodeNumber,
                        multiplier,
                        finalIncome,
                        timeScale,
                        ct));
                }

                await UniTask.WhenAll(impactTasks);
            }

            int multiplierDelay = (int)(SettlementAnimConfig.MultiplierShowDuration * 500 * timeScale);
            await UniTask.Delay(multiplierDelay, cancellationToken: ct);
        }

        // 4. 数字飞向总资产
        Vector3 moneyTargetPos = GameLogicUI.Instance?.GetMoneyWorldPosition() ?? Vector3.zero;
        var flyTasks = new List<UniTask>();
        foreach (var fn in activeFloatingNumbers)
        {
            if (fn != null)
            {
                flyTasks.Add(fn.FlyTo(moneyTargetPos, SettlementAnimConfig.NumberFlyDuration * timeScale));
            }
        }
        await UniTask.WhenAll(flyTasks);
        activeFloatingNumbers.Clear();

        // 5. 熄灭结构
        float dimDuration = 0.2f * timeScale;
        foreach (var node in structure.Nodes)
        {
            node.cityNodeView?.SetDim(true, dimDuration);
        }
        foreach (var edge in structure.Edges)
        {
            edge.edgeLineView?.SetDim(true, dimDuration);
        }

        // 结构间间隔（加速）
        int structureDelay = (int)(SettlementAnimConfig.StructureInterval * 1000 * timeScale);
        if (structureDelay > 50)  // 最小间隔 50ms
        {
            await UniTask.Delay(structureDelay, cancellationToken: ct);
        }
    }

    #endregion

    #region 阶段：成本

    private async UniTask PhaseCost(SettlementResult result, CancellationToken ct)
    {
        Log("Phase: 成本计算");

        // 1. 显示节点成本
        Log("  显示节点成本...");
        foreach (var kvp in aviationSystem.aviationNodeDict)
        {
            var node = kvp.Value;
            var cityNode = node.cityNodeView;
            if (cityNode == null) continue;

            // 节点闪红
            cityNode.Flash(SettlementAnimConfig.CostColor, 0.3f);

            // 获取节点成本
            float nodeCost = result.nodeCosts.GetValueOrDefault(node.nodeIndex, 0);
            if (nodeCost > 0)
            {
                // 显示负数（成本）
                await CreateFloatingNumber(
                    cityNode.GetFloatingNumberPosition(),
                    -nodeCost,  // 负数显示
                    SettlementAnimConfig.CostColor);
            }
        }

        await UniTask.Delay((int)(SettlementAnimConfig.CostShowDuration * 500), cancellationToken: ct);

        // 2. 显示航线成本
        Log("  显示航线成本...");
        foreach (var kvp in aviationSystem.aviationEdgeDict)
        {
            var edge = kvp.Value;
            var edgeView = edge.edgeLineView;
            if (edgeView == null) continue;

            // 边线变红
            edgeView.SetTint(SettlementAnimConfig.CostColor, 0.2f);

            // 获取航线成本
            float edgeCost = result.edgeCosts.GetValueOrDefault(edge.edgeIndex, 0);
            if (edgeCost > 0)
            {
                // 在航线中点显示成本
                Vector3 midPos = GetEdgeMidpoint(edge);
                await CreateFloatingNumber(
                    midPos,
                    -edgeCost,  // 负数显示
                    SettlementAnimConfig.CostColor);
            }
        }

        await UniTask.Delay((int)(SettlementAnimConfig.CostShowDuration * 500), cancellationToken: ct);

        // 3. 清理成本数字
        ClearFloatingNumbers();

        // 4. 更新总资产（动画滚动）
        long finalAssets = result.assetsAfter;
        if (GameLogicUI.Instance != null)
        {
            await GameLogicUI.Instance.AnimateMoneyTo(finalAssets, SettlementAnimConfig.MoneyRollDuration);
        }
    }

    /// <summary>
    /// 获取航线中点位置
    /// </summary>
    private Vector3 GetEdgeMidpoint(AviationEdge edge)
    {
        if (edge.pathCoords == null || edge.pathCoords.Count == 0)
        {
            // 使用起点和终点的中点
            Vector3 from = HexConverter2D.HexToWorld(edge.fromNode.hexCoord);
            Vector3 to = HexConverter2D.HexToWorld(edge.toNode.hexCoord);
            return (from + to) / 2f + SettlementAnimConfig.FloatingNumberOffset;
        }

        // 使用路径中点
        int midIndex = edge.pathCoords.Count / 2;
        return HexConverter2D.HexToWorld(edge.pathCoords[midIndex]) + SettlementAnimConfig.FloatingNumberOffset;
    }

    #endregion

    #region 阶段：恢复

    private async UniTask PhaseRestore(CancellationToken ct)
    {
        Log("Phase: 恢复");

        float duration = SettlementAnimConfig.RestoreDuration;

        // 所有节点恢复
        foreach (var cityNode in aviationSystem.GetAllCityNodes())
        {
            cityNode.Restore(duration);
        }

        // 所有边线恢复
        foreach (var edgeView in aviationSystem.GetAllEdgeViews())
        {
            edgeView.Restore(duration);
        }

        await UniTask.Delay((int)(duration * 1000), cancellationToken: ct);
    }

    #endregion

    #region 辅助方法

    private async UniTask<FloatingNumber> CreateFloatingNumber(Vector3 worldPos, float value, Color color, float duration = -1f)
    {
        if (floatingNumberPrefab == null)
        {
            Debug.LogWarning("[SettlementAnimator] floatingNumberPrefab 未设置");
            return null;
        }

        // 如果没有指定 duration，使用默认值
        if (duration < 0)
        {
            duration = SettlementAnimConfig.NodePopDelay;
        }

        var go = Instantiate(floatingNumberPrefab, worldPos, Quaternion.identity);
        var fn = go.GetComponent<FloatingNumber>();
        if (fn != null)
        {
            await fn.Show(value, color, duration);
            activeFloatingNumbers.Add(fn);
        }
        return fn;
    }

    /// <summary>
    /// 创建倍率显示数字（格式：×1.5）
    /// </summary>
    private async UniTask CreateMultiplierNumber(Vector3 worldPos, float multiplier, Color color, float duration)
    {
        if (floatingNumberPrefab == null) return;

        var go = Instantiate(floatingNumberPrefab, worldPos, Quaternion.identity);
        var fn = go.GetComponent<FloatingNumber>();
        if (fn != null)
        {
            // 倍率使用特殊格式显示
            await fn.ShowMultiplier(multiplier, color, duration);
            activeFloatingNumbers.Add(fn);
        }
    }

    /// <summary>
    /// 倍率撞击动画：倍率数字从起点飞向目标节点，碰撞后更新节点数字为最终值
    /// </summary>
    private async UniTask AnimateMultiplierImpact(
        Vector3 startPos,
        Vector3 targetPos,
        FloatingNumber nodeNumber,
        float multiplier,
        float finalValue,
        float timeScale,
        CancellationToken ct)
    {
        if (floatingNumberPrefab == null || nodeNumber == null) return;

        // 1. 创建飞行的倍率数字
        var multiplierGo = Instantiate(floatingNumberPrefab, startPos, Quaternion.identity);
        var multiplierFn = multiplierGo.GetComponent<FloatingNumber>();
        if (multiplierFn == null)
        {
            Destroy(multiplierGo);
            return;
        }

        // 显示倍率（小一点）
        multiplierFn.transform.localScale = Vector3.one * 0.7f;
        await multiplierFn.ShowMultiplier(multiplier, SettlementAnimConfig.MultiplierColor, 0.1f * timeScale);

        // 2. 飞向目标节点
        float flyDuration = 0.3f * timeScale;
        var flyTween = multiplierFn.transform.DOMove(targetPos, flyDuration).SetEase(Ease.InQuad);
        await UniTask.WaitUntil(() => !flyTween.IsActive() || flyTween.IsComplete(), cancellationToken: ct);

        // 3. 碰撞效果 - 销毁倍率数字
        Destroy(multiplierGo);

        // 4. 节点数字震动并更新为最终值
        if (nodeNumber != null)
        {
            // 震动效果
            nodeNumber.transform.DOShakeScale(0.2f * timeScale, 0.3f, 10);

            // 更新为最终收益值
            await nodeNumber.AnimateTo(finalValue, SettlementAnimConfig.IncomeColor, 0.2f * timeScale);
        }
    }

    private void ClearFloatingNumbers()
    {
        foreach (var fn in activeFloatingNumbers)
        {
            if (fn != null)
            {
                fn.DestroyImmediate();
            }
        }
        activeFloatingNumbers.Clear();
    }

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SettlementAnimator] {message}");
        }
    }

    #endregion
}
