using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 六边形鼠标高亮测试
/// 鼠标移动到某个格子时高亮显示该格子
/// 点击两个格子可以绘制直线连接
/// 使用 XY 平面，Z 始终为 0
/// 在 Game 视图和 Scene 视图都可见
/// </summary>
public class HexMouseHighlightTest : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("六边形大小")]
    public float hexSize = 1f;
    
    [Tooltip("是否为尖顶六边形")]
    public bool isPointyTop = true;
    
    [Tooltip("高亮颜色")]
    public Color highlightColor = Color.yellow;
    
    [Tooltip("网格线颜色")]
    public Color gridColor = new Color(1f, 1f, 1f, 0.3f);
    
    [Tooltip("选中格子颜色")]
    public Color selectedColor = Color.green;
    
    [Tooltip("路径颜色")]
    public Color pathColor = Color.cyan;
    
    [Tooltip("显示网格范围")]
    public int gridRadius = 5;
    
    [Tooltip("线条宽度")]
    public float lineWidth = 0.05f;

    [Header("调试信息")]
    [SerializeField] private HexCoord currentHex;
    [SerializeField] private Vector3 mouseWorldPos;
    [SerializeField] private HexCoord? startHex;
    [SerializeField] private HexCoord? endHex;
    [SerializeField] private List<HexCoord> currentPath = new List<HexCoord>();

    private Camera mainCamera;
    private LineRenderer highlightRenderer;
    private LineRenderer startRenderer;
    private LineRenderer endRenderer;
    private LineRenderer pathRenderer;
    private LineRenderer[] gridRenderers;
    private HexCoord lastHex;

    void Start()
    {
        mainCamera = Camera.main;
        
        // 设置六边形参数
        HexMetrics.SetSize(hexSize);
        HexMetrics.SetOrientation(isPointyTop);
        
        // 创建各种 LineRenderer
        CreateHighlightRenderer();
        CreateSelectionRenderers();
        CreatePathRenderer();
        CreateGridRenderers();
    }

    void Update()
    {
        // 更新六边形参数（方便运行时调试）
        HexMetrics.SetSize(hexSize);
        HexMetrics.SetOrientation(isPointyTop);
        
        // 获取鼠标世界坐标（Z=0 平面）
        mouseWorldPos = GetMouseWorldPosition();
        
        // 转换为六边形坐标
        currentHex = HexConverter2D.WorldToHex(mouseWorldPos);
        
        // 只在格子变化时更新高亮
        if (currentHex != lastHex)
        {
            UpdateHighlight();
            lastHex = currentHex;
        }
        
        // 处理鼠标点击
        HandleMouseClick();
    }

    /// <summary>
    /// 处理鼠标点击选择格子
    /// </summary>
    private void HandleMouseClick()
    {
        // 左键点击选择起点/终点
        if (Input.GetMouseButtonDown(0))
        {
            if (!startHex.HasValue)
            {
                // 选择起点
                startHex = currentHex;
                UpdateStartRenderer();
                Debug.Log($"选择起点: {startHex.Value}");
            }
            else if (!endHex.HasValue)
            {
                // 选择终点
                endHex = currentHex;
                UpdateEndRenderer();
                
                // 计算直线路径
                CalculatePath();
                Debug.Log($"选择终点: {endHex.Value}, 路径长度: {currentPath.Count}");
            }
        }
        
        // 右键清除选择
        if (Input.GetMouseButtonDown(1))
        {
            ClearSelection();
            Debug.Log("清除选择");
        }
    }

    /// <summary>
    /// 计算两点之间的路径（使用 A* 算法，带转向代价）
    /// </summary>
    private void CalculatePath()
    {
        if (!startHex.HasValue || !endHex.HasValue) return;
        
        // 使用 A* 寻路（带转向代价 + 叉积偏好）
        currentPath = startHex.Value.FindPathAStar(endHex.Value);
        
        UpdatePathRenderer();
    }

    /// <summary>
    /// 清除选择
    /// </summary>
    private void ClearSelection()
    {
        startHex = null;
        endHex = null;
        currentPath.Clear();
        
        if (startRenderer != null) startRenderer.positionCount = 0;
        if (endRenderer != null) endRenderer.positionCount = 0;
        if (pathRenderer != null) pathRenderer.positionCount = 0;
    }

    /// <summary>
    /// 创建高亮用的 LineRenderer
    /// </summary>
    private void CreateHighlightRenderer()
    {
        GameObject highlightObj = new GameObject("HexHighlight");
        highlightObj.transform.SetParent(transform);
        
        highlightRenderer = highlightObj.AddComponent<LineRenderer>();
        highlightRenderer.positionCount = 7; // 6个顶点 + 闭合
        highlightRenderer.loop = false;
        highlightRenderer.useWorldSpace = true;
        highlightRenderer.startWidth = lineWidth * 2f;
        highlightRenderer.endWidth = lineWidth * 2f;
        
        // 使用默认 Sprite 材质（支持颜色）
        highlightRenderer.material = new Material(Shader.Find("Sprites/Default"));
        highlightRenderer.startColor = highlightColor;
        highlightRenderer.endColor = highlightColor;
    }

    /// <summary>
    /// 创建选择点的 LineRenderer
    /// </summary>
    private void CreateSelectionRenderers()
    {
        // 起点
        GameObject startObj = new GameObject("StartHex");
        startObj.transform.SetParent(transform);
        startRenderer = startObj.AddComponent<LineRenderer>();
        SetupHexRenderer(startRenderer, selectedColor, lineWidth * 3f);
        
        // 终点
        GameObject endObj = new GameObject("EndHex");
        endObj.transform.SetParent(transform);
        endRenderer = endObj.AddComponent<LineRenderer>();
        SetupHexRenderer(endRenderer, selectedColor, lineWidth * 3f);
    }

    /// <summary>
    /// 创建路径 LineRenderer
    /// </summary>
    private void CreatePathRenderer()
    {
        GameObject pathObj = new GameObject("PathLine");
        pathObj.transform.SetParent(transform);
        
        pathRenderer = pathObj.AddComponent<LineRenderer>();
        pathRenderer.loop = false;
        pathRenderer.useWorldSpace = true;
        pathRenderer.startWidth = lineWidth * 4f;
        pathRenderer.endWidth = lineWidth * 4f;
        pathRenderer.material = new Material(Shader.Find("Sprites/Default"));
        pathRenderer.startColor = pathColor;
        pathRenderer.endColor = pathColor;
    }

    private void SetupHexRenderer(LineRenderer lr, Color color, float width)
    {
        lr.positionCount = 0;
        lr.loop = false;
        lr.useWorldSpace = true;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
    }

    /// <summary>
    /// 创建网格的 LineRenderers
    /// </summary>
    private void CreateGridRenderers()
    {
        var hexes = HexCoord.Zero.GetHexesInRange(gridRadius);
        gridRenderers = new LineRenderer[hexes.Count];
        
        GameObject gridParent = new GameObject("HexGrid");
        gridParent.transform.SetParent(transform);
        
        for (int i = 0; i < hexes.Count; i++)
        {
            GameObject cellObj = new GameObject($"Cell_{hexes[i]}");
            cellObj.transform.SetParent(gridParent.transform);
            
            LineRenderer lr = cellObj.AddComponent<LineRenderer>();
            lr.positionCount = 7;
            lr.loop = false;
            lr.useWorldSpace = true;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = gridColor;
            lr.endColor = gridColor;
            
            // 设置顶点位置
            Vector3[] corners = HexConverter2D.GetHexCorners(hexes[i]);
            Vector3[] positions = new Vector3[7];
            for (int j = 0; j < 6; j++)
            {
                positions[j] = corners[j];
            }
            positions[6] = corners[0]; // 闭合
            lr.SetPositions(positions);
            
            gridRenderers[i] = lr;
        }
    }

    /// <summary>
    /// 更新高亮显示
    /// </summary>
    private void UpdateHighlight()
    {
        if (highlightRenderer == null) return;
        
        Vector3[] corners = HexConverter2D.GetHexCorners(currentHex);
        Vector3[] positions = new Vector3[7];
        for (int i = 0; i < 6; i++)
        {
            positions[i] = corners[i];
        }
        positions[6] = corners[0]; // 闭合
        
        highlightRenderer.SetPositions(positions);
        highlightRenderer.startColor = highlightColor;
        highlightRenderer.endColor = highlightColor;
    }

    /// <summary>
    /// 更新起点渲染
    /// </summary>
    private void UpdateStartRenderer()
    {
        if (startRenderer == null || !startHex.HasValue) return;
        
        Vector3[] corners = HexConverter2D.GetHexCorners(startHex.Value);
        Vector3[] positions = new Vector3[7];
        for (int i = 0; i < 6; i++)
        {
            positions[i] = corners[i];
        }
        positions[6] = corners[0];
        
        startRenderer.positionCount = 7;
        startRenderer.SetPositions(positions);
    }

    /// <summary>
    /// 更新终点渲染
    /// </summary>
    private void UpdateEndRenderer()
    {
        if (endRenderer == null || !endHex.HasValue) return;
        
        Vector3[] corners = HexConverter2D.GetHexCorners(endHex.Value);
        Vector3[] positions = new Vector3[7];
        for (int i = 0; i < 6; i++)
        {
            positions[i] = corners[i];
        }
        positions[6] = corners[0];
        
        endRenderer.positionCount = 7;
        endRenderer.SetPositions(positions);
    }

    /// <summary>
    /// 更新路径渲染（连接所有路径格子的中心）
    /// </summary>
    private void UpdatePathRenderer()
    {
        if (pathRenderer == null || currentPath.Count == 0) return;
        
        pathRenderer.positionCount = currentPath.Count;
        Vector3[] positions = new Vector3[currentPath.Count];
        
        for (int i = 0; i < currentPath.Count; i++)
        {
            positions[i] = HexConverter2D.HexToWorld(currentPath[i]);
        }
        
        pathRenderer.SetPositions(positions);
    }

    /// <summary>
    /// 获取鼠标在 Z=0 平面上的世界坐标
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return Vector3.zero;
        }
        
        // 方法1：正交相机（推荐用于2D）
        if (mainCamera.orthographic)
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            return mousePos;
        }
        
        // 方法2：透视相机 - 射线与Z=0平面求交
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane xyPlane = new Plane(Vector3.forward, Vector3.zero);
        
        if (xyPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return Vector3.zero;
    }

    void OnDrawGizmos()
    {
        // 确保参数已设置
        HexMetrics.SetSize(hexSize);
        HexMetrics.SetOrientation(isPointyTop);
        
        // 绘制网格
        DrawHexGrid();
        
        // 绘制高亮格子
        if (Application.isPlaying)
        {
            DrawHighlightedHex();
            
            // 绘制鼠标位置
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mouseWorldPos, 0.1f);
        }
    }

    /// <summary>
    /// 绘制六边形网格
    /// </summary>
    private void DrawHexGrid()
    {
        Gizmos.color = gridColor;
        
        var hexes = HexCoord.Zero.GetHexesInRange(gridRadius);
        foreach (var hex in hexes)
        {
            DrawHexOutline(hex, gridColor);
        }
    }

    /// <summary>
    /// 绘制高亮的六边形
    /// </summary>
    private void DrawHighlightedHex()
    {
        // 绘制高亮边框
        DrawHexOutline(currentHex, highlightColor);
        
        // 绘制填充效果（用实心球表示中心）
        Vector3 center = HexConverter2D.HexToWorld(currentHex);
        Gizmos.color = highlightColor;
        Gizmos.DrawSphere(center, 0.15f);
        
        // 绘制坐标文本（在Scene视图中）
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(center + Vector3.up * 0.5f, currentHex.ToString());
        #endif
    }

    /// <summary>
    /// 绘制六边形边框
    /// </summary>
    private void DrawHexOutline(HexCoord hex, Color color)
    {
        Gizmos.color = color;
        Vector3[] corners = HexConverter2D.GetHexCorners(hex);
        
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            Gizmos.DrawLine(corners[i], corners[next]);
        }
    }
}
