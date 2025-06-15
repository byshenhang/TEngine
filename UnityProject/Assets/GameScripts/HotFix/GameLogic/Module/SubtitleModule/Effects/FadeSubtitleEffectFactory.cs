using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 淡入淡出效果工厂
    /// </summary>
    public class FadeSubtitleEffectFactory : ISubtitleEffectFactory
    {
        /// <summary>
        /// 创建淡入淡出效果
        /// </summary>
        /// <param name="config">效果配置</param>
        /// <returns>淡入淡出效果实例</returns>
        public ISubtitleEffect CreateEffect(ISubtitleEffectConfig config)
        {
            if (config is FadeEffectConfig fadeConfig)
            {
                var effect = new FadeSubtitleEffect();
                effect.Initialize(fadeConfig);
                return effect;
            }
            
            Log.Error($"[FadeSubtitleEffectFactory] 配置类型不匹配: {config?.GetType().Name}");
            return null;
        }
    }
}