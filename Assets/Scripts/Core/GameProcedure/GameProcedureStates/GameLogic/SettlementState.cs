using UnityEngine;

/// <summary>
/// 结算状态 - 处理回合结算的计算、动画和数据更新逻辑
/// 
/// 结算流程：
/// 1. 结构识别 - 检测所有航线结构（环、单线、放射）
/// 2. 结算计算 - 计算收益和成本
/// 3. 应用结算 - 更新玩家资产
/// 4. 破产检测 - 检查是否破产
/// </summary>
public class SettlementState : LeafState<GameProcedureContext>
{
    private SettlementCalculator settlementCalculator;
    private AllStructures currentStructures;
    private SettlementResult currentResult;
    private bool settlementComplete;
    private float settlementTimer;

    // 结算动画时长（秒）- 简化版先用固定时长
    private const float SETTLEMENT_DISPLAY_TIME = 1.5f;

    public SettlementState()
    {
        Name = nameof(SettlementState);
        settlementCalculator = new SettlementCalculator();
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 开始回合结算");

        settlementComplete = false;
        settlementTimer = 0f;

        // 获取游戏逻辑状态
        var gameLogicState = GameProcedure.Instance?.GameLogicState;
        if (gameLogicState == null)
        {
            Debug.LogError($"[{Name}] GameLogicState 为空");
            settlementComplete = true;
            return;
        }

        var aviationSystem = gameLogicState.aviationSystem;
        var playerInfo = gameLogicState.playerRunTimeInfo;

        if (aviationSystem == null || playerInfo == null)
        {
            Debug.LogError($"[{Name}] aviationSystem 或 playerInfo 为空");
            settlementComplete = true;
            return;
        }

        // Step 1: 结构识别
        Debug.Log($"[{Name}] Step 1: 结构识别...");
        currentStructures = StructureDetector.DetectAll(aviationSystem);

        // Step 2: 获取 BuffHandler（如果有）
        BuffHandler buffHandler = null;
        // TODO: 从玩家对象获取 BuffHandler
        // buffHandler = playerInfo.GetBuffHandler();

        // Step 3: 执行结算计算
        Debug.Log($"[{Name}] Step 2: 结算计算...");
        currentResult = settlementCalculator.Calculate(
            aviationSystem,
            currentStructures,
            playerInfo,
            buffHandler
        );

        // Step 4: 应用结算结果
        Debug.Log($"[{Name}] Step 3: 应用结算结果...");
        playerInfo.ApplySettlement(currentResult);

        // 打印结算摘要
        PrintSettlementSummary();

        // Step 5: 检测破产
        if (playerInfo.IsBankrupt)
        {
            Debug.LogWarning($"[{Name}] 玩家破产！资产={playerInfo.Assets}");
            ctx.Send(GameEvent.Bankruptcy);
            settlementComplete = true;
            return;
        }

        Debug.Log($"[{Name}] 结算完成，等待显示...");
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        if (settlementComplete) return;

        // 简化版：等待固定时间后自动进入下一状态
        settlementTimer += Time.deltaTime;

        if (settlementTimer >= SETTLEMENT_DISPLAY_TIME)
        {
            settlementComplete = true;
            Debug.Log($"[{Name}] 结算显示完成，进入下一状态");
            ctx.Next();
        }

        // TODO: 未来可以在这里添加结算动画逻辑
        // - 结构高亮动画
        // - 数字滚动动画
        // - 资产变化动画
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 回合结算完成");

        // 清理临时数据
        currentStructures = null;
        currentResult = null;
    }

    /// <summary>
    /// 打印结算摘要
    /// </summary>
    private void PrintSettlementSummary()
    {
        if (currentResult == null) return;

        Debug.Log("========== 结算摘要 ==========");
        Debug.Log($"结构数量: 环形={currentStructures?.Rings.Count ?? 0}, 放射={currentStructures?.Radials.Count ?? 0}, 单线={currentStructures?.SingleLines.Count ?? 0}");
        Debug.Log($"总收益: {currentResult.totalIncome}");
        Debug.Log($"节点成本: {currentResult.totalNodeCost}");
        Debug.Log($"航线成本: {currentResult.totalEdgeCost}");
        Debug.Log($"总成本: {currentResult.TotalCost}");
        Debug.Log($"净利润: {currentResult.NetProfit}");
        Debug.Log($"资产变化: {currentResult.assetsBefore} -> {currentResult.assetsAfter}");
        Debug.Log("==============================");
    }
}
