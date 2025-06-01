using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 模拟数据提供器 - 用于提供临时配置数据，后续可替换为真实配置表
    /// </summary>
    public class MockDataProvider : Singleton<MockDataProvider>
    {
        private Dictionary<int, SkillConfig> _skillConfigs;
        
        /// <summary>
        /// 初始化模拟数据
        /// </summary>
        public void Initialize()
        {
            _skillConfigs = new Dictionary<int, SkillConfig>();
            InitializeSkillConfigs();
        }
        
        /// <summary>
        /// 获取所有技能配置
        /// </summary>
        public Dictionary<int, SkillConfig> GetAllSkillConfigs()
        {
            if (_skillConfigs == null)
            {
                Initialize();
            }
            return _skillConfigs;
        }
        
        /// <summary>
        /// 获取指定ID的技能配置
        /// </summary>
        public SkillConfig GetSkillConfig(int skillId)
        {
            if (_skillConfigs == null)
            {
                Initialize();
            }
            
            if (_skillConfigs.TryGetValue(skillId, out SkillConfig config))
            {
                return config;
            }
            
            return null;
        }
        
        /// <summary>
        /// 初始化技能配置数据
        /// </summary>
        private void InitializeSkillConfigs()
        {
            // 近战普通攻击
            AddSkillConfig(new SkillConfig
            {
                SkillId = 1001,
                Name = "普通攻击",
                Description = "基础的近战攻击",
                Type = SkillType.MeleeAttack,
                TargetType = SkillTargetType.SingleEnemy,
                TriggerType = SkillTriggerType.Button,
                Cooldown = 1.5f,
                BaseDamage = 10f,
                AttackMultiplier = 1.0f,
                Range = 1.5f,
                Duration = 0f,
                ResourcePath = "Effects/MeleeAttack",
                InputType = VRInputType.TriggerButton
            });
            
            // 远程射击
            AddSkillConfig(new SkillConfig
            {
                SkillId = 2001,
                Name = "远程射击",
                Description = "射出一道能量弹攻击敌人",
                Type = SkillType.RangedAttack,
                TargetType = SkillTargetType.SingleEnemy,
                TriggerType = SkillTriggerType.Button,
                Cooldown = 3.0f,
                BaseDamage = 20f,
                AttackMultiplier = 1.2f,
                Range = 10f,
                Duration = 0f,
                ResourcePath = "Effects/RangedAttack",
                InputType = VRInputType.GripButton
            });
            
            // 范围攻击
            AddSkillConfig(new SkillConfig
            {
                SkillId = 3001,
                Name = "旋风斩",
                Description = "360度范围攻击所有敌人",
                Type = SkillType.AreaEffect,
                TargetType = SkillTargetType.AllEnemies,
                TriggerType = SkillTriggerType.Gesture,
                Cooldown = 8.0f,
                BaseDamage = 15f,
                AttackMultiplier = 0.8f,
                Range = 3f,
                Duration = 0f,
                ResourcePath = "Effects/WhirlwindSlash",
                GestureParams = "Circular"
            });
            
            // 增益效果
            var buffSkill = new SkillConfig
            {
                SkillId = 4001,
                Name = "力量增强",
                Description = "暂时增加攻击力",
                Type = SkillType.Buff,
                TargetType = SkillTargetType.Self,
                TriggerType = SkillTriggerType.Button,
                Cooldown = 15.0f,
                BaseDamage = 0f,
                AttackMultiplier = 0f,
                Range = 0f,
                Duration = 10f,
                ResourcePath = "Effects/AttackBuff",
                InputType = VRInputType.MenuButton
            };
            buffSkill.ExtraParams["AttributeType"] = "Attack";
            buffSkill.ExtraParams["Value"] = 15f;
            AddSkillConfig(buffSkill);
            
            // 减益效果
            var debuffSkill = new SkillConfig
            {
                SkillId = 5001,
                Name = "虚弱诅咒",
                Description = "降低敌人的防御力",
                Type = SkillType.Debuff,
                TargetType = SkillTargetType.SingleEnemy,
                TriggerType = SkillTriggerType.Gesture,
                Cooldown = 12.0f,
                BaseDamage = 0f,
                AttackMultiplier = 0f,
                Range = 5f,
                Duration = 8f,
                ResourcePath = "Effects/DefenseDebuff",
                GestureParams = "Point"
            };
            debuffSkill.ExtraParams["AttributeType"] = "Defense";
            debuffSkill.ExtraParams["Value"] = -10f;
            AddSkillConfig(debuffSkill);
            
            // 终极技能
            AddSkillConfig(new SkillConfig
            {
                SkillId = 6001,
                Name = "元素风暴",
                Description = "召唤元素风暴攻击所有敌人",
                Type = SkillType.Ultimate,
                TargetType = SkillTargetType.AllEnemies,
                TriggerType = SkillTriggerType.Gesture,
                Cooldown = 30.0f,
                BaseDamage = 50f,
                AttackMultiplier = 2.0f,
                Range = 8f,
                Duration = 0f,
                ResourcePath = "Effects/ElementalStorm",
                GestureParams = "CrossArms"
            });
        }
        
        /// <summary>
        /// 添加技能配置
        /// </summary>
        private void AddSkillConfig(SkillConfig config)
        {
            _skillConfigs[config.SkillId] = config;
        }
    }
    
    
}
