using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 结构类型枚举
/// </summary>
public enum E_StructureType
{
    None = 0,
    /// <summary>环形结构</summary>
    Ring = 1,
    /// <summary>单向结构（单线 A-B）</summary>
    SingleLine = 2,
    /// <summary>放射结构（枢纽辐射）</summary>
    Radial = 3,
    /// <summary>跨区结构（洲际航线）</summary>
    CrossRegion = 4,
    /// <summary>匹配所有结构类型（用于 Buff）</summary>
    All = 99,
}

/// <summary>
/// 收益/成本修正器
/// 用于 Buff 修正节点收益和成本
/// </summary>
[System.Serializable]
public class IncomeModifier
{
    /// <summary>固定加成（在倍率计算前加）</summary>
    public long FlatBonus { get; set; } = 0;

    /// <summary>乘数（对基础值 + 固定加成的乘数）</summary>
    public float Multiplier { get; set; } = 1f;

    /// <summary>
    /// 应用修正（long 版本）
    /// 公式: (baseValue + FlatBonus) * Multiplier
    /// </summary>
    public long Apply(long baseValue)
    {
        return (long)((baseValue + FlatBonus) * Multiplier);
    }

    /// <summary>
    /// 应用修正（float 版本）
    /// 公式: (baseValue + FlatBonus) * Multiplier
    /// </summary>
    public float Apply(float baseValue)
    {
        return (baseValue + FlatBonus) * Multiplier;
    }

    /// <summary>
    /// 重置修正器
    /// </summary>
    public void Reset()
    {
        FlatBonus = 0;
        Multiplier = 1f;
    }

    /// <summary>
    /// 叠加另一个修正器（用于多个 Buff 叠加）
    /// </summary>
    public void Combine(IncomeModifier other)
    {
        FlatBonus += other.FlatBonus;
        Multiplier *= other.Multiplier;
    }

    public override string ToString()
    {
        return $"[IncomeModifier] Flat:{FlatBonus}, Mult:{Multiplier:F2}";
    }
}

/// <summary>
/// 倍率修正器
/// 用于 Buff 修正结构倍率
/// </summary>
[System.Serializable]
public class MultiplierModifier
{
    /// <summary>固定加成（在倍率计算前加）</summary>
    public float FlatBonus { get; set; } = 0f;

    /// <summary>乘数（对基础值 + 固定加成的乘数）</summary>
    public float Multiplier { get; set; } = 1f;

    /// <summary>
    /// 应用修正
    /// 公式: (baseValue + FlatBonus) * Multiplier
    /// </summary>
    public float Apply(float baseValue)
    {
        return (baseValue + FlatBonus) * Multiplier;
    }

    /// <summary>
    /// 重置修正器
    /// </summary>
    public void Reset()
    {
        FlatBonus = 0f;
        Multiplier = 1f;
    }

    /// <summary>
    /// 叠加另一个修正器
    /// </summary>
    public void Combine(MultiplierModifier other)
    {
        FlatBonus += other.FlatBonus;
        Multiplier *= other.Multiplier;
    }

    public override string ToString()
    {
        return $"[MultiplierModifier] Flat:{FlatBonus:F2}, Mult:{Multiplier:F2}";
    }
}

