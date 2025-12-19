using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 加载游戏场景状态 - 处理从主菜单到游戏场景的过渡
/// 包含：场景加载、配置表加载、地形生成、航空系统初始化
/// </summary>
public class LoadingGameState : LeafState<GameProcedureContext>
{
    private AsyncOperation loadOperation;
    private bool isLoading;
    private bool isSceneLoaded;
    private bool isTableLoaded;
    private bool isTerrainGenerated;

    // ========== 地图生成配置 ==========
    private int terrainSeed = -1;  // -1 表示随机种子
    private int mapRadius = 50;    // 地图半径

    public LoadingGameState()
    {
        Name = nameof(LoadingGameState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 开始加载游戏场景和配置表");
        isLoading = true;
        isSceneLoaded = false;
        isTableLoaded = false;
        isTerrainGenerated = false;

        // TODO: 显示加载UI
        // UIManager.Instance.ShowPanel<LoadingPanel>();

        // 1. 异步加载游戏场景
        loadOperation = SceneManager.LoadSceneAsync("GameScene");
        if (loadOperation != null)
        {
            loadOperation.completed += OnSceneLoaded;
        }
        else
        {
            Debug.LogError($"[{Name}] 场景加载失败，请检查场景名称");
            isSceneLoaded = true; // 标记为完成以便继续流程
        }

        // 2. 加载配置表（如果尚未加载）
        if (!TableLoader.IsLoaded)
        {
            TableLoader.LoadTablesAsync(OnTableLoaded);
        }
        else
        {
            Debug.Log($"[{Name}] 配置表已加载，跳过");
            isTableLoaded = true;
        }
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        if (!isLoading) return;

        // TODO: 更新加载进度
        // float sceneProgress = loadOperation?.progress ?? 1f;
        // float totalProgress = (sceneProgress + (isTableLoaded ? 1f : 0f)) / 2f;
        // LoadingPanel.Instance?.SetProgress(totalProgress);
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 加载完成");
        loadOperation = null;
        isLoading = false;

        // TODO: 隐藏加载UI
        // UIManager.Instance.HidePanel<LoadingPanel>();
    }

    private void OnTableLoaded()
    {
        Debug.Log($"[{Name}] 配置表加载完成");
        isTableLoaded = true;
        TryGenerateTerrain();
    }

    private void OnSceneLoaded(AsyncOperation operation)
    {
        Debug.Log($"[{Name}] 场景加载完成");
        isSceneLoaded = true;
        TryGenerateTerrain();
    }

    /// <summary>
    /// 尝试生成地形（需要场景和配置表都加载完成）
    /// </summary>
    private void TryGenerateTerrain()
    {
        // 必须场景和配置表都加载完成才能生成地形
        if (!isSceneLoaded || !isTableLoaded || isTerrainGenerated)
            return;

        Debug.Log($"[{Name}] 开始生成地形...");

        // 设置六边形参数
        HexMetrics.SetSize(1f);
        HexMetrics.SetOrientation(true); // Pointy-top

        // 1. 初始化并生成地形
        var terrainSystem = new TerrainSystem();
        terrainSystem.GenerateTerrain(terrainSeed, mapRadius);

        // 2. 初始化航空系统
        var aviationSystem = new AviationSystem();

        // 3. 查找或创建 TerrainView 并生成视图
        var terrainView = Object.FindFirstObjectByType<TerrainView>();
        if (terrainView == null)
        {
            // 自动创建 TerrainView
            terrainView = TerrainView.CreateInScene();
        }
        terrainView.GenerateView(terrainSystem);
        Debug.Log($"[{Name}] 地形视图生成完成");


        isTerrainGenerated = true;
        Debug.Log($"[{Name}] 地形生成完成: seed={terrainSystem.seed}, radius={mapRadius}");

        TryProceedToNextState();
    }

    private void TryProceedToNextState()
    {
        // 场景、配置表和地形都加载完成后，才进入下一状态
        if (isSceneLoaded && isTableLoaded && isTerrainGenerated)
        {
            isLoading = false;
            GameProcedure.Instance?.Context?.Next();
        }
    }
}

