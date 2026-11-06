using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kyrnel
{
    /// <summary>
    /// URP 材质紫色批量修复工具
    /// </summary>
    public class URPMaterialFixerTool : EditorWindow
    {
        private DefaultAsset targetFolder;
        private Vector2 scrollPos;
        private List<Material> detectedMaterials = new List<Material>();

        [MenuItem("Tools/URP 材质紫色修复工具")]
        public static void ShowWindow()
        {
            GetWindow<URPMaterialFixerTool>("URP 材质修复工具");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("🎨 URP 材质紫色一键修复工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("该工具可自动将旧版内置管线（Built-in/Standard）的紫材质替换为 URP 对应的 Shader，并保留原本的贴图与颜色。",
                MessageType.Info);

            GUILayout.Space(10);

            // 文件夹拖拽框
            targetFolder =
                (DefaultAsset)EditorGUILayout.ObjectField("目标文件夹 (拖入文件夹)", targetFolder, typeof(DefaultAsset), false);

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔍 扫描整个项目", GUILayout.Height(30)))
            {
                ScanMaterials(null);
            }

            if (GUILayout.Button("📂 扫描指定文件夹", GUILayout.Height(30)))
            {
                if (targetFolder == null)
                {
                    EditorUtility.DisplayDialog("提示", "请先将要修复的文件夹拖入上方【目标文件夹】框中！", "确定");
                }
                else
                {
                    string path = AssetDatabase.GetAssetPath(targetFolder);
                    ScanMaterials(path);
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            // 显示扫描到的问题材质
            if (detectedMaterials.Count > 0)
            {
                EditorGUILayout.LabelField($"检测到 {detectedMaterials.Count} 个异常/旧版材质：", EditorStyles.boldLabel);

                if (GUILayout.Button("🚀 批量修复所有检测到的材质", GUILayout.Height(35)))
                {
                    FixMaterials(detectedMaterials);
                    // 修复完成后重新扫描
                    string path = targetFolder != null ? AssetDatabase.GetAssetPath(targetFolder) : null;
                    ScanMaterials(path);
                }

                GUILayout.Space(10);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                foreach (var mat in detectedMaterials)
                {
                    EditorGUILayout.ObjectField(mat, typeof(Material), false);
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("当前尚未扫描，或未发现紫色/旧版材质。", MessageType.None);
            }
        }

        private void ScanMaterials(string folderPath)
        {
            detectedMaterials.Clear();
            string[] searchPaths = string.IsNullOrEmpty(folderPath)
                ? new string[] { "Assets" }
                : new string[] { folderPath };
            string[] guids = AssetDatabase.FindAssets("t:Material", searchPaths);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                if (IsLegacyOrBrokenShader(mat))
                {
                    detectedMaterials.Add(mat);
                }
            }
        }

        private bool IsLegacyOrBrokenShader(Material mat)
        {
            if (mat.shader == null) return true;
            string shaderName = mat.shader.name;

            // 如果已经是正确的 URP Shader，跳过
            if (shaderName.StartsWith("Universal Render Pipeline/") || shaderName.StartsWith("Shader Graphs/"))
            {
                return false;
            }

            // 丢失 Shader 或 报错 Shader（紫材质）
            if (shaderName == "Hidden/InternalErrorShader" || shaderName == "None" || string.IsNullOrEmpty(shaderName))
            {
                return true;
            }

            // 旧版内置管线常用的 Shader
            if (shaderName.StartsWith("Standard") ||
                shaderName.StartsWith("Legacy Shaders/") ||
                shaderName.StartsWith("Mobile/") ||
                shaderName.StartsWith("Unlit/") ||
                shaderName.StartsWith("Particles/") ||
                shaderName.StartsWith("Bumped") ||
                shaderName.StartsWith("Specular"))
            {
                return true;
            }

            return false;
        }

        private void FixMaterials(List<Material> materials)
        {
            int fixedCount = 0;
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");

            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("错误", "未在项目中找到 'Universal Render Pipeline/Lit' Shader，请确认已安装并正确配置 URP！",
                    "确定");
                return;
            }

            try
            {
                for (int i = 0; i < materials.Count; i++)
                {
                    Material mat = materials[i];
                    if (mat == null) continue;

                    EditorUtility.DisplayProgressBar("修复 URP 材质", $"正在修复材质: {mat.name} ({i + 1}/{materials.Count})",
                        (float)i / materials.Count);

                    Undo.RecordObject(mat, "Fix URP Material Shader");

                    // 1. 备份旧材质属性（非常关键：防止切 Shader 后贴图丢失）
                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                    Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                    Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                    Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
                    Color emissionColor =
                        mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

                    string oldShaderName = mat.shader != null ? mat.shader.name : "";

                    // 2. 根据旧类型切换到 URP 对应的 Shader
                    if (oldShaderName.StartsWith("Unlit/"))
                    {
                        mat.shader = urpUnlit != null ? urpUnlit : urpLit;
                    }
                    else
                    {
                        mat.shader = urpLit;
                    }

                    // 3. 将贴图和颜色映射迁移到 URP 的新属性（_MainTex -> _BaseMap, _Color -> _BaseColor）
                    if (mainTex != null)
                    {
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                    }

                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", mainColor);
                    }

                    if (bumpMap != null && mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", bumpMap);
                    }

                    if (emissionMap != null && mat.HasProperty("_EmissionMap"))
                    {
                        mat.SetTexture("_EmissionMap", emissionMap);
                        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
                    }

                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("修复完成", $"成功将 {fixedCount} 个材质修复为 URP 材质！", "确定");
        }
    }
}