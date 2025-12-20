using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 结算状态 - 处理回合结算的计算、动画和数据更新逻辑
/// 
/// 结算流程：
/// 1. 结构识别 - 检测所有航线结构（环、单线、放射）
/// 2. 结算计算 - 计算收益和成本
/// 3. 播放动画 - 小丑牌风格的结算演出
/// 4. 应用结算 - 更新玩家资产
/// 5. 破产检测 - 检查是否破产
/// </summary>
public class SettlementState : LeafState<GameProcedureContext>
{
    private SettlementCalculator settlementCalculator;
    private AllStructures currentStructures;
    private SettlementResult currentResult;
    private bool settlementComplete;
    private bool animationStarted;

    // 缓存引用
    private AviationSystem aviationSystem;
    private PlayerRunTimeInfo playerInfo;
    private GameProcedureContext currentContext;

    public SettlementState()
    {
        Name = nameof(SettlementState);
        settlementCalculator = new SettlementCalculator();
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 开始回合结算");

        settlementComplete = false;
        animationStarted = false;
        currentContext = ctx;

        // 获取游戏逻辑状态
        var gameLogicState = GameProcedure.Instance?.GameLogicState;
        if (gameLogicState == null)
        {
            Debug.LogError($"[{Name}] GameLogicState 为空");
            settlementComplete = true;
            return;
        }

        aviationSystem = gameLogicState.aviationSystem;
        playerInfo = gameLogicState.playerRunTimeInfo;

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

        // 打印结算摘要
        PrintSettlementSummary();

        // Step 4: 启动结算动画
        Debug.Log($"[{Name}] Step 3: 播放结算动画...");
        PlaySettlementAnimationAsync().Forget();
    }

    /// <summary>
    /// 异步播放结算动画
    /// </summary>
    private async UniTaskVoid PlaySettlementAnimationAsync()
    {
        animationStarted = true;

        // 检查动画器是否存在
        if (SettlementAnimator.Instance != null)
        {
            // 播放完整动画
            await SettlementAnimator.Instance.PlayAsync(
                currentStructures,
                currentResult,
                aviationSystem
            );
        }
        else
        {
            Debug.LogWarning($"[{Name}] SettlementAnimator 不存在，跳过动画");
            // 没有动画器时直接等待一小段时间
            await UniTask.Delay(500);
        }

        // 动画完成后应用结算结果
        Debug.Log($"[{Name}] Step 4: 应用结算结果...");
        playerInfo.ApplySettlement(currentResult);

        // 破产检测
        if (playerInfo.IsBankrupt)
        {
            Debug.LogWarning($"[{Name}] 玩家破产！资产={playerInfo.Assets}");
            currentContext.Send(GameEvent.Bankruptcy);
            settlementComplete = true;
            return;
        }

        // 进入下一状态
        Debug.Log($"[{Name}] 结算完成，进入下一状态");
        settlementComplete = true;
        currentContext.Next();
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 动画由 UniTask 控制，不需要 Update 逻辑
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 回合结算完成");

        // 清理临时数据
        currentStructures = null;
        currentResult = null;
        aviationSystem = null;
        playerInfo = null;
        currentContext = null;
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

