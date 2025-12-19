using UnityEngine;

/// <summary>
/// 路线连接与修改状态 - 处理路线编辑的复杂交互逻辑
/// 使用射线检测处理节点选择和连线
/// </summary>
public class ConnectModifyRouteState : LeafState<GameProcedureContext>
{
    // 当前选中的第一个节点（起点）
    private CityNode firstSelectedNode = null;
    // 当前悬停的节点
    private CityNode currentHoveredNode = null;

    public ConnectModifyRouteState()
    {
        Name = nameof(ConnectModifyRouteState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 进入路线编辑模式");
        ClearSelection();
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        HandleRaycast();
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 退出路线编辑模式");
        ClearSelection();
    }

    /// <summary>
    /// 射线检测处理（3D Collider）
    /// </summary>
    private void HandleRaycast()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        CityNode hitNode = null;

        // 3D 射线检测
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // 支持碰撞体在子物体上的情况，避免悬停状态更新不及时
            hitNode = hit.collider.GetComponentInParent<CityNode>();
        }

        // 处理悬停状态变化
        if (hitNode != currentHoveredNode)
        {
            if (currentHoveredNode != null)
            {
                currentHoveredNode.SetHovered(false);
            }

            if (hitNode != null) // The condition '&& hitNode != selectedNode' was not present, so no change is made here.
            {
                hitNode.SetHovered(true);
            }

            currentHoveredNode = hitNode;
        }

        // 检测点击
        if (Input.GetMouseButtonDown(0) && hitNode != null)
        {
            OnNodeClicked(hitNode);
        }
    }

    /// <summary>
    /// 节点被点击
    /// </summary>
    private void OnNodeClicked(CityNode clickedNode)
    {
        Debug.Log($"[{Name}] 点击节点: {clickedNode.aviationNode?.nodeData?.Name ?? "Unknown"} at {clickedNode.aviationNode?.hexCoord}");

        if (firstSelectedNode == null)
        {
            // 第一次点击：选择起点
            firstSelectedNode = clickedNode;
            clickedNode.SetSelected(true);
            clickedNode.SetHovered(false); // 选中时清除悬停状态，防止颜色残留
            Debug.Log($"[{Name}] 选择起点: {clickedNode.aviationNode?.hexCoord}");
        }
        else if (firstSelectedNode == clickedNode)
        {
            // 点击同一个节点：取消选择
            clickedNode.SetSelected(false);
            firstSelectedNode = null;
            Debug.Log($"[{Name}] 取消选择");
        }
        else
        {
            // 第二次点击：选择终点，创建连线
            Debug.Log($"[{Name}] 选择终点: {clickedNode.aviationNode?.hexCoord}，创建连线");

            if (AviationSystem.Instance != null &&
                firstSelectedNode.aviationNode != null &&
                clickedNode.aviationNode != null)
            {
                AviationSystem.Instance.AddEdgeWithView(
                    firstSelectedNode.aviationNode,
                    clickedNode.aviationNode,
                    firstSelectedNode.transform.parent
                );
            }

            // 重置选择状态
            firstSelectedNode.SetSelected(false);
            firstSelectedNode = null;
        }
    }

    /// <summary>
    /// 清除选择状态
    /// </summary>
    private void ClearSelection()
    {
        if (firstSelectedNode != null)
        {
            firstSelectedNode.SetSelected(false);
            firstSelectedNode = null;
        }
        if (currentHoveredNode != null)
        {
            currentHoveredNode.SetHovered(false);
            currentHoveredNode = null;
        }
    }
}
