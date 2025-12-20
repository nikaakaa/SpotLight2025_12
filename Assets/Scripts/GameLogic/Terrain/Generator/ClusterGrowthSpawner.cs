using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 簇状生长节点生成器
/// 实现渐进扩散的节点生成算法：
/// - 大部分节点靠近已有节点生成（城市群扩展）
/// - 越高级的节点越稀有，生成位置越远
/// </summary>
public static class ClusterGrowthSpawner
{
    /// <summary>
    /// 生成一批节点（用于每回合生成）
    /// </summary>
    /// <param name="existingCoords">已存在的节点坐标</param>
    /// <param name="candidateCoords">所有可放置城市的候选坐标</param>
    /// <param name="count">要生成的节点数量</param>
    /// <param name="round">当前回合数</param>
    /// <returns>生成结果列表 (坐标, 节点等级)</returns>
    public static List<(HexCoord coord, int level)> Generate(
        IEnumerable<HexCoord> existingCoords,
        IEnumerable<HexCoord> candidateCoords,
        int count,
        int round)
    {
        var result = new List<(HexCoord, int)>();
        var existing = new HashSet<HexCoord>(existingCoords);
        var candidates = new HashSet<HexCoord>(candidateCoords);

        // 移除已有节点的位置
        candidates.ExceptWith(existing);

        // 第一回合：使用 Poisson Disk 生成均匀分布的种子点
        if (existing.Count == 0)
        {
            return GenerateInitialSeeds(candidates.ToList(), count);
        }

        // 后续回合：簇状生长
        for (int i = 0; i < count; i++)
        {
            // 1. 随机决定节点等级
            int level = NodeSpawnConfig.GetRandomLevel(round);

            // 2. 随机决定生成类型
            var spawnType = NodeSpawnConfig.GetRandomSpawnType(level);

            // 3. 根据等级和类型找到合适的位置
            HexCoord? spawnPos = FindSpawnPosition(
                existing,
                candidates,
                level,
                spawnType
            );

            if (spawnPos.HasValue)
            {
                result.Add((spawnPos.Value, level));
                existing.Add(spawnPos.Value);
                candidates.Remove(spawnPos.Value);

                // 移除新节点周围过近的候选位置
                RemoveNearbyFromCandidates(spawnPos.Value, candidates, NodeSpawnConfig.MinNodeDistance);
            }
        }

        return result;
    }

    /// <summary>
    /// 生成初始种子节点（第一回合使用）
    /// </summary>
    private static List<(HexCoord, int)> GenerateInitialSeeds(List<HexCoord> candidates, int count)
    {
        var result = new List<(HexCoord, int)>();

        // 使用 Poisson Disk 采样生成均匀分布的种子点
        var seedCoords = PoissonDiskSampler.Sample(
            candidates,
            Mathf.Min(count, NodeSpawnConfig.InitialSeedCount),
            NodeSpawnConfig.InitialSeedMinDistance
        );

        // 初始种子全部设为 lv1
        foreach (var coord in seedCoords)
        {
            result.Add((coord, 1));
        }

        return result;
    }

    /// <summary>
    /// 根据等级和生成类型查找合适的生成位置
    /// </summary>
    private static HexCoord? FindSpawnPosition(
        HashSet<HexCoord> existing,
        HashSet<HexCoord> candidates,
        int level,
        NodeSpawnConfig.SpawnType spawnType)
    {
        var (minDist, maxDist) = NodeSpawnConfig.GetDistanceRange(level);

        switch (spawnType)
        {
            case NodeSpawnConfig.SpawnType.ClusterEdge:
                return FindClusterEdgePosition(existing, candidates, minDist, maxDist);

            case NodeSpawnConfig.SpawnType.ClusterBridge:
                return FindClusterBridgePosition(existing, candidates, minDist, maxDist);

            case NodeSpawnConfig.SpawnType.Isolated:
                return FindIsolatedPosition(existing, candidates, minDist);

            default:
                return FindClusterEdgePosition(existing, candidates, minDist, maxDist);
        }
    }

    /// <summary>
    /// 在城市群边缘找位置（靠近已有节点）
    /// </summary>
    private static HexCoord? FindClusterEdgePosition(
        HashSet<HexCoord> existing,
        HashSet<HexCoord> candidates,
        int minDist,
        int maxDist)
    {
        // 收集所有符合距离要求的候选位置
        var validCandidates = new List<(HexCoord coord, float score)>();

        foreach (var candidate in candidates)
        {
            int distToNearest = GetDistanceToNearest(candidate, existing);

            // 检查是否在距离范围内
            if (distToNearest >= minDist && distToNearest <= maxDist)
            {
                // 检查密度
                int nearbyCount = CountNodesInRadius(candidate, existing, NodeSpawnConfig.DensityCheckRadius);
                float densityScore = NodeSpawnConfig.CalculateDensityScore(nearbyCount);

                // 距离分数：越接近 minDist 越好（城市群边缘优先）
                float distanceScore = 1f - (float)(distToNearest - minDist) / (maxDist - minDist + 1);

                float totalScore = densityScore * distanceScore;
                if (totalScore > 0.1f)
                {
                    validCandidates.Add((candidate, totalScore));
                }
            }
        }

        return WeightedRandomSelect(validCandidates);
    }

    /// <summary>
    /// 在城市群之间找位置（桥接位置）
    /// </summary>
    private static HexCoord? FindClusterBridgePosition(
        HashSet<HexCoord> existing,
        HashSet<HexCoord> candidates,
        int minDist,
        int maxDist)
    {
        var validCandidates = new List<(HexCoord coord, float score)>();

        foreach (var candidate in candidates)
        {
            int distToNearest = GetDistanceToNearest(candidate, existing);

            // 桥接位置：在 maxDist 附近，但不能太远
            if (distToNearest >= minDist && distToNearest <= maxDist + 3)
            {
                // 低密度区域优先（避开城市群中心）
                int nearbyCount = CountNodesInRadius(candidate, existing, NodeSpawnConfig.DensityCheckRadius);

                // 桥接位置偏好低密度
                float densityScore = nearbyCount <= 2 ? 1f : (nearbyCount <= 4 ? 0.5f : 0.2f);

                // 距离分数：偏好中远距离
                float distanceScore = Mathf.Clamp01((float)(distToNearest - minDist) / (maxDist - minDist + 1));

                float totalScore = densityScore * distanceScore;
                if (totalScore > 0.1f)
                {
                    validCandidates.Add((candidate, totalScore));
                }
            }
        }

        // 如果找不到桥接位置，回退到边缘位置
        if (validCandidates.Count == 0)
        {
            return FindClusterEdgePosition(existing, candidates, minDist, maxDist);
        }

        return WeightedRandomSelect(validCandidates);
    }

    /// <summary>
    /// 找孤立位置（远离所有现有节点）
    /// </summary>
    private static HexCoord? FindIsolatedPosition(
        HashSet<HexCoord> existing,
        HashSet<HexCoord> candidates,
        int minDist)
    {
        var validCandidates = new List<(HexCoord coord, float score)>();

        foreach (var candidate in candidates)
        {
            int distToNearest = GetDistanceToNearest(candidate, existing);

            // 孤立位置：距离至少是 minDist * 2
            if (distToNearest >= minDist * 2)
            {
                // 越远越好
                float score = Mathf.Min(distToNearest / 20f, 1f);
                validCandidates.Add((candidate, score));
            }
        }

        // 如果找不到足够远的位置，放宽条件
        if (validCandidates.Count == 0)
        {
            foreach (var candidate in candidates)
            {
                int distToNearest = GetDistanceToNearest(candidate, existing);
                if (distToNearest >= minDist)
                {
                    validCandidates.Add((candidate, distToNearest / 10f));
                }
            }
        }

        return WeightedRandomSelect(validCandidates);
    }

    /// <summary>
    /// 根据权重随机选择一个位置
    /// </summary>
    private static HexCoord? WeightedRandomSelect(List<(HexCoord coord, float score)> candidates)
    {
        if (candidates.Count == 0) return null;

        float totalScore = candidates.Sum(c => c.score);
        if (totalScore <= 0) return candidates[Random.Range(0, candidates.Count)].coord;

        float roll = Random.value * totalScore;
        float cumulative = 0f;

        foreach (var (coord, score) in candidates)
        {
            cumulative += score;
            if (roll <= cumulative)
                return coord;
        }

        return candidates[^1].coord;
    }

    /// <summary>
    /// 计算到最近节点的距离
    /// </summary>
    private static int GetDistanceToNearest(HexCoord pos, HashSet<HexCoord> nodes)
    {
        if (nodes.Count == 0) return int.MaxValue;

        int minDist = int.MaxValue;
        foreach (var node in nodes)
        {
            int dist = pos.DistanceTo(node);
            if (dist < minDist)
                minDist = dist;
        }
        return minDist;
    }

    /// <summary>
    /// 统计指定半径内的节点数量
    /// </summary>
    private static int CountNodesInRadius(HexCoord center, HashSet<HexCoord> nodes, int radius)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (center.DistanceTo(node) <= radius)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 从候选列表中移除过近的位置
    /// </summary>
    private static void RemoveNearbyFromCandidates(HexCoord center, HashSet<HexCoord> candidates, int minDistance)
    {
        var toRemove = new List<HexCoord>();
        foreach (var candidate in candidates)
        {
            if (center.DistanceTo(candidate) < minDistance)
                toRemove.Add(candidate);
        }

        foreach (var coord in toRemove)
            candidates.Remove(coord);
    }
}
