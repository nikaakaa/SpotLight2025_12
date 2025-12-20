using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLogicUI : MonoBehaviour
{
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

    private Player boundPlayer;
    private CityNode currentNode;
    private Coroutine bindCoroutine;
    private Coroutine refreshCoroutine;

    private readonly List<TextMeshProUGUI> buffItemTexts = new();
    private readonly StringBuilder sb = new();

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

        UnbindPlayer();
    }

    private void Update()
    {
        // 实时刷新 UI 数据
        RefreshMoney(PlayerRunTimeInfo.Current?.Assets ?? 0);
        RefreshStatistics();
        RefreshCurrentNodeInfo();

        if (enableHoverRaycastForNodeInfo)
        {
            UpdateHoveredNodeInfo();
        }
    }

    private IEnumerator BindWhenReady()
    {
        // 等待 Player 与 PlayerRunTimeInfo 初始化
        while (Player.Instance == null && PlayerRunTimeInfo.Current == null)
        {
            yield return null;
        }

        // 优先绑定 Player（可转发事件、可拿到 BuffHandler）
        if (Player.Instance != null)
        {
            BindPlayer(Player.Instance);
        }
        else
        {
            // 兜底：只用 PlayerRunTimeInfo 刷新静态 UI
            RefreshAll();
        }
    }

    private void BindPlayer(Player player)
    {
        if (player == null) return;
        if (boundPlayer == player) return;

        UnbindPlayer();
        boundPlayer = player;

        boundPlayer.OnAssetsChanged += HandleAssetsChanged;
        boundPlayer.OnRoundStarted += HandleRoundStarted;
        boundPlayer.OnSettlementCompleted += HandleSettlementCompleted;

        RefreshAll();

        if (autoRefreshBuffList)
        {
            refreshCoroutine = StartCoroutine(RefreshBuffListLoop());
        }
    }

    private void UnbindPlayer()
    {
        if (boundPlayer == null) return;
        boundPlayer.OnAssetsChanged -= HandleAssetsChanged;
        boundPlayer.OnRoundStarted -= HandleRoundStarted;
        boundPlayer.OnSettlementCompleted -= HandleSettlementCompleted;
        boundPlayer = null;
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
        RefreshMoney(PlayerRunTimeInfo.Current?.Assets ?? boundPlayer?.Assets ?? 0);
        RefreshStatistics();
        RefreshBuffList();
        RefreshCurrentNodeInfo();
    }

    private void RefreshMoney(long assets)
    {
        if (moneyText == null) return;
        moneyText.text = $"${assets:N0}";
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

        var player = boundPlayer != null ? boundPlayer : Player.Instance;
        if (player == null)
        {
            EnsureBuffItemCount(1);
            buffItemTexts[0].text = "Player 未初始化";
            return;
        }

        var rows = new List<string>();

        // 使用配置的标题文本
        rows.Add(BuffDisplayConfig.MarketBuffTitle);
        AppendBuffRows(rows, player.MarketBuffHandler, true);
        rows.Add(" ");
        rows.Add(BuffDisplayConfig.PlayerBuffTitle);
        AppendBuffRows(rows, player.PlayerBuffHandler, false);

        EnsureBuffItemCount(rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            buffItemTexts[i].text = rows[i];
        }
    }

    private static void AppendBuffRows(List<string> rows, BuffHandler handler, bool isMarketBuff)
    {
        if (handler == null)
        {
            rows.Add("  (无处理器)");
            return;
        }

        var list = handler.BuffInfoList;
        if (list == null || list.Count == 0)
        {
            rows.Add("  (无)");
            return;
        }

        foreach (var buff in list)
        {
            if (buff == null || buff.buffData == null)
            {
                rows.Add("  (空Buff)");
                continue;
            }

            int buffId = buff.buffData.id;

            // 优先使用 BuffDisplayConfig 配置的显示文本
            string displayText = BuffDisplayConfig.GetBuffDisplayText(buffId);

            if (!string.IsNullOrEmpty(displayText))
            {
                // 使用配置的文本
                rows.Add($"  {displayText}");
            }
            else
            {
                // 如果配置中没有，使用 BuffData 的 description 或 buffName
                string fallbackText = !string.IsNullOrWhiteSpace(buff.buffData.description)
                    ? buff.buffData.description
                    : (!string.IsNullOrWhiteSpace(buff.buffData.buffName)
                        ? buff.buffData.buffName
                        : $"Buff_{buffId}");
                rows.Add($"  {fallbackText}");
            }
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
}
