using DG.Tweening;
using UnityEngine;

/// <summary>
/// 城市节点视图组件
/// 挂载在城市节点预制体上，只负责显示
/// 交互逻辑由 ConnectModifyRouteState 通过射线检测处理
/// 
/// 标识显示：直接在预制体上添加子物体作为标识
/// </summary>
public class CityNode : MonoBehaviour
{
    [Header("数据")]
    public AviationNode aviationNode;

    [Header("视觉")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color hoverColor = Color.yellow;

    [Header("等级显示")]
    [SerializeField] private TMPro.TextMeshProUGUI levelText;  // 等级文本（子物体）

    [Header("标识设置（子物体）")]
    [SerializeField] private Transform markerTransform;  // 拖入标识子物体
    [SerializeField] private float showZoomThreshold = 30f;  // 缩放超过此值显示标识
    [SerializeField] private float referenceZoom = 20f;  // 参考缩放值
    [SerializeField] private float markerBaseScale = 1f;  // 标识基础大小

    private bool isSelected = false;
    private bool isHovered = false;
    private Camera mainCamera;

    // 结算动画用
    private Vector3 originalScale;
    private Color originalColor;
    private bool isDimmed = false;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // 自动查找等级文本
        if (levelText == null)
            levelText = GetComponentInChildren<TMPro.TextMeshProUGUI>();

        // 设置渲染层级 - 城市节点在最上层
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = RenderLayers.CITY;

        // 注意：TextMeshProUGUI 的渲染层级由 Canvas 的 Sort Order 控制

        originalScale = transform.localScale;
    }

    void Start()
    {
        mainCamera = Camera.main;
        originalColor = normalColor;

        // 安全检查
        if (aviationNode == null)
        {
            Debug.LogError($"[CityNode] '{name}' 的 aviationNode 数据为空！");
            return;
        }
        UpdateVisual();
    }

    void LateUpdate()
    {
        // 更新标识显示
        if (markerTransform != null && mainCamera != null)
        {
            float zoom = GetCameraZoom();
            bool shouldShow = zoom >= showZoomThreshold;

            // 显示/隐藏标识
            if (markerTransform.gameObject.activeSelf != shouldShow)
            {
                markerTransform.gameObject.SetActive(shouldShow);
            }

            // 动态缩放以保持屏幕固定大小
            if (shouldShow)
            {
                float scale = (zoom / referenceZoom) * markerBaseScale;
                markerTransform.localScale = Vector3.one * scale;
            }
        }
    }

    private float GetCameraZoom()
    {
        if (mainCamera == null) return referenceZoom;
        return mainCamera.orthographic
            ? mainCamera.orthographicSize
            : Mathf.Abs(mainCamera.transform.position.z);
    }

    /// <summary>
    /// 初始化节点视图（由 AviationSystem 调用）
    /// </summary>
    public void Initialize(AviationNode node)
    {
        aviationNode = node;
        // 设置反向引用（用于动画系统）
        node.cityNodeView = this;

        // 设置等级文本
        UpdateLevelText();

        UpdateVisual();
    }

    /// <summary>
    /// 更新等级显示文本
    /// </summary>
    public void UpdateLevelText()
    {
        if (levelText == null)
        {
            Debug.LogWarning($"[CityNode] {name}: levelText 为空，无法更新等级显示！");
            return;
        }

        if (aviationNode == null || aviationNode.nodeData == null)
        {
            Debug.LogWarning($"[CityNode] {name}: aviationNode 或 nodeData 为空！");
            return;
        }

        // 从 nodeData.Id 计算等级（1001=lv1, 1002=lv2, ...）
        int level = aviationNode.nodeData.Id - 1000;
        levelText.text = level.ToString();
        Debug.Log($"[CityNode] {name}: 设置等级文本 = {level}");
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisual();
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null && !isDimmed)
        {
            if (isSelected)
                spriteRenderer.color = selectedColor;
            else if (isHovered)
                spriteRenderer.color = hoverColor;
            else
                spriteRenderer.color = normalColor;
        }
    }

    #region 结算动画方法

    /// <summary>
    /// 设置变暗状态（结算准备阶段）
    /// </summary>
    public void SetDim(bool dim, float duration = 0f)
    {
        isDimmed = dim;
        Color targetColor = dim ? SettlementAnimConfig.DimColor : originalColor;
        float targetScale = dim ? 0.95f : 1f;

        if (spriteRenderer == null) return;

        if (duration > 0)
        {
            spriteRenderer.DOColor(targetColor, duration);
            transform.DOScale(originalScale * targetScale, duration);
        }
        else
        {
            spriteRenderer.color = targetColor;
            transform.localScale = originalScale * targetScale;
        }
    }

    /// <summary>
    /// 高亮节点（结算时亮起）
    /// </summary>
    public Tween Highlight(float duration = 0.1f)
    {
        if (spriteRenderer == null) return null;

        var seq = DOTween.Sequence();
        seq.Append(spriteRenderer.DOColor(SettlementAnimConfig.HighlightColor, duration));
        seq.Join(transform.DOScale(originalScale * SettlementAnimConfig.NodeHighlightScale, duration));
        return seq;
    }

    /// <summary>
    /// 闪烁效果
    /// </summary>
    public Tween Flash(Color color, float duration = 0.2f)
    {
        if (spriteRenderer == null) return null;

        return spriteRenderer.DOColor(color, duration / 2f)
            .SetLoops(2, LoopType.Yoyo);
    }

    /// <summary>
    /// 设置动画缩放
    /// </summary>
    public Tween SetAnimScale(float scale, float duration = 0.1f)
    {
        return transform.DOScale(originalScale * scale, duration);
    }

    /// <summary>
    /// 恢复正常状态
    /// </summary>
    public void Restore(float duration = 0.2f)
    {
        isDimmed = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(normalColor, duration);
        }
        transform.DOScale(originalScale, duration);
    }

    /// <summary>
    /// 获取头顶浮动数字位置
    /// </summary>
    public Vector3 GetFloatingNumberPosition()
    {
        return transform.position + SettlementAnimConfig.FloatingNumberOffset;
    }

    #endregion
}
