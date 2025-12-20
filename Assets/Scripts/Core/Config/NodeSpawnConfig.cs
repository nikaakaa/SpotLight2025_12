using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 节点生成配置
/// 簇状生长法：越高级的节点越稀有，生成位置越远
/// </summary>
public static class NodeSpawnConfig
{
    // ========== 基础配置 ==========

    /// <summary>
    /// 节点最小间距（六边形格子数）
    /// </summary>
    public const int MinNodeDistance = 3;

    /// <summary>
    /// 初始回合生成的种子节点数量
    /// </summary>
    public const int InitialSeedCount = 5;

    /// <summary>
    /// 初始种子节点的最小间距
    /// </summary>
    public const int InitialSeedMinDistance = 8;

    /// <summary>
    /// 每回合生成的节点数量（基础值）
    /// </summary>
    public const int NodesPerRound = 1;

    // ========== 生成距离配置（基于地图大小动态计算） ==========

    /// <summary>
    /// 各等级节点的生成距离比例 [等级] = (最小比例, 最大比例)
    /// 比例相对于地图半径 GameConfig.MAP_RADIUS
    /// 越高级的节点，生成位置离已有节点越远
    /// </summary>
    private static readonly Dictionary<int, (float minRatio, float maxRatio)> LevelDistanceRatio = new()
    {
        { 1, (0.03f, 0.05f) },   // lv1：3-5% 地图半径（近距离）
        { 2, (0.03f, 0.08f) },   // lv2：3-8% 地图半径
        { 3, (0.08f, 0.15f) },   // lv3：8-15% 地图半径（中距离）
        { 4, (0.20f, 0.40f) },   // lv4：20-40% 地图半径（远距离）
        { 5, (0.50f, 0.80f) },   // lv5：50-80% 地图半径（横跨地图）
    };

    /// <summary>
    /// 节点生成距离上限（距离已有节点不得超过此值）
    /// </summary>
    public const int MaxSpawnDistanceFromNode = 25;

    /// <summary>
    /// 获取指定等级的生成距离范围（动态计算，基于地图大小）
    /// </summary>
    public static (int minDist, int maxDist) GetDistanceRange(int level)
    {
        int mapRadius = GameConfig.MAP_RADIUS;

        if (LevelDistanceRatio.TryGetValue(level, out var ratio))
        {
            int minDist = Mathf.Max(3, Mathf.RoundToInt(mapRadius * ratio.minRatio));
            int maxDist = Mathf.Max(minDist + 1, Mathf.RoundToInt(mapRadius * ratio.maxRatio));

            // 限制最大距离不超过 MaxSpawnDistanceFromNode
            maxDist = Mathf.Min(maxDist, MaxSpawnDistanceFromNode);

            return (minDist, maxDist);
        }

        // 默认：最小间距
        return (MinNodeDistance, Mathf.Min(MinNodeDistance + 2, MaxSpawnDistanceFromNode));
    }

    // ========== 等级生成权重配置 ==========

    /// <summary>
    /// 各回合阶段的等级生成权重
    /// 键：回合数上限，值：lv1-lv5 的生成权重数组
    /// </summary>
    public static readonly SortedDictionary<int, float[]> RoundLevelWeights = new()
    {
        // 回合 1-3：只有 lv1 和少量 lv2
        { 3, new[] { 0.70f, 0.25f, 0.05f, 0f, 0f } },

        // 回合 4-6：lv3 开始出现
        { 6, new[] { 0.40f, 0.35f, 0.20f, 0.05f, 0f } },

        // 回合 7-10：lv4 开始出现
        { 10, new[] { 0.20f, 0.30f, 0.30f, 0.15f, 0.05f } },

        // 回合 11-15：lv5 出现，高级节点增多
        { 15, new[] { 0.15f, 0.25f, 0.30f, 0.20f, 0.10f } },

        // 回合 16+：后期平衡分布
        { int.MaxValue, new[] { 0.10f, 0.20f, 0.30f, 0.25f, 0.15f } },
    };

    // ========== 生成类型概率配置 ==========

