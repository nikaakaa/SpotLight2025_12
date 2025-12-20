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

    #region Buff 调试

    [TitleGroup("Buff 系统调试", "查看和管理激活的 Buff")]

    // ========== 系统状态 ==========
    [BoxGroup("Buff 系统调试/系统状态")]
    [ShowInInspector, ReadOnly, LabelText("Player.Instance")]
    [GUIColor("GetPlayerInstanceColor")]
    public string PlayerInstanceStatus => Player.Instance != null ? "✓ 存在" : "✗ NULL";

    [BoxGroup("Buff 系统调试/系统状态")]
    [ShowInInspector, ReadOnly, LabelText("PlayerBuffSystem")]
    public string PlayerBuffSystemStatus => GetBuffSystemStatus(PlayerRunTimeInfo.Current?.PlayerBuffSystem);

    [BoxGroup("Buff 系统调试/系统状态")]
    [ShowInInspector, ReadOnly, LabelText("MarketBuffSystem")]
    public string MarketBuffSystemStatus => GetBuffSystemStatus(PlayerRunTimeInfo.Current?.MarketBuffSystem);

    [BoxGroup("Buff 系统调试/系统状态")]
    [ShowInInspector, ReadOnly, LabelText("BuffRegistry 状态")]
    public string BuffRegistryStatus => $"已注册 {GetRegisteredBuffCount()} 个 Buff";

    private Color GetPlayerInstanceColor() => Player.Instance != null ? Color.green : Color.red;

    private string GetBuffSystemStatus(BuffSystem system)
    {
        if (system == null) return "✗ NULL";
        return $"✓ 存在 ({system.BuffInfoList?.Count ?? 0} 个 Buff)";
    }

    private int GetRegisteredBuffCount()
    {
        try { return System.Linq.Enumerable.Count(BuffRegistry.GetAllBuffs()); }
        catch { return 0; }
    }

    // ========== 激活的 Buff 列表 ==========
    [BoxGroup("Buff 系统调试/已激活 Buff")]
    [ShowInInspector, ReadOnly, LabelText("玩家 Buff")]
    [ListDrawerSettings(ShowFoldout = true, DraggableItems = false)]
    public List<string> ActivePlayerBuffs => GetActiveBuffList(PlayerRunTimeInfo.Current?.PlayerBuffSystem);

    [BoxGroup("Buff 系统调试/已激活 Buff")]
    [ShowInInspector, ReadOnly, LabelText("市场 Buff")]
    [ListDrawerSettings(ShowFoldout = true, DraggableItems = false)]
    public List<string> ActiveMarketBuffs => GetActiveBuffList(PlayerRunTimeInfo.Current?.MarketBuffSystem);

    private List<string> GetActiveBuffList(BuffSystem system)
    {
        var list = new List<string>();
        if (system?.BuffInfoList == null) return list;
        foreach (var buff in system.BuffInfoList)
        {
            list.Add($"[{buff.buffData.id}] {buff.buffData.buffName}: {buff.buffData.description}");
        }
        if (list.Count == 0) list.Add("(无)");
        return list;
    }

    // ========== 快速添加 Buff ==========
    [BoxGroup("Buff 系统调试/快速添加")]
    [ShowInInspector, LabelText("选择 Buff")]
    [ValueDropdown("GetAllBuffOptions")]
    public int SelectedBuffId = 101;

    private IEnumerable<ValueDropdownItem<int>> GetAllBuffOptions()
    {
        var options = new List<ValueDropdownItem<int>>();

        // 玩家 Buff
        options.Add(new ValueDropdownItem<int>("=== 玩家 Buff ===", 0));
        options.Add(new ValueDropdownItem<int>("[101] lv1收益+3", 101));
        options.Add(new ValueDropdownItem<int>("[102] lv2收益+8", 102));
        options.Add(new ValueDropdownItem<int>("[103] lv3收益+15", 103));
        options.Add(new ValueDropdownItem<int>("[104] lv4收益+25", 104));
        options.Add(new ValueDropdownItem<int>("[105] lv5收益+35", 105));
        options.Add(new ValueDropdownItem<int>("[106] 环倍率+0.2", 106));
        options.Add(new ValueDropdownItem<int>("[107] 单线倍率+0.1", 107));
        options.Add(new ValueDropdownItem<int>("[108] 放射倍率+0.1", 108));

        // 市场 Buff
        options.Add(new ValueDropdownItem<int>("=== 市场 Buff ===", 0));
        options.Add(new ValueDropdownItem<int>("[1] 所有节点收益×0.9", 1));
        options.Add(new ValueDropdownItem<int>("[2] 所有结构倍率×0.9", 2));
        options.Add(new ValueDropdownItem<int>("[6] lv1节点收益×0.85", 6));
        options.Add(new ValueDropdownItem<int>("[20] 小型复苏(lv1-2×1.3)", 20));

        return options;
    }

    [BoxGroup("Buff 系统调试/快速添加")]
    [HorizontalGroup("Buff 系统调试/快速添加/Buttons")]
    [Button("添加到玩家 Buff", ButtonSizes.Medium), GUIColor(0.4f, 0.9f, 0.4f)]
    [EnableIf("IsPlaying")]
    private void AddToPlayerBuff()
    {
        if (SelectedBuffId <= 0) { Debug.LogWarning("请选择有效的 Buff"); return; }
        var system = PlayerRunTimeInfo.Current?.PlayerBuffSystem;
        if (system == null) { Debug.LogError("PlayerBuffSystem 不存在"); return; }

        bool result = system.AddBuffById(SelectedBuffId);
        Debug.Log($"[BuffDebug] 添加玩家 Buff ID={SelectedBuffId}: {(result ? "成功" : "失败")}");
    }

    [HorizontalGroup("Buff 系统调试/快速添加/Buttons")]
    [Button("添加到市场 Buff", ButtonSizes.Medium), GUIColor(0.4f, 0.7f, 1f)]
    [EnableIf("IsPlaying")]
    private void AddToMarketBuff()
    {
        if (SelectedBuffId <= 0) { Debug.LogWarning("请选择有效的 Buff"); return; }
        var system = PlayerRunTimeInfo.Current?.MarketBuffSystem;
        if (system == null) { Debug.LogError("MarketBuffSystem 不存在"); return; }

        bool result = system.AddBuffById(SelectedBuffId);
        Debug.Log($"[BuffDebug] 添加市场 Buff ID={SelectedBuffId}: {(result ? "成功" : "失败")}");
    }

    // ========== 清空 Buff ==========
    [BoxGroup("Buff 系统调试/管理")]
    [HorizontalGroup("Buff 系统调试/管理/ClearRow")]
    [Button("清空玩家 Buff", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.4f)]
    [EnableIf("IsPlaying")]
    private void ClearPlayerBuffs()
    {
        PlayerRunTimeInfo.Current?.PlayerBuffSystem?.ClearBuff();
        Debug.Log("[BuffDebug] 已清空玩家 Buff");
    }

    [HorizontalGroup("Buff 系统调试/管理/ClearRow")]
    [Button("清空市场 Buff", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.4f)]
    [EnableIf("IsPlaying")]
    private void ClearMarketBuffs()
    {
        PlayerRunTimeInfo.Current?.MarketBuffSystem?.ClearBuff();
        Debug.Log("[BuffDebug] 已清空市场 Buff");
    }

    // ========== 批量添加 Buff ==========
    [BoxGroup("Buff 系统调试/管理")]
    [HorizontalGroup("Buff 系统调试/管理/AddAllRow")]
    [Button("🎁 添加所有玩家 Buff", ButtonSizes.Large), GUIColor(0.3f, 1f, 0.5f)]
    [EnableIf("IsPlaying")]
    private void AddAllPlayerBuffs()
    {
        var system = PlayerRunTimeInfo.Current?.PlayerBuffSystem;
        if (system == null)
        {
            Debug.LogError("PlayerBuffSystem 不存在");
            return;
        }

        // 玩家 Buff ID: 101-112
        int[] playerBuffIds = { 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112 };
        int successCount = 0;

        foreach (var id in playerBuffIds)
        {
            if (system.AddBuffById(id))
                successCount++;
        }

        Debug.Log($"[BuffDebug] 批量添加玩家 Buff: {successCount}/{playerBuffIds.Length} 成功");
    }

    [HorizontalGroup("Buff 系统调试/管理/AddAllRow")]
    [Button("🌍 添加所有市场 Buff", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 1f)]
    [EnableIf("IsPlaying")]
    private void AddAllMarketBuffs()
    {
        var system = PlayerRunTimeInfo.Current?.MarketBuffSystem;
        if (system == null)
        {
            Debug.LogError("MarketBuffSystem 不存在");
            return;
        }

        // 市场 Buff ID: 1-21
        int successCount = 0;
        for (int id = 1; id <= 21; id++)
        {
            if (system.AddBuffById(id))
                successCount++;
        }

        Debug.Log($"[BuffDebug] 批量添加市场 Buff: {successCount}/21 成功");
    }

    [BoxGroup("Buff 系统调试/管理")]
    [Button("🔥 添加所有 Buff (玩家+市场)", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.2f)]
    [EnableIf("IsPlaying")]
    private void AddAllBuffs()
    {
        AddAllPlayerBuffs();
        AddAllMarketBuffs();
        Debug.Log("[BuffDebug] 已添加所有 Buff (玩家+市场)");
    }

    // ========== 调试工具 ==========
    [BoxGroup("Buff 系统调试/调试工具")]
    [Button("🔍 检查 Buff 系统状态", ButtonSizes.Large), GUIColor(0.8f, 0.8f, 1f)]
    [EnableIf("IsPlaying")]
    private void DiagnoseBuffSystem()
    {
        Debug.Log("========== Buff 系统诊断 ==========");

        // 1. BuffRegistry 检查
        BuffRegistry.Initialize();
        int regCount = GetRegisteredBuffCount();
        Debug.Log($"[诊断] BuffRegistry: {regCount} 个 Buff 已注册");

        // 2. Player.Instance 检查
        if (Player.Instance == null)
        {
            Debug.LogError("[诊断] ✗ Player.Instance 为 NULL！");
            return;
        }
        Debug.Log("[诊断] ✓ Player.Instance 存在");

        // 3. BuffSystem 检查
        var playerSystem = PlayerRunTimeInfo.Current?.PlayerBuffSystem;
        var marketSystem = PlayerRunTimeInfo.Current?.MarketBuffSystem;

        Debug.Log($"[诊断] PlayerBuffSystem: {(playerSystem != null ? "存在" : "NULL")}");
        Debug.Log($"[诊断] MarketBuffSystem: {(marketSystem != null ? "存在" : "NULL")}");

        // 4. 激活的 Buff 检查
        if (playerSystem != null)
        {
            Debug.Log($"[诊断] 玩家 Buff 数量: {playerSystem.BuffInfoList?.Count ?? 0}");
            foreach (var buff in playerSystem.BuffInfoList)
            {
                Debug.Log($"[诊断]   - ID={buff.buffData.id}, Name={buff.buffData.buffName}");
                Debug.Log($"[诊断]     Effects: {buff.buffData.Effects.Count} 个回调类型");
                foreach (var kvp in buff.buffData.Effects)
                {
                    Debug.Log($"[诊断]       - {kvp.Key}: {(kvp.Value != null ? "已注册" : "NULL")}");
                }
            }
        }

        // 5. PlayerRunTimeInfo 检查
        var playerInfo = PlayerRunTimeInfo.Current;
        if (playerInfo != null)
        {
            Debug.Log($"[诊断] PlayerRunTimeInfo.OwnedPlayerBuffIds: {playerInfo.OwnedPlayerBuffIds.Count} 个");
            foreach (var id in playerInfo.OwnedPlayerBuffIds)
            {
                Debug.Log($"[诊断]   - ID={id}");
            }
        }

        Debug.Log("========== 诊断完成 ==========");
    }

    [BoxGroup("Buff 系统调试/调试工具")]
    [Button("🔄 初始化 BuffRegistry", ButtonSizes.Medium), GUIColor(0.6f, 0.8f, 0.6f)]
    private void InitBuffRegistry()
    {
        BuffRegistry.Initialize();
        Debug.Log("[BuffDebug] BuffRegistry 已初始化");
    }

    [BoxGroup("Buff 系统调试/调试工具")]
    [Button("📊 测试 Buff 效果触发", ButtonSizes.Medium), GUIColor(0.8f, 0.6f, 1f)]
    [EnableIf("IsPlaying")]
    private void TestBuffTrigger()
    {
        Debug.Log("========== 测试 Buff 触发 ==========");

        var modifier = new IncomeModifier();
        Debug.Log($"[测试] 初始 modifier: FlatBonus={modifier.FlatBonus}, Multiplier={modifier.Multiplier}");

        // 模拟触发 lv1 节点收益计算
        if (PlayerRunTimeInfo.Current != null)
        {
            // 使用 IGameplayBuffSystem 接口触发
            PlayerRunTimeInfo.Current.TriggerNodeIncomeBuffs(1, modifier);
            Debug.Log($"[测试] 触发后 modifier: FlatBonus={modifier.FlatBonus}, Multiplier={modifier.Multiplier}");

            if (modifier.FlatBonus == 0 && modifier.Multiplier == 1f)
            {
                Debug.LogWarning("[测试] Buff 未生效！检查 PlayerBuffSystem 中是否有激活的 Buff");
            }
            else
            {
                Debug.Log("[测试] ✓ Buff 触发成功！");
                if (modifier.AppliedBuffIds.Count > 0)
                {
                    Debug.Log($"[测试] 生效 Buff ID: {string.Join(", ", modifier.AppliedBuffIds)}");
                }
            }
        }
        else
        {
            Debug.LogError("[测试] PlayerRunTimeInfo.Current 为 NULL");
        }
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
