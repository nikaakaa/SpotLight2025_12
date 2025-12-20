using UnityEngine;

/// <summary>
/// 简单摄像机控制器
/// 功能：中键拖动平移，滚轮缩放，轻微缓动
/// 直接挂到 Main Camera 上即可使用
/// </summary>
public class CinemachineCameraController : MonoBehaviour
{
    // ========== 单例 ==========
    public static CinemachineCameraController Instance { get; private set; }

    [Header("平移设置")]
    [SerializeField] private float panSpeed = 0.5f;
    [SerializeField] private float panSmoothing = 0.1f;  // 0=无缓动, 1=很慢

    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 100f;
    [SerializeField] private float zoomSmoothing = 0.1f;

    [Header("移动动画设置")]
    [SerializeField] private float moveDuration = 0.5f;  // 移动动画时长

    private Camera cam;
    private Vector3 lastMousePos;
    private float targetZoom;
    private bool isOrthographic;

    // 移动动画状态
    private bool isMoving = false;
    private Vector3 moveStartPos;
    private Vector3 moveTargetPos;
    private float moveTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (cam != null)
        {
            isOrthographic = cam.orthographic;
            if (isOrthographic)
            {
                targetZoom = cam.orthographicSize;
            }
            else
            {
                // 透视相机用 Z 位置控制缩放
                targetZoom = Mathf.Abs(transform.position.z);
            }
        }
        else
        {
            targetZoom = 20f;
        }

        Debug.Log($"[CameraController] 初始化: isOrthographic={isOrthographic}, targetZoom={targetZoom}");
    }

    void Update()
    {
        // 优先处理移动动画
        if (isMoving)
        {
            UpdateMoveAnimation();
        }
        else
        {
            // 按 Home 键回到中心点
            if (Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.H))
            {
                MoveToCenter();
            }

            HandlePan();
        }

        HandleZoom();
    }

    // ========== 公开 API ==========

    /// <summary>
    /// 移动摄像机到指定世界坐标（带缓动）
    /// </summary>
    /// <param name="worldPos">目标世界坐标（XY平面）</param>
    /// <param name="duration">动画时长（可选，默认使用 moveDuration）</param>
    public void MoveTo(Vector3 worldPos, float duration = -1f)
    {
        moveStartPos = transform.position;
        moveTargetPos = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        moveTimer = 0f;
        moveDuration = duration > 0 ? duration : this.moveDuration;
        isMoving = true;

        Debug.Log($"[CameraController] 移动到 ({worldPos.x:F1}, {worldPos.y:F1})");
    }

    /// <summary>
    /// 移动摄像机到指定六边形坐标（带缓动）
    /// </summary>
    public void MoveToHex(HexCoord hexCoord, float duration = -1f)
    {
        Vector3 worldPos = HexConverter2D.HexToWorld(hexCoord);
        MoveTo(worldPos, duration);
    }

    /// <summary>
    /// 回到地图中心点（带缓动）
    /// </summary>
    public void MoveToCenter(float duration = -1f)
    {
        MoveTo(Vector3.zero, duration);
        Debug.Log("[CameraController] 回到中心点");
    }

    /// <summary>
    /// 立即设置摄像机位置（无动画）
    /// </summary>
    public void SetPositionImmediate(Vector3 worldPos)
    {
        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        isMoving = false;
    }

    /// <summary>
    /// 立即回到中心点（无动画）
    /// </summary>
    public void SetCenterImmediate()
    {
        SetPositionImmediate(Vector3.zero);
    }

    /// <summary>
    /// 摄像机抖动效果
    /// </summary>
    /// <param name="strength">抖动强度</param>
    /// <param name="duration">抖动时长</param>
    /// <param name="vibrato">抖动频率</param>
    public void Shake(float strength = 0.15f, float duration = 0.2f, int vibrato = 10)
    {
        if (isShaking) return; // 避免重复抖动
        StartCoroutine(ShakeCoroutine(strength, duration, vibrato));
    }

    private bool isShaking = false;

    private System.Collections.IEnumerator ShakeCoroutine(float strength, float duration, int vibrato)
    {
        isShaking = true;
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        float interval = duration / vibrato;

        while (elapsed < duration)
        {
            // 随机偏移
            float x = Random.Range(-strength, strength);
            float y = Random.Range(-strength, strength);
            transform.position = originalPos + new Vector3(x, y, 0);

            elapsed += interval;
            yield return new WaitForSeconds(interval);
        }

        // 恢复原位
        transform.position = originalPos;
        isShaking = false;
    }

    // ========== 内部方法 ==========

    private void UpdateMoveAnimation()
    {
        moveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(moveTimer / moveDuration);

        // 使用 SmoothStep 缓动
        float smoothT = t * t * (3f - 2f * t);

        transform.position = Vector3.Lerp(moveStartPos, moveTargetPos, smoothT);

        if (t >= 1f)
        {
            isMoving = false;
            transform.position = moveTargetPos;
        }
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
            float zoomFactor = targetZoom / 20f;
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
            targetZoom -= scroll * zoomSpeed * (targetZoom / 10f);
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // 应用缩放（带缓动）
        if (cam != null)
        {
            float smoothFactor = 1f - zoomSmoothing;

            if (isOrthographic)
            {
                // 正交相机：调整 orthographicSize
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, smoothFactor);
            }
            else
            {
                // 透视相机：调整 Z 位置
                Vector3 pos = transform.position;
                pos.z = Mathf.Lerp(pos.z, -targetZoom, smoothFactor);
                transform.position = pos;
            }
        }
    }
}
