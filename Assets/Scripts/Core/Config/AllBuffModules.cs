using UnityEngine;

// ============================================================================
// 市场趋势 Buff 模块 (ID: 1-21)
// ============================================================================

#region Buff 1-5: 收益/倍率乘算

/// <summary>Buff #1: 市场疲软1 - 所有节点收益×0.8</summary>
[BuffModule(1, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Market_AllNodeIncome_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        modifier.Multiplier *= Mathf.Pow(0.9f, stack);
    }
}

/// <summary>Buff #2: 市场疲软2 - 所有结构倍率×0.8</summary>
[BuffModule(2, E_BuffCallBackType.OnCalculateStructureMultiplier)]
public static class Market_AllStructureMultiplier_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        var modifier = args[1] as MultiplierModifier;
        int stack = ((IBuffTicker)info).CurStack;
        modifier.Multiplier *= Mathf.Pow(0.9f, stack);
    }
}

/// <summary>Buff #3: 环形航线紧张 - 环结构倍率×0.7</summary>
[BuffModule(3, E_BuffCallBackType.OnCalculateStructureMultiplier)]
public static class Market_RingMultiplier_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        var structType = (E_StructureType)args[0];
        var modifier = args[1] as MultiplierModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (structType == E_StructureType.Ring)
            modifier.Multiplier *= Mathf.Pow(0.85f, stack);
    }
}

/// <summary>Buff #4: 直达航班疲软 - 单线结构倍率×0.8</summary>
[BuffModule(4, E_BuffCallBackType.OnCalculateStructureMultiplier)]
public static class Market_SingleLineMultiplier_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        var structType = (E_StructureType)args[0];
        var modifier = args[1] as MultiplierModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (structType == E_StructureType.SingleLine)
            modifier.Multiplier *= Mathf.Pow(0.9f, stack);
    }
}

/// <summary>Buff #5: 枢纽运力不足 - 放射结构收益×0.7</summary>
[BuffModule(5, E_BuffCallBackType.OnCalculateStructureMultiplier)]
public static class Market_RadialIncome_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        var structType = (E_StructureType)args[0];
        var modifier = args[1] as MultiplierModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (structType == E_StructureType.Radial)
            modifier.Multiplier *= Mathf.Pow(0.85f, stack);
    }
}

#endregion

#region Buff 6-11: 节点等级收益

/// <summary>Buff #6-10: 特定等级节点收益×0.7</summary>
[BuffModule(6, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Market_Level1Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 1) modifier.Multiplier *= Mathf.Pow(0.85f, stack);
    }
}

[BuffModule(7, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Market_Level2Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 2) modifier.Multiplier *= Mathf.Pow(0.85f, stack);
    }
}

[BuffModule(8, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Market_Level3Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 3) modifier.Multiplier *= Mathf.Pow(0.85f, stack);
    }
}

[BuffModule(9, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Market_Level4Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 4) modifier.Multiplier *= Mathf.Pow(0.85f, stack);
    }
}

[BuffModule(10, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Market_Level5Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 5) modifier.Multiplier *= Mathf.Pow(0.85f, stack);
    }
}

/// <summary>Buff #11: 枢纽资源紧张 - 枢纽节点收益×0.7</summary>
[BuffModule(11, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Market_HubIncome_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        // 枢纽 = lv4 或 lv5
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel >= 4) modifier.Multiplier *= Mathf.Pow(0.85f, stack);
    }
}

#endregion

#region Buff 13: 航线成本

/// <summary>Buff #13: 燃油上涨 - 所有航线成本×1.5</summary>
[BuffModule(13, E_BuffCallBackType.OnCalculateEdgeCost)]
public static class Market_AllEdgeCost_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        modifier.Multiplier *= Mathf.Pow(1.25f, stack);
    }
}

#endregion

#region Buff 20-21: 复合效果

/// <summary>Buff #20: 小型复苏 - lv1-2×1.5，lv4-5×0.6</summary>
[BuffModule(20, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Market_SmallAirportRevival_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel <= 2) modifier.Multiplier *= Mathf.Pow(1.3f, stack);
        else if (nodeLevel >= 4) modifier.Multiplier *= Mathf.Pow(0.75f, stack);
    }
}

/// <summary>Buff #21: 中型过剩 - lv3收益×0.6</summary>
[BuffModule(21, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Market_MediumAirportSurplus_Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 3) modifier.Multiplier *= Mathf.Pow(0.8f, stack);
    }
}

