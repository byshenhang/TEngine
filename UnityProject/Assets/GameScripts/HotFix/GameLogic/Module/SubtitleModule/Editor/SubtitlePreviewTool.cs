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
            if (_previewAsset == null)
            {
                Debug.LogError("[SubtitlePreview] Cannot start preview: _previewAsset is null");
                return;
            }

            Debug.Log($"[SubtitlePreview] 开始预览: {_previewAsset.name}");
            CleanupPreview();

            _isPreviewActive = true;
            _isPlaying = true;
            _isPaused = false;
            _previewTime = 0f;

            CreatePreviewObjects();
            CalculateDuration();
            Debug.Log($"[SubtitlePreview] 总时长: {_totalDuration:F3}秒");
            
            // 立即更新预览状态，确保初始状态正确显示
            UpdatePreviewAtTime(_previewTime);

            if (Application.isPlaying)
            {
                Debug.Log("[SubtitlePreview] 使用运行时预览");
                StartRuntimePreview();
            }
            else
            {
                Debug.Log("[SubtitlePreview] 使用编辑器预览");
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
            Debug.Log($"[SubtitlePreview] 创建预览对象: Text='{config.Text}', FontSize={config.FontSize}");

            // 创建根对象
            _previewRoot = new GameObject("SubtitlePreview");
            _previewRoot.transform.position = config.Position;
            _previewRoot.transform.rotation = config.Rotation;
            _previewRoot.transform.localScale = config.Scale;
            Debug.Log($"[SubtitlePreview] 创建根对象: Position={config.Position}, Scale={config.Scale}");

            // 创建Canvas
            var canvas = _previewRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // canvas.worldCamera = UnityEditor.SceneView.lastActiveSceneView?.camera;
            
            var canvasScaler = _previewRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            
            var graphicRaycaster = _previewRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 创建字符对象（使用TextMeshProUGUI组件）
            _previewCharacters.Clear();
            
            // 根据字体大小动态计算字符间距
            float characterSpacing = Mathf.Max(config.FontSize * 0.8f, 20f);
            float characterSize = Mathf.Max(config.FontSize * 1.2f, 30f);
            Debug.Log($"[SubtitlePreview] 字符设置: spacing={characterSpacing}, size={characterSize}");

            for (int i = 0; i < config.Text.Length; i++)
            {
                var charObj = new GameObject($"Char_{i}_{config.Text[i]}");
                charObj.transform.SetParent(_previewRoot.transform, false);
                
                var rectTransform = charObj.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(i * characterSpacing, 0);
                rectTransform.sizeDelta = new Vector2(characterSize, characterSize);

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
                Debug.Log($"[SubtitlePreview] 创建字符 {i}: '{config.Text[i]}', 位置=({i * characterSpacing}, 0)");
            }
            
            Debug.Log($"[SubtitlePreview] 总共创建了 {_previewCharacters.Count} 个字符对象");
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
            Debug.Log($"[SubtitlePreview] UpdateEditorPreview: _previewTime={_previewTime:F3}, _totalDuration={_totalDuration:F3}");

            if (_previewTime >= _totalDuration)
            {
                Debug.Log($"[SubtitlePreview] 预览结束，停止播放");
                StopPreview();
                return;
            }

            UpdatePreviewAtTime(_previewTime);

            // 强制重绘Scene视图
            SceneView.RepaintAll();
        }

        private void UpdatePreviewAtTime(float time)
        {
            if (_previewAsset == null)
            {
                Debug.LogError("[SubtitlePreview] _previewAsset is null");
                return;
            }

            var config = _previewAsset.Config;
            Debug.Log($"[SubtitlePreview] UpdatePreviewAtTime: time={time:F3}, StartDelay={config.StartDelay:F3}, CharacterInterval={config.CharacterInterval:F3}");

            // 简化的时间轴模拟
            float charTime = config.StartDelay;

            for (int i = 0; i < _previewCharacters.Count; i++)
            {
                var charObj = _previewCharacters[i];
                if (charObj == null)
                {
                    Debug.LogWarning($"[SubtitlePreview] Character {i} is null");
                    continue;
                }

                bool shouldShow = time >= charTime;
                Debug.Log($"[SubtitlePreview] Character {i}: charTime={charTime:F3}, shouldShow={shouldShow}");
                charObj.SetActive(shouldShow);

                if (shouldShow)
                {
                    // 重置字符的初始状态
                    ResetCharacterToDefault(charObj);
                    
                    // 简单的效果模拟
                    float effectTime = time - charTime;
                    //Debug.Log($"[SubtitlePreview] Character {i}: effectTime={effectTime:F3}, effects count={config.Effects?.Count ?? 0}");
                    ApplySimpleEffects(charObj, effectTime, config.Effects);
                }

                charTime += config.CharacterInterval;
            }
        }

        private void ResetCharacterToDefault(GameObject charObj)
        {
            // 重置变换
            charObj.transform.localScale = Vector3.one; // 正常缩放
            
            // 重置文本颜色和透明度
            var textMeshPro = charObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (textMeshPro != null && _previewAsset != null)
            {
                textMeshPro.color = _previewAsset.Config.TextColor;
            }
        }

        private void ApplySimpleEffects(GameObject charObj, float effectTime, List<SubtitleEffectConfig> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                Debug.Log($"[SubtitlePreview] 没有效果需要应用");
                return;
            }
            
            foreach (var effect in effects)
            {
                float startTime = effect.Delay;
                float endTime = startTime + effect.Duration;
                Debug.Log($"[SubtitlePreview] 检查效果 {effect.GetType().Name}: effectTime={effectTime:F3}, startTime={startTime:F3}, endTime={endTime:F3}");

                if (effectTime >= startTime && effectTime <= endTime)
                {
                    float t = (effectTime - startTime) / effect.Duration;
                    if (effect.AnimationCurve != null)
                    {
                        t = effect.AnimationCurve.Evaluate(t);
                    }
                    Debug.Log($"[SubtitlePreview] 应用效果 {effect.GetType().Name}: t={t:F3}");

                    ApplySimpleEffect(charObj, effect, t);
                }
            }
        }

        private void ApplySimpleEffect(GameObject charObj, SubtitleEffectConfig effect, float t)
        {
            Debug.Log($"[SubtitlePreview] ApplySimpleEffect: {effect.GetType().Name}, EffectType={effect.EffectType}, t={t:F3}");
            
            // 由于Unity YAML反序列化的限制，我们需要通过EffectType字符串来判断效果类型
            switch (effect.EffectType)
            {
                case "Scale":
                    if (effect is ScaleEffectConfig scale)
                    {
                        Vector3 currentScale = Vector3.Lerp(scale.ScaleStart, scale.ScaleEnd, t);
                        Debug.Log($"[SubtitlePreview] 缩放效果: {scale.ScaleStart} -> {scale.ScaleEnd}, 当前={currentScale}");
                        charObj.transform.localScale = currentScale;
                    }
                    else
                    {
                        // 从基础配置中获取参数
                        Vector3 scaleStart = effect.GetParameter("ScaleStart", Vector3.zero);
                        Vector3 scaleEnd = effect.GetParameter("ScaleEnd", Vector3.one);
                        Vector3 currentScale = Vector3.Lerp(scaleStart, scaleEnd, t);
                        Debug.Log($"[SubtitlePreview] 缩放效果(通用): {scaleStart} -> {scaleEnd}, 当前={currentScale}");
                        charObj.transform.localScale = currentScale;
                    }
                    break;

                case "Fade":
                    if (effect is FadeEffectConfig fade)
                    {
                        float alpha = Mathf.Lerp(fade.AlphaStart, fade.AlphaEnd, t);
                        Debug.Log($"[SubtitlePreview] 淡入淡出效果: {fade.AlphaStart} -> {fade.AlphaEnd}, 当前alpha={alpha:F3}");
                        var textMeshPro = charObj.GetComponent<TMPro.TextMeshProUGUI>();
                        if (textMeshPro != null)
                        {
                            var color = textMeshPro.color;
                            color.a = alpha;
                            textMeshPro.color = color;
                        }
                    }
                    else
                    {
                        // 从基础配置中获取参数，或使用默认值
                        float alphaStart = effect.GetParameter("AlphaStart", 0f);
                        float alphaEnd = effect.GetParameter("AlphaEnd", 1f);
                        
                        // 根据Phase确定默认的淡入淡出方向
                        if (effect.Phase == SubtitleEffectPhase.Enter)
                        {
                            alphaStart = 0f;
                            alphaEnd = 1f;
                        }
                        else if (effect.Phase == SubtitleEffectPhase.Exit)
                        {
                            alphaStart = 1f;
                            alphaEnd = 0f;
                        }
                        
                        float alpha = Mathf.Lerp(alphaStart, alphaEnd, t);
                        Debug.Log($"[SubtitlePreview] 淡入淡出效果(通用): {alphaStart} -> {alphaEnd}, 当前alpha={alpha:F3}, Phase={effect.Phase}");
                        var textMeshPro = charObj.GetComponent<TMPro.TextMeshProUGUI>();
                        if (textMeshPro != null)
                        {
                            var color = textMeshPro.color;
                            color.a = alpha;
                            textMeshPro.color = color;
                        }
                    }
                    break;

                case "Blur":
                    if (effect is BlurEffectConfig blur)
                    {
                        float blurAmount = Mathf.Lerp(blur.BlurStart, blur.BlurEnd, t);
                        float blurAlpha = Mathf.Lerp(1f, 0.3f, blurAmount / 30f);
                        Debug.Log($"[SubtitlePreview] 模糊效果: {blur.BlurStart} -> {blur.BlurEnd}, 当前模糊={blurAmount:F1}, alpha={blurAlpha:F3}");
                        var blurTextMeshPro = charObj.GetComponent<TMPro.TextMeshProUGUI>();
                        if (blurTextMeshPro != null)
                        {
                            var blurColor = blurTextMeshPro.color;
                            blurColor.a = blurAlpha;
                            blurTextMeshPro.color = blurColor;
                        }
                    }
                    else
                    {
                        // 从基础配置中获取参数
                        float blurStart = effect.GetParameter("BlurStart", 30f);
                        float blurEnd = effect.GetParameter("BlurEnd", 0f);
                        float blurAmount = Mathf.Lerp(blurStart, blurEnd, t);
                        float blurAlpha = Mathf.Lerp(1f, 0.3f, blurAmount / 30f);
                        Debug.Log($"[SubtitlePreview] 模糊效果(通用): {blurStart} -> {blurEnd}, 当前模糊={blurAmount:F1}, alpha={blurAlpha:F3}");
                        var blurTextMeshPro = charObj.GetComponent<TMPro.TextMeshProUGUI>();
                        if (blurTextMeshPro != null)
                        {
                            var blurColor = blurTextMeshPro.color;
                            blurColor.a = blurAlpha;
                            blurTextMeshPro.color = blurColor;
                        }
                    }
                    break;

                case "Typewriter":
                    if (effect is TypewriterEffectConfig typewriter)
                    {
                        Debug.Log($"[SubtitlePreview] 打字机效果: t={t:F3}");
                        var typewriterTextMeshPro = charObj.GetComponent<TMPro.TextMeshProUGUI>();
                        if (typewriterTextMeshPro != null)
                        {
                            var typewriterColor = typewriterTextMeshPro.color;
                            typewriterColor.a = t >= 1f ? 1f : 0f;
                            typewriterTextMeshPro.color = typewriterColor;
                        }
                    }
                    else
                    {
                        Debug.Log($"[SubtitlePreview] 打字机效果(通用): t={t:F3}");
                        var typewriterTextMeshPro = charObj.GetComponent<TMPro.TextMeshProUGUI>();
                        if (typewriterTextMeshPro != null)
                        {
                            var typewriterColor = typewriterTextMeshPro.color;
                            typewriterColor.a = t >= 1f ? 1f : 0f;
                            typewriterTextMeshPro.color = typewriterColor;
                        }
                    }
                    break;
                    
                default:
                    Debug.LogWarning($"[SubtitlePreview] 未知的效果类型: {effect.EffectType} (类型: {effect.GetType().Name})");
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