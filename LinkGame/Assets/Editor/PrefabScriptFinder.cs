using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kyrnel
{
    public class PrefabScriptFinder : EditorWindow
    {
        private string           _scriptName     = string.Empty;
        private List<GameObject> _prefabs        = new();
        private Vector2          _scrollPosition = Vector2.zero;

        [MenuItem("Tools/预制体脚本查找工具")]
        public static void ShowWindow()
        {
            GetWindow<PrefabScriptFinder>("预制体脚本查找工具");
        }

        private void OnGUI()
        {
            GUILayout.Label("PrefabScriptFinder", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _scriptName = EditorGUILayout.TextField("Script Name:", _scriptName);
            if (GUILayout.Button("Search", GUILayout.Width(100))) 
                SearchPrefabs();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_prefabs.Count > 0)
            {
                GUILayout.Label("Results:",  EditorStyles.boldLabel);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                foreach (var result in _prefabs)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(result, typeof(GameObject), false);

                    if (GUILayout.Button("Ping", GUILayout.Width(100)))
                    {
                        EditorGUIUtility.PingObject(result);
                        Selection.activeObject = result;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Results:",  EditorStyles.boldLabel);
            }
        }

        private void SearchPrefabs()
        {
            _prefabs.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var components = prefab.GetComponentsInChildren<Component>();
                if (!prefab) continue;
                foreach (var component in components)
                {
                    if (component != null && component.GetType().Name == _scriptName)
                    {
                        _prefabs.Add(component.gameObject);
                        break;
                    }
                }
            }
        }
    }
}