using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 静态配置数据（无 SO 依赖）
/// </summary>
[Serializable]
public class BuffData
{
    public int id;
    public string buffName;
    public string description;
    public string iconName;
    public int priority;
    public int maxStack = 1;
    public bool isVisible = true;
    public List<string> tags = new();

    // 时间信息
    public bool isForever;      // 是否为永久 Buff
    public int duration = 1;    // 持续回合数
    public int tickTime = 1;    // 每多少回合触发一次

    // 更新方式
    public E_BuffUpdateTime buffUpdateTime = E_BuffUpdateTime.Replace;
    public E_BuffRemoveStackUpdate buffRemoveStackUpdate = E_BuffRemoveStackUpdate.ClearStack;
    public bool triggerTickOnCreate;

    /// <summary>回调类型 -> 效果委托字典</summary>
    public Dictionary<E_BuffCallBackType, Action<BuffInfo, object[]>> Effects { get; } = new();

    /// <summary>
    /// 触发指定类型的效果
    /// </summary>
    public void TriggerEffect(E_BuffCallBackType type, BuffInfo info, params object[] args)
    {
        // 触发运行时注册的 Action
        info.TriggerBuffAction(type);

        // 触发静态定义的 Effect
        if (Effects.TryGetValue(type, out var effect))
        {
            effect?.Invoke(info, args);
        }
    }

    /// <summary>
    /// 检查是否存在指定回调类型的效果
    /// </summary>
    public bool HasEffect(E_BuffCallBackType type) => Effects.ContainsKey(type);

    /// <summary>
    /// 检查是否包含指定标签
    /// </summary>
    public bool HasTag(string tag) => tags.Contains(tag);
}
