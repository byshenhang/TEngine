using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 状态效果类型
    /// </summary>
    public enum StatusEffectType
    {
        Buff,       // 增益效果
        Debuff,     // 减益效果
        OverTime,   // 持续性效果
        Control     // 控制效果
    }
    
    /// <summary>
    /// 战斗结果
    /// </summary>
    public enum CombatResult
    {
        Unknown,    // 未知
        Victory,    // 胜利
        Defeat,     // 失败
        Draw        // 平局
    }
    
    /// <summary>
    /// 状态效果 - 表示一个临时的效果，作用于战斗实体
    /// </summary>
    public abstract class StatusEffect
    {
        // 效果唯一ID
        private string _effectId;
        
        // 效果名称
        private string _name;
        
        // 效果描述
        private string _description;
        
        // 效果类型
        private StatusEffectType _effectType;
        
        // 效果来源实体
        private string _sourceEntityId;
        
        // 效果图标
        private string _iconName;
        
        // 持续时间（秒）
        private float _duration;
        
        // 开始时间
        private float _startTime;
        
        // 最后一次更新时间
        private float _lastUpdateTime;
        
        // 特效名称
        private string _vfxName;
        
        // 是否可堆叠
        private bool _isStackable;
        
        // 当前堆叠层数
        private int _stackCount = 1;
        
        // 最大堆叠层数
        private int _maxStacks = 1;
        
        // 是否已激活
        private bool _isActive = false;
        
        // 影响的属性修饰符
        private List<AttributeModifier> _attributeModifiers = new List<AttributeModifier>();
        
        #region 属性访问器
        
        /// <summary>
        /// 效果唯一ID
        /// </summary>
        public string EffectId => _effectId;
        
        /// <summary>
        /// 效果名称
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// 效果描述
        /// </summary>
        public string Description => _description;
        
        /// <summary>
        /// 效果类型
        /// </summary>
        public StatusEffectType EffectType => _effectType;
        
        /// <summary>
        /// 效果来源实体ID
        /// </summary>
        public string SourceEntityId => _sourceEntityId;
        
        /// <summary>
        /// 效果图标
        /// </summary>
        public string IconName => _iconName;
        
        /// <summary>
        /// 效果持续时间
        /// </summary>
        public float Duration => _duration;
        
        /// <summary>
        /// 效果已持续时间
        /// </summary>
        public float ElapsedTime => Time.time - _startTime;
        
        /// <summary>
        /// 效果剩余时间
        /// </summary>
        public float RemainingTime => Mathf.Max(0, _duration - ElapsedTime);
        
        /// <summary>
        /// 效果是否已过期
        /// </summary>
        public bool IsExpired => _duration > 0 && ElapsedTime >= _duration;
        
        /// <summary>
        /// 效果特效名称
        /// </summary>
        public string VfxName => _vfxName;
        
        /// <summary>
        /// 是否可堆叠
        /// </summary>
        public bool IsStackable => _isStackable;
        
        /// <summary>
        /// 当前堆叠层数
        /// </summary>
        public int StackCount => _stackCount;
        
        /// <summary>
        /// 最大堆叠层数
        /// </summary>
        public int MaxStacks => _maxStacks;
        
        /// <summary>
        /// 是否已激活
        /// </summary>
        public bool IsActive => _isActive;
        
        #endregion
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public StatusEffect(string effectId, string name, string description, StatusEffectType effectType, float duration, string sourceEntityId = null)
        {
            _effectId = effectId;
            _name = name;
            _description = description;
            _effectType = effectType;
            _duration = duration;
            _sourceEntityId = sourceEntityId;
            
            _startTime = Time.time;
            _lastUpdateTime = _startTime;
        }
        
        /// <summary>
        /// 设置效果图标
        /// </summary>
        public void SetIcon(string iconName)
        {
            _iconName = iconName;
        }
        
        /// <summary>
        /// 设置效果特效
        /// </summary>
        public void SetVfx(string vfxName)
        {
            _vfxName = vfxName;
        }
        
        /// <summary>
        /// 设置堆叠相关属性
        /// </summary>
        public void SetStackingProperties(bool isStackable, int maxStacks)
        {
            _isStackable = isStackable;
            _maxStacks = maxStacks;
        }
        
        /// <summary>
        /// 增加堆叠层数
        /// </summary>
        public void AddStack(int count = 1)
        {
            if (!_isStackable) return;
            
            int oldStacks = _stackCount;
            _stackCount = Mathf.Min(_stackCount + count, _maxStacks);
            
            if (_stackCount != oldStacks)
            {
                // 重置持续时间
                _startTime = Time.time;
                Log.Info($"状态效果 {_name} 堆叠增加: {oldStacks} -> {_stackCount}");
            }
        }
        
        /// <summary>
        /// 减少堆叠层数
        /// </summary>
        public void RemoveStack(int count = 1)
        {
            if (!_isStackable) return;
            
            int oldStacks = _stackCount;
            _stackCount = Mathf.Max(0, _stackCount - count);
            
            if (_stackCount != oldStacks)
            {
                Log.Info($"状态效果 {_name} 堆叠减少: {oldStacks} -> {_stackCount}");
            }
        }
        
        /// <summary>
        /// 添加属性修饰符
        /// </summary>
        public void AddAttributeModifier(AttributeModifier modifier)
        {
            if (modifier != null)
            {
                _attributeModifiers.Add(modifier);
            }
        }
        
        /// <summary>
        /// 应用效果
        /// </summary>
        public virtual void OnApply(CombatEntityBase target)
        {
            if (_isActive) return;
            
            _isActive = true;
            _startTime = Time.time;
            _lastUpdateTime = _startTime;
            
            // 应用所有属性修饰符
            foreach (var modifier in _attributeModifiers)
            {
                target.Attributes.AddModifier(modifier);
            }
            
            // 播放特效
            if (!string.IsNullOrEmpty(_vfxName) && target.GameObject != null)
            {
                // VFXModule.PlayEffect(_vfxName, target.GameObject);
                Log.Info($"播放状态特效: {_vfxName} 在 {target.Name} 上");
            }
            
            Log.Info($"状态效果 {_name} 已应用于 {target.Name}");
            
            // 子类可以重写此方法添加更多逻辑
        }
        
        /// <summary>
        /// 移除效果
        /// </summary>
        public virtual void OnRemove(CombatEntityBase target)
        {
            if (!_isActive) return;
            
            _isActive = false;
            
            // 移除所有属性修饰符
            foreach (var modifier in _attributeModifiers)
            {
                target.Attributes.RemoveModifier(modifier.Id, modifier.AttributeType);
            }
            
            // 停止特效
            if (!string.IsNullOrEmpty(_vfxName) && target.GameObject != null)
            {
                // VFXModule.StopEffect(_vfxName, target.GameObject);
                Log.Info($"停止状态特效: {_vfxName} 在 {target.Name} 上");
            }
            
            Log.Info($"状态效果 {_name} 已从 {target.Name} 移除");
            
            // 子类可以重写此方法添加更多逻辑
        }
        
        /// <summary>
        /// 更新效果
        /// </summary>
        public virtual void OnUpdate(CombatEntityBase target)
        {
            if (!_isActive) return;
            
            float currentTime = Time.time;
            float deltaTime = currentTime - _lastUpdateTime;
            _lastUpdateTime = currentTime;
            
            // 子类可以重写此方法添加更多逻辑
            OnTick(target, deltaTime);
        }
        
        /// <summary>
        /// 效果周期性触发
        /// </summary>
        protected virtual void OnTick(CombatEntityBase target, float deltaTime)
        {
            // 子类实现
        }
    }
    
    /// <summary>
    /// 持续伤害状态效果（例如：中毒、燃烧等）
    /// </summary>
    public class DamageOverTimeEffect : StatusEffect
    {
        private float _tickInterval; // 触发间隔（秒）
        private float _damagePerTick; // 每次伤害量
        private DamageType _damageType; // 伤害类型
        private float _timeUntilNextTick; // 距离下次触发的时间
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public DamageOverTimeEffect(string effectId, string name, string description, float duration, 
            float tickInterval, float damagePerTick, DamageType damageType, string sourceEntityId = null)
            : base(effectId, name, description, StatusEffectType.OverTime, duration, sourceEntityId)
        {
            _tickInterval = tickInterval;
            _damagePerTick = damagePerTick;
            _damageType = damageType;
            _timeUntilNextTick = 0; // 立即第一次触发
        }
        
        protected override void OnTick(CombatEntityBase target, float deltaTime)
        {
            _timeUntilNextTick -= deltaTime;
            
            if (_timeUntilNextTick <= 0)
            {
                // 应用伤害
                CombatEntityBase source = null;
                if (!string.IsNullOrEmpty(SourceEntityId))
                {
                    source = GameModule.Combat.GetCombatEntity(SourceEntityId);
                }
                
                // 计算实际伤害，考虑堆叠
                float actualDamage = _damagePerTick * StackCount;
                
                target.TakeDamage(source, actualDamage, _damageType);
                
                // 重置计时器
                _timeUntilNextTick = _tickInterval;
            }
        }
    }
    
    /// <summary>
    /// 控制型状态效果（例如：眩晕、冰冻等）
    /// </summary>
    public class ControlEffect : StatusEffect
    {
        private bool _preventAttack; // 阻止攻击
        private bool _preventMovement; // 阻止移动
        private bool _preventSkillUse; // 阻止技能使用
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ControlEffect(string effectId, string name, string description, float duration, 
            bool preventAttack, bool preventMovement, bool preventSkillUse, string sourceEntityId = null)
            : base(effectId, name, description, StatusEffectType.Control, duration, sourceEntityId)
        {
            _preventAttack = preventAttack;
            _preventMovement = preventMovement;
            _preventSkillUse = preventSkillUse;
        }
        
        public override void OnApply(CombatEntityBase target)
        {
            base.OnApply(target);
            
            // 应用控制效果的逻辑
            if (_preventAttack)
            {
                Log.Info($"{target.Name} 被禁止攻击");
            }
            
            if (_preventMovement)
            {
                Log.Info($"{target.Name} 被禁止移动");
            }
            
            if (_preventSkillUse)
            {
                Log.Info($"{target.Name} 被禁止使用技能");
            }
            
            // 在实际实现中，应该将控制状态保存到目标实体的相关属性中
        }
        
        public override void OnRemove(CombatEntityBase target)
        {
            base.OnRemove(target);
            
            // 移除控制效果的逻辑
            if (_preventAttack)
            {
                Log.Info($"{target.Name} 可以再次攻击");
            }
            
            if (_preventMovement)
            {
                Log.Info($"{target.Name} 可以再次移动");
            }
            
            if (_preventSkillUse)
            {
                Log.Info($"{target.Name} 可以再次使用技能");
            }
            
            // 在实际实现中，应该恢复目标实体的相关状态
        }
    }
}
