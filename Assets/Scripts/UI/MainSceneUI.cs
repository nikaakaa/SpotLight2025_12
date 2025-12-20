using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主场景 UI - 主菜单界面
/// 包含：开始游戏、退出
/// </summary>
public class MainSceneUI : MonoBehaviour
{
    public static MainSceneUI Instance { get; private set; }

    [Header("按钮引用")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;

    [Header("动画设置")]
    [SerializeField] private float buttonAnimDuration = 0.2f;
    [SerializeField] private float buttonHoverScale = 1.1f;

    private void Awake()
    {
        Instance = this;

        // 自动查找按钮引用（如果未在 Inspector 中赋值）
        if (startButton == null)
            startButton = transform.Find("Button")?.GetComponent<Button>();
        if (exitButton == null)
            exitButton = transform.Find("Button (1)")?.GetComponent<Button>();
    }

    private void Start()
    {
        // 绑定按钮事件
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClick);
            AddButtonHoverEffect(startButton);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClick);
            AddButtonHoverEffect(exitButton);
        }

        // 入场动画
        PlayEntranceAnimation();
    }

    #region 按钮事件

    /// <summary>
    /// 开始游戏
    /// </summary>
    private void OnStartClick()
    {
        Debug.Log("[MainSceneUI] 点击开始游戏");

        // 播放按钮动画
        if (startButton != null)
        {
            startButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
        }

        // 隐藏菜单界面
        Hide();

        // 通知状态机开始游戏
        if (GameProcedure.Instance != null)
        {
            GameProcedure.Instance.Context.Send(GameEvent.StartRound);
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    private void OnExitClick()
    {
        Debug.Log("[MainSceneUI] 点击退出游戏");

        // 播放退出动画后退出
        var canvasGroup = GetComponent<CanvasGroup>();
        var seq = DOTween.Sequence();
        seq.Append(transform.DOScale(0.9f, 0.2f));
        if (canvasGroup != null)
        {
            seq.Join(canvasGroup.DOFade(0f, 0.3f));
        }
        seq.OnComplete(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    #endregion

    #region 动画效果

    /// <summary>
    /// 入场动画
    /// </summary>
    private void PlayEntranceAnimation()
    {
        // 按钮依次滑入
        Button[] buttons = { startButton, exitButton };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            var rt = buttons[i].GetComponent<RectTransform>();
            if (rt == null) continue;

            // 保存原始位置
            Vector2 originalPos = rt.anchoredPosition;

            // 从右侧滑入
            rt.anchoredPosition = new Vector2(originalPos.x + 500f, originalPos.y);
            rt.DOAnchorPos(originalPos, 0.5f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.1f * i);
        }
    }

    /// <summary>
    /// 添加按钮悬停效果
    /// </summary>
    private void AddButtonHoverEffect(Button button)
    {
        var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // 鼠标进入
        var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((data) =>
        {
            button.transform.DOScale(buttonHoverScale, buttonAnimDuration).SetEase(Ease.OutBack);
        });
        eventTrigger.triggers.Add(entryEnter);

        // 鼠标离开
        var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
        };
        entryExit.callback.AddListener((data) =>
        {
            button.transform.DOScale(1f, buttonAnimDuration).SetEase(Ease.OutQuad);
        });
        eventTrigger.triggers.Add(entryExit);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 显示主菜单
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        PlayEntranceAnimation();
    }

    /// <summary>
    /// 隐藏主菜单
    /// </summary>
    public void Hide()
    {
        DOTween.Sequence()
            .Append(transform.DOScale(0.9f, 0.2f))
            .OnComplete(() => gameObject.SetActive(false));
    }

    #endregion
}
