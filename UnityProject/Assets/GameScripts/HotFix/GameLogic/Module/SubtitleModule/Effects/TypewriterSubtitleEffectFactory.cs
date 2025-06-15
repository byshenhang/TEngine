using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 打字机效果工厂
    /// </summary>
    public class TypewriterSubtitleEffectFactory : ISubtitleEffectFactory
    {
        /// <summary>
        /// 创建打字机效果
        /// </summary>
        /// <param name="config">效果配置</param>
        /// <returns>打字机效果实例</returns>
        public ISubtitleEffect CreateEffect(ISubtitleEffectConfig config)
        {
            if (config is TypewriterEffectConfig typewriterConfig)
            {
                var effect = new TypewriterSubtitleEffect();
                effect.Initialize(typewriterConfig);
                return effect;
            }
            
            Log.Error($"[TypewriterSubtitleEffectFactory] 配置类型不匹配: {config?.GetType().Name}");
            return null;
        }
    }
}