using UnityEngine;

/// <summary>
/// 路线连接与修改状态 - 处理路线编辑的复杂交互逻辑
/// 左键：选择节点并连线
/// 右键按住：进入删除模式，滑过边时删除
/// </summary>
public class ConnectModifyRouteState : LeafState<GameProcedureContext>
{
    // 当前选中的第一个节点（起点）
    private CityNode firstSelectedNode = null;
    // 当前悬停的节点
    private CityNode currentHoveredNode = null;
    // 当前悬停的边
    private EdgeLineView currentHoveredEdge = null;
    // 是否处于删除模式（右键按住）
    private bool isDeleteMode = false;

    public ConnectModifyRouteState()
    {
        Name = nameof(ConnectModifyRouteState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 进入路线编辑模式（左键连线，右键按住删除）");
        ClearSelection();
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // 检测右键状态
        if (Input.GetMouseButtonDown(1))
        {
            EnterDeleteMode();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            ExitDeleteMode();
        }

        // 根据模式处理不同的射线检测
        if (isDeleteMode)
        {
            HandleEdgeRaycast();
        }
        else
        {
            HandleNodeRaycast();
        }
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 退出路线编辑模式");
        ExitDeleteMode();
        ClearSelection();
    }

    /// <summary>
    /// 进入删除模式
    /// </summary>
    private void EnterDeleteMode()
    {
        isDeleteMode = true;
        // 清除节点选择状态
        if (firstSelectedNode != null)
        {
            firstSelectedNode.SetSelected(false);
            firstSelectedNode = null;
        }
        Debug.Log($"[{Name}] 进入删除模式");
    }

    /// <summary>
    /// 退出删除模式
    /// </summary>
    private void ExitDeleteMode()
    {
        isDeleteMode = false;
        // 清除边的悬停状态
        if (currentHoveredEdge != null)
        {
            currentHoveredEdge.SetHovered(false);
            currentHoveredEdge = null;
        }
        Debug.Log($"[{Name}] 退出删除模式");
    }

    /// <summary>
    /// 节点射线检测（用于连线）
    /// </summary>
    private void HandleNodeRaycast()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        CityNode hitNode = null;

        // 3D 射线检测
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            hitNode = hit.collider.GetComponentInParent<CityNode>();
        }

        // 处理悬停状态变化
        if (hitNode != currentHoveredNode)
        {
            if (currentHoveredNode != null)
            {
                currentHoveredNode.SetHovered(false);
            }

            if (hitNode != null)
            {
                hitNode.SetHovered(true);
            }

            currentHoveredNode = hitNode;
        }

        // 检测左键点击
        if (Input.GetMouseButtonDown(0) && hitNode != null)
        {
            OnNodeClicked(hitNode);
        }
    }

    /// <summary>
    /// 边射线检测（用于删除）- 使用 3D 射线检测 BoxCollider
    /// </summary>
    private void HandleEdgeRaycast()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        EdgeLineView hitEdge = null;

        // 3D 射线检测（用于检测 BoxCollider）
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // 先尝试在碰撞体所在物体上找 EdgeColliderReference
            EdgeColliderReference edgeRef = hit.collider.GetComponent<EdgeColliderReference>();
            if (edgeRef != null)
            {
                hitEdge = edgeRef.edgeLineView;
            }
            else
            {
                // 兜底：尝试在父物体上找 EdgeLineView
                hitEdge = hit.collider.GetComponentInParent<EdgeLineView>();
            }
        }

        // 处理边的悬停状态变化
        if (hitEdge != currentHoveredEdge)
        {
            if (currentHoveredEdge != null)
            {
                currentHoveredEdge.SetHovered(false);
            }

            if (hitEdge != null)
            {
                hitEdge.SetHovered(true);

                // 右键按住时，滑过边立即删除
                DeleteEdge(hitEdge);
                hitEdge = null; // 已删除，清空引用
            }

            currentHoveredEdge = hitEdge;
        }
    }

    /// <summary>
    /// 删除边
    /// </summary>
    private void DeleteEdge(EdgeLineView edgeView)
    {
        if (edgeView == null || edgeView.aviationEdge == null) return;

        Debug.Log($"[{Name}] 删除航线: {edgeView.aviationEdge.fromNode?.hexCoord} -> {edgeView.aviationEdge.toNode?.hexCoord}");

        AviationSystem.Instance?.RemoveEdgeWithView(edgeView.aviationEdge, edgeView);
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
            clickedNode.SetHovered(false);
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
        if (currentHoveredEdge != null)
        {
            currentHoveredEdge.SetHovered(false);
            currentHoveredEdge = null;
        }
    }
}

