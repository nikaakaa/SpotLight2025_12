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
    public string CurrentStateName
    {
        get
        {
            var current = rootState?.CurrentSubState;
            if (current is GameLogicState gameLogic)
            {
                return $"{current.Name}/{gameLogic.CurrentSubStateName}";
            }
            return current?.Name ?? "None";
        }
    }

    /// <summary>
    /// 获取游戏逻辑状态（用于子状态转换）
    /// </summary>
    public GameLogicState GameLogicState { get; private set; }

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
        
        // ========== 顶层状态实例化 ==========
        var mainMenuState = new MainMenuState();
        var loadingGameState = new LoadingGameState();
        var pauseToMenuState = new PauseToMenuState();
        var bankruptcyState = new BankruptcyState();
        var endGameState = new EndGameState();
        var quitGameState = new QuitGameState();

        // ========== 游戏逻辑子状态实例化 ==========
        var startRoundState = new StartRoundState();
        var randomNodeState = new RandomNodeState();
        var viewRoundBuffState = new ViewRoundBuffState();
        var connectModifyRouteState = new ConnectModifyRouteState();
        var settlementState = new SettlementState();
        var purchaseBuffState = new PurchaseBuffState();

        // ========== 构建 GameLogicState（游戏循环复合状态） ==========
        GameLogicState = new GameLogicState();
        GameLogicState
            .RegisterSubState(startRoundState)
            .RegisterSubState(randomNodeState)
            .RegisterSubState(viewRoundBuffState)
            .RegisterSubState(connectModifyRouteState)
            .RegisterSubState(settlementState)
            .RegisterSubState(purchaseBuffState);
        GameLogicState.CurrentSubState = startRoundState; // 默认从 StartRound 开始

        // GameLogic 内部转换
        GameLogicState
            .RegisterTransition(new LambdaTransition<GameProcedureContext>(
                startRoundState, randomNodeState, ctx => ctx.Consume(GameEvent.Next)))
            .RegisterTransition(new LambdaTransition<GameProcedureContext>(
                randomNodeState, viewRoundBuffState, ctx => ctx.Consume(GameEvent.Next)))
            .RegisterTransition(new LambdaTransition<GameProcedureContext>(
                viewRoundBuffState, connectModifyRouteState, ctx => ctx.Consume(GameEvent.Next)))
            .RegisterTransition(new LambdaTransition<GameProcedureContext>(
                connectModifyRouteState, settlementState, ctx => ctx.Consume(GameEvent.Next)))
            .RegisterTransition(new LambdaTransition<GameProcedureContext>(
                settlementState, purchaseBuffState, ctx => ctx.Consume(GameEvent.Next)))
            .RegisterTransition(new LambdaTransition<GameProcedureContext>(
                purchaseBuffState, startRoundState, ctx => ctx.Consume(GameEvent.Next))); // 循环

        // ========== 构建根状态机 ==========
        rootState = HFSMBuilder<GameProcedureContext>.Create()
            // 注册顶层状态
            .SubState(mainMenuState, isDefault: true)
            .SubState(loadingGameState)
            .SubState(GameLogicState)
            .SubState(pauseToMenuState)
            .SubState(bankruptcyState)
            .SubState(endGameState)
            .SubState(quitGameState)

            // ===== 全局转换 =====
            .AnyTransition(pauseToMenuState, ctx => ctx.Consume(GameEvent.PauseToMenu), priority: 100)
            .Transition(pauseToMenuState, mainMenuState, ctx => ctx.Consume(GameEvent.ReturnToMenu))

            // ===== 主流程 =====
            .Transition(mainMenuState, loadingGameState, ctx => ctx.Consume(GameEvent.StartRound))
            .Transition(loadingGameState, GameLogicState, ctx => ctx.Consume(GameEvent.Next))
            .Transition(mainMenuState, quitGameState, ctx => ctx.Consume(GameEvent.Quit))

            // ===== 从 GameLogic 退出的转换 =====
            .Transition(GameLogicState, bankruptcyState, ctx => ctx.Consume(GameEvent.Bankruptcy))
            .Transition(GameLogicState, endGameState, ctx => ctx.Consume(GameEvent.EndGame))

            // ===== 破产/结束流程 =====
            .Transition(bankruptcyState, endGameState, ctx => ctx.Consume(GameEvent.Next))
            .Transition(bankruptcyState, mainMenuState, ctx => ctx.Consume(GameEvent.ReturnToMenu))
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