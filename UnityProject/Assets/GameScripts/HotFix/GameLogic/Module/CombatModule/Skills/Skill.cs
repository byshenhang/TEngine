using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6280u80fdu57fau7c7b - u6240u6709u6280u80fdu7684u57fau7840u5b9eu73b0
    /// </summary>
    public abstract class Skill
    {
        protected SkillConfig _config;
        protected Entity _owner;
        protected float _cooldownRemaining;
        protected bool _isActive;
        
        /// <summary>
        /// u6280u80fdu914du7f6e
        /// </summary>
        public SkillConfig Config => _config;
        
        /// <summary>
        /// u6280u80fdu62e5u6709u8005
        /// </summary>
        public Entity Owner => _owner;
        
        /// <summary>
        /// u662fu5426u5904u4e8eu51b7u5374u4e2d
        /// </summary>
        public bool IsOnCooldown => _cooldownRemaining > 0;
        
        /// <summary>
        /// u5f53u524du51b7u5374u5269u4f59u65f6u95f4
        /// </summary>
        public float CooldownRemaining => _cooldownRemaining;
        
        /// <summary>
        /// u6280u80fdu662fu5426u6b63u5728u6fc0u6d3bu4e2d
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// u521du59cbu5316u6280u80fd
        /// </summary>
        public virtual void Init(SkillConfig config, Entity owner)
        {
            _config = config;
            _owner = owner;
            _cooldownRemaining = 0;
            _isActive = false;
            
            Log.Info($"[u6280u80fd] {config.Name} u521du59cbu5316u5b8cu6210");
        }
        
        /// <summary>
        /// u66f4u65b0u6280u80fdu72b6u6001
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            // u66f4u65b0u51b7u5374u65f6u95f4
            if (_cooldownRemaining > 0)
            {
                _cooldownRemaining -= deltaTime;
                if (_cooldownRemaining <= 0)
                {
                    _cooldownRemaining = 0;
                    OnCooldownComplete();
                }
            }
            
            // u66f4u65b0u6280u80fdu6fc0u6d3bu72b6u6001
            if (_isActive)
            {
                UpdateActiveSkill(deltaTime);
            }
        }
        
        /// <summary>
        /// u5c1du8bd5u65bdu653eu6280u80fd
        /// </summary>
        public virtual bool TryUseSkill(Entity target = null)
        {
            if (IsOnCooldown || _isActive)
            {
                return false;
            }
            
            if (!CanUseSkill(target))
            {
                return false;
            }
            
            UseSkill(target);
            StartCooldown();
            return true;
        }
        
        /// <summary>
        /// u5224u65adu662fu5426u53efu4ee5u4f7fu7528u6280u80fd
        /// </summary>
        protected virtual bool CanUseSkill(Entity target)
        {
            // u57fau7840u5224u65ad: u62e5u6709u8005u5b58u5728u4e14u975eu6b7bu4ea1u72b6u6001
            if (_owner == null || _owner.CurrentState == EntityStateType.Dead)
            {
                return false;
            }
            
            // u76eeu6807u5224u65ad: u5982u679cu9700u8981u76eeu6807u4f46u76eeu6807u4e0du5b58u5728
            if (NeedsTarget() && target == null)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// u662fu5426u9700u8981u76eeu6807
        /// </summary>
        protected virtual bool NeedsTarget()
        {
            return _config.TargetType != SkillTargetType.Self && 
                   _config.TargetType != SkillTargetType.AllEnemies &&
                   _config.TargetType != SkillTargetType.AllAllies;
        }
        
        /// <summary>
        /// u4f7fu7528u6280u80fd - u5b50u7c7bu5b9eu73b0u5177u4f53u6548u679c
        /// </summary>
        protected abstract void UseSkill(Entity target);
        
        /// <summary>
        /// u66f4u65b0u6fc0u6d3bu4e2du7684u6280u80fd - u7528u4e8eu6301u7eedu6027u6280u80fd
        /// </summary>
        protected virtual void UpdateActiveSkill(float deltaTime)
        {
            // u9ed8u8ba4u4e0du505au4efbu4f55u4e8b
        }
        
        /// <summary>
        /// u5f3au5236u7ed3u675fu6280u80fd
        /// </summary>
        public virtual void EndSkill()
        {
            if (_isActive)
            {
                _isActive = false;
                OnSkillEnd();
            }
        }
        
        /// <summary>
        /// u5f00u59cbu51b7u5374
        /// </summary>
        protected virtual void StartCooldown()
        {
            _cooldownRemaining = _config.Cooldown;
        }
        
        /// <summary>
        /// u51b7u5374u7ed3u675fu65f6u7684u56deu8c03
        /// </summary>
        protected virtual void OnCooldownComplete()
        {
            // u53efu4ee5u5728u5b50u7c7bu4e2du5b9eu73b0u5177u4f53u903bu8f91
        }
        
        /// <summary>
        /// u6280u80fdu7ed3u675fu65f6u7684u56deu8c03
        /// </summary>
        protected virtual void OnSkillEnd()
        {
            // u53efu4ee5u5728u5b50u7c7bu4e2du5b9eu73b0u5177u4f53u903bu8f91
        }
    }
    
    /// <summary>
    /// u8fd1u6218u653bu51fbu6280u80fd
    /// </summary>
    public class MeleeAttackSkill : Skill
    {
        protected override void UseSkill(Entity target)
        {
            _isActive = true;
            
            // u5982u679cu6709u76eeu6807uff0cu8ba1u7b97u4f24u5bb3u5e76u5e94u7528
            if (target != null)
            {
                // u8ba1u7b97u4f24u5bb3
                float damage = DamageCalculator.CalculateDamage(_owner, target, _config.BaseDamage, _config.AttackMultiplier);
                
                // u5e94u7528u4f24u5bb3
                target.TakeDamage(damage, _owner);
                
                Log.Info($"[u6280u80fd] {_owner.Name} u5bf9 {target.Name} u4f7fu7528u4e86 {_config.Name}, u9020u6210 {damage} u70b9u4f24u5bb3");
            }
            
            // u5ef6u8fdfu7ed3u675fu6280u80fduff08u6a21u62dfu6280u80fdu52a8u753bu65f6u95f4uff09
            _owner.StartCoroutine(EndSkillAfterDelay(0.5f));
        }
        
        private IEnumerator<float> EndSkillAfterDelay(float delay)
        {
            yield return delay;
            EndSkill();
        }
    }
    
    /// <summary>
    /// u8fdcu7a0bu653bu51fbu6280u80fd
    /// </summary>
    public class RangedAttackSkill : Skill
    {
        protected override void UseSkill(Entity target)
        {
            _isActive = true;
            
            // u5bf9u8fdcu7a0bu76eeu6807u9020u6210u4f24u5bb3
            if (target != null)
            {
                // u8ba1u7b97u4f24u5bb3
                float damage = DamageCalculator.CalculateDamage(_owner, target, _config.BaseDamage, _config.AttackMultiplier);
                
                // u5e94u7528u4f24u5bb3
                target.TakeDamage(damage, _owner);
                
                Log.Info($"[u6280u80fd] {_owner.Name} u5bf9 {target.Name} u4f7fu7528u4e86 {_config.Name}, u9020u6210 {damage} u70b9u4f24u5bb3");
            }
            
            // u5ef6u8fdfu7ed3u675fu6280u80fduff08u6a21u62dfu6280u80fdu52a8u753bu65f6u95f4uff09
            _owner.StartCoroutine(EndSkillAfterDelay(0.5f));
        }
        
        private IEnumerator<float> EndSkillAfterDelay(float delay)
        {
            yield return delay;
            EndSkill();
        }
    }
    
    /// <summary>
    /// u8303u56f4u6548u679cu6280u80fd
    /// </summary>
    public class AreaEffectSkill : Skill
    {
        protected override void UseSkill(Entity target)
        {
            _isActive = true;
            
            // u83b7u53d6u8303u56f4u5185u7684u76eeu6807
            List<Entity> targets = GameModule.Combat.GetEntitiesInRange(_owner.Transform.position, _config.Range);
            
            foreach (var entity in targets)
            {
                // u8df3u8fc7u81eau8eab
                if (entity == _owner)
                {
                    continue;
                }
                
                // u8ba1u7b97u4f24u5bb3
                float damage = DamageCalculator.CalculateDamage(_owner, entity, _config.BaseDamage, _config.AttackMultiplier);
                
                // u5e94u7528u4f24u5bb3
                entity.TakeDamage(damage, _owner);
            }
            
            Log.Info($"[u6280u80fd] {_owner.Name} u4f7fu7528u4e86u8303u56f4u6280u80fd {_config.Name}, u5f71u54cdu4e86 {targets.Count} u4e2au76eeu6807");
            
            // u5ef6u8fdfu7ed3u675fu6280u80fduff08u6a21u62dfu6280u80fdu52a8u753bu65f6u95f4uff09
            _owner.StartCoroutine(EndSkillAfterDelay(0.5f));
        }
        
        private IEnumerator<float> EndSkillAfterDelay(float delay)
        {
            yield return delay;
            EndSkill();
        }
    }
    
    /// <summary>
    /// u589eu76cau6548u679cu6280u80fduff08Buffuff09
    /// </summary>
    public class BuffSkill : Skill
    {
        protected override void UseSkill(Entity target)
        {
            _isActive = true;
            
            // u83b7u53d6u76eeu6807uff08u5982u679cu662fu81eau8eabuff0cu5219u4f7fu7528u62e5u6709u8005uff09
            Entity buffTarget = (_config.TargetType == SkillTargetType.Self) ? _owner : target;
            
            if (buffTarget != null)
            {
                // u6dfbu52a0u589eu76cau6548u679c
                // u5047u8bbeu6211u4eecu6709u4e00u4e2aStatusEffectu7cfbu7edfuff0cu53efu4ee5u8fd9u6837u8c03u7528uff1a
                // buffTarget.AddStatusEffect(new BuffStatusEffect(_config.Duration, _config.ExtraParams));
                
                // u7b80u5316u5b9eu73b0uff0cu76f4u63a5u589eu52a0u5c5eu6027
                string attributeName = _config.ExtraParams.ContainsKey("AttributeType") ? 
                    _config.ExtraParams["AttributeType"].ToString() : "Attack";
                float value = _config.ExtraParams.ContainsKey("Value") ? 
                    (float)_config.ExtraParams["Value"] : 10f;
                
                // u5c06u5b57u7b26u4e32u8f6cu6362u4e3au5c5eu6027u7c7bu578b
                AttributeType attrType = AttributeType.Attack;
                switch (attributeName)
                {
                    case "Attack": attrType = AttributeType.Attack; break;
                    case "Defense": attrType = AttributeType.Defense; break;
                    case "Speed": attrType = AttributeType.Speed; break;
                    case "Critical": attrType = AttributeType.Critical; break;
                    default: attrType = AttributeType.Attack; break;
                }
                
                // u6dfbu52a0u4e34u65f6u5c5eu6027u4feeu9970u7b26
                buffTarget.Attributes.AddModifier(attrType, value, _config.Duration);
                
                Log.Info($"[u6280u80fd] {_owner.Name} u5bf9 {buffTarget.Name} u4f7fu7528u4e86u589eu76cau6280u80fd {_config.Name}, u589eu52a0u4e86 {value} u70b9 {attributeName}");
            }
            
            EndSkill(); // u7acbu5373u7ed3u675fu6280u80fduff0cu56e0u4e3au6548u679cu5df2u7ecfu5e94u7528
        }
    }
    
    /// <summary>
    /// u51cfu76cau6548u679cu6280u80fduff08Debuffuff09
    /// </summary>
    public class DebuffSkill : Skill
    {
        protected override void UseSkill(Entity target)
        {
            _isActive = true;
            
            if (target != null)
            {
                // u6dfbu52a0u51cfu76cau6548u679c
                // u7b80u5316u5b9eu73b0uff0cu76f4u63a5u51cfu5c11u5c5eu6027
                string attributeName = _config.ExtraParams.ContainsKey("AttributeType") ? 
                    _config.ExtraParams["AttributeType"].ToString() : "Defense";
                float value = _config.ExtraParams.ContainsKey("Value") ? 
                    (float)_config.ExtraParams["Value"] : -5f;
                
                // u5c06u5b57u7b26u4e32u8f6cu6362u4e3au5c5eu6027u7c7bu578b
                AttributeType attrType = AttributeType.Defense;
                switch (attributeName)
                {
                    case "Attack": attrType = AttributeType.Attack; break;
                    case "Defense": attrType = AttributeType.Defense; break;
                    case "Speed": attrType = AttributeType.Speed; break;
                    default: attrType = AttributeType.Defense; break;
                }
                
                // u6dfbu52a0u4e34u65f6u5c5eu6027u4feeu9970u7b26uff08u8d1fu503cu8868u793au51cfu5c11uff09
                target.Attributes.AddModifier(attrType, value, _config.Duration);
                
                Log.Info($"[u6280u80fd] {_owner.Name} u5bf9 {target.Name} u4f7fu7528u4e86u51cfu76cau6280u80fd {_config.Name}, u51cfu5c11u4e86 {-value} u70b9 {attributeName}");
            }
            
            EndSkill(); // u7acbu5373u7ed3u675fu6280u80fduff0cu56e0u4e3au6548u679cu5df2u7ecfu5e94u7528
        }
    }
}
