using UnityEngine;

/// <summary>
/// 查看回合Buff状态 - 显示本回合随机获得的增益效果
/// </summary>
public class ViewRoundBuffState : LeafState<GameProcedureContext>
{
    public ViewRoundBuffState()
    {
        Name = nameof(ViewRoundBuffState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 显示回合Buff预览");

        // 模拟：清空旧的市场 Buff
        if (Player.Instance != null)
        {
            Player.Instance.ClearMarketBuffs();
        }

        // 模拟：随机添加一个市场 Buff
        // UI 测试阶段仅打印日志
        if (Random.value > 0.5f)
        {
            Debug.Log($"[{Name}] (模拟) 随机事件发生");
        }
        else
        {
            Debug.Log($"[{Name}] (模拟) 本回合无特殊事件");
        }

        // GameUIController 会自动处理面板显示
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 等待 GameUIController 触发 GameEvent.Next
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit");
    }
}
