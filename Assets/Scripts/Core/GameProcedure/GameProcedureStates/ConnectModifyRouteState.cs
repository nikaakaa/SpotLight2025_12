using UnityEngine;

/// <summary>
/// 路线连接与修改状态 - 处理路线编辑的复杂交互逻辑
/// </summary>
public class ConnectModifyRouteState : LeafState<GameProcedureContext>
{
    public ConnectModifyRouteState()
    {
        Name = nameof(ConnectModifyRouteState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 进入路线编辑模式");
        // TODO: 启用路线编辑 UI
        // TODO: 显示可连接的节点
        // TODO: 初始化拖拽/点击交互系统
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 处理用户输入（拖拽、点击）
        // TODO: 实时验证路线有效性
        // TODO: 更新路线预览
        // TODO: 检测确认按钮点击 -> ctx.RequestSettlementClickReady = true
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 退出路线编辑模式");
        // TODO: 禁用路线编辑 UI
        // TODO: 确认最终路线配置
    }
}
