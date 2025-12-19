using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 加载游戏场景状态 - 处理从主菜单到游戏场景的过渡
/// </summary>
public class LoadingGameState : LeafState<GameProcedureContext>
{
    private AsyncOperation loadOperation;
    private bool isLoading;

    public LoadingGameState()
    {
        Name = nameof(LoadingGameState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 开始加载游戏场景");
        isLoading = true;
        
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
            isLoading = false;
            ctx.Next();
        }
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        if (!isLoading || loadOperation == null) return;
        
        // TODO: 更新加载进度
        // float progress = loadOperation.progress;
        // LoadingPanel.Instance?.SetProgress(progress);
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 场景加载完成");
        loadOperation = null;
        isLoading = false;
        
        // TODO: 隐藏加载UI
        // UIManager.Instance.HidePanel<LoadingPanel>();
    }

    private void OnSceneLoaded(AsyncOperation operation)
    {
        Debug.Log($"[{Name}] 场景加载完成");
        isLoading = false;
        
        // 场景加载完成，自动推进到下一状态
        GameProcedure.Instance?.Context?.Next();
    }
}
