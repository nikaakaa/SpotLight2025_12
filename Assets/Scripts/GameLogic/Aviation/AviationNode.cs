using System.Collections.Generic;
using cfg;

/// <summary>
/// 航空节点运行时数据
/// 配置层: cfg.Node
/// 运行时层: AviationNode
/// 显示层: CityNode（单向引用 AviationNode）
/// </summary>
public class AviationNode
{
    public int nodeIndex; // 在 AviationSystem 中的索引
    public cfg.Node nodeData; // 配置数据
    public HexCoord hexCoord; // 六边形坐标
    
    // 该节点连接的所有边
    public List<AviationEdge> edges = new List<AviationEdge>();

    public AviationNode(int nodeId, int index)
    {
        nodeIndex = index;
        nodeData = TableLoader.Tables.TbNode.Get(nodeId);
    }

    /// <summary>
    /// 设置六边形坐标
    /// </summary>
    public void SetHexCoord(HexCoord coord)
    {
        hexCoord = coord;
    }

    /// <summary>
    /// 添加边到节点
    /// </summary>
    public void AddEdge(AviationEdge edge)
    {
        if (!edges.Contains(edge))
        {
            edges.Add(edge);
        }
    }

    /// <summary>
    /// 移除边
    /// </summary>
    public void RemoveEdge(AviationEdge edge)
    {
        edges.Remove(edge);
    }

    /// <summary>
    /// 获取连接的节点列表
    /// </summary>
    public List<AviationNode> GetConnectedNodes()
    {
        var result = new List<AviationNode>();
        foreach (var edge in edges)
        {
            if (edge.fromNode == this)
                result.Add(edge.toNode);
            else
                result.Add(edge.fromNode);
        }
        return result;
    }

    /// <summary>
    /// 是否已连接到指定节点
    /// </summary>
    public bool IsConnectedTo(AviationNode other)
    {
        foreach (var edge in edges)
        {
            if (edge.fromNode == other || edge.toNode == other)
                return true;
        }
        return false;
    }
}
