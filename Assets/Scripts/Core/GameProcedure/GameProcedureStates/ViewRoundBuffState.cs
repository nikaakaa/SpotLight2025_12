using UnityEngine;

/// <summary>
/// 查看回合Buff状态 - 显示本回合随机获得的增益效果
/// </summary>
public class ViewRoundBuffState : LeafState<GameProcedureContext>
{
    public ViewRoundBuffState()
    {
        Name = nameof(ViewRoundBuffState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 显示回合Buff预览");
        // TODO: 随机生成本回合的 Buff
        // TODO: 显示 Buff 预览 UI
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 等待玩家确认
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit");
        // TODO: 应用选中的 Buff
        // TODO: 隐藏 Buff 预览 UI
    }
}
