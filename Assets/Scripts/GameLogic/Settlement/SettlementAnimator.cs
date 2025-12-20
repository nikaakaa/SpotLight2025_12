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

    // ================================
    // ★ 音效相关
    // ================================
    private AudioClip settlementSoundClip;
    private bool soundClipLoaded = false;
    private int soundPlayCount = 0;

    void Awake()
    {
        Instance = this;
        LoadSettlementSound();
    }

    /// <summary>
    /// 预加载结算音效
    /// </summary>
    private void LoadSettlementSound()
    {
        try
        {
            settlementSoundClip = Resources.Load<AudioClip>("Audio/Retro9");
            if (settlementSoundClip != null)
            {
                soundClipLoaded = true;
                Debug.Log($"[SettlementAnimator] 结算音效预加载成功 (长度: {settlementSoundClip.length}s)");
            }
            else
            {
                Debug.LogWarning("[SettlementAnimator] 无法加载音效 Audio/Retro9");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SettlementAnimator] 加载音效异常: {ex.Message}");
        }
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
        soundPlayCount = 0;

        Log("开始结算动画");

        GameLogicUI.Instance?.SetSettlementMode(true);

        try
        {
            await PhaseDim(ct);
            await PhaseStructuresIncome(structures, result, ct);
            await PhaseCost(result, ct);
            await PhaseRestore(ct);
        }
        finally
        {
            GameLogicUI.Instance?.SetSettlementMode(false);
            ClearFloatingNumbers();
        }

        Log($"结算动画完成 (播放音效 {soundPlayCount} 次)");
    }

    #region 阶段：准备（变暗）

    private async UniTask PhaseDim(CancellationToken ct)
    {
        Log("Phase: 准备（变暗）");

        float duration = SettlementAnimConfig.DimDuration;

        foreach (var cityNode in aviationSystem.GetAllCityNodes())
            cityNode.SetDim(true, duration);

        foreach (var edgeView in aviationSystem.GetAllEdgeViews())
            edgeView.SetDim(true, duration);

        await UniTask.Delay((int)(duration * 1000), cancellationToken: ct);
    }

    #endregion

    #region 阶段：结构收益

    private async UniTask PhaseStructuresIncome(AllStructures structures, SettlementResult result, CancellationToken ct)
    {
        Log("Phase: 结构收益");

        int totalStructures = structures.TotalCount;
        int processedCount = 0;

        foreach (var ring in structures.Rings)
        {
            float speedMultiplier = CalculateSpeedMultiplier(processedCount, totalStructures);
            await AnimateStructure(ring, result, processedCount, speedMultiplier, ct);
            processedCount++;
        }

        foreach (var radial in structures.Radials)
        {
            float speedMultiplier = CalculateSpeedMultiplier(processedCount, totalStructures);
            await AnimateStructure(radial, result, processedCount, speedMultiplier, ct);
            processedCount++;
        }

        foreach (var singleLine in structures.SingleLines)
        {
            float speedMultiplier = CalculateSpeedMultiplier(processedCount, totalStructures);
            await AnimateStructure(singleLine, result, processedCount, speedMultiplier, ct);
            processedCount++;
        }
    }

    private float CalculateSpeedMultiplier(int currentIndex, int total)
    {
        float speedMultiplier = 1f + currentIndex * SettlementAnimConfig.SpeedIncrementPerStructure;
        return Mathf.Min(speedMultiplier, SettlementAnimConfig.MaxSpeedMultiplier);
    }

    private async UniTask AnimateStructure(
        StructureBase structure,
        SettlementResult result,
        int structureIndex,
        float speedMultiplier,
        CancellationToken ct)
    {
        Log($"  结构 #{structureIndex}: {structure.Type}, {structure.Nodes.Count} 个节点, 速度x{speedMultiplier:F1}");

        float timeScale = 1f / speedMultiplier;

        foreach (var edge in structure.Edges)
            edge.edgeLineView?.SetTint(SettlementAnimConfig.IncomeColor, 0.2f * timeScale);

        if (result.structureAppliedBuffs != null &&
            result.structureAppliedBuffs.TryGetValue(structureIndex, out var structureBuffIds))
        {
            foreach (var buffId in structureBuffIds)
                GameLogicUI.Instance?.PunchBuffItem(buffId);
        }

        var nodeFloatingNumbers = new Dictionary<int, FloatingNumber>();

        foreach (var node in structure.Nodes)
        {
            var cityNode = node.cityNodeView;
            if (cityNode == null) continue;

            cityNode.Highlight(0.1f * timeScale);

            float rawIncome = result.nodeRawIncomes.GetValueOrDefault(node.nodeIndex, 0);

            var floatingNumber = await CreateFloatingNumber(
                cityNode.GetFloatingNumberPosition(),
                rawIncome,
                SettlementAnimConfig.IncomeColor,
                SettlementAnimConfig.NodePopDelay * timeScale);

            if (floatingNumber != null)
            {
                nodeFloatingNumbers[node.nodeIndex] = floatingNumber;
                
                // ★ 每个节点都播放一次音效（使用独立 AudioSource）
                PlaySettlementSound();
            }

            int nodeDelay = (int)(SettlementAnimConfig.NodePopDelay * 1000 * timeScale);
            if (nodeDelay > 0)
                await UniTask.Delay(nodeDelay, cancellationToken: ct);
        }

        await AnimateBuffEffects(structure, result, nodeFloatingNumbers, timeScale, ct);

        if (result.structureMultipliers.TryGetValue(structureIndex, out float multiplier) && multiplier != 1f)
        {
            Log($"    结构倍率: ×{multiplier:F1}");

            foreach (var edge in structure.Edges)
                edge.edgeLineView?.Flash(SettlementAnimConfig.MultiplierColor, 0.2f * timeScale);

            if (structure.Edges.Count > 0)
            {
                var centerEdge = structure.Edges[structure.Edges.Count / 2];
                Vector3 multiplierStartPos = GetEdgeMidpoint(centerEdge);

                var impactTasks = new List<UniTask>();
                foreach (var node in structure.Nodes)
                {
                    if (!nodeFloatingNumbers.TryGetValue(node.nodeIndex, out var nodeNumber)) continue;
                    if (node.cityNodeView == null) continue;

                    Vector3 nodeTargetPos = node.cityNodeView.GetFloatingNumberPosition();
                    float baseIncome = result.nodeBaseIncomes.GetValueOrDefault(node.nodeIndex, 0);
                    float finalIncome = result.nodeFinalIncomes.GetValueOrDefault(node.nodeIndex, baseIncome);

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

        Vector3 moneyTargetPos = GameLogicUI.Instance?.GetMoneyWorldPosition() ?? Vector3.zero;
        var flyTasks = new List<UniTask>();

        foreach (var fn in activeFloatingNumbers)
            if (fn != null)
                flyTasks.Add(fn.FlyTo(moneyTargetPos, SettlementAnimConfig.NumberFlyDuration * timeScale));

        await UniTask.WhenAll(flyTasks);
        activeFloatingNumbers.Clear();

        float dimDuration = 0.2f * timeScale;
        foreach (var node in structure.Nodes)
            node.cityNodeView?.SetDim(true, dimDuration);

        foreach (var edge in structure.Edges)
            edge.edgeLineView?.SetDim(true, dimDuration);

        int structureDelay = (int)(SettlementAnimConfig.StructureInterval * 1000 * timeScale);
        if (structureDelay > 50)
            await UniTask.Delay(structureDelay, cancellationToken: ct);
    }

    #endregion

    #region 阶段：成本

    private async UniTask PhaseCost(SettlementResult result, CancellationToken ct)
    {
        Log("Phase: 成本计算");

        Log("  显示节点成本...");
        foreach (var kvp in aviationSystem.aviationNodeDict)
        {
            var node = kvp.Value;
            var cityNode = node.cityNodeView;
            if (cityNode == null) continue;

            cityNode.Flash(SettlementAnimConfig.CostColor, 0.3f);

            float nodeCost = result.nodeCosts.GetValueOrDefault(node.nodeIndex, 0);
            if (nodeCost > 0)
            {
                await CreateFloatingNumber(
                    cityNode.GetFloatingNumberPosition(),
                    -nodeCost,
                    SettlementAnimConfig.CostColor);
            }
        }

        await UniTask.Delay((int)(SettlementAnimConfig.CostShowDuration * 500), cancellationToken: ct);

        Log("  显示航线成本...");
        foreach (var kvp in aviationSystem.aviationEdgeDict)
        {
            var edge = kvp.Value;
            var edgeView = edge.edgeLineView;
            if (edgeView == null) continue;

            edgeView.SetTint(SettlementAnimConfig.CostColor, 0.2f);

            float edgeCost = result.edgeCosts.GetValueOrDefault(edge.edgeIndex, 0);
            if (edgeCost > 0)
            {
                Vector3 midPos = GetEdgeMidpoint(edge);
                await CreateFloatingNumber(
                    midPos,
                    -edgeCost,
                    SettlementAnimConfig.CostColor);
            }
        }

        await UniTask.Delay((int)(SettlementAnimConfig.CostShowDuration * 500), cancellationToken: ct);

        ClearFloatingNumbers();

        long finalAssets = result.assetsAfter;
        if (GameLogicUI.Instance != null)
        {
            await GameLogicUI.Instance.AnimateMoneyTo(finalAssets, SettlementAnimConfig.MoneyRollDuration, result.assetsBefore);
        }
    }

    private Vector3 GetEdgeMidpoint(AviationEdge edge)
    {
        if (edge.pathCoords == null || edge.pathCoords.Count == 0)
        {
            Vector3 from = HexConverter2D.HexToWorld(edge.fromNode.hexCoord);
            Vector3 to = HexConverter2D.HexToWorld(edge.toNode.hexCoord);
            return (from + to) / 2f + SettlementAnimConfig.FloatingNumberOffset;
        }

        int midIndex = edge.pathCoords.Count / 2;
        return HexConverter2D.HexToWorld(edge.pathCoords[midIndex]) + SettlementAnimConfig.FloatingNumberOffset;
    }

    #endregion

    #region 阶段：恢复

    private async UniTask PhaseRestore(CancellationToken ct)
    {
        Log("Phase: 恢复");

        float duration = SettlementAnimConfig.RestoreDuration;

        foreach (var cityNode in aviationSystem.GetAllCityNodes())
            cityNode.Restore(duration);

        foreach (var edgeView in aviationSystem.GetAllEdgeViews())
            edgeView.Restore(duration);

        await UniTask.Delay((int)(duration * 1000), cancellationToken: ct);
    }

    #endregion

    #region 音效

    /// <summary>
    /// ★ 使用独立 AudioSource 播放音效
    /// 每个节点调用时都创建新的 AudioSource，确保不会互相覆盖
    /// </summary>
    private void PlaySettlementSound()
    {
        if (!soundClipLoaded || settlementSoundClip == null)
            return;

        soundPlayCount++;

        // 创建新的 GameObject 和 AudioSource
        GameObject audioGO = new GameObject($"SettlementAudio_{soundPlayCount}");
        AudioSource audioSource = audioGO.AddComponent<AudioSource>();
        
        audioSource.clip = settlementSoundClip;
        audioSource.volume = 0.8f;
        audioSource.spatialBlend = 0f;
        audioSource.Play();

        // 播放完后销毁
        Destroy(audioGO, settlementSoundClip.length);

        if (enableDebugLog)
            Debug.Log($"[SettlementAnimator] 播放节点音效 #{soundPlayCount} (长度: {settlementSoundClip.length}s)");
    }

    #endregion

    #region Buff 效果

    private async UniTask AnimateBuffEffects(
        StructureBase structure,
        SettlementResult result,
        Dictionary<int, FloatingNumber> nodeFloatingNumbers,
        float timeScale,
        CancellationToken ct)
    {
        bool hasAnyBuff = false;
        foreach (var node in structure.Nodes)
        {
            long flatBonus = result.nodeBuffFlatBonus.GetValueOrDefault(node.nodeIndex, 0);
            float multiplier = result.nodeBuffMultiplier.GetValueOrDefault(node.nodeIndex, 1f);
            if (flatBonus != 0 || Mathf.Abs(multiplier - 1f) > 0.001f)
            {
                hasAnyBuff = true;
                break;
            }
        }

        if (!hasAnyBuff)
        {
            Log("    无 Buff 效果，跳过动画");
            return;
        }

        Log("    播放 Buff 效果动画...");

        var buffTasks = new List<UniTask>();

        foreach (var node in structure.Nodes)
        {
            if (!nodeFloatingNumbers.TryGetValue(node.nodeIndex, out var nodeNumber)) continue;
            if (node.cityNodeView == null) continue;

            long flatBonus = result.nodeBuffFlatBonus.GetValueOrDefault(node.nodeIndex, 0);
            float multiplier = result.nodeBuffMultiplier.GetValueOrDefault(node.nodeIndex, 1f);
            float rawIncome = result.nodeRawIncomes.GetValueOrDefault(node.nodeIndex, 0);
            float incomeAfterBuff = result.nodeBaseIncomes.GetValueOrDefault(node.nodeIndex, rawIncome);

            Vector3 nodePos = node.cityNodeView.GetFloatingNumberPosition();

            if (result.nodeAppliedBuffs != null &&
                result.nodeAppliedBuffs.TryGetValue(node.nodeIndex, out var buffIds))
            {
                foreach (var buffId in buffIds)
                    GameLogicUI.Instance?.PunchBuffItem(buffId);
            }

            if (flatBonus != 0)
            {
                float valueAfterFlat = rawIncome + flatBonus;

                buffTasks.Add(AnimateBuffImpact(
                    nodePos,
                    nodeNumber,
                    flatBonus,
                    isAdditive: true,
                    valueAfterFlat,
                    timeScale,
                    ct));
            }

            if (Mathf.Abs(multiplier - 1f) > 0.001f)
            {
                buffTasks.Add(AnimateBuffImpact(
                    nodePos,
                    nodeNumber,
                    multiplier,
                    isAdditive: false,
                    incomeAfterBuff,
                    timeScale,
                    ct));
            }
        }

        if (buffTasks.Count > 0)
        {
            await UniTask.WhenAll(buffTasks);
            await UniTask.Delay((int)(100 * timeScale), cancellationToken: ct);
        }
    }

    private async UniTask AnimateBuffImpact(
        Vector3 nodePos,
        FloatingNumber nodeNumber,
        float buffValue,
        bool isAdditive,
        float targetValue,
        float timeScale,
        CancellationToken ct)
    {
        if (floatingNumberPrefab == null || nodeNumber == null) return;

        Vector3 startPos = nodePos + SettlementAnimConfig.BuffStartOffset;

        var buffGo = Instantiate(floatingNumberPrefab, startPos, Quaternion.identity);
        var buffFn = buffGo.GetComponent<FloatingNumber>();
        if (buffFn == null)
        {
            Destroy(buffGo);
            return;
        }

        bool isPositive = isAdditive ? buffValue > 0 : buffValue > 1f;
        Color buffColor = isPositive
            ? SettlementAnimConfig.BuffPositiveColor
            : SettlementAnimConfig.BuffNegativeColor;

        buffFn.transform.localScale = Vector3.one * 0.6f;
        if (isAdditive)
            await buffFn.ShowAdditive((long)buffValue, buffColor, 0.1f * timeScale);
        else
            await buffFn.ShowMultiplier(buffValue, buffColor, 0.1f * timeScale);

        float flyDuration = SettlementAnimConfig.BuffFlyDuration * timeScale;
        var flyTween = buffFn.transform.DOMove(nodePos, flyDuration).SetEase(Ease.InQuad);
        await UniTask.WaitUntil(() => !flyTween.IsActive() || flyTween.IsComplete(), cancellationToken: ct);

        Destroy(buffGo);

        if (nodeNumber != null)
        {
            nodeNumber.transform.DOShakeScale(0.15f * timeScale, 0.2f, 8);
            await nodeNumber.AnimateTo(targetValue, SettlementAnimConfig.IncomeColor, 0.15f * timeScale);
        }
    }

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

        var multiplierGo = Instantiate(floatingNumberPrefab, startPos, Quaternion.identity);
        var multiplierFn = multiplierGo.GetComponent<FloatingNumber>();
        if (multiplierFn == null)
        {
            Destroy(multiplierGo);
            return;
        }

        multiplierFn.transform.localScale = Vector3.one * 0.7f;
        await multiplierFn.ShowMultiplier(multiplier, SettlementAnimConfig.MultiplierColor, 0.1f * timeScale);

        float flyDuration = 0.3f * timeScale;
        var flyTween = multiplierFn.transform.DOMove(targetPos, flyDuration).SetEase(Ease.InQuad);
        await UniTask.WaitUntil(() => !flyTween.IsActive() || flyTween.IsComplete(), cancellationToken: ct);

        Destroy(multiplierGo);

        if (nodeNumber != null)
        {
            nodeNumber.transform.DOShakeScale(0.2f * timeScale, 0.3f, 10);
            await nodeNumber.AnimateTo(finalValue, SettlementAnimConfig.IncomeColor, 0.2f * timeScale);
        }
    }

    #endregion

    #region 辅助

    private async UniTask<FloatingNumber> CreateFloatingNumber(
        Vector3 worldPos,
        float value,
        Color color,
        float duration = -1f)
    {
        if (floatingNumberPrefab == null)
        {
            Debug.LogWarning("[SettlementAnimator] floatingNumberPrefab 未设置");
            return null;
        }

        if (duration < 0)
            duration = SettlementAnimConfig.NodePopDelay;

        var go = Instantiate(floatingNumberPrefab, worldPos, Quaternion.identity);
        var fn = go.GetComponent<FloatingNumber>();
        if (fn != null)
        {
            await fn.Show(value, color, duration);
            activeFloatingNumbers.Add(fn);
            ShakeCamera();
        }
        return fn;
    }

    private void ShakeCamera()
    {
        CinemachineCameraController.Instance?.Shake(
            SettlementAnimConfig.CameraShakeStrength,
            SettlementAnimConfig.CameraShakeDuration,
            SettlementAnimConfig.CameraShakeVibrato);
    }

    private void ClearFloatingNumbers()
    {
        foreach (var fn in activeFloatingNumbers)
            fn?.DestroyImmediate();

        activeFloatingNumbers.Clear();
    }

    private void Log(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[SettlementAnimator] {message}");
    }

    #endregion
}
