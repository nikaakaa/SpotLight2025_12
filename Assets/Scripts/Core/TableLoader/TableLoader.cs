using System;
using cfg;
using SimpleJSON;
using UnityEngine;

/// <summary>
/// Luban 配置表加载器
/// 使用 AddressablesMgr 加载配置表数据
/// </summary>
public static class TableLoader
{
    #region 配置项 - 根据你的 Addressables 设置修改

    /// <summary>
    /// Addressables 资源路径前缀
    /// 例如: "DataConfig/" 或 "" (无前缀)
    /// </summary>
    private const string AddressablePathPrefix = "";

    /// <summary>
    /// Addressables 资源后缀
    /// 例如: ".json" 或 "" (无后缀)
    /// </summary>
    private const string AddressablePathSuffix = "";

    #endregion

    private static Tables tables;

    /// <summary>
    /// 获取配置表实例（懒加载，同步方式）
    /// </summary>
    public static Tables Tables
    {
        get
        {
            if (tables == null)
            {
                LoadTablesSync();
            }
            return tables;
        }
    }

    /// <summary>
    /// 是否已加载配置表
    /// </summary>
    public static bool IsLoaded => tables != null;

    /// <summary>
    /// 同步加载所有配置表
    /// </summary>
    public static void LoadTablesSync()
    {
        tables = new Tables(LoadJsonSync);
        Debug.Log("[TableLoader] 配置表同步加载完成");
    }

    /// <summary>
    /// 异步加载所有配置表
    /// </summary>
    public static void LoadTablesAsync(Action onComplete)
    {
        tables = new Tables(LoadJsonSync);
        Debug.Log("[TableLoader] 配置表加载完成");
        onComplete?.Invoke();
    }

    /// <summary>
    /// 重新加载配置表
    /// </summary>
    public static void ReloadTables()
    {
        UnloadTables();
        LoadTablesSync();
    }

    /// <summary>
    /// 卸载配置表
    /// </summary>
    public static void UnloadTables()
    {
        tables = null;
    }

    /// <summary>
    /// 获取 Addressables 资源地址
    /// </summary>
    /// <param name="tableName">Luban 传入的表名 (如 "tbnode")</param>
    /// <returns>完整的 Addressables 地址</returns>
    private static string GetAddressablePath(string tableName)
    {
        // 根据你的 Addressables 配置修改这里的拼接规则
        // 常见格式:
        // - "tbnode"            -> 直接用表名
        // - "DataConfig/tbnode" -> 带路径前缀
        // - "tbnode.json"       -> 带文件后缀
        return $"{AddressablePathPrefix}{tableName}{AddressablePathSuffix}";
    }

    /// <summary>
    /// 同步加载 JSON 配置
    /// </summary>
    private static JSONNode LoadJsonSync(string tableName)
    {
        string addressablePath = GetAddressablePath(tableName);
        TextAsset textAsset = AddressablesMgr.Instance.LoadAssetSync<TextAsset>(addressablePath);

        if (textAsset != null)
        {
            return JSON.Parse(textAsset.text);
        }

        Debug.LogError($"[TableLoader] 无法加载配置表: {tableName} (地址: {addressablePath})");
        return null;
    }
}
