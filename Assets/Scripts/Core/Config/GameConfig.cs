using System.Collections.Generic;

/// <summary>
/// 游戏配置中心 - 替代 Luban 的静态配置类
/// 48 小时极限开发专用，快速易用
/// </summary>
public static class GameConfig
{
    // ========== 地图配置 ==========

    /// <summary>
    /// 地图半径（六边形格子数）
    /// </summary>
    public const int MAP_RADIUS = 100;

    /// <summary>
    /// 地形种子（-1 = 随机）
    /// </summary>
    public const int TERRAIN_SEED = -1;

    // ========== 城市配置 ==========

    /// <summary>
    /// 城市最小间距（六边形格子数）
    /// </summary>
    public const int CITY_MIN_DISTANCE = 3;

    /// <summary>
    /// 城市节点预制体地址（Addressables）
    /// </summary>
    public const string CITY_NODE_PREFAB = "CityNode";

    // ========== 节点配置（替代 Luban TbNode） ==========

    /// <summary>
    /// 节点配置数据（替代 cfg.Node）
    /// </summary>
    public class NodeConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NodeLevel { get; set; }
        public int NodeIncome { get; set; }
        public int NodeCost { get; set; }

        public NodeConfig(int id, string name, string nodeLevel, int nodeIncome, int nodeCost)
        {
            Id = id;
            Name = name;
            NodeLevel = nodeLevel;
            NodeIncome = nodeIncome;
            NodeCost = nodeCost;
        }
    }

    /// <summary>
    /// 所有节点配置列表
    /// </summary>
    public static readonly List<NodeConfig> NodeConfigs = new List<NodeConfig>
    {
        new NodeConfig(1001, "小型机场", "1级", 12000, 20000),
        new NodeConfig(1002, "中型机场", "2级", 30000, 50000),
        new NodeConfig(1003, "大型机场", "3级", 55000, 100000),
        new NodeConfig(1004, "国际机场", "4级", 85000, 200000),
        new NodeConfig(1005, "枢纽机场", "5级", 120000, 300000),
    };

    /// <summary>
    /// 节点配置字典（按 ID 索引，运行时自动构建）
    /// </summary>
    private static Dictionary<int, NodeConfig> nodeConfigDict;

    /// <summary>
    /// 获取节点配置（按 ID）
    /// </summary>
    public static NodeConfig GetNodeConfig(int id)
    {
        // 懒加载字典
        if (nodeConfigDict == null)
        {
            nodeConfigDict = new Dictionary<int, NodeConfig>();
            foreach (var config in NodeConfigs)
            {
                nodeConfigDict[config.Id] = config;
            }
        }

        nodeConfigDict.TryGetValue(id, out var result);
        return result;
    }

    /// <summary>
    /// 获取随机节点配置 ID
    /// </summary>
    public static int GetRandomNodeConfigId()
    {
        return NodeConfigs[UnityEngine.Random.Range(0, NodeConfigs.Count)].Id;
    }

    /// <summary>
    /// 根据等级获取节点配置 ID
    /// 等级 1-5 对应 ID 1001-1005
    /// </summary>
    /// <param name="level">节点等级 (1-5)</param>
    /// <returns>对应的节点配置 ID</returns>
    public static int GetNodeConfigIdByLevel(int level)
    {
        // 确保等级在有效范围内
        level = UnityEngine.Mathf.Clamp(level, 1, 5);
        return 1000 + level;
    }

    // ========== 地形寻路配置 ==========

    /// <summary>
    /// 地形移动成本配置（数据驱动，可调整路径优先级）
    /// 值越小越优先通过，0 = 不可通过
    /// </summary>
    public static class TerrainCosts
    {
        // 基础移动成本（A* 寻路权重）
        public static float DeepWater = 3.0f;       // 不可通过
        public static float ShallowWater = 2.2f;  // 水上成本高
        public static float Coast = 1.15f;        // 海岸
        public static float Plain = 1.0f;         // 平原（基准）
        public static float Hill = 1.25f;         // 丘陵
        public static float Mountain = 1.8f;      // 山地
        public static float HighMountain = 0f;    // 不可通过

        // 建造成本倍率（影响航线建造费用）
        public static float BuildDeepWater = 1f;
        public static float BuildShallowWater = 1.35f;
        public static float BuildCoast = 1.15f;
        public static float BuildPlain = 1.0f;
        public static float BuildHill = 1.2f;
        public static float BuildMountain = 1.7f;
        public static float BuildHighMountain = 0f;
    }

    /// <summary>
    /// 获取地形移动成本（用于 A* 寻路）
    /// </summary>
    public static float GetTerrainMoveCost(TerrainType type)
    {
        return type switch
        {
            TerrainType.DeepWater => TerrainCosts.DeepWater,
            TerrainType.ShallowWater => TerrainCosts.ShallowWater,
            TerrainType.Coast => TerrainCosts.Coast,
            TerrainType.Plain => TerrainCosts.Plain,
            TerrainType.Hill => TerrainCosts.Hill,
            TerrainType.Mountain => TerrainCosts.Mountain,
            TerrainType.HighMountain => TerrainCosts.HighMountain,
            _ => 1.0f
        };
    }

    /// <summary>
    /// 获取地形建造成本倍率
    /// </summary>
    public static float GetTerrainBuildCost(TerrainType type)
    {
        return type switch
        {
            TerrainType.DeepWater => TerrainCosts.BuildDeepWater,
            TerrainType.ShallowWater => TerrainCosts.BuildShallowWater,
            TerrainType.Coast => TerrainCosts.BuildCoast,
            TerrainType.Plain => TerrainCosts.BuildPlain,
            TerrainType.Hill => TerrainCosts.BuildHill,
            TerrainType.Mountain => TerrainCosts.BuildMountain,
            TerrainType.HighMountain => TerrainCosts.BuildHighMountain,
            _ => 1.0f
        };
    }

    /// <summary>
    /// 地形是否可通过（移动成本 > 0）
    /// </summary>
    public static bool IsTerrainPassable(TerrainType type)
    {
        return GetTerrainMoveCost(type) > 0f;
    }
}

