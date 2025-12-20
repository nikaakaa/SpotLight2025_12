using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 纯 C# Buff 系统
/// 管理 Buff 的添加、移除、触发
/// 替代原有的 MonoBehaviour BuffHandler
/// </summary>
public class BuffSystem
{
    private readonly List<BuffInfo> buffInfoList = new();
    private readonly Queue<BuffInfo> buffToAddQueue = new();
    private readonly Queue<BuffInfo> buffToRemoveQueue = new();

    // Buff 持有者（逻辑上的，不一定是 GameObject，可能是 null）
    private readonly GameObject owner;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="owner">Buff 的持有者对象（可选，用于传递给 BuffModule）</param>
    public BuffSystem(GameObject owner = null)
    {
        this.owner = owner;
        // 确保 BuffRegistry 已初始化
        BuffRegistry.Initialize();
    }

    /// <summary>
    /// 更新逻辑（由 GameLogicState 驱动）
    /// </summary>
    /// <param name="deltaTime">时间增量（虽然目前 Buff 系统基于回合或 Tick，但预留时间接口）</param>
    public void Update(float deltaTime)
    {
        // 处理待添加的 Buff
        while (buffToAddQueue.Count > 0)
        {
            BuffInfo newBuff = buffToAddQueue.Dequeue();
            AddBuffSorted(newBuff);
        }

        // 处理待移除的 Buff
        while (buffToRemoveQueue.Count > 0)
        {
            BuffInfo buffInfo = buffToRemoveQueue.Dequeue();
            if (buffInfoList.Remove(buffInfo))
            {
                OnBuffRemoved?.Invoke(buffInfo);
            }
        }

        // 触发 Tick 回调
        // 目前设计中 Tick 可能不是每帧触发，而是由特定时机触发
        // 但如果 Buff 有 "每秒触发" 的需求，可以在这里实现
    }

    /// <summary>Buff 列表（只读）</summary>
    public IReadOnlyList<BuffInfo> BuffInfoList => buffInfoList.ToList();

    /// <summary>清空所有 Buff</summary>
    public void ClearBuff()
    {
        buffInfoList.Clear();
        buffToAddQueue.Clear();
        buffToRemoveQueue.Clear();
    }

    /// <summary>检查 Buff 是否存在</summary>
    public bool BuffExist(int id) => buffInfoList.Any(b => b.buffData.id == id);

    /// <summary>
    /// 触发自定义回调（供结算等系统调用）
    /// </summary>
    public void TriggerCustom(E_BuffCallBackType callbackType, params object[] args)
    {
        // 尝试获取 Modifier (用于追踪哪个 Buff 生效了)
        var incomeMod = args.FirstOrDefault(a => a is IncomeModifier) as IncomeModifier;
        var multMod = args.FirstOrDefault(a => a is MultiplierModifier) as MultiplierModifier;

        foreach (var buffInfo in BuffInfoList) // 使用副本防止遍历时修改
        {
            // 记录调用前的状态
            long preFlat = incomeMod?.FlatBonus ?? 0;
            float preMult = incomeMod?.Multiplier ?? multMod?.Multiplier ?? 1f;
            float preMultFlat = multMod?.FlatBonus ?? 0f;

            buffInfo.buffData.TriggerEffect(callbackType, buffInfo, args);

            // 检查状态变化并记录 ID
            if (incomeMod != null)
            {
                if (incomeMod.FlatBonus != preFlat || Mathf.Abs(incomeMod.Multiplier - preMult) > 0.0001f)
                {
                    incomeMod.AppliedBuffIds.Add(buffInfo.buffData.id);
                }
            }
            if (multMod != null)
            {
                // MultiplierModifier 同时有 FlatBonus 和 Multiplier
                if (Mathf.Abs(multMod.FlatBonus - preMultFlat) > 0.0001f || Mathf.Abs(multMod.Multiplier - preMult) > 0.0001f)
                {
                    multMod.AppliedBuffIds.Add(buffInfo.buffData.id);
                }
            }
        }
    }

