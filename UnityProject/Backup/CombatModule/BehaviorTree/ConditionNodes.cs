using System;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6761u4ef6u8282u70b9 - u8bc4u4f30u6761u4ef6u7684u8282u70b9
    /// </summary>
    public abstract class ConditionNode : BehaviorTreeNode
    {
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public ConditionNode(string name, string description = "") 
            : base(name, description)
        {
        }
    }
    
    /// <summary>
    /// u901au7528u6761u4ef6u8282u70b9 - u4f7fu7528u81eau5b9au4e49u51fdu6570u8bc4u4f30u6761u4ef6
    /// </summary>
    public class GenericConditionNode : ConditionNode
    {
        // u6761u4ef6u51fdu6570
        private Func<CombatEntityBase, bool> _condition;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public GenericConditionNode(string name, Func<CombatEntityBase, bool> condition, string description = "") 
            : base(name, description)
        {
            _condition = condition;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            // u6267u884cu6761u4ef6u51fdu6570u5e76u8fd4u56deu7ed3u679c
            bool result = _condition?.Invoke(entity) ?? false;
            _status = result ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
            return _status;
        }
    }
    
    /// <summary>
    /// u662fu5426u5728u6218u6597u4e2du6761u4ef6
    /// </summary>
    public class IsInCombatCondition : ConditionNode
    {
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public IsInCombatCondition(string name = "IsInCombat", string description = "u68c0u67e5u662fu5426u5728u6218u6597u4e2d") 
            : base(name, description)
        {
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            _status = entity.IsInCombat ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
            return _status;
        }
    }
    
    /// <summary>
    /// u751fu547du503cu6761u4ef6
    /// </summary>
    public class HealthCondition : ConditionNode
    {
        // u751fu547du503cu767eu5206u6bd4u9608u503c
        private float _healthPercentage;
        
        // u6bd4u8f83u7c7bu578b
        private ComparisonType _comparisonType;
        
        /// <summary>
        /// u6bd4u8f83u7c7bu578b
        /// </summary>
        public enum ComparisonType
        {
            LessThan,       // u5c0fu4e8e
            LessThanOrEqual, // u5c0fu4e8eu7b49u4e8e
            Equal,          // u7b49u4e8e
            GreaterThanOrEqual, // u5927u4e8eu7b49u4e8e
            GreaterThan     // u5927u4e8e
        }
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public HealthCondition(string name, float healthPercentage, ComparisonType comparisonType, string description = "") 
            : base(name, description)
        {
            _healthPercentage = Mathf.Clamp01(healthPercentage); // u786eu4fddu5728 0-1 u4e4bu95f4
            _comparisonType = comparisonType;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            float currentHealthPercentage = entity.HealthPercentage;
            bool result = false;
            
            switch (_comparisonType)
            {
                case ComparisonType.LessThan:
                    result = currentHealthPercentage < _healthPercentage;
                    break;
                case ComparisonType.LessThanOrEqual:
                    result = currentHealthPercentage <= _healthPercentage;
                    break;
                case ComparisonType.Equal:
                    result = Mathf.Approximately(currentHealthPercentage, _healthPercentage);
                    break;
                case ComparisonType.GreaterThanOrEqual:
                    result = currentHealthPercentage >= _healthPercentage;
                    break;
                case ComparisonType.GreaterThan:
                    result = currentHealthPercentage > _healthPercentage;
                    break;
            }
            
            _status = result ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
            return _status;
        }
    }
    
    /// <summary>
    /// u8dddu79bbu76eeu6807u6761u4ef6
    /// </summary>
    public class DistanceToTargetCondition : ConditionNode
    {
        // u76eeu6807u9009u62e9u5668u51fdu6570
        private Func<CombatEntityBase, CombatEntityBase> _targetSelector;
        
        // u8dddu79bbu9608u503c
        private float _distance;
        
        // u6bd4u8f83u7c7bu578b
        private HealthCondition.ComparisonType _comparisonType;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public DistanceToTargetCondition(string name, Func<CombatEntityBase, CombatEntityBase> targetSelector, 
            float distance, HealthCondition.ComparisonType comparisonType, string description = "") 
            : base(name, description)
        {
            _targetSelector = targetSelector;
            _distance = distance;
            _comparisonType = comparisonType;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            // u83b7u53d6u76eeu6807
            var target = _targetSelector?.Invoke(entity);
            if (target == null || !target.IsAlive)
            {
                _status = BehaviorNodeStatus.Failure;
                return _status;
            }
            
            // u83b7u53d6u5b9eu4f53u548cu76eeu6807u7684u4f4du7f6e
            Vector3 entityPosition = Vector3.zero;
            Vector3 targetPosition = Vector3.zero;
            
            if (entity.GameObject != null)
            {
                entityPosition = entity.GameObject.transform.position;
            }
            
            if (target.GameObject != null)
            {
                targetPosition = target.GameObject.transform.position;
            }
            
            // u8ba1u7b97u8dddu79bb
            float currentDistance = Vector3.Distance(entityPosition, targetPosition);
            bool result = false;
            
            switch (_comparisonType)
            {
                case HealthCondition.ComparisonType.LessThan:
                    result = currentDistance < _distance;
                    break;
                case HealthCondition.ComparisonType.LessThanOrEqual:
                    result = currentDistance <= _distance;
                    break;
                case HealthCondition.ComparisonType.Equal:
                    result = Mathf.Approximately(currentDistance, _distance);
                    break;
                case HealthCondition.ComparisonType.GreaterThanOrEqual:
                    result = currentDistance >= _distance;
                    break;
                case HealthCondition.ComparisonType.GreaterThan:
                    result = currentDistance > _distance;
                    break;
            }
            
            _status = result ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
            return _status;
        }
    }
    
    /// <summary>
    /// u68c0u67e5u72b6u6001u6548u679cu6761u4ef6
    /// </summary>
    public class HasStatusEffectCondition : ConditionNode
    {
        // u72b6u6001u6548u679c ID u6216u524du7f00
        private string _effectId;
        
        // u662fu5426u4f7fu7528u524du7f00u5339u914d
        private bool _usePrefixMatch;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public HasStatusEffectCondition(string name, string effectId, bool usePrefixMatch = false, string description = "") 
            : base(name, description)
        {
            _effectId = effectId;
            _usePrefixMatch = usePrefixMatch;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            bool hasEffect = false;
            
            if (_usePrefixMatch)
            {
                // u68c0u67e5u662fu5426u6709u4ee5u6307u5b9au524du7f00u5f00u5934u7684u72b6u6001u6548u679c
                var allEffects = entity.GetAllStatusEffects();
                foreach (var effect in allEffects)
                {
                    if (effect.EffectId.StartsWith(_effectId))
                    {
                        hasEffect = true;
                        break;
                    }
                }
            }
            else
            {
                // u7cbeu786eu5339u914du72b6u6001u6548u679c ID
                hasEffect = entity.HasStatusEffect(_effectId);
            }
            
            _status = hasEffect ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
            return _status;
        }
    }
    
    /// <summary>
    /// u6280u80fdu51b7u5374u5b8cu6210u6761u4ef6
    /// </summary>
    public class SkillCooldownCondition : ConditionNode
    {
        // u6280u80fd ID
        private string _skillId;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public SkillCooldownCondition(string name, string skillId, string description = "") 
            : base(name, description)
        {
            _skillId = skillId;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            // u68c0u67e5u5b9eu4f53u662fu5426u6709u8be5u6280u80fd
            if (!entity.HasSkill(_skillId))
            {
                _status = BehaviorNodeStatus.Failure;
                return _status;
            }
            
            // u8fd9u91ccu5e94u8be5u8c03u7528u6280u80fdu7ba1u7406u5668u68c0u67e5u6280u80fdu51b7u5374u72b6u6001
            // u5f53u524du6211u4eecu7b80u5316u5904u7406uff0cu5047u8bbeu6280u80fdu53efu7528
            bool isCooldownComplete = true;
            
            // u5b9eu9645u5b9eu73b0u4e2du5e94u8be5u8c03u7528u6280u80fdu7ba1u7406u5668
            // isCooldownComplete = GameModule.Combat.SkillManager.IsSkillReady(entity.EntityId, _skillId);
            
            _status = isCooldownComplete ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
            return _status;
        }
    }
    
    /// <summary>
    /// u53cdu8f6cu8282u70b9 - u53cdu8f6cu5b50u8282u70b9u7684u7ed3u679c
    /// </summary>
    public class InverterNode : BehaviorTreeNode
    {
        // u88abu53cdu8f6cu7684u5b50u8282u70b9
        private BehaviorTreeNode _child;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public InverterNode(string name, BehaviorTreeNode child, string description = "") 
            : base(name, description)
        {
            _child = child;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            // u6ca1u6709u5b50u8282u70b9uff0cu8fd4u56deu5931u8d25
            if (_child == null)
            {
                _status = BehaviorNodeStatus.Failure;
                return _status;
            }
            
            // u6267u884cu5b50u8282u70b9
            var childStatus = _child.Update(entity);
            
            // u53cdu8f6cu7ed3u679c
            switch (childStatus)
            {
                case BehaviorNodeStatus.Success:
                    _status = BehaviorNodeStatus.Failure;
                    break;
                case BehaviorNodeStatus.Failure:
                    _status = BehaviorNodeStatus.Success;
                    break;
                default:
                    _status = childStatus; // Running u72b6u6001u4fddu6301u4e0du53d8
                    break;
            }
            
            return _status;
        }
        
        public override void Reset()
        {
            base.Reset();
            _child?.Reset();
        }
    }
}
