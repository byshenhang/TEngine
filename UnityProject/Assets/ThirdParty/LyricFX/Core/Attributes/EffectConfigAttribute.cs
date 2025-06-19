using System;

namespace LyricFX.Core.Attributes
{
    /// <summary>
    /// 用于标记效果类和协调器类对应的配置类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class EffectConfigAttribute : Attribute
    {
        /// <summary>
        /// 配置类型
        /// </summary>
        public Type ConfigType { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configType">效果或协调器对应的配置类型</param>
        public EffectConfigAttribute(Type configType)
        {
            ConfigType = configType;
        }
    }
}
