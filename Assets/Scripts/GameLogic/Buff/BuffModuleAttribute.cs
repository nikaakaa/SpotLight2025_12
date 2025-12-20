using System;

/// <summary>
/// Buff 模块特性
/// 用于标记静态类为某个 Buff 的效果模块，初始化时通过反射扫描注册
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class BuffModuleAttribute : Attribute
{
    /// <summary>所属 Buff 的 ID</summary>
    public int BuffId { get; }

    /// <summary>该模块响应的回调类型</summary>
    public E_BuffCallBackType CallbackType { get; }

    /// <summary>模块优先级（数值越大越先执行）</summary>
    public int Priority { get; }

    public BuffModuleAttribute(int buffId, E_BuffCallBackType callbackType, int priority = 0)
    {
        BuffId = buffId;
        CallbackType = callbackType;
        Priority = priority;
    }
}

/// <summary>
/// Buff 定义特性
/// 用于标记 Buff 配置类（可选，也可以直接在 BuffRegistry 中定义）
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class BuffDefinitionAttribute : Attribute
{
    public int BuffId { get; }
    public string BuffName { get; }

    public BuffDefinitionAttribute(int buffId, string buffName)
    {
        BuffId = buffId;
        BuffName = buffName;
    }
}
