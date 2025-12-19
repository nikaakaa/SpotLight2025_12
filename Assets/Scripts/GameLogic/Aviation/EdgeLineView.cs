using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 航线视图组件
/// 挂载在航线 GameObject 上，显示连线
/// 单向引用 AviationEdge（运行时数据）
/// 使用 3D BoxCollider 用于射线检测
/// </summary>
public class EdgeLineView : MonoBehaviour
{
    [Header("数据引用")]
    public AviationEdge aviationEdge;

    [Header("组件")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("样式")]
    public Color lineColor = Color.cyan;
    public Color hoverColor = Color.red;
    public float lineWidth = 0.1f;
    public float colliderHeight = 0.5f; // 碰撞体高度（便于点击）

    private bool isHovered = false;
    private List<GameObject> colliderObjects = new List<GameObject>();

    void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
    }

    void OnDestroy()
    {
        // 清理碰撞体子物体
        ClearColliders();
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

        // 更新 LineRenderer
        lineRenderer.positionCount = aviationEdge.pathCoords.Count;
        for (int i = 0; i < aviationEdge.pathCoords.Count; i++)
        {
            Vector3 pos = HexConverter2D.HexToWorld(aviationEdge.pathCoords[i]);
            lineRenderer.SetPosition(i, pos);
        }

        // 更新碰撞体
        UpdateColliders();

        Debug.Log($"[EdgeLineView] UpdateView: 设置了 {aviationEdge.pathCoords.Count} 个点");
    }

    /// <summary>
    /// 更新碰撞体（为每个线段创建 BoxCollider）
    /// </summary>
    private void UpdateColliders()
    {
        ClearColliders();

        if (aviationEdge == null || aviationEdge.pathCoords == null || aviationEdge.pathCoords.Count < 2)
            return;

        // 为相邻点之间的每个线段创建碰撞体
        for (int i = 0; i < aviationEdge.pathCoords.Count - 1; i++)
        {
            Vector3 start = HexConverter2D.HexToWorld(aviationEdge.pathCoords[i]);
            Vector3 end = HexConverter2D.HexToWorld(aviationEdge.pathCoords[i + 1]);

            CreateSegmentCollider(start, end, i);
        }
    }

    /// <summary>
    /// 为一个线段创建 BoxCollider
    /// </summary>
    private void CreateSegmentCollider(Vector3 start, Vector3 end, int index)
    {
        GameObject colliderObj = new GameObject($"EdgeCollider_{index}");
        colliderObj.transform.SetParent(transform);
        colliderObj.layer = gameObject.layer;

        // 计算线段中点和长度
        Vector3 midPoint = (start + end) / 2f;
        float length = Vector3.Distance(start, end);

        // 设置位置
        colliderObj.transform.position = midPoint;

        // 计算旋转（让碰撞体沿线段方向）
        Vector3 direction = (end - start).normalized;
        if (direction != Vector3.zero)
        {
            colliderObj.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(direction, Vector3.forward));
            colliderObj.transform.right = direction;
        }

        // 添加 BoxCollider
        BoxCollider boxCollider = colliderObj.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(length, lineWidth * 3f, colliderHeight);
        boxCollider.center = Vector3.zero;

        // 添加引用回 EdgeLineView（用于射线检测后获取）
        EdgeColliderReference refComp = colliderObj.AddComponent<EdgeColliderReference>();
        refComp.edgeLineView = this;

        colliderObjects.Add(colliderObj);
    }

    /// <summary>
    /// 清理所有碰撞体子物体
    /// </summary>
    private void ClearColliders()
    {
        foreach (var obj in colliderObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        colliderObjects.Clear();
    }

    /// <summary>
    /// 设置悬停状态
    /// </summary>
    public void SetHovered(bool hovered)
    {
        if (isHovered == hovered) return;
        isHovered = hovered;

        if (lineRenderer != null)
        {
            Color targetColor = hovered ? hoverColor : lineColor;
            lineRenderer.startColor = targetColor;
            lineRenderer.endColor = targetColor;
        }
    }

    /// <summary>
    /// 是否处于悬停状态
    /// </summary>
    public bool IsHovered => isHovered;

    /// <summary>
    /// 设置颜色
    /// </summary>
    public void SetColor(Color color)
    {
        lineColor = color;
        if (lineRenderer != null && !isHovered)
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

/// <summary>
/// 边碰撞体引用组件（用于射线检测后找到 EdgeLineView）
/// </summary>
public class EdgeColliderReference : MonoBehaviour
{
    public EdgeLineView edgeLineView;
}


