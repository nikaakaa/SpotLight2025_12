/// <summary>
/// 游戏逻辑 Buff 系统接口
/// 用于统一管理 Buff 的触发（如计算收益、成本等）
/// </summary>
public interface IGameplayBuffSystem
{
    void TriggerNodeIncomeBuffs(int nodeLevel, IncomeModifier modifier);
    void TriggerStructureMultiplierBuffs(E_StructureType type, MultiplierModifier modifier);
    void TriggerNodeCostBuffs(int nodeLevel, IncomeModifier modifier);
    void TriggerEdgeCostBuffs(int length, IncomeModifier modifier);
    void TriggerSettlementCompleteBuffs(SettlementResult result);
}