    /// <summary>查找指定 ID 的 Buff</summary>
    public BuffInfo FindBuff(int buffId)
    {
        return buffInfoList.FirstOrDefault(b => b.buffData.id == buffId);
    }

    /// <summary>
    /// 通过名称添加 Buff（从注册表查询）
    /// </summary>
    public bool AddBuff(string buffName, GameObject creator = null, int addStackCount = 1, int addTime = 0)
    {
        BuffData buffData = BuffRegistry.GetByName(buffName);
        if (buffData == null)
        {
            Debug.LogWarning($"[BuffSystem] 找不到 Buff: {buffName}");
            return false;
        }

        // 如果没有传入 creator，默认使用 owner
        if (creator == null) creator = owner;

        // target 默认为 owner
        GameObject target = owner;

        AddBuff(new BuffInfo
        {
            buffData = buffData,
            creator = creator,
            target = target,
        }, addStackCount, addTime);

        return true;
    }

    /// <summary>
    /// 通过 ID 添加 Buff
    /// </summary>
    public bool AddBuffById(int buffId, GameObject creator = null, int addStackCount = 1, int addTime = 0)
    {
        BuffData buffData = BuffRegistry.GetById(buffId);
        if (buffData == null)
        {
            Debug.LogWarning($"[BuffSystem] 找不到 Buff ID: {buffId}");
            return false;
        }

        // 如果没有传入 creator，默认使用 owner
        if (creator == null) creator = owner;

        // target 默认为 owner
        GameObject target = owner;

        AddBuff(new BuffInfo
        {
            buffData = buffData,
            creator = creator,
            target = target,
        }, addStackCount, addTime);

        return true;
    }

    /// <summary>
    /// 内部添加 Buff 逻辑
    /// </summary>
    private void AddBuff(BuffInfo buffInfo, int addStackCount = 1, int addTime = 0)
    {
        BuffInfo existingBuff = FindBuff(buffInfo.buffData.id);

        if (existingBuff != null) // Buff 已存在
        {
            var ticker = (IBuffTicker)existingBuff;
            switch (existingBuff.buffData.buffUpdateTime)
            {
                case E_BuffUpdateTime.AddDuration:
                    // 移除时长逻辑，Buff 均为永久，此处仅做占位或改为增加层数
                    // ticker.DurationTimer += addTime;
                    break;
                case E_BuffUpdateTime.Replace:
                    // 移除时长重置逻辑
                    // ticker.DurationTimer = buffInfo.buffData.duration;
                    buffInfo.buffData.TriggerEffect(E_BuffCallBackType.Create, existingBuff);
                    break;
                case E_BuffUpdateTime.KeepAndAddStack:
                    if (ticker.CurStack < existingBuff.buffData.maxStack)
                    {
                        ticker.CurStack += addStackCount;
                        buffInfo.buffData.TriggerEffect(E_BuffCallBackType.AddStack, existingBuff);
                    }
                    break;
            }
        }
        else // 新 Buff
        {
            // 添加到队列，下一帧（Update）处理，或者立即处理
            // 为简化逻辑，保持原 BuffHandler 为 Update 中处理，这里我们也放入队列
            // 但考虑到 BuffSystem 可能是非 Mono，如果不每帧 Update，这里可能需要立即处理
            // 为了安全起见，我们选择**立即处理**，因为通常逻辑调用添加 Buff 后期望立即生效
            // 除非有明确的队列需求。原 BuffHandler 用 Queue 可能是为了避免遍历时修改集合。
            // 这里我们直接调用 AddBuffSorted 并处理回调。

            // 注意：如果回调中又触发了 Add/Remove，直接操作 List 可能会报错。
            // 暂时保持 Queue 机制，并在 Update 中处理，但也提供 Flush 方法。
            buffToAddQueue.Enqueue(buffInfo);

            // 为了保证即时性，如果是在逻辑帧内，我们可以手动 Flush 一次
            // 或者仅仅依赖 Update。让我们看看原代码：Awake -> Update.
            // 原代码是 Update 处理 Queue。

            // 初始化数据
            var ticker = (IBuffTicker)buffInfo;
            ticker.TickTimer = buffInfo.buffData.tickTime;
            ticker.CurStack = addStackCount;
            ticker.TickTimer = buffInfo.buffData.tickTime;
            ticker.CurStack = addStackCount;
            // 移除时长初始化，设置为最大值或忽略
            ticker.DurationTimer = int.MaxValue;

            Debug.Log($"[BuffSystem] 添加 Buff 请求: {buffInfo.buffData.buffName}");
        }
    }

