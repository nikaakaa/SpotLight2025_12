using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家运行时数据
/// 普通实例类（非单例），生命周期由 GameLogicState 管理
/// 
/// 设计原则：
/// - PlayerData = 静态配置（初始值、规则参数）
/// - PlayerRunTimeInfo = 运行时数据（资产、回合、Buff等）
/// 
/// 生命周期：
/// - LoadingGameState 中创建实例
/// - GameLogicState 中注册使用
/// - 退出 GameLogicState 时自动清理
/// </summary>
public class PlayerRunTimeInfo
{
    // ========== 静态访问（运行时实例） ==========

    /// <summary>
    /// 当前运行时实例（由 GameLogicState 设置）
    /// </summary>
    public static PlayerRunTimeInfo Current { get; set; }

    // ========== 核心运行时数据 ==========

    private long assets;
    private int currentRound;

    /// <summary>
    /// 当前资产
    /// </summary>
    public long Assets
    {
        get => assets;
        set
        {
            long oldValue = assets;
            assets = value;
            OnAssetsChanged?.Invoke(oldValue, assets);
        }
    }

    /// <summary>
    /// 当前回合数
    /// </summary>
    public int CurrentRound
    {
        get => currentRound;
        set => currentRound = Mathf.Max(1, value);
    }

    /// <summary>
    /// 本回合新增的节点数量（用于 Buff #15 判定）
    /// </summary>
    public int NodesAddedThisRound { get; set; }

    /// <summary>
    /// 本回合新增的航线数量
    /// </summary>
    public int EdgesAddedThisRound { get; set; }

    /// <summary>
    /// 本回合是否禁止升级机场（Buff #18）
    /// </summary>
    public bool IsUpgradeBlocked { get; set; }

    /// <summary>
    /// 本回合是否禁止新建节点（Buff #18）
    /// </summary>
    public bool IsBuildBlocked { get; set; }

    /// <summary>
    /// 是否破产
    /// </summary>
    public bool IsBankrupt => assets < PlayerData.BANKRUPTCY_THRESHOLD;

    // ========== Buff 相关 ==========

    /// <summary>
    /// 玩家永久 Buff ID 列表（从商店购买的升级Buff）
    /// </summary>
    public List<int> OwnedPlayerBuffIds { get; private set; } = new List<int>();

    /// <summary>
    /// 当前回合的市场趋势 Buff ID 列表（每回合随机抽取）
    /// </summary>
    public List<int> CurrentMarketBuffIds { get; private set; } = new List<int>();

    // ========== 统计数据 ==========

    public long TotalIncomeEarned { get; set; }
    public long TotalCostPaid { get; set; }
    public long HighestRoundIncome { get; set; }
    public long HighestRoundProfit { get; set; }
    public long PeakAssets { get; set; }

    /// <summary>
    /// 当前回合的结算结果
    /// </summary>
    public SettlementResult LastSettlementResult { get; private set; } = new SettlementResult();

    // ========== 事件 ==========

    /// <summary>
    /// 资产变化事件（UI 绑定用）
    /// </summary>
    public event Action<long, long> OnAssetsChanged;

    /// <summary>
    /// 回合开始事件
    /// </summary>
    public event Action<int> OnRoundStarted;

    /// <summary>
    /// 回合结算完成事件
    /// </summary>
    public event Action<SettlementResult> OnSettlementCompleted;

    // ========== 构造函数 ==========

    public PlayerRunTimeInfo()
    {
        Reset();
    }

    // ========== 回合生命周期 ==========

    /// <summary>
    /// 开始新回合
    /// </summary>
    public void StartNewRound()
    {
        CurrentRound++;
        NodesAddedThisRound = 0;
        EdgesAddedThisRound = 0;
        IsUpgradeBlocked = false;
        IsBuildBlocked = false;
        CurrentMarketBuffIds.Clear();

        OnRoundStarted?.Invoke(CurrentRound);
        Debug.Log($"[PlayerRunTimeInfo] 第 {CurrentRound} 回合开始");
    }

