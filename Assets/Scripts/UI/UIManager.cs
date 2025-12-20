using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 管理器 - 基于 Addressables 的纯调度器
/// 负责 UI 面板的异步加载、实例化和显示管理
/// 具体 UI 逻辑由各个 Panel 自身的脚本处理
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 根节点")]
    [SerializeField] private Transform uiRoot;

    [Header("自动创建（Addressables Key，可选）")]
    [Tooltip("当场景中没有 EventSystem 时，使用该 Addressables Key 加载并实例化。留空则代码创建默认 EventSystem。")]
    [SerializeField] private string eventSystemAddress;

    [Tooltip("当场景中没有 Canvas 时，使用该 Addressables Key 加载并实例化。留空则代码创建默认 Canvas。")]
    [SerializeField] private string canvasAddress;

    // 面板缓存：Key = 面板 Addressable Name, Value = 实例化后的 GameObject
    private readonly Dictionary<string, GameObject> panelDict = new Dictionary<string, GameObject>();

    // 加载中的面板集合，防止重复加载
    private readonly HashSet<string> loadingPanels = new HashSet<string>();



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureUIHierarchy();
    }

    private void EnsureUIHierarchy()
    {
        // 1) 确保 EventSystem 存在（否则 UI 无法交互）
        var eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            if (!string.IsNullOrWhiteSpace(eventSystemAddress))
            {
                AddressablesMgr.Instance.LoadAssetAsync<GameObject>(eventSystemAddress, prefab =>
                {
                    if (prefab == null)
                    {
                        Debug.LogError($"[UIManager] EventSystem 预制体加载失败: {eventSystemAddress}");
                        return;
                    }

                    // 回调时可能已经存在（例如别的系统先创建了）
                    if (FindFirstObjectByType<EventSystem>() != null) return;

                    var esGo = Instantiate(prefab);
                    esGo.name = prefab.name;
                    DontDestroyOnLoad(esGo);
                    Debug.Log("[UIManager] 使用 Addressables 创建 EventSystem");
                });
            }
            else
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(esGo);
                Debug.Log("[UIManager] 代码创建 EventSystem");
            }
        }
        else
        {
            // 你手动放到场景里的对象：也确保过场景不移除
            DontDestroyOnLoad(eventSystem.gameObject);
        }

        // 2) 确保 Canvas 存在（否则无法承载 UI）
        if (uiRoot == null)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                if (!string.IsNullOrWhiteSpace(canvasAddress))
                {
                    AddressablesMgr.Instance.LoadAssetAsync<GameObject>(canvasAddress, prefab =>
                    {
                        if (prefab == null)
                        {
                            Debug.LogError($"[UIManager] Canvas 预制体加载失败: {canvasAddress}");
                            return;
                        }

                        // 回调时可能已经存在
                        var existingCanvas = FindFirstObjectByType<Canvas>();
                        if (existingCanvas != null)
                        {
                            if (uiRoot == null) uiRoot = existingCanvas.transform;
                            DontDestroyOnLoad(existingCanvas.gameObject);
                            return;
                        }

                        var canvasGo = Instantiate(prefab);
                        canvasGo.name = prefab.name;

                        var createdCanvas = canvasGo.GetComponentInChildren<Canvas>(true);
                        if (createdCanvas == null)
                        {
                            Debug.LogError($"[UIManager] Canvas 预制体缺少 Canvas 组件: {canvasAddress}");
                            Destroy(canvasGo);
                            return;
                        }

                        DontDestroyOnLoad(canvasGo);
                        DontDestroyOnLoad(createdCanvas.gameObject);
                        uiRoot = createdCanvas.transform;
                        Debug.Log("[UIManager] 使用 Addressables 创建 Canvas");
                    });
                }
                else
                {
                    var canvasGo = new GameObject("Canvas");
                    canvas = canvasGo.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGo.AddComponent<CanvasScaler>();
                    canvasGo.AddComponent<GraphicRaycaster>();
                    DontDestroyOnLoad(canvasGo);
                    Debug.Log("[UIManager] 代码创建 Canvas");
                }
            }

            if (canvas != null)
            {
                DontDestroyOnLoad(canvas.gameObject);
                uiRoot = canvas.transform;
            }
            else
            {
                // 如果走了 Addressables 异步加载，这里可能暂时还拿不到 canvas
                Debug.LogWarning("[UIManager] 当前未找到 Canvas（可能正在异步加载），uiRoot 暂为空");
            }
        }
    }

    private void Start()
    {
        // 自动创建逻辑已移至 EnsureUIHierarchy，Start中不再需要
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 打开面板（异步）
    /// 如果面板已存在则直接显示，否则通过 AddressablesMgr 加载
    /// </summary>
    /// <param name="panelName">Addressables 资源名称（Key）</param>
    /// <param name="onOpened">加载/显示完成后的回调</param>
    public void ShowPanel(string panelName, Action<GameObject> onOpened = null)
    {
        // 兜底：如果 uiRoot 还没就绪，尝试补齐
        if (uiRoot == null)
        {
            EnsureUIHierarchy();
            if (uiRoot == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null) uiRoot = canvas.transform;
            }
        }

        // 1. 检查缓存
        if (panelDict.TryGetValue(panelName, out GameObject panel))
        {
            if (panel != null)
            {
                panel.SetActive(true);
                panel.transform.SetAsLastSibling(); // 确保在最上层
                onOpened?.Invoke(panel);
                return;
            }
            else
            {
                panelDict.Remove(panelName);
            }
        }

        // 2. 检查是否正在加载
        if (loadingPanels.Contains(panelName))
        {
            Debug.LogWarning($"[UIManager] 面板 {panelName} 正在加载中...");
            return;
        }

        // 3. 开始加载
        loadingPanels.Add(panelName);
        Debug.Log($"[UIManager] 开始加载面板: {panelName}");

        AddressablesMgr.Instance.LoadAssetAsync<GameObject>(panelName, (prefab) =>
        {
            loadingPanels.Remove(panelName);

            if (prefab == null)
            {
                Debug.LogError($"[UIManager] 面板资源加载失败: {panelName}");
                return;
            }

            // 实例化
            GameObject instance = Instantiate(prefab, uiRoot ? uiRoot : transform);
            instance.name = panelName; // 去掉(Clone)后缀

            // 缓存
            panelDict[panelName] = instance;

            // 回调
            onOpened?.Invoke(instance);
        });
    }

    /// <summary>
    /// 关闭面板
    /// 默认只是隐藏，不销毁
    /// </summary>
    /// <param name="panelName">面板名称</param>
    /// <param name="destroy">是否销毁实例</param>
    public void HidePanel(string panelName, bool destroy = false)
    {
        if (panelDict.TryGetValue(panelName, out GameObject panel))
        {
            if (panel != null)
            {
                if (destroy)
                {
                    Destroy(panel);
                    panelDict.Remove(panelName);
                    // Addressables 资源的释放由 AddressablesMgr 管理，
                    // 这里我们只是 Destroy 实例。
                }
                else
                {
                    panel.SetActive(false);
                }
            }
            else
            {
                panelDict.Remove(panelName);
            }
        }
    }

    /// <summary>
    /// 获取已打开的面板实例
    /// </summary>
    public GameObject GetPanel(string panelName)
    {
        if (panelDict.TryGetValue(panelName, out GameObject panel))
        {
            return panel;
        }
        return null;
    }

    /// <summary>
    /// 检查面板是否打开
    /// </summary>
    public bool IsPanelOpen(string panelName)
    {
        if (panelDict.TryGetValue(panelName, out GameObject panel))
        {
            return panel != null && panel.activeSelf;
        }
        return false;
    }

    /// <summary>
    /// 隐藏所有已加载的面板
    /// </summary>
    public void HideAllPanels()
    {
        foreach (var panel in panelDict.Values)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
