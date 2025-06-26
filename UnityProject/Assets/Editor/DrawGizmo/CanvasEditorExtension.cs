using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Canvas))]
public class CanvasEditorExtension : Editor
{
    private void OnSceneGUI()
    {
        Canvas canvas = (Canvas)target;

        // 获取 Canvas 的 RectTransform
        RectTransform rectTransform = canvas.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        // 获取 Canvas 的世界位置、宽度、高度和缩放
        Vector3 worldPosition = rectTransform.position;
        Vector3 localScale = rectTransform.localScale;
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // 绘制边框
        Handles.color = Color.green;
        Handles.DrawWireCube(worldPosition, new Vector3(width, height, 0));

        // 绘制文本，显示宽度、高度、位置、缩放
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        // 显示信息
        string info = $"Width: {width}\nHeight: {height}\nPos: {worldPosition}\nScale: {localScale}";
        Handles.Label(worldPosition + new Vector3(0, height / 2 + 10, 0), info, style);

        // 使视图更新
        SceneView.RepaintAll();
    }
}
