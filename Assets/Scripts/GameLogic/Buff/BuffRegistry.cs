using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Buff 注册表
/// 管理所有 Buff 配置，通过反射扫描 [BuffModule] 特性自动注册效果
/// </summary>
public static class BuffRegistry
{
    private static readonly Dictionary<int, BuffData> _buffsById = new();
    private static readonly Dictionary<string, BuffData> _buffsByName = new();
    private static bool _initialized = false;

    /// <summary>根据 ID 获取 Buff</summary>
    public static BuffData GetById(int id) => _buffsById.TryGetValue(id, out var buff) ? buff : null;

    /// <summary>根据名称获取 Buff</summary>
    public static BuffData GetByName(string name) => _buffsByName.TryGetValue(name, out var buff) ? buff : null;

    /// <summary>获取所有已注册的 Buff</summary>
    public static IEnumerable<BuffData> GetAllBuffs() => _buffsById.Values;

    /// <summary>
    /// 初始化注册表（在游戏启动时调用一次）
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        Debug.Log("[BuffRegistry] 开始初始化...");

        // Step 1: 注册所有 Buff 配置
        RegisterAllBuffConfigs();

        // Step 2: 扫描并注册所有模块
        ScanAndRegisterModules();

        Debug.Log($"[BuffRegistry] 初始化完成: {_buffsById.Count} 个 Buff");
    }

    /// <summary>
    /// 注册 Buff 配置
    /// </summary>
    public static void Register(BuffData data)
    {
        _buffsById[data.id] = data;
        if (!string.IsNullOrEmpty(data.buffName))
        {
            _buffsByName[data.buffName] = data;
        }
    }

    /// <summary>
    /// 注册所有 Buff 配置
    /// </summary>
    private static void RegisterAllBuffConfigs()
    {
        // ========== 测试 Buff ==========
        Register(new BuffData { id = 9999, buffName = "TestBuff_SetMoney", description = "测试：设置金币为10000", isForever = true });

        // ========== 市场趋势 Buff (ID: 1-21) ==========
        Register(new BuffData { id = 1, buffName = "Market_AllNodeIncome", description = "市场疲软1：所有节点收益×0.8", duration = 1 });
        Register(new BuffData { id = 2, buffName = "Market_AllStructureMultiplier", description = "市场疲软2：所有结构倍率×0.8", duration = 1 });
        Register(new BuffData { id = 3, buffName = "Market_RingMultiplier", description = "环形航线紧张：环结构倍率×0.7", duration = 1 });
        Register(new BuffData { id = 4, buffName = "Market_SingleLineMultiplier", description = "直达航班疲软：单线结构倍率×0.8", duration = 1 });
        Register(new BuffData { id = 5, buffName = "Market_RadialIncome", description = "枢纽运力不足：放射结构收益×0.7", duration = 1 });
        Register(new BuffData { id = 6, buffName = "Market_Level1Income", description = "小型机场客流减少：lv1节点收益×0.7", duration = 1 });
        Register(new BuffData { id = 7, buffName = "Market_Level2Income", description = "通用机场客流减少：lv2节点收益×0.7", duration = 1 });
        Register(new BuffData { id = 8, buffName = "Market_Level3Income", description = "中型机场客流减少：lv3节点收益×0.7", duration = 1 });
        Register(new BuffData { id = 9, buffName = "Market_Level4Income", description = "大型机场客流减少：lv4节点收益×0.7", duration = 1 });
        Register(new BuffData { id = 10, buffName = "Market_Level5Income", description = "巨型机场客流减少：lv5节点收益×0.7", duration = 1 });
        Register(new BuffData { id = 11, buffName = "Market_HubIncome", description = "枢纽资源紧张：枢纽节点收益×0.7", duration = 1 });
        Register(new BuffData { id = 12, buffName = "Market_CrossRegionPenalty", description = "跨区审批受限：跨区成本×1.5，倍率-0.2", duration = 1 });
        Register(new BuffData { id = 13, buffName = "Market_AllEdgeCost", description = "燃油上涨：所有航线成本×1.5", duration = 1 });
        Register(new BuffData { id = 14, buffName = "Market_SingleEdgePenalty", description = "空载惩罚：单边节点收益-30%", duration = 1 });
        Register(new BuffData { id = 15, buffName = "Market_ExpansionPenalty", description = "反扩张：新增≥2节点，成本+30%", duration = 1 });
        Register(new BuffData { id = 16, buffName = "Market_StructureDiminishing", description = "鼓励修改：同类≥3，每多1倍率-0.1", duration = 1 });
        Register(new BuffData { id = 17, buffName = "Market_Polarization", description = "市场极化：最高+80%，其余-80%", duration = 1 });
        Register(new BuffData { id = 18, buffName = "Market_UpgradeBlocked", description = "审查制度：禁止新建/升级机场", duration = 1 });
        Register(new BuffData { id = 19, buffName = "Market_HighLevelUnderutilized", description = "设施闲置：lv4/5航线不足，收益-60%", duration = 1 });
        Register(new BuffData { id = 20, buffName = "Market_SmallAirportRevival", description = "小型复苏：lv1-2×1.5，lv4-5×0.6", duration = 1 });
        Register(new BuffData { id = 21, buffName = "Market_MediumAirportSurplus", description = "中型过剩：lv3收益×0.6，升级×0.7", duration = 1 });

        // ========== 玩家升级 Buff (ID: 101-112) ==========
        Register(new BuffData { id = 101, buffName = "Player_Level1Income", description = "小型机场升级：lv1收益+10", isForever = true });
        Register(new BuffData { id = 102, buffName = "Player_Level2Income", description = "通用机场升级：lv2收益+25", isForever = true });
        Register(new BuffData { id = 103, buffName = "Player_Level3Income", description = "中型机场升级：lv3收益+50", isForever = true });
        Register(new BuffData { id = 104, buffName = "Player_Level4Income", description = "大型机场升级：lv4收益+100", isForever = true });
        Register(new BuffData { id = 105, buffName = "Player_Level5Income", description = "巨型机场升级：lv5收益+200", isForever = true });
        Register(new BuffData { id = 106, buffName = "Player_RingMultiplier", description = "环形密度提升：环倍率+0.2", isForever = true });
        Register(new BuffData { id = 107, buffName = "Player_SingleLineMultiplier", description = "直达密度提升：单线倍率+0.1", isForever = true });
        Register(new BuffData { id = 108, buffName = "Player_RadialMultiplier", description = "枢纽密度提升：放射倍率+0.1", isForever = true });
        Register(new BuffData { id = 109, buffName = "Player_RemoteRouteCost", description = "偏远补贴：lv1-2航线成本-50%", isForever = true });
        Register(new BuffData { id = 110, buffName = "Player_DecentralizedBonus", description = "去中心化：非枢纽结构倍率+0.2", isForever = true });
        Register(new BuffData { id = 111, buffName = "Player_ShortRouteBonus", description = "短途爆发：长度≤2成本-20%，倍率+0.1", isForever = true });
        Register(new BuffData { id = 112, buffName = "Player_IsolatedNodeBonus", description = "孤立繁荣：未参与结构节点+100%", isForever = true });
    }

    /// <summary>
    /// 扫描所有程序集，查找带有 [BuffModule] 特性的静态类并注册
    /// </summary>
    private static void ScanAndRegisterModules()
    {
        var moduleTypes = new List<(Type type, BuffModuleAttribute attr)>();

        // 扫描当前程序集中的所有类型
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // 跳过系统程序集
            if (assembly.FullName.StartsWith("System") ||
                assembly.FullName.StartsWith("Unity") ||
                assembly.FullName.StartsWith("mscorlib"))
                continue;

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttribute<BuffModuleAttribute>();
                    if (attr != null && type.IsClass && type.IsAbstract && type.IsSealed) // 静态类
                    {
                        moduleTypes.Add((type, attr));
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 忽略无法加载的程序集
            }
        }

        // 按优先级排序
        moduleTypes = moduleTypes.OrderByDescending(x => x.attr.Priority).ToList();

        // 注册模块到对应的 Buff
        foreach (var (type, attr) in moduleTypes)
        {
            if (!_buffsById.TryGetValue(attr.BuffId, out var buffData))
            {
                Debug.LogWarning($"[BuffRegistry] 模块 {type.Name} 引用了不存在的 Buff ID: {attr.BuffId}");
                continue;
            }

            // 查找 Apply 方法
            var applyMethod = type.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static);
            if (applyMethod == null)
            {
                Debug.LogWarning($"[BuffRegistry] 模块 {type.Name} 缺少 public static Apply 方法");
                continue;
            }

            // 创建委托并注册
            try
            {
                var action = (Action<BuffInfo, object[]>)Delegate.CreateDelegate(
                    typeof(Action<BuffInfo, object[]>), applyMethod);

                // 如果已有同类型回调，组合委托
                if (buffData.Effects.TryGetValue(attr.CallbackType, out var existing))
                {
                    buffData.Effects[attr.CallbackType] = (info, args) =>
                    {
                        existing(info, args);
                        action(info, args);
                    };
                }
                else
                {
                    buffData.Effects[attr.CallbackType] = action;
                }

                Debug.Log($"[BuffRegistry] 注册模块: {type.Name} -> Buff[{attr.BuffId}].{attr.CallbackType}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BuffRegistry] 注册模块 {type.Name} 失败: {e.Message}");
            }
        }
    }
}
