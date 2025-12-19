using UnityEngine;

/// <summary>
/// 城市节点视图组件
/// 挂载在城市节点预制体上，只负责显示
/// 交互逻辑由 ConnectModifyRouteState 通过射线检测处理
/// 需要 Collider2D 组件
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

    private bool isSelected = false;
    private bool isHovered = false;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // 安全检查：如果数据为空，可能是因为热重载或者未通过 AviationSystem 创建
        if (aviationNode == null)
        {
            Debug.LogError($"[CityNode] '{name}' 的 aviationNode 数据为空！\n" +
                           "可能是因为在运行过程中修改了代码导致热重载(Hot Reload)，丢失了非序列化引用。\n" +
                           "请重新运行游戏 (Stop -> Play)。");
            return;
        }
        UpdateVisual();
    }

    /// <summary>
    /// 初始化节点视图（由 AviationSystem 调用）
    /// 视图层单向引用运行时层
    /// </summary>
    public void Initialize(AviationNode node)
    {
        aviationNode = node;
        UpdateVisual();
    }

    /// <summary>
    /// 设置选中状态（由 ConnectModifyRouteState 调用）
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisual();
    }

    /// <summary>
    /// 设置悬停状态（由 ConnectModifyRouteState 调用）
    /// </summary>
    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
        UpdateVisual();
    }

    /// <summary>
    /// 更新视觉效果
    /// </summary>
    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            if (isSelected)
            {
                spriteRenderer.color = selectedColor;
            }
            else if (isHovered)
            {
                spriteRenderer.color = hoverColor;
            }
            else
            {
                spriteRenderer.color = normalColor;
            }
        }
    }
}
