using UnityEngine;

/// <summary>
/// 结算状态 - 处理回合结算的计算、动画和数据更新逻辑
/// </summary>
public class SettlementState : LeafState<GameProcedureContext>
{
    public SettlementState()
    {
        Name = nameof(SettlementState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 开始回合结算");
        // TODO: 计算本回合收益/损失
        // TODO: 播放结算动画
        // TODO: 更新玩家资源数据
        // TODO: 检测是否破产 -> ctx.RequestBankrupt = true
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 等待结算动画播放完成
        // TODO: 显示结算详情
        // TODO: 结算完成后 -> ctx.RequestPurchaseReady = true
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 回合结算完成");
        // TODO: 持久化玩家数据
    }
}
