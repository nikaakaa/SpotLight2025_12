using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家视图层组件 - 场景中的玩家 GameObject
/// 
/// 架构设计：
/// - 视图层: Player (MonoBehaviour)
/// - 运行时层: PlayerRunTimeInfo (单向引用)
/// - Buff层: BuffHandler (组件引用)
/// 
/// 职责：
/// - 监听 PlayerRunTimeInfo 的事件并更新 UI 表现
/// - 提供场景中的玩家可视化入口
/// - 管理玩家永久 Buff 和市场趋势 Buff
/// - 提供结算时的 Buff 修正接口
/// </summary>
public class Player : MonoBehaviour
{
    // ========== 单例访问 ==========

    public static Player Instance { get; private set; }

    // ========== 运行时引用（单向） ==========

    [SerializeField] private bool autoBindOnStart = true;

    /// <summary>
    /// 运行时数据引用（单向引用，不持有数据所有权）
    /// </summary>
    private PlayerRunTimeInfo runtimeInfo;

    /// <summary>
    /// 是否已绑定运行时数据
    /// </summary>
    public bool IsBound => runtimeInfo != null;

    // ========== Buff 系统 ==========

    [Header("Buff 系统")]
    [SerializeField] private BuffHandler playerBuffHandler;  // 玩家永久 Buff
    [SerializeField] private BuffHandler marketBuffHandler;  // 市场趋势 Buff（每回合清空）

    /// <summary>
    /// 玩家永久 Buff 处理器
    /// </summary>
    public BuffHandler PlayerBuffHandler => playerBuffHandler;

    /// <summary>
    /// 市场趋势 Buff 处理器
    /// </summary>
    public BuffHandler MarketBuffHandler => marketBuffHandler;

    // ========== 事件（供 UI 监听） ==========

    /// <summary>
    /// 资产变化事件（转发自 PlayerRunTimeInfo）
    /// </summary>
    public event Action<long, long> OnAssetsChanged;

    /// <summary>
    /// 回合开始事件（转发自 PlayerRunTimeInfo）
    /// </summary>
    public event Action<int> OnRoundStarted;

    /// <summary>
    /// 结算完成事件（转发自 PlayerRunTimeInfo）
    /// </summary>
    public event Action<SettlementResult> OnSettlementCompleted;

    // ========== Unity 生命周期 ==========

    private void Awake()
    {
        Instance = this;
        EnsureBuffHandlers();
    }

    private void Start()
    {
        if (autoBindOnStart && PlayerRunTimeInfo.Current != null)
        {
            Bind(PlayerRunTimeInfo.Current);
        }
    }

