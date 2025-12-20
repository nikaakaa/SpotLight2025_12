using System;
using UnityEngine;

/// <summary>
/// 玩家视图层组件 - 场景中的玩家 GameObject
/// 
/// 架构重构后：
/// - 纯视图组件，不处理任何逻辑
/// - 负责监听 PlayerRunTimeInfo 事件并更新场景表现
/// - 负责播放特效、音效等
/// - Buff 系统已移至 PlayerRunTimeInfo.BuffSystem
/// </summary>
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    // 运行时数据引用
    private PlayerRunTimeInfo runTimeInfo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 尝试绑定当前数据（如果已存在）
        if (PlayerRunTimeInfo.Current != null)
        {
            Bind(PlayerRunTimeInfo.Current);
        }
    }

    private void OnDestroy()
    {
        if (runTimeInfo != null)
        {
            Unbind(runTimeInfo);
        }
    }

    /// <summary>
    /// 绑定到运行时数据
    /// </summary>
    public void Bind(PlayerRunTimeInfo info)
    {
        if (runTimeInfo != null) Unbind(runTimeInfo);

        runTimeInfo = info;

        // 绑定事件
        runTimeInfo.OnAssetsChanged += HandleAssetsChanged;
        runTimeInfo.OnRoundStarted += HandleRoundStarted;
        runTimeInfo.OnSettlementCompleted += HandleSettlementCompleted;

        // 绑定 Buff 事件 (如果需要显示 Buff 图标)
        if (runTimeInfo.PlayerBuffSystem != null)
        {
            runTimeInfo.PlayerBuffSystem.OnBuffAdded += HandlePlayerBuffAdded;
            runTimeInfo.PlayerBuffSystem.OnBuffRemoved += HandlePlayerBuffRemoved;
        }

        if (runTimeInfo.MarketBuffSystem != null)
        {
            runTimeInfo.MarketBuffSystem.OnBuffAdded += HandleMarketBuffAdded;
            runTimeInfo.MarketBuffSystem.OnBuffRemoved += HandleMarketBuffRemoved;
        }

        Debug.Log("[Player] 已绑定到运行时数据");
    }

    /// <summary>
    /// 解绑运行时数据
    /// </summary>
    public void Unbind(PlayerRunTimeInfo info)
    {
        if (info == null) return;

        // 解绑事件
        info.OnAssetsChanged -= HandleAssetsChanged;
        info.OnRoundStarted -= HandleRoundStarted;
        info.OnSettlementCompleted -= HandleSettlementCompleted;

        if (info.PlayerBuffSystem != null)
        {
            info.PlayerBuffSystem.OnBuffAdded -= HandlePlayerBuffAdded;
            info.PlayerBuffSystem.OnBuffRemoved -= HandlePlayerBuffRemoved; // 需在 BuffSystem 中实现此事件
        }

        if (info.MarketBuffSystem != null)
        {
            info.MarketBuffSystem.OnBuffAdded -= HandleMarketBuffAdded;
            info.MarketBuffSystem.OnBuffRemoved -= HandleMarketBuffRemoved;
        }

        runTimeInfo = null;
        Debug.Log("[Player] 已解绑运行时数据");
    }

    // ========== 事件处理 ==========

    private void HandleAssetsChanged(long oldVal, long newVal)
    {
        // 可以在这里播放金币获得/减少的声效或特效
        // UI 更新通常由 UI 面板自己监听，这里是场景中的表现
    }

    private void HandleRoundStarted(int round)
    {
        // 播放回合开始音效
    }

    private void HandleSettlementCompleted(SettlementResult result)
    {
        // 播放结算完成特效
    }

    private void HandlePlayerBuffAdded(BuffInfo buff)
    {
        // 显示获得 Buff 的特效
        Debug.Log($"[PlayerView] 获得玩家 Buff: {buff.buffData.buffName}");
    }

    private void HandlePlayerBuffRemoved(BuffInfo buff)
    {
        Debug.Log($"[PlayerView] 失去玩家 Buff: {buff.buffData.buffName}");
    }

    private void HandleMarketBuffAdded(BuffInfo buff)
    {
        Debug.Log($"[PlayerView] 市场趋势生效: {buff.buffData.buffName}");
    }

    private void HandleMarketBuffRemoved(BuffInfo buff)
    {
        Debug.Log($"[PlayerView] 市场趋势结束: {buff.buffData.buffName}");
    }

    // ========== 静态工厂 ==========

    /// <summary>
    /// 在场景中创建 Player 对象
    /// </summary>
    public static Player CreateInScene()
    {
        if (Instance != null) return Instance;

        // string prefabPath = "Player"; // 假设 Resources 目录下有
        // 简化：直接创建空对象挂载
        GameObject go = new GameObject("Player");
        Player player = go.AddComponent<Player>();

        // 如果有美术资源，后续可以在这里加载

        return player;
    }
}