#endregion

// ============================================================================
// 玩家升级 Buff 模块 (ID: 101-112)
// ============================================================================

#region Buff 101-105: 节点等级收益加成

/// <summary>Buff #101: lv1收益+10</summary>
[BuffModule(101, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Player_Level1Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 1) modifier.FlatBonus += 3 * stack;
    }
}

[BuffModule(102, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Player_Level2Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 2) modifier.FlatBonus += 8 * stack;
    }
}

[BuffModule(103, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Player_Level3Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 3) modifier.FlatBonus += 15 * stack;
    }
}

[BuffModule(104, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Player_Level4Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 4) modifier.FlatBonus += 25 * stack;
    }
}

[BuffModule(105, E_BuffCallBackType.OnCalculateNodeBaseIncome)]
public static class Player_Level5Income_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int nodeLevel = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (nodeLevel == 5) modifier.FlatBonus += 35 * stack;
    }
}

#endregion

#region Buff 106-108: 结构倍率加成

/// <summary>Buff #106: 环倍率+0.2</summary>
[BuffModule(106, E_BuffCallBackType.OnCalculateStructureMultiplier)]
public static class Player_RingMultiplier_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        var structType = (E_StructureType)args[0];
        var modifier = args[1] as MultiplierModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (structType == E_StructureType.Ring)
            modifier.FlatBonus += 0.2f * stack;
    }
}

/// <summary>Buff #107: 单线倍率+0.1</summary>
[BuffModule(107, E_BuffCallBackType.OnCalculateStructureMultiplier)]
public static class Player_SingleLineMultiplier_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        var structType = (E_StructureType)args[0];
        var modifier = args[1] as MultiplierModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (structType == E_StructureType.SingleLine)
            modifier.FlatBonus += 0.1f * stack;
    }
}

/// <summary>Buff #108: 放射倍率+0.1</summary>
[BuffModule(108, E_BuffCallBackType.OnCalculateStructureMultiplier)]
public static class Player_RadialMultiplier_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        var structType = (E_StructureType)args[0];
        var modifier = args[1] as MultiplierModifier;
        int stack = ((IBuffTicker)info).CurStack;
        if (structType == E_StructureType.Radial)
            modifier.FlatBonus += 0.1f * stack;
    }
}

#endregion

// ============================================================================
// Player Buff 109-112: 特殊效果
// ============================================================================

#region Buff 111: 短途航线爆发

/// <summary>Buff #111: 短途航线爆发 - 长度<10成本-20%</summary>
[BuffModule(111, E_BuffCallBackType.OnCalculateEdgeCost)]
public static class Player_ShortRouteCost_Module
{
    public const int SHORT_ROUTE_THRESHOLD = 10; // 短途阈值：距离<10

    public static void Apply(BuffInfo info, object[] args)
    {
        if (args?.Length < 2) return;
        int pathLength = (int)args[0];
        var modifier = args[1] as IncomeModifier;
        int stack = ((IBuffTicker)info).CurStack;

        if (pathLength < SHORT_ROUTE_THRESHOLD)
        {
            modifier.Multiplier *= Mathf.Pow(0.8f, stack); // 成本-20%
        }
    }
}

/// <summary>Buff #111: 短途航线爆发 - 参与结构倍率+0.1</summary>
[BuffModule(111, E_BuffCallBackType.OnCalculateStructureMultiplier)]
public static class Player_ShortRouteMultiplier_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        // 结构倍率固定+0.1，不判断航线长度（结构内默认有短途航线时生效）
        if (args?.Length < 2) return;
        var modifier = args[1] as MultiplierModifier;
        int stack = ((IBuffTicker)info).CurStack;
        modifier.FlatBonus += 0.1f * stack;
    }
}

#endregion

// ============================================================================
// 测试 Buff 模块
// ============================================================================

/// <summary>测试 Buff：设置金币为10000</summary>
[BuffModule(9999, E_BuffCallBackType.Create)]
public static class TestBuff_SetMoney_Module
{
    public static void Apply(BuffInfo info, object[] args)
    {
        if (PlayerRunTimeInfo.Current != null)
        {
            long old = PlayerRunTimeInfo.Current.Assets;
            PlayerRunTimeInfo.Current.Assets = 10000;
            Debug.Log($"<color=green>[Test] 金币: {old} -> 10000</color>");
        }
    }
}