    private void OnDestroy()
    {
        Unbind();
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 确保 BuffHandler 组件存在
    /// </summary>
    private void EnsureBuffHandlers()
    {
        if (playerBuffHandler == null)
        {
            var playerBuffGo = new GameObject("PlayerBuffHandler");
            playerBuffGo.transform.SetParent(transform);
            playerBuffHandler = playerBuffGo.AddComponent<BuffHandler>();
        }

        if (marketBuffHandler == null)
        {
            var marketBuffGo = new GameObject("MarketBuffHandler");
            marketBuffGo.transform.SetParent(transform);
            marketBuffHandler = marketBuffGo.AddComponent<BuffHandler>();
        }
    }

    // ========== 绑定管理 ==========

    /// <summary>
    /// 绑定到运行时数据
    /// </summary>
    public void Bind(PlayerRunTimeInfo info)
    {
        if (info == null)
        {
            Debug.LogWarning("[Player] 尝试绑定空的 PlayerRunTimeInfo");
            return;
        }

        // 先解绑旧的
        Unbind();

        runtimeInfo = info;

        // 订阅事件
        runtimeInfo.OnAssetsChanged += HandleAssetsChanged;
        runtimeInfo.OnRoundStarted += HandleRoundStarted;
        runtimeInfo.OnSettlementCompleted += HandleSettlementCompleted;

        Debug.Log($"[Player] 成功绑定 PlayerRunTimeInfo, 当前资产={runtimeInfo.Assets}");
    }

    /// <summary>
    /// 解绑运行时数据
    /// </summary>
    public void Unbind()
    {
        if (runtimeInfo == null) return;

        // 取消订阅事件
        runtimeInfo.OnAssetsChanged -= HandleAssetsChanged;
        runtimeInfo.OnRoundStarted -= HandleRoundStarted;
        runtimeInfo.OnSettlementCompleted -= HandleSettlementCompleted;

        runtimeInfo = null;
        Debug.Log("[Player] 已解绑 PlayerRunTimeInfo");
    }

    // ========== 属性代理（只读） ==========

    /// <summary>
    /// 当前资产
    /// </summary>
    public long Assets => runtimeInfo?.Assets ?? 0;

    /// <summary>
    /// 当前回合
    /// </summary>
    public int CurrentRound => runtimeInfo?.CurrentRound ?? 0;

    /// <summary>
    /// 是否破产
    /// </summary>
    public bool IsBankrupt => runtimeInfo?.IsBankrupt ?? false;

    /// <summary>
    /// 本回合新增节点数
    /// </summary>
    public int NodesAddedThisRound => runtimeInfo?.NodesAddedThisRound ?? 0;

    /// <summary>
    /// 本回合新增航线数
    /// </summary>
    public int EdgesAddedThisRound => runtimeInfo?.EdgesAddedThisRound ?? 0;

    /// <summary>
    /// 最近一次结算结果
    /// </summary>
    public SettlementResult LastSettlementResult => runtimeInfo?.LastSettlementResult;

    // ========== Buff 管理 ==========

    /// <summary>
    /// 添加玩家永久 Buff（通过 Addressables 名称）
    /// </summary>
    public bool AddPlayerBuff(string buffName)
    {
        if (playerBuffHandler == null) return false;
        return playerBuffHandler.AddBuff(buffName, gameObject);
    }

    /// <summary>
    /// 添加市场趋势 Buff（通过 Addressables 名称）
    /// </summary>
    public bool AddMarketBuff(string buffName)
    {
        if (marketBuffHandler == null) return false;
        return marketBuffHandler.AddBuff(buffName, gameObject);
    }

    /// <summary>
    /// 清空玩家永久 Buff
    /// </summary>
    public void ClearPlayerBuffs()
    {
        playerBuffHandler?.ClearBuff();
        Debug.Log("[Player] 已清空玩家永久 Buff");
    }

    /// <summary>
    /// 清空市场趋势 Buff（每回合开始时调用）
    /// </summary>
    public void ClearMarketBuffs()
    {
        marketBuffHandler?.ClearBuff();
        Debug.Log("[Player] 已清空市场趋势 Buff");
    }

    /// <summary>
    /// 检查是否拥有指定玩家 Buff
    /// </summary>
    public bool HasPlayerBuff(int buffId)
    {
        return playerBuffHandler?.BuffExist(buffId) ?? false;
    }

    /// <summary>
    /// 检查是否有指定市场 Buff
    /// </summary>
    public bool HasMarketBuff(int buffId)
    {
        return marketBuffHandler?.BuffExist(buffId) ?? false;
    }

    // ========== 结算 Buff 触发 ==========

    /// <summary>
    /// 计算节点收益时触发所有相关 Buff
    /// </summary>
    /// <param name="nodeLevel">节点等级</param>
    /// <param name="modifier">收益修正器</param>
    public void TriggerNodeIncomeBuffs(int nodeLevel, IncomeModifier modifier)
    {
        var callbackType = E_BuffCallBackType.OnCalculateNodeBaseIncome;
        playerBuffHandler?.TriggerCustom(callbackType, nodeLevel, modifier);
        marketBuffHandler?.TriggerCustom(callbackType, nodeLevel, modifier);
    }

    /// <summary>
    /// 计算结构倍率时触发所有相关 Buff
    /// </summary>
    /// <param name="structureType">结构类型</param>
    /// <param name="modifier">倍率修正器</param>
    public void TriggerStructureMultiplierBuffs(E_StructureType structureType, MultiplierModifier modifier)
    {
        var callbackType = E_BuffCallBackType.OnCalculateStructureMultiplier;
        playerBuffHandler?.TriggerCustom(callbackType, structureType, modifier);
        marketBuffHandler?.TriggerCustom(callbackType, structureType, modifier);
    }

    /// <summary>
    /// 计算节点成本时触发所有相关 Buff
    /// </summary>
    /// <param name="nodeLevel">节点等级</param>
    /// <param name="modifier">成本修正器</param>
    public void TriggerNodeCostBuffs(int nodeLevel, IncomeModifier modifier)
    {
        var callbackType = E_BuffCallBackType.OnCalculateNodeCost;
        playerBuffHandler?.TriggerCustom(callbackType, nodeLevel, modifier);
        marketBuffHandler?.TriggerCustom(callbackType, nodeLevel, modifier);
    }

    /// <summary>
    /// 计算航线成本时触发所有相关 Buff
    /// </summary>
    /// <param name="edgeLength">航线长度</param>
    /// <param name="modifier">成本修正器</param>
    public void TriggerEdgeCostBuffs(int edgeLength, IncomeModifier modifier)
    {
        var callbackType = E_BuffCallBackType.OnCalculateEdgeCost;
        playerBuffHandler?.TriggerCustom(callbackType, edgeLength, modifier);
        marketBuffHandler?.TriggerCustom(callbackType, edgeLength, modifier);
    }

    /// <summary>
    /// 结算完成后触发所有相关 Buff
    /// </summary>
    /// <param name="result">结算结果</param>
    public void TriggerSettlementCompleteBuffs(SettlementResult result)
    {
        var callbackType = E_BuffCallBackType.OnSettlementComplete;
        playerBuffHandler?.TriggerCustom(callbackType, result);
        marketBuffHandler?.TriggerCustom(callbackType, result);
    }

    // ========== 事件处理 ==========

    private void HandleAssetsChanged(long oldValue, long newValue)
    {
        // 转发事件
        OnAssetsChanged?.Invoke(oldValue, newValue);

        // TODO: 可以在这里添加视觉反馈，如资产变化动画
        Debug.Log($"[Player] 资产变化: {oldValue} → {newValue}");
    }

    private void HandleRoundStarted(int round)
    {
        // 清空上回合的市场 Buff
        ClearMarketBuffs();

        // 转发事件
        OnRoundStarted?.Invoke(round);

        Debug.Log($"[Player] 第 {round} 回合开始");
    }

    private void HandleSettlementCompleted(SettlementResult result)
    {
        // 转发事件
        OnSettlementCompleted?.Invoke(result);

        Debug.Log($"[Player] 结算完成: 净利润={result.NetProfit}");
    }

    // ========== 工厂方法 ==========

    /// <summary>
    /// 在场景中创建 Player 对象
    /// </summary>
    public static Player CreateInScene(PlayerRunTimeInfo info = null)
    {
        var go = new GameObject("Player");
        var player = go.AddComponent<Player>();
        player.autoBindOnStart = false; // 手动绑定

        if (info != null)
        {
            player.Bind(info);
        }
        else if (PlayerRunTimeInfo.Current != null)
        {
            player.Bind(PlayerRunTimeInfo.Current);
        }

        return player;
    }
}
