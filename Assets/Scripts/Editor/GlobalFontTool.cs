using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class GlobalFontTool : EditorWindow
{
    private TMP_FontAsset targetFont;
    private bool includePrefabs = true;
    private bool includeCurrentScene = true;

    [MenuItem("Tools/统一修改字体 (Global Font Replacer)")]
    public static void ShowWindow()
    {
        GetWindow<GlobalFontTool>("字体替换工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("全局字体替换设置", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("目标字体 (TMP Font Asset)", targetFont, typeof(TMP_FontAsset), false);
        
        EditorGUILayout.Space();
        includeCurrentScene = EditorGUILayout.Toggle("替换当前场景", includeCurrentScene);
        includePrefabs = EditorGUILayout.Toggle("替换所有预制体", includePrefabs);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("警告：此操作不可撤销，建议操作前备份项目！\n将查找所有 TextMeshProUGUI 组件并替换其 Font Asset。", MessageType.Warning);

        if (GUILayout.Button("开始替换"))
        {
            if (targetFont == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择目标字体！", "确定");
                return;
            }

            if (EditorUtility.DisplayDialog("确认替换", 
                $"确定要将项目中 { (includePrefabs ? "所有预制体" : "") } { (includeCurrentScene && includePrefabs ? "和" : "") } { (includeCurrentScene ? "当前场景" : "") } 的字体替换为 {targetFont.name} 吗？", 
                "确定执行", "取消"))
            {
                ReplaceFonts();
            }
        }
    }

    private void ReplaceFonts()
    {
        int count = 0;

        // 1. 替换当前场景
        if (includeCurrentScene)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var txt in texts)
                {
                    if (txt.font != targetFont)
                    {
                        Undo.RecordObject(txt, "Replace Font");
                        txt.font = targetFont;
                        EditorUtility.SetDirty(txt);
                        count++;
                    }
                }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        // 2. 替换所有预制体
        if (includePrefabs)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            List<string> paths = new List<string>();
            foreach (var guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            float total = paths.Count;
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                EditorUtility.DisplayProgressBar("正在替换预制体字体", path, i / total);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    // 仅加载内容不实例化
                    GameObject contents = PrefabUtility.LoadPrefabContents(path);
                    var texts = contents.GetComponentsInChildren<TextMeshProUGUI>(true);
                    bool dirty = false;

                    foreach (var txt in texts)
                    {
                        if (txt.font != targetFont)
                        {
                            txt.font = targetFont;
                            dirty = true;
                            count++;
                        }
                    }

                    if (dirty)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                    }
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"字体替换完成！\n共修改了 {count} 个文本组件。", "确定");
    }
}
