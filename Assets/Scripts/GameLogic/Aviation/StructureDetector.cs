using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 结构检测器 - 从航线网络中检测所有结构类型
/// 
/// 检测算法：
/// - 环形结构：DFS 找环
/// - 单线结构：度数为1的节点对
/// - 放射结构：度数 >= 3 的枢纽节点
/// </summary>
public static class StructureDetector
{
    private static int structureIdCounter = 0;

    /// <summary>
    /// 一次性检测所有结构类型
    /// </summary>
    public static AllStructures DetectAll(AviationSystem system)
    {
        if (system == null)
        {
            Debug.LogWarning("[StructureDetector] AviationSystem 为空");
            return new AllStructures();
        }

        structureIdCounter = 0;
        var result = new AllStructures();

        // 按顺序检测各类结构
        DetectRings(system, result.Rings);
        DetectRadials(system, result.Radials);
        DetectSingleLines(system, result.SingleLines);

        Debug.Log($"[StructureDetector] 检测完成: 环形={result.Rings.Count}, 放射={result.Radials.Count}, 单线={result.SingleLines.Count}");
        return result;
    }

    #region 环形结构检测

    /// <summary>
    /// 检测所有环形结构（使用 DFS 找环）
    /// </summary>
    public static void DetectRings(AviationSystem system, List<RingStructure> rings)
    {
        if (system == null || system.aviationNodeDict.Count == 0) return;

        var visited = new HashSet<int>();
        var edgesInRings = new HashSet<int>(); // 已被环使用的边

        foreach (var kvp in system.aviationNodeDict)
        {
            var startNode = kvp.Value;
            if (visited.Contains(startNode.nodeIndex)) continue;

            // 从每个未访问节点开始 DFS 找环
            FindCyclesDFS(startNode, null, new List<AviationNode>(), new List<AviationEdge>(),
                          visited, edgesInRings, rings);
        }

        // 验证枢纽要求
        foreach (var ring in rings)
        {
            ValidateRingHubRequirement(ring);
        }
    }

    /// <summary>
    /// DFS 找环 - 检测从当前节点开始的所有环
    /// </summary>
    private static void FindCyclesDFS(
        AviationNode current,
        AviationEdge fromEdge,
        List<AviationNode> path,
        List<AviationEdge> pathEdges,
        HashSet<int> globalVisited,
        HashSet<int> edgesInRings,
        List<RingStructure> rings)
    {
        // 检查是否形成环（回到路径中的某个节点）
        int indexInPath = path.FindIndex(n => n.nodeIndex == current.nodeIndex);
        if (indexInPath >= 0)
        {
            // 找到环！提取环的部分
            var ringNodes = path.GetRange(indexInPath, path.Count - indexInPath);
            var ringEdges = pathEdges.GetRange(indexInPath, pathEdges.Count - indexInPath);

            // 确保环至少有3个节点，且边未被使用
            if (ringNodes.Count >= 3 && !HasOverlapWithExistingRings(ringEdges, edgesInRings))
            {
                var ring = new RingStructure
                {
                    StructureId = ++structureIdCounter
                };
                ring.Nodes.AddRange(ringNodes);
                ring.Edges.AddRange(ringEdges);

                // 检查是否包含枢纽
                ring.ContainsHub = HasHubNode(ringNodes);

                rings.Add(ring);

                // 标记边已被使用
                foreach (var edge in ringEdges)
                {
                    edgesInRings.Add(edge.edgeIndex);
                }

                Debug.Log($"[StructureDetector] 发现环形结构: {ringNodes.Count} 个节点");
            }
            return;
        }

        // 继续 DFS
        path.Add(current);

        foreach (var edge in current.edges)
        {
            // 跳过来时的边
            if (fromEdge != null && edge.edgeIndex == fromEdge.edgeIndex) continue;

            // 获取邻居节点
            var neighbor = edge.fromNode == current ? edge.toNode : edge.fromNode;

            // 添加边到路径
            pathEdges.Add(edge);

            // 递归
            FindCyclesDFS(neighbor, edge, path, pathEdges, globalVisited, edgesInRings, rings);

            // 回溯
            pathEdges.RemoveAt(pathEdges.Count - 1);
        }

        path.RemoveAt(path.Count - 1);
        globalVisited.Add(current.nodeIndex);
    }

