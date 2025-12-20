/// <summary>
/// 渲染层级常量（SortingOrder 值）
/// 数值越大越靠前（显示在上层）
/// </summary>
public static class RenderLayers
{
    /// <summary>
    /// 地形 - 最下层
    /// </summary>
    public const int TERRAIN = 0;

    /// <summary>
    /// 航线 - 中间层
    /// </summary>
    public const int AIRLINE = 50;

    /// <summary>
    /// 城市节点 - 最上层
    /// </summary>
    public const int CITY = 100;

    /// <summary>
    /// UI 元素
    /// </summary>
    public const int UI = 1000;
}
