using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 技能类型
    /// </summary>
    public enum SkillType
    {
        Active,     // 主动技能
        Passive,    // 被动技能
        Toggle,     // 切换技能
        Item        // 物品技能
    }
    
    /// <summary>
    /// 技能目标类型
    /// </summary>
    public enum SkillTargetType
    {
        Self,               // 自身
        SingleTarget,       // 单体目标
        MultiTarget,        // 多目标
        Area,               // 区域
        Direction,          // 方向
        Chain,              // 链式
        All,                // 全体
        AllAllies,          // 所有队友
        AllEnemies          // 所有敌人
    }
    
    /// <summary>
    /// 技能范围类型
    /// </summary>
    public enum SkillRangeType
    {
        Melee,      // 近战
        Ranged,     // 远程
        SelfOnly    // 仅自身
    }
    
    /// <summary>
    /// 技能基类 - 所有技能的抽象基类
    /// </summary>
    public abstract class SkillBase
    {
        // 技能ID
        protected string _skillId;
        
        // 技能名称
        protected string _name;
        
        // 技能描述
        protected string _description;
        
        // 技能图标
        protected string _iconName;
        
        // 技能类型
        protected SkillType _skillType;
        
        // 技能目标类型
        protected SkillTargetType _targetType;
        
        // 技能范围类型
        protected SkillRangeType _rangeType;
        
        // 技能等级
        protected int _level = 1;
        
        // 最大等级
        protected int _maxLevel = 5;
        
        // 技能消耗
        protected Dictionary<AttributeType, float> _costs = new Dictionary<AttributeType, float>();
        
        // 冷却时间（秒）
        protected float _cooldownTime;
        
        // 当前冷却剩余时间
        protected float _currentCooldown;
        
        // 技能效果范围
        protected float _effectRange;
        
        // 伤害类型
        protected DamageType _damageType = DamageType.Physical;
        
        // 伤害系数
        protected float _damageCoefficient = 1.0f;
        
        // 附加效果
        protected List<Func<CombatEntityBase, CombatEntityBase, bool>> _additionalEffects = 
            new List<Func<CombatEntityBase, CombatEntityBase, bool>>();
            
        // 技能特效
        protected string _vfxName;
        
        // 技能音效
        protected string _sfxName;
        
        // 技能动画
        protected string _animationName;
        
        // 施法时间（秒）
        protected float _castTime;
        
        // VR交互所需手势或动作
        protected string _vrGestureName;
        
        // 最后一次使用时间
        protected float _lastUseTime;
        
        // 是否是被动技能
        protected bool IsPassive => _skillType == SkillType.Passive;
        
        #region 属性访问器
        
        /// <summary>
        /// 技能ID
        /// </summary>
        public string SkillId => _skillId;
        
        /// <summary>
        /// 技能名称
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// 技能描述
        /// </summary>
        public string Description => _description;
        
        /// <summary>
        /// 技能图标
        /// </summary>
        public string IconName => _iconName;
        
        /// <summary>
        /// 技能类型
        /// </summary>
        public SkillType SkillType => _skillType;
        
        /// <summary>
        /// 技能目标类型
        /// </summary>
        public SkillTargetType TargetType => _targetType;
        
        /// <summary>
        /// 技能范围类型
        /// </summary>
        public SkillRangeType RangeType => _rangeType;
        
        /// <summary>
        /// 技能等级
        /// </summary>
        public int Level => _level;
        
        /// <summary>
        /// 最大等级
        /// </summary>
        public int MaxLevel => _maxLevel;
        
        /// <summary>
        /// 冷却时间（秒）
        /// </summary>
        public float CooldownTime => _cooldownTime;
        
        /// <summary>
        /// 当前冷却剩余时间
        /// </summary>
        public float CurrentCooldown => _currentCooldown;
        
        /// <summary>
        /// 技能效果范围
        /// </summary>
        public float EffectRange => _effectRange;
        
        /// <summary>
        /// 技能伤害类型
        /// </summary>
        public DamageType DamageType => _damageType;
        
        /// <summary>
        /// 技能伤害系数
        /// </summary>
        public float DamageCoefficient => _damageCoefficient;
        
        /// <summary>
        /// 施法时间（秒）
        /// </summary>
        public float CastTime => _castTime;
        
        /// <summary>
        /// 技能特效名称
        /// </summary>
        public string VfxName => _vfxName;
        
        /// <summary>
        /// 技能音效名称
        /// </summary>
        public string SfxName => _sfxName;
        
        /// <summary>
        /// 技能动画名称
        /// </summary>
        public string AnimationName => _animationName;
        
        /// <summary>
        /// VR交互所需手势
        /// </summary>
        public string VrGestureName => _vrGestureName;
        
        /// <summary>
        /// 最后一次使用时间
        /// </summary>
        public float LastUseTime => _lastUseTime;
        
        /// <summary>
        /// 技能是否已冷却完成
        /// </summary>
        public bool IsReady => _currentCooldown <= 0;
        
        #endregion
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public SkillBase(string skillId, string name, string description, SkillType skillType, SkillTargetType targetType, SkillRangeType rangeType)
        {
            _skillId = skillId;
            _name = name;
            _description = description;
            _skillType = skillType;
            _targetType = targetType;
            _rangeType = rangeType;
            _lastUseTime = -9999; // 确保初始时可以使用
        }
        
        /// <summary>
        /// 初始化技能
        /// </summary>
        public virtual void Initialize()
        {
            _currentCooldown = 0;
            Log.Info($"技能 {_name} 已初始化");
        }
        
        /// <summary>
        /// 设置技能等级
        /// </summary>
        public virtual void SetLevel(int level)
        {
            _level = Mathf.Clamp(level, 1, _maxLevel);
            OnLevelChanged();
            Log.Info($"技能 {_name} 等级设置为 {_level}");
        }
        
        /// <summary>
        /// 等级变化时调用
        /// </summary>
        protected virtual void OnLevelChanged()
        {
            // 子类实现，更新随等级变化的属性
        }
        
        /// <summary>
        /// 升级技能
        /// </summary>
        public virtual bool LevelUp()
        {
            if (_level < _maxLevel)
            {
                SetLevel(_level + 1);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 设置技能消耗
        /// </summary>
        public void SetCost(AttributeType costType, float value)
        {
            _costs[costType] = value;
        }
        
        /// <summary>
        /// 获取技能消耗
        /// </summary>
        public float GetCost(AttributeType costType)
        {
            return _costs.TryGetValue(costType, out float value) ? value : 0f;
        }
        
        /// <summary>
        /// 设置技能特效
        /// </summary>
        public void SetVfx(string vfxName)
        {
            _vfxName = vfxName;
        }
        
        /// <summary>
        /// 设置技能音效
        /// </summary>
        public void SetSfx(string sfxName)
        {
            _sfxName = sfxName;
        }
        
        /// <summary>
        /// 设置技能动画
        /// </summary>
        public void SetAnimation(string animationName)
        {
            _animationName = animationName;
        }
        
        /// <summary>
        /// 添加附加效果
        /// </summary>
        public void AddAdditionalEffect(Func<CombatEntityBase, CombatEntityBase, bool> effect)
        {
            if (effect != null)
            {
                _additionalEffects.Add(effect);
            }
        }
        
        /// <summary>
        /// 更新技能冷却
        /// </summary>
        public virtual void UpdateCooldown(float deltaTime)
        {
            if (_currentCooldown > 0)
            {
                _currentCooldown -= deltaTime;
                if (_currentCooldown < 0)
                {
                    _currentCooldown = 0;
                }
            }
        }
        
        /// <summary>
        /// 重置冷却
        /// </summary>
        public virtual void ResetCooldown()
        {
            _currentCooldown = 0;
            Log.Info($"技能 {_name} 冷却已重置");
        }
        
        /// <summary>
        /// 启动冷却
        /// </summary>
        protected virtual void StartCooldown()
        {
            _currentCooldown = _cooldownTime;
            _lastUseTime = Time.time;
        }
        
        /// <summary>
        /// 检查技能是否可用
        /// </summary>
        public virtual bool CanUse(CombatEntityBase caster)
        {
            if (caster == null || !caster.IsAlive || !caster.IsInCombat)
            {
                return false;
            }
            
            // 检查冷却
            if (_currentCooldown > 0)
            {
                Log.Warning($"技能 {_name} 正在冷却中，剩余 {_currentCooldown:F1} 秒");
                return false;
            }
            
            // 检查消耗
            foreach (var cost in _costs)
            {
                float requiredCost = cost.Value;
                float currentValue = 0;
                
                switch (cost.Key)
                {
                    case AttributeType.Mana:
                        // 假设实体有获取当前魔法值的方法
                        currentValue = caster.Attributes.GetAttributeValue(AttributeType.Mana);
                        break;
                    case AttributeType.Stamina:
                        // 假设实体有获取当前耐力值的方法
                        currentValue = caster.Attributes.GetAttributeValue(AttributeType.Stamina);
                        break;
                    // 可以添加其他资源类型
                }
                
                if (currentValue < requiredCost)
                {
                    Log.Warning($"技能 {_name} 使用失败，{cost.Key} 不足，需要 {requiredCost}，当前 {currentValue}");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 使用技能
        /// </summary>
        public virtual async UniTask<bool> Use(CombatEntityBase caster, CombatEntityBase target)
        {
            // 检查技能是否可用
            if (!CanUse(caster))
            {
                return false;
            }
            
            // 检查目标是否有效
            if (target == null && _targetType != SkillTargetType.Self && _targetType != SkillTargetType.Area && _targetType != SkillTargetType.Direction)
            {
                Log.Warning($"技能 {_name} 使用失败，无效的目标");
                return false;
            }
            
            Log.Info($"{caster.Name} 开始使用技能 {_name}");
            
            // 消耗资源
            ConsumeResources(caster);
            
            // 播放技能特效和动画
            PlaySkillEffects(caster, target);
            
            // 如果有施法时间，等待施法完成
            if (_castTime > 0)
            {
                Log.Info($"{caster.Name} 正在施放 {_name}，施法时间: {_castTime} 秒");
                await UniTask.Delay(TimeSpan.FromSeconds(_castTime));
            }
            
            // 执行技能效果
            bool success = await ExecuteSkillEffect(caster, target);
            
            // 应用附加效果
            if (success)
            {
                ApplyAdditionalEffects(caster, target);
            }
            
            // 启动冷却
            StartCooldown();
            
            Log.Info($"{caster.Name} 完成使用技能 {_name}");
            return success;
        }
        
        /// <summary>
        /// 消耗资源
        /// </summary>
        protected virtual void ConsumeResources(CombatEntityBase caster)
        {
            foreach (var cost in _costs)
            {
                switch (cost.Key)
                {
                    case AttributeType.Mana:
                        // 假设实体有消耗魔法的方法
                        // caster.ConsumeMana(cost.Value);
                        Log.Info($"{caster.Name} 消耗 {cost.Value} 点魔法值");
                        break;
                    case AttributeType.Stamina:
                        // 假设实体有消耗耐力的方法
                        // caster.ConsumeStamina(cost.Value);
                        Log.Info($"{caster.Name} 消耗 {cost.Value} 点耐力值");
                        break;
                    // 可以添加其他资源类型
                }
            }
        }
        
        /// <summary>
        /// 播放技能特效和动画
        /// </summary>
        protected virtual void PlaySkillEffects(CombatEntityBase caster, CombatEntityBase target)
        {
            // 播放技能特效
            if (!string.IsNullOrEmpty(_vfxName))
            {
                // VFXModule.PlayEffect(_vfxName, caster.GameObject);
                Log.Info($"播放技能特效: {_vfxName}");
            }
            
            // 播放技能音效
            if (!string.IsNullOrEmpty(_sfxName))
            {
                // AudioModule.PlaySound(_sfxName);
                Log.Info($"播放技能音效: {_sfxName}");
            }
            
            // 播放技能动画
            if (!string.IsNullOrEmpty(_animationName) && caster.GameObject != null)
            {
                // Animator animator = caster.GameObject.GetComponent<Animator>();
                // if (animator != null)
                // {
                //     animator.Play(_animationName);
                // }
                Log.Info($"{caster.Name} 播放技能动画: {_animationName}");
            }
        }
        
        /// <summary>
        /// 执行技能效果
        /// </summary>
        protected abstract UniTask<bool> ExecuteSkillEffect(CombatEntityBase caster, CombatEntityBase target);
        
        /// <summary>
        /// 应用附加效果
        /// </summary>
        protected virtual void ApplyAdditionalEffects(CombatEntityBase caster, CombatEntityBase target)
        {
            foreach (var effect in _additionalEffects)
            {
                effect?.Invoke(caster, target);
            }
        }
        
        /// <summary>
        /// 计算技能伤害
        /// </summary>
        protected virtual float CalculateDamage(CombatEntityBase caster, CombatEntityBase target)
        {
            if (caster == null) return 0;
            
            // 基础伤害计算
            float baseDamage = 0;
            
            switch (_damageType)
            {
                case DamageType.Physical:
                    baseDamage = caster.Attributes.GetAttributeValue(AttributeType.Attack) * _damageCoefficient;
                    break;
                case DamageType.Magical:
                    baseDamage = caster.Attributes.GetAttributeValue(AttributeType.MagicAttack) * _damageCoefficient;
                    break;
                case DamageType.True:
                    // 真实伤害基于攻击和魔法攻击的平均值
                    baseDamage = (caster.Attributes.GetAttributeValue(AttributeType.Attack) + 
                        caster.Attributes.GetAttributeValue(AttributeType.MagicAttack)) * 0.5f * _damageCoefficient;
                    break;
            }
            
            // 根据技能等级调整伤害
            float levelMultiplier = 1.0f + (_level - 1) * 0.2f; // 每级增加20%伤害
            
            // 计算最终伤害
            float finalDamage = baseDamage * levelMultiplier;
            
            return finalDamage;
        }
        
        /// <summary>
        /// 获取可用目标列表
        /// </summary>
        public virtual List<CombatEntityBase> GetValidTargets(CombatEntityBase caster, string combatId)
        {
            if (caster == null || string.IsNullOrEmpty(combatId))
            {
                return new List<CombatEntityBase>();
            }
            
            // 这里需要通过战斗管理器获取战斗中的实体
            // 实际实现应该使用 GameModule.Combat
            // 这里只是示例代码
            List<CombatEntityBase> allEntities = new List<CombatEntityBase>();
            List<CombatEntityBase> validTargets = new List<CombatEntityBase>();
            
            switch (_targetType)
            {
                case SkillTargetType.Self:
                    validTargets.Add(caster);
                    break;
                    
                case SkillTargetType.SingleTarget:
                    // 根据范围类型筛选单体目标
                    if (_rangeType == SkillRangeType.SelfOnly)
                    {
                        validTargets.Add(caster);
                    }
                    else
                    {
                        // 对于近战或远程，所有敌方实体都是潜在目标
                        foreach (var entity in allEntities)
                        {
                            if (entity != caster && entity.IsAlive && entity.EntityType != caster.EntityType)
                            {
                                validTargets.Add(entity);
                            }
                        }
                    }
                    break;
                    
                case SkillTargetType.AllAllies:
                    // 所有友方实体
                    foreach (var entity in allEntities)
                    {
                        if (entity.IsAlive && entity.EntityType == caster.EntityType)
                        {
                            validTargets.Add(entity);
                        }
                    }
                    break;
                    
                case SkillTargetType.AllEnemies:
                    // 所有敌方实体
                    foreach (var entity in allEntities)
                    {
                        if (entity.IsAlive && entity.EntityType != caster.EntityType)
                        {
                            validTargets.Add(entity);
                        }
                    }
                    break;
                    
                case SkillTargetType.All:
                    // 所有实体
                    foreach (var entity in allEntities)
                    {
                        if (entity.IsAlive)
                        {
                            validTargets.Add(entity);
                        }
                    }
                    break;
                    
                // 其他目标类型需要进一步实现
                // 例如 Area, Direction, Chain, MultiTarget 等
            }
            
            return validTargets;
        }
    }
}
