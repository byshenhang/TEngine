#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MikeNspired.VRConsoleLog.Editor
{
    [CustomEditor(typeof(VRConsoleLogger))]
    public class VRConsoleLoggerEditor : UnityEditor.Editor
    {
        private SerializedProperty logMessagePrefabProperty;
        private SerializedProperty logMessageParentProperty;
        private SerializedProperty verticalLayoutGroupProperty;
        private SerializedProperty scrollRectProperty;
        private SerializedProperty canvasGroupProperty;
        private SerializedProperty canvasProperty;
        private SerializedProperty keyWordFilterProperty;
        private SerializedProperty keyWordsProperty;
        private SerializedProperty keyWordsIgnoreProperty;
        private SerializedProperty timeTypeProperty;
        private SerializedProperty fontSizeProperty;
        private SerializedProperty logColorProperty;
        private SerializedProperty warningColorProperty;
        private SerializedProperty errorColorProperty;
        private SerializedProperty backgroundColor1Property;
        private SerializedProperty backgroundColor2Property;
        private SerializedProperty logStackTraceProperty;
        private SerializedProperty showLogProperty;
        private SerializedProperty showWarningProperty;
        private SerializedProperty showErrorProperty;
        private SerializedProperty reverseArrangementProperty;
        private SerializedProperty maxMessageCountProperty;
        private SerializedProperty autoScrollAtDistancePercentageProperty;
        private SerializedProperty startDisabledProperty;

        private void OnEnable()
        {
            logMessagePrefabProperty = serializedObject.FindProperty("logMessagePrefab");
            logMessageParentProperty = serializedObject.FindProperty("logMessageParent");
            verticalLayoutGroupProperty = serializedObject.FindProperty("verticalLayoutGroup");
            scrollRectProperty = serializedObject.FindProperty("scrollRect");
            canvasGroupProperty = serializedObject.FindProperty("canvasGroup");
            canvasProperty = serializedObject.FindProperty("canvas");
            keyWordFilterProperty = serializedObject.FindProperty("keyWordFilter");
            keyWordsProperty = serializedObject.FindProperty("keyWords");
            keyWordsIgnoreProperty = serializedObject.FindProperty("keyWordsIgnore");
            timeTypeProperty = serializedObject.FindProperty("timeType");
            maxMessageCountProperty = serializedObject.FindProperty("maxMessageCount");
            fontSizeProperty = serializedObject.FindProperty("fontSize");
            autoScrollAtDistancePercentageProperty = serializedObject.FindProperty("autoScrollAtDistancePercentage");
            startDisabledProperty = serializedObject.FindProperty("startDisabled");
            logColorProperty = serializedObject.FindProperty("logColor");
            warningColorProperty = serializedObject.FindProperty("warningColor");
            errorColorProperty = serializedObject.FindProperty("errorColor");
            backgroundColor1Property = serializedObject.FindProperty("backgroundColor1");
            backgroundColor2Property = serializedObject.FindProperty("backgroundColor2");
            logStackTraceProperty = serializedObject.FindProperty("logStackTrace");
            showLogProperty = serializedObject.FindProperty("showLog");
            showWarningProperty = serializedObject.FindProperty("showWarning");
            showErrorProperty = serializedObject.FindProperty("showError");
            reverseArrangementProperty = serializedObject.FindProperty("reverseArrangement");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoBehaviour), false);
            EditorGUI.EndDisabledGroup();

            // Using the revised method to add tooltips properly
            EditorGUILayout.PropertyField(logMessagePrefabProperty, new GUIContent("Log Message Prefab", "Prefab for individual log messages."));
            EditorGUILayout.PropertyField(logMessageParentProperty, new GUIContent("Log Message Parent", "Parent transform under which log messages will be instantiated."));
            EditorGUILayout.PropertyField(verticalLayoutGroupProperty, new GUIContent("Vertical Layout Group", "Vertical layout group that organizes the log messages."));
            EditorGUILayout.PropertyField(scrollRectProperty, new GUIContent("Scroll Rect", "ScrollRect component to enable scrolling through the log messages."));
            EditorGUILayout.PropertyField(canvasGroupProperty, new GUIContent("Canvas Group", "CanvasGroup to control the visibility and interactivity of the entire log panel."));
            EditorGUILayout.PropertyField(canvasProperty, new GUIContent("Canvas", "Canvas where the logging UI is rendered."));
            
            // Additional properties with conditional display for keyWords
            EditorGUILayout.PropertyField(keyWordFilterProperty, new GUIContent("Keyword Filter", "Toggle to enable keyword filtering of log messages."));
            if (keyWordFilterProperty.boolValue)
            {
                EditorGUILayout.PropertyField(keyWordsProperty, new GUIContent("Keywords", "Only log messages with these words."), true);
                EditorGUILayout.PropertyField(keyWordsIgnoreProperty, new GUIContent("Keywords To Ignore", "Ignore any messages containing these words."), true);

            }
            
            EditorGUILayout.PropertyField(startDisabledProperty, new GUIContent("Start Disabled", "Start with the console disabled."));
            CheckAndApplyPropertyChange(timeTypeProperty, "UpdateMessages", "Time Type", "Defines the format of the timestamp shown with each log message.");
            CheckAndApplyPropertyChange(fontSizeProperty, "OnFontSizeChanged", "Font Size", "Font size for the log messages.");
            CheckAndApplyPropertyChange(maxMessageCountProperty, "AdjustPoolSizeProxy", "Max Message Count", "Maximum number of messages displayed in the console.", true);
            EditorGUILayout.PropertyField(autoScrollAtDistancePercentageProperty, new GUIContent("Auto Scroll %", "Percentage of the viewport's height from the bottom/top at which auto-scrolling is triggered."));
            CheckAndApplyPropertyChange(logColorProperty, "UpdateMessageDisplay", "Log Color", "Color used for log level messages.");
            CheckAndApplyPropertyChange(warningColorProperty, "UpdateMessageDisplay", "Warning Color", "Color used for warning level messages.");
            CheckAndApplyPropertyChange(errorColorProperty, "UpdateMessageDisplay", "Error Color", "Color used for error level messages.");
            CheckAndApplyPropertyChange(backgroundColor1Property, "UpdateMessageDisplay", "Background Color 1", "Background color for even-indexed messages.");
            CheckAndApplyPropertyChange(backgroundColor2Property, "UpdateMessageDisplay", "Background Color 2", "Background color for odd-indexed messages.");
            CheckAndApplyPropertyChange(logStackTraceProperty, "UpdateMessages", "Log Stack Trace", "Whether to log stack trace information with errors.");
            CheckAndApplyPropertyChange(showLogProperty, "UpdateLogVisibility", "Show Log", "Shows or hides normal log messages.");
            CheckAndApplyPropertyChange(showWarningProperty, "UpdateLogVisibility", "Show Warning", "Shows or hides warning messages.");
            CheckAndApplyPropertyChange(showErrorProperty, "UpdateLogVisibility", "Show Error", "Shows or hides error messages.");
            CheckAndApplyPropertyChange(reverseArrangementProperty, "OnReverseArrangementChanged", "Reverse Arrangement", "Whether the new messages appear at the top or bottom of the list.");

            serializedObject.ApplyModifiedProperties();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void CheckAndApplyPropertyChange(SerializedProperty property, string methodName, string label, string tooltip, bool onlyApplyInPlayMode = false)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                // Invoke the method only if we are not in play mode or if onlyApplyInPlayMode is false
                if (!onlyApplyInPlayMode || (onlyApplyInPlayMode && EditorApplication.isPlaying))
                {
                    InvokeMethodOnTarget(methodName);
                }
            }
        }

        private void InvokeMethodOnTarget(string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (method != null)
                method.Invoke(target, null);
            else
                Debug.LogWarning($"Method '{methodName}' not found on {target.GetType()}.");
        }
    }
}
#endif
