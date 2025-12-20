using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class GameToolsWindow : OdinMenuEditorWindow
{
    [MenuItem("Tools/Game Tools")]
    private static void OpenWindow()
    {
        var window = GetWindow<GameToolsWindow>();
        window.titleContent = new GUIContent("Game Tools");
        window.Show();
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.Config.DrawSearchToolbar = true;
        tree.Selection.SupportsMultiSelect = false;

        tree.Add("Overview", new OverviewViewModel());
        tree.Add("Core/Game Procedure", new GameProcedureDebugViewModel());
        tree.Add("Core/Player Info", new PlayerInfoDebugViewModel());
        tree.Add("Systems/Buff Debugger", new BuffDebugViewModel());

        // 仅在运行时且相关系统存在时显示
        if (GameProcedure.Instance != null && GameProcedure.Instance.GameLogicState != null)
        {
            tree.Add("Systems/Aviation System", new AviationSystemDebugViewModel());
        }

        return tree;
    }

    protected override void OnImGUI()
    {
        base.OnImGUI();
        // 运行时实时刷新，但在编译时禁止刷新以防崩溃
        if (Application.isPlaying && !EditorApplication.isCompiling)
        {
            Repaint();
        }
    }
}

// === View Models ===

public class OverviewViewModel
{
    [Title("Welcome to SpotLight Tools")]
    [InfoBox("此工具箱整合了游戏的所有调试与测试功能。\n请在左侧菜单选择相应模块。")]
    [ShowInInspector, DisplayAsString, HideLabel, PropertySpace(20)]
    public string Status => Application.isPlaying ? "Game is Running" : "Editor Mode";
}

public class GameProcedureDebugViewModel
{
    [Title("State Machine")]
    [ShowInInspector, DisplayAsString, LabelText("Top State")]
    public string CurrentStateInfo
    {
        get
        {
            if (GameProcedure.Instance == null) return "No Instance";
            if (GameProcedure.Instance.Context == null) return "No Context";
            return GameProcedure.Instance.CurrentStateName;
        }
    }

    [Button(ButtonSizes.Large), GUIColor(0.5f, 1f, 0.5f)]
    public void TriggerEventNext()
    {
        if (GameProcedure.Instance != null && GameProcedure.Instance.Context != null)
        {
            GameProcedure.Instance.Context.Send(GameEvent.Next);
            Debug.Log("[Debugger] Sent GameEvent.Next");
        }
    }

