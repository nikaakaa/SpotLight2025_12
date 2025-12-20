using UnityEngine;

/// <summary>
/// 航空模拟器 Buff 模块基类
/// 用于在结算时修正收益、倍率、成本等数值
/// 
/// 使用方式：
/// 1. 继承此类创建具体的 Buff 模块
/// 2. 在 BuffData 的 buffModuleList 中添加模块实例
/// 3. 结算时通过 BuffHandler.TriggerCustom() 触发回调
/// </summary>
public abstract class AviationBuffModule : BuffModuleBase
{
    [Header("航空 Buff 通用配置")]

    /// <summary>
    /// 目标节点等级（0 = 所有等级）
    /// </summary>
    [Tooltip("目标节点等级，0 表示所有等级")]
    [Range(0, 5)]
    public int targetNodeLevel = 0;

    /// <summary>
    /// 目标结构类型（用于结构倍率修正）
    /// </summary>
    public E_StructureType targetStructureType = E_StructureType.All;

    /// <summary>
    /// 检查节点等级是否匹配
    /// </summary>
    protected bool IsNodeLevelMatch(int nodeLevel)
    {
        return targetNodeLevel == 0 || targetNodeLevel == nodeLevel;
    }

    /// <summary>
    /// 检查结构类型是否匹配
    /// </summary>
    protected bool IsStructureTypeMatch(E_StructureType structureType)
    {
        return targetStructureType == E_StructureType.All || targetStructureType == structureType;
    }
}

