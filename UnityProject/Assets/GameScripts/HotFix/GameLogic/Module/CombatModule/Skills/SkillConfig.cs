using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 技能类型
    /// </summary>
    public enum SkillType
    {
        None = 0,
        MeleeAttack = 1,      // 近战攻击
        RangedAttack = 2,     // 远程攻击
        AreaEffect = 3,       // 范围效果
        Buff = 4,             // 增益效果
        Debuff = 5,           // 减益效果
        Healing = 6,          // 治疗
        Movement = 7,         // 移动技能
        Summon = 8,           // 召唤技能
        Counter = 9,          // 反击技能
        Ultimate = 10         // 终极技能
    }
    
    /// <summary>
    /// 技能目标类型
    /// </summary>
    public enum SkillTargetType
    {
        None = 0,
        Self = 1,             // 自身
        SingleEnemy = 2,      // 单个敌人
        SingleAlly = 3,       // 单个友方
        AllEnemies = 4,       // 所有敌人
        AllAllies = 5,        // 所有友方
        Area = 6              // 区域目标
    }
    
    /// <summary>
    /// 技能触发类型
    /// </summary>
    public enum SkillTriggerType
    {
        None = 0,
        Button = 1,           // 按钮触发
        Gesture = 2,          // 手势触发
        Automatic = 3,        // 自动触发
        Reactive = 4,         // 反应性触发（如受伤后）
        Combo = 5             // 连招触发
    }
    
    /// <summary>
    /// 技能配置 - 用于存储技能基础属性
    /// </summary>
    public class SkillConfig
    {
        /// <summary>
        /// 技能ID
        /// </summary>
        public int SkillId { get; set; }
        
        /// <summary>
        /// 技能名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 技能描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 技能类型
        /// </summary>
        public SkillType Type { get; set; }
        
        /// <summary>
        /// 技能目标类型
        /// </summary>
        public SkillTargetType TargetType { get; set; }
        
        /// <summary>
        /// 技能触发类型
        /// </summary>
        public SkillTriggerType TriggerType { get; set; }
        
        /// <summary>
        /// 技能冷却时间（秒）
        /// </summary>
        public float Cooldown { get; set; }
        
        /// <summary>
        /// 技能基础伤害值
        /// </summary>
        public float BaseDamage { get; set; }
        
        /// <summary>
        /// 技能攻击力系数
        /// </summary>
        public float AttackMultiplier { get; set; }
        
        /// <summary>
        /// 技能范围（如果是范围技能）
        /// </summary>
        public float Range { get; set; }
        
        /// <summary>
        /// 技能持续时间（如果有）
        /// </summary>
        public float Duration { get; set; }
        
        /// <summary>
        /// 资源路径（特效、音效等）
        /// </summary>
        public string ResourcePath { get; set; }
        
        /// <summary>
        /// VR手势识别参数（如果是手势触发）
        /// </summary>
        public string GestureParams { get; set; }
        
        /// <summary>
        /// 技能所需VR输入类型（如果是按钮触发）
        /// </summary>
        public VRInputType InputType { get; set; }
        
        /// <summary>
        /// 技能特殊参数 - 可用于扩展特定技能效果
        /// </summary>
        public Dictionary<string, object> ExtraParams { get; set; } = new Dictionary<string, object>();
        
        public SkillConfig()
        {
            Name = "Unknown Skill";
            Description = "No description";
            Type = SkillType.None;
            TargetType = SkillTargetType.None;
            TriggerType = SkillTriggerType.None;
            Cooldown = 5f;
            BaseDamage = 10f;
            AttackMultiplier = 1.0f;
            Range = 1f;
            Duration = 0f;
            ResourcePath = string.Empty;
        }
    }
}