/// <summary>
/// 市场趋势 Buff 枚举（策划案 #1-#21）
/// </summary>
public enum EMarketBuff
{
    /// <summary>#1 所有节点收益xN - 市场疲软1</summary>
    AllNodeIncomeMultiplier = 1,
    /// <summary>#2 所有结构倍率xN - 市场疲软2</summary>
    AllStructureMultiplier = 2,
    /// <summary>#3 所有"环"结构倍率xN - 环形航线资源紧张</summary>
    RingMultiplier = 3,
    /// <summary>#4 所有"单向"结构倍率xN - 直达航班市场疲软</summary>
    SingleLineMultiplier = 4,
    /// <summary>#5 所有"放射"结构收益xN - 枢纽城市运力不足</summary>
    RadialIncome = 5,
    /// <summary>#6 所有lv1节点收益xN - 小型机场客流减少</summary>
    Level1NodeIncome = 6,
    /// <summary>#7 所有lv2节点收益xN - 通用机场客流减少</summary>
    Level2NodeIncome = 7,
    /// <summary>#8 所有lv3节点收益xN - 中型机场客流减少</summary>
    Level3NodeIncome = 8,
    /// <summary>#9 所有lv4节点收益xN - 大型机场客流减少</summary>
    Level4NodeIncome = 9,
    /// <summary>#10 所有lv5节点收益xN - 巨型机场客流减少</summary>
    Level5NodeIncome = 10,
    /// <summary>#11 所有"枢纽"收益xN - 交通枢纽航空资源紧张</summary>
    HubIncome = 11,
    /// <summary>#12 跨区边线路成本x1.5,收益倍率-0.2 - 跨区空域审批受限</summary>
    CrossRegionPenalty = 12,
    /// <summary>#13 所有航线成本x1.5 - 燃油成本上涨</summary>
    AllEdgeCostMultiplier = 13,
    /// <summary>#14 只参与一条航线的节点收益-30% - 空载惩罚政策出台</summary>
    SingleEdgeNodePenalty = 14,
    /// <summary>#15 本回合新增节点数>=2 总成本+30% - 反盲目扩张政策出台</summary>
    ExpansionPenalty = 15,
    /// <summary>#16 同类结构>=3时，每多一个，此类结构收益倍率-0.1 - 鼓励政策修改</summary>
    StructureDiminishing = 16,
    /// <summary>#17 结构内最高收益的节点收益+80%，其余节点的收益-80% - 市场极化</summary>
    MarketPolarization = 17,
    /// <summary>#18 本回合禁止新建以及改变机场等级 - 机场等级审查制度</summary>
    UpgradeBlocked = 18,
    /// <summary>#19 所有lv4/lv5机场线路分别少于6/8条的节点收益-60% - 超额设施闲置</summary>
    HighLevelUnderutilized = 19,
    /// <summary>#20 lv1-2节点收益x1.5、lv4-5节点收益x0.6 - 小型机场复苏</summary>
    SmallAirportRevival = 20,
    /// <summary>#21 lv3节点收益x0.6 升级成本x0.7 - 中型机场过剩</summary>
    MediumAirportSurplus = 21,
}

/// <summary>
/// 玩家升级 Buff 枚举（策划案 #1-#12）
/// </summary>
public enum EPlayerBuff
{
    /// <summary>#1 所有lv1节点收益+10 - 小型机场设施升级</summary>
    Level1IncomeBonus = 101,
    /// <summary>#2 所有lv2节点收益+25 - 通用型机场设施升级</summary>
    Level2IncomeBonus = 102,
    /// <summary>#3 所有lv3节点收益+50 - 中型机场设施升级</summary>
    Level3IncomeBonus = 103,
    /// <summary>#4 所有lv4节点收益+100 - 大型机场设施升级</summary>
    Level4IncomeBonus = 104,
    /// <summary>#5 所有lv5节点收益+200 - 巨型机场设施升级</summary>
    Level5IncomeBonus = 105,
    /// <summary>#6 "环"结构倍率+0.2 - 环行航班密度提升提案通过</summary>
    RingMultiplierBonus = 106,
    /// <summary>#7 "单向"结构倍率+0.1 - 直达航班密度提升提案通过</summary>
    SingleLineMultiplierBonus = 107,
    /// <summary>#8 "放射"结构倍率+0.1 - 枢纽航班密度提升提案通过</summary>
    RadialMultiplierBonus = 108,
    /// <summary>#9 连接lv1与lv2的航线成本-50% - 偏远航线补贴</summary>
    RemoteRouteCostReduction = 109,
    /// <summary>#10 所有没有"枢纽"节点参与的结构，倍率+0.2 - 去中心化联盟成立</summary>
    DecentralizedBonus = 110,
    /// <summary>#11 长度<=2的线路成本-20%，参与结构倍率+0.1 - 短途航线爆发</summary>
    ShortRouteBonus = 111,
    /// <summary>#12 所有未参加结构的节点收益+100% - 孤立繁荣政策实施</summary>
    IsolatedNodeBonus = 112,
}

