using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 行为树管理器 - 负责创建和管理AI行为树
    /// </summary>
    public class BehaviorTreeManager : IDisposable
    {
        // 行为树模板字典 <模板ID, 行为树根节点>
        private Dictionary<string, BehaviorTreeNode> _behaviorTreeTemplates = new Dictionary<string, BehaviorTreeNode>();
        
        // 实体行为树字典 <实体ID, 行为树根节点>
        private Dictionary<string, BehaviorTreeNode> _entityBehaviorTrees = new Dictionary<string, BehaviorTreeNode>();
        
        /// <summary>
        /// 初始化行为树管理器
        /// </summary>
        public void Initialize()
        {
            // 注册内置行为树模板
            RegisterBuiltInBehaviorTrees();
            
            Log.Info("行为树管理器初始化完成");
        }
        
        /// <summary>
        /// 注册内置行为树模板
        /// </summary>
        private void RegisterBuiltInBehaviorTrees()
        {
            // 注册近战敌人基础行为树
            RegisterBehaviorTreeTemplate("melee_enemy_basic", CreateMeleeEnemyBehaviorTree());
            
            // 注册远程敌人基础行为树
            RegisterBehaviorTreeTemplate("ranged_enemy_basic", CreateRangedEnemyBehaviorTree());
            
            // 注册Boss基础行为树
            RegisterBehaviorTreeTemplate("boss_basic", CreateBossBehaviorTree());
            
            Log.Info("内置行为树模板注册完成");
        }
        
        /// <summary>
        /// 创建近战敌人行为树
        /// </summary>
        private BehaviorTreeNode CreateMeleeEnemyBehaviorTree()
        {
            // 创建根选择器节点
            var rootSelector = new SelectorNode("Root", "近战敌人行为树根节点");
            
            // 1. 检查是否被眩晕或控制
            var stunSequence = new SequenceNode("StunSequence", "被控制时的行为");
            stunSequence.AddChild(new HasStatusEffectCondition("IsStunned", "stun", true));
            stunSequence.AddChild(new WaitNode("WaitStunned", 0.5f, "等待被控制状态结束"));
            rootSelector.AddChild(stunSequence);
            
            // 2. 检查生命值是否过低，尝试逃跑或使用恢复技能
            var lowHealthSequence = new SequenceNode("LowHealthSequence", "低生命值时的行为");
            lowHealthSequence.AddChild(new HealthCondition("IsLowHealth", 0.3f, HealthCondition.ComparisonType.LessThan));
            
            // 低生命值时的选择器
            var lowHealthSelector = new SelectorNode("LowHealthSelector", "低生命值时的选择");
            
            // 2.1 如果有治疗技能，使用治疗技能
            var healSequence = new SequenceNode("HealSequence", "使用治疗技能");
            healSequence.AddChild(new SkillCooldownCondition("CanHeal", "basic_heal"));
            healSequence.AddChild(new AttackNode("UseHeal", "basic_heal", entity => entity));
            lowHealthSelector.AddChild(healSequence);
            
            // 2.2 如果没有治疗技能，尝试逃跑
            var retreatSequence = new SequenceNode("RetreatSequence", "逃离战斗");
            retreatSequence.AddChild(new SimpleActionNode("Retreat", entity => {
                Log.Info($"{entity.Name} 正在尝试逃离战斗！");
                // 实际应该调用逃跑逻辑
            }));
            lowHealthSelector.AddChild(retreatSequence);
            
            lowHealthSequence.AddChild(lowHealthSelector);
            rootSelector.AddChild(lowHealthSequence);
            
            // 3. 主要战斗行为
            var combatSequence = new SequenceNode("CombatSequence", "战斗中的行为");
            combatSequence.AddChild(new IsInCombatCondition());
            
            // 主要战斗选择器
            var combatSelector = new SelectorNode("CombatSelector", "战斗选择器");
            
            // 3.1 如果目标在攻击范围内，进行攻击
            var attackSequence = new SequenceNode("AttackSequence", "攻击行为");
            // 目标选择函数 - 选择最近的敌人
            Func<CombatEntityBase, CombatEntityBase> selectTarget = entity => {
                // 这里应该通过CombatModule获取当前战斗中最近的敌人
                // 简化版本，返回null
                return null;
            };
            
            attackSequence.AddChild(new DistanceToTargetCondition("InAttackRange", selectTarget, 2.0f, 
                HealthCondition.ComparisonType.LessThanOrEqual));
            
            // 技能选择器
            var skillSelector = new SelectorNode("SkillSelector", "技能选择");
            
            // 3.1.1 尝试使用重击技能
            var heavyAttackSequence = new SequenceNode("HeavyAttackSequence", "使用重击");
            heavyAttackSequence.AddChild(new SkillCooldownCondition("CanHeavyAttack", "heavy_attack"));
            heavyAttackSequence.AddChild(new AttackNode("UseHeavyAttack", "heavy_attack", selectTarget));
            skillSelector.AddChild(heavyAttackSequence);
            
            // 3.1.2 尝试使用横扫技能
            var aoeAttackSequence = new SequenceNode("AoeAttackSequence", "使用横扫");
            aoeAttackSequence.AddChild(new SkillCooldownCondition("CanAoeAttack", "aoe_attack"));
            aoeAttackSequence.AddChild(new AttackNode("UseAoeAttack", "aoe_attack", selectTarget));
            skillSelector.AddChild(aoeAttackSequence);
            
            // 3.1.3 使用基础攻击
            var basicAttackSequence = new SequenceNode("BasicAttackSequence", "使用基础攻击");
            basicAttackSequence.AddChild(new SkillCooldownCondition("CanBasicAttack", "basic_attack"));
            basicAttackSequence.AddChild(new AttackNode("UseBasicAttack", "basic_attack", selectTarget));
            skillSelector.AddChild(basicAttackSequence);
            
            attackSequence.AddChild(skillSelector);
            combatSelector.AddChild(attackSequence);
            
            // 3.2 如果目标不在攻击范围内，移动到目标位置
            var moveSequence = new SequenceNode("MoveSequence", "移动行为");
            moveSequence.AddChild(new InverterNode("NotInRange", 
                new DistanceToTargetCondition("CheckRange", selectTarget, 2.0f, 
                    HealthCondition.ComparisonType.LessThanOrEqual)));
            moveSequence.AddChild(new MoveToTargetNode("MoveToTarget", selectTarget, 1.5f));
            combatSelector.AddChild(moveSequence);
            
            combatSequence.AddChild(combatSelector);
            rootSelector.AddChild(combatSequence);
            
            // 4. 非战斗行为（巡逻，闲置等）
            var idleSequence = new SequenceNode("IdleSequence", "非战斗状态行为");
            idleSequence.AddChild(new InverterNode("NotInCombat", new IsInCombatCondition()));
            idleSequence.AddChild(new SimpleActionNode("Idle", entity => {
                // 简单的闲置行为
                Log.Info($"{entity.Name} 处于闲置状态");
            }));
            rootSelector.AddChild(idleSequence);
            
            return rootSelector;
        }
        
        /// <summary>
        /// 创建远程敌人行为树
        /// </summary>
        private BehaviorTreeNode CreateRangedEnemyBehaviorTree()
        {
            // 创建根选择器节点
            var rootSelector = new SelectorNode("Root", "远程敌人行为树根节点");
            
            // 1. 检查是否被眩晕或控制
            var stunSequence = new SequenceNode("StunSequence", "被控制时的行为");
            stunSequence.AddChild(new HasStatusEffectCondition("IsStunned", "stun", true));
            stunSequence.AddChild(new WaitNode("WaitStunned", 0.5f, "等待被控制状态结束"));
            rootSelector.AddChild(stunSequence);
            
            // 2. 检查生命值是否过低，尝试逃跑或使用恢复技能
            var lowHealthSequence = new SequenceNode("LowHealthSequence", "低生命值时的行为");
            lowHealthSequence.AddChild(new HealthCondition("IsLowHealth", 0.3f, HealthCondition.ComparisonType.LessThan));
            
            // 低生命值时的选择器
            var lowHealthSelector = new SelectorNode("LowHealthSelector", "低生命值时的选择");
            
            // 2.1 如果有治疗技能，使用治疗技能
            var healSequence = new SequenceNode("HealSequence", "使用治疗技能");
            healSequence.AddChild(new SkillCooldownCondition("CanHeal", "basic_heal"));
            healSequence.AddChild(new AttackNode("UseHeal", "basic_heal", entity => entity));
            lowHealthSelector.AddChild(healSequence);
            
            // 2.2 如果没有治疗技能，尝试拉开距离
            var retreatSequence = new SequenceNode("RetreatSequence", "拉开距离");
            // 目标选择函数 - 选择最近的敌人
            Func<CombatEntityBase, CombatEntityBase> selectTarget = entity => {
                // 这里应该通过CombatModule获取当前战斗中最近的敌人
                // 简化版本，返回null
                return null;
            };
            
            // 检查是否太靠近敌人
            retreatSequence.AddChild(new DistanceToTargetCondition("TooClose", selectTarget, 5.0f, 
                HealthCondition.ComparisonType.LessThan));
            
            // 向后移动的行为
            retreatSequence.AddChild(new SimpleActionNode("MoveBack", entity => {
                Log.Info($"{entity.Name} 正在尝试拉开距离！");
                // 实际应该调用后退逻辑
            }));
            lowHealthSelector.AddChild(retreatSequence);
            
            lowHealthSequence.AddChild(lowHealthSelector);
            rootSelector.AddChild(lowHealthSequence);
            
            // 3. 主要战斗行为
            var combatSequence = new SequenceNode("CombatSequence", "战斗中的行为");
            combatSequence.AddChild(new IsInCombatCondition());
            
            // 主要战斗选择器
            var combatSelector = new SelectorNode("CombatSelector", "战斗选择器");
            
            // 3.1 如果距离太近，尝试拉开距离
            var distanceSequence = new SequenceNode("DistanceSequence", "保持距离");
            distanceSequence.AddChild(new DistanceToTargetCondition("TooClose", selectTarget, 5.0f, 
                HealthCondition.ComparisonType.LessThan));
            distanceSequence.AddChild(new SimpleActionNode("MoveBack", entity => {
                Log.Info($"{entity.Name} 正在尝试拉开距离！");
                // 实际应该调用后退逻辑
            }));
            combatSelector.AddChild(distanceSequence);
            
            // 3.2 如果在攻击范围内，进行攻击
            var attackSequence = new SequenceNode("AttackSequence", "攻击行为");
            attackSequence.AddChild(new DistanceToTargetCondition("InAttackRange", selectTarget, 15.0f, 
                HealthCondition.ComparisonType.LessThanOrEqual));
            
            // 技能选择器
            var skillSelector = new SelectorNode("SkillSelector", "技能选择");
            
            // 3.2.1 尝试使用特殊攻击技能
            var specialAttackSequence = new SequenceNode("SpecialAttackSequence", "使用特殊攻击");
            specialAttackSequence.AddChild(new SkillCooldownCondition("CanSpecialAttack", "special_attack"));
            specialAttackSequence.AddChild(new AttackNode("UseSpecialAttack", "special_attack", selectTarget));
            skillSelector.AddChild(specialAttackSequence);
            
            // 3.2.2 使用基础远程攻击
            var basicAttackSequence = new SequenceNode("BasicAttackSequence", "使用基础远程攻击");
            basicAttackSequence.AddChild(new SkillCooldownCondition("CanBasicAttack", "ranged_attack"));
            basicAttackSequence.AddChild(new AttackNode("UseBasicAttack", "ranged_attack", selectTarget));
            skillSelector.AddChild(basicAttackSequence);
            
            attackSequence.AddChild(skillSelector);
            combatSelector.AddChild(attackSequence);
            
            // 3.3 如果不在攻击范围内但也不太近，移动到适合的攻击距离
            var moveSequence = new SequenceNode("MoveSequence", "移动行为");
            moveSequence.AddChild(new InverterNode("NotInRange", 
                new DistanceToTargetCondition("CheckRange", selectTarget, 15.0f, 
                    HealthCondition.ComparisonType.LessThanOrEqual)));
            moveSequence.AddChild(new MoveToTargetNode("MoveToTarget", selectTarget, 10.0f));
            combatSelector.AddChild(moveSequence);
            
            combatSequence.AddChild(combatSelector);
            rootSelector.AddChild(combatSequence);
            
            // 4. 非战斗行为（巡逻，闲置等）
            var idleSequence = new SequenceNode("IdleSequence", "非战斗状态行为");
            idleSequence.AddChild(new InverterNode("NotInCombat", new IsInCombatCondition()));
            idleSequence.AddChild(new SimpleActionNode("Idle", entity => {
                // 简单的闲置行为
                Log.Info($"{entity.Name} 处于闲置状态");
            }));
            rootSelector.AddChild(idleSequence);
            
            return rootSelector;
        }
        
        /// <summary>
        /// 创建Boss行为树
        /// </summary>
        private BehaviorTreeNode CreateBossBehaviorTree()
        {
            // 创建Boss行为树，更复杂的行为模式
            var rootSelector = new SelectorNode("Root", "Boss行为树根节点");
            
            // 这里简化实现，实际Boss行为树应该更复杂
            // 可以包含多个阶段、特殊技能使用模式、阶段转换等
            
            return rootSelector;
        }
        
        /// <summary>
        /// 注册行为树模板
        /// </summary>
        public void RegisterBehaviorTreeTemplate(string templateId, BehaviorTreeNode rootNode)
        {
            if (string.IsNullOrEmpty(templateId) || rootNode == null)
            {
                Log.Error("注册行为树模板失败：模板ID为空或根节点为空");
                return;
            }
            
            if (_behaviorTreeTemplates.ContainsKey(templateId))
            {
                Log.Warning($"行为树模板 {templateId} 已存在，将被覆盖");
            }
            
            _behaviorTreeTemplates[templateId] = rootNode;
            Log.Info($"行为树模板 {templateId} 注册成功");
        }
        
        /// <summary>
        /// 获取行为树模板
        /// </summary>
        public BehaviorTreeNode GetBehaviorTreeTemplate(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return null;
            
            if (_behaviorTreeTemplates.TryGetValue(templateId, out var template))
            {
                return template;
            }
            
            Log.Warning($"行为树模板 {templateId} 不存在");
            return null;
        }
        
        /// <summary>
        /// 为实体分配行为树
        /// </summary>
        public BehaviorTreeNode AssignBehaviorTree(string entityId, string templateId)
        {
            if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(templateId))
            {
                Log.Error("为实体分配行为树失败：实体ID或模板ID为空");
                return null;
            }
            
            // 获取行为树模板
            var template = GetBehaviorTreeTemplate(templateId);
            if (template == null)
            {
                Log.Error($"为实体 {entityId} 分配行为树失败：模板 {templateId} 不存在");
                return null;
            }
            
            // 克隆行为树模板
            // 注意：实际实现中应该深度克隆行为树，这里简化处理
            // 应该自行实现深度克隆或使用序列化反序列化方法
            var behaviorTree = template;
            
            // 分配给实体
            _entityBehaviorTrees[entityId] = behaviorTree;
            Log.Info($"为实体 {entityId} 分配行为树 {templateId} 成功");
            
            return behaviorTree;
        }
        
        /// <summary>
        /// 获取实体的行为树
        /// </summary>
        public BehaviorTreeNode GetEntityBehaviorTree(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            
            if (_entityBehaviorTrees.TryGetValue(entityId, out var behaviorTree))
            {
                return behaviorTree;
            }
            
            Log.Warning($"实体 {entityId} 没有分配行为树");
            return null;
        }
        
        /// <summary>
        /// 更新实体的行为树
        /// </summary>
        public BehaviorNodeStatus UpdateEntityBehaviorTree(string entityId, CombatEntityBase entity)
        {
            if (string.IsNullOrEmpty(entityId) || entity == null) return BehaviorNodeStatus.Failure;
            
            var behaviorTree = GetEntityBehaviorTree(entityId);
            if (behaviorTree == null) return BehaviorNodeStatus.Failure;
            
            // 更新行为树
            var status = behaviorTree.Update(entity);
            return status;
        }
        
        /// <summary>
        /// 重置实体的行为树
        /// </summary>
        public void ResetEntityBehaviorTree(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return;
            
            var behaviorTree = GetEntityBehaviorTree(entityId);
            if (behaviorTree == null) return;
            
            // 重置行为树
            behaviorTree.Reset();
            Log.Info($"重置实体 {entityId} 的行为树");
        }
        
        /// <summary>
        /// 移除实体的行为树
        /// </summary>
        public bool RemoveEntityBehaviorTree(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return false;
            
            bool result = _entityBehaviorTrees.Remove(entityId);
            if (result)
            {
                Log.Info($"移除实体 {entityId} 的行为树成功");
            }
            else
            {
                Log.Warning($"实体 {entityId} 没有分配行为树，无法移除");
            }
            
            return result;
        }
        
        /// <summary>
        /// 清理所有实体行为树
        /// </summary>
        public void ClearAll()
        {
            _entityBehaviorTrees.Clear();
            Log.Info("清理所有实体的行为树");
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearAll();
            Log.Info("行为树管理器已释放资源");
        }
    }
}
