using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 缩放效果工厂
    /// </summary>
    public class ScaleSubtitleEffectFactory : ISubtitleEffectFactory
    {
        /// <summary>
        /// 创建缩放效果
        /// </summary>
        /// <param name="config">效果配置</param>
        /// <returns>缩放效果实例</returns>
        public ISubtitleEffect CreateEffect(ISubtitleEffectConfig config)
        {
            if (config is ScaleEffectConfig scaleConfig)
            {
                var effect = new ScaleSubtitleEffect();
                effect.Initialize(scaleConfig);
                return effect;
            }
            
            Log.Error($"[ScaleSubtitleEffectFactory] 配置类型不匹配: {config?.GetType().Name}");
            return null;
        }
    }
}