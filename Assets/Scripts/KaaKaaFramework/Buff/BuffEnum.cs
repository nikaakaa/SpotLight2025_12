public enum E_BuffUpdateTime
{
    //增加持续时间
    AddDuration,
    //刷新持续时间，增加层数
    Replace,
    //保留持续时间，增加层数
    KeepAndAddStack
}
public enum E_BuffRemoveStackUpdate
{
    ClearStack,
    ReduceStack,
}
public enum E_BuffCallBackType
{
    //这5个都是当...时的回调,不是代表他们的功能
    Create,
    AddStack,
    ReduceStack,
    Remove,
    Tick,

    //下面四个的顺序也就是他们的调用顺序
    BeforeDoDamage,
    BeforeGetDamage,
    AfterGetDamage,
    AfterDoDamage,

    // ========== 航空模拟器结算回调 ==========

    /// <summary>
    /// 计算节点基础收益时触发（参数: nodeLevel, ref baseIncome）
    /// </summary>
    OnCalculateNodeBaseIncome,

    /// <summary>
    /// 计算结构倍率时触发（参数: structureType, ref multiplier）
    /// </summary>
    OnCalculateStructureMultiplier,

    /// <summary>
    /// 计算节点成本时触发（参数: nodeLevel, ref cost）
    /// </summary>
    OnCalculateNodeCost,

    /// <summary>
    /// 计算航线成本时触发（参数: edgeLength, ref cost）
    /// </summary>
    OnCalculateEdgeCost,

    /// <summary>
    /// 结算完成后触发（参数: SettlementResult）
    /// </summary>
    OnSettlementComplete,
}