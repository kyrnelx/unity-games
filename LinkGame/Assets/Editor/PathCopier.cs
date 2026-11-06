using UnityEditor;
using UnityEngine;

namespace Kyrnel
{
    public static class PathCopier
    {
        [MenuItem("GameObject/Copy Hierarchy Path", false, 0)]
        private static void CopyHierarchyPath()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null) return;
            string path = GetGameObjectPath(go.transform);
            GUIUtility.systemCopyBuffer = path;
        }

        private static string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path      = transform.name + "/" + path;
            }

            return path;
        }

        [MenuItem("Assets/Copy Project Path", false, 0)]
        private static void CopyProjectPath()
        {
            Object obj = Selection.activeObject;
            if (obj == null) return;
            string path = AssetDatabase.GetAssetPath(obj);
            GUIUtility.systemCopyBuffer = path;
        }
    }
}