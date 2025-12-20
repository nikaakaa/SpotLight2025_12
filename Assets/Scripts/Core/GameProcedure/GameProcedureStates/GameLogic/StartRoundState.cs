using UnityEngine;

/// <summary>
/// 回合开始状态 - 处理回合初始化、数据重置、资源预加载等逻辑
/// 自动跳转到下一状态
/// </summary>
public class StartRoundState : LeafState<GameProcedureContext>
{
    public StartRoundState()
    {
        Name = nameof(StartRoundState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 开始初始化新回合");

        // 增加回合数
        if (PlayerRunTimeInfo.Current != null)
        {
            PlayerRunTimeInfo.Current.StartNewRound();
            Debug.Log($"[{Name}] 当前回合: {PlayerRunTimeInfo.Current.CurrentRound}");
        }

        // 自动进入下一状态
        ctx.Next();
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 自动跳转，无需等待
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 回合初始化完成");
    }
}
