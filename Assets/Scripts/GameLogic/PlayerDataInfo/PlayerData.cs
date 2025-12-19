using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家静态数据（纯数据容器，无 MonoBehaviour 依赖）
/// 遵循数据与逻辑分离原则
/// </summary>
[Serializable]
public class PlayerData
{
    // ========== 核心经济数据 ==========

    /// <summary>
    /// 当前资产（金钱）
    /// </summary>
    [SerializeField] private long assets = 1000;
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
    /// 资产变化事件（用于 UI 更新）
    /// </summary>
    public event Action<long, long> OnAssetsChanged;

    // ========== 回合数据 ==========

    /// <summary>
    /// 当前回合数
    /// </summary>
    [SerializeField] private int currentRound = 1;
    public int CurrentRound
    {
        get => currentRound;
        set => currentRound = Mathf.Max(1, value);
    }

    /// <summary>
    /// 本回合新增的节点数量（用于 Buff #15 判定）
    /// </summary>
    public int NodesAddedThisRound { get; set; } = 0;

    /// <summary>
    /// 本回合是否禁止升级机场（用于 Buff #18 判定）
    /// </summary>
    public bool IsUpgradeBlocked { get; set; } = false;

    // ========== Buff 相关 ==========

    /// <summary>
    /// 玩家永久 Buff ID 列表（从商店购买的）
    /// </summary>
    public List<int> ownedPlayerBuffIds = new List<int>();

    /// <summary>
    /// 当前回合的市场趋势 Buff ID 列表（每回合随机）
    /// </summary>
    public List<int> currentMarketBuffIds = new List<int>();

    // ========== 统计数据 ==========

    /// <summary>
    /// 累计收益（历史总额）
    /// </summary>
    public long TotalIncomeEarned { get; set; } = 0;

    /// <summary>
    /// 累计成本（历史总额）
    /// </summary>
    public long TotalCostPaid { get; set; } = 0;

    /// <summary>
    /// 最高单回合收益
    /// </summary>
    public long HighestRoundIncome { get; set; } = 0;

    // ========== 方法 ==========

    /// <summary>
    /// 开始新回合（重置回合相关数据）
    /// </summary>
    public void StartNewRound()
    {
        currentRound++;
        NodesAddedThisRound = 0;
        IsUpgradeBlocked = false;
        currentMarketBuffIds.Clear();
    }

    /// <summary>
    /// 结算收益
    /// </summary>
    /// <param name="income">本回合收益</param>
    /// <param name="cost">本回合成本</param>
    /// <returns>净收益（可为负）</returns>
    public long ApplySettlement(long income, long cost)
    {
        long netProfit = income - cost;

        // 更新统计
        TotalIncomeEarned += income;
        TotalCostPaid += cost;
        if (income > HighestRoundIncome)
            HighestRoundIncome = income;

        // 更新资产（通过属性触发事件）
        Assets += netProfit;

        return netProfit;
    }

    /// <summary>
    /// 是否破产
    /// </summary>
    public bool IsBankrupt => assets < 0;

    /// <summary>
    /// 重置为初始状态
    /// </summary>
    public void Reset()
    {
        assets = 1000;
        currentRound = 1;
        NodesAddedThisRound = 0;
        IsUpgradeBlocked = false;
        ownedPlayerBuffIds.Clear();
        currentMarketBuffIds.Clear();
        TotalIncomeEarned = 0;
        TotalCostPaid = 0;
        HighestRoundIncome = 0;
    }
}
