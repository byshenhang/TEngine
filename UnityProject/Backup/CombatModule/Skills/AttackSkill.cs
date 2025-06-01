using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 攻击技能 - 基础攻击类技能
    /// </summary>
    public class AttackSkill : SkillBase
    {
        // 攻击范围倍率（相对于普通攻击范围）
        private float _areaMultiplier = 1.0f;
        
        // 是否附带控制效果
        private bool _hasControlEffect = false;
        
        // 控制效果类型
        private string _controlEffectId = null;
        
        // 控制效果持续时间
        private float _controlEffectDuration = 0f;
        
        // 命中率修正
        private float _hitRateModifier = 0f;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public AttackSkill(string skillId, string name, string description) 
            : base(skillId, name, description, SkillType.Active, SkillTargetType.SingleTarget, SkillRangeType.Melee)
        {
            // 设置默认攻击技能参数
            _cooldownTime = 1.0f;     // 1秒冷却
            _castTime = 0.5f;         // 0.5秒施法时间
            _damageType = DamageType.Physical; // 物理伤害
            _damageCoefficient = 1.0f; // 伤害系数
            _effectRange = 2.0f;      // 默认范围
            
            // 设置技能消耗
            SetCost(AttributeType.Stamina, 10f);
        }
        
        /// <summary>
        /// 设置区域倍率
        /// </summary>
        public void SetAreaMultiplier(float multiplier)
        {
            _areaMultiplier = multiplier;
            _effectRange = 2.0f * _areaMultiplier;
        }
        
        /// <summary>
        /// 设置控制效果
        /// </summary>
        public void SetControlEffect(string effectId, float duration)
        {
            _hasControlEffect = true;
            _controlEffectId = effectId;
            _controlEffectDuration = duration;
        }
        
        /// <summary>
        /// 设置命中率修正
        /// </summary>
        public void SetHitRateModifier(float modifier)
        {
            _hitRateModifier = modifier;
        }
        
        /// <summary>
        /// 等级变化时调用
        /// </summary>
        protected override void OnLevelChanged()
        {
            // 随等级提升伤害系数
            _damageCoefficient = 1.0f + (_level - 1) * 0.2f; // 每级增加20%伤害
            
            // 冷却时间可能随等级减少
            _cooldownTime = Mathf.Max(0.5f, 1.0f - (_level - 1) * 0.1f); // 每级减少0.1秒冷却，最低0.5秒
        }
        
        /// <summary>
        /// 执行技能效果
        /// </summary>
        protected override async UniTask<bool> ExecuteSkillEffect(CombatEntityBase caster, CombatEntityBase target)
        {
            if (caster == null) return false;
            
            if (_targetType == SkillTargetType.SingleTarget)
            {
                // 单体攻击
                if (target == null || !target.IsAlive)
                {
                    Log.Warning($"{caster.Name} 使用技能 {_name} 失败，目标无效");
                    return false;
                }
                
                return await AttackSingleTarget(caster, target);
            }
            else if (_targetType == SkillTargetType.Area || _targetType == SkillTargetType.AllEnemies)
            {
                // 区域攻击
                return await AttackAreaTargets(caster, target);
            }
            
            return false;
        }
        
        /// <summary>
        /// 攻击单个目标
        /// </summary>
        private async UniTask<bool> AttackSingleTarget(CombatEntityBase caster, CombatEntityBase target)
        {
            // 计算伤害
            float damage = CalculateDamage(caster, target);
            
            // 计算命中
            bool isHit = CalculateHit(caster, target);
            
            if (!isHit)
            {
                Log.Info($"{caster.Name} 的技能 {_name} 未命中 {target.Name}");
                // 可以在这里添加闪避特效
                return false;
            }
            
            // 计算暴击
            bool isCritical = CalculateCritical(caster);
            if (isCritical)
            {
                // 暴击伤害翻倍
                damage *= 2.0f;
                Log.Info($"{caster.Name} 的技能 {_name} 对 {target.Name} 造成暴击！");
            }
            
            // 应用伤害
            target.TakeDamage(caster, damage, _damageType);
            Log.Info($"{caster.Name} 的技能 {_name} 对 {target.Name} 造成 {damage:F1} 点{_damageType}伤害");
            
            // 应用控制效果
            if (_hasControlEffect && _controlEffectId != null)
            {
                ApplyControlEffect(caster, target);
            }
            
            // 等待一点时间确保动画和效果完成
            await UniTask.Delay(100);
            
            return true;
        }
        
        /// <summary>
        /// 攻击区域目标
        /// </summary>
        private async UniTask<bool> AttackAreaTargets(CombatEntityBase caster, CombatEntityBase mainTarget)
        {
            bool hitAny = false;
            
            // 获取战斗ID
            string combatId = caster.IsInCombat ? caster.CurrentCombatId : null;
            if (string.IsNullOrEmpty(combatId))
            {
                Log.Warning($"{caster.Name} 不在战斗中，无法使用区域技能");
                return false;
            }
            
            // 获取所有可能的目标
            List<CombatEntityBase> targets = GetValidTargets(caster, combatId);
            
            // 过滤掉超出范围的目标
            List<CombatEntityBase> inRangeTargets = new List<CombatEntityBase>();
            
            // 如果有主目标，确保它在目标列表中
            if (mainTarget != null && mainTarget.IsAlive && !targets.Contains(mainTarget))
            {
                targets.Add(mainTarget);
            }
            
            // 过滤有效范围内的目标
            foreach (var potentialTarget in targets)
            {
                if (potentialTarget == null || !potentialTarget.IsAlive) continue;
                
                // 计算与施法者的距离
                float distance = CalculateDistance(caster, potentialTarget);
                
                // 如果在范围内，添加到目标列表
                if (distance <= _effectRange)
                {
                    inRangeTargets.Add(potentialTarget);
                }
            }
            
            // 依次攻击每个目标
            foreach (var target in inRangeTargets)
            {
                // 计算伤害（对区域目标可能有伤害衰减）
                float damageMultiplier = (target == mainTarget) ? 1.0f : 0.8f; // 非主目标伤害降低20%
                float damage = CalculateDamage(caster, target) * damageMultiplier;
                
                // 计算命中
                bool isHit = CalculateHit(caster, target);
                
                if (!isHit)
                {
                    Log.Info($"{caster.Name} 的技能 {_name} 未命中 {target.Name}");
                    continue;
                }
                
                hitAny = true;
                
                // 计算暴击
                bool isCritical = CalculateCritical(caster);
                if (isCritical)
                {
                    // 暴击伤害翻倍
                    damage *= 2.0f;
                    Log.Info($"{caster.Name} 的技能 {_name} 对 {target.Name} 造成暴击！");
                }
                
                // 应用伤害
                target.TakeDamage(caster, damage, _damageType);
                Log.Info($"{caster.Name} 的技能 {_name} 对 {target.Name} 造成 {damage:F1} 点{_damageType}伤害");
                
                // 应用控制效果（对区域目标可能有几率衰减）
                if (_hasControlEffect && _controlEffectId != null)
                {
                    float controlChance = (target == mainTarget) ? 1.0f : 0.5f; // 非主目标控制几率降低
                    if (Random.value <= controlChance)
                    {
                        ApplyControlEffect(caster, target);
                    }
                }
                
                // 短暂延迟，使效果看起来更自然
                await UniTask.Delay(50);
            }
            
            return hitAny;
        }
        
        /// <summary>
        /// 计算命中
        /// </summary>
        private bool CalculateHit(CombatEntityBase caster, CombatEntityBase target)
        {
            if (caster == null || target == null) return false;
            
            // 获取攻击者的命中属性
            float hitRate = caster.Attributes.GetAttributeValue(AttributeType.HitRate) + _hitRateModifier;
            
            // 获取防御者的闪避属性
            float dodgeRate = target.Attributes.GetAttributeValue(AttributeType.DodgeRate);
            
            // 计算实际命中率
            float finalHitRate = Mathf.Clamp01(hitRate - dodgeRate + 0.5f); // 基础命中率50%
            
            // 随机判定是否命中
            return Random.value <= finalHitRate;
        }
        
        /// <summary>
        /// 计算暴击
        /// </summary>
        private bool CalculateCritical(CombatEntityBase caster)
        {
            if (caster == null) return false;
            
            // 获取攻击者的暴击率属性
            float critRate = caster.Attributes.GetAttributeValue(AttributeType.CritRate);
            
            // 随机判定是否暴击
            return Random.value <= critRate;
        }
        
        /// <summary>
        /// 应用控制效果
        /// </summary>
        private void ApplyControlEffect(CombatEntityBase caster, CombatEntityBase target)
        {
            if (_controlEffectId == null || !_hasControlEffect) return;
            
            // 创建控制效果
            // 实际实现中应该通过状态效果工厂或管理器创建
            StatusEffect effect = null;
            
            // 根据控制效果ID创建不同类型的控制效果
            switch (_controlEffectId)
            {
                case "stun":
                    effect = new ControlEffect("stun", "眩晕", _controlEffectDuration, true, true, true);
                    break;
                case "root":
                    effect = new ControlEffect("root", "定身", _controlEffectDuration, false, true, false);
                    break;
                case "silence":
                    effect = new ControlEffect("silence", "沉默", _controlEffectDuration, false, false, true);
                    break;
            }
            
            // 应用效果
            if (effect != null)
            {
                target.ApplyStatusEffect(effect, caster);
                Log.Info($"{caster.Name} 的技能 {_name} 对 {target.Name} 施加了 {effect.Name} 效果，持续 {_controlEffectDuration} 秒");
            }
        }
        
        /// <summary>
        /// 计算两个实体之间的距离
        /// </summary>
        private float CalculateDistance(CombatEntityBase entity1, CombatEntityBase entity2)
        {
            if (entity1 == null || entity2 == null || 
                entity1.GameObject == null || entity2.GameObject == null)
            {
                return float.MaxValue;
            }
            
            return Vector3.Distance(
                entity1.GameObject.transform.position, 
                entity2.GameObject.transform.position);
        }
    }
}
