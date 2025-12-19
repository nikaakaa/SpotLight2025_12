using UnityEngine;

/// <summary>
/// 六边形网格的度量参数
/// </summary>
public static class HexMetrics
{
    /// <summary>
    /// 六边形外半径（中心到顶点的距离）
    /// </summary>
    public static float OuterRadius = 1f;

    /// <summary>
    /// 六边形内半径（中心到边的距离）
    /// 计算公式: OuterRadius * sqrt(3) / 2
    /// </summary>
    public static float InnerRadius => OuterRadius * 0.866025404f;

    /// <summary>
    /// 六边形大小（用于坐标转换，等于 OuterRadius）
    /// </summary>
    public static float HexSize => OuterRadius;

    /// <summary>
    /// 六边形朝向（true = Pointy-top 尖顶朝上, false = Flat-top 平顶朝上）
    /// </summary>
    public static bool IsPointyTop = true;

    /// <summary>
    /// 六边形水平间距
    /// </summary>
    public static float HorizontalSpacing => IsPointyTop ? InnerRadius * 2f : OuterRadius * 1.5f;

    /// <summary>
    /// 六边形垂直间距
    /// </summary>
    public static float VerticalSpacing => IsPointyTop ? OuterRadius * 1.5f : InnerRadius * 2f;

    /// <summary>
    /// 设置六边形大小
    /// </summary>
    public static void SetSize(float outerRadius)
    {
        OuterRadius = outerRadius;
    }

    /// <summary>
    /// 设置六边形朝向
    /// </summary>
    public static void SetOrientation(bool pointyTop)
    {
        IsPointyTop = pointyTop;
    }

    #region Pointy-top 六边形顶点偏移

    /// <summary>
    /// Pointy-top 六边形的6个顶点方向（相对于中心）
    /// </summary>
    public static readonly Vector3[] PointyCorners = new Vector3[]
    {
        new Vector3(0f, 0f, 1f),                    // 上
        new Vector3(0.866025404f, 0f, 0.5f),        // 右上
        new Vector3(0.866025404f, 0f, -0.5f),       // 右下
        new Vector3(0f, 0f, -1f),                   // 下
        new Vector3(-0.866025404f, 0f, -0.5f),      // 左下
        new Vector3(-0.866025404f, 0f, 0.5f),       // 左上
    };

    /// <summary>
    /// Flat-top 六边形的6个顶点方向（相对于中心）
    /// </summary>
    public static readonly Vector3[] FlatCorners = new Vector3[]
    {
        new Vector3(1f, 0f, 0f),                    // 右
        new Vector3(0.5f, 0f, 0.866025404f),        // 右上
        new Vector3(-0.5f, 0f, 0.866025404f),       // 左上
        new Vector3(-1f, 0f, 0f),                   // 左
        new Vector3(-0.5f, 0f, -0.866025404f),      // 左下
        new Vector3(0.5f, 0f, -0.866025404f),       // 右下
    };

    /// <summary>
    /// 获取六边形第 i 个顶点的世界偏移
    /// </summary>
    public static Vector3 GetCorner(int index)
    {
        var corners = IsPointyTop ? PointyCorners : FlatCorners;
        return corners[index % 6] * OuterRadius;
    }

    #endregion
}
