using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 模糊效果工厂
    /// </summary>
    public class BlurSubtitleEffectFactory : ISubtitleEffectFactory
    {
        /// <summary>
        /// 创建模糊效果
        /// </summary>
        /// <param name="config">效果配置</param>
        /// <returns>模糊效果实例</returns>
        public ISubtitleEffect CreateEffect(ISubtitleEffectConfig config)
        {
            if (config is BlurEffectConfig blurConfig)
            {
                var effect = new BlurSubtitleEffect();
                effect.Initialize(blurConfig);
                return effect;
            }
            
            Log.Error($"[BlurSubtitleEffectFactory] 配置类型不匹配: {config?.GetType().Name}");
            return null;
        }
    }
}