using System.IO;
using cfg;
using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement; // 添加命名空间引用

/// <summary>
/// 主菜单状态 - 处理主菜单界面的显示、交互和导航逻辑
/// </summary>
public class MainMenuState : LeafState<GameProcedureContext>
{
    private static bool s_tablesRegistered;
    private static Tables s_tables;

    public MainMenuState()
    {
        Name = nameof(MainMenuState);
    }

    protected override void OnEnter(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Enter - 显示主菜单界面");

        // 如果当前不在主菜单场景，则加载
        if (SceneManager.GetActiveScene().name != "MainScene")
        {
            SceneManager.LoadScene("MainScene");
        }

        // 显示主菜单 UI
        
        UIManager.Instance.HideAllPanels(); // 确保其他面板关闭
        UIManager.Instance.ShowPanel("MainSceneUI", (panel) =>
        {
            Debug.Log("主菜单 UI 加载完成");
        });
        
    }

    protected override void OnUpdate(GameProcedureContext ctx)
    {
        // UI 交互逻辑由 Panel 自身脚本处理，状态机仅负责状态流转
    }

    protected override void OnExit(GameProcedureContext ctx)
    {
        Debug.Log($"[{Name}] Exit - 隐藏主菜单界面");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HidePanel("MainMenuPanel");
        }
    }
}
