using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简单的优先队列实现（用于A*寻路）
/// </summary>
public class SimplePriorityQueue<TElement, TPriority> where TPriority : System.IComparable<TPriority>
{
    private List<(TElement element, TPriority priority)> elements = new List<(TElement, TPriority)>();

    public int Count => elements.Count;

    public void Enqueue(TElement element, TPriority priority)
    {
        elements.Add((element, priority));
    }

    public TElement Dequeue()
    {
        int bestIndex = 0;
        for (int i = 1; i < elements.Count; i++)
        {
            if (elements[i].priority.CompareTo(elements[bestIndex].priority) < 0)
            {
                bestIndex = i;
            }
        }
        TElement bestElement = elements[bestIndex].element;
        elements.RemoveAt(bestIndex);
        return bestElement;
    }
}

/// <summary>
/// 六边形网格管理器
/// 管理网格中的所有六边形单元
/// </summary>
/// <typeparam name="T">网格单元存储的数据类型</typeparam>
public class HexGrid<T> where T : class
{
    /// <summary>
    /// 存储所有六边形单元的字典
    /// </summary>
    private readonly Dictionary<HexCoord, T> cells = new Dictionary<HexCoord, T>();

    /// <summary>
    /// 网格中的单元数量
    /// </summary>
    public int Count => cells.Count;

    /// <summary>
    /// 所有坐标
    /// </summary>
    public IEnumerable<HexCoord> AllCoords => cells.Keys;

    /// <summary>
    /// 所有单元
    /// </summary>
    public IEnumerable<T> AllCells => cells.Values;

    #region 基本操作

    /// <summary>
    /// 获取指定坐标的单元
    /// </summary>
    public T GetCell(HexCoord coord)
    {
        cells.TryGetValue(coord, out var cell);
        return cell;
    }

    /// <summary>
    /// 获取指定坐标的单元
    /// </summary>
    public T this[HexCoord coord] => GetCell(coord);

    /// <summary>
    /// 设置指定坐标的单元
    /// </summary>
    public void SetCell(HexCoord coord, T cell)
    {
        cells[coord] = cell;
    }

    /// <summary>
    /// 移除指定坐标的单元
    /// </summary>
    public bool RemoveCell(HexCoord coord)
    {
        return cells.Remove(coord);
    }

    /// <summary>
    /// 检查坐标是否存在单元
    /// </summary>
    public bool HasCell(HexCoord coord)
    {
        return cells.ContainsKey(coord);
    }

    /// <summary>
    /// 尝试获取单元
    /// </summary>
    public bool TryGetCell(HexCoord coord, out T cell)
    {
        return cells.TryGetValue(coord, out cell);
    }

    /// <summary>
    /// 清空网格
    /// </summary>
    public void Clear()
    {
        cells.Clear();
    }

    #endregion

    #region 范围操作

    /// <summary>
    /// 获取指定范围内的所有单元
    /// </summary>
    public List<T> GetCellsInRange(HexCoord center, int range)
    {
        var results = new List<T>();
        var coords = center.GetHexesInRange(range);
        
        foreach (var coord in coords)
        {
            if (TryGetCell(coord, out var cell))
            {
                results.Add(cell);
            }
        }
        return results;
    }

    /// <summary>
    /// 获取邻居单元
    /// </summary>
    public List<T> GetNeighbors(HexCoord coord)
    {
        var results = new List<T>();
        var neighbors = coord.GetAllNeighbors();
        
        foreach (var neighbor in neighbors)
        {
            if (TryGetCell(neighbor, out var cell))
            {
                results.Add(cell);
            }
        }
        return results;
    }

    /// <summary>
    /// 获取邻居坐标和单元
    /// </summary>
    public List<(HexCoord coord, T cell)> GetNeighborsWithCoord(HexCoord coord)
    {
        var results = new List<(HexCoord, T)>();
        var neighbors = coord.GetAllNeighbors();
        
        foreach (var neighbor in neighbors)
        {
            if (TryGetCell(neighbor, out var cell))
            {
                results.Add((neighbor, cell));
            }
        }
        return results;
    }

    #endregion

    #region 生成预设形状

    /// <summary>
    /// 生成六边形区域（以原点为中心）
    /// </summary>
    public static List<HexCoord> GenerateHexagonShape(int radius)
    {
        return HexCoord.Zero.GetHexesInRange(radius);
    }

    /// <summary>
    /// 生成矩形区域
    /// </summary>
    public static List<HexCoord> GenerateRectangleShape(int width, int height)
    {
        var results = new List<HexCoord>();
        
        for (int r = 0; r < height; r++)
        {
            int rOffset = r / 2;
            for (int q = -rOffset; q < width - rOffset; q++)
            {
                results.Add(new HexCoord(q, r));
            }
        }
        return results;
    }

    /// <summary>
    /// 生成三角形区域
    /// </summary>
    public static List<HexCoord> GenerateTriangleShape(int size)
    {
        var results = new List<HexCoord>();
        
        for (int q = 0; q <= size; q++)
        {
            for (int r = 0; r <= size - q; r++)
            {
                results.Add(new HexCoord(q, r));
            }
        }
        return results;
    }

    #endregion

    #region 寻路（A*）

    /// <summary>
    /// A*寻路
    /// </summary>
    /// <param name="start">起点</param>
    /// <param name="goal">终点</param>
    /// <param name="isPassable">检查坐标是否可通行</param>
    /// <param name="getCost">获取移动到该坐标的代价</param>
    /// <returns>路径坐标列表，如果无路径返回null</returns>
    public List<HexCoord> FindPath(
        HexCoord start, 
        HexCoord goal, 
        System.Func<HexCoord, bool> isPassable = null,
        System.Func<HexCoord, int> getCost = null)
    {
        isPassable ??= coord => HasCell(coord);
        getCost ??= coord => 1;

        var openSet = new SimplePriorityQueue<HexCoord, int>();
        var cameFrom = new Dictionary<HexCoord, HexCoord>();
        var gScore = new Dictionary<HexCoord, int>();
        var fScore = new Dictionary<HexCoord, int>();

        gScore[start] = 0;
        fScore[start] = start.DistanceTo(goal);
        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in current.GetAllNeighbors())
            {
                if (!isPassable(neighbor))
                    continue;

                int tentativeG = gScore[current] + getCost(neighbor);

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + neighbor.DistanceTo(goal);
                    
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return null; // 无路径
    }

    private List<HexCoord> ReconstructPath(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord current)
    {
        var path = new List<HexCoord> { current };
        
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        
        return path;
    }

    #endregion
}
