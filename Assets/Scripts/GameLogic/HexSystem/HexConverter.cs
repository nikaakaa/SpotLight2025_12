using UnityEngine;

/// <summary>
/// 六边形坐标与世界坐标的转换器
/// </summary>
public static class HexConverter
{
    #region 六边形坐标 → 世界坐标

    /// <summary>
    /// 将六边形坐标转换为世界坐标（XZ平面）
    /// </summary>
    public static Vector3 HexToWorld(HexCoord hex)
    {
        return HexToWorld(hex.q, hex.r);
    }

    /// <summary>
    /// 将六边形坐标 (q, r) 转换为世界坐标
    /// </summary>
    public static Vector3 HexToWorld(int q, int r)
    {
        float x, z;
        float size = HexMetrics.HexSize;

        if (HexMetrics.IsPointyTop)
        {
            // Pointy-top 布局
            x = size * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
            z = size * (3f / 2f * r);
        }
        else
        {
            // Flat-top 布局
            x = size * (3f / 2f * q);
            z = size * (Mathf.Sqrt(3f) / 2f * q + Mathf.Sqrt(3f) * r);
        }

        return new Vector3(x, 0f, z);
    }

    /// <summary>
    /// 将六边形坐标转换为世界坐标（带高度）
    /// </summary>
    public static Vector3 HexToWorld(HexCoord hex, float height)
    {
        var pos = HexToWorld(hex);
        pos.y = height;
        return pos;
    }

    #endregion

    #region 世界坐标 → 六边形坐标

    /// <summary>
    /// 将世界坐标转换为六边形坐标
    /// </summary>
    public static HexCoord WorldToHex(Vector3 worldPosition)
    {
        return WorldToHex(worldPosition.x, worldPosition.z);
    }

    /// <summary>
    /// 将世界坐标 (x, z) 转换为六边形坐标
    /// </summary>
    public static HexCoord WorldToHex(float x, float z)
    {
        float q, r;
        float size = HexMetrics.HexSize;

        if (HexMetrics.IsPointyTop)
        {
            // Pointy-top 布局
            q = (Mathf.Sqrt(3f) / 3f * x - 1f / 3f * z) / size;
            r = (2f / 3f * z) / size;
        }
        else
        {
            // Flat-top 布局
            q = (2f / 3f * x) / size;
            r = (-1f / 3f * x + Mathf.Sqrt(3f) / 3f * z) / size;
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

    #region Offset坐标转换（可选）

    /// <summary>
    /// Offset坐标转 Axial坐标 (Odd-r 布局)
    /// </summary>
    public static HexCoord OffsetToAxial_OddR(int col, int row)
    {
        int q = col - (row - (row & 1)) / 2;
        int r = row;
        return new HexCoord(q, r);
    }

    /// <summary>
    /// Axial坐标转 Offset坐标 (Odd-r 布局)
    /// </summary>
    public static (int col, int row) AxialToOffset_OddR(HexCoord hex)
    {
        int col = hex.q + (hex.r - (hex.r & 1)) / 2;
        int row = hex.r;
        return (col, row);
    }

    /// <summary>
    /// Offset坐标转 Axial坐标 (Even-r 布局)
    /// </summary>
    public static HexCoord OffsetToAxial_EvenR(int col, int row)
    {
        int q = col - (row + (row & 1)) / 2;
        int r = row;
        return new HexCoord(q, r);
    }

    /// <summary>
    /// Axial坐标转 Offset坐标 (Even-r 布局)
    /// </summary>
    public static (int col, int row) AxialToOffset_EvenR(HexCoord hex)
    {
        int col = hex.q + (hex.r + (hex.r & 1)) / 2;
        int row = hex.r;
        return (col, row);
    }

    #endregion
}
