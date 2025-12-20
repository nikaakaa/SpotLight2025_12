using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Buff 商店单个元素组件
/// 挂载在 BuffShopElementl/m/r 上
/// </summary>
public class BuffShopElement : MonoBehaviour, IPointerClickHandler
{
    [Header("组件引用")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text descText;

    private BuffShopData currentData;

    /// <summary>点击事件</summary>
    public event Action OnClicked;

    void Awake()
    {
        // 自动查找组件（如果没有手动赋值）
        if (descText == null)
            descText = GetComponentInChildren<TMP_Text>();

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>();
    }

    /// <summary>
    /// 实现点击接口（整个元素可点击）
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[BuffShopElement] 点击: {currentData?.Name}");
        OnClicked?.Invoke();
    }

    /// <summary>
    /// 设置显示数据
    /// </summary>
    public void SetData(BuffShopData data)
    {
        currentData = data;

        Debug.Log($"[BuffShopElement] SetData: {data.Name}, Desc={data.Description}, Price={data.Price}");

        if (iconImage != null && data.Icon != null)
        {
            iconImage.sprite = data.Icon;
        }

        if (descText != null)
        {
            // 显示描述和价格
            string priceStr = data.Price > 0 ? $"\n<color=#FFD700>价格: ${data.Price}</color>" : "";
            descText.text = data.Description + priceStr;
            Debug.Log($"[BuffShopElement] 设置文字: {descText.text}");
        }
        else
        {
            Debug.LogWarning($"[BuffShopElement] descText 为空！无法显示文字");
        }
    }

    /// <summary>
    /// 获取当前数据
    /// </summary>
    public BuffShopData GetData() => currentData;
}


/// <summary>
/// Buff 商店数据
/// </summary>
[Serializable]
public class BuffShopData
{
    /// <summary>Buff ID（EPlayerBuff 枚举值）</summary>
    public int BuffId;

    /// <summary>Buff 名称</summary>
    public string Name;

    /// <summary>Buff 描述</summary>
    public string Description;

    /// <summary>图标</summary>
    public Sprite Icon;

    /// <summary>价格</summary>
    public int Price;

    /// <summary>
    /// 从 EPlayerBuff 创建（自动读取配置的描述和价格）
    /// </summary>
    public static BuffShopData FromPlayerBuff(EPlayerBuff buff, Sprite icon = null)
    {
        int buffId = (int)buff;
        string desc = BuffDisplayConfig.GetBuffDisplayText(buffId) ?? buff.ToString();
        int price = BuffDisplayConfig.GetBuffPrice(buffId);

        return new BuffShopData
        {
            BuffId = buffId,
            Name = buff.ToString(),
            Description = desc,
            Icon = icon,
            Price = price
        };
    }
}
