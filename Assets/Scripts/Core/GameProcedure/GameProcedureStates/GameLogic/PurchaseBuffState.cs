using UnityEngine;

/// <summary>
/// 购买增益状态 - 处理购买界面的交互和升级逻辑
/// </summary>
public class PurchaseBuffState : LeafState<GameProcedureContext>
{
    public PurchaseBuffState()
    {
        Name = nameof(PurchaseBuffState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 进入购买/升级界面");
        // TODO: 显示购买/升级 UI
        // TODO: 加载可购买项目列表
        // TODO: 显示当前资源状态
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 处理购买交互
        // TODO: 更新 UI 显示
        // TODO: 检测确认/跳过按钮 -> ctx.RequestNextRound = true
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 退出购买界面");
        // TODO: 确认购买事务
        // TODO: 隐藏购买 UI
    }
}
