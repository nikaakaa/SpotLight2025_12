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
    public const long INITIAL_ASSETS = 10000;

    /// <summary>
    /// 初始回合数
    /// </summary>
    public const int INITIAL_ROUND = 1;

    // ========== 航线成本配置 ==========
    // 公式：成本 = BASE × log(节点数 + OFFSET)^POWER × DISTANCE_SCALE / sqrt(距离)

    /// <summary>
    /// 航线成本基础常数 - 整体成本缩放
    /// 增大 = 所有航线更贵，减小 = 所有航线更便宜
    /// </summary>
    public const float EDGE_COST_BASE = 60f;

    /// <summary>
    /// 节点数偏移量 - 控制前期曲线
    /// 增大 = 前期更轻松（log曲线起点更平缓）
    /// 减小 = 前期更困难（快速进入陡峭区间）
    /// </summary>
    public const float EDGE_COST_NODE_OFFSET = 3f;

    /// <summary>
    /// 对数指数 - 控制增长速度
    /// 1.0 = 标准对数增长
    /// &gt;1.0 = 后期成本增长更快
    /// &lt;1.0 = 后期成本增长更慢
    /// </summary>
    public const float EDGE_COST_LOG_POWER = 2.0f;

    /// <summary>
    /// 距离影响系数 - 控制距离对成本的影响程度
    /// 0.5 = 标准（sqrt）
    /// &lt;0.5 = 长距离成本降低更少（长航线更贵）
    /// &gt;0.5 = 长距离成本降低更多（长航线更便宜）
    /// </summary>
    public const float EDGE_COST_DISTANCE_POWER = 0.5f;

    /// <summary>
    /// 最小成本下限 - 无论公式如何，成本不低于此值
    /// </summary>
    public const float EDGE_COST_MIN = 5f;

    /// <summary>
    /// 最大成本上限 - 无论公式如何，成本不高于此值
    /// </summary>
    public const float EDGE_COST_MAX = 500f;

    /// <summary>
    /// 航线成本常数（旧公式，已废弃）
    /// </summary>
    [System.Obsolete("使用新的 EDGE_COST_BASE 和对数公式")]
    public const float EDGE_COST_CONSTANT = 120f;

    /// <summary>
    /// 每格航线建造基础成本（旧公式，已废弃）
    /// </summary>
    [System.Obsolete("使用 EDGE_COST_BASE 和新公式")]
    public const int EDGE_BUILD_COST_PER_TILE = 30;

    /// <summary>
    /// 拆除航线返还比例（0.0 - 1.0）
    /// </summary>
    public const float EDGE_REMOVE_REFUND_RATE = 0.55f;

    // ========== 节点成本配置 ==========

    /// <summary>
    /// 节点升级基础成本
    /// 节点成本 = 节点等级 × NODE_UPGRADE_BASE_COST
    /// </summary>
    public const int NODE_UPGRADE_BASE_COST = 200;

    // ========== 结算规则配置 ==========

    /// <summary>
    /// 破产阈值（资产低于此值判定破产）
    /// </summary>
    public const long BANKRUPTCY_THRESHOLD = 0;

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
    public const float RING_BASE_MULTIPLIER = 1.8f;

    /// <summary>
    /// 单线结构基础倍率
    /// </summary>
    public const float SINGLE_LINE_BASE_MULTIPLIER = 1.05f;

    /// <summary>
    /// 放射结构基础倍率（枢纽）
    /// </summary>
    public const float RADIAL_BASE_MULTIPLIER = 1.2f;

    // ========== 枢纽规则配置 ==========

    /// <summary>
    /// 环形结构中每 N 个节点必须包含一个枢纽
    /// </summary>
    public const int RING_HUB_REQUIRED_PER_NODES = 3;

    /// <summary>
    /// Lv4 机场最少航线数量（Buff #19）
    /// </summary>
    public const int LV4_MIN_EDGE_COUNT = 3;

    /// <summary>
    /// Lv5 机场最少航线数量（Buff #19）
    /// </summary>
    public const int LV5_MIN_EDGE_COUNT = 5;

    /// <summary>
    /// 超额设施闲置惩罚（Buff #19）
    /// </summary>
    public const float UNDERUTILIZED_PENALTY = 0.8f;

    // ========== 市场趋势 Buff 配置 ==========

    /// <summary>
    /// 每回合随机市场 Buff 数量
    /// </summary>
    public const int MARKET_BUFF_COUNT_PER_ROUND = 1;

    // ========== 商店配置 ==========

    /// <summary>
    /// 玩家 Buff 基础价格
    /// </summary>
    public const int PLAYER_BUFF_BASE_PRICE = 5000;
}
