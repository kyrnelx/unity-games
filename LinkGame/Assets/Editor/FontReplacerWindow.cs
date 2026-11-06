using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel
{
    public class FontReplacerWindow : EditorWindow
    {
        private UnityEngine.Object targetAsset; // 拖入的目标（Prefab / SceneAsset / GameObject）
        private Font targetUGUIFont;
        private UnityEngine.Object targetTMPFont; // 智能识别 TMP_FontAsset

        // 反射识别 TMP 的类型
        private static Type tmpTextType;
        private static Type tmpFontAssetType;
        private static bool isTMPInstalled;

        [MenuItem("Tools/字体替换工具")]
        public static void ShowWindow()
        {
            InitTMPTypes();
            GetWindow<FontReplacerWindow>("字体替换工具");
        }

        private void OnEnable()
        {
            InitTMPTypes();
        }

        /// <summary>
        /// 自动在当前项目程序集中探测 TextMeshPro 是否存在
        /// </summary>
        private static void InitTMPTypes()
        {
            tmpTextType = FindTypeInAssemblies("TMPro.TMP_Text");
            tmpFontAssetType = FindTypeInAssemblies("TMPro.TMP_FontAsset");
            isTMPInstalled = (tmpTextType != null && tmpFontAssetType != null);
        }

        private static Type FindTypeInAssemblies(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null) return type;
            }

            return null;
        }

        private void OnGUI()
        {
            GUILayout.Label("资源字体替换工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 目标拖拽框
            targetAsset = EditorGUILayout.ObjectField("目标资源 (Prefab/Scene/GO)", targetAsset, typeof(UnityEngine.Object),
                true);
            EditorGUILayout.Space();

            // UGUI 字体选择框
            targetUGUIFont =
                (Font)EditorGUILayout.ObjectField("目标 UGUI 字体 (.ttf)", targetUGUIFont, typeof(Font), false);

            // 智能显示/隐藏 TMP 选择框
            if (isTMPInstalled)
            {
                targetTMPFont =
                    EditorGUILayout.ObjectField("目标 TMP 字体 (.asset)", targetTMPFont, tmpFontAssetType, false);
            }
            else
            {
                EditorGUILayout.HelpBox("当前项目未检测到 TextMeshPro，仅启用 UGUI 字体替换。", MessageType.Info);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("开始替换字体", GUILayout.Height(40)))
            {
                if (targetAsset == null)
                {
                    EditorUtility.DisplayDialog("错误", "请先拖入目标预制体 (Prefab)、场景 (Scene) 或层级树节点！", "确定");
                    return;
                }

                if (targetUGUIFont == null && targetTMPFont == null)
                {
                    EditorUtility.DisplayDialog("错误", "请至少指定一种目标字体！", "确定");
                    return;
                }

                ProcessReplacement(targetAsset);
            }
        }

        private void ProcessReplacement(UnityEngine.Object obj)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);

            if (obj is GameObject && PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                ReplaceInPrefab(assetPath);
            }
            else if (obj is GameObject go && !PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                ReplaceInSceneGameObject(go);
            }
            else if (obj is SceneAsset)
            {
                ReplaceInSceneAsset(assetPath);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "不支持的对象类型！请拖入 Prefab 预制体、.unity 场景文件或 Hierarchy 中的 GameObject 节点。",
                    "确定");
            }
        }

        /// <summary>
        /// 核心字体替换逻辑（同时处理 UGUI 和 TMP）
        /// </summary>
        private (int uguiCount, int tmpCount) ReplaceFontsOnGameObject(GameObject root)
        {
            int uguiCount = 0;
            int tmpCount = 0;

            // 1. 替换 UGUI Text
            if (targetUGUIFont != null)
            {
                Text[] texts = root.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    Undo.RecordObject(t, "Replace UGUI Font");
                    t.font = targetUGUIFont;
                    uguiCount++;
                }
            }

            // 2. 利用反射替换 TextMeshPro (TMP_Text)
            if (isTMPInstalled && targetTMPFont != null && tmpTextType != null)
            {
                Component[] tmps = root.GetComponentsInChildren(tmpTextType, true);
                PropertyInfo fontProp = tmpTextType.GetProperty("font");

                if (fontProp != null)
                {
                    foreach (var tmp in tmps)
                    {
                        Undo.RecordObject(tmp, "Replace TMP Font");
                        fontProp.SetValue(tmp, targetTMPFont, null);
                        tmpCount++;
                    }
                }
            }

            return (uguiCount, tmpCount);
        }

        private void ReplaceInPrefab(string prefabPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            var (uguiCount, tmpCount) = ReplaceFontsOnGameObject(prefabRoot);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("成功",
                $"预制体 [{Path.GetFileName(prefabPath)}] 替换完成！\n- UGUI: {uguiCount} 处\n- TMP: {tmpCount} 处", "确定");
        }

        private void ReplaceInSceneGameObject(GameObject root)
        {
            var (uguiCount, tmpCount) = ReplaceFontsOnGameObject(root);
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorUtility.DisplayDialog("成功",
                $"节点 [{root.name}] 及其所有子节点替换完成！\n- UGUI: {uguiCount} 处\n- TMP: {tmpCount} 处\n\n注意：请按 Ctrl+S 保存场景。",
                "确定");
        }

        private void ReplaceInSceneAsset(string scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int uguiCount = 0;
            int tmpCount = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                var (uCount, tCount) = ReplaceFontsOnGameObject(root);
                uguiCount += uCount;
                tmpCount += tCount;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("成功",
                $"场景 [{Path.GetFileName(scenePath)}] 替换并保存成功！\n- UGUI: {uguiCount} 处\n- TMP: {tmpCount} 处", "确定");
        }
    }
}