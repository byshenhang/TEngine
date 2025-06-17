using System;

namespace LyricFX.Core
{
    /// <summary>
    /// 效果作用域枚举
    /// </summary>
    public enum EffectScope
    {
        /// <summary>
        /// 字符级效果 - 每个字符独立的效果
        /// </summary>
        Character,
        
        /// <summary>
        /// 行级效果 - 需要整行协调的效果
        /// </summary>
        Line,
        
        /// <summary>
        /// 全局效果 - 影响多行或全局的效果
        /// </summary>
        Global
    }
    
    /// <summary>
    /// 效果元数据
    /// 描述效果的基本信息和类型
    /// </summary>
    public class EffectMetadata
    {
        /// <summary>
        /// 效果ID
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// 效果作用域
        /// </summary>
        public EffectScope Scope { get; set; }
        
        /// <summary>
        /// 效果实现类型
        /// </summary>
        public Type EffectType { get; set; }
        
        /// <summary>
        /// 协调器类型（仅对行级和全局效果有效）
        /// </summary>
        public Type CoordinatorType { get; set; }
        
        /// <summary>
        /// 效果描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 是否需要协调器
        /// </summary>
        public bool RequiresCoordinator => Scope != EffectScope.Character;
    }
}