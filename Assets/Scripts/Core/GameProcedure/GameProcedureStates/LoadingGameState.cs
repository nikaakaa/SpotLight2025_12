using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 加载游戏场景状态 - 处理从主菜单到游戏场景的过渡
/// 包含：场景加载、地形生成、航空系统初始化
/// 注：已移除 Luban TableLoader 依赖，使用 GameConfig 静态配置
/// </summary>
public class LoadingGameState : LeafState<GameProcedureContext>
{
    private AsyncOperation loadOperation;
    private bool isLoading;
    private bool isSceneLoaded;
    private bool isTerrainGenerated;

    public LoadingGameState()
    {
        Name = nameof(LoadingGameState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 开始加载游戏场景");
        isLoading = true;
        isSceneLoaded = false;
        isTerrainGenerated = false;

        // TODO: 显示加载UI
        // UIManager.Instance.ShowPanel<LoadingPanel>();

        // 异步加载游戏场景
        loadOperation = SceneManager.LoadSceneAsync("GameScene");
        if (loadOperation != null)
        {
            loadOperation.completed += OnSceneLoaded;
        }
        else
        {
            Debug.LogError($"[{Name}] 场景加载失败，请检查场景名称");
            isSceneLoaded = true; // 标记为完成以便继续流程
            TryGenerateTerrain();
        }
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        if (!isLoading) return;

        // TODO: 更新加载进度
        // float progress = loadOperation?.progress ?? 1f;
        // LoadingPanel.Instance?.SetProgress(progress);
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 加载完成");
        loadOperation = null;
        isLoading = false;

        // TODO: 隐藏加载UI
        // UIManager.Instance.HidePanel<LoadingPanel>();
    }

    private void OnSceneLoaded(AsyncOperation operation)
    {
        Debug.Log($"[{Name}] 场景加载完成");
        isSceneLoaded = true;
        TryGenerateTerrain();
    }

    /// <summary>
    /// 尝试生成地形（需要场景加载完成）
    /// </summary>
    private void TryGenerateTerrain()
    {
        if (!isSceneLoaded || isTerrainGenerated)
            return;

        Debug.Log($"[{Name}] 开始生成地形...");

        // 设置六边形参数
        HexMetrics.SetSize(1f);
        HexMetrics.SetOrientation(true); // Pointy-top

        // 1. 创建玩家运行时数据
        var playerRunTimeInfo = new PlayerRunTimeInfo();
        PlayerRunTimeInfo.Current = playerRunTimeInfo;
        Debug.Log($"[{Name}] 玩家运行时数据已创建");

        // 2. 初始化并生成地形（使用 GameConfig 常量）
        var terrainSystem = new TerrainSystem();
        terrainSystem.GenerateTerrain(GameConfig.TERRAIN_SEED, GameConfig.MAP_RADIUS);

        // 3. 初始化航空系统
        var aviationSystem = new AviationSystem();

        // 4. 查找或创建 TerrainView 并生成视图
        var terrainView = Object.FindFirstObjectByType<TerrainView>();
        if (terrainView == null)
        {
            terrainView = TerrainView.CreateInScene();
        }
        terrainView.GenerateView(terrainSystem);
        Debug.Log($"[{Name}] 地形视图生成完成");

        isTerrainGenerated = true;
        Debug.Log($"[{Name}] 地形生成完成: seed={terrainSystem.seed}, radius={GameConfig.MAP_RADIUS}");

        TryProceedToNextState();
    }

    private void TryProceedToNextState()
    {
        if (isSceneLoaded && isTerrainGenerated)
        {
            isLoading = false;
            GameProcedure.Instance?.Context?.Next();
        }
    }
}
