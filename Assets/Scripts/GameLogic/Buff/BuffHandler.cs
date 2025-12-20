using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 挂载在目标 GameObject 上的 Buff 处理器
/// 管理 Buff 的添加、移除、触发
/// </summary>
public class BuffHandler : MonoBehaviour
{
    private readonly List<BuffInfo> buffInfoList = new();
    private readonly Queue<BuffInfo> buffToAddQueue = new();
    private readonly Queue<BuffInfo> buffToRemoveQueue = new();

    private void Awake()
    {
        // 确保 BuffRegistry 已初始化
        BuffRegistry.Initialize();
    }

    private void Update()
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
            buffInfoList.Remove(buffInfo);
        }

        // 触发 Tick 回调
        foreach (var buffInfo in buffInfoList)
        {
            buffInfo.buffData.TriggerEffect(E_BuffCallBackType.Tick, buffInfo);
        }
    }

    /// <summary>Buff 列表（只读）</summary>
    public IReadOnlyList<BuffInfo> BuffInfoList => buffInfoList.ToList();

    /// <summary>清空所有 Buff</summary>
    public void ClearBuff() => buffInfoList.Clear();

    /// <summary>检查 Buff 是否存在</summary>
    public bool BuffExist(int id) => buffInfoList.Any(b => b.buffData.id == id);

    /// <summary>
    /// 触发自定义回调（供结算等系统调用）
    /// </summary>
    public void TriggerCustom(E_BuffCallBackType callbackType, params object[] args)
    {
        foreach (var buffInfo in buffInfoList)
        {
            buffInfo.buffData.TriggerEffect(callbackType, buffInfo, args);
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
            Debug.LogWarning($"[BuffHandler] 找不到 Buff: {buffName}");
            return false;
        }

        return AddBuff(new BuffInfo
        {
            buffData = buffData,
            creator = creator,
            target = gameObject,
        }, addStackCount, addTime);
    }

    /// <summary>
    /// 通过 ID 添加 Buff
    /// </summary>
    public bool AddBuffById(int buffId, GameObject creator = null, int addStackCount = 1, int addTime = 0)
    {
        BuffData buffData = BuffRegistry.GetById(buffId);
        if (buffData == null)
        {
            Debug.LogWarning($"[BuffHandler] 找不到 Buff ID: {buffId}");
            return false;
        }

        return AddBuff(new BuffInfo
        {
            buffData = buffData,
            creator = creator,
            target = gameObject,
        }, addStackCount, addTime);
    }

    /// <summary>
    /// 内部添加 Buff 逻辑
    /// </summary>
    private bool AddBuff(BuffInfo buffInfo, int addStackCount = 1, int addTime = 0)
    {
        BuffInfo existingBuff = FindBuff(buffInfo.buffData.id);

        if (existingBuff != null) // Buff 已存在
        {
            var ticker = (IBuffTicker)existingBuff;
            switch (existingBuff.buffData.buffUpdateTime)
            {
                case E_BuffUpdateTime.AddDuration:
                    ticker.DurationTimer += addTime;
                    break;
                case E_BuffUpdateTime.Replace:
                    ticker.DurationTimer = buffInfo.buffData.duration;
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
            buffToAddQueue.Enqueue(buffInfo);
            var ticker = (IBuffTicker)buffInfo;
            ticker.TickTimer = buffInfo.buffData.tickTime;
            ticker.CurStack = addStackCount;
            ticker.DurationTimer = addTime > 0 ? addTime : buffInfo.buffData.duration;

            buffInfo.buffData.TriggerEffect(E_BuffCallBackType.Create, buffInfo);

            if (buffInfo.buffData.triggerTickOnCreate)
            {
                buffInfo.buffData.TriggerEffect(E_BuffCallBackType.Tick, buffInfo);
            }

            Debug.Log($"[BuffHandler] 添加 Buff: {buffInfo.buffData.buffName}");
        }

        return true;
    }

    /// <summary>按优先级插入 Buff</summary>
    private void AddBuffSorted(BuffInfo newBuff)
    {
        if (buffInfoList.Count == 0)
        {
            buffInfoList.Add(newBuff);
            return;
        }

        for (int i = 0; i < buffInfoList.Count; i++)
        {
            if (newBuff.buffData.priority > buffInfoList[i].buffData.priority)
            {
                buffInfoList.Insert(i, newBuff);
                return;
            }
        }

        buffInfoList.Add(newBuff);
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
                ticker.DurationTimer = Mathf.Max(0, ticker.DurationTimer - reduceTime);
                findBuff.buffData.TriggerEffect(E_BuffCallBackType.ReduceStack, findBuff);

                if (ticker.CurStack <= 0 || (!findBuff.buffData.isForever && ticker.DurationTimer <= 0))
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

        Debug.Log($"[BuffHandler] 移除 Buff: {findBuff.buffData.buffName}");
        return true;
    }
}
