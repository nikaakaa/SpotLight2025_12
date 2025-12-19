using System.Collections.Generic;

/// <summary>
/// 航线边运行时数据（连接两个节点）
/// 运行时层: AviationEdge
/// 显示层: EdgeLineView（单向引用 AviationEdge）
/// </summary>
public class AviationEdge
{
    public int edgeIndex; // 在 AviationSystem 中的索引
    public AviationNode fromNode;
    public AviationNode toNode;
    public int cost = 1; // 基础成本（路径格子数）
    public float totalBuildCostMultiplier = 1f; // 地形建造成本倍率总和
    public List<HexCoord> pathCoords = new List<HexCoord>(); // 路径上的六边形坐标

    public AviationEdge(int index, AviationNode from, AviationNode to)
    {
        edgeIndex = index;
        fromNode = from;
        toNode = to;

        // 将边添加到两个节点的边列表中
        fromNode?.AddEdge(this);
        toNode?.AddEdge(this);

        // 自动计算路径
        CalculatePath();
    }

    /// <summary>
    /// 计算两个节点之间的路径（避开已被其他航线占用的格子、其他节点和不可通过地形）
    /// </summary>
    public void CalculatePath()
    {
        if (fromNode == null || toNode == null) return;

        // 清除旧路径的占用记录
        ClearPathOccupation();

        // 使用 A* 算法计算最优路径
        var system = AviationSystem.Instance;
        var terrain = TerrainSystem.Instance;

        if (system != null)
        {
            // 构建障碍物集合：已占用的路径格子 + 其他节点（不含本航线的起点终点）
            var blockedCoords = new HashSet<HexCoord>(system.occupiedPathCoords);

            // 将其他节点加入障碍物（航线不能穿过其他城市）
            foreach (var nodeCoord in system.nodeCoords)
            {
                // 排除本航线的起点和终点
                if (nodeCoord != fromNode.hexCoord && nodeCoord != toNode.hexCoord)
                {
                    blockedCoords.Add(nodeCoord);
                }
            }

            // 将不可通过的地形加入障碍物
            if (terrain != null)
            {
                foreach (var coord in terrain.terrainGrid.AllCoords)
                {
                    if (!terrain.IsPassable(coord))
                    {
                        blockedCoords.Add(coord);
                    }
                }
            }

            // 只有本航线的起点和终点是允许的端点
            var allowedEndpoints = new HashSet<HexCoord>
            {
                fromNode.hexCoord,
                toNode.hexCoord
            };

            // 使用带障碍物避让的 A* 寻路
            // blockedPenalty = float.MaxValue 表示完全禁止经过障碍物
            pathCoords = fromNode.hexCoord.FindPathAStar(
                toNode.hexCoord,
                blockedCoords,              // 障碍物：已占用格子 + 其他节点 + 不可通过地形
                allowedEndpoints,           // 允许通过：仅本航线起点终点
                turnPenalty: 0.3f,
                crossPenalty: 0.001f,
                blockedPenalty: float.MaxValue  // 完全禁止穿过障碍物
            );

            // 注册新路径的占用（不含端点）
            RegisterPathOccupation();
        }
        else
        {
            // 兜底：没有 AviationSystem 时使用普通寻路
            pathCoords = fromNode.hexCoord.FindPathAStar(toNode.hexCoord);
        }

        // 计算代价（考虑地形成本）
        CalculateCost();
    }

    /// <summary>
    /// 计算路径的建造成本（考虑地形成本倍率）
    /// </summary>
    private void CalculateCost()
    {
        if (pathCoords == null || pathCoords.Count == 0)
        {
            cost = 0;
            totalBuildCostMultiplier = 0f;
            return;
        }

        var terrain = TerrainSystem.Instance;
        float totalMultiplier = 0f;

        foreach (var coord in pathCoords)
        {
            float multiplier = terrain?.GetBuildCostMultiplier(coord) ?? 1f;
            totalMultiplier += multiplier;
        }

        // 存储总成本倍率
        totalBuildCostMultiplier = totalMultiplier;
        // 基础成本 = 路径长度 - 1（不含起点）
        cost = pathCoords.Count > 0 ? pathCoords.Count - 1 : 0;
    }

    /// <summary>
    /// 将路径格子注册到占用集合（不含起点和终点）
    /// </summary>
    private void RegisterPathOccupation()
    {
        var system = AviationSystem.Instance;
        if (system == null || pathCoords == null || pathCoords.Count <= 2) return;

        // 跳过首尾（端点是节点，允许其他航线经过）
        for (int i = 1; i < pathCoords.Count - 1; i++)
        {
            system.occupiedPathCoords.Add(pathCoords[i]);
        }
    }

    /// <summary>
    /// 从占用集合中清除本路径的格子
    /// </summary>
    public void ClearPathOccupation()
    {
        var system = AviationSystem.Instance;
        if (system == null || pathCoords == null) return;

        // 清除路径中间的格子（跳过首尾端点）
        for (int i = 1; i < pathCoords.Count - 1; i++)
        {
            system.occupiedPathCoords.Remove(pathCoords[i]);
        }
    }
}
