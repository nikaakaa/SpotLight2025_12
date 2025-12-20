/// <summary>
/// 地形类型枚举
/// </summary>
public enum TerrainType
{
    DeepWater = 0,    // 深海 - 不可通过
    ShallowWater = 1, // 浅海 - 可通过，高成本
    Coast = 2,        // 海岸/沙滩 - 可通过
    Plain = 3,        // 平原 - 基础成本
    Hill = 4,         // 丘陵 - 稍高成本
    Mountain = 5,     // 山地 - 高成本
    HighMountain = 6  // 高山 - 不可通过
}

/// <summary>
/// TerrainType 扩展方法
/// </summary>
public static class TerrainTypeExtensions
{
    /// <summary>
    /// 是否可通过（航线可以经过）
    /// </summary>
    public static bool IsPassable(this TerrainType type)
    {
        return type switch
        {
            TerrainType.DeepWater => false,
            TerrainType.HighMountain => false,
            _ => true
        };
    }

    /// <summary>
    /// 移动成本（用于 A* 寻路）
    /// </summary>
    public static float GetMoveCost(this TerrainType type)
    {
        return type switch
        {
            TerrainType.DeepWater => 0f,      // 不可通过
            TerrainType.ShallowWater => 2.0f, // 水上航线成本高
            TerrainType.Coast => 1.2f,
            TerrainType.Plain => 1.0f,        // 基准成本
            TerrainType.Hill => 1.3f,
            TerrainType.Mountain => 1.8f,
            TerrainType.HighMountain => 0f,   // 不可通过
            _ => 1.0f
        };
    }

    /// <summary>
    /// 建造成本倍率
    /// </summary>
    public static float GetBuildCostMultiplier(this TerrainType type)
    {
        return type switch
        {
            TerrainType.DeepWater => 0f,      // 不可建造
            TerrainType.ShallowWater => 1.5f, // 水上建造贵
            TerrainType.Coast => 1.2f,
            TerrainType.Plain => 1.0f,        // 基准
            TerrainType.Hill => 1.3f,
            TerrainType.Mountain => 2.0f,     // 山区建造贵
            TerrainType.HighMountain => 0f,   // 不可建造
            _ => 1.0f
        };
    }

    /// <summary>
    /// 是否可以放置城市节点
    /// </summary>
    public static bool CanPlaceCity(this TerrainType type)
    {
        return type switch
        {
            TerrainType.Plain => true,
            TerrainType.Hill => true,
            TerrainType.Coast => true,
            _ => false
        };
    }

    /// <summary>
    /// 获取地形颜色（用于可视化）
    /// </summary>
    public static UnityEngine.Color GetColor(this TerrainType type)
    {
        return type switch
        {
            TerrainType.DeepWater => new UnityEngine.Color(0.1f, 0.2f, 0.5f),
            TerrainType.ShallowWater => new UnityEngine.Color(0.2f, 0.4f, 0.7f),
            TerrainType.Coast => new UnityEngine.Color(0.9f, 0.85f, 0.6f),
            TerrainType.Plain => new UnityEngine.Color(0.4f, 0.7f, 0.3f),
            TerrainType.Hill => new UnityEngine.Color(0.5f, 0.6f, 0.3f),
            TerrainType.Mountain => new UnityEngine.Color(0.5f, 0.45f, 0.4f),
            TerrainType.HighMountain => new UnityEngine.Color(0.9f, 0.9f, 0.95f),
            _ => UnityEngine.Color.magenta
        };
    }

    /// <summary>
    /// 获取地形名称
    /// </summary>
    public static string GetName(this TerrainType type)
    {
        return type switch
        {
            TerrainType.DeepWater => "深海",
            TerrainType.ShallowWater => "浅海",
            TerrainType.Coast => "海岸",
            TerrainType.Plain => "平原",
            TerrainType.Hill => "丘陵",
            TerrainType.Mountain => "山地",
            TerrainType.HighMountain => "高山",
            _ => "未知"
        };
    }
}
