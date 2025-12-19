using System;
using UnityEngine;

public class GameProcedure:MonoBehaviour
{
    private static GameProcedure instance;
    public static GameProcedure Instance => instance;
    private ComposeState<GameProcedureContext> rootState;
    private GameProcedureContext context;

    /// <summary>
    /// 获取流程上下文，用于触发状态转换
    /// </summary>
    public GameProcedureContext Context => context;

    /// <summary>
    /// 获取当前状态名称
    /// </summary>
    public string CurrentStateName => rootState?.CurrentSubState?.Name ?? "None";

    void Awake()
    {
        // 单例保护：防止场景切换时重复创建
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // 跨场景保持流程状态
        
        // ========== 状态实例化 ==========
        var mainMenuState = new MainMenuState();
        var loadingGameState = new LoadingGameState();
        var startRoundState = new StartRoundState();
        var randomNodeState = new RandomNodeState();
        var viewRoundBuffState = new ViewRoundBuffState();
        var connectModifyRouteState = new ConnectModifyRouteState();
        var settlementState = new SettlementState();
        var purchaseBuffState = new PurchaseBuffState();
        var pauseToMenuState = new PauseToMenuState();
        var bankruptcyState = new BankruptcyState();
        var endGameState = new EndGameState();
        var quitGameState = new QuitGameState();

        // ========== 构建状态机（直接在 root 层注册状态） ==========
        rootState = HFSMBuilder<GameProcedureContext>.Create()
            // 注册所有状态（直接挂在 root 下，不创建额外的 Compose 层）
            .SubState(mainMenuState, isDefault: true)
            .SubState(pauseToMenuState)
            .SubState(loadingGameState)
            .SubState(startRoundState)
            .SubState(randomNodeState)
            .SubState(viewRoundBuffState)
            .SubState(connectModifyRouteState)
            .SubState(settlementState)
            .SubState(purchaseBuffState)
            .SubState(bankruptcyState)
            .SubState(endGameState)
            .SubState(quitGameState)

            // ===== 全局转换 =====
            .AnyTransition(pauseToMenuState, ctx => ctx.Consume(GameEvent.PauseToMenu), priority: 100)
            .Transition(pauseToMenuState, mainMenuState, ctx => ctx.Consume(GameEvent.ReturnToMenu))

            // ===== 主流程 =====
            .Transition(mainMenuState, loadingGameState, ctx => ctx.Consume(GameEvent.StartRound))
            .Transition(loadingGameState, startRoundState, ctx => ctx.Consume(GameEvent.Next))
            .Transition(mainMenuState, quitGameState, ctx => ctx.Consume(GameEvent.Quit))

            // ===== 回合流程 =====
            .Transition(startRoundState, randomNodeState, ctx => ctx.Consume(GameEvent.Next))
            .Transition(randomNodeState, viewRoundBuffState, ctx => ctx.Consume(GameEvent.Next))
            .Transition(viewRoundBuffState, connectModifyRouteState, ctx => ctx.Consume(GameEvent.Next))
            .Transition(connectModifyRouteState, settlementState, ctx => ctx.Consume(GameEvent.Next))

            // ===== 结算分支（三个出口） =====
            .Transition(settlementState, purchaseBuffState, ctx => ctx.Consume(GameEvent.Next))         // 正常继续
            .Transition(settlementState, bankruptcyState, ctx => ctx.Consume(GameEvent.Bankruptcy))     // 破产
            .Transition(settlementState, endGameState, ctx => ctx.Consume(GameEvent.EndGame))           // 直接结束游戏

            // ===== 循环回合 =====
            .Transition(purchaseBuffState, startRoundState, ctx => ctx.Consume(GameEvent.Next))

            // ===== 破产/结束流程 =====
            .Transition(bankruptcyState, endGameState, ctx => ctx.Consume(GameEvent.Next))
            .Transition(endGameState, mainMenuState, ctx => ctx.Consume(GameEvent.ReturnToMenu))
            .Build();

        // 初始化 Context，传入 rootState 用于强制跳转
        context = new GameProcedureContext(rootState);
        rootState.Enter(context);
    }

    void Update()
    {
        rootState?.Update(context);
    }
}

/// <summary>
/// 游戏流程事件枚举 - 统一管理所有状态转换触发器
/// </summary>
public enum GameEvent
{
    None,
    // 通用
    Next,           // 推进到下一状态（顺序流程）
    Back,           // 返回上一状态

    // 菜单相关
    StartRound,     // 开始回合
    ReturnToMenu,   // 返回主菜单
    PauseToMenu,    // 暂停并返回菜单
    Quit,           // 退出游戏

    // 结算分支
    Bankruptcy,     // 破产
    EndGame,        // 结束游戏（胜利或主动结束）
}

/// <summary>
/// 游戏流程上下文 - 提供灵活的流程控制 API
/// </summary>
public class GameProcedureContext
{
    private readonly ComposeState<GameProcedureContext> rootCompose;
    private GameEvent pendingEvent = GameEvent.None;

    // ========== 业务数据（与状态转换逻辑分离） ==========
    public int CurrentRound { get; set; } = 1;
    public int PlayerMoney { get; set; } = 1000;

    public GameProcedureContext(ComposeState<GameProcedureContext> rootCompose)
    {
        this.rootCompose = rootCompose;
    }

    // ========== 流程控制 API ==========

    /// <summary>
    /// 发送事件请求状态转换（事件驱动方式）
    /// </summary>
    public void Send(GameEvent evt)
    {
        if (pendingEvent != GameEvent.None)
        {
            Debug.LogWarning($"[Context] 覆盖未消费的事件: {pendingEvent} -> {evt}");
        }
        pendingEvent = evt;
        Debug.Log($"<color=cyan>[Context] 发送事件: {evt}</color>");
    }

    /// <summary>
    /// 检查并消费指定事件（供 Transition 条件使用）
    /// </summary>
    public bool Consume(GameEvent expected)
    {
        if (pendingEvent != expected) return false;
        pendingEvent = GameEvent.None;
        return true;
    }

    /// <summary>
    /// 强制跳转到指定状态（命令式方式，忽略转换条件）
    /// 适用于调试或特殊流程需求
    /// </summary>
    public void ForceGoTo(string stateName)
    {
        if (rootCompose.subStates.TryGetValue(stateName, out var targetState))
        {
            Debug.Log($"<color=yellow>[Context] 强制跳转: {rootCompose.CurrentSubState?.Name} -> {stateName}</color>");
            rootCompose.ChangeToTheSubState(targetState, this);
        }
        else
        {
            Debug.LogError($"[Context] 找不到状态: {stateName}");
        }
    }

    /// <summary>
    /// 便捷方法：推进到下一状态
    /// </summary>
    public void Next() => Send(GameEvent.Next);

    /// <summary>
    /// 便捷方法：返回主菜单
    /// </summary>
    public void ReturnToMenu() => Send(GameEvent.ReturnToMenu);

    /// <summary>
    /// 便捷方法：暂停并返回菜单
    /// </summary>
    public void PauseToMenu() => Send(GameEvent.PauseToMenu);

    /// <summary>
    /// 获取当前待处理的事件（调试用）
    /// </summary>
    public GameEvent PendingEvent => pendingEvent;
}