    /// <summary>
    /// 生成类型枚举
    /// </summary>
    public enum SpawnType
    {
        ClusterEdge,    // 城市群边缘（默认）
        ClusterBridge,  // 城市群桥接（稍远）
        Isolated,       // 孤立节点（随机位置）
    }

    /// <summary>
    /// 各生成类型的基础概率（低级节点使用）
    /// </summary>
    public static readonly Dictionary<SpawnType, float> BaseSpawnTypeProbability = new()
    {
        { SpawnType.ClusterEdge, 0.75f },   // 75%：城市群边缘扩展
        { SpawnType.ClusterBridge, 0.20f }, // 20%：城市群桥接
        { SpawnType.Isolated, 0.05f },      // 5%：孤立节点
    };

    /// <summary>
    /// 高级节点（lv4+）的生成类型概率覆盖
    /// 高级节点更倾向于远离现有城市群
    /// </summary>
    public static readonly Dictionary<SpawnType, float> HighLevelSpawnTypeProbability = new()
    {
        { SpawnType.ClusterEdge, 0.20f },   // 20%：靠近现有节点
        { SpawnType.ClusterBridge, 0.60f }, // 60%：在城市群之间
        { SpawnType.Isolated, 0.20f },      // 20%：完全独立
    };

    // ========== 密度控制配置 ==========

    /// <summary>
    /// 密度检测半径（六边形格子数）
    /// </summary>
    public const int DensityCheckRadius = 5;

    /// <summary>
    /// 理想密度范围（周围节点数量）
    /// </summary>
    public static readonly (int min, int max) IdealDensityRange = (2, 4);

    /// <summary>
    /// 密度过高阈值（超过此值大幅降低生成概率）
    /// </summary>
    public const int MaxDensityThreshold = 6;

    // ========== 辅助方法 ==========

    /// <summary>
    /// 获取指定回合的等级生成权重
    /// </summary>
    public static float[] GetLevelWeights(int round)
    {
        foreach (var kvp in RoundLevelWeights)
        {
            if (round <= kvp.Key)
                return kvp.Value;
        }
        return RoundLevelWeights[int.MaxValue];
    }

    /// <summary>
    /// 根据权重随机选择节点等级
    /// </summary>
    /// <param name="round">当前回合数</param>
    /// <returns>节点等级 1-5</returns>
    public static int GetRandomLevel(int round)
    {
        float[] weights = GetLevelWeights(round);
        float total = 0f;
        foreach (var w in weights) total += w;

        float roll = Random.value * total;
        float cumulative = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return i + 1; // 等级从 1 开始
        }

        return 1; // 默认返回 lv1
    }

    /// <summary>
    /// 获取指定等级的生成类型概率
    /// </summary>
    public static Dictionary<SpawnType, float> GetSpawnTypeProbabilities(int level)
    {
        return level >= 4 ? HighLevelSpawnTypeProbability : BaseSpawnTypeProbability;
    }

    /// <summary>
    /// 根据概率随机选择生成类型
    /// </summary>
    public static SpawnType GetRandomSpawnType(int level)
    {
        var probs = GetSpawnTypeProbabilities(level);

        float total = 0f;
        foreach (var p in probs.Values) total += p;

        float roll = Random.value * total;
        float cumulative = 0f;

        foreach (var kvp in probs)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative)
                return kvp.Key;
        }

        return SpawnType.ClusterEdge;
    }

    /// <summary>
    /// 计算位置的密度分数（0-1，越高越适合生成）
    /// </summary>
    public static float CalculateDensityScore(int nearbyNodeCount)
    {
        if (nearbyNodeCount == 0) return 0.3f;                           // 孤立：低概率但可行
        if (nearbyNodeCount <= IdealDensityRange.min) return 0.8f;       // 边缘：适中
        if (nearbyNodeCount <= IdealDensityRange.max) return 1.0f;       // 理想：最高
        if (nearbyNodeCount <= MaxDensityThreshold) return 0.4f;         // 稍密：降低
        return 0.1f;                                                      // 过密：几乎不选
    }
}
