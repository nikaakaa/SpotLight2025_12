/// <summary>
/// 地形类型枚举
/// </summary>
public enum TerrainType
{
    DeepWater = 0,    // 深海 - 不可通过
    ShallowWater = 1, // 浅海 - 可通过，但成本较高
    Coast = 2,        // 海岸 / 沙滩 - 可通过
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
        return GameConfig.IsTerrainPassable(type);
    }

    /// <summary>
    /// 移动成本（用于 A* 寻路）
    /// </summary>
    public static float GetMoveCost(this TerrainType type)
    {
        return GameConfig.GetTerrainMoveCost(type);
    }

    /// <summary>
    /// 建造成本倍率
    /// </summary>
    public static float GetBuildCostMultiplier(this TerrainType type)
    {
        return GameConfig.GetTerrainBuildCost(type);
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
            // 海洋：偏天空蓝，不偏紫
            TerrainType.DeepWater => new UnityEngine.Color(0.20f, 0.42f, 0.65f),
            TerrainType.ShallowWater => new UnityEngine.Color(0.35f, 0.60f, 0.80f),

            // 海岸：非常淡的沙色
            TerrainType.Coast => new UnityEngine.Color(0.90f, 0.88f, 0.78f),

            // 平原：清新的植被绿（不荧光）
            TerrainType.Plain => new UnityEngine.Color(0.50f, 0.72f, 0.46f),

            // 丘陵：同色相，更暗一点
            TerrainType.Hill => new UnityEngine.Color(0.55f, 0.84f, 0.60f),

            // 山地：偏冷的灰绿 / 灰棕
            TerrainType.Mountain => new UnityEngine.Color(0.58f, 0.56f, 0.52f),

            // 高山：云雪白（不是纯白）
            TerrainType.HighMountain => new UnityEngine.Color(0.92f, 0.94f, 0.96f),

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
