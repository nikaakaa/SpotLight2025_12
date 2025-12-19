using UnityEngine;

/// <summary>
/// 暂停到菜单状态 - 处理游戏暂停和返回主菜单的过渡
/// </summary>
public class PauseToMenuState : LeafState<GameProcedureContext>
{
    public PauseToMenuState()
    {
        Name = nameof(PauseToMenuState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 暂停游戏，准备返回菜单");
        // TODO: 显示暂停菜单 UI
        // TODO: 暂停游戏时间 Time.timeScale = 0
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 等待玩家确认返回菜单或继续游戏
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit");
        // TODO: 恢复游戏时间 Time.timeScale = 1
        // TODO: 隐藏暂停菜单 UI
    }
}
