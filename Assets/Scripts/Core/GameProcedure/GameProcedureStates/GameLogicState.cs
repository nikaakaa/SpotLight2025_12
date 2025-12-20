using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏逻辑状态 - 管理游戏回合循环的复合状态
/// 子状态：StartRound → RandomNode → ViewRoundBuff → ConnectModifyRoute → Settlement → PurchaseBuff → (循环)
/// </summary>
public class GameLogicState : ComposeState<GameProcedureContext>
{
    public AviationSystem aviationSystem;
    public PlayerRunTimeInfo playerRunTimeInfo;

    public GameLogicState()
    {
        Name = nameof(GameLogicState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        // 获取 LoadingGameState 中创建的运行时数据
        playerRunTimeInfo = PlayerRunTimeInfo.Current;

        // 初始化游戏数据
        aviationSystem = new AviationSystem();

        // 初始化航线控制器
        InitAirLineController();

        Debug.Log($"[{Name}] Enter - 进入游戏逻辑循环, 资产={playerRunTimeInfo?.Assets}");
    }

    /// <summary>
    /// 初始化航线控制器
    /// </summary>
    private void InitAirLineController()
    {
        var existing = Object.FindFirstObjectByType<AirLineController>();
        if (existing == null)
        {
            AirLineController.CreateInScene();
        }
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // ComposeState 的 Update 会自动处理子状态
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        // 清理运行时数据
        PlayerRunTimeInfo.Current = null;
        playerRunTimeInfo = null;
        aviationSystem = null;

        Debug.Log($"[{Name}] Exit - 退出游戏逻辑循环，数据已清理");
    }

    /// <summary>
    /// 获取当前子状态名称
    /// </summary>
    public string CurrentSubStateName => CurrentSubState?.Name ?? "None";
}
