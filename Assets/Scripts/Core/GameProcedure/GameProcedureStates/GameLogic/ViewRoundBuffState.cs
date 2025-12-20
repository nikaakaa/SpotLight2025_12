using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 查看回合Buff状态 - 显示本回合随机获得的增益效果
/// 当前版本：显示全屏通知动画后自动跳转
/// </summary>
public class ViewRoundBuffState : LeafState<GameProcedureContext>
{
    private bool isAnimationComplete = false;

    public ViewRoundBuffState()
    {
        Name = nameof(ViewRoundBuffState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 显示回合Buff预览");
        isAnimationComplete = false;

        string buffName = "";

        if (PlayerRunTimeInfo.Current != null)
        {
            // 随机生成一个市场 Buff (ID 1-21)
            int randomBuffId = Random.Range(1, 22); // range is [min, max)
            PlayerRunTimeInfo.Current.SetMarketBuffs(randomBuffId);

            buffName = BuffDisplayConfig.GetBuffDisplayText(randomBuffId);
            Debug.Log($"[{Name}] 生成随机市场趋势: {buffName} (ID: {randomBuffId})");
        }
        else
        {
            Debug.LogWarning($"[{Name}] PlayerRunTimeInfo.Current is null, skipping buff generation.");
            buffName = "未找到玩家数据";
        }

        // 启动异步显示流程
        ShowBuffAnimation(buffName).Forget();
    }

    private async UniTaskVoid ShowBuffAnimation(string buffName)
    {
        if (GameLogicUI.Instance != null)
        {
            // 使用配置的时长（默认 2.0s）
            await GameLogicUI.Instance.ShowCentralNotification(
                $"本回合市场趋势\n<size=80%>{buffName}</size>",
                SettlementAnimConfig.BuffNotificationDuration
            );
        }
        else
        {
            // 主要是防止 UI 未初始化导致卡死
            await UniTask.Delay(1000);
        }

        isAnimationComplete = true;
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 动画播放完毕后才跳转
        if (isAnimationComplete)
        {
            ctx.Next();
        }
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit");
    }
}
