using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 加载游戏场景状态 - 处理从主菜单到游戏场景的过渡
/// </summary>
public class LoadingGameState : LeafState<GameProcedureContext>
{
    private AsyncOperation loadOperation;
    private bool isLoading;
    private bool isSceneLoaded;
    private bool isTableLoaded;

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
        TryProceedToNextState();
    }

    private void OnSceneLoaded(AsyncOperation operation)
    {
        Debug.Log($"[{Name}] 场景加载完成");
        isSceneLoaded = true;
        TryProceedToNextState();
    }

    private void TryProceedToNextState()
    {
        // 场景和配置表都加载完成后，才进入下一状态
        if (isSceneLoaded && isTableLoaded)
        {
            isLoading = false;
            GameProcedure.Instance?.Context?.Next();
        }
    }
}
