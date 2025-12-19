using UnityEngine;

/// <summary>
/// 简单摄像机控制器
/// 功能：中键拖动平移，滚轮缩放，轻微缓动
/// 直接挂到 Main Camera 上即可使用
/// </summary>
public class CinemachineCameraController : MonoBehaviour
{
    [Header("平移设置")]
    [SerializeField] private float panSpeed = 0.5f;
    [SerializeField] private float panSmoothing = 0.1f;  // 0=无缓动, 1=很慢

    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;
    [SerializeField] private float zoomSmoothing = 0.1f;

    private Camera cam;
    private Vector3 lastMousePos;
    private float targetZoom;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (cam != null && cam.orthographic)
        {
            targetZoom = cam.orthographicSize;
        }
    }

    void Update()
    {
        HandlePan();
        HandleZoom();
    }

    void HandlePan()
    {
        // 中键按下
        if (Input.GetMouseButtonDown(2))
        {
            lastMousePos = Input.mousePosition;
        }

        // 中键拖动
        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            // 根据缩放比例调整移动速度
            float zoomFactor = cam != null && cam.orthographic ? cam.orthographicSize / 10f : 1f;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * panSpeed * zoomFactor * 0.01f;

            // 直接移动（带轻微缓动）
            transform.position = Vector3.Lerp(transform.position, transform.position + move, 1f - panSmoothing);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetZoom -= scroll * zoomSpeed * targetZoom * 0.5f;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // 应用缩放（带缓动）
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, 1f - zoomSmoothing);
        }
    }
}

