using UnityEngine;

/// <summary>
/// 回合开始状态 - 处理回合初始化、数据重置、资源预加载等逻辑
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
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 等待初始化完成
        // TODO: 显示加载进度（如果需要）
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 回合初始化完成");
    }
}
