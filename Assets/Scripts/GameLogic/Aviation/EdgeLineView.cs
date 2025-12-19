using UnityEngine;

/// <summary>
/// 航线视图组件
/// 挂载在航线 GameObject 上，显示连线
/// 单向引用 AviationEdge（运行时数据）
/// </summary>
public class EdgeLineView : MonoBehaviour
{
    [Header("数据引用")]
    public AviationEdge aviationEdge;

    [Header("组件")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("样式")]
    public Color lineColor = Color.cyan;
    public float lineWidth = 0.1f;

    void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
    }

    /// <summary>
    /// 初始化视图
    /// </summary>
    public void Initialize(AviationEdge edge)
    {
        aviationEdge = edge;
        SetupLineRenderer();
        UpdateView();

        Debug.Log($"[EdgeLineView] Initialize: pathCount={edge?.pathCoords?.Count ?? 0}");
    }

    /// <summary>
    /// 设置 LineRenderer 样式
    /// </summary>
    private void SetupLineRenderer()
    {
        if (lineRenderer == null) return;

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.useWorldSpace = true;
    }

    /// <summary>
    /// 更新视图显示
    /// </summary>
    public void UpdateView()
    {
        if (lineRenderer == null || aviationEdge == null) return;
        if (aviationEdge.pathCoords == null || aviationEdge.pathCoords.Count == 0)
        {
            Debug.LogWarning("[EdgeLineView] UpdateView: pathCoords 为空或长度为0");
            return;
        }

        lineRenderer.positionCount = aviationEdge.pathCoords.Count;
        for (int i = 0; i < aviationEdge.pathCoords.Count; i++)
        {
            Vector3 pos = HexConverter2D.HexToWorld(aviationEdge.pathCoords[i]);
            lineRenderer.SetPosition(i, pos);
        }

        Debug.Log($"[EdgeLineView] UpdateView: 设置了 {aviationEdge.pathCoords.Count} 个点");
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    public void SetColor(Color color)
    {
        lineColor = color;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }

    /// <summary>
    /// 设置线宽
    /// </summary>
    public void SetWidth(float width)
    {
        lineWidth = width;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }
}
