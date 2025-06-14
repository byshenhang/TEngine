using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 字幕序列资源 - ScriptableObject配置文件
    /// </summary>
    [CreateAssetMenu(fileName = "SubtitleSequence", menuName = "GameLogic/Subtitle/Subtitle Sequence", order = 1)]
    public class SubtitleSequenceAsset : ScriptableObject
    {
        [Header("序列信息")]
        [SerializeField] private string _sequenceName = "New Subtitle Sequence";
        [SerializeField] private string _description = "";
        [SerializeField] private string _version = "1.0";
        
        [Header("序列配置")]
        [SerializeField] private SubtitleSequenceConfig _config = new SubtitleSequenceConfig();
        
        [Header("预览设置")]
        [SerializeField] private bool _enablePreview = true;
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private Transform _previewParent;
        
        /// <summary>
        /// 序列名称
        /// </summary>
        public string SequenceName
        {
            get => _sequenceName;
            set => _sequenceName = value;
        }
        
        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get => _description;
            set => _description = value;
        }
        
        /// <summary>
        /// 版本
        /// </summary>
        public string Version
        {
            get => _version;
            set => _version = value;
        }
        
        /// <summary>
        /// 配置
        /// </summary>
        public SubtitleSequenceConfig Config
        {
            get => _config;
            set => _config = value;
        }
        
        /// <summary>
        /// 是否启用预览
        /// </summary>
        public bool EnablePreview
        {
            get => _enablePreview;
            set => _enablePreview = value;
        }
        
        /// <summary>
        /// 预览相机
        /// </summary>
        public Camera PreviewCamera
        {
            get => _previewCamera;
            set => _previewCamera = value;
        }
        
        /// <summary>
        /// 预览父对象
        /// </summary>
        public Transform PreviewParent
        {
            get => _previewParent;
            set => _previewParent = value;
        }
        
        /// <summary>
        /// 创建配置副本
        /// </summary>
        public SubtitleSequenceConfig CreateConfigCopy()
        {
            var copy = new SubtitleSequenceConfig
            {
                SequenceType = _config.SequenceType,
                DisplayMode = _config.DisplayMode,
                Text = _config.Text,
                Position = _config.Position,
                Rotation = _config.Rotation,
                Scale = _config.Scale,
                
                StartDelay = _config.StartDelay,
                CharacterInterval = _config.CharacterInterval,
                WordInterval = _config.WordInterval,
                LineInterval = _config.LineInterval,
                HoldDuration = _config.HoldDuration,
                FadeOutDuration = _config.FadeOutDuration,
                
                Font = _config.Font,
                FontSize = _config.FontSize,
                TextColor = _config.TextColor,
                TextMaterial = _config.TextMaterial,
                PrefabPath = _config.PrefabPath,
                
                Is3D = _config.Is3D,
                AnchorPointName = _config.AnchorPointName,
                TargetCamera = _config.TargetCamera,
                UILayer = _config.UILayer,
                
                Effects = new List<SubtitleEffectConfig>()
            };
            
            // 深拷贝效果配置
            foreach (var effect in _config.Effects)
            {
                copy.Effects.Add(CopyEffectConfig(effect));
            }
            
            return copy;
        }
        
        /// <summary>
        /// 复制效果配置
        /// </summary>
        private SubtitleEffectConfig CopyEffectConfig(SubtitleEffectConfig original)
        {
            switch (original)
            {
                case BlurEffectConfig blur:
                    return new BlurEffectConfig
                    {
                        Duration = blur.Duration,
                        Delay = blur.Delay,
                        Phase = blur.Phase,
                        AnimationCurve = new AnimationCurve(blur.AnimationCurve.keys),
                        BlurStart = blur.BlurStart,
                        BlurEnd = blur.BlurEnd,
                        BlurThreshold = blur.BlurThreshold
                    };
                    
                case FadeEffectConfig fade:
                    return new FadeEffectConfig
                    {
                        Duration = fade.Duration,
                        Delay = fade.Delay,
                        Phase = fade.Phase,
                        AnimationCurve = new AnimationCurve(fade.AnimationCurve.keys),
                        AlphaStart = fade.AlphaStart,
                        AlphaEnd = fade.AlphaEnd,
                        FadeIn = fade.FadeIn
                    };
                    
                case ScaleEffectConfig scale:
                    return new ScaleEffectConfig
                    {
                        Duration = scale.Duration,
                        Delay = scale.Delay,
                        Phase = scale.Phase,
                        AnimationCurve = new AnimationCurve(scale.AnimationCurve.keys),
                        ScaleStart = scale.ScaleStart,
                        ScaleEnd = scale.ScaleEnd,
                        Bounce = scale.Bounce
                    };
                    
                case TypewriterEffectConfig typewriter:
                    return new TypewriterEffectConfig
                    {
                        Duration = typewriter.Duration,
                        Delay = typewriter.Delay,
                        Phase = typewriter.Phase,
                        AnimationCurve = new AnimationCurve(typewriter.AnimationCurve.keys),
                        CharacterSpeed = typewriter.CharacterSpeed,
                        RandomSpeed = typewriter.RandomSpeed,
                        SpeedVariation = typewriter.SpeedVariation
                    };
                    
                default:
                    return new SubtitleEffectConfig
                    {
                        EffectType = original.EffectType,
                        Duration = original.Duration,
                        Delay = original.Delay,
                        Phase = original.Phase,
                        AnimationCurve = new AnimationCurve(original.AnimationCurve.keys),
                        Parameters = new Dictionary<string, object>(original.Parameters)
                    };
            }
        }
        
        /// <summary>
        /// 验证配置
        /// </summary>
        public bool ValidateConfig()
        {
            if (string.IsNullOrEmpty(_config.Text))
            {
                Debug.LogWarning($"[SubtitleSequenceAsset] 字幕文本为空: {name}");
                return false;
            }
            
            if (_config.Duration <= 0)
            {
                Debug.LogWarning($"[SubtitleSequenceAsset] 持续时间无效: {name}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 重置为默认配置
        /// </summary>
        [ContextMenu("重置为默认配置")]
        public void ResetToDefault()
        {
            _config = new SubtitleSequenceConfig();
            _sequenceName = "New Subtitle Sequence";
            _description = "";
            _version = "1.0";
        }
        
        /// <summary>
        /// 添加效果
        /// </summary>
        public void AddEffect(SubtitleEffectConfig effect)
        {
            if (effect != null)
            {
                _config.Effects.Add(effect);
            }
        }
        
        /// <summary>
        /// 移除效果
        /// </summary>
        public void RemoveEffect(int index)
        {
            if (index >= 0 && index < _config.Effects.Count)
            {
                _config.Effects.RemoveAt(index);
            }
        }
        
        /// <summary>
        /// 清空所有效果
        /// </summary>
        public void ClearEffects()
        {
            _config.Effects.Clear();
        }
    }
}