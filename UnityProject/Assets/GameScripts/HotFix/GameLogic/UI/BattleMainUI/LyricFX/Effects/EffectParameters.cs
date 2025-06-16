using UnityEngine;

namespace LyricFX.Effects
{
    /// <summary>
    /// 效果参数基类，所有效果参数继承自此类
    /// </summary>
    public abstract class EffectParameters
    {
        /// <summary>
        /// 效果持续时间
        /// </summary>
        public float Duration { get; set; } = 1.0f;
        
        /// <summary>
        /// 效果曲线，控制效果的变化过程
        /// </summary>
        public AnimationCurve Curve { get; set; } = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        /// <summary>
        /// 是否自动创建反向效果
        /// </summary>
        public bool AutoReverse { get; set; } = false;
    }

    /// <summary>
    /// 淡入淡出效果参数
    /// </summary>
    public class FadeParameters : EffectParameters
    {
        /// <summary>
        /// 起始透明度
        /// </summary>
        public float StartAlpha { get; set; } = 0.0f;
        
        /// <summary>
        /// 结束透明度
        /// </summary>
        public float EndAlpha { get; set; } = 1.0f;
    }

    /// <summary>
    /// 缩放效果参数
    /// </summary>
    public class ScaleParameters : EffectParameters
    {
        /// <summary>
        /// 起始缩放
        /// </summary>
        public Vector3 StartScale { get; set; } = Vector3.zero;
        
        /// <summary>
        /// 结束缩放
        /// </summary>
        public Vector3 EndScale { get; set; } = Vector3.one;
    }

    /// <summary>
    /// 模糊效果参数
    /// </summary>
    public class BlurParameters : EffectParameters
    {
        /// <summary>
        /// 起始模糊度
        /// </summary>
        public float StartBlur { get; set; } = 30.0f;
        
        /// <summary>
        /// 结束模糊度
        /// </summary>
        public float EndBlur { get; set; } = 0.0f;
        
        /// <summary>
        /// 模糊阈值，用于GroupEffectController的条件等待
        /// </summary>
        public float BlurThreshold { get; set; } = 10.0f;
    }
}
