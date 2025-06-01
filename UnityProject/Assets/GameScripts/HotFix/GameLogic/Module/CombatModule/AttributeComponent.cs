using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u5c5eu6027u7ec4u4ef6 - u7ba1u7406u5b9eu4f53u5c5eu6027u548cu5c5eu6027u4feeu9970u7b26
    /// </summary>
    public class AttributeComponent
    {
        private Dictionary<AttributeType, float> _baseAttributes = new Dictionary<AttributeType, float>();
        private Dictionary<AttributeType, float> _currentAttributes = new Dictionary<AttributeType, float>();
        private List<AttributeModifier> _modifiers = new List<AttributeModifier>();
        
        /// <summary>
        /// u521du59cbu5316u5c5eu6027u7ec4u4ef6
        /// </summary>
        public void Init(Dictionary<AttributeType, float> baseAttributes)
        {
            _baseAttributes = new Dictionary<AttributeType, float>(baseAttributes);
            
            // u521du59cbu5316u5f53u524du5c5eu6027u503c
            foreach (var kvp in _baseAttributes)
            {
                _currentAttributes[kvp.Key] = kvp.Value;
            }
        }
        
        /// <summary>
        /// u83b7u53d6u5f53u524du5c5eu6027u503c
        /// </summary>
        public float GetAttribute(AttributeType type)
        {
            if (_currentAttributes.TryGetValue(type, out float value))
            {
                return value;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// u83b7u53d6u57fau7840u5c5eu6027u503c
        /// </summary>
        public float GetBaseAttribute(AttributeType type)
        {
            if (_baseAttributes.TryGetValue(type, out float value))
            {
                return value;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// u8bbeu7f6eu57fau7840u5c5eu6027u503c
        /// </summary>
        public void SetBaseAttribute(AttributeType type, float value)
        {
            _baseAttributes[type] = value;
            RecalculateAttributes();
        }
        
        /// <summary>
        /// u76f4u63a5u8bbeu7f6eu5f53u524du5c5eu6027u503cuff08u4e0du5f71u54cdu57fau7840u503cuff09
        /// </summary>
        public void SetCurrentAttribute(AttributeType type, float value)
        {
            _currentAttributes[type] = value;
        }
        
        /// <summary>
        /// u6dfbu52a0u5c5eu6027u4feeu9970u7b26
        /// </summary>
        public void AddModifier(AttributeModifier modifier)
        {
            _modifiers.Add(modifier);
            RecalculateAttributes();
        }
        
        /// <summary>
        /// u6dfbu52a0u5c5eu6027u4feeu9970u7b26 - u4fbfu5229u65b9u6cd5
        /// </summary>
        public void AddModifier(AttributeType attributeType, float value, float duration = 0)
        {
            // u9ed8u8ba4u4f7fu7528u52a0u6cd5u4feeu9970u7b26
            AttributeModifier modifier = new AttributeModifier(attributeType, ModifierType.Additive, value, duration);
            AddModifier(modifier);
        }
        
        /// <summary>
        /// u79fbu9664u5c5eu6027u4feeu9970u7b26
        /// </summary>
        public void RemoveModifier(AttributeModifier modifier)
        {
            _modifiers.Remove(modifier);
            RecalculateAttributes();
        }
        
        /// <summary>
        /// u79fbu9664u5c5eu6027u4feeu9970u7b26 - u6839u636eu5c5eu6027u7c7bu578bu548cu503cu79fbu9664
        /// </summary>
        public void RemoveModifier(AttributeType attributeType, float value)
        {
            // u67e5u627eu5339u914du7684u4feeu9970u7b26
            AttributeModifier modifierToRemove = null;
            
            foreach (var modifier in _modifiers)
            {
                if (modifier.AttributeType == attributeType && Mathf.Approximately(modifier.Value, value))
                {
                    modifierToRemove = modifier;
                    break;
                }
            }
            
            // u5982u679cu627eu5230u5339u914du7684u4feeu9970u7b26uff0cu5219u79fbu9664
            if (modifierToRemove != null)
            {
                RemoveModifier(modifierToRemove);
            }
        }
        
        /// <summary>
        /// u66f4u65b0u5c5eu6027u4feeu9970u7b26
        /// </summary>
        public void UpdateModifiers(float deltaTime)
        {
            bool needRecalculate = false;
            
            // u79fbu9664u8fc7u671fu7684u4feeu9970u7b26
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (!_modifiers[i].Update(deltaTime))
                {
                    _modifiers.RemoveAt(i);
                    needRecalculate = true;
                }
            }
            
            if (needRecalculate)
            {
                RecalculateAttributes();
            }
        }
        
        /// <summary>
        /// u91cdu65b0u8ba1u7b97u5c5eu6027u503c
        /// </summary>
        private void RecalculateAttributes()
        {
            // u4eceu57fau7840u5c5eu6027u5f00u59cb
            foreach (var kvp in _baseAttributes)
            {
                _currentAttributes[kvp.Key] = kvp.Value;
            }
            
            // u9996u5148u8ba1u7b97u6240u6709u52a0u6cd5u4feeu9970u7b26
            foreach (var modifier in _modifiers)
            {
                if (modifier.ModifierType == ModifierType.Add && _currentAttributes.TryGetValue(modifier.AttributeType, out float currentValue))
                {
                    _currentAttributes[modifier.AttributeType] = currentValue + modifier.Value;
                }
            }
            
            // u7136u540eu8ba1u7b97u6240u6709u4e58u6cd5u4feeu9970u7b26
            foreach (var modifier in _modifiers)
            {
                if (modifier.ModifierType == ModifierType.Multiply && _currentAttributes.TryGetValue(modifier.AttributeType, out float currentValue))
                {
                    _currentAttributes[modifier.AttributeType] = currentValue * modifier.Value;
                }
            }
            
            // u6700u540eu8ba1u7b97u6240u6709u8986u76d6u4feeu9970u7b26
            foreach (var modifier in _modifiers)
            {
                if (modifier.ModifierType == ModifierType.Override)
                {
                    _currentAttributes[modifier.AttributeType] = modifier.Value;
                }
            }
        }
    }
    
    /// <summary>
    /// u5c5eu6027u4feeu9970u7b26 - u7528u4e8eu4feeu6539u5b9eu4f53u5c5eu6027
    /// </summary>
    public class AttributeModifier
    {
        /// <summary>
        /// u5c5eu6027u7c7bu578b
        /// </summary>
        public AttributeType AttributeType { get; private set; }
        
        /// <summary>
        /// u4feeu9970u7b26u7c7bu578b
        /// </summary>
        public ModifierType ModifierType { get; private set; }
        
        /// <summary>
        /// u4feeu9970u7b26u503c
        /// </summary>
        public float Value { get; private set; }
        
        /// <summary>
        /// u6301u7eedu65f6u95f4
        /// </summary>
        public float Duration { get; private set; }
        
        /// <summary>
        /// u5269u4f59u65f6u95f4
        /// </summary>
        public float RemainingTime { get; private set; }
        
        /// <summary>
        /// u521bu5efau5c5eu6027u4feeu9970u7b26
        /// </summary>
        public AttributeModifier(AttributeType type, ModifierType modifierType, float value, float duration = 0)
        {
            AttributeType = type;
            ModifierType = modifierType;
            Value = value;
            Duration = duration;
            RemainingTime = duration;
        }
        
        /// <summary>
        /// u66f4u65b0u4feeu9970u7b26u65f6u95f4
        /// </summary>
        /// <returns>u8fd4u56deu4feeu9970u7b26u662fu5426u4ecdu7136u6709u6548</returns>
        public bool Update(float deltaTime)
        {
            if (Duration <= 0)
            {
                return true; // u6c38u4e45u4feeu9970u7b26
            }
            
            RemainingTime -= deltaTime;
            return RemainingTime > 0;
        }
    }
}
