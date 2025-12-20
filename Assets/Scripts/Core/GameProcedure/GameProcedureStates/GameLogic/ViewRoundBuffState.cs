using UnityEngine;

/// <summary>
/// 查看回合Buff状态 - 显示本回合随机获得的增益效果
/// 当前版本：自动跳转（暂时跳过 Buff 预览）
/// </summary>
public class ViewRoundBuffState : LeafState<GameProcedureContext>
{
    public ViewRoundBuffState()
    {
        Name = nameof(ViewRoundBuffState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 显示回合Buff预览（自动跳过）");

        // 模拟：清空旧的市场 Buff
        if (Player.Instance != null)
        {
            Player.Instance.ClearMarketBuffs();
        }

        // TODO: 未来在这里随机生成市场 Buff 并显示预览 UI
        Debug.Log($"[{Name}] (自动化) 跳过 Buff 预览");

        // 自动进入下一状态
        ctx.Next();
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 自动跳转，无需等待
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit");
    }
}
