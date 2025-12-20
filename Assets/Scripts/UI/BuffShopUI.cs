using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Buff 商店 UI 面板
/// 用于 PurchaseBuffState 阶段显示和购买 Buff
/// </summary>
public class BuffShopUI : MonoBehaviour
{
    public static BuffShopUI Instance { get; private set; }

    [Header("面板")]
    [SerializeField] private GameObject panel;

    [Header("返回按钮")]
    [SerializeField] private Button skipButton;

    [Header("Buff 元素 (左中右)")]
    [SerializeField] private BuffShopElement elementLeft;
    [SerializeField] private BuffShopElement elementMiddle;
    [SerializeField] private BuffShopElement elementRight;

    /// <summary>当前显示的 Buff 选项</summary>
    private List<BuffShopData> currentBuffs = new();

    /// <summary>选择 Buff 后的回调</summary>
    public event Action<int> OnBuffSelected;

    /// <summary>点击返回/跳过的回调</summary>
    public event Action OnSkipped;

    void Awake()
    {
        Instance = this;

        // 绑定返回按钮
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipBtnClicked);
        }

        // 绑定元素点击事件
        if (elementLeft != null) elementLeft.OnClicked += () => OnElementClicked(0);
        if (elementMiddle != null) elementMiddle.OnClicked += () => OnElementClicked(1);
        if (elementRight != null) elementRight.OnClicked += () => OnElementClicked(2);

        // 默认隐藏
        Hide();
    }

    /// <summary>
    /// 显示 Buff 商店面板
    /// </summary>
    /// <param name="buffOptions">可选的 Buff 列表（最多3个）</param>
    public void Show(List<BuffShopData> buffOptions)
    {
        currentBuffs = buffOptions ?? new List<BuffShopData>();

        // 更新 UI 元素
        UpdateElement(elementLeft, currentBuffs.Count > 0 ? currentBuffs[0] : null);
        UpdateElement(elementMiddle, currentBuffs.Count > 1 ? currentBuffs[1] : null);
        UpdateElement(elementRight, currentBuffs.Count > 2 ? currentBuffs[2] : null);

        if (panel != null)
            panel.SetActive(true);
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void UpdateElement(BuffShopElement element, BuffShopData data)
    {
        if (element == null) return;

        if (data != null)
        {
            element.SetData(data);
            element.gameObject.SetActive(true);
        }
        else
        {
            element.gameObject.SetActive(false);
        }
    }

    private void OnSkipBtnClicked()
    {
        Debug.Log("[BuffShopUI] 跳过购买");
        Hide();
        OnSkipped?.Invoke();
    }

    private void OnElementClicked(int index)
    {
        if (index >= 0 && index < currentBuffs.Count)
        {
            var selectedBuff = currentBuffs[index];
            Debug.Log($"[BuffShopUI] 选择 Buff: {selectedBuff.BuffId} - {selectedBuff.Name}");
            Hide();
            OnBuffSelected?.Invoke(selectedBuff.BuffId);
        }
    }
}

