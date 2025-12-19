using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏逻辑状态 - 管理游戏回合循环的复合状态
/// 子状态：StartRound → RandomNode → ViewRoundBuff → ConnectModifyRoute → Settlement → PurchaseBuff → (循环)
/// </summary>
public class GameLogicState : ComposeState<GameProcedureContext>
{
    public AviationSystem aviationSystem;
    public GameLogicState()
    {
        Name = nameof(GameLogicState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        //初始化游戏数据,如果有保存数据的话,不过不做保存
        aviationSystem = new AviationSystem();

        Debug.Log($"[{Name}] Enter - 进入游戏逻辑循环");
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // ComposeState 的 Update 会自动处理子状态
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 退出游戏逻辑循环");
    }

    /// <summary>
    /// 获取当前子状态名称
    /// </summary>
    public string CurrentSubStateName => CurrentSubState?.Name ?? "None";
}
