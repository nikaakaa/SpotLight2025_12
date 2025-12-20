using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形可视化组件（优化版）
/// 使用合并 Mesh 方式渲染，按地形类型分组
/// 大幅减少 Draw Call，支持大地图（30000+ 格子）
/// </summary>
public class TerrainView : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private float tileZOffset = 0.5f;
    [SerializeField] private float hexScale = 0.95f;

    [Header("性能")]
    [Tooltip("每个 Mesh 最大顶点数（Unity 限制 65535）")]
    [SerializeField] private int maxVerticesPerMesh = 60000;

    private Transform tilesParent;
    private List<GameObject> meshObjects = new List<GameObject>();

    // 缓存的六边形顶点和三角形模板
    private Vector3[] hexVertices;
    private int[] hexTriangles;

    /// <summary>
    /// 根据 TerrainSystem 生成视图（合并 Mesh 版）
    /// </summary>
    public void GenerateView(TerrainSystem terrainSystem)
    {
        ClearView();

        if (terrainSystem == null || terrainSystem.terrainGrid.Count == 0)
        {
            Debug.LogWarning("[TerrainView] TerrainSystem 为空");
            return;
        }

        // 初始化六边形模板
        InitHexTemplate();

        // 创建父物体
        tilesParent = new GameObject("TerrainTiles").transform;
        tilesParent.SetParent(transform);
        tilesParent.localPosition = Vector3.zero;

        // 按地形类型分组
        var cellsByType = new Dictionary<TerrainType, List<(HexCoord coord, TerrainCell cell)>>();

        foreach (var coord in terrainSystem.terrainGrid.AllCoords)
        {
            var cell = terrainSystem.GetTerrainAt(coord);
            if (cell == null) continue;

            if (!cellsByType.ContainsKey(cell.type))
            {
                cellsByType[cell.type] = new List<(HexCoord, TerrainCell)>();
            }
            cellsByType[cell.type].Add((coord, cell));
        }

        // 为每种地形类型创建合并的 Mesh
        int totalTiles = 0;
        foreach (var kvp in cellsByType)
        {
            CreateMergedMeshForType(kvp.Key, kvp.Value);
            totalTiles += kvp.Value.Count;
        }

        Debug.Log($"[TerrainView] 生成了 {totalTiles} 个地形瓦片，合并为 {meshObjects.Count} 个 Mesh");
    }

    /// <summary>
    /// 初始化六边形顶点和三角形模板
    /// </summary>
    private void InitHexTemplate()
    {
        hexVertices = new Vector3[7];
        hexVertices[0] = Vector3.zero;

        float outerRadius = HexMetrics.OuterRadius * hexScale;
        bool isPointyTop = HexMetrics.IsPointyTop;

        for (int i = 0; i < 6; i++)
        {
            float angle;
            if (isPointyTop)
            {
                angle = (60f * i + 30f) * Mathf.Deg2Rad;
            }
            else
            {
                angle = (60f * i) * Mathf.Deg2Rad;
            }

            hexVertices[i + 1] = new Vector3(
                outerRadius * Mathf.Cos(angle),
                outerRadius * Mathf.Sin(angle),
                0
            );
        }

        // 三角形（法线朝向 Z 轴负方向）
        hexTriangles = new int[18];
        for (int i = 0; i < 6; i++)
        {
            hexTriangles[i * 3] = 0;
            hexTriangles[i * 3 + 1] = (i < 5) ? i + 2 : 1;
            hexTriangles[i * 3 + 2] = i + 1;
        }
    }

    /// <summary>
    /// 为一种地形类型创建合并的 Mesh
    /// </summary>
    private void CreateMergedMeshForType(TerrainType type, List<(HexCoord coord, TerrainCell cell)> cells)
    {
        Color color = type.GetColor();
        int verticesPerHex = 7;
        int trianglesPerHex = 18;
        int maxHexesPerMesh = maxVerticesPerMesh / verticesPerHex;

        // 分批创建 Mesh（防止超过 65535 顶点限制）
        int batchCount = 0;
        for (int start = 0; start < cells.Count; start += maxHexesPerMesh)
        {
            int count = Mathf.Min(maxHexesPerMesh, cells.Count - start);
            CreateSingleMergedMesh(type, cells, start, count, color, batchCount);
            batchCount++;
        }
    }

    /// <summary>
    /// 创建单个合并的 Mesh
    /// </summary>
    private void CreateSingleMergedMesh(TerrainType type, List<(HexCoord coord, TerrainCell cell)> cells,
        int startIndex, int count, Color color, int batchIndex)
    {
        int verticesPerHex = 7;
        int trianglesPerHex = 18;

        Vector3[] vertices = new Vector3[count * verticesPerHex];
        int[] triangles = new int[count * trianglesPerHex];
        Color[] colors = new Color[count * verticesPerHex];

        for (int i = 0; i < count; i++)
        {
            var (coord, cell) = cells[startIndex + i];
            Vector3 worldPos = HexConverter2D.HexToWorld(coord);
            worldPos.z = tileZOffset;

            int vertexOffset = i * verticesPerHex;
            int triangleOffset = i * trianglesPerHex;

            // 复制顶点（偏移到世界位置）
            for (int v = 0; v < verticesPerHex; v++)
            {
                vertices[vertexOffset + v] = hexVertices[v] + worldPos;
                colors[vertexOffset + v] = color;
            }

            // 复制三角形（调整索引偏移）
            for (int t = 0; t < trianglesPerHex; t++)
            {
                triangles[triangleOffset + t] = hexTriangles[t] + vertexOffset;
            }
        }

        // 创建 Mesh
        Mesh mesh = new Mesh();
        mesh.name = $"TerrainMesh_{type}_{batchIndex}";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 创建 GameObject
        GameObject meshObj = new GameObject($"Terrain_{type}_{batchIndex}");
        meshObj.transform.SetParent(tilesParent);
        meshObj.transform.localPosition = Vector3.zero;

        MeshFilter meshFilter = meshObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObj.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;

        // 使用顶点颜色的材质
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        meshRenderer.material = mat;

        // 设置渲染层级 - 地形在最下层
        meshRenderer.sortingOrder = RenderLayers.TERRAIN;

        meshObjects.Add(meshObj);
    }

    /// <summary>
    /// 清除现有视图
    /// </summary>
    public void ClearView()
    {
        if (tilesParent != null)
        {
            Destroy(tilesParent.gameObject);
            tilesParent = null;
        }
        meshObjects.Clear();
    }

    /// <summary>
    /// 静态方法：在场景中自动创建 TerrainView
    /// </summary>
    public static TerrainView CreateInScene()
    {
        GameObject go = new GameObject("TerrainView");
        var view = go.AddComponent<TerrainView>();
        Debug.Log("[TerrainView] 自动创建 TerrainView 对象");
        return view;
    }
}


