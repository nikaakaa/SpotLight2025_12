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
    public int cost = 1;
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
    /// 计算两个节点之间的路径
    /// </summary>
    public void CalculatePath()
    {
        if (fromNode == null || toNode == null) return;
        
        // 使用 A* 算法计算最优路径
        pathCoords = fromNode.hexCoord.FindPathAStar(toNode.hexCoord);
        
        // 计算代价（路径长度）
        cost = pathCoords.Count > 0 ? pathCoords.Count - 1 : 0;
    }
}