    /// <summary>
    /// 应用结算结果
    /// </summary>
    public void ApplySettlement(SettlementResult result)
    {
        result.assetsBefore = Assets;

        long netProfit = result.totalIncome - result.TotalCost;

        // 更新统计
        TotalIncomeEarned += result.totalIncome;
        TotalCostPaid += result.TotalCost;
        if (result.totalIncome > HighestRoundIncome) HighestRoundIncome = result.totalIncome;
        if (netProfit > HighestRoundProfit) HighestRoundProfit = netProfit;

        // 更新资产
        Assets += netProfit;
        if (Assets > PeakAssets) PeakAssets = Assets;

        result.assetsAfter = Assets;
        LastSettlementResult = result;

        OnSettlementCompleted?.Invoke(result);
        Debug.Log($"[PlayerRunTimeInfo] 结算完成: 收益={result.totalIncome}, 成本={result.TotalCost}, 净利润={result.NetProfit}");
    }

    // ========== 节点/航线操作 ==========

    /// <summary>
    /// 记录新增节点
    /// </summary>
    public void RecordNodeAdded()
    {
        NodesAddedThisRound++;
    }

    /// <summary>
    /// 记录新增航线
    /// </summary>
    public void RecordEdgeAdded()
    {
        EdgesAddedThisRound++;
    }

    // ========== Buff 操作 ==========

    /// <summary>
    /// 购买 Buff
    /// </summary>
    public bool PurchaseBuff(int buffId, long cost)
    {
        if (Assets < cost)
        {
            Debug.LogWarning($"[PlayerRunTimeInfo] 资产不足，无法购买 Buff {buffId}");
            return false;
        }

        Assets -= cost;
        OwnedPlayerBuffIds.Add(buffId);
        Debug.Log($"[PlayerRunTimeInfo] 购买 Buff {buffId}，花费 {cost}");
        return true;
    }

    /// <summary>
    /// 设置本回合市场 Buff
    /// </summary>
    public void SetMarketBuffs(params int[] buffIds)
    {
        CurrentMarketBuffIds.Clear();
        CurrentMarketBuffIds.AddRange(buffIds);
    }

    /// <summary>
    /// 检查是否拥有指定玩家 Buff
    /// </summary>
    public bool HasPlayerBuff(int buffId) => OwnedPlayerBuffIds.Contains(buffId);

    /// <summary>
    /// 检查当前回合是否有指定市场 Buff
    /// </summary>
    public bool HasMarketBuff(int buffId) => CurrentMarketBuffIds.Contains(buffId);

    // ========== 资产操作 ==========

    /// <summary>
    /// 检查是否能支付指定金额
    /// </summary>
    public bool CanAfford(long amount) => Assets >= amount;

    /// <summary>
    /// 尝试扣除资产
    /// </summary>
    public bool TrySpend(long amount)
    {
        if (Assets < amount) return false;
        Assets -= amount;
        return true;
    }

    // ========== 重置 ==========

    /// <summary>
    /// 重置所有数据（新游戏时调用）
    /// </summary>
    public void Reset()
    {
        assets = PlayerData.INITIAL_ASSETS;
        currentRound = PlayerData.INITIAL_ROUND;
        NodesAddedThisRound = 0;
        EdgesAddedThisRound = 0;
        IsUpgradeBlocked = false;
        IsBuildBlocked = false;
        OwnedPlayerBuffIds.Clear();
        CurrentMarketBuffIds.Clear();
        TotalIncomeEarned = 0;
        TotalCostPaid = 0;
        HighestRoundIncome = 0;
        HighestRoundProfit = 0;
        PeakAssets = PlayerData.INITIAL_ASSETS;
        LastSettlementResult?.Clear();

        Debug.Log("[PlayerRunTimeInfo] 玩家运行时数据已重置");
    }
}
