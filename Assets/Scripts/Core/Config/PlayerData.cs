/// <summary>
/// 玩家静态配置数据（只读常量）
/// 运行时可变数据请使用 PlayerRunTimeInfo
/// 
/// 设计原则：
/// - PlayerData = 静态配置（初始值、规则参数）
/// - PlayerRunTimeInfo = 运行时数据（资产、回合、Buff等）
/// </summary>
public static class PlayerData
{
    // ========== 初始值配置 ==========

    /// <summary>
    /// 初始资产
    /// </summary>
    public const long INITIAL_ASSETS = 1000000;

    /// <summary>
    /// 初始回合数
    /// </summary>
    public const int INITIAL_ROUND = 1;

    // ========== 航线成本配置 ==========

    /// <summary>
    /// 每格航线建造基础成本
    /// </summary>
    public const int EDGE_BUILD_COST_PER_TILE = 8;

    /// <summary>
    /// 拆除航线返还比例（0.0 - 1.0）
    /// </summary>
    public const float EDGE_REMOVE_REFUND_RATE = 0.75f;

    // ========== 节点成本配置 ==========

    /// <summary>
    /// 节点升级基础成本
    /// 节点成本 = 节点等级 × NODE_UPGRADE_BASE_COST
    /// </summary>
    public const int NODE_UPGRADE_BASE_COST = 80;

    // ========== 结算规则配置 ==========

    /// <summary>
    /// 破产阈值（资产低于此值判定破产）
    /// </summary>
    public const long BANKRUPTCY_THRESHOLD = -2000;

    /// <summary>
    /// 反盲目扩张惩罚阈值（Buff #15）
    /// 本回合新增节点数 >= 此值时触发
    /// </summary>
    public const int EXPANSION_PENALTY_THRESHOLD = 4;

    /// <summary>
    /// 反盲目扩张惩罚倍率（Buff #15）
    /// </summary>
    public const float EXPANSION_PENALTY_MULTIPLIER = 1.15f;

    // ========== 结构倍率配置 ==========

    /// <summary>
    /// 环形结构基础倍率
    /// </summary>
    public const float RING_BASE_MULTIPLIER = 1.5f;

    /// <summary>
    /// 单线结构基础倍率
    /// </summary>
    public const float SINGLE_LINE_BASE_MULTIPLIER = 1.0f;

    /// <summary>
    /// 放射结构基础倍率（枢纽）
    /// </summary>
    public const float RADIAL_BASE_MULTIPLIER = 1.2f;

    // ========== 枢纽规则配置 ==========

    /// <summary>
    /// 环形结构中每 N 个节点必须包含一个枢纽
    /// </summary>
    public const int RING_HUB_REQUIRED_PER_NODES = 4;

    /// <summary>
    /// Lv4 机场最少航线数量（Buff #19）
    /// </summary>
    public const int LV4_MIN_EDGE_COUNT = 6;

    /// <summary>
    /// Lv5 机场最少航线数量（Buff #19）
    /// </summary>
    public const int LV5_MIN_EDGE_COUNT = 8;

    /// <summary>
    /// 超额设施闲置惩罚（Buff #19）
    /// </summary>
    public const float UNDERUTILIZED_PENALTY = 0.6f;

    // ========== 市场趋势 Buff 配置 ==========

    /// <summary>
    /// 每回合随机市场 Buff 数量
    /// </summary>
    public const int MARKET_BUFF_COUNT_PER_ROUND = 2;

    // ========== 商店配置 ==========

    /// <summary>
    /// 玩家 Buff 基础价格
    /// </summary>
    public const int PLAYER_BUFF_BASE_PRICE = 500;
}
