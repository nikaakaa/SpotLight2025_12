using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 随机节点状态 - 使用簇状生长算法生成节点
/// 特点：大部分节点靠近已有节点生成，越高级的节点越稀有且生成越远
/// 摄像机会缓动到新生成的节点位置
/// </summary>
public class RandomNodeState : LeafState<GameProcedureContext>
{
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
            ctx.Next();
            return;
        }

        // 设置六边形参数（如果还没设置）
        HexMetrics.SetSize(1f);
        HexMetrics.SetOrientation(true); // Pointy-top

        // 获取当前回合数
        int currentRound = PlayerRunTimeInfo.Current?.CurrentRound ?? 1;

        // 计算本回合生成的节点数量
        int nodeCount = NodeSpawnConfig.NodesPerRound;

        // 使用簇状生长算法生成节点（返回生成的坐标）
        List<HexCoord> generatedCoords = AviationSystem.Instance.GenerateNodesWithClusterGrowth(nodeCount, currentRound);

        Debug.Log($"[{Name}] 簇状生长节点生成完成，回合 {currentRound}，当前节点数: {AviationSystem.Instance.aviationNodeDict.Count}");

        // 摄像机移动到新生成的第一个节点位置
        if (generatedCoords != null && generatedCoords.Count > 0)
        {
            HexCoord firstNewNode = generatedCoords[0];

            if (CinemachineCameraController.Instance != null)
            {
                // 摄像机缓动到新节点位置
                CinemachineCameraController.Instance.MoveToHex(firstNewNode, 0.8f);
                Debug.Log($"[{Name}] 摄像机移动到新节点: {firstNewNode}");
            }
        }

        // 自动进入下一状态
        ctx.Next();
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 节点生成是同步的，这里无需处理
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 随机节点生成完成");
    }
}