    /// <summary>
    /// 检查边是否与已有环重叠
    /// </summary>
    private static bool HasOverlapWithExistingRings(List<AviationEdge> edges, HashSet<int> edgesInRings)
    {
        foreach (var edge in edges)
        {
            if (edgesInRings.Contains(edge.edgeIndex)) return true;
        }
        return false;
    }

    /// <summary>
    /// 验证环形结构的枢纽要求（每4节点1枢纽）
    /// </summary>
    private static void ValidateRingHubRequirement(RingStructure ring)
    {
        int nodeCount = ring.Nodes.Count;
        int requiredHubs = (nodeCount + PlayerData.RING_HUB_REQUIRED_PER_NODES - 1) / PlayerData.RING_HUB_REQUIRED_PER_NODES;
        int actualHubs = CountHubNodes(ring.Nodes);

        ring.MeetsHubRequirement = actualHubs >= requiredHubs;

        if (!ring.MeetsHubRequirement)
        {
            // 惩罚：每少一个枢纽，倍率降低 10%
            int deficit = requiredHubs - actualHubs;
            ring.HubPenaltyMultiplier = Mathf.Max(0.5f, 1f - deficit * 0.1f);
            Debug.Log($"[StructureDetector] 环形结构枢纽不足: 需要{requiredHubs}, 实际{actualHubs}, 惩罚倍率={ring.HubPenaltyMultiplier}");
        }
    }

    #endregion

    #region 放射结构检测

    /// <summary>
    /// 检测所有放射结构（度数 >= 3 的枢纽节点）
    /// </summary>
    public static void DetectRadials(AviationSystem system, List<RadialStructure> radials)
    {
        if (system == null) return;

        foreach (var kvp in system.aviationNodeDict)
        {
            var node = kvp.Value;

            // 度数 >= 3 视为枢纽
            if (node.edges.Count >= 3)
            {
                var radial = new RadialStructure
                {
                    StructureId = ++structureIdCounter,
                    HubNode = node,
                    ContainsHub = true
                };

                // 添加枢纽本身
                radial.Nodes.Add(node);

                // 添加所有连接的边和邻居节点
                foreach (var edge in node.edges)
                {
                    radial.Edges.Add(edge);
                    var neighbor = edge.fromNode == node ? edge.toNode : edge.fromNode;
                    if (!radial.Nodes.Contains(neighbor))
                    {
                        radial.Nodes.Add(neighbor);
                    }
                }

                radials.Add(radial);
                Debug.Log($"[StructureDetector] 发现放射结构: 枢纽={node.nodeData?.Name}, 辐射数={node.edges.Count}");
            }
        }
    }

    #endregion
    #region 单线结构检测

    /// <summary>
    /// 检测所有单线结构（每条边都是一个单线）
    /// </summary>
    public static void DetectSingleLines(AviationSystem system, List<SingleLineStructure> singleLines)
    {
        if (system == null) return;

        foreach (var kvp in system.aviationEdgeDict)
        {
            var edge = kvp.Value;

            var singleLine = new SingleLineStructure
            {
                StructureId = ++structureIdCounter,
                ContainsHub = edge.fromNode.edges.Count >= 3 || edge.toNode.edges.Count >= 3
            };

            singleLine.Nodes.Add(edge.fromNode);
            singleLine.Nodes.Add(edge.toNode);
            singleLine.Edges.Add(edge);

            singleLines.Add(singleLine);
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查节点列表中是否包含枢纽节点（度数 >= 3）
    /// </summary>
    private static bool HasHubNode(List<AviationNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.edges.Count >= 3) return true;
        }
        return false;
    }

    /// <summary>
    /// 统计枢纽节点数量
    /// </summary>
    private static int CountHubNodes(List<AviationNode> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (node.edges.Count >= 3) count++;
        }
        return count;
    }

    /// <summary>
    /// 检查节点是否为枢纽（高等级或高度数）
    /// </summary>
    public static bool IsHubNode(AviationNode node)
    {
        if (node == null) return false;

        // 度数 >= 3 视为枢纽
        if (node.edges.Count >= 3) return true;

        // 等级 >= 4 视为枢纽
        if (node.nodeData != null && node.nodeData.Id >= 1004) return true;

        return false;
    }

    #endregion
}