/// <summary>
/// Buff 显示配置
/// 用于 UI 中显示的静态文本配置
/// </summary>
public static class BuffDisplayConfig
{
    /// <summary>市场趋势 Buff 显示标题</summary>
    public const string MarketBuffTitle = "【市场趋势】";

    /// <summary>玩家永久 Buff 显示标题</summary>
    public const string PlayerBuffTitle = "【玩家Buff】";

    /// <summary>
    /// 市场趋势 Buff 显示文本（按 EMarketBuff 枚举 ID 索引）
    /// </summary>
    public static readonly Dictionary<int, string> MarketBuffTexts = new()
    {
        { 1, "市场疲软1：所有节点收益×0.8" },
        { 2, "市场疲软2：所有结构倍率×0.8" },
        { 3, "环形航线资源紧张：环结构倍率×0.7" },
        { 4, "直达航班市场疲软：单线结构倍率×0.8" },
        { 5, "枢纽城市运力不足：放射结构收益×0.7" },
        { 6, "小型机场客流减少：lv1节点收益×0.7" },
        { 7, "通用机场客流减少：lv2节点收益×0.7" },
        { 8, "中型机场客流减少：lv3节点收益×0.7" },
        { 9, "大型机场客流减少：lv4节点收益×0.7" },
        { 10, "巨型机场客流减少：lv5节点收益×0.7" },
        { 11, "交通枢纽航空资源紧张：枢纽收益×0.7" },
        { 12, "跨区空域审批受限：跨区成本×1.5，倍率-0.2" },
        { 13, "燃油成本上涨：所有航线成本×1.5" },
        { 14, "空载惩罚政策出台：单边节点收益-30%" },
        { 15, "反盲目扩张政策出台：新增≥2节点，成本+30%" },
        { 16, "鼓励政策修改：同类≥3，每多1倍率-0.1" },
        { 17, "市场极化：最高收益节点+80%，其余-80%" },
        { 18, "机场等级审查制度：禁止新建/升级机场" },
        { 19, "超额设施闲置：lv4/5航线不足，收益-60%" },
        { 20, "小型机场复苏：lv1-2×1.5，lv4-5×0.6" },
        { 21, "中型机场过剩：lv3收益×0.6，升级成本×0.7" },
    };

    /// <summary>
    /// 玩家升级 Buff 显示文本（按 EPlayerBuff 枚举 ID 索引）
    /// </summary>
    public static readonly Dictionary<int, string> PlayerBuffTexts = new()
    {
        { 101, "小型机场设施升级：lv1收益+10" },
        { 102, "通用型机场设施升级：lv2收益+25" },
        { 103, "中型机场设施升级：lv3收益+50" },
        { 104, "大型机场设施升级：lv4收益+100" },
        { 105, "巨型机场设施升级：lv5收益+200" },
        { 106, "环行航班密度提升提案通过：环倍率+0.2" },
        { 107, "直达航班密度提升提案通过：单线倍率+0.1" },
        { 108, "枢纽航班密度提升提案通过：放射倍率+0.1" },
        { 109, "偏远航线补贴：lv1-2航线成本-50%" },
        { 110, "去中心化联盟成立：非枢纽结构倍率+0.2" },
        { 111, "短途航线爆发：长度≤2成本-20%，倍率+0.1" },
        { 112, "孤立繁荣政策实施：未参与结构节点+100%" },
    };

    /// <summary>
    /// 获取 Buff 显示文本
    /// </summary>
    /// <param name="buffId">Buff ID</param>
    /// <returns>显示文本，若不存在则返回 null</returns>
    public static string GetBuffDisplayText(int buffId)
    {
        if (MarketBuffTexts.TryGetValue(buffId, out var marketText))
            return marketText;

        if (PlayerBuffTexts.TryGetValue(buffId, out var playerText))
            return playerText;

        return null;
    }

    /// <summary>
    /// 判断是否为市场趋势 Buff
    /// </summary>
    public static bool IsMarketBuff(int buffId) => buffId >= 1 && buffId <= 21;

    /// <summary>
    /// 判断是否为玩家升级 Buff
    /// </summary>
    public static bool IsPlayerBuff(int buffId) => buffId >= 101 && buffId <= 112;
}
