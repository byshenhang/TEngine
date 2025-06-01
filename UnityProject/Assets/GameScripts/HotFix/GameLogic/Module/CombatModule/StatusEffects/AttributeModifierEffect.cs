using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u5c5eu6027u4feeu6539u72b6u6001u6548u679c - u7528u4e8eu589eu52a0u6216u51cfu5c11u5b9eu4f53u5c5eu6027
    /// </summary>
    public class AttributeModifierEffect : StatusEffect
    {
        /// <summary>
        /// u76eeu6807u5c5eu6027u7c7bu578b
        /// </summary>
        private AttributeType _attributeType;
        
        /// <summary>
        /// u4feeu6539u503c
        /// </summary>
        private float _modifierValue;
        
        /// <summary>
        /// u662fu5426u5df2u5e94u7528
        /// </summary>
        private bool _isApplied;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public AttributeModifierEffect(int effectId, Entity source, Entity target, AttributeType attributeType, float modifierValue, float duration) 
            : base(effectId, source, target, duration, Mathf.Abs(modifierValue))
        {
            _attributeType = attributeType;
            _modifierValue = modifierValue;
            _isApplied = false;
            
            // u8bbeu7f6eu57fau672cu5c5eu6027
            Type = StatusEffectType.AttributeModifier;
            IsPositive = _modifierValue > 0;
            Name = IsPositive ? $"{_attributeType} Buff" : $"{_attributeType} Debuff";
            Description = IsPositive 
                ? $"Increases {_attributeType} by {_modifierValue}" 
                : $"Decreases {_attributeType} by {Mathf.Abs(_modifierValue)}";
            
            // u8bbeu7f6eu5806u53e0u7b56u7565
            StackPolicy = StackingPolicy.Stack;
            MaxStacks = 5;
            
            // u8bbeu7f6eu56feu6807
            IconPath = IsPositive ? "Icons/Buffs/AttributeBuff" : "Icons/Debuffs/AttributeDebuff";
            VfxPath = IsPositive ? "Effects/Buffs/AttributeBuff" : "Effects/Debuffs/AttributeDebuff";
        }
        
        /// <summary>
        /// u5f53u6548u679cu88abu5e94u7528u65f6
        /// </summary>
        protected override void OnApply()
        {
            if (!_isApplied && Target != null)
            {
                // u7ed9u76eeu6807u6dfbu52a0u5c5eu6027u4feeu6539
                Target.Attributes.AddModifier(_attributeType, _modifierValue, Duration);
                _isApplied = true;
                
                // u65e5u5fd7
                Log.Info($"[StatusEffect] {Name} applied to {Target.Name}. Value: {_modifierValue}, Duration: {Duration}s");
            }
        }
        
        /// <summary>
        /// u5f53u589eu52a0u5806u53e0u65f6
        /// </summary>
        protected override void OnStackAdded()
        {
            if (Target != null)
            {
                // u6bcfu5c42u5806u53e0u589eu52a0u4e00u5b9au767eu5206u6bd4u7684u6548u679c
                float stackBonus = _modifierValue * 0.5f; // 50%u7684u539fu59cbu6548u679c
                Target.Attributes.AddModifier(_attributeType, stackBonus, Duration);
                
                // u8bbeu7f6eu603bu5f3au5ea6
                Magnitude = Mathf.Abs(_modifierValue + (stackBonus * (CurrentStacks - 1)));
                
                // u65e5u5fd7
                Log.Info($"[StatusEffect] {Name} on {Target.Name} gained a stack ({CurrentStacks}/{MaxStacks}). Added: {stackBonus}");
            }
        }
        
        /// <summary>
        /// u5f53u6548u679cu5230u671fu65f6
        /// </summary>
        protected override void OnExpire()
        {
            if (_isApplied && Target != null)
            {
                // u79fbu9664u6240u6709u5806u53e0u5c42u6570u7684u5c5eu6027u4feeu6539
                float totalValue = _modifierValue;
                if (CurrentStacks > 1)
                {
                    totalValue += _modifierValue * 0.5f * (CurrentStacks - 1);
                }
                
                // u79fbu9664u5c5eu6027u4feeu6539
                Target.Attributes.RemoveModifier(_attributeType, totalValue);
                _isApplied = false;
                
                // u65e5u5fd7
                Log.Info($"[StatusEffect] {Name} expired on {Target.Name}. Total removed: {totalValue}");
            }
        }
        
        /// <summary>
        /// u5f53u6548u679cu88abu79fbu9664u65f6
        /// </summary>
        protected override void OnRemove()
        {
            // u8c03u7528u8fc7u671fu903bu8f91u6765u79fbu9664u5c5eu6027u4feeu6539
            OnExpire();
        }
    }
}
