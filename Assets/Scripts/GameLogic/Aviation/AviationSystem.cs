using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 航空系统 - 管理所有节点和边的运行时数据
/// 在 GameLogicState 中实例化
/// 
/// 架构层次：
/// - 配置层: cfg.Node (Luban 配置表)
/// - 运行时层: AviationNode, AviationEdge (本系统管理)
/// - 显示层: CityNode, EdgeLineView (单向引用运行时层)
/// </summary>
public class AviationSystem
{
    public static AviationSystem Instance { get; private set; }

    private int edgeIndex = 0;
    private int nodeIndex = 0;

    public Dictionary<int, AviationNode> aviationNodeDict = new Dictionary<int, AviationNode>();
    public Dictionary<int, AviationEdge> aviationEdgeDict = new Dictionary<int, AviationEdge>();

    // 按六边形坐标索引节点（用于快速查找）
    public Dictionary<HexCoord, AviationNode> nodeByCoord = new Dictionary<HexCoord, AviationNode>();

    /// <summary>
    /// 城市节点预制体地址（Addressables）
    /// </summary>
    public const string CITY_NODE_PREFAB = "CityNode";

    public AviationSystem()
    {
        Instance = this;
        InitAviationSystem();
    }

    public void InitAviationSystem()
    {
        edgeIndex = 0;
        nodeIndex = 0;
        aviationNodeDict.Clear();
        aviationEdgeDict.Clear();
        nodeByCoord.Clear();
    }

    #region 节点管理

    /// <summary>
    /// 添加节点（仅数据）
    /// </summary>
    public AviationNode AddNode(int nodeConfigId, HexCoord coord)
    {
        // 检查该位置是否已有节点
        if (nodeByCoord.ContainsKey(coord))
        {
            Debug.LogWarning($"[AviationSystem] 坐标 {coord} 已存在节点");
            return nodeByCoord[coord];
        }

        AviationNode node = new AviationNode(nodeConfigId, nodeIndex);
        node.SetHexCoord(coord);

        aviationNodeDict.Add(nodeIndex, node);
        nodeByCoord.Add(coord, node);
        nodeIndex++;

        return node;
    }

    /// <summary>
    /// 添加节点并实例化预制体视图
    /// </summary>
    public void AddNodeWithView(int nodeConfigId, HexCoord coord, Transform parent = null)
    {
        AviationNode node = AddNode(nodeConfigId, coord);

        // 异步加载预制体
        AddressablesMgr.Instance.LoadAssetAsync<GameObject>(CITY_NODE_PREFAB, (prefab) =>
        {
            if (prefab == null)
            {
                Debug.LogError($"[AviationSystem] 加载预制体失败: {CITY_NODE_PREFAB}");
                return;
            }

            Vector3 worldPos = HexConverter2D.HexToWorld(coord);
            GameObject go = Object.Instantiate(prefab, worldPos, Quaternion.identity, parent);
            go.name = $"CityNode_{node.nodeIndex}_{node.nodeData?.Name ?? "Unknown"}";

            // 视图层单向引用运行时层
            CityNode cityNode = go.GetComponent<CityNode>();
            if (cityNode != null)
            {
                cityNode.Initialize(node);
            }
        });
    }

    /// <summary>
    /// 获取指定坐标的节点
    /// </summary>
    public AviationNode GetNodeByCoord(HexCoord coord)
    {
        nodeByCoord.TryGetValue(coord, out var node);
        return node;
    }

    #endregion

    #region 边管理

    /// <summary>
    /// 添加边（仅数据）
    /// </summary>
    public AviationEdge AddEdge(AviationNode from, AviationNode to)
    {
        if (from == null || to == null)
        {
            Debug.LogError("[AviationSystem] 无法创建边：节点为空");
            return null;
        }

        // 检查是否已存在该边
        foreach (var edge in aviationEdgeDict.Values)
        {
            if ((edge.fromNode == from && edge.toNode == to) ||
                (edge.fromNode == to && edge.toNode == from))
            {
                Debug.LogWarning("[AviationSystem] 该边已存在");
                return edge;
            }
        }

        AviationEdge newEdge = new AviationEdge(edgeIndex, from, to);
        aviationEdgeDict.Add(edgeIndex, newEdge);
        edgeIndex++;

        return newEdge;
    }

    /// <summary>
    /// 添加边并创建视图
    /// </summary>
    public void AddEdgeWithView(AviationNode from, AviationNode to, Transform parent = null)
    {
        Debug.Log($"[AviationSystem] AddEdgeWithView: from={from?.hexCoord}, to={to?.hexCoord}");

        AviationEdge edge = AddEdge(from, to);
        if (edge == null)
        {
            Debug.LogWarning("[AviationSystem] AddEdge 返回 null，可能参数无效");
            return;
        }

        // 重新计算路径（如果之前失败过，或者地图发生了变化）
        if (edge.pathCoords == null || edge.pathCoords.Count == 0)
        {
            Debug.Log("[AviationSystem] 现有边的路径为空，尝试重新计算...");
            edge.CalculatePath();
        }

        Debug.Log($"[AviationSystem] 创建边成功: index={edge.edgeIndex}, pathCount={edge.pathCoords?.Count ?? 0}");

        // 防止重复创建视图
        string viewName = $"Edge_{edge.edgeIndex}_{from.nodeIndex}_to_{to.nodeIndex}";

        // 如果指定了父节点，先在父节点下查找
        if (parent != null)
        {
            Transform existing = parent.Find(viewName);
            if (existing != null)
            {
                Debug.Log($"[AviationSystem] 视图 {viewName} 已存在，跳过创建");
                return;
            }
        }
        else
        {
            // 全局查找（性能稍差，但点击频率低可接受）
            GameObject existingGo = GameObject.Find(viewName);
            if (existingGo != null)
            {
                Debug.Log($"[AviationSystem] 视图 {viewName} 已存在，跳过创建");
                return;
            }
        }

        // 创建视图 GameObject
        GameObject lineObj = new GameObject(viewName);
        if (parent != null) lineObj.transform.SetParent(parent);

        // 视图层单向引用运行时层
        EdgeLineView lineView = lineObj.AddComponent<EdgeLineView>();
        lineView.Initialize(edge);

        Debug.Log($"[AviationSystem] EdgeLineView 创建完成");
    }

    #endregion

    #region 随机生成

    /// <summary>
    /// 随机生成节点
    /// </summary>
    /// <param name="count">生成数量</param>
    /// <param name="radius">生成范围（六边形半径）</param>
    /// <param name="parent">父物体</param>
    public void GenerateRandomNodes(int count, int radius, Transform parent = null)
    {
        var allHexes = HexCoord.Zero.GetHexesInRange(radius);

        // 随机打乱
        for (int i = allHexes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = allHexes[i];
            allHexes[i] = allHexes[j];
            allHexes[j] = temp;
        }

        // 获取所有可用的节点配置 ID
        var nodeConfigs = TableLoader.Tables.TbNode.DataList;

        int generated = 0;
        for (int i = 0; i < allHexes.Count && generated < count; i++)
        {
            HexCoord coord = allHexes[i];

            // 跳过已有节点的位置
            if (nodeByCoord.ContainsKey(coord))
                continue;

            // 随机选择一个节点配置
            int configId = nodeConfigs[Random.Range(0, nodeConfigs.Count)].Id;

            AddNodeWithView(configId, coord, parent);
            generated++;
        }

        Debug.Log($"[AviationSystem] 生成了 {generated} 个随机节点");
    }

    #endregion
}