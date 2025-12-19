using UnityEngine;

/// <summary>
/// 随机节点状态 - 处理随机节点的生成、配置和展示逻辑
/// </summary>
public class RandomNodeState : LeafState<GameProcedureContext>
{
    public RandomNodeState()
    {
        Name = nameof(RandomNodeState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 开始生成随机节点");
        // TODO: 执行随机节点生成算法
        // TODO: 配置节点属性
        // TODO: 在场景中展示节点
        // TODO: 完成后触发 ctx.RequestRoundProfitReady = true
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 等待节点生成动画完成
        // TODO: 处理节点交互预览
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 随机节点生成完成");
    }
}
