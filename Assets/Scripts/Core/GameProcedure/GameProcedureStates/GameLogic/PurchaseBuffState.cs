using UnityEngine;

/// <summary>
/// 购买增益状态 - 处理购买界面的交互和升级逻辑
/// 当前版本：自动跳转（暂时跳过购买功能）
/// </summary>
public class PurchaseBuffState : LeafState<GameProcedureContext>
{
    public PurchaseBuffState()
    {
        Name = nameof(PurchaseBuffState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 进入购买/升级界面（自动跳过）");

        // TODO: 未来在这里显示购买 UI
        Debug.Log($"[{Name}] (自动化) 跳过购买阶段");

        // 自动进入下一状态（回到新回合）
        ctx.Next();
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 自动跳转，无需等待
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 退出购买界面");
    }
}
