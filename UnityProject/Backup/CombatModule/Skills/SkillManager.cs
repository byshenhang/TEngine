using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 技能管理器 - 负责管理所有技能的创建、注册、使用和更新
    /// </summary>
    public class SkillManager : IDisposable
    {
        // 技能字典 <技能ID, 技能实例>
        private Dictionary<string, SkillBase> _skillTemplates = new Dictionary<string, SkillBase>();
        
        // 实体技能字典 <实体ID, <技能ID, 技能实例>>
        private Dictionary<string, Dictionary<string, SkillBase>> _entitySkills = new Dictionary<string, Dictionary<string, SkillBase>>();
        
        // 技能冷却字典 <实体ID, <技能ID, 剩余冷却时间>>
        private Dictionary<string, Dictionary<string, float>> _skillCooldowns = new Dictionary<string, Dictionary<string, float>>();
        
        /// <summary>
        /// 初始化技能管理器
        /// </summary>
        public void Initialize()
        {
            // 注册内置技能模板
            RegisterBuiltInSkills();
            
            Log.Info("技能管理器初始化完成");
        }
        
        /// <summary>
        /// 更新技能冷却时间
        /// </summary>
        public void Update(float deltaTime)
        {
            // 更新所有实体的技能冷却
            foreach (var entityId in _entitySkills.Keys)
            {
                var skills = _entitySkills[entityId];
                foreach (var skill in skills.Values)
                {
                    skill.UpdateCooldown(deltaTime);
                }
            }
        }
        
        /// <summary>
        /// 注册内置技能模板
        /// </summary>
        private void RegisterBuiltInSkills()
        {
            // 基础攻击技能
            var basicAttack = new AttackSkill("basic_attack", "基础攻击", "对单个目标造成物理伤害。");
            RegisterSkillTemplate(basicAttack);
            
            // 重击技能
            var heavyAttack = new AttackSkill("heavy_attack", "重击", "对单个目标造成更高的物理伤害，有几率击晕目标。");
            heavyAttack.SetCost(AttributeType.Stamina, 20f);
            heavyAttack.SetControlEffect("stun", 2.0f);
            RegisterSkillTemplate(heavyAttack);
            
            // 范围攻击技能
            var aoeAttack = new AttackSkill("aoe_attack", "横扫", "对周围敌人造成物理伤害。");
            aoeAttack.SetAreaMultiplier(2.5f);
            RegisterSkillTemplate(aoeAttack);
            
            // 基础治疗技能
            var basicHeal = new HealSkill("basic_heal", "治疗术", "为单个目标恢复生命值。");
            RegisterSkillTemplate(basicHeal);
            
            // 群体治疗技能
            var aoeHeal = new HealSkill("aoe_heal", "群体治疗", "为周围队友恢复生命值。");
            aoeHeal.SetHealingParameters(40f, 0.4f);
            RegisterSkillTemplate(aoeHeal);
            
            // 持续治疗技能
            var hotHeal = new HealSkill("hot_heal", "生命之泉", "为目标提供持续的生命恢复效果。");
            hotHeal.SetHealOverTimeEffect(10f, 15f, 2f);
            RegisterSkillTemplate(hotHeal);
            
            Log.Info("内置技能模板注册完成");
        }
        
        /// <summary>
        /// 注册技能模板
        /// </summary>
        public void RegisterSkillTemplate(SkillBase skill)
        {
            if (skill == null) return;
            
            string skillId = skill.SkillId;
            if (string.IsNullOrEmpty(skillId))
            {
                Log.Error("注册技能模板失败：技能ID为空");
                return;
            }
            
            if (_skillTemplates.ContainsKey(skillId))
            {
                Log.Warning($"技能模板 {skillId} 已存在，将被覆盖");
            }
            
            _skillTemplates[skillId] = skill;
            Log.Info($"技能模板 {skill.Name}({skillId}) 注册成功");
        }
        
        /// <summary>
        /// 获取技能模板
        /// </summary>
        public SkillBase GetSkillTemplate(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            
            if (_skillTemplates.TryGetValue(skillId, out var skill))
            {
                return skill;
            }
            
            Log.Warning($"技能模板 {skillId} 不存在");
            return null;
        }
        
        /// <summary>
        /// 为实体添加技能
        /// </summary>
        public SkillBase AddSkillToEntity(string entityId, string skillId, int skillLevel = 1)
        {
            if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(skillId))
            {
                Log.Error("为实体添加技能失败：实体ID或技能ID为空");
                return null;
            }
            
            // 获取技能模板
            var skillTemplate = GetSkillTemplate(skillId);
            if (skillTemplate == null)
            {
                Log.Error($"为实体 {entityId} 添加技能失败：技能模板 {skillId} 不存在");
                return null;
            }
            
            // 确保实体技能字典中有该实体的条目
            if (!_entitySkills.ContainsKey(entityId))
            {
                _entitySkills[entityId] = new Dictionary<string, SkillBase>();
            }
            
            // 创建技能实例并设置等级
            SkillBase skillInstance = CloneSkill(skillTemplate);
            skillInstance.SetLevel(skillLevel);
            skillInstance.Initialize();
            
            // 添加技能到实体技能字典
            var entitySkillDict = _entitySkills[entityId];
            if (entitySkillDict.ContainsKey(skillId))
            {
                Log.Warning($"实体 {entityId} 已拥有技能 {skillId}，将被覆盖");
            }
            
            entitySkillDict[skillId] = skillInstance;
            Log.Info($"为实体 {entityId} 添加技能 {skillInstance.Name}({skillId}) 成功，等级 {skillLevel}");
            
            return skillInstance;
        }
        
        /// <summary>
        /// 克隆技能实例
        /// </summary>
        private SkillBase CloneSkill(SkillBase template)
        {
            // 根据技能类型创建相应的技能实例
            if (template is AttackSkill attackTemplate)
            {
                // 创建攻击技能实例
                var attackSkill = new AttackSkill(template.SkillId, template.Name, template.Description);
                // 复制攻击技能特定属性
                if (attackTemplate.DamageCoefficient > 0)
                {
                    // 假设有设置伤害系数的方法
                }
                return attackSkill;
            }
            else if (template is HealSkill healTemplate)
            {
                // 创建治疗技能实例
                var healSkill = new HealSkill(template.SkillId, template.Name, template.Description);
                // 复制治疗技能特定属性
                return healSkill;
            }
            else
            {
                // 默认情况，创建基本技能实例
                // 实际实现中应根据技能类型进行分类
                Log.Warning($"未知技能类型，无法正确克隆：{template.GetType().Name}");
                return null;
            }
        }
        
        /// <summary>
        /// 从实体移除技能
        /// </summary>
        public bool RemoveSkillFromEntity(string entityId, string skillId)
        {
            if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(skillId))
            {
                Log.Error("从实体移除技能失败：实体ID或技能ID为空");
                return false;
            }
            
            // 检查实体是否存在
            if (!_entitySkills.TryGetValue(entityId, out var entitySkills))
            {
                Log.Warning($"实体 {entityId} 不存在或没有任何技能");
                return false;
            }
            
            // 移除技能
            if (!entitySkills.Remove(skillId))
            {
                Log.Warning($"实体 {entityId} 没有技能 {skillId}");
                return false;
            }
            
            Log.Info($"从实体 {entityId} 移除技能 {skillId} 成功");
            return true;
        }
        
        /// <summary>
        /// 获取实体的技能
        /// </summary>
        public SkillBase GetEntitySkill(string entityId, string skillId)
        {
            if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(skillId))
            {
                Log.Error("获取实体技能失败：实体ID或技能ID为空");
                return null;
            }
            
            // 检查实体是否存在
            if (!_entitySkills.TryGetValue(entityId, out var entitySkills))
            {
                Log.Warning($"实体 {entityId} 不存在或没有任何技能");
                return null;
            }
            
            // 获取技能
            if (!entitySkills.TryGetValue(skillId, out var skill))
            {
                Log.Warning($"实体 {entityId} 没有技能 {skillId}");
                return null;
            }
            
            return skill;
        }
        
        /// <summary>
        /// 获取实体的所有技能
        /// </summary>
        public List<SkillBase> GetEntitySkills(string entityId)
        {
            if (string.IsNullOrEmpty(entityId))
            {
                Log.Error("获取实体所有技能失败：实体ID为空");
                return new List<SkillBase>();
            }
            
            // 检查实体是否存在
            if (!_entitySkills.TryGetValue(entityId, out var entitySkills))
            {
                Log.Warning($"实体 {entityId} 不存在或没有任何技能");
                return new List<SkillBase>();
            }
            
            return new List<SkillBase>(entitySkills.Values);
        }
        
        /// <summary>
        /// 检查技能是否已冷却完成
        /// </summary>
        public bool IsSkillReady(string entityId, string skillId)
        {
            var skill = GetEntitySkill(entityId, skillId);
            if (skill == null) return false;
            
            return skill.IsReady;
        }
        
        /// <summary>
        /// 获取技能剩余冷却时间
        /// </summary>
        public float GetSkillCooldownRemaining(string entityId, string skillId)
        {
            var skill = GetEntitySkill(entityId, skillId);
            if (skill == null) return -1f;
            
            return skill.CurrentCooldown;
        }
        
        /// <summary>
        /// 重置技能冷却
        /// </summary>
        public bool ResetSkillCooldown(string entityId, string skillId)
        {
            var skill = GetEntitySkill(entityId, skillId);
            if (skill == null) return false;
            
            skill.ResetCooldown();
            Log.Info($"重置实体 {entityId} 的技能 {skillId} 冷却");
            return true;
        }
        
        /// <summary>
        /// 实体使用技能
        /// </summary>
        public async UniTask<bool> UseSkill(string entityId, string skillId, string targetEntityId)
        {
            if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(skillId))
            {
                Log.Error("使用技能失败：实体ID或技能ID为空");
                return false;
            }
            
            // 获取技能
            var skill = GetEntitySkill(entityId, skillId);
            if (skill == null)
            {
                Log.Error($"使用技能失败：实体 {entityId} 没有技能 {skillId}");
                return false;
            }
            
            // 获取施法者实体和目标实体
            // 在实际实现中，应该从 CombatEntityManager 获取
            CombatEntityBase caster = null;
            CombatEntityBase target = null;
            
            // 获取目标实体（如果有）
            if (!string.IsNullOrEmpty(targetEntityId))
            {
                target = GetEntityById(targetEntityId);
                if (target == null)
                {
                    Log.Warning($"使用技能失败：目标实体 {targetEntityId} 不存在");
                    return false;
                }
            }
            
            // 获取施法者实体
            caster = GetEntityById(entityId);
            if (caster == null)
            {
                Log.Error($"使用技能失败：施法者实体 {entityId} 不存在");
                return false;
            }
            
            // 使用技能
            bool success = await skill.Use(caster, target);
            
            if (success)
            {
                Log.Info($"实体 {entityId} 成功使用技能 {skillId}");
            }
            else
            {
                Log.Warning($"实体 {entityId} 使用技能 {skillId} 失败");
            }
            
            return success;
        }
        
        /// <summary>
        /// 获取实体
        /// </summary>
        private CombatEntityBase GetEntityById(string entityId)
        {
            // 在实际实现中，应该从 CombatModule 或 EntityManager 获取
            // 这里暂时返回 null，需要与实体管理系统集成
            return null;
        }
        
        /// <summary>
        /// 清理实体数据
        /// </summary>
        public void ClearEntityData(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return;
            
            _entitySkills.Remove(entityId);
            _skillCooldowns.Remove(entityId);
            
            Log.Info($"清理实体 {entityId} 的技能数据");
        }
        
        /// <summary>
        /// 清理所有数据
        /// </summary>
        public void ClearAll()
        {
            _entitySkills.Clear();
            _skillCooldowns.Clear();
            
            Log.Info("清理所有实体的技能数据");
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearAll();
            Log.Info("技能管理器已释放资源");
        }
    }
}
