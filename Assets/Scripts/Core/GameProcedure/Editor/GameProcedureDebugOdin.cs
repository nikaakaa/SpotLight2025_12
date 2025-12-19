using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 游戏流程调试面板（Odin Editor Window 版本）
/// 功能：
/// 1. 实时显示当前状态
/// 2. 显示当前状态可以转换到的所有目标状态
/// 3. 一键触发状态转换
/// </summary>
public class GameProcedureDebugOdin : OdinEditorWindow
{
    [MenuItem("Tools/GameProcedure/流程调试面板")]
    private static void OpenWindow()
    {
        var window = GetWindow<GameProcedureDebugOdin>();
        window.titleContent = new GUIContent("流程调试", EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow").image);
        window.Show();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        Repaint();
    }

    protected override void OnImGUI()
    {
        // 持续刷新以显示最新状态
        if (EditorApplication.isPlaying)
        {
            Repaint();
        }
        base.OnImGUI();
    }

    #region 状态显示

    [TitleGroup("状态监控", "实时显示当前游戏流程状态")]
    [ShowInInspector, ReadOnly, LabelText("当前状态"), PropertyOrder(-10)]
    [GUIColor("GetStateColor")]
    public string CurrentState => GetCurrentState();

    [ShowInInspector, ReadOnly, LabelText("待处理事件"), PropertyOrder(-9)]
    public string PendingEvent => GetPendingEvent();

    private string GetCurrentState()
    {
        if (!EditorApplication.isPlaying) return "未运行";
        return GameProcedure.Instance?.CurrentStateName ?? "未初始化";
    }

    private string GetPendingEvent()
    {
        if (!EditorApplication.isPlaying) return "-";
        return GameProcedure.Instance?.Context?.PendingEvent.ToString() ?? "None";
    }

    private Color GetStateColor()
    {
        var state = CurrentState;
        return state switch
        {
            "未运行" => new Color(0.6f, 0.6f, 0.6f),
            "未初始化" => new Color(0.6f, 0.6f, 0.6f),
            "MainMenuState" => new Color(0.3f, 0.8f, 0.3f),      // 绿色 - 主菜单
            "StartRoundState" => new Color(0.3f, 0.6f, 1f),     // 蓝色 - 开始回合
            "RandomNodeState" => new Color(1f, 0.8f, 0.3f),     // 橙色 - 随机节点
            "SettlementState" => new Color(0.9f, 0.5f, 0.5f),   // 红色 - 结算
            "Bankrupt" => new Color(1f, 0.2f, 0.2f),            // 深红 - 破产
            "EndGame" => new Color(0.5f, 0.5f, 0.5f),           // 灰色 - 结束
            _ => Color.white
        };
    }

    #endregion

    #region 快速事件触发

    [TitleGroup("事件触发", "发送事件来触发状态转换")]
    
    [Button("🚀 开始游戏", ButtonSizes.Large), GUIColor(0.2f, 0.9f, 0.4f)]
    [EnableIf("IsPlaying")]
    [PropertyOrder(-1)]
    private void TriggerStartGame() => SendEvent(GameEvent.StartRound);

    [HorizontalGroup("事件触发/Row1", Width = 0.33f)]
    [Button("▶ Next", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
    [EnableIf("IsPlaying")]
    private void TriggerNext() => SendEvent(GameEvent.Next);

    [HorizontalGroup("事件触发/Row1", Width = 0.33f)]
    [Button("🏠 返回菜单", ButtonSizes.Large), GUIColor(0.8f, 0.8f, 0.4f)]
    [EnableIf("IsPlaying")]
    private void TriggerReturnToMenu() => SendEvent(GameEvent.ReturnToMenu);

    [HorizontalGroup("事件触发/Row1", Width = 0.33f)]
    [Button("⏸ 暂停到菜单", ButtonSizes.Medium), GUIColor(0.8f, 0.6f, 0.3f)]
    [EnableIf("IsPlaying")]
    private void TriggerPauseToMenu() => SendEvent(GameEvent.PauseToMenu);

    [HorizontalGroup("事件触发/Row2", Width = 0.33f)]
    [Button("💸 破产", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
    [EnableIf("IsPlaying")]
    private void TriggerBankrupt() => SendEvent(GameEvent.Bankruptcy);

    [HorizontalGroup("事件触发/Row2", Width = 0.33f)]
    [Button("🏁 结束游戏", ButtonSizes.Medium), GUIColor(0.7f, 0.5f, 0.8f)]
    [EnableIf("IsPlaying")]
    private void TriggerEndGame() => SendEvent(GameEvent.EndGame);

    [HorizontalGroup("事件触发/Row2", Width = 0.33f)]
    [Button("❌ 退出游戏", ButtonSizes.Medium), GUIColor(0.6f, 0.6f, 0.6f)]
    [EnableIf("IsPlaying")]
    private void TriggerQuit() => SendEvent(GameEvent.Quit);

    private bool IsPlaying => EditorApplication.isPlaying;

    #endregion

    #region 可用转换

    [TitleGroup("可用转换", "从当前状态可以转换到的目标状态")]
    [ShowInInspector, ReadOnly, LabelText("可用目标状态")]
    [ListDrawerSettings(ShowFoldout = false, DraggableItems = false)]
    public List<string> AvailableTransitions => GetAvailableTransitions();

    private List<string> GetAvailableTransitions()
    {
        var transitions = new List<string>();
        if (!EditorApplication.isPlaying || GameProcedure.Instance == null) 
            return transitions;

        var currentState = CurrentState;

        // 根据当前状态返回可能的转换目标
        // 注意：层级状态显示为 "GameLogicState/SubState"
        transitions = currentState switch
        {
            "MainMenuState" => new List<string> { "LoadingGameState (StartRound)", "QuitGameState (Quit)" },
            "LoadingGameState" => new List<string> { "GameLogicState (Next) [自动]" },
            "PauseToMenuState" => new List<string> { "MainMenuState (ReturnToMenu)" },
            // GameLogic 内部状态
            "GameLogicState/StartRoundState" => new List<string> { "RandomNodeState (Next)" },
            "GameLogicState/RandomNodeState" => new List<string> { "ViewRoundBuffState (Next)" },
            "GameLogicState/ViewRoundBuffState" => new List<string> { "ConnectModifyRouteState (Next)" },
            "GameLogicState/ConnectModifyRouteState" => new List<string> { "SettlementState (Next)" },
            "GameLogicState/SettlementState" => new List<string> { "PurchaseBuffState (Next)", "Bankruptcy (Bankruptcy)", "EndGame (EndGame)" },
            "GameLogicState/PurchaseBuffState" => new List<string> { "StartRoundState (Next) [循环]" },
            "BankruptcyState" => new List<string> { "EndGameState (Next)" },
            "EndGameState" => new List<string> { "MainMenuState (ReturnToMenu)" },
            _ => new List<string>()
        };

        // 全局转换（任何状态都可以触发）
        if (!currentState.Contains("PauseToMenu") && !currentState.Contains("MainMenu"))
        {
            transitions.Add("PauseToMenuState (PauseToMenu) [全局]");
        }

        return transitions;
    }

    #endregion

    #region 强制跳转

    [TitleGroup("强制跳转", "跳过转换条件，直接切换到指定状态（仅调试用）")]
    [ShowInInspector, LabelText("目标状态")]
    [ValueDropdown("GetAllStateNames")]
    private string forceGoToTarget = "MainMenuState";

    [Button("强制跳转", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.2f)]
    [EnableIf("IsPlaying")]
    private void ForceGoToState()
    {
        if (string.IsNullOrEmpty(forceGoToTarget)) return;
        GameProcedure.Instance?.Context?.ForceGoTo(forceGoToTarget);
    }

    private IEnumerable<string> GetAllStateNames()
    {
        return new List<string>
        {
            // 顶层状态
            "MainMenuState",
            "LoadingGameState",
            "GameLogicState",
            "PauseToMenuState",
            "BankruptcyState",
            "EndGameState",
            "QuitGameState",
            // GameLogic 子状态（需要通过 GameLogicState 跳转）
            "StartRoundState",
            "RandomNodeState",
            "ViewRoundBuffState",
            "ConnectModifyRouteState",
            "SettlementState",
            "PurchaseBuffState"
        };
    }

    #endregion

    #region 业务数据

    [TitleGroup("业务数据", "游戏流程上下文中的业务数据")]
    [ShowInInspector, LabelText("当前回合")]
    [EnableIf("IsPlaying")]
    public int CurrentRound
    {
        get => GameProcedure.Instance?.Context?.CurrentRound ?? 0;
        set { if (GameProcedure.Instance?.Context != null) GameProcedure.Instance.Context.CurrentRound = value; }
    }

    [ShowInInspector, LabelText("玩家金钱")]
    [EnableIf("IsPlaying")]
    public int PlayerMoney
    {
        get => GameProcedure.Instance?.Context?.PlayerMoney ?? 0;
        set { if (GameProcedure.Instance?.Context != null) GameProcedure.Instance.Context.PlayerMoney = value; }
    }

    #endregion

    #region 流程图预览

    [TitleGroup("流程图", "游戏流程状态转换图")]
    [ShowInInspector, HideLabel]
    [DisplayAsString(false)]
    [PropertyOrder(100)]
    public string FlowChart => @"
┌─────────────┐
│  MainMenu   │ ◀────────────────────────────────────────┐
└──────┬──────┘                                          │
       │ StartRound                                      │ ReturnToMenu
       ▼                                                 │
┌─────────────┐                                          │
│ LoadingGame │ ← 加载场景                                │
└──────┬──────┘                                          │
       │ Next (自动)                                      │
       ▼                                                 │
┌════════════════════════════════════════════════════┐   │
║              GameLogicState (复合状态)              ║   │
║  ┌───────────┐                                     ║   │
║  │StartRound │◀─────────────────┐                  ║   │
║  └─────┬─────┘                  │ Next (循环)      ║   │
║        │ Next                   │                  ║   │
║        ▼                        │                  ║   │
║  ┌───────────┐           ┌──────┴───────┐          ║   │
║  │RandomNode │           │ PurchaseBuff │          ║   │
║  └─────┬─────┘           └──────▲───────┘          ║   │
║        │ Next                   │ Next             ║   │
║        ▼                        │                  ║   │
║  ┌─────────────┐          ┌─────┴──────┐           ║   │
║  │ViewRoundBuff│          │ Settlement │──┬──────────▶ Bankruptcy/EndGame
║  └─────┬───────┘          └─────▲──────┘  │        ║   │
║        │ Next                   │         │        ║   │
║        ▼                        │         │        ║   │
║  ┌───────────────────┐          │         │        ║   │
║  │ConnectModifyRoute │──────────┘         │        ║   │
║  └───────────────────┘   Next             │        ║   │
╚════════════════════════════════════════════════════╝   │
                                                         │
       ┌──────────┐      ┌──────────┐                    │
       │Bankruptcy│─Next→│ EndGame  │────────────────────┘
       └──────────┘      └──────────┘

结算退出: Bankruptcy→破产 | EndGame→结束（跳出 GameLogicState）
※ 任意状态可通过 PauseToMenu 事件暂停到菜单
";

    #endregion

    #region 辅助方法

    private void SendEvent(GameEvent evt)
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[GameProcedureDebugOdin] 请先运行游戏");
            return;
        }
        if (GameProcedure.Instance?.Context == null)
        {
            Debug.LogWarning("[GameProcedureDebugOdin] GameProcedure 未初始化");
            return;
        }
        GameProcedure.Instance.Context.Send(evt);
    }

    #endregion
}
