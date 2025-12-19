using System;
using UnityEngine;

/// <summary>
/// 玩家运行时数据管理器
/// 持有当前游戏局的所有玩家相关运行时数据
/// 单例模式，跨场景保持
/// </summary>
public class PlayerRunTimeInfo : BaseManager<PlayerRunTimeInfo>
{
    private PlayerRunTimeInfo() { }

    // ========== 核心数据 ==========

    /// <summary>
    /// 玩家数据（经济、回合、Buff等）
    /// </summary>
    public PlayerData playerData = new PlayerData();

    /// <summary>
    /// 当前回合的结算结果（结算完成后填充）
    /// </summary>
    public SettlementResult lastSettlementResult = new SettlementResult();

    // ========== 快捷访问属性 ==========

    /// <summary>
    /// 当前资产
    /// </summary>
    public long Assets
    {
        get => playerData.Assets;
        set => playerData.Assets = value;
    }

    /// <summary>
    /// 当前回合数
    /// </summary>
    public int CurrentRound
    {
        get => playerData.CurrentRound;
        set => playerData.CurrentRound = value;
    }

    /// <summary>
    /// 是否破产
    /// </summary>
    public bool IsBankrupt => playerData.IsBankrupt;

    // ========== 事件 ==========

    /// <summary>
    /// 资产变化事件（UI 绑定用）
    /// </summary>
    public event Action<long, long> OnAssetsChanged
    {
        add => playerData.OnAssetsChanged += value;
        remove => playerData.OnAssetsChanged -= value;
    }

    /// <summary>
    /// 回合开始事件
    /// </summary>
    public event Action<int> OnRoundStarted;

    /// <summary>
    /// 回合结算完成事件
    /// </summary>
    public event Action<SettlementResult> OnSettlementCompleted;

    // ========== 边建造/拆除成本配置 ==========

    /// <summary>
    /// 每格航线建造成本
    /// </summary>
    public int edgeBuildCostPerTile = 10;

    /// <summary>
    /// 拆除航线返还比例（0.0 - 1.0）
    /// </summary>
    public float edgeRemoveRefundRate = 0.5f;

    // ========== 边建造/拆除处理 ==========

    /// <summary>
    /// 绑定 AviationSystem 事件（在游戏开始时调用）
    /// </summary>
    public void BindAviationEvents()
    {
        if (AviationSystem.Instance != null)
        {
            AviationSystem.Instance.OnEdgeAdded -= OnEdgeBuilt;
            AviationSystem.Instance.OnEdgeRemoved -= OnEdgeRemoved;
            AviationSystem.Instance.OnEdgeAdded += OnEdgeBuilt;
            AviationSystem.Instance.OnEdgeRemoved += OnEdgeRemoved;
            Debug.Log("[PlayerRunTimeInfo] 已绑定 AviationSystem 事件");
        }
    }

    /// <summary>
    /// 解绑 AviationSystem 事件（在游戏结束时调用）
    /// </summary>
    public void UnbindAviationEvents()
    {
        if (AviationSystem.Instance != null)
        {
            AviationSystem.Instance.OnEdgeAdded -= OnEdgeBuilt;
            AviationSystem.Instance.OnEdgeRemoved -= OnEdgeRemoved;
            Debug.Log("[PlayerRunTimeInfo] 已解绑 AviationSystem 事件");
        }
    }

    /// <summary>
    /// 边建造完成回调
    /// </summary>
    private void OnEdgeBuilt(AviationEdge edge, int pathLength)
    {
        // 计算建造成本（考虑地形成本倍率）
        // totalBuildCostMultiplier 是每个格子成本倍率的总和
        float terrainMultiplier = edge.totalBuildCostMultiplier;
        long buildCost = (long)(terrainMultiplier * edgeBuildCostPerTile);

        // 扣除金钱
        Assets -= buildCost;

        Debug.Log($"[PlayerRunTimeInfo] 建造航线: 路径长度={pathLength}, 地形倍率={terrainMultiplier:F1}, 成本={buildCost}, 剩余资产={Assets}");
    }

    /// <summary>
    /// 边拆除完成回调
    /// </summary>
    private void OnEdgeRemoved(AviationEdge edge, int pathLength)
    {
        // 计算返还金额（使用存储的地形成本倍率）
        float terrainMultiplier = edge.totalBuildCostMultiplier;
        long refund = (long)(terrainMultiplier * edgeBuildCostPerTile * edgeRemoveRefundRate);

        // 返还金钱
        Assets += refund;

        Debug.Log($"[PlayerRunTimeInfo] 拆除航线: 路径长度={pathLength}, 地形倍率={terrainMultiplier:F1}, 返还={refund}, 剩余资产={Assets}");
    }

    // ========== 方法 ==========

    /// <summary>
    /// 开始新回合
    /// </summary>
    public void StartNewRound()
    {
        playerData.StartNewRound();
        OnRoundStarted?.Invoke(playerData.CurrentRound);
        Debug.Log($"[PlayerRunTimeInfo] 第 {playerData.CurrentRound} 回合开始");
    }

    /// <summary>
    /// 应用结算结果
    /// </summary>
    /// <param name="result">结算结果</param>
    public void ApplySettlement(SettlementResult result)
    {
        result.assetsBefore = playerData.Assets;
        playerData.ApplySettlement(result.totalIncome, result.TotalCost);
        result.assetsAfter = playerData.Assets;

        lastSettlementResult = result;
        OnSettlementCompleted?.Invoke(result);

        Debug.Log($"[PlayerRunTimeInfo] 结算完成: 收益={result.totalIncome}, 成本={result.TotalCost}, 净利润={result.NetProfit}");
    }

    /// <summary>
    /// 记录新增节点（用于 Buff 判定）
    /// </summary>
    public void RecordNodeAdded()
    {
        playerData.NodesAddedThisRound++;
    }

    /// <summary>
    /// 购买 Buff
    /// </summary>
    /// <param name="buffId">Buff ID</param>
    /// <param name="cost">购买成本</param>
    /// <returns>是否购买成功</returns>
    public bool PurchaseBuff(int buffId, long cost)
    {
        if (playerData.Assets < cost)
        {
            Debug.LogWarning($"[PlayerRunTimeInfo] 资产不足，无法购买 Buff {buffId}");
            return false;
        }

        playerData.Assets -= cost;
        playerData.ownedPlayerBuffIds.Add(buffId);
        Debug.Log($"[PlayerRunTimeInfo] 购买 Buff {buffId}，花费 {cost}");
        return true;
    }

    /// <summary>
    /// 设置本回合市场 Buff
    /// </summary>
    /// <param name="buffIds">Buff ID 列表</param>
    public void SetMarketBuffs(params int[] buffIds)
    {
        playerData.currentMarketBuffIds.Clear();
        playerData.currentMarketBuffIds.AddRange(buffIds);
    }

    /// <summary>
    /// 重置所有数据（新游戏时调用）
    /// </summary>
    public void Reset()
    {
        playerData.Reset();
        lastSettlementResult.Clear();
        Debug.Log("[PlayerRunTimeInfo] 玩家运行时数据已重置");
    }

    /// <summary>
    /// 初始化（可选：从存档加载）
    /// </summary>
    /// <param name="savedData">存档数据（可为 null）</param>
    public void Initialize(PlayerData savedData = null)
    {
        if (savedData != null)
        {
            playerData = savedData;
            Debug.Log("[PlayerRunTimeInfo] 从存档加载玩家数据");
        }
        else
        {
            Reset();
        }
    }
}
