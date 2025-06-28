using UnityEditor;
using UnityEngine;

public class BuildDocWindow : EditorWindow
{
    [MenuItem("Tools/Doc/BuildDoc")]
    public static void ShowWindow()
    {
        var window = GetWindow<BuildDocWindow>("打包运行文档");
        window.minSize = new Vector2(500, 350);
        window.Show();
    }

    void OnGUI()
    {
        // 标题样式
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 10, 20)
        };

        // 步骤样式
        GUIStyle stepStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            padding = new RectOffset(20, 10, 5, 5),
            wordWrap = true
        };

        // 警告样式
        GUIStyle warningStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.red },
            padding = new RectOffset(20, 10, 5, 5),
            wordWrap = true
        };

        // 绘制标题
        EditorGUILayout.LabelField("打包运行", titleStyle);
        EditorGUILayout.Space(10);

        // 步骤1
        EditorGUILayout.LabelField("1.运行菜单 HybridCLR/Install... 安装HybridCLR，每次更新HybridCLR版本需要重新执行一次安装。", stepStyle);

        // 步骤2
        EditorGUILayout.LabelField("2.运行菜单 HybridCLR/Define Symbols/Enable HybridCLR 运行开启 HybridCLR 热更新。", stepStyle);

        // 步骤3（带警告）
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("3.运行菜单 HybridCLR/Generate/All 进行必要的生成操作。", stepStyle);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("这一步不可遗漏!!!", warningStyle);

        // 步骤4
        EditorGUILayout.LabelField("4.运行菜单 HybridCLR/Build/BuildAssets And CopyTo AssemblyPath，生成热更新dll并copy到热更程序集中。", stepStyle);

        // 步骤5
        EditorGUILayout.LabelField("5.运行菜单 YooAsset/AssetBundle Builder 构建 AB。", stepStyle);

        // 步骤6
        EditorGUILayout.LabelField("6.打开Build Settings对话框，点击Build And Run（选择ClearAndCopyAll），打包并且运行热更新示例工程。", stepStyle);

        // 添加一些额外空间
        EditorGUILayout.Space(20);

        // 添加关闭按钮
        if (GUILayout.Button("关闭", GUILayout.Height(30)))
        {
            Close();
        }
    }
}