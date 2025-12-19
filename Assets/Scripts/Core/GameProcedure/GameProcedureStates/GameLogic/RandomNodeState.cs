using UnityEngine;

/// <summary>
/// 随机节点状态 - 处理随机节点的生成、配置和展示逻辑
/// </summary>
public class RandomNodeState : LeafState<GameProcedureContext>
{
    [Header("生成配置")]
    private int nodeCount = 5;      // 每回合生成的节点数量
    private int gridRadius = 5;     // 生成范围（六边形半径）

    public RandomNodeState()
    {
        Name = nameof(RandomNodeState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 开始生成随机节点");
        
        // 确保 AviationSystem 已初始化
        if (AviationSystem.Instance == null)
        {
            Debug.LogWarning($"[{Name}] AviationSystem 未初始化，跳过节点生成");
            return;
        }

        // 设置六边形参数
        HexMetrics.SetSize(1f);
        HexMetrics.SetOrientation(true); // Pointy-top

        // 生成随机节点
        AviationSystem.Instance.GenerateRandomNodes(nodeCount, gridRadius);
        
        Debug.Log($"[{Name}] 随机节点生成完成，当前节点数: {AviationSystem.Instance.aviationNodeDict.Count}");
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 节点生成是异步的，可以在这里检查是否完成
        // 目前简化处理，直接在 OnEnter 中完成
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 随机节点生成完成");
    }
}
