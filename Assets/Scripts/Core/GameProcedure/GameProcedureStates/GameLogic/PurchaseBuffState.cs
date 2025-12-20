using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 购买增益状态 - 处理购买界面的交互和升级逻辑
/// </summary>
public class PurchaseBuffState : LeafState<GameProcedureContext>
{
    /// <summary>BuffShopUI 预制体的 Addressables Key</summary>
    private const string BUFF_SHOP_PANEL = "BuffShopUI";

    private bool waitingForSelection;
    private GameProcedureContext cachedContext;
    private BuffShopUI shopUI;

    public PurchaseBuffState()
    {
        Name = nameof(PurchaseBuffState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 进入购买/升级界面");

        cachedContext = ctx;
        waitingForSelection = true;

        // 通过 UIManager 加载 BuffShopUI 面板
        UIManager.Instance.ShowPanel(BUFF_SHOP_PANEL, OnPanelLoaded);
    }

    private void OnPanelLoaded(GameObject panelGo)
    {
        if (panelGo == null)
        {
            Debug.LogError($"[{Name}] BuffShopUI 加载失败，自动跳过");
            cachedContext?.Next();
            return;
        }

        shopUI = panelGo.GetComponent<BuffShopUI>();
        if (shopUI == null)
        {
            Debug.LogError($"[{Name}] 面板缺少 BuffShopUI 组件，自动跳过");
            cachedContext?.Next();
            return;
        }

        // 生成随机 Buff 选项（3个玩家 Buff）
        var buffOptions = GenerateRandomBuffOptions(3);

        // 绑定事件
        shopUI.OnBuffSelected += OnBuffSelected;
        shopUI.OnSkipped += OnSkipped;

        // 显示 UI
        shopUI.Show(buffOptions);
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 等待玩家选择
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 退出购买界面");

        // 解绑事件
        if (shopUI != null)
        {
            shopUI.OnBuffSelected -= OnBuffSelected;
            shopUI.OnSkipped -= OnSkipped;
            shopUI = null;
        }

        // 通过 UIManager 隐藏面板
        UIManager.Instance.HidePanel(BUFF_SHOP_PANEL);

        cachedContext = null;
    }

    private void OnBuffSelected(int buffId)
    {
        if (!waitingForSelection) return;
        waitingForSelection = false;

        Debug.Log($"[{Name}] 玩家选择了 Buff: {buffId}");

        // 获取 Buff 价格
        int price = BuffDisplayConfig.GetBuffPrice(buffId);

        // 检查玩家是否有足够资产
        var playerInfo = PlayerRunTimeInfo.Current;
        if (playerInfo == null)
        {
            Debug.LogError($"[{Name}] PlayerRunTimeInfo.Current 为空！");
            cachedContext?.Next();
            return;
        }

        // 尝试购买
        if (playerInfo.PurchaseBuff(buffId, price))
        {
            Debug.Log($"[{Name}] 购买成功！Buff ID={buffId}, 价格={price}, 剩余资产={playerInfo.Assets}");
            Debug.Log($"[{Name}] 拥有的 Buff 数量: {playerInfo.OwnedPlayerBuffIds.Count}");

            // 刷新 UI
            GameLogicUI.Instance?.RefreshAll();
        }
        else
        {
            Debug.LogWarning($"[{Name}] 购买失败！资产不足，需要 {price}，当前 {playerInfo.Assets}");
        }

        // 进入下一状态
        cachedContext?.Next();
    }

    private void OnSkipped()
    {
        if (!waitingForSelection) return;
        waitingForSelection = false;

        Debug.Log($"[{Name}] 玩家跳过购买");

        // 进入下一状态
        cachedContext?.Next();
    }

    /// <summary>
    /// 生成随机 Buff 选项
    /// </summary>
    private List<BuffShopData> GenerateRandomBuffOptions(int count)
    {
        var options = new List<BuffShopData>();

        // 获取所有玩家 Buff 枚举值
        var allBuffs = System.Enum.GetValues(typeof(EPlayerBuff));
        var shuffledList = new List<EPlayerBuff>();
        foreach (EPlayerBuff buff in allBuffs)
        {
            shuffledList.Add(buff);
        }

        // 随机打乱
        for (int i = shuffledList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffledList[i], shuffledList[j]) = (shuffledList[j], shuffledList[i]);
        }

        // 取前 count 个
        for (int i = 0; i < Mathf.Min(count, shuffledList.Count); i++)
        {
            options.Add(BuffShopData.FromPlayerBuff(shuffledList[i]));
        }

        return options;
    }
}