    /// <summary>按优先级插入 Buff</summary>
    private void AddBuffSorted(BuffInfo newBuff)
    {
        // 触发创建回调
        newBuff.buffData.TriggerEffect(E_BuffCallBackType.Create, newBuff);
        if (newBuff.buffData.triggerTickOnCreate)
        {
            newBuff.buffData.TriggerEffect(E_BuffCallBackType.Tick, newBuff);
        }

        if (buffInfoList.Count == 0)
        {
            buffInfoList.Add(newBuff);
            // 事件通知
            OnBuffAdded?.Invoke(newBuff);
            return;
        }

        bool added = false;
        for (int i = 0; i < buffInfoList.Count; i++)
        {
            if (newBuff.buffData.priority > buffInfoList[i].buffData.priority)
            {
                buffInfoList.Insert(i, newBuff);
                added = true;
                break;
            }
        }

        if (!added)
        {
            buffInfoList.Add(newBuff);
        }

        OnBuffAdded?.Invoke(newBuff);
    }

    /// <summary>减少 Buff 层数或时间</summary>
    public bool ReduceBuff(int id, int reduceStackCount = 0, int reduceTime = 0)
    {
        BuffInfo findBuff = FindBuff(id);
        if (findBuff == null) return false;

        IBuffTicker ticker = (IBuffTicker)findBuff;

        switch (findBuff.buffData.buffRemoveStackUpdate)
        {
            case E_BuffRemoveStackUpdate.ClearStack:
                ticker.CurStack = 0;
                ticker.DurationTimer = 0;
                findBuff.buffData.TriggerEffect(E_BuffCallBackType.ReduceStack, findBuff);
                findBuff.buffData.TriggerEffect(E_BuffCallBackType.Remove, findBuff);
                buffToRemoveQueue.Enqueue(findBuff);
                break;

            case E_BuffRemoveStackUpdate.ReduceStack:
                ticker.CurStack = Mathf.Max(0, ticker.CurStack - reduceStackCount);
                // 移除时长减少逻辑
                // ticker.DurationTimer = Mathf.Max(0, ticker.DurationTimer - reduceTime);
                findBuff.buffData.TriggerEffect(E_BuffCallBackType.ReduceStack, findBuff);

                if (ticker.CurStack <= 0)
                {
                    findBuff.buffData.TriggerEffect(E_BuffCallBackType.Remove, findBuff);
                    buffToRemoveQueue.Enqueue(findBuff);
                }
                break;
        }

        return true;
    }

    /// <summary>移除指定 Buff</summary>
    public bool RemoveBuff(int id)
    {
        BuffInfo findBuff = FindBuff(id);
        if (findBuff == null) return false;

        IBuffTicker ticker = (IBuffTicker)findBuff;
        ticker.CurStack = 0;
        ticker.DurationTimer = 0;

        findBuff.buffData.TriggerEffect(E_BuffCallBackType.ReduceStack, findBuff);
        findBuff.buffData.TriggerEffect(E_BuffCallBackType.Remove, findBuff);
        buffToRemoveQueue.Enqueue(findBuff);

        Debug.Log($"[BuffSystem] 移除 Buff: {findBuff.buffData.buffName}");
        return true;
    }

    // ========== 事件 ==========

    /// <summary>
    /// Buff 添加事件（供 UI 监听）
    /// </summary>
    public event System.Action<BuffInfo> OnBuffAdded;

    /// <summary>
    /// Buff 移除事件（供 UI 监听）
    /// </summary>
    public event System.Action<BuffInfo> OnBuffRemoved; // 需在移除队列处理时触发
}
