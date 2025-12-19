using System;

/// <summary>
/// 单个六边形格子的地形数据
/// </summary>
[Serializable]
public class TerrainCell
{
    /// <summary>
    /// 地形类型
    /// </summary>
    public TerrainType type;

    /// <summary>
    /// 原始高度值 (0.0 - 1.0)
    /// </summary>
    public float height;

    /// <summary>
    /// 是否可通过
    /// </summary>
    public bool IsPassable => type.IsPassable();

    /// <summary>
    /// 移动成本（用于 A* 寻路）
    /// </summary>
    public float MoveCost => type.GetMoveCost();

    /// <summary>
    /// 建造成本倍率
    /// </summary>
    public float BuildCostMultiplier => type.GetBuildCostMultiplier();

    /// <summary>
    /// 是否可以放置城市
    /// </summary>
    public bool CanPlaceCity => type.CanPlaceCity();

    public TerrainCell()
    {
        type = TerrainType.Plain;
        height = 0.5f;
    }

    public TerrainCell(TerrainType type, float height)
    {
        this.type = type;
        this.height = height;
    }

    public override string ToString()
    {
        return $"TerrainCell({type.GetName()}, h={height:F2})";
    }
}
