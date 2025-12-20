using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 航线绘制控制器 - 统一管理所有航线的 LineRenderer 绘制
/// 
/// 职责：
/// - 管理航线视图的创建和销毁
/// - 统一设置渲染层级（SortingOrder）
/// - 提供航线的增删改查接口
/// 
/// 使用方式：
/// - 在 GameLogicState.OnEnter() 中自动初始化
/// - 通过 AirLineController.Instance 访问单例
/// </summary>
public class AirLineController : MonoBehaviour
{
    public static AirLineController Instance { get; private set; }

    [Header("航线样式")]
    [SerializeField] private Color defaultLineColor = Color.cyan;
    [SerializeField] private float defaultLineWidth = 0.1f;

    /// <summary>
    /// 航线视图字典（edgeIndex -> EdgeLineView）
    /// </summary>
    private Dictionary<int, EdgeLineView> lineViews = new Dictionary<int, EdgeLineView>();

    /// <summary>
    /// 航线父物体
    /// </summary>
    private Transform linesParent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 创建航线父物体
        linesParent = new GameObject("AirLines").transform;
        linesParent.SetParent(transform);
        linesParent.localPosition = Vector3.zero;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region 公开接口

    /// <summary>
    /// 添加航线视图
    /// </summary>
    /// <param name="edge">航线数据</param>
    /// <returns>创建的视图组件</returns>
    public EdgeLineView AddLine(AviationEdge edge)
    {
        if (edge == null)
        {
            Debug.LogError("[AirLineController] AddLine: edge 为空");
            return null;
        }

        // 检查是否已存在
        if (lineViews.ContainsKey(edge.edgeIndex))
        {
            Debug.LogWarning($"[AirLineController] 航线 {edge.edgeIndex} 的视图已存在");
            return lineViews[edge.edgeIndex];
        }

        // 创建视图 GameObject
        string viewName = $"Edge_{edge.edgeIndex}_{edge.fromNode?.nodeIndex}_to_{edge.toNode?.nodeIndex}";
        GameObject lineObj = new GameObject(viewName);
        lineObj.transform.SetParent(linesParent);

        // 添加并初始化 EdgeLineView 组件
        EdgeLineView lineView = lineObj.AddComponent<EdgeLineView>();

        // 设置默认样式
        lineView.lineColor = defaultLineColor;
        lineView.lineWidth = defaultLineWidth;

        // 初始化（会调用 SetupLineRenderer 和 UpdateView）
        lineView.Initialize(edge);

        // 注册到字典
        lineViews.Add(edge.edgeIndex, lineView);

        Debug.Log($"[AirLineController] 添加航线视图: index={edge.edgeIndex}");
        return lineView;
    }

    /// <summary>
    /// 移除航线视图
    /// </summary>
    /// <param name="edgeIndex">航线索引</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveLine(int edgeIndex)
    {
        if (!lineViews.TryGetValue(edgeIndex, out var lineView))
        {
            Debug.LogWarning($"[AirLineController] 未找到航线视图: index={edgeIndex}");
            return false;
        }

        // 销毁 GameObject
        if (lineView != null)
        {
            Destroy(lineView.gameObject);
        }

        lineViews.Remove(edgeIndex);
        Debug.Log($"[AirLineController] 移除航线视图: index={edgeIndex}");
        return true;
    }

    /// <summary>
    /// 获取航线视图
    /// </summary>
    /// <param name="edgeIndex">航线索引</param>
    /// <returns>视图组件，不存在返回 null</returns>
    public EdgeLineView GetLine(int edgeIndex)
    {
        lineViews.TryGetValue(edgeIndex, out var lineView);
        return lineView;
    }

    /// <summary>
    /// 清除所有航线视图
    /// </summary>
    public void ClearAllLines()
    {
        foreach (var kvp in lineViews)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        lineViews.Clear();
        Debug.Log("[AirLineController] 清除所有航线视图");
    }

    /// <summary>
    /// 更新航线样式
    /// </summary>
    /// <param name="edgeIndex">航线索引</param>
    /// <param name="color">线条颜色</param>
    /// <param name="width">线条宽度</param>
    public void UpdateLineStyle(int edgeIndex, Color color, float width)
    {
        if (!lineViews.TryGetValue(edgeIndex, out var lineView))
        {
            Debug.LogWarning($"[AirLineController] 未找到航线视图: index={edgeIndex}");
            return;
        }

        lineView.SetColor(color);
        lineView.SetWidth(width);
    }

    /// <summary>
    /// 获取所有航线视图数量
    /// </summary>
    public int LineCount => lineViews.Count;

    /// <summary>
    /// 设置默认航线颜色
    /// </summary>
    public void SetDefaultColor(Color color)
    {
        defaultLineColor = color;
    }

    /// <summary>
    /// 设置默认航线宽度
    /// </summary>
    public void SetDefaultWidth(float width)
    {
        defaultLineWidth = width;
    }

    #endregion

    #region 静态工具方法

    /// <summary>
    /// 在场景中创建 AirLineController
    /// </summary>
    public static AirLineController CreateInScene()
    {
        var existing = FindFirstObjectByType<AirLineController>();
        if (existing != null)
        {
            Debug.Log("[AirLineController] 已存在实例，返回现有实例");
            return existing;
        }

        GameObject go = new GameObject("AirLineController");
        var controller = go.AddComponent<AirLineController>();
        Debug.Log("[AirLineController] 创建新实例");
        return controller;
    }

    #endregion
}
