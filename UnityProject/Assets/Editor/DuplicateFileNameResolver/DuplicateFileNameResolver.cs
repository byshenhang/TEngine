using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class DuplicateFileNameResolver : EditorWindow
{
    private string selectedPath = "";
    private Dictionary<string, List<string>> duplicateGroups = new();
    private Vector2 scroll;

    [MenuItem("Tools/Duplicate File Name Resolver")]
    public static void ShowWindow()
    {
        GetWindow<DuplicateFileNameResolver>("File Name Resolver");
    }

    void OnGUI()
    {
        GUILayout.Label("选择目录", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        selectedPath = EditorGUILayout.TextField(selectedPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择要分析的文件夹", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                selectedPath = path;
            }
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("分析"))
        {
            AnalyzeFiles();
        }

        if (duplicateGroups.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("发现的重名文件：", EditorStyles.boldLabel);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(200));
            foreach (var kvp in duplicateGroups)
            {
                GUILayout.Label($"文件名: {kvp.Key} ({kvp.Value.Count} 个)", EditorStyles.helpBox);
                foreach (var path in kvp.Value)
                {
                    GUILayout.Label("   " + path);
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("解决重名"))
            {
                SolveDuplicates();
            }
        }
    }

    void AnalyzeFiles()
    {
        duplicateGroups.Clear();

        if (!Directory.Exists(selectedPath))
        {
            Debug.LogError("路径不存在！");
            return;
        }

        string[] files = Directory.GetFiles(selectedPath, "*.*", SearchOption.AllDirectories)
                                  .Where(f => !f.EndsWith(".meta"))
                                  .ToArray();
        Dictionary<string, List<string>> nameMap = new();

        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (!nameMap.ContainsKey(fileName))
                nameMap[fileName] = new List<string>();
            nameMap[fileName].Add(file);
        }

        duplicateGroups = nameMap.Where(kvp => kvp.Value.Count > 1)
                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    void SolveDuplicates()
    {
        foreach (var kvp in duplicateGroups)
        {
            var name = kvp.Key;
            var paths = kvp.Value;

            HashSet<string> usedNames = new();
            foreach (string absolutePath in paths)
            {
                string unityOldPath = ToUnityPath(absolutePath);
                string dir = Path.GetDirectoryName(unityOldPath); // <- 相对路径中的目录
                string ext = Path.GetExtension(unityOldPath).ToLower().TrimStart('.');
                string baseName = $"{name}_{ext}";
                string newName = baseName;
                int counter = 1;

                while (usedNames.Contains(newName) || AssetExists(Path.Combine(dir, newName + "." + ext)))
                {
                    newName = $"{baseName}_{counter.ToString("00")}";
                    counter++;
                }

                string unityNewPath = Path.Combine(dir, newName + "." + ext).Replace("\\", "/");
                string error = AssetDatabase.MoveAsset(unityOldPath, unityNewPath);

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"重命名失败: {unityOldPath} → {unityNewPath}, 错误: {error}");
                }
                else
                {
                    Debug.Log($"已重命名: {unityOldPath} → {unityNewPath}");
                }

                usedNames.Add(newName);
            }
        }

        AssetDatabase.Refresh();
        duplicateGroups.Clear();
        EditorUtility.DisplayDialog("完成", "重命名完成！", "确定");
    }

    private bool AssetExists(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null;
    }

    private string ToUnityPath(string fullPath)
    {
        string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        return fullPath.Replace(projectRoot, "").Replace("\\", "/");
    }
}
