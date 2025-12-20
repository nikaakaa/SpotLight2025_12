using UnityEngine;

/// <summary>
/// 节点收益乘算模块
/// 用于实现如 "所有lv1节点收益×1.5" 的效果
/// 
/// 触发时机: OnCalculateNodeBaseIncome
/// 参数: object[0] = nodeLevel (int), object[1] = IncomeModifier (引用类型)
/// </summary>
[CreateAssetMenu(fileName = "NodeIncomeMultiplyModule", menuName = "Buff/Aviation/NodeIncomeMultiplyModule")]
public class NodeIncomeMultiplyModule : AviationBuffModule
{
    [Header("收益乘算配置")]

    /// <summary>
    /// 收益乘算倍率
    /// </summary>
    [Tooltip("收益乘算倍率，1.0 = 不变，0.6 = 减少40%，1.5 = 增加50%")]
    public float incomeMultiplier = 1.0f;

    public NodeIncomeMultiplyModule()
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

        // 应用乘算（层数不影响乘算，而是多次触发）
        modifier.Multiplier *= incomeMultiplier;

        Debug.Log($"[NodeIncomeMultiplyModule] 节点等级{nodeLevel} 收益×{incomeMultiplier}");
    }
}
