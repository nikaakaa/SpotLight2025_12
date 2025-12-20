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

    [Header("标识设置（子物体）")]
    [SerializeField] private Transform markerTransform;  // 拖入标识子物体
    [SerializeField] private float showZoomThreshold = 30f;  // 缩放超过此值显示标识
    [SerializeField] private float referenceZoom = 20f;  // 参考缩放值
    [SerializeField] private float markerBaseScale = 1f;  // 标识基础大小

    private bool isSelected = false;
    private bool isHovered = false;
    private Camera mainCamera;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // 设置渲染层级 - 城市节点在最上层
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = RenderLayers.CITY;
    }

    void Start()
    {
        mainCamera = Camera.main;

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
        UpdateVisual();
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
        if (spriteRenderer != null)
        {
            if (isSelected)
                spriteRenderer.color = selectedColor;
            else if (isHovered)
                spriteRenderer.color = hoverColor;
            else
                spriteRenderer.color = normalColor;
        }
    }
}

