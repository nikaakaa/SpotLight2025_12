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
    /// 已被航线占用的路径格子（不含节点端点）
    /// 用于寻路时避开已有航线
    /// </summary>
    public HashSet<HexCoord> occupiedPathCoords = new HashSet<HexCoord>();

    /// <summary>
    /// 所有节点所在的坐标（允许航线经过）
    /// </summary>
    public HashSet<HexCoord> nodeCoords = new HashSet<HexCoord>();

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
        occupiedPathCoords.Clear();
        nodeCoords.Clear();
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
        nodeCoords.Add(coord); // 记录节点坐标（允许航线经过）
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
    /// 边添加完成事件（用于扣除玩家金钱等）
    /// 参数：edge, pathLength (用于计算建造成本)
    /// </summary>
    public event System.Action<AviationEdge, int> OnEdgeAdded;

    /// <summary>
    /// 边移除完成事件（用于返还玩家金钱等）
    /// 参数：edge, pathLength (用于计算拆除返还)
    /// </summary>
    public event System.Action<AviationEdge, int> OnEdgeRemoved;

    /// <summary>
    /// 添加边（仅数据，不创建视图）
    /// </summary>
    /// <param name="from">起点节点</param>
    /// <param name="to">终点节点</param>
    /// <param name="triggerEvent">是否触发事件（默认true）</param>
    /// <returns>创建的边，失败返回null</returns>
    public AviationEdge AddEdge(AviationNode from, AviationNode to, bool triggerEvent = true)
    {
        if (from == null || to == null)
        {
            Debug.LogError("[AviationSystem] 无法创建边：节点为空");
            return null;
        }

        if (from == to)
        {
            Debug.LogWarning("[AviationSystem] 无法创建边：起点和终点相同");
            return null;
        }

        // 检查是否已存在该边
        foreach (var existingEdge in aviationEdgeDict.Values)
        {
            if ((existingEdge.fromNode == from && existingEdge.toNode == to) ||
                (existingEdge.fromNode == to && existingEdge.toNode == from))
            {
                Debug.LogWarning("[AviationSystem] 该边已存在");
                return existingEdge;
            }
        }

        // 创建边（构造函数中会自动计算路径并注册占用）
        AviationEdge newEdge = new AviationEdge(edgeIndex, from, to);

        // 检查路径是否有效
        if (newEdge.pathCoords == null || newEdge.pathCoords.Count == 0)
        {
            Debug.LogWarning("[AviationSystem] 无法创建边：找不到有效路径");
            // 清理已注册到节点的边引用
            from.RemoveEdge(newEdge);
            to.RemoveEdge(newEdge);
            return null;
        }

        aviationEdgeDict.Add(edgeIndex, newEdge);
        edgeIndex++;

        Debug.Log($"[AviationSystem] 添加边成功: index={newEdge.edgeIndex}, pathLength={newEdge.pathCoords.Count}");

        // 触发事件（用于扣除金钱等）
        if (triggerEvent)
        {
            OnEdgeAdded?.Invoke(newEdge, newEdge.pathCoords.Count);
        }

        return newEdge;
    }

    /// <summary>
    /// 添加边并创建视图
    /// </summary>
    /// <param name="from">起点节点</param>
    /// <param name="to">终点节点</param>
    /// <param name="parent">视图父物体</param>
    /// <param name="triggerEvent">是否触发事件（默认true）</param>
    /// <returns>创建的边，失败返回null</returns>
    public AviationEdge AddEdgeWithView(AviationNode from, AviationNode to, Transform parent = null, bool triggerEvent = true)
    {
        Debug.Log($"[AviationSystem] AddEdgeWithView: from={from?.hexCoord}, to={to?.hexCoord}");

        AviationEdge edge = AddEdge(from, to, triggerEvent);
        if (edge == null)
        {
            Debug.LogWarning("[AviationSystem] AddEdge 返回 null，可能参数无效或路径不存在");
            return null;
        }

        // 使用 AirLineController 统一创建视图
        if (AirLineController.Instance != null)
        {
            AirLineController.Instance.AddLine(edge);
        }
        else
        {
            Debug.LogWarning("[AviationSystem] AirLineController 未初始化，跳过视图创建");
        }

        Debug.Log($"[AviationSystem] EdgeLineView 创建完成");

        return edge;
    }

    /// <summary>
    /// 移除边（仅数据，完整清理所有状态）
    /// </summary>
    /// <param name="edge">要移除的边</param>
    /// <param name="triggerEvent">是否触发事件（默认true）</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveEdge(AviationEdge edge, bool triggerEvent = true)
    {
        if (edge == null) return false;

        // 先保存路径长度（用于事件回调）
        int pathLength = edge.pathCoords?.Count ?? 0;

        // 1. 清除路径占用（从 occupiedPathCoords 中移除）
        edge.ClearPathOccupation();

        // 2. 从节点的边列表中移除
        edge.fromNode?.RemoveEdge(edge);
        edge.toNode?.RemoveEdge(edge);

        // 3. 清空边的内部数据
        edge.pathCoords?.Clear();

        // 4. 从系统字典中移除
        bool removed = aviationEdgeDict.Remove(edge.edgeIndex);

        if (removed)
        {
            Debug.Log($"[AviationSystem] 移除边成功: index={edge.edgeIndex}");

            // 触发事件（用于返还金钱等）
            if (triggerEvent)
            {
                OnEdgeRemoved?.Invoke(edge, pathLength);
            }
        }

        return removed;
    }

    /// <summary>
    /// 移除边并销毁视图
    /// </summary>
    /// <param name="edge">要移除的边</param>
    /// <param name="edgeView">边的视图组件（可选，会自动查找）</param>
    /// <param name="triggerEvent">是否触发事件（默认true）</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveEdgeWithView(AviationEdge edge, EdgeLineView edgeView = null, bool triggerEvent = true)
    {
        if (edge == null) return false;

        int edgeIndex = edge.edgeIndex;

        // 移除数据
        bool removed = RemoveEdge(edge, triggerEvent);

        // 使用 AirLineController 统一销毁视图
        if (AirLineController.Instance != null)
        {
            AirLineController.Instance.RemoveLine(edgeIndex);
        }
        else if (edgeView != null)
        {
            // 后备：直接销毁传入的视图
            Object.Destroy(edgeView.gameObject);
            Debug.Log($"[AviationSystem] 销毁边视图（后备方式）");
        }

        return removed;
    }

    /// <summary>
    /// 根据边索引移除边
    /// </summary>
    public bool RemoveEdgeByIndex(int edgeIndex, bool triggerEvent = true)
    {
        if (aviationEdgeDict.TryGetValue(edgeIndex, out var edge))
        {
            return RemoveEdgeWithView(edge, null, triggerEvent);
        }
        return false;
    }

    /// <summary>
    /// 移除所有边
    /// </summary>
    /// <param name="triggerEvent">是否为每条边触发事件</param>
    public void RemoveAllEdges(bool triggerEvent = false)
    {
        var edgesToRemove = new List<AviationEdge>(aviationEdgeDict.Values);
        foreach (var edge in edgesToRemove)
        {
            RemoveEdgeWithView(edge, null, triggerEvent);
        }
        Debug.Log($"[AviationSystem] 移除了所有 {edgesToRemove.Count} 条边");
    }

    #endregion

    #region 随机生成

    /// <summary>
    /// 随机生成节点（集成地形系统和 Poisson Disk 均匀分布）
    /// </summary>
    /// <param name="count">生成数量</param>
    /// <param name="minDistance">城市最小间隔（六边形格子数）</param>
    /// <param name="parent">父物体</param>
    public void GenerateRandomNodes(int count, int minDistance = 3, Transform parent = null)
    {
        // 1. 获取所有可放置城市的坐标
        List<HexCoord> candidates;

        if (TerrainSystem.Instance != null && TerrainSystem.Instance.terrainGrid.Count > 0)
        {
            // 使用地形系统的可放置城市坐标
            candidates = TerrainSystem.Instance.GetAllCityPlaceableCoords();
            Debug.Log($"[AviationSystem] 从地形系统获取 {candidates.Count} 个可放置城市的坐标");
        }
        else
        {
            // 后备：使用默认范围（适用于还没有地形系统的情况）
            candidates = HexCoord.Zero.GetHexesInRange(15);
            Debug.LogWarning("[AviationSystem] 地形系统未初始化，使用默认范围生成城市");
        }

        // 过滤掉已有节点的位置
        candidates.RemoveAll(coord => nodeByCoord.ContainsKey(coord));

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[AviationSystem] 没有可用的城市放置位置");
            return;
        }

        // 2. 使用 Poisson Disk 采样均匀分布城市
        var selectedCoords = PoissonDiskSampler.Sample(candidates, count, minDistance);

        // 3. 获取所有可用的节点配置 ID
        var nodeConfigs = GameConfig.NodeConfigs;

        // 4. 在选中的坐标上创建城市
        foreach (var coord in selectedCoords)
        {
            // 随机选择一个节点配置
            int configId = GameConfig.GetRandomNodeConfigId();
            AddNodeWithView(configId, coord, parent);
        }

        Debug.Log($"[AviationSystem] 生成了 {selectedCoords.Count} 个城市节点（使用 Poisson Disk 采样，最小间距={minDistance}）");
    }

    /// <summary>
    /// 使用簇状生长算法生成节点（推荐使用）
    /// 特点：大部分节点靠近已有节点生成，越高级的节点越稀有且生成越远
    /// </summary>
    /// <param name="count">生成数量</param>
    /// <param name="round">当前回合数（影响节点等级概率）</param>
    /// <param name="parent">父物体</param>
    public void GenerateNodesWithClusterGrowth(int count, int round, Transform parent = null)
    {
        // 1. 获取所有可放置城市的坐标
        List<HexCoord> candidates;

        if (TerrainSystem.Instance != null && TerrainSystem.Instance.terrainGrid.Count > 0)
        {
            candidates = TerrainSystem.Instance.GetAllCityPlaceableCoords();
            Debug.Log($"[AviationSystem] 簇状生长：从地形系统获取 {candidates.Count} 个候选位置");
        }
        else
        {
            candidates = HexCoord.Zero.GetHexesInRange(GameConfig.MAP_RADIUS);
            Debug.LogWarning("[AviationSystem] 地形系统未初始化，使用默认范围");
        }

        // 2. 获取已有节点坐标
        var existingCoords = new List<HexCoord>(nodeByCoord.Keys);

        // 3. 使用簇状生长算法生成
        var spawnResults = ClusterGrowthSpawner.Generate(
            existingCoords,
            candidates,
            count,
            round
        );

        // 4. 在选中的坐标上创建城市
        foreach (var (coord, level) in spawnResults)
        {
            // 根据等级获取对应的节点配置
            int configId = GameConfig.GetNodeConfigIdByLevel(level);
            AddNodeWithView(configId, coord, parent);
        }

        Debug.Log($"[AviationSystem] 簇状生长：生成了 {spawnResults.Count} 个城市节点（回合 {round}）");
    }

    /// <summary>
    /// 旧版随机生成节点（不使用地形系统，保留兼容性）
    /// </summary>
    [System.Obsolete("请使用新版 GenerateRandomNodes")]
    public void GenerateRandomNodesLegacy(int count, int radius, Transform parent = null)
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
        var nodeConfigs = GameConfig.NodeConfigs;

        int generated = 0;
        for (int i = 0; i < allHexes.Count && generated < count; i++)
        {
            HexCoord coord = allHexes[i];

            // 跳过已有节点的位置
            if (nodeByCoord.ContainsKey(coord))
                continue;

            // 随机选择一个节点配置
            int configId = GameConfig.GetRandomNodeConfigId();

            AddNodeWithView(configId, coord, parent);
            generated++;
        }

        Debug.Log($"[AviationSystem] 生成了 {generated} 个随机节点（旧版方法）");
    }

    #endregion
}
