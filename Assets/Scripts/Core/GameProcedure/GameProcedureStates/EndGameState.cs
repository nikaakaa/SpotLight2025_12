using UnityEngine;

/// <summary>
/// 游戏结束状态 - 显示游戏结束界面（胜利或失败）
/// </summary>
public class EndGameState : LeafState<GameProcedureContext>
{
    public EndGameState()
    {
        Name = nameof(EndGameState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 显示游戏结束界面");
        // TODO: 显示游戏结束 UI
        // TODO: 显示最终得分/统计
        // TODO: 显示返回主菜单按钮
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 等待玩家操作
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit");
        // TODO: 隐藏结束界面 UI
        // TODO: 重置游戏数据
    }
}
