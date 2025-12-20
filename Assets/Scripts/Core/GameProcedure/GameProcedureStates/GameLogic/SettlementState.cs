using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

        // Step 2: 执行结算计算
        Debug.Log($"[{Name}] Step 2: 结算计算...");

        // 传递 playerInfo 作为 IGameplayBuffSystem 接口
        IGameplayBuffSystem buffSystem = playerInfo;

        currentResult = settlementCalculator.Calculate(
            aviationSystem,
            currentStructures,
            playerInfo,
            buffSystem
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

        // ★ 在异步方法开始时捕获所有引用到局部变量
        // 这样即使 OnExit 清空了成员变量，局部变量仍然有效
        var localPlayerInfo = playerInfo;
        var localResult = currentResult;
        var localStructures = currentStructures;
        var localAviationSystem = aviationSystem;
        var localContext = currentContext;

        // 预先验证
        if (localPlayerInfo == null || localResult == null)
        {
            Debug.LogError($"[{Name}] 关键数据为空，无法进行结算");
            settlementComplete = true;
            return;
        }

        // 播放结算音效
        await PlaySettlementSoundAsync();

        // 检查动画器是否存在
        if (SettlementAnimator.Instance != null)
        {
            // 播放完整动画
            await SettlementAnimator.Instance.PlayAsync(
                localStructures,
                localResult,
                localAviationSystem
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
        localPlayerInfo.ApplySettlement(localResult);

        // 破产检测
        if (localPlayerInfo.IsBankrupt)
        {
            Debug.LogWarning($"[{Name}] 玩家破产！资产={localPlayerInfo.Assets}");
            localContext?.Send(GameEvent.Bankruptcy);
            settlementComplete = true;
            return;
        }

        // 进入下一状态
        Debug.Log($"[{Name}] 结算完成，进入下一状态");
        settlementComplete = true;
        localContext?.Next();
    }

    /// <summary>
    /// 播放结算音效（异步版本）
    /// </summary>
    private async UniTask PlaySettlementSoundAsync()
    {
        try
        {
            Debug.Log($"[{Name}] 开始加载音效...");

            // 方式1：使用 Resources.Load（需要音频在 Resources 文件夹中）
            AudioClip clip = Resources.Load<AudioClip>("Audio/Retro9");
            
            if (clip != null)
            {
                PlayAudioClip(clip);
                Debug.Log($"[{Name}] 使用 Resources.Load 成功加载音效");
                return;
            }

            Debug.Log($"[{Name}] Resources.Load 失败，尝试 Addressables...");

            // 方式2：使用 Addressables（备选方案）
            var handle = Addressables.LoadAssetAsync<AudioClip>("Assets/Audio/Retro9.mp3");
            
            // 等待加载完成
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                PlayAudioClip(handle.Result);
                Debug.Log($"[{Name}] 使用 Addressables 成功加载音效");
            }
            else
            {
                Debug.LogWarning($"[{Name}] Addressables 加载失败");
            }

            Addressables.Release(handle);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[{Name}] 播放音效异常: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 播放音频片段
    /// </summary>
    private void PlayAudioClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[{Name}] AudioClip 为空");
            return;
        }

        // 创建临时 GameObject 播放音效
        GameObject tempAudioGO = new GameObject("SettlementAudio");
        AudioSource audioSource = tempAudioGO.AddComponent<AudioSource>();
        
        audioSource.clip = clip;
        audioSource.volume = 1.0f;
        audioSource.Play();

        Debug.Log($"[{Name}] 播放结算音效: {clip.name} (长度: {clip.length}s)");

        // 播放完毕后销毁
        Object.Destroy(tempAudioGO, clip.length + 0.1f);
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

