using System.Collections.Generic;

/// <summary>
/// 结构基类 - 所有航线结构的抽象基类
/// 结构由节点和边组成，用于结算时计算倍率
/// </summary>
public abstract class StructureBase
{
    /// <summary>
    /// 结构类型
    /// </summary>
    public abstract E_StructureType Type { get; }

    /// <summary>
    /// 参与此结构的节点列表
    /// </summary>
    public List<AviationNode> Nodes { get; } = new List<AviationNode>();

    /// <summary>
    /// 参与此结构的边列表
    /// </summary>
    public List<AviationEdge> Edges { get; } = new List<AviationEdge>();

    /// <summary>
    /// 基础倍率（未应用 Buff 前）
    /// </summary>
    public abstract float BaseMultiplier { get; }

    /// <summary>
    /// 最终倍率（应用 Buff 后，由结算时计算）
    /// </summary>
    public float FinalMultiplier { get; set; }

    /// <summary>
    /// 此结构的总收益（结算时计算）
    /// </summary>
    public float TotalIncome { get; set; }

    /// <summary>
    /// 是否包含枢纽节点（用于某些 Buff 判定）
    /// </summary>
    public bool ContainsHub { get; set; }

    /// <summary>
    /// 结构唯一标识（用于UI高亮等）
    /// </summary>
    public int StructureId { get; set; }
}

/// <summary>
/// 环形结构 - 闭合回路
/// 规则：每4个节点必须包含1个枢纽，否则结算时有惩罚
/// </summary>
public class RingStructure : StructureBase
{
    public override E_StructureType Type => E_StructureType.Ring;
    public override float BaseMultiplier => PlayerData.RING_BASE_MULTIPLIER;

    /// <summary>
    /// 是否满足枢纽要求（每4节点1枢纽）
    /// </summary>
    public bool MeetsHubRequirement { get; set; } = true;

    /// <summary>
    /// 枢纽不足惩罚倍率（默认1.0无惩罚）
    /// </summary>
    public float HubPenaltyMultiplier { get; set; } = 1.0f;
}

/// <summary>
/// 单线结构 - A-B 直连
/// 两个节点通过一条边直接连接，无中间节点
/// </summary>
public class SingleLineStructure : StructureBase
{
    public override E_StructureType Type => E_StructureType.SingleLine;
    public override float BaseMultiplier => PlayerData.SINGLE_LINE_BASE_MULTIPLIER;
}

/// <summary>
/// 放射结构 - 枢纽辐射
/// 以一个高度数节点（枢纽）为中心，连接多条航线
/// </summary>
public class RadialStructure : StructureBase
{
    public override E_StructureType Type => E_StructureType.Radial;
    public override float BaseMultiplier => PlayerData.RADIAL_BASE_MULTIPLIER;

    /// <summary>
    /// 枢纽节点（中心节点）
    /// </summary>
    public AviationNode HubNode { get; set; }

    /// <summary>
    /// 辐射出去的边数量
    /// </summary>
    public int RadialCount => HubNode?.edges.Count ?? 0;
}

/// <summary>
/// 所有结构的容器
/// 由 StructureDetector 一次性检测生成
/// </summary>
public class AllStructures
{
    public List<RingStructure> Rings { get; } = new List<RingStructure>();
    public List<SingleLineStructure> SingleLines { get; } = new List<SingleLineStructure>();
    public List<RadialStructure> Radials { get; } = new List<RadialStructure>();

    /// <summary>
    /// 所有结构的总数
    /// </summary>
    public int TotalCount => Rings.Count + SingleLines.Count + Radials.Count;

    /// <summary>
    /// 获取所有结构的迭代器
    /// </summary>
    public IEnumerable<StructureBase> GetAllStructures()
    {
        foreach (var ring in Rings) yield return ring;
        foreach (var line in SingleLines) yield return line;
        foreach (var radial in Radials) yield return radial;
    }

    /// <summary>
    /// 清空所有结构
    /// </summary>
    public void Clear()
    {
        Rings.Clear();
        SingleLines.Clear();
        Radials.Clear();
    }
}
