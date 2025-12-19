using UnityEngine;

/// <summary>
/// 退出游戏状态 - 执行退出应用程序
/// </summary>
public class QuitGameState : LeafState<GameProcedureContext>
{
    public QuitGameState()
    {
        Name = nameof(QuitGameState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 执行退出游戏");
        
        // TODO: 保存游戏数据
        // TODO: 清理资源
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 退出状态不需要 Update
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        // 退出状态不会 Exit
    }
}
