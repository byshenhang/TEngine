#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
using TMPro;

namespace GameLogic.Editor
{
    /// <summary>
    /// 字幕序列编辑器窗口
    /// </summary>
    public class SubtitleSequenceEditorWindow : EditorWindow
    {
        private SubtitleSequenceAsset _currentAsset;
        private SubtitleSequenceConfig _tempConfig;
        private Vector2 _scrollPosition;
        private Vector2 _effectScrollPosition;
        private bool _showBasicSettings = true;
        private bool _showTimingSettings = true;
        private bool _showVisualSettings = true;
        private bool _showEffectSettings = true;
        private bool _show3DSettings = false;
        private bool _showPreviewSettings = true;
        
        // 预览相关
        private bool _isPreviewActive = false;
        private GameObject _previewObject;
        private SubtitleModule _subtitleModule;
        private string _previewSequenceId = "editor_preview";
        
        // 效果编辑
        private int _selectedEffectIndex = -1;
        private string[] _effectTypeOptions = { "Blur", "Fade", "Scale", "Typewriter" };
        private string[] _phaseOptions = { "Enter", "Stay", "Exit" };
        
        [MenuItem("GameLogic/Subtitle/Subtitle Sequence Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SubtitleSequenceEditorWindow>("字幕序列编辑器");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeSubtitleModule();
        }
        
        private void OnDisable()
        {
            StopPreview();
        }
        
        private void InitializeSubtitleModule()
        {
                // Editor模式下手动创建SubtitleModule实例
                _subtitleModule = SubtitleModule.Instance;
        }
        
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawHeader();
            DrawAssetSelection();
            
            if (_currentAsset != null)
            {
                DrawConfigurationSections();
                DrawPreviewSection();
                DrawActionButtons();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space();
            GUILayout.Label("字幕序列编辑器", EditorStyles.largeLabel);
            EditorGUILayout.Space();
        }
        
        private void DrawAssetSelection()
        {
            EditorGUILayout.BeginHorizontal();
            
            var newAsset = EditorGUILayout.ObjectField("字幕序列资源", _currentAsset, typeof(SubtitleSequenceAsset), false) as SubtitleSequenceAsset;
            if (newAsset != _currentAsset)
            {
                LoadAsset(newAsset);
            }
            
            if (GUILayout.Button("新建", GUILayout.Width(60)))
            {
                CreateNewAsset();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
        
        private void DrawConfigurationSections()
        {
            if (_tempConfig == null) return;
            
            // 基础设置
            _showBasicSettings = EditorGUILayout.Foldout(_showBasicSettings, "基础设置", true);
            if (_showBasicSettings)
            {
                EditorGUI.indentLevel++;
                DrawBasicSettings();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 时序设置
            _showTimingSettings = EditorGUILayout.Foldout(_showTimingSettings, "时序设置", true);
            if (_showTimingSettings)
            {
                EditorGUI.indentLevel++;
                DrawTimingSettings();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 视觉设置
            _showVisualSettings = EditorGUILayout.Foldout(_showVisualSettings, "视觉设置", true);
            if (_showVisualSettings)
            {
                EditorGUI.indentLevel++;
                DrawVisualSettings();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 3D设置
            _show3DSettings = EditorGUILayout.Foldout(_show3DSettings, "3D设置", true);
            if (_show3DSettings)
            {
                EditorGUI.indentLevel++;
                Draw3DSettings();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 效果设置
            _showEffectSettings = EditorGUILayout.Foldout(_showEffectSettings, "效果设置", true);
            if (_showEffectSettings)
            {
                EditorGUI.indentLevel++;
                DrawEffectSettings();
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawBasicSettings()
        {
            _currentAsset.SequenceName = EditorGUILayout.TextField("序列名称", _currentAsset.SequenceName);
            _currentAsset.Description = EditorGUILayout.TextField("描述", _currentAsset.Description);
            _currentAsset.Version = EditorGUILayout.TextField("版本", _currentAsset.Version);
            
            EditorGUILayout.Space();
            
            _tempConfig.SequenceType = (SubtitleSequenceType)EditorGUILayout.EnumPopup("序列类型", _tempConfig.SequenceType);
            _tempConfig.DisplayMode = (SubtitleDisplayMode)EditorGUILayout.EnumPopup("显示模式", _tempConfig.DisplayMode);
            _tempConfig.Text = EditorGUILayout.TextArea(_tempConfig.Text, GUILayout.Height(60));
            
            EditorGUILayout.Space();
            
            _tempConfig.Position = EditorGUILayout.Vector3Field("位置", _tempConfig.Position);
            _tempConfig.Rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("旋转", _tempConfig.Rotation.eulerAngles));
            _tempConfig.Scale = EditorGUILayout.Vector3Field("缩放", _tempConfig.Scale);
        }
        
        private void DrawTimingSettings()
        {
            _tempConfig.StartDelay = EditorGUILayout.FloatField("开始延迟", _tempConfig.StartDelay);
            _tempConfig.CharacterInterval = EditorGUILayout.FloatField("字符间隔", _tempConfig.CharacterInterval);
            _tempConfig.WordInterval = EditorGUILayout.FloatField("单词间隔", _tempConfig.WordInterval);
            _tempConfig.LineInterval = EditorGUILayout.FloatField("行间隔", _tempConfig.LineInterval);
            _tempConfig.HoldDuration = EditorGUILayout.FloatField("保持时间", _tempConfig.HoldDuration);
            _tempConfig.FadeOutDuration = EditorGUILayout.FloatField("淡出时间", _tempConfig.FadeOutDuration);
        }
        
        private void DrawVisualSettings()
        {
            _tempConfig.Font = EditorGUILayout.ObjectField("字体", _tempConfig.Font, typeof(TMP_FontAsset), false) as TMP_FontAsset;
            _tempConfig.FontSize = EditorGUILayout.IntField("字体大小", _tempConfig.FontSize);
            _tempConfig.TextColor = EditorGUILayout.ColorField("文字颜色", _tempConfig.TextColor);
            _tempConfig.TextMaterial = EditorGUILayout.ObjectField("文字材质", _tempConfig.TextMaterial, typeof(Material), false) as Material;
            _tempConfig.PrefabPath = EditorGUILayout.TextField("预制体路径", _tempConfig.PrefabPath);
        }
        
        private void Draw3DSettings()
        {
            _tempConfig.Is3D = EditorGUILayout.Toggle("启用3D", _tempConfig.Is3D);
            
            if (_tempConfig.Is3D)
            {
                _tempConfig.AnchorPointName = EditorGUILayout.TextField("锚点名称", _tempConfig.AnchorPointName);
                _tempConfig.TargetCamera = EditorGUILayout.ObjectField("目标相机", _tempConfig.TargetCamera, typeof(Camera), true) as Camera;
                _tempConfig.UILayer = EditorGUILayout.LayerField("UI层级", _tempConfig.UILayer);
            }
        }
        
        private void DrawEffectSettings()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("效果列表", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                AddNewEffect();
            }
            
            EditorGUILayout.EndHorizontal();
            
            _effectScrollPosition = EditorGUILayout.BeginScrollView(_effectScrollPosition, GUILayout.Height(200));
            
            for (int i = 0; i < _tempConfig.Effects.Count; i++)
            {
                DrawEffectItem(i);
            }
            
            EditorGUILayout.EndScrollView();
            
            if (_selectedEffectIndex >= 0 && _selectedEffectIndex < _tempConfig.Effects.Count)
            {
                EditorGUILayout.Space();
                DrawEffectDetails(_selectedEffectIndex);
            }
        }
        
        private void DrawEffectItem(int index)
        {
            var effect = _tempConfig.Effects[index];
            
            EditorGUILayout.BeginHorizontal("box");
            
            bool isSelected = _selectedEffectIndex == index;
            if (GUILayout.Toggle(isSelected, "", GUILayout.Width(20)) != isSelected)
            {
                _selectedEffectIndex = isSelected ? -1 : index;
            }
            
            GUILayout.Label($"{effect.EffectType} ({effect.Phase})", EditorStyles.label);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                _tempConfig.Effects.RemoveAt(index);
                if (_selectedEffectIndex == index)
                {
                    _selectedEffectIndex = -1;
                }
                else if (_selectedEffectIndex > index)
                {
                    _selectedEffectIndex--;
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawEffectDetails(int index)
        {
            var effect = _tempConfig.Effects[index];
            
            EditorGUILayout.LabelField("效果详细设置", EditorStyles.boldLabel);
            
            // 基础设置
            int typeIndex = Array.IndexOf(_effectTypeOptions, effect.EffectType);
            typeIndex = EditorGUILayout.Popup("效果类型", typeIndex, _effectTypeOptions);
            if (typeIndex >= 0)
            {
                string newType = _effectTypeOptions[typeIndex];
                if (newType != effect.EffectType)
                {
                    // 更换效果类型
                    _tempConfig.Effects[index] = CreateEffectByType(newType);
                    effect = _tempConfig.Effects[index];
                }
            }
            
            int phaseIndex = (int)effect.Phase;
            phaseIndex = EditorGUILayout.Popup("效果阶段", phaseIndex, _phaseOptions);
            effect.Phase = (SubtitleEffectPhase)phaseIndex;
            
            effect.Duration = EditorGUILayout.FloatField("持续时间", effect.Duration);
            effect.Delay = EditorGUILayout.FloatField("延迟", effect.Delay);
            effect.AnimationCurve = EditorGUILayout.CurveField("动画曲线", effect.AnimationCurve);
            
            EditorGUILayout.Space();
            
            // 特定效果设置
            DrawSpecificEffectSettings(effect);
        }
        
        private void DrawSpecificEffectSettings(SubtitleEffectConfig effect)
        {
            switch (effect)
            {
                case BlurEffectConfig blur:
                    blur.BlurStart = EditorGUILayout.FloatField("起始模糊", blur.BlurStart);
                    blur.BlurEnd = EditorGUILayout.FloatField("结束模糊", blur.BlurEnd);
                    blur.BlurThreshold = EditorGUILayout.FloatField("模糊阈值", blur.BlurThreshold);
                    break;
                    
                case FadeEffectConfig fade:
                    fade.AlphaStart = EditorGUILayout.Slider("起始透明度", fade.AlphaStart, 0f, 1f);
                    fade.AlphaEnd = EditorGUILayout.Slider("结束透明度", fade.AlphaEnd, 0f, 1f);
                    fade.FadeIn = EditorGUILayout.Toggle("淡入", fade.FadeIn);
                    break;
                    
                case ScaleEffectConfig scale:
                    scale.ScaleStart = EditorGUILayout.Vector3Field("起始缩放", scale.ScaleStart);
                    scale.ScaleEnd = EditorGUILayout.Vector3Field("结束缩放", scale.ScaleEnd);
                    scale.Bounce = EditorGUILayout.Toggle("弹跳", scale.Bounce);
                    break;
                    
                case TypewriterEffectConfig typewriter:
                    typewriter.CharacterSpeed = EditorGUILayout.FloatField("字符速度", typewriter.CharacterSpeed);
                    typewriter.RandomSpeed = EditorGUILayout.Toggle("随机速度", typewriter.RandomSpeed);
                    if (typewriter.RandomSpeed)
                    {
                        typewriter.SpeedVariation = EditorGUILayout.FloatField("速度变化", typewriter.SpeedVariation);
                    }
                    break;
            }
        }
        
        private void DrawPreviewSection()
        {
            EditorGUILayout.Space();
            _showPreviewSettings = EditorGUILayout.Foldout(_showPreviewSettings, "预览设置", true);
            
            if (_showPreviewSettings)
            {
                EditorGUI.indentLevel++;
                
                _currentAsset.EnablePreview = EditorGUILayout.Toggle("启用预览", _currentAsset.EnablePreview);
                
                if (_currentAsset.EnablePreview)
                {
                    _currentAsset.PreviewCamera = EditorGUILayout.ObjectField("预览相机", _currentAsset.PreviewCamera, typeof(Camera), true) as Camera;
                    _currentAsset.PreviewParent = EditorGUILayout.ObjectField("预览父对象", _currentAsset.PreviewParent, typeof(Transform), true) as Transform;
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    GUI.enabled = Application.isPlaying && !_isPreviewActive;
                    if (GUILayout.Button("开始预览"))
                    {
                        StartPreview();
                    }
                    
                    GUI.enabled = Application.isPlaying && _isPreviewActive;
                    if (GUILayout.Button("停止预览"))
                    {
                        StopPreview();
                    }
                    
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    
                    if (!Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("预览功能需要在播放模式下使用", MessageType.Info);
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("保存"))
            {
                SaveAsset();
            }
            
            if (GUILayout.Button("重置"))
            {
                LoadAsset(_currentAsset);
            }
            
            if (GUILayout.Button("验证配置"))
            {
                ValidateConfiguration();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void LoadAsset(SubtitleSequenceAsset asset)
        {
            StopPreview();
            _currentAsset = asset;
            
            if (_currentAsset != null)
            {
                _tempConfig = _currentAsset.CreateConfigCopy();
            }
            else
            {
                _tempConfig = null;
            }
            
            _selectedEffectIndex = -1;
        }
        
        private void CreateNewAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "创建字幕序列资源",
                "NewSubtitleSequence",
                "asset",
                "选择保存位置");
                
            if (!string.IsNullOrEmpty(path))
            {
                var asset = CreateInstance<SubtitleSequenceAsset>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                LoadAsset(asset);
                Selection.activeObject = asset;
            }
        }
        
        private void SaveAsset()
        {
            if (_currentAsset != null && _tempConfig != null)
            {
                _currentAsset.Config = _tempConfig;
                EditorUtility.SetDirty(_currentAsset);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"字幕序列已保存: {_currentAsset.name}");
            }
        }
        
        private void ValidateConfiguration()
        {
            if (_currentAsset != null)
            {
                bool isValid = _currentAsset.ValidateConfig();
                string message = isValid ? "配置验证通过" : "配置验证失败，请检查设置";
                EditorUtility.DisplayDialog("配置验证", message, "确定");
            }
        }
        
        private void AddNewEffect()
        {
            if (_tempConfig != null)
            {
                var newEffect = new FadeEffectConfig();
                _tempConfig.Effects.Add(newEffect);
                _selectedEffectIndex = _tempConfig.Effects.Count - 1;
            }
        }
        
        private SubtitleEffectConfig CreateEffectByType(string effectType)
        {
            return effectType switch
            {
                "Blur" => new BlurEffectConfig(),
                "Fade" => new FadeEffectConfig(),
                "Scale" => new ScaleEffectConfig(),
                "Typewriter" => new TypewriterEffectConfig(),
                _ => new SubtitleEffectConfig { EffectType = effectType }
            };
        }
        
        private async void StartPreview()
        {
            if (!Application.isPlaying || _subtitleModule == null || _tempConfig == null)
            {
                return;
            }
            
            try
            {
                _isPreviewActive = true;
                
                // 设置预览配置
                var previewConfig = _currentAsset.CreateConfigCopy();
                if (_currentAsset.PreviewParent != null)
                {
                    previewConfig.ParentTransform = _currentAsset.PreviewParent;
                }
                
                // 播放预览
                await _subtitleModule.PlaySubtitleSequenceAsync(_previewSequenceId, previewConfig);
                
                _isPreviewActive = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"预览失败: {ex.Message}");
                _isPreviewActive = false;
            }
        }
        
        private void StopPreview()
        {
            if (_subtitleModule != null && _isPreviewActive)
            {
                _subtitleModule.StopSubtitleSequence(_previewSequenceId);
                _isPreviewActive = false;
            }
        }
    }
}
#endif