    [Button(ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
    public void TriggerEventBankruptcy()
    {
        if (GameProcedure.Instance != null && GameProcedure.Instance.Context != null)
        {
            GameProcedure.Instance.Context.Send(GameEvent.Bankruptcy);
            Debug.Log("[Debugger] Sent GameEvent.Bankruptcy");
        }
    }
}

public class PlayerInfoDebugViewModel
{
    [ShowInInspector, HideReferenceObjectPicker]
    [InlineProperty(LabelWidth = 100)]
    public PlayerRunTimeInfo RuntimeInfo => PlayerRunTimeInfo.Current;

    [Button]
    public void AddMoney(long amount = 1000000)
    {
        if (PlayerRunTimeInfo.Current != null)
        {
            PlayerRunTimeInfo.Current.Assets += amount;
            Debug.Log($"[Debugger] Added {amount} to assets. New Total: {PlayerRunTimeInfo.Current.Assets}");
        }
    }

    [Button]
    public void ResetRound()
    {
        if (PlayerRunTimeInfo.Current != null)
        {
            PlayerRunTimeInfo.Current.CurrentRound = 1;
        }
    }
}

public class BuffDebugViewModel
{
    [Title("Buff Registry")]
    [Button("Initialize Registry"), GUIColor(0.8f, 0.8f, 1f)]
    public void InitializeRegistry()
    {
        BuffRegistry.Initialize();
        Debug.Log("[BuffDebugger] BuffRegistry initialized.");
    }

    [ShowInInspector, DisplayAsString, LabelText("Registered Buffs")]
    public string RegisteredBuffCount => $"{GetRegisteredBuffNames().Count} buffs available";

    [ShowInInspector, ValueDropdown("GetRegisteredBuffNames")]
    [LabelText("Select Buff")]
    public string SelectedBuffName = "MarketBuff_Fatigue";

    private static List<string> GetRegisteredBuffNames()
    {
        var names = new List<string>();
        foreach (var buff in BuffRegistry.GetAllBuffs())
        {
            names.Add($"[{buff.id}] {buff.buffName}");
        }
        return names.Count > 0 ? names : new List<string> { "MarketBuff_Fatigue" };
    }

    [PropertySpace(10)]
    [Title("Add Buff Actions")]

    [HorizontalGroup("AddActions")]
    [Button("Add to Player"), GUIColor(0.5f, 1f, 0.5f)]
    public void AddPlayerBuff()
    {
        if (PlayerRunTimeInfo.Current?.PlayerBuffSystem != null)
        {
            // Parse ID from string "[ID] Name"
            int buffId = 0;
            if (int.TryParse(SelectedBuffName.Split(' ')[0].Trim('[', ']'), out buffId))
            {
                bool success = PlayerRunTimeInfo.Current.PlayerBuffSystem.AddBuffById(buffId);
                Debug.Log($"[BuffDebugger] Add Player Buff ID {buffId}: {success}");
            }
            else
            {
                // Fallback to name search
                var rawName = SelectedBuffName.Substring(SelectedBuffName.IndexOf(']') + 2);
                bool success = PlayerRunTimeInfo.Current.PlayerBuffSystem.AddBuff(rawName);
                Debug.Log($"[BuffDebugger] Add Player Buff '{rawName}': {success}");
            }
        }
        else
        {
            Debug.LogWarning("[BuffDebugger] Cannot add buff: PlayerRunTimeInfo missing or empty name.");
        }
    }

    [HorizontalGroup("AddActions")]
    [Button("Add to Market"), GUIColor(0.5f, 0.8f, 1f)]
    public void AddMarketBuff()
    {
        if (PlayerRunTimeInfo.Current?.MarketBuffSystem != null)
        {
            int buffId = 0;
            if (int.TryParse(SelectedBuffName.Split(' ')[0].Trim('[', ']'), out buffId))
            {
                bool success = PlayerRunTimeInfo.Current.MarketBuffSystem.AddBuffById(buffId);
                Debug.Log($"[BuffDebugger] Add Market Buff ID {buffId}: {success}");
            }
            else
            {
                var rawName = SelectedBuffName.Substring(SelectedBuffName.IndexOf(']') + 2);
                bool success = PlayerRunTimeInfo.Current.MarketBuffSystem.AddBuff(rawName);
                Debug.Log($"[BuffDebugger] Add Market Buff '{rawName}': {success}");
            }
        }
        else
        {
            Debug.LogWarning("[BuffDebugger] Cannot add buff: PlayerRunTimeInfo missing or empty name.");
        }
    }

    [HorizontalGroup("AddActions")]
    [Button("Test Buff Shake", ButtonSizes.Medium), GUIColor(1f, 0.8f, 0.4f)]
    public void TestBuffShake()
    {
        if (int.TryParse(SelectedBuffName.Split(' ')[0].Trim('[', ']'), out int buffId))
        {
            GameLogicUI.Instance?.PunchBuffItem(buffId);
            Debug.Log($"[BuffDebugger] Testing Shake for Buff ID: {buffId}");
        }
        else
        {
            // Fallback if parsing fails (e.g. if name format changes), try to find ID from name lookup
            var buff = BuffRegistry.GetAllBuffs().FirstOrDefault(b => b.buffName == SelectedBuffName);
            if (buff != null)
            {
                GameLogicUI.Instance?.PunchBuffItem(buff.id);
                Debug.Log($"[BuffDebugger] Testing Shake for Buff ID: {buff.id}");
            }
            else
            {
                Debug.LogWarning("[BuffDebugger] Could not parse Buff ID from selection.");
            }
        }
    }

    [PropertySpace(20)]
    [Title("Global Actions")]
    [Button("Clear All Buffs"), GUIColor(1f, 0.5f, 0.5f)]
    public void ClearAllBuffs()
    {
        if (PlayerRunTimeInfo.Current != null)
        {
            PlayerRunTimeInfo.Current.PlayerBuffSystem?.ClearBuff();
            PlayerRunTimeInfo.Current.MarketBuffSystem?.ClearBuff();
            Debug.Log("[BuffDebugger] All buffs cleared.");
        }
    }

    [PropertySpace(30)]
    [Title("Current Buff State")]

    [BoxGroup("Player Main Buffs")]
    [ShowInInspector, ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
    public List<BuffInfo> PlayerBuffs => PlayerRunTimeInfo.Current?.PlayerBuffSystem?.BuffInfoList.ToList() ?? new List<BuffInfo>();

    [BoxGroup("Market Trend Buffs")]
    [ShowInInspector, ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
    public List<BuffInfo> MarketBuffs => PlayerRunTimeInfo.Current?.MarketBuffSystem?.BuffInfoList.ToList() ?? new List<BuffInfo>();
}

public class AviationSystemDebugViewModel
{
    [ShowInInspector]
    public AviationSystem System => GameProcedure.Instance?.GameLogicState?.aviationSystem;

    [Button]
    public void DetectStructuresNow()
    {
        if (System != null)
        {
            var result = StructureDetector.DetectAll(System);
            Debug.Log($"[Debugger] Detetcion Result: {result.Rings.Count} Rings, {result.SingleLines.Count} SingleLines, {result.Radials.Count} Radials");
        }
    }
}
