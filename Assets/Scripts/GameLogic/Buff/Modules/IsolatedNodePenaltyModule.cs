using UnityEngine;

/// <summary>
/// 孤立节点惩罚模块
/// 用于实现如 "只参与一条航线的节点收益-30% - 空载惩罚政策" 的效果
/// 
/// 触发时机: OnCalculateNodeBaseIncome
/// 参数: object[0] = nodeLevel (int), object[1] = IncomeModifier (引用类型)
/// 
/// 注意：需要在外部检查节点的边数量，并传入额外参数
/// </summary>
[CreateAssetMenu(fileName = "IsolatedNodePenaltyModule", menuName = "Buff/Aviation/IsolatedNodePenaltyModule")]
public class IsolatedNodePenaltyModule : AviationBuffModule
{
    [Header("孤立节点惩罚配置")]

    /// <summary>
    /// 收益惩罚倍率（0.7 = 收益降低30%）
    /// </summary>
    [Tooltip("收益惩罚倍率，0.7 = 收益降低30%")]
    [Range(0f, 1f)]
    public float penaltyMultiplier = 0.7f;

    /// <summary>
    /// 触发惩罚的最大边数（航线数）
    /// 边数 <= 此值时触发惩罚
    /// </summary>
    [Tooltip("触发惩罚的最大边数")]
    public int maxEdgeCountForPenalty = 1;

    public IsolatedNodePenaltyModule()
    {
        buffCallBackType = E_BuffCallBackType.OnCalculateNodeBaseIncome;
    }

    public override void Apply(BuffInfo buffInfo, params object[] customInfo)
    {
        if (customInfo == null || customInfo.Length < 3) return;

        // 参数解析
        int nodeLevel = (int)customInfo[0];
        var modifier = customInfo[1] as IncomeModifier;
        int edgeCount = (int)customInfo[2]; // 需要额外传入边数量

        if (modifier == null) return;

        // 检查节点等级是否匹配
        if (!IsNodeLevelMatch(nodeLevel)) return;

        // 检查边数量是否满足惩罚条件
        if (edgeCount > maxEdgeCountForPenalty) return;

        // 应用惩罚
        modifier.Multiplier *= penaltyMultiplier;

        Debug.Log($"[IsolatedNodePenaltyModule] 节点等级{nodeLevel} 边数{edgeCount} 收益×{penaltyMultiplier:F2}");
    }
}
