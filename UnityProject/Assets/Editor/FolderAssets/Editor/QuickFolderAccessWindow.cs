using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class QuickFolderAccessWindow : EditorWindow
{
    [System.Serializable]
    private class FolderEntry
    {
        public string name;
        public string path;
        public string group;
    }

    private Dictionary<string, List<FolderEntry>> groupedFolders = new Dictionary<string, List<FolderEntry>>();
    private Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();
    private Vector2 scrollPos;
    private const string PREF_KEY = "QuickFolderAccess_Grouped";

    private string newGroupName = "Default";

    [MenuItem("Tools/Quick Folder Access")]
    public static void ShowWindow()
    {
        GetWindow<QuickFolderAccessWindow>("Quick Folder Access");
    }

    private void OnEnable()
    {
        LoadFolders();
    }

    private void OnGUI()
    {
        GUILayout.Label("📁 分组快捷访问目录", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        newGroupName = EditorGUILayout.TextField("分组名", newGroupName);
        if (GUILayout.Button("添加当前选中目录", GUILayout.Width(150)))
        {
            AddSelectedFolder(newGroupName);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var group in groupedFolders)
        {
            groupFoldouts.TryAdd(group.Key, true);
            groupFoldouts[group.Key] = EditorGUILayout.Foldout(groupFoldouts[group.Key], group.Key, true);
            if (groupFoldouts[group.Key])
            {
                EditorGUI.indentLevel++;
                var folderList = group.Value;
                for (int i = 0; i < folderList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("📂 " + folderList[i].name, GUILayout.ExpandWidth(true)))
                    {
                        FocusFolder(folderList[i].path);
                    }

                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        folderList.RemoveAt(i);
                        SaveFolders();
                        GUI.backgroundColor = Color.white;
                        break;
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void AddSelectedFolder(string group)
    {
        var selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogWarning("未选择任何对象");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selected);

        if (!AssetDatabase.IsValidFolder(path))
        {
            // 如果是文件，获取其所在文件夹
            path = Path.GetDirectoryName(path);
        }

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("无法获取路径");
            return;
        }

        if (!groupedFolders.ContainsKey(group))
            groupedFolders[group] = new List<FolderEntry>();

        if (groupedFolders[group].Exists(f => f.path == path))
        {
            Debug.LogWarning("该路径已存在于分组中");
            return;
        }

        groupedFolders[group].Add(new FolderEntry
        {
            name = Path.GetFileName(path),
            path = path,
            group = group
        });

        SaveFolders();
    }

    private void FocusFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogWarning("路径不是有效文件夹: " + path);
            return;
        }

        // 获取目录下第一个非.meta文件
        string fullPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), path);
        if (Directory.Exists(fullPath))
        {
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                if (!file.EndsWith(".meta"))
                {
                    string relativePath = "Assets" + file.Replace(Application.dataPath, "").Replace("\\", "/");
                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
                    if (obj != null)
                    {
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                        return;
                    }
                }
            }
        }

        // 如果没有文件则选中文件夹
        var folderObj = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (folderObj)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = folderObj;
        }
        else
        {
            Debug.LogWarning("无法找到路径：" + path);
        }
    }

    private void SaveFolders()
    {
        List<string> saveList = new List<string>();
        foreach (var kvp in groupedFolders)
        {
            foreach (var entry in kvp.Value)
            {
                saveList.Add(entry.name + "|" + entry.path + "|" + entry.group);
            }
        }
        EditorPrefs.SetString(PREF_KEY, string.Join(";", saveList));
    }

    private void LoadFolders()
    {
        groupedFolders.Clear();
        groupFoldouts.Clear();

        string data = EditorPrefs.GetString(PREF_KEY, "");
        if (!string.IsNullOrEmpty(data))
        {
            var entries = data.Split(';');
            foreach (var entry in entries)
            {
                var parts = entry.Split('|');
                if (parts.Length == 3)
                {
                    FolderEntry folder = new FolderEntry
                    {
                        name = parts[0],
                        path = parts[1],
                        group = parts[2]
                    };

                    if (!groupedFolders.ContainsKey(folder.group))
                        groupedFolders[folder.group] = new List<FolderEntry>();

                    groupedFolders[folder.group].Add(folder);
                }
            }
        }
    }
}
