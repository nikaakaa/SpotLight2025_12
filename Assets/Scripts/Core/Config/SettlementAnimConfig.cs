using UnityEngine;

/// <summary>
/// 结算动画配置 - 集中管理所有动画参数
/// </summary>
public static class SettlementAnimConfig
{
    // ========== 时长配置 ==========

    /// <summary>地图变暗时长</summary>
    public const float DimDuration = 0.3f;

    /// <summary>节点间弹出间隔</summary>
    public const float NodePopDelay = 0.15f;

    /// <summary>Buff 跳动时长</summary>
    public const float BuffPunchDuration = 0.25f;

    /// <summary>倍率显示时长</summary>
    public const float MultiplierShowDuration = 0.4f;

    /// <summary>数字飞向总收益时长</summary>
    public const float NumberFlyDuration = 0.5f;

    /// <summary>结构间切换间隔</summary>
    public const float StructureInterval = 0.3f;

    /// <summary>成本显示时长</summary>
    public const float CostShowDuration = 0.8f;

    /// <summary>资产滚动时长</summary>
    public const float MoneyRollDuration = 1.0f;

    /// <summary>恢复正常时长</summary>
    public const float RestoreDuration = 0.3f;

    // ========== 颜色配置 ==========

    /// <summary>变暗颜色</summary>
    public static readonly Color DimColor = new(0.3f, 0.3f, 0.3f, 1f);

    /// <summary>高亮颜色</summary>
    public static readonly Color HighlightColor = Color.white;

    /// <summary>收益颜色（绿色）</summary>
    public static readonly Color IncomeColor = new(0.2f, 1f, 0.4f, 1f);

    /// <summary>成本颜色（红色）</summary>
    public static readonly Color CostColor = new(1f, 0.3f, 0.3f, 1f);

    /// <summary>Buff 跳动颜色（金色）</summary>
    public static readonly Color BuffPunchColor = new(1f, 0.9f, 0.3f, 1f);

    /// <summary>倍率颜色（橙色）</summary>
    public static readonly Color MultiplierColor = new(1f, 0.6f, 0.2f, 1f);

    /// <summary>Buff 增益颜色（青绿色，与倍率区分）</summary>
    public static readonly Color BuffPositiveColor = new(0.3f, 1f, 0.7f, 1f);

    /// <summary>Buff 减益颜色（紫红色，与成本红区分）</summary>
    public static readonly Color BuffNegativeColor = new(0.9f, 0.4f, 0.8f, 1f);

    // ========== Buff 动画配置 ==========

    /// <summary>Buff 碰撞动画时长</summary>
    public const float BuffImpactDuration = 0.25f;

    /// <summary>Buff 数字飞行时长</summary>
    public const float BuffFlyDuration = 0.3f;

    /// <summary>Buff 数字起始偏移（相对于节点）</summary>
    public static readonly Vector3 BuffStartOffset = new(-2f, 0.5f, 0);

    // ========== 缩放配置 ==========

    /// <summary>节点高亮时的缩放</summary>
    public const float NodeHighlightScale = 1.2f;

    /// <summary>Buff 跳动时的缩放</summary>
    public const float BuffPunchScale = 1.3f;

    /// <summary>成本抖动强度</summary>
    public const float CostShakeStrength = 10f;

    // ========== 浮动数字配置 ==========

    /// <summary>浮动数字偏移（节点上方）</summary>
    public static readonly Vector3 FloatingNumberOffset = new(0, 1.5f, 0);

    /// <summary>浮动数字飞行目标偏移（屏幕左上角）</summary>
    public static readonly Vector2 MoneyUIAnchor = new(0.1f, 0.9f);

    /// <summary>浮动数字基础字体大小</summary>
    public const float FloatingNumberBaseFontSize = 1f;

    /// <summary>浮动数字最大字体大小</summary>
    public const float FloatingNumberMaxFontSize = 3f;

    /// <summary>浮动数字最小字体大小</summary>
    public const float FloatingNumberMinFontSize = 1f;

    /// <summary>
    /// 根据数值计算字体大小
    /// 数值越大，字体越大（对数映射，避免大数字过大）
    /// </summary>
    /// <param name="value">数值（绝对值）</param>
    /// <returns>字体大小</returns>
    public static float GetFontSizeByValue(float value)
    {
        float absValue = Mathf.Abs(value);

        if (absValue <= 0) return FloatingNumberBaseFontSize;

        // 使用对数映射：log10(value+1) * scale + base
        // 例如：10 → 48+10=58, 100 → 48+20=68, 1000 → 48+30=78
        float logScale = Mathf.Log10(absValue + 1) * 10f;
        float targetSize = FloatingNumberBaseFontSize + logScale;

        return Mathf.Clamp(targetSize, FloatingNumberMinFontSize, FloatingNumberMaxFontSize);
    }

    /// <summary>
    /// 根据数值计算缩放倍率（用于节点数字整体缩放）
    /// </summary>
    public static float GetScaleByValue(float value)
    {
        float fontSize = GetFontSizeByValue(value);
        return fontSize / FloatingNumberBaseFontSize;
    }
}

