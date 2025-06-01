using UnityEditor;
using UnityEngine;
using System.Text;

public static class ExportHierarchyContext
{
    [MenuItem("GameObject/自定义/导出层级与脚本信息到剪贴板", false, 49)]
    private static void ExportSelectedHierarchy()
    {
        var selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("请在 Hierarchy 中选择至少一个 GameObject。");
            return;
        }

        StringBuilder sb = new StringBuilder();
        foreach (GameObject go in selectedObjects)
        {
            Traverse(go.transform, 0, sb);
        }

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("已复制层级与脚本信息到剪贴板！");
    }

    private static void Traverse(Transform transform, int level, StringBuilder sb)
    {
        string indent = new string(' ', level * 4);
        sb.AppendLine($"{indent}- {transform.name}");

        Component[] components = transform.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var type = comp.GetType();
            if (type == typeof(Transform)) continue; // 忽略 Transform
            sb.AppendLine($"{indent}    [Component] {type.FullName}");
        }

        foreach (Transform child in transform)
        {
            Traverse(child, level + 1, sb);
        }
    }
}
