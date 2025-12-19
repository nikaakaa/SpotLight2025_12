using UnityEngine;

/// <summary>
/// 主菜单状态 - 处理主菜单界面的显示、交互和导航逻辑
/// </summary>
public class MainMenuState : LeafState<GameProcedureContext>
{
    public MainMenuState()
    {
        Name = nameof(MainMenuState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 显示主菜单界面");
        // TODO: 显示主菜单 UI
        // TODO: 初始化菜单按钮事件
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // TODO: 处理菜单交互逻辑
        // TODO: 检测输入事件
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 隐藏主菜单界面");
        // TODO: 隐藏主菜单 UI
        // TODO: 清理菜单资源
    }
}
