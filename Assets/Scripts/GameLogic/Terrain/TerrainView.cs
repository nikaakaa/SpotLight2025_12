using UnityEngine;

/// <summary>
/// 地形可视化组件
/// 将地形数据渲染为六边形网格
/// 不需要预制体，程序化生成六边形 Mesh
/// </summary>
public class TerrainView : MonoBehaviour
{
    [Header("预制体（可选）")]
    [Tooltip("如果不设置，会自动生成六边形 Mesh")]
    [SerializeField] private GameObject hexTilePrefab;

    [Header("设置")]
    [SerializeField] private float tileZOffset = 0.5f; // 地形瓦片 Z 偏移（放在背景层）
    [SerializeField] private bool useHeightForY = false; // 是否将高度映射到 Y 轴
    [SerializeField] private float hexScale = 0.95f; // 六边形缩放（小于1会留出间隙）

    private Transform tilesParent;
    private static Mesh cachedHexMesh; // 缓存六边形 Mesh（所有瓦片共用）
    private static Material cachedMaterial; // 缓存材质

    /// <summary>
    /// 根据 TerrainSystem 生成视图
    /// </summary>
    public void GenerateView(TerrainSystem terrainSystem)
    {
        ClearView();

        if (terrainSystem == null || terrainSystem.terrainGrid.Count == 0)
        {
            Debug.LogWarning("[TerrainView] TerrainSystem 为空");
            return;
        }

        // 确保六边形 Mesh 已创建
        EnsureHexMesh();

        // 创建父物体
        tilesParent = new GameObject("TerrainTiles").transform;
        tilesParent.SetParent(transform);
        tilesParent.localPosition = Vector3.zero;

        foreach (var coord in terrainSystem.terrainGrid.AllCoords)
        {
            var cell = terrainSystem.GetTerrainAt(coord);
            if (cell == null) continue;

            CreateTile(coord, cell);
        }

        Debug.Log($"[TerrainView] 生成了 {terrainSystem.terrainGrid.Count} 个地形瓦片");
    }

    /// <summary>
    /// 确保六边形 Mesh 已创建
    /// </summary>
    private void EnsureHexMesh()
    {
        if (cachedHexMesh != null) return;

        cachedHexMesh = CreateHexMesh();

        // 创建默认材质
        cachedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    /// <summary>
    /// 创建六边形 Mesh
    /// </summary>
    private Mesh CreateHexMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "HexTileMesh";

        // 获取六边形的6个顶点 + 中心点
        Vector3[] vertices = new Vector3[7];
        vertices[0] = Vector3.zero; // 中心点

        float outerRadius = HexMetrics.OuterRadius * hexScale;
        bool isPointyTop = HexMetrics.IsPointyTop;

        for (int i = 0; i < 6; i++)
        {
            float angle;
            if (isPointyTop)
            {
                // Pointy-top: 从30度开始
                angle = (60f * i + 30f) * Mathf.Deg2Rad;
            }
            else
            {
                // Flat-top: 从0度开始
                angle = (60f * i) * Mathf.Deg2Rad;
            }

            vertices[i + 1] = new Vector3(
                outerRadius * Mathf.Cos(angle),
                outerRadius * Mathf.Sin(angle),
                0
            );
        }

        // 三角形索引（6个三角形，从中心到每条边）
        // 绕序反转让法线朝向 Z 轴负方向（朝向摄像机）
        int[] triangles = new int[18];
        for (int i = 0; i < 6; i++)
        {
            triangles[i * 3] = 0;           // 中心点
            triangles[i * 3 + 1] = (i < 5) ? i + 2 : 1; // 下一个顶点（循环）
            triangles[i * 3 + 2] = i + 1;   // 当前顶点
        }


        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// 创建单个地形瓦片
    /// </summary>
    private void CreateTile(HexCoord coord, TerrainCell cell)
    {
        Vector3 worldPos = HexConverter2D.HexToWorld(coord);

        // 如果使用高度作为 Y 轴
        if (useHeightForY)
        {
            worldPos.y = cell.height * 2f; // 缩放高度
        }
        worldPos.z = tileZOffset; // 放在背景层

        GameObject tile;
        if (hexTilePrefab != null)
        {
            tile = Instantiate(hexTilePrefab, worldPos, Quaternion.identity, tilesParent);
        }
        else
        {
            // 程序化创建六边形瓦片
            tile = CreateHexTile(worldPos, cell);
        }

        tile.name = $"Tile_{coord.q}_{coord.r}";

        // 设置颜色
        SetTileColor(tile, cell.type.GetColor());
    }

    /// <summary>
    /// 创建程序化六边形瓦片
    /// </summary>
    private GameObject CreateHexTile(Vector3 position, TerrainCell cell)
    {
        GameObject tile = new GameObject();
        tile.transform.SetParent(tilesParent);
        tile.transform.position = position;

        // 添加 MeshFilter 和 MeshRenderer
        MeshFilter meshFilter = tile.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh = cachedHexMesh;

        // 每个瓦片需要独立的材质实例（因为颜色不同）
        Material mat = new Material(cachedMaterial);
        mat.color = cell.type.GetColor();
        meshRenderer.material = mat;

        return tile;
    }

    /// <summary>
    /// 设置瓦片颜色
    /// </summary>
    private void SetTileColor(GameObject tile, Color color)
    {
        var spriteRenderer = tile.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            return;
        }

        var meshRenderer = tile.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
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
    }

    /// <summary>
    /// 更新单个瓦片颜色
    /// </summary>
    public void UpdateTileColor(HexCoord coord, Color color)
    {
        if (tilesParent == null) return;

        var tileName = $"Tile_{coord.q}_{coord.r}";
        var tileTransform = tilesParent.Find(tileName);
        if (tileTransform != null)
        {
            SetTileColor(tileTransform.gameObject, color);
        }
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

