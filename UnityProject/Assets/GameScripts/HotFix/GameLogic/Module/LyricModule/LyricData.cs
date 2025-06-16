using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 歌词数据容器
    /// </summary>
    [Serializable]
    public class LyricData
    {
        /// <summary>
        /// 歌词行列表
        /// </summary>
        public List<LyricLineData> Lines = new List<LyricLineData>();
        
        /// <summary>
        /// 歌词标题
        /// </summary>
        public string Title = string.Empty;
        
        /// <summary>
        /// 艺术家
        /// </summary>
        public string Artist = string.Empty;
        
        /// <summary>
        /// 专辑
        /// </summary>
        public string Album = string.Empty;
        
        /// <summary>
        /// 偏移时间（毫秒）
        /// </summary>
        public float Offset = 0f;
    }
    
    /// <summary>
    /// 歌词行数据
    /// </summary>
    [Serializable]
    public class LyricLineData
    {
        /// <summary>
        /// 显示时间（秒）
        /// </summary>
        public float Time;
        
        /// <summary>
        /// 歌词文本
        /// </summary>
        public string Text;
        
        /// <summary>
        /// 持续时间（秒），如果为0则使用默认值
        /// </summary>
        public float Duration = 0f;
        
        /// <summary>
        /// 自定义效果配置
        /// </summary>
        public LyricEffectConfig EffectConfig;
    }
    
    /// <summary>
    /// 歌词显示模式
    /// </summary>
    public enum LyricDisplayMode
    {
        /// <summary>
        /// 多行模式 - 每行歌词创建新的GameObject（默认模式）
        /// </summary>
        MultiLine,
        
        /// <summary>
        /// 单行复用模式 - 在同一位置显示歌词，下一行时清除上一行内容
        /// </summary>
        SingleLineReuse
    }
    
    /// <summary>
    /// 歌词配置
    /// </summary>
    [Serializable]
    public class LyricConfig
    {
        /// <summary>
        /// 显示模式
        /// </summary>
        public LyricDisplayMode DisplayMode = LyricDisplayMode.MultiLine;
        
        /// <summary>
        /// 字体大小
        /// </summary>
        public float FontSize = 48f;
        
        /// <summary>
        /// 字符间距
        /// </summary>
        public float CharacterSpacing = 5f;
        
        /// <summary>
        /// 行间距
        /// </summary>
        public float LineSpacing = 100f;
        
        /// <summary>
        /// 默认字体颜色
        /// </summary>
        public Color DefaultColor = Color.white;
        
        /// <summary>
        /// 高亮颜色
        /// </summary>
        public Color HighlightColor = Color.yellow;
        
        /// <summary>
        /// 进入效果配置
        /// </summary>
        public LyricEffectConfig EnterEffect;
        
        /// <summary>
        /// 离开效果配置
        /// </summary>
        public LyricEffectConfig ExitEffect;
        
        /// <summary>
        /// 字符效果配置
        /// </summary>
        public LyricEffectConfig CharacterEffect;
        
        /// <summary>
        /// 行效果配置
        /// </summary>
        public LyricEffectConfig LineEffect;
        
        /// <summary>
        /// 默认配置
        /// </summary>
        public static LyricConfig Default => new LyricConfig
        {
            FontSize = 48,
            CharacterSpacing = 2f,
            LineSpacing = 60f,
            DefaultColor = Color.white,
            HighlightColor = Color.yellow,
            DisplayMode = LyricDisplayMode.MultiLine,
            EnterEffect = LyricEffectConfig.DefaultEnter,
            ExitEffect = LyricEffectConfig.DefaultExit,
            CharacterEffect = LyricEffectConfig.DefaultCharacter,
            LineEffect = LyricEffectConfig.DefaultLine
        };
    }
    
    /// <summary>
    /// 歌词效果配置
    /// </summary>
    [Serializable]
    public class LyricEffectConfig
    {
        /// <summary>
        /// 效果类型
        /// </summary>
        public LyricEffectType EffectType = LyricEffectType.None;
        
        /// <summary>
        /// 效果持续时间
        /// </summary>
        public float Duration = 1f;
        
        /// <summary>
        /// 效果延迟时间
        /// </summary>
        public float Delay = 0f;
        
        /// <summary>
        /// 模糊效果参数
        /// </summary>
        public BlurEffectParams BlurParams = new BlurEffectParams();
        
        /// <summary>
        /// 缩放效果参数
        /// </summary>
        public ScaleEffectParams ScaleParams = new ScaleEffectParams();
        
        /// <summary>
        /// 淡入淡出效果参数
        /// </summary>
        public FadeEffectParams FadeParams = new FadeEffectParams();
        
        /// <summary>
        /// 移动效果参数
        /// </summary>
        public MoveEffectParams MoveParams = new MoveEffectParams();
        
        /// <summary>
        /// 旋转效果参数
        /// </summary>
        public RotateEffectParams RotateParams = new RotateEffectParams();
        
        /// <summary>
        /// 动画曲线
        /// </summary>
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        /// <summary>
        /// 进入效果配置（用于嵌套效果）
        /// </summary>
        public LyricEffectConfig EnterEffect;
        
        /// <summary>
        /// 离开效果配置（用于嵌套效果）
        /// </summary>
        public LyricEffectConfig ExitEffect;
        
        /// <summary>
        /// 字符效果配置（用于嵌套效果）
        /// </summary>
        public LyricEffectConfig CharacterEffect;
        
        /// <summary>
        /// 默认进入效果
        /// </summary>
        public static LyricEffectConfig DefaultEnter => new LyricEffectConfig
        {
            EffectType = LyricEffectType.BlurFade,
            Duration = 1f,
            BlurParams = new BlurEffectParams { StartBlur = 30f, EndBlur = 0f },
            FadeParams = new FadeEffectParams { StartAlpha = 0f, EndAlpha = 1f }
        };
        
        /// <summary>
        /// 默认离开效果
        /// </summary>
        public static LyricEffectConfig DefaultExit => new LyricEffectConfig
        {
            EffectType = LyricEffectType.Fade,
            Duration = 0.5f,
            FadeParams = new FadeEffectParams { StartAlpha = 1f, EndAlpha = 0f }
        };
        
        /// <summary>
        /// 默认字符效果
        /// </summary>
        public static LyricEffectConfig DefaultCharacter => new LyricEffectConfig
        {
            EffectType = LyricEffectType.BlurFade,
            Duration = 1f,
            BlurParams = new BlurEffectParams { StartBlur = 30f, EndBlur = 0f },
            FadeParams = new FadeEffectParams { StartAlpha = 0f, EndAlpha = 1f }
        };
        
        /// <summary>
        /// 默认行效果
        /// </summary>
        public static LyricEffectConfig DefaultLine => new LyricEffectConfig
        {
            EffectType = LyricEffectType.None,
            Duration = 0f
        };
    }
    
    /// <summary>
    /// 歌词效果类型
    /// </summary>
    public enum LyricEffectType
    {
        None,           // 无效果
        Blur,           // 模糊效果
        Fade,           // 淡入淡出
        Scale,          // 缩放效果
        Move,           // 移动效果
        Rotate,         // 旋转效果
        BlurFade,       // 模糊+淡入淡出
        ScaleFade,      // 缩放+淡入淡出
        MoveFade,       // 移动+淡入淡出
        Custom          // 自定义效果
    }
    
    /// <summary>
    /// 模糊效果参数
    /// </summary>
    [Serializable]
    public class BlurEffectParams
    {
        public float StartBlur = 30f;
        public float EndBlur = 0f;
        public float BlurThreshold = 10f;
    }
    
    /// <summary>
    /// 缩放效果参数
    /// </summary>
    [Serializable]
    public class ScaleEffectParams
    {
        public Vector3 StartScale = Vector3.zero;
        public Vector3 EndScale = Vector3.one;
    }
    
    /// <summary>
    /// 淡入淡出效果参数
    /// </summary>
    [Serializable]
    public class FadeEffectParams
    {
        public float StartAlpha = 0f;
        public float EndAlpha = 1f;
    }
    
    /// <summary>
    /// 移动效果参数
    /// </summary>
    [Serializable]
    public class MoveEffectParams
    {
        public Vector3 StartPosition = Vector3.zero;
        public Vector3 EndPosition = Vector3.zero;
        public bool UseRelativePosition = true;
    }
    
    /// <summary>
    /// 旋转效果参数
    /// </summary>
    [Serializable]
    public class RotateEffectParams
    {
        public Vector3 StartRotation = Vector3.zero;
        public Vector3 EndRotation = Vector3.zero;
        public bool UseRelativeRotation = true;
    }
}