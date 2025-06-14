#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

namespace GameLogic.Editor
{
    /// <summary>
    /// 字幕预览工具 - Scene视图中的可视化预览
    /// </summary>
    public class SubtitlePreviewTool : EditorWindow
    {
        private SubtitleSequenceAsset _previewAsset;
        private bool _isPreviewActive = false;
        private bool _showGizmos = true;
        private bool _showEffectBounds = true;
        private bool _showTimeline = true;

        // 预览控制
        private float _previewTime = 0f;
        private float _totalDuration = 0f;
        private bool _isPlaying = false;
        private bool _isPaused = false;

        // 可视化设置
        private Color _gizmoColor = Color.yellow;
        private Color _effectBoundsColor = Color.cyan;
        private float _gizmoSize = 1f;

        // 预览对象
        private GameObject _previewRoot;
        private List<GameObject> _previewCharacters = new List<GameObject>();

        [MenuItem("GameLogic/Subtitle/Subtitle Preview Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<SubtitlePreviewTool>("字幕预览工具");
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            CleanupPreview();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawAssetSelection();
            DrawPreviewControls();
            DrawVisualizationSettings();
            DrawTimelineControls();
            DrawPreviewInfo();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            GUILayout.Label("字幕预览工具", EditorStyles.largeLabel);
            EditorGUILayout.Space();
        }

        private void DrawAssetSelection()
        {
            EditorGUILayout.BeginHorizontal();

            var newAsset = EditorGUILayout.ObjectField("预览资源", _previewAsset, typeof(SubtitleSequenceAsset), false) as SubtitleSequenceAsset;
            if (newAsset != _previewAsset)
            {
                _previewAsset = newAsset;
                if (_isPreviewActive)
                {
                    StopPreview();
                }
                CalculateDuration();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawPreviewControls()
        {
            EditorGUILayout.LabelField("预览控制", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _previewAsset != null && !_isPreviewActive;
            if (GUILayout.Button("开始预览"))
            {
                StartPreview();
            }

            GUI.enabled = _isPreviewActive;
            if (GUILayout.Button(_isPlaying ? "暂停" : "播放"))
            {
                if (_isPlaying)
                {
                    PausePreview();
                }
                else
                {
                    ResumePreview();
                }
            }

            if (GUILayout.Button("停止"))
            {
                StopPreview();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawVisualizationSettings()
        {
            EditorGUILayout.LabelField("可视化设置", EditorStyles.boldLabel);

            _showGizmos = EditorGUILayout.Toggle("显示Gizmos", _showGizmos);
            _showEffectBounds = EditorGUILayout.Toggle("显示效果边界", _showEffectBounds);
            _showTimeline = EditorGUILayout.Toggle("显示时间轴", _showTimeline);

            EditorGUILayout.Space();

            _gizmoColor = EditorGUILayout.ColorField("Gizmo颜色", _gizmoColor);
            _effectBoundsColor = EditorGUILayout.ColorField("效果边界颜色", _effectBoundsColor);
            _gizmoSize = EditorGUILayout.Slider("Gizmo大小", _gizmoSize, 0.1f, 5f);

            EditorGUILayout.Space();
        }

        private void DrawTimelineControls()
        {
            if (!_showTimeline || _previewAsset == null) return;

            EditorGUILayout.LabelField("时间轴控制", EditorStyles.boldLabel);

            // 时间滑块
            float newTime = EditorGUILayout.Slider("时间", _previewTime, 0f, _totalDuration);
            if (Math.Abs(newTime - _previewTime) > 0.01f)
            {
                _previewTime = newTime;
                if (_isPreviewActive)
                {
                    UpdatePreviewAtTime(_previewTime);
                }
            }

            // 时间信息
            EditorGUILayout.LabelField($"当前时间: {_previewTime:F2}s / {_totalDuration:F2}s");

            EditorGUILayout.Space();
        }

        private void DrawPreviewInfo()
        {
            if (_previewAsset == null) return;

            EditorGUILayout.LabelField("预览信息", EditorStyles.boldLabel);

            var config = _previewAsset.Config;
            EditorGUILayout.LabelField($"序列类型: {config.SequenceType}");
            EditorGUILayout.LabelField($"显示模式: {config.DisplayMode}");
            EditorGUILayout.LabelField($"文本长度: {config.Text.Length} 字符");
            EditorGUILayout.LabelField($"效果数量: {config.Effects.Count}");
            EditorGUILayout.LabelField($"预计总时长: {_totalDuration:F2}s");

            if (_isPreviewActive)
            {
                EditorGUILayout.LabelField($"预览状态: {(_isPlaying ? "播放中" : "已暂停")}");
                EditorGUILayout.LabelField($"预览对象数: {_previewCharacters.Count}");
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showGizmos || _previewAsset == null) return;

            var config = _previewAsset.Config;

            // 绘制字幕位置Gizmo
            Handles.color = _gizmoColor;
            Vector3 position = config.Position;

            // 绘制位置标记
            Handles.DrawWireCube(position, Vector3.one * _gizmoSize);
            Handles.Label(position + Vector3.up * _gizmoSize, $"字幕位置\n{config.Text}");

            // 绘制效果范围
            if (_showEffectBounds && config.Effects.Count > 0)
            {
                Handles.color = _effectBoundsColor;

                foreach (var effect in config.Effects)
                {
                    DrawEffectBounds(position, effect);
                }
            }

            // 绘制预览字符
            if (_isPreviewActive)
            {
                DrawPreviewCharacters();
            }
        }

        private void DrawEffectBounds(Vector3 center, SubtitleEffectConfig effect)
        {
            float radius = effect.Duration * 0.5f; // 根据持续时间计算范围

            switch (effect.EffectType)
            {
                case "Scale":
                    Handles.DrawWireDisc(center, Vector3.up, radius);
                    break;
                case "Blur":
                    Handles.DrawWireCube(center, Vector3.one * radius);
                    break;
                case "Fade":
                    // Draw wire sphere using multiple arcs since DrawWireSphere doesn't exist
                    float sphereRadius = radius * 0.5f;
                    Handles.DrawWireArc(center, Vector3.up, Vector3.forward, 360, sphereRadius);
                    Handles.DrawWireArc(center, Vector3.right, Vector3.up, 360, sphereRadius);
                    Handles.DrawWireArc(center, Vector3.forward, Vector3.right, 360, sphereRadius);
                    break;
                default:
                    Handles.DrawWireDisc(center, Vector3.forward, radius * 0.3f);
                    break;
            }

            // 效果标签
            Vector3 labelPos = center + Vector3.right * radius;
            Handles.Label(labelPos, $"{effect.EffectType}\n{effect.Phase}\n{effect.Duration:F1}s");
        }

        private void DrawPreviewCharacters()
        {
            Handles.color = Color.green;

            foreach (var charObj in _previewCharacters)
            {
                if (charObj != null)
                {
                    Vector3 pos = charObj.transform.position;
                    Handles.DrawWireCube(pos, Vector3.one * 0.1f);
                }
            }
        }

        private void StartPreview()
        {
            if (_previewAsset == null) return;

            CleanupPreview();

            _isPreviewActive = true;
            _isPlaying = true;
            _isPaused = false;
            _previewTime = 0f;

            CreatePreviewObjects();

            if (Application.isPlaying)
            {
                StartRuntimePreview();
            }
            else
            {
                StartEditorPreview();
            }
        }

        private void StopPreview()
        {
            _isPreviewActive = false;
            _isPlaying = false;
            _isPaused = false;
            _previewTime = 0f;

            CleanupPreview();
        }

        private void PausePreview()
        {
            _isPlaying = false;
            _isPaused = true;
        }

        private void ResumePreview()
        {
            _isPlaying = true;
            _isPaused = false;
        }

        private void CreatePreviewObjects()
        {
            var config = _previewAsset.Config;

            // 创建根对象
            _previewRoot = new GameObject("SubtitlePreview");
            _previewRoot.transform.position = config.Position;
            _previewRoot.transform.rotation = config.Rotation;
            _previewRoot.transform.localScale = config.Scale;

            // 创建Canvas
            var canvas = _previewRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // canvas.worldCamera = UnityEditor.SceneView.lastActiveSceneView?.camera;
            
            var canvasScaler = _previewRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            
            var graphicRaycaster = _previewRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 创建字符对象（使用TextMeshProUGUI组件）
            _previewCharacters.Clear();

            for (int i = 0; i < config.Text.Length; i++)
            {
                var charObj = new GameObject($"Char_{i}_{config.Text[i]}");
                charObj.transform.SetParent(_previewRoot.transform, false);
                
                var rectTransform = charObj.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(i * 50f, 0);
                rectTransform.sizeDelta = new Vector2(50, 50);

                // 添加TextMeshProUGUI组件显示文字
                var textMeshPro = charObj.AddComponent<TMPro.TextMeshProUGUI>();
                textMeshPro.text = config.Text[i].ToString();
                textMeshPro.fontSize = config.FontSize;
                textMeshPro.color = config.TextColor;
                textMeshPro.alignment = TMPro.TextAlignmentOptions.Center;
                
                // 设置字体（如果有的话）
                if (config.Font != null)
                {
                    textMeshPro.font = config.Font;
                }

                charObj.SetActive(false);
                _previewCharacters.Add(charObj);
            }
        }

        private async void StartRuntimePreview()
        {
            // 运行时预览使用实际的字幕模块
            var subtitleModule = SubtitleModule.Instance;
            if (subtitleModule != null)
            {
                try
                {
                    await subtitleModule.PlaySubtitleSequenceAsync("preview", _previewAsset.Config);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"运行时预览失败: {ex.Message}");
                }
            }
        }

        private void StartEditorPreview()
        {
            // 编辑器预览使用简化的模拟
            EditorApplication.update += UpdateEditorPreview;
        }

        private void UpdateEditorPreview()
        {
            if (!_isPreviewActive || !_isPlaying)
            {
                return;
            }

            _previewTime += Time.deltaTime;

            if (_previewTime >= _totalDuration)
            {
                StopPreview();
                return;
            }

            UpdatePreviewAtTime(_previewTime);

            // 强制重绘Scene视图
            SceneView.RepaintAll();
        }

        private void UpdatePreviewAtTime(float time)
        {
            if (_previewAsset == null) return;

            var config = _previewAsset.Config;

            // 简化的时间轴模拟
            float charTime = config.StartDelay;

            for (int i = 0; i < _previewCharacters.Count; i++)
            {
                var charObj = _previewCharacters[i];
                if (charObj == null) continue;

                bool shouldShow = time >= charTime;
                charObj.SetActive(shouldShow);

                if (shouldShow)
                {
                    // 简单的效果模拟
                    float effectTime = time - charTime;
                    ApplySimpleEffects(charObj, effectTime, config.Effects);
                }

                charTime += config.CharacterInterval;
            }
        }

        private void ApplySimpleEffects(GameObject charObj, float effectTime, List<SubtitleEffectConfig> effects)
        {
            foreach (var effect in effects)
            {
                if (effect.Phase != SubtitleEffectPhase.Enter) continue;

                float startTime = effect.Delay;
                float endTime = startTime + effect.Duration;

                if (effectTime >= startTime && effectTime <= endTime)
                {
                    float t = (effectTime - startTime) / effect.Duration;
                    t = effect.AnimationCurve.Evaluate(t);

                    ApplySimpleEffect(charObj, effect, t);
                }
            }
        }

        private void ApplySimpleEffect(GameObject charObj, SubtitleEffectConfig effect, float t)
        {
            switch (effect)
            {
                case ScaleEffectConfig scale:
                    Vector3 currentScale = Vector3.Lerp(scale.ScaleStart, scale.ScaleEnd, t);
                    charObj.transform.localScale = currentScale * 0.1f; // 基础缩放
                    break;

                case FadeEffectConfig fade:
                    var textMeshPro = charObj.GetComponent<TMPro.TextMeshProUGUI>();
                    if (textMeshPro != null)
                    {
                        float alpha = Mathf.Lerp(fade.AlphaStart, fade.AlphaEnd, t);
                        var color = textMeshPro.color;
                        color.a = alpha;
                        textMeshPro.color = color;
                    }
                    break;
            }
        }

        private void CalculateDuration()
        {
            if (_previewAsset == null)
            {
                _totalDuration = 0f;
                return;
            }

            var config = _previewAsset.Config;

            // 计算基础时长
            float baseDuration = config.StartDelay +
                               (config.Text.Length * config.CharacterInterval) +
                               config.HoldDuration +
                               config.FadeOutDuration;

            // 计算效果时长
            float maxEffectDuration = 0f;
            foreach (var effect in config.Effects)
            {
                float effectEnd = effect.Delay + effect.Duration;
                maxEffectDuration = Mathf.Max(maxEffectDuration, effectEnd);
            }

            _totalDuration = Mathf.Max(baseDuration, maxEffectDuration);
        }

        private void CleanupPreview()
        {
            EditorApplication.update -= UpdateEditorPreview;

            if (_previewRoot != null)
            {
                DestroyImmediate(_previewRoot);
                _previewRoot = null;
            }

            _previewCharacters.Clear();
        }
    }
}
#endif