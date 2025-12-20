using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 浮动数字组件 - 显示在节点头顶的收益/成本数字
/// 支持弹出、数值变化、飞向目标等动画
/// </summary>
public class FloatingNumber : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Canvas canvas;

    private float currentValue;
    private Camera mainCamera;

    void Awake()
    {
        if (numberText == null)
            numberText = GetComponentInChildren<TMP_Text>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvas == null)
            canvas = GetComponent<Canvas>();

        mainCamera = Camera.main;

        // 设置 Canvas 为最高层级
        if (canvas != null)
        {
            canvas.sortingOrder = RenderLayers.FLOATING_NUMBER;
        }

        // 应用基础字体大小（从全局配置获取）
        if (numberText != null)
        {
            numberText.fontSize = SettlementAnimConfig.FloatingNumberBaseFontSize;
        }
    }

    /// <summary>
    /// 初始化并显示数字
    /// </summary>
    public async UniTask Show(float value, Color color, float duration = 0.3f)
    {
        currentValue = value;
        UpdateText(value, color);

        // 设置初始状态
        transform.localScale = Vector3.zero;
        if (canvasGroup != null)
            canvasGroup.alpha = 0;

        // 弹出动画
        var seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1f, duration).SetEase(Ease.OutBack));
        if (canvasGroup != null)
            seq.Join(canvasGroup.DOFade(1f, duration * 0.5f));

        await WaitForTween(seq);
    }

    /// <summary>
    /// 显示倍率（格式：×1.5）
    /// </summary>
    public async UniTask ShowMultiplier(float multiplier, Color color, float duration = 0.3f)
    {
        currentValue = multiplier;

        // 设置倍率文本格式
        if (numberText != null)
        {
            numberText.text = $"×{multiplier:F1}";
            numberText.color = color;
        }

        // 设置初始状态
        transform.localScale = Vector3.zero;
        if (canvasGroup != null)
            canvasGroup.alpha = 0;

        // 弹出动画（更夸张的效果）
        var seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.3f, duration * 0.5f).SetEase(Ease.OutBack));
        seq.Append(transform.DOScale(1f, duration * 0.5f).SetEase(Ease.InOutSine));
        if (canvasGroup != null)
            seq.Join(canvasGroup.DOFade(1f, duration * 0.3f));

        await WaitForTween(seq);
    }

    /// <summary>
    /// 数值变化动画（Buff加成时使用）
    /// </summary>
    public async UniTask AnimateTo(float newValue, Color color, float duration = 0.3f)
    {
        float startValue = currentValue;

        // 缩放弹跳效果
        var scaleSeq = DOTween.Sequence();
        scaleSeq.Append(transform.DOScale(1.2f, duration * 0.4f).SetEase(Ease.OutQuad));
        scaleSeq.Append(transform.DOScale(1f, duration * 0.6f).SetEase(Ease.OutBounce));

        // 数值滚动
        var tween = DOTween.To(() => currentValue, x =>
        {
            currentValue = x;
            UpdateText(x, color);
        }, newValue, duration);

        await WaitForTween(tween);
    }

    /// <summary>
    /// 飞向目标位置（汇总到总收益时使用）
    /// </summary>
    public async UniTask FlyTo(Vector3 worldTarget, float duration = 0.5f)
    {
        // 先变小
        transform.DOScale(0.5f, duration);

        // 飞向目标
        var moveTween = transform.DOMove(worldTarget, duration).SetEase(Ease.InQuad);
        await WaitForTween(moveTween);

        // 淡出并销毁
        if (canvasGroup != null)
        {
            var fadeTween = canvasGroup.DOFade(0, 0.1f);
            await WaitForTween(fadeTween);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 淡出并销毁
    /// </summary>
    public async UniTask FadeOutAndDestroy(float duration = 0.3f)
    {
        if (canvasGroup != null)
        {
            var tween = canvasGroup.DOFade(0, duration);
            await WaitForTween(tween);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// 设置数值（无动画）
    /// </summary>
    public void SetValue(float value, Color color)
    {
        currentValue = value;
        UpdateText(value, color);
    }

    /// <summary>
    /// 设置世界坐标位置
    /// </summary>
    public void SetWorldPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
    }

    private void UpdateText(float value, Color color)
    {
        if (numberText == null) return;

        // 根据正负显示不同格式
        string prefix = value >= 0 ? "+" : "";
        numberText.text = $"{prefix}{value:N0}";
        numberText.color = color;

        // 直接使用全局配置的映射函数计算字体大小
        numberText.fontSize = SettlementAnimConfig.GetFontSizeByValue(value);
    }

    /// <summary>
    /// 立即销毁
    /// </summary>
    public void DestroyImmediate()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// 等待 Tween 完成（替代 ToUniTask）
    /// </summary>
    private async UniTask WaitForTween(Tween tween)
    {
        if (tween == null) return;
        await UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete());
    }
}

