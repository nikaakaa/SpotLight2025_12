using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;

/// <summary>
/// 中心化数据管理器
/// 统一入口读取：Luban 配置表、ScriptableObject 配置、运行时数据
/// 遵循单一职责：只做数据访问，不做业务逻辑
/// </summary>
public class DataManager : BaseManager<DataManager>
{
    private DataManager() { }

    // ========== Luban 配置表（静态配置） ==========

    /// <summary>
    /// 获取 Luban 配置表
    /// 使用方式：DataManager.Instance.Tables.TbNode.Get(id)
    /// </summary>
    public Tables Tables => TableLoader.Tables;

    /// <summary>
    /// Luban 表是否已加载
    /// </summary>
    public bool IsTablesLoaded => TableLoader.IsLoaded;

    /// <summary>
    /// 重新加载 Luban 配置表（热更新用）
    /// </summary>
    public void ReloadTables() => TableLoader.ReloadTables();

    // ========== ScriptableObject 配置 ==========

    // SO 缓存字典（按类型 + 资源名缓存）
    private readonly Dictionary<string, ScriptableObject> soCache = new Dictionary<string, ScriptableObject>();

    /// <summary>
    /// 同步加载 ScriptableObject 配置
    /// </summary>
    /// <typeparam name="T">ScriptableObject 类型</typeparam>
    /// <param name="addressableName">Addressables 地址</param>
    /// <param name="useCache">是否使用缓存（默认 true）</param>
    /// <returns>SO 实例，加载失败返回 null</returns>
    public T LoadSO<T>(string addressableName, bool useCache = true) where T : ScriptableObject
    {
        string cacheKey = $"{typeof(T).Name}_{addressableName}";

        // 尝试从缓存获取
        if (useCache && soCache.TryGetValue(cacheKey, out var cached))
        {
            return cached as T;
        }

        // 同步加载
        T so = AddressablesMgr.Instance.LoadAssetSync<T>(addressableName);
        if (so != null && useCache)
        {
            soCache[cacheKey] = so;
        }

        if (so == null)
        {
            Debug.LogWarning($"[DataManager] 无法加载 SO: {addressableName} (类型: {typeof(T).Name})");
        }

        return so;
    }

    /// <summary>
    /// 异步加载 ScriptableObject 配置
    /// </summary>
    /// <typeparam name="T">ScriptableObject 类型</typeparam>
    /// <param name="addressableName">Addressables 地址</param>
    /// <param name="callback">加载完成回调</param>
    /// <param name="useCache">是否使用缓存（默认 true）</param>
    public void LoadSOAsync<T>(string addressableName, Action<T> callback, bool useCache = true) where T : ScriptableObject
    {
        string cacheKey = $"{typeof(T).Name}_{addressableName}";

        // 尝试从缓存获取
        if (useCache && soCache.TryGetValue(cacheKey, out var cached))
        {
            callback?.Invoke(cached as T);
            return;
        }

        // 异步加载
        AddressablesMgr.Instance.LoadAssetAsync<T>(addressableName, (so) =>
        {
            if (so != null && useCache)
            {
                soCache[cacheKey] = so;
            }
            callback?.Invoke(so);
        });
    }

    /// <summary>
    /// 批量加载同类型的 ScriptableObject
    /// </summary>
    /// <typeparam name="T">ScriptableObject 类型</typeparam>
    /// <param name="addressableNames">Addressables 地址列表</param>
    /// <returns>SO 实例列表</returns>
    public List<T> LoadSOBatch<T>(params string[] addressableNames) where T : ScriptableObject
    {
        var results = new List<T>();
        foreach (var name in addressableNames)
        {
            var so = LoadSO<T>(name);
            if (so != null)
            {
                results.Add(so);
            }
        }
        return results;
    }

    /// <summary>
    /// 释放 SO 缓存
    /// </summary>
    /// <param name="addressableName">指定资源名（为空则清空全部）</param>
    public void ReleaseSO<T>(string addressableName = null) where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(addressableName))
        {
            // 清空所有该类型的缓存
            var keysToRemove = new List<string>();
            string typePrefix = $"{typeof(T).Name}_";

            foreach (var key in soCache.Keys)
            {
                if (key.StartsWith(typePrefix))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                soCache.Remove(key);
            }
        }
        else
        {
            string cacheKey = $"{typeof(T).Name}_{addressableName}";
            soCache.Remove(cacheKey);
        }
    }

    /// <summary>
    /// 清空所有 SO 缓存
    /// </summary>
    public void ClearSOCache()
    {
        soCache.Clear();
    }

    // ========== 运行时数据访问（快捷方式） ==========

    /// <summary>
    /// 获取玩家运行时数据
    /// 使用方式：DataManager.Instance.Player.Assets
    /// </summary>

    /// <summary>
    /// 获取航线系统
    /// 使用方式：DataManager.Instance.Aviation.aviationNodeDict
    /// </summary>
    public AviationSystem Aviation => AviationSystem.Instance;

    // ========== 全局数据操作 ==========

    /// <summary>
    /// 初始化所有数据（游戏启动时调用）
    /// </summary>
    public void Initialize()
    {
        // 预加载 Luban 表
        if (!IsTablesLoaded)
        {
            TableLoader.LoadTablesSync();
        }

        Debug.Log("[DataManager] 数据管理器初始化完成");
    }

    /// <summary>
    /// 开始新游戏（重置所有运行时数据）
    /// </summary>
    public void StartNewGame()
    {
        // Player?.Reset();
        Aviation?.InitAviationSystem();
        Debug.Log("[DataManager] 新游戏数据已重置");
    }

    /// <summary>
    /// 清理所有数据（游戏退出时调用）
    /// </summary>
    public void Cleanup()
    {
        ClearSOCache();
        TableLoader.UnloadTables();
        Debug.Log("[DataManager] 数据管理器已清理");
    }
}
