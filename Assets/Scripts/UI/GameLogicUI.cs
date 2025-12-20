using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLogicUI : MonoBehaviour
{
    public static GameLogicUI Instance { get; private set; }
    public ScrollRect buffListScrollView;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI currentNodeInformation;
    public TextMeshProUGUI StatisticsText;

    [Header("刷新")]
    [SerializeField] private bool autoRefreshBuffList = true;
    [SerializeField] private float buffListRefreshInterval = 0.5f;
    [SerializeField] private bool enableHoverRaycastForNodeInfo = true;

    [Header("Buff 列表生成")]
    [SerializeField] private bool reuseFirstChildAsTemplate = true;
    [SerializeField] private float buffItemFontSize = 20f;
    [SerializeField, Tooltip("Addressables 中的 Buff 项预制体名称，留空则动态创建")]
    private string buffItemPrefabName = "";

    private PlayerRunTimeInfo boundInfo;
    private CityNode currentNode;
    private Coroutine bindCoroutine;
    private Coroutine refreshCoroutine;

    private readonly List<TextMeshProUGUI> buffItemTexts = new();
    private readonly StringBuilder sb = new();

    // 结算动画控制
    private bool isSettlementMode = false;
    private Dictionary<int, TextMeshProUGUI> buffIdToText = new();

    void Awake()
    {
        Instance = this;

        // 自动查找未赋值的组件
        if (moneyText == null)
        {
            // 尝试通过名称查找
            var moneyObj = transform.Find("MoneyText");
            if (moneyObj == null) moneyObj = transform.Find("Money");
            if (moneyObj == null) moneyObj = transform.Find("Assets");
            if (moneyObj != null)
            {
                moneyText = moneyObj.GetComponent<TextMeshProUGUI>();
            }

            if (moneyText == null)
            {
                Debug.LogWarning("[GameLogicUI] moneyText 未赋值且未找到！请在 Inspector 中拖入 MoneyText");
            }
        }
    }

    private void OnEnable()
    {
        bindCoroutine = StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        if (bindCoroutine != null)
        {
            StopCoroutine(bindCoroutine);
            bindCoroutine = null;
        }

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }

        UnbindInfo();
    }

    private void Update()
    {
        // 结算动画期间暂停自动刷新
        if (isSettlementMode) return;

        // 实时刷新 UI 数据
        // 如果已绑定事件，通常不需要在 Update 中频繁刷新，但为了保险起见可以保留，或者依赖事件驱动
        // 这里保留刷新逻辑，但使用 boundInfo
        RefreshMoney(boundInfo?.Assets ?? 0);
        RefreshStatistics();
        RefreshCurrentNodeInfo();

        if (enableHoverRaycastForNodeInfo)
        {
            UpdateHoveredNodeInfo();
        }
    }

    private IEnumerator BindWhenReady()
    {
        // 等待 PlayerRunTimeInfo 初始化
        while (PlayerRunTimeInfo.Current == null)
        {
            yield return null;
        }

        BindInfo(PlayerRunTimeInfo.Current);
    }

    private void BindInfo(PlayerRunTimeInfo info)
    {
        if (info == null) return;
        if (boundInfo == info) return;

        UnbindInfo();
        boundInfo = info;

        boundInfo.OnAssetsChanged += HandleAssetsChanged;
        boundInfo.OnRoundStarted += HandleRoundStarted;
        boundInfo.OnSettlementCompleted += HandleSettlementCompleted;

        RefreshAll();

        if (autoRefreshBuffList)
        {
            refreshCoroutine = StartCoroutine(RefreshBuffListLoop());
        }
    }

    private void UnbindInfo()
    {
        if (boundInfo == null) return;
        boundInfo.OnAssetsChanged -= HandleAssetsChanged;
        boundInfo.OnRoundStarted -= HandleRoundStarted;
        boundInfo.OnSettlementCompleted -= HandleSettlementCompleted;
        boundInfo = null;
    }

    private IEnumerator RefreshBuffListLoop()
    {
        var wait = new WaitForSeconds(buffListRefreshInterval);
        while (true)
        {
            RefreshBuffList();
            yield return wait;
        }
    }

    private void HandleAssetsChanged(long oldValue, long newValue)
    {
        RefreshMoney(newValue);
        RefreshStatistics();
    }

    private void HandleRoundStarted(int round)
    {
        RefreshStatistics();
        RefreshBuffList();
    }

    private void HandleSettlementCompleted(SettlementResult result)
    {
        RefreshStatistics();
        RefreshBuffList();
    }

    public void RefreshAll()
    {
        Debug.Log($"[GameLogicUI] RefreshAll 被调用");
        RefreshMoney(PlayerRunTimeInfo.Current?.Assets ?? boundInfo?.Assets ?? 0);
        RefreshStatistics();
        RefreshBuffList();
        RefreshCurrentNodeInfo();
    }

    private void RefreshMoney(long assets)
    {
        if (moneyText == null)
        {
            Debug.LogWarning("[GameLogicUI] moneyText 为空！");
            return;
        }
        moneyText.text = $"${assets:N0}";
        Debug.Log($"[GameLogicUI] 刷新资产显示: ${assets:N0}");
    }

    private void RefreshStatistics()
    {
        if (StatisticsText == null) return;

        // 获取航空系统数据
        var aviationSystem = GameProcedure.Instance?.GameLogicState?.aviationSystem;
        if (aviationSystem == null)
        {
            StatisticsText.text = "航空系统未初始化";
            return;
        }

        int nodeCount = aviationSystem.aviationNodeDict?.Count ?? 0;
        int edgeCount = aviationSystem.aviationEdgeDict?.Count ?? 0;

        // 检测结构
        var structures = StructureDetector.DetectAll(aviationSystem);
        int ringCount = structures?.Rings?.Count ?? 0;
        int singleLineCount = structures?.SingleLines?.Count ?? 0;
        int radialCount = structures?.Radials?.Count ?? 0;

        sb.Clear();
        sb.AppendLine($"节点: {nodeCount}  |  航线: {edgeCount}");
        sb.AppendLine($"环: {ringCount}  |  单线: {singleLineCount}  |  放射: {radialCount}");
        sb.Append($"结构总数: {ringCount + singleLineCount + radialCount}");

        StatisticsText.text = sb.ToString();
    }

    private void RefreshBuffList()
    {
        if (buffListScrollView == null) return;
        if (buffListScrollView.content == null) return;

        var playerInfo = PlayerRunTimeInfo.Current;
        if (playerInfo == null)
        {
            EnsureBuffItemCount(1);
            buffItemTexts[0].text = "PlayerRunTimeInfo 未初始化";
            return;
        }

        var rows = new List<string>();

        // 市场趋势 Buff（从 PlayerRunTimeInfo 读取）
        rows.Add(BuffDisplayConfig.MarketBuffTitle);
        if (playerInfo.CurrentMarketBuffIds.Count > 0)
        {
            foreach (var buffId in playerInfo.CurrentMarketBuffIds)
            {
                string text = BuffDisplayConfig.GetBuffDisplayText(buffId) ?? $"Buff_{buffId}";
                rows.Add($"  {text}");
            }
        }
        else
        {
            rows.Add("  (无)");
        }

        rows.Add(" ");

        // 玩家永久 Buff（从 PlayerRunTimeInfo 读取）
        rows.Add(BuffDisplayConfig.PlayerBuffTitle);
        if (playerInfo.OwnedPlayerBuffIds.Count > 0)
        {
            foreach (var buffId in playerInfo.OwnedPlayerBuffIds)
            {
                string text = BuffDisplayConfig.GetBuffDisplayText(buffId) ?? $"Buff_{buffId}";
                rows.Add($"  {text}");
            }
        }
        else
        {
            rows.Add("  (无)");
        }

        EnsureBuffItemCount(rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            buffItemTexts[i].text = rows[i];
        }
    }

    private void EnsureBuffItemCount(int count)
    {
        if (count < 0) count = 0;

        // 第一次：尝试复用 content 的第一个子物体作为模板
        if (buffItemTexts.Count == 0)
        {
            var content = buffListScrollView.content;
            if (reuseFirstChildAsTemplate && content.childCount > 0)
            {
                var first = content.GetChild(0);
                var tmp = first.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    buffItemTexts.Add(tmp);
                }
            }
        }

        // 如果没有模板，就创建一个
        if (buffItemTexts.Count == 0)
        {
            buffItemTexts.Add(CreateBuffTextItem(buffListScrollView.content));
        }

        // 增加
        while (buffItemTexts.Count < count)
        {
            buffItemTexts.Add(CreateBuffTextItem(buffListScrollView.content));
        }

        // 多余的隐藏（不销毁，避免 GC）
        for (int i = 0; i < buffItemTexts.Count; i++)
        {
            bool active = i < count;
            if (buffItemTexts[i] != null && buffItemTexts[i].gameObject.activeSelf != active)
            {
                buffItemTexts[i].gameObject.SetActive(active);
            }
        }
    }

    private TextMeshProUGUI CreateBuffTextItem(Transform parent)
    {
        // 优先使用 Addressables 预制体
        if (!string.IsNullOrEmpty(buffItemPrefabName))
        {
            var prefab = AddressablesMgr.Instance.LoadAssetSync<GameObject>(buffItemPrefabName);
            if (prefab != null)
            {
                var go = Instantiate(prefab, parent);
                var tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp != null) return tmp;

                // 如果预制体没有 TMP 组件，尝试子物体
                tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) return tmp;

                Debug.LogWarning($"[GameLogicUI] 预制体 {buffItemPrefabName} 缺少 TextMeshProUGUI 组件");
            }
        }

        // 降级：动态创建
        var fallbackGo = new GameObject("BuffItem", typeof(RectTransform));
        fallbackGo.transform.SetParent(parent, false);

        var fallbackTmp = fallbackGo.AddComponent<TextMeshProUGUI>();
        fallbackTmp.raycastTarget = false;
        if (buffItemFontSize > 0)
        {
            fallbackTmp.fontSize = buffItemFontSize;
        }

        var layout = fallbackGo.AddComponent<LayoutElement>();
        layout.minHeight = buffItemFontSize > 0 ? buffItemFontSize + 6f : 26f;

        return fallbackTmp;
    }

    // ========== 节点信息 ==========

    /// <summary>
    /// 外部可调用：设置当前关注节点（例如点击选中节点时）
    /// </summary>
    public void SetCurrentNode(CityNode node)
    {
        currentNode = node;
        RefreshCurrentNodeInfo();
    }

    private void RefreshCurrentNodeInfo()
    {
        if (currentNodeInformation == null) return;

        if (currentNode == null || currentNode.aviationNode == null || currentNode.aviationNode.nodeData == null)
        {
            currentNodeInformation.text = "当前节点: (无)";
            return;
        }

        var n = currentNode.aviationNode;
        var d = n.nodeData;
        currentNodeInformation.text =
            $"当前节点: [{n.nodeIndex}] {d.Name}\n" +
            $"等级: {d.NodeLevel}\n" +
            $"基础收益: {d.NodeIncome}\n" +
            $"基础成本: {d.NodeCost}\n" +
            $"连接航线数: {n.edges?.Count ?? 0}";
    }

    private void UpdateHoveredNodeInfo()
    {
        // 如果外部已经主动 SetCurrentNode，就不抢 UI
        if (currentNode != null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        CityNode hitNode = null;

        // 优先 3D Raycast
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            hitNode = hit.collider.GetComponentInParent<CityNode>();
        }

        // 兜底 2D Raycast
        if (hitNode == null)
        {
            var hit2D = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
            if (hit2D.collider != null)
            {
                hitNode = hit2D.collider.GetComponentInParent<CityNode>();
            }
        }

        if (hitNode != null)
        {
            SetCurrentNode(hitNode);
            // 立刻释放，让下一帧继续跟随鼠标
            currentNode = null;
        }
    }

    #region 结算动画方法

    /// <summary>
    /// 设置结算动画模式（暂停自动刷新）
    /// </summary>
    public void SetSettlementMode(bool active)
    {
        isSettlementMode = active;
    }

    /// <summary>
    /// 金额数字滚动动画
    /// </summary>
    /// <param name="target">目标金额</param>
    /// <param name="duration">动画时长</param>
    /// <param name="startValue">起始值（如为 null 则从当前 UI 文本解析）</param>
    public async UniTask AnimateMoneyTo(long target, float duration = 1f, long? startValue = null)
    {
        if (moneyText == null) return;

        // 优先使用传入的起始值，否则尝试从 UI 文本解析当前显示的数值
        long current;
        if (startValue.HasValue)
        {
            current = startValue.Value;
        }
        else
        {
            // 从 moneyText.text 解析（格式为 "$1,234"）
            string text = moneyText.text.Replace("$", "").Replace(",", "");
            if (!long.TryParse(text, out current))
            {
                current = PlayerRunTimeInfo.Current?.Assets ?? 0;
            }
        }

        var tween = DOTween.To(() => current, x =>
        {
            current = x;
            moneyText.text = $"${x:N0}";
        }, target, duration).SetEase(Ease.OutQuad);

        await UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete());
    }

    /// <summary>
    /// Buff 项跳动效果（小丑牌风格）
    /// </summary>
    public void PunchBuffItem(int buffId)
    {
        // 尝试从缓存获取
        if (!buffIdToText.TryGetValue(buffId, out var text) || text == null)
        {
            // 遍历查找
            foreach (var item in buffItemTexts)
            {
                if (item != null && item.text.Contains($"#{buffId}"))
                {
                    text = item;
                    buffIdToText[buffId] = text;
                    break;
                }
            }
        }

        if (text == null) return;

        // 跳动动画
        DOTween.Sequence()
            .Append(text.transform.DOScale(SettlementAnimConfig.BuffPunchScale, 0.1f).SetEase(Ease.OutBack))
            .Append(text.transform.DOScale(1f, 0.15f).SetEase(Ease.InOutSine))
            .Join(text.DOColor(SettlementAnimConfig.BuffPunchColor, 0.1f))
            .Append(text.DOColor(Color.white, 0.2f));
    }

    /// <summary>
    /// 高亮指定 Buff 项
    /// </summary>
    public void HighlightBuffItem(int buffId, Color color)
    {
        if (!buffIdToText.TryGetValue(buffId, out var text) || text == null) return;
        text.color = color;
    }

    /// <summary>
    /// 获取金额 UI 的世界坐标（用于数字飞向目标）
    /// </summary>
    public Vector3 GetMoneyWorldPosition()
    {
        if (moneyText == null) return Vector3.zero;
        return moneyText.transform.position;
    }

    #endregion
}

