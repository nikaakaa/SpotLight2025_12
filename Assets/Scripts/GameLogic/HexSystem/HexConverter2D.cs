using UnityEngine;

/// <summary>
/// 六边形坐标与世界坐标的转换器（2D版本，使用XY平面，Z=0）
/// </summary>
public static class HexConverter2D
{
    #region 六边形坐标 → 世界坐标

    /// <summary>
    /// 将六边形坐标转换为世界坐标（XY平面，Z=0）
    /// </summary>
    public static Vector3 HexToWorld(HexCoord hex)
    {
        return HexToWorld(hex.q, hex.r);
    }

    /// <summary>
    /// 将六边形坐标 (q, r) 转换为世界坐标（XY平面）
    /// </summary>
    public static Vector3 HexToWorld(int q, int r)
    {
        float x, y;
        float size = HexMetrics.HexSize;

        if (HexMetrics.IsPointyTop)
        {
            // Pointy-top 布局
            x = size * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
            y = size * (3f / 2f * r);
        }
        else
        {
            // Flat-top 布局
            x = size * (3f / 2f * q);
            y = size * (Mathf.Sqrt(3f) / 2f * q + Mathf.Sqrt(3f) * r);
        }

        return new Vector3(x, y, 0f);
    }

    #endregion

    #region 世界坐标 → 六边形坐标

    /// <summary>
    /// 将世界坐标转换为六边形坐标（使用 x, y）
    /// </summary>
    public static HexCoord WorldToHex(Vector3 worldPosition)
    {
        return WorldToHex(worldPosition.x, worldPosition.y);
    }

    /// <summary>
    /// 将世界坐标 (x, y) 转换为六边形坐标
    /// </summary>
    public static HexCoord WorldToHex(float x, float y)
    {
        float q, r;
        float size = HexMetrics.HexSize;

        if (HexMetrics.IsPointyTop)
        {
            // Pointy-top 布局
            q = (Mathf.Sqrt(3f) / 3f * x - 1f / 3f * y) / size;
            r = (2f / 3f * y) / size;
        }
        else
        {
            // Flat-top 布局
            q = (2f / 3f * x) / size;
            r = (-1f / 3f * x + Mathf.Sqrt(3f) / 3f * y) / size;
        }

        return HexRound(q, r);
    }

    /// <summary>
    /// 浮点坐标四舍五入到最近的六边形
    /// </summary>
    private static HexCoord HexRound(float q, float r)
    {
        float s = -q - r;
        return HexCoord.HexRound(q, r, s);
    }

    #endregion

    #region 获取六边形顶点（2D版本）

    /// <summary>
    /// 获取六边形第 i 个顶点的世界坐标偏移（XY平面）
    /// </summary>
    public static Vector3 GetCorner2D(int index)
    {
        float angle_deg = HexMetrics.IsPointyTop 
            ? 60f * index - 30f    // Pointy-top: 从30°开始
            : 60f * index;          // Flat-top: 从0°开始
        
        float angle_rad = Mathf.Deg2Rad * angle_deg;
        return new Vector3(
            HexMetrics.OuterRadius * Mathf.Cos(angle_rad),
            HexMetrics.OuterRadius * Mathf.Sin(angle_rad),
            0f
        );
    }

    /// <summary>
    /// 获取六边形所有顶点的世界坐标
    /// </summary>
    public static Vector3[] GetHexCorners(HexCoord hex)
    {
        Vector3 center = HexToWorld(hex);
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            corners[i] = center + GetCorner2D(i);
        }
        return corners;
    }

    #endregion
}
