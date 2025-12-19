using UnityEngine;

/// <summary>
/// 破产状态 - 显示破产结算界面
/// </summary>
public class BankruptcyState : LeafState<GameProcedureContext>
{
    public BankruptcyState()
    {
        Name = nameof(BankruptcyState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 显示破产界面");
        // TODO: 显示破产结算 UI
        // TODO: 显示最终统计数据
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 等待玩家确认
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit");
        // TODO: 隐藏破产 UI
    }
}
