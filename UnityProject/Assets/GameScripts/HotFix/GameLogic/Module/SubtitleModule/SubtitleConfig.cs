using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GameLogic
{
    /// <summary>
    /// 字幕序列类型
    /// </summary>
    public enum SubtitleSequenceType
    {
        /// <summary>
        /// 简单序列 - 基础文本显示
        /// </summary>
        Simple,
        
        /// <summary>
        /// 复杂序列 - 支持多种效果组合
        /// </summary>
        Complex,
        
        /// <summary>
        /// 歌词序列 - 专门用于歌词显示
        /// </summary>
        Lyric
    }
    
    /// <summary>
    /// 字幕显示模式
    /// </summary>
    public enum SubtitleDisplayMode
    {
        /// <summary>
        /// 逐字显示
        /// </summary>
        CharacterByCharacter,
        
        /// <summary>
        /// 逐字显示（简写）
        /// </summary>
        Character = CharacterByCharacter,
        
        /// <summary>
        /// 逐词显示
        /// </summary>
        WordByWord,
        
        /// <summary>
        /// 逐行显示
        /// </summary>
        LineByLine,
        
        /// <summary>
        /// 整体显示
        /// </summary>
        All
    }
    
    /// <summary>
    /// 字幕效果阶段
    /// </summary>
    public enum SubtitleEffectPhase
    {
        /// <summary>
        /// 进场阶段 - 字幕出现时的效果
        /// </summary>
        Enter,
        
        /// <summary>
        /// 停留阶段 - 字幕显示期间的效果
        /// </summary>
        Stay,
        
        /// <summary>
        /// 离场阶段 - 字幕消失时的效果
        /// </summary>
        Exit
    }
    
    /// <summary>
    /// 字幕序列配置
    /// </summary>
    [System.Serializable]
    public class SubtitleSequenceConfig
    {
        [Header("基础设置")]
        public SubtitleSequenceType SequenceType = SubtitleSequenceType.Simple;
        public SubtitleDisplayMode DisplayMode = SubtitleDisplayMode.CharacterByCharacter;
        
        [Header("文本内容")]
        [TextArea(3, 10)]
        public string Text = "示例字幕文本";
        
        [Header("位置和变换")]
        public Vector3 Position = Vector3.zero;
        public Quaternion Rotation = Quaternion.identity;
        public Vector3 Scale = Vector3.one;
        
        [Header("时间设置")]
        public float StartDelay = 0f;
        public float CharacterInterval = 0.05f;
        public float WordInterval = 0.1f;
        public float LineInterval = 0.2f;
        public float HoldDuration = 2f;
        public float FadeOutDuration = 1f;
        public float Duration = 5f; // 总持续时间
        
        [Header("字体和样式")]
        public TMP_FontAsset Font;
        public int FontSize = 24;
        public Color TextColor = Color.white;
        public Material TextMaterial;
        public string FontPath = ""; // 字体路径
        
        [Header("预制体路径")]
        public string PrefabPath = "";
        
        [Header("效果配置")]
        public List<SubtitleEffectConfig> Effects = new List<SubtitleEffectConfig>();
        
        [Header("3D设置")]
        public bool Is3D = false;
        public string AnchorPointName = "";
        public Camera TargetCamera;
        public int UILayer = 5;
        
        [Header("父级变换")]
        public Transform ParentTransform;
        
        /// <summary>
        /// HoldDuration 的别名，用于兼容性
        /// </summary>
        public float HoldTime
        {
            get => HoldDuration;
            set => HoldDuration = value;
        }
    }
    
    /// <summary>
    /// 字幕效果配置
    /// </summary>
    [Serializable]
    public class SubtitleEffectConfig : ISubtitleEffectConfig
    {
        [Header("基础配置")]
        public string EffectType = "";
        public float Duration = 1f;
        public float Delay = 0f;
        public GameObject Target { get; set; }
        
        // 显式实现接口属性
        float ISubtitleEffectConfig.Duration => Duration;
        float ISubtitleEffectConfig.Delay => Delay;
        GameObject ISubtitleEffectConfig.Target => Target;
        
        [Header("效果阶段")]
        public SubtitleEffectPhase Phase = SubtitleEffectPhase.Enter;
        
        [Header("动画曲线")]
        public AnimationCurve AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("效果参数")]
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();
        
        /// <summary>
        /// 设置参数
        /// </summary>
        public void SetParameter<T>(string key, T value)
        {
            Parameters[key] = value;
        }
        
        /// <summary>
        /// 获取参数
        /// </summary>
        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
    }
    
    /// <summary>
    /// 模糊效果配置
    /// </summary>
    [Serializable]
    public class BlurEffectConfig : SubtitleEffectConfig
    {
        [Header("模糊配置")]
        public float BlurStart = 30f;
        public float BlurEnd = 0f;
        public float BlurThreshold = 10f;
        
        /// <summary>
        /// 起始模糊半径（BlurStart的别名）
        /// </summary>
        public float StartBlurRadius
        {
            get => BlurStart;
            set => BlurStart = value;
        }
        
        /// <summary>
        /// 结束模糊半径（BlurEnd的别名）
        /// </summary>
        public float EndBlurRadius
        {
            get => BlurEnd;
            set => BlurEnd = value;
        }
        
        public BlurEffectConfig()
        {
            EffectType = "Blur";
            SetParameter("BlurStart", BlurStart);
            SetParameter("BlurEnd", BlurEnd);
            SetParameter("BlurThreshold", BlurThreshold);
        }
    }
    
    /// <summary>
    /// 淡入淡出效果配置
    /// </summary>
    [Serializable]
    public class FadeEffectConfig : SubtitleEffectConfig
    {
        [Header("淡入淡出配置")]
        public float AlphaStart = 0f;
        public float AlphaEnd = 1f;
        public bool FadeIn = true;
        
        /// <summary>
        /// 起始透明度（AlphaStart的别名）
        /// </summary>
        public float StartAlpha
        {
            get => AlphaStart;
            set => AlphaStart = value;
        }
        
        /// <summary>
        /// 结束透明度（AlphaEnd的别名）
        /// </summary>
        public float EndAlpha
        {
            get => AlphaEnd;
            set => AlphaEnd = value;
        }
        
        public FadeEffectConfig()
        {
            EffectType = "Fade";
            SetParameter("AlphaStart", AlphaStart);
            SetParameter("AlphaEnd", AlphaEnd);
            SetParameter("FadeIn", FadeIn);
        }
    }
    
    /// <summary>
    /// 打字机效果配置
    /// </summary>
    [Serializable]
    public class TypewriterEffectConfig : SubtitleEffectConfig
    {
        [Header("打字机配置")]
        public float CharacterSpeed = 0.05f;
        public bool RandomSpeed = false;
        public float SpeedVariation = 0.02f;
        
        public TypewriterEffectConfig()
        {
            EffectType = "Typewriter";
            SetParameter("CharacterSpeed", CharacterSpeed);
            SetParameter("RandomSpeed", RandomSpeed);
            SetParameter("SpeedVariation", SpeedVariation);
        }
    }
    
    /// <summary>
    /// 缩放效果配置
    /// </summary>
    [Serializable]
    public class ScaleEffectConfig : SubtitleEffectConfig
    {
        [Header("缩放配置")]
        public Vector3 ScaleStart = Vector3.zero;
        public Vector3 ScaleEnd = Vector3.one;
        public bool Bounce = false;
        
        /// <summary>
        /// 起始缩放（ScaleStart的别名）
        /// </summary>
        public Vector3 StartScale
        {
            get => ScaleStart;
            set => ScaleStart = value;
        }
        
        /// <summary>
        /// 结束缩放（ScaleEnd的别名）
        /// </summary>
        public Vector3 EndScale
        {
            get => ScaleEnd;
            set => ScaleEnd = value;
        }
        
        public ScaleEffectConfig()
        {
            EffectType = "Scale";
            SetParameter("ScaleStart", ScaleStart);
            SetParameter("ScaleEnd", ScaleEnd);
            SetParameter("Bounce", Bounce);
        }
    }
}