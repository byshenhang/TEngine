using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 字幕效果工厂接口
    /// </summary>
    public interface ISubtitleEffectFactory
    {
        /// <summary>
        /// 创建字幕效果
        /// </summary>
        /// <param name="config">效果配置</param>
        /// <returns>字幕效果实例</returns>
        ISubtitleEffect CreateEffect(ISubtitleEffectConfig config);
    }
}