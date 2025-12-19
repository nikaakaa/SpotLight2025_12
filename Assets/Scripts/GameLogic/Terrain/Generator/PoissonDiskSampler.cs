using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Poisson Disk 采样器 - 用于在六边形网格上均匀分布点
/// 确保任意两点之间的最小距离
/// </summary>
public static class PoissonDiskSampler
{
    /// <summary>
    /// 在六边形网格上进行 Poisson Disk 采样
    /// </summary>
    /// <param name="candidates">候选坐标列表（通常是可放置城市的格子）</param>
    /// <param name="count">需要生成的点数</param>
    /// <param name="minDistance">最小间隔距离（六边形格子数）</param>
    /// <param name="seed">随机种子（-1 表示使用当前随机状态）</param>
    /// <returns>采样结果坐标列表</returns>
    public static List<HexCoord> Sample(List<HexCoord> candidates, int count, int minDistance = 3, int seed = -1)
    {
        if (candidates == null || candidates.Count == 0)
            return new List<HexCoord>();

        // 可选：设置随机种子
        if (seed >= 0)
        {
            Random.InitState(seed);
        }

        var result = new List<HexCoord>();
        var candidateSet = new HashSet<HexCoord>(candidates);
        var activeList = new List<HexCoord>(candidates);

        // 随机打乱候选列表
        ShuffleList(activeList);

        while (result.Count < count && activeList.Count > 0)
        {
            // 从列表末尾取一个候选点
            int lastIndex = activeList.Count - 1;
            HexCoord candidate = activeList[lastIndex];
            activeList.RemoveAt(lastIndex);

            // 检查是否可以放置
            if (IsValidPlacement(candidate, result, minDistance))
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    /// <summary>
    /// 检查新点是否满足最小距离要求
    /// </summary>
    private static bool IsValidPlacement(HexCoord newPoint, List<HexCoord> existingPoints, int minDistance)
    {
        foreach (var existing in existingPoints)
        {
            if (newPoint.DistanceTo(existing) < minDistance)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Fisher-Yates 随机打乱列表
    /// </summary>
    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
