using UnityEngine;

/// <summary>
/// 节点收益加成模块
/// 用于实现如 "所有lv1节点收益+10" 的效果
/// 
/// 触发时机: OnCalculateNodeBaseIncome
/// 参数: object[0] = nodeLevel (int), object[1] = IncomeModifier (引用类型)
/// </summary>
[CreateAssetMenu(fileName = "NodeIncomeAddModule", menuName = "Buff/Aviation/NodeIncomeAddModule")]
public class NodeIncomeAddModule : AviationBuffModule
{
    [Header("收益加成配置")]

    /// <summary>
    /// 收益加成值（正数增加，负数减少）
    /// </summary>
    [Tooltip("收益加成值，正数增加，负数减少")]
    public int incomeAddValue = 10;

    public NodeIncomeAddModule()
    {
        buffCallBackType = E_BuffCallBackType.OnCalculateNodeBaseIncome;
    }

    public override void Apply(BuffInfo buffInfo, params object[] customInfo)
    {
        if (customInfo == null || customInfo.Length < 2) return;

        // 参数解析
        int nodeLevel = (int)customInfo[0];
        var modifier = customInfo[1] as IncomeModifier;

        if (modifier == null) return;

        // 检查节点等级是否匹配
        if (!IsNodeLevelMatch(nodeLevel)) return;

        // 应用加成（考虑层数）
        int stackCount = buffInfo.CurStack > 0 ? buffInfo.CurStack : 1;
        modifier.FlatBonus += incomeAddValue * stackCount;

        Debug.Log($"[NodeIncomeAddModule] 节点等级{nodeLevel} 收益+{incomeAddValue * stackCount}");
    }
}

