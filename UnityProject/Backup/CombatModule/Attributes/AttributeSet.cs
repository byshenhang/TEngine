using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 属性类型枚举
    /// </summary>
    public enum AttributeType
    {
        // 基础属性
        Strength,       // 力量
        Dexterity,      // 敏捷
        Constitution,   // 体质
        Intelligence,   // 智力
        Wisdom,         // 智慧
        Charisma,       // 魅力
        
        // 战斗属性
        Health,         // 生命值
        Mana,           // 魔法值
        Stamina,        // 耐力值
        Attack,         // 攻击力
        Defense,        // 防御力
        MagicAttack,    // 魔法攻击
        MagicDefense,   // 魔法防御
        CriticalChance, // 暴击几率
        CriticalDamage, // 暴击伤害
        HitRate,        // 命中率
        DodgeRate,      // 闪避率
        MoveSpeed,      // 移动速度
        AttackSpeed,    // 攻击速度
        CastSpeed,      // 施法速度
        
        // 抗性属性
        FireResist,     // 火焰抗性
        IceResist,      // 冰冻抗性
        LightningResist,// 雷电抗性
        PoisonResist,   // 毒素抗性
        StunResist,     // 眩晕抗性
        
        // 特殊属性
        Experience,     // 经验值
        Level,          // 等级
        Gold,           // 金币
    }
    
    /// <summary>
    /// 属性修饰符类型
    /// </summary>
    public enum AttributeModifierType
    {
        Flat,       // 固定值修饰符
        Percent,    // 百分比修饰符
        Final       // 最终修饰符（应用于所有计算之后）
    }
    
    /// <summary>
    /// 属性修饰符
    /// </summary>
    public class AttributeModifier
    {
        // 修饰符唯一ID
        private string _id;
        
        // 修饰符源
        private string _source;
        
        // 属性类型
        private AttributeType _attributeType;
        
        // 修饰符类型
        private AttributeModifierType _modifierType;
        
        // 修饰值
        private float _value;
        
        // 是否为永久修饰符
        private bool _isPermanent;
        
        // 过期时间（如果非永久）
        private float _expirationTime;
        
        /// <summary>
        /// 修饰符唯一ID
        /// </summary>
        public string Id => _id;
        
        /// <summary>
        /// 修饰符源
        /// </summary>
        public string Source => _source;
        
        /// <summary>
        /// 属性类型
        /// </summary>
        public AttributeType AttributeType => _attributeType;
        
        /// <summary>
        /// 修饰符类型
        /// </summary>
        public AttributeModifierType ModifierType => _modifierType;
        
        /// <summary>
        /// 修饰值
        /// </summary>
        public float Value => _value;
        
        /// <summary>
        /// 是否为永久修饰符
        /// </summary>
        public bool IsPermanent => _isPermanent;
        
        /// <summary>
        /// 过期时间（如果非永久）
        /// </summary>
        public float ExpirationTime => _expirationTime;
        
        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired => !_isPermanent && Time.time > _expirationTime;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public AttributeModifier(string id, AttributeType attributeType, AttributeModifierType modifierType, float value, string source = "", bool isPermanent = true, float duration = 0)
        {
            _id = id;
            _attributeType = attributeType;
            _modifierType = modifierType;
            _value = value;
            _source = source;
            _isPermanent = isPermanent;
            
            if (!isPermanent)
            {
                _expirationTime = Time.time + duration;
            }
        }
    }
    
    /// <summary>
    /// 属性集 - 管理实体的所有属性和属性修饰符
    /// </summary>
    public class AttributeSet : IDisposable
    {
        // 基础属性值
        private Dictionary<AttributeType, float> _baseAttributes = new Dictionary<AttributeType, float>();
        
        // 属性修饰符列表，按属性类型分组
        private Dictionary<AttributeType, List<AttributeModifier>> _modifiers = new Dictionary<AttributeType, List<AttributeModifier>>();
        
        // 缓存的计算结果（提高性能）
        private Dictionary<AttributeType, float> _cachedValues = new Dictionary<AttributeType, float>();
        
        // 是否需要重新计算属性
        private HashSet<AttributeType> _dirtyAttributes = new HashSet<AttributeType>();
        
        // 属性变更事件
        public event Action<AttributeType, float, float> OnAttributeChanged;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public AttributeSet()
        {
            // 初始化所有属性的默认值为0
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                _baseAttributes[type] = 0;
                _cachedValues[type] = 0;
                _modifiers[type] = new List<AttributeModifier>();
            }
        }
        
        /// <summary>
        /// 设置基础属性值
        /// </summary>
        public void SetBaseAttributeValue(AttributeType attributeType, float value)
        {
            float oldValue = _baseAttributes[attributeType];
            
            if (Mathf.Approximately(oldValue, value))
            {
                return;
            }
            
            _baseAttributes[attributeType] = value;
            MarkAttributeDirty(attributeType);
            
            Log.Info($"设置基础属性 {attributeType} = {value}");
        }
        
        /// <summary>
        /// 获取基础属性值
        /// </summary>
        public float GetBaseAttributeValue(AttributeType attributeType)
        {
            return _baseAttributes.TryGetValue(attributeType, out float value) ? value : 0f;
        }
        
        /// <summary>
        /// 获取属性最终值（包含所有修饰符）
        /// </summary>
        public float GetAttributeValue(AttributeType attributeType)
        {
            // 检查属性是否需要重新计算
            if (_dirtyAttributes.Contains(attributeType))
            {
                CalculateAttributeValue(attributeType);
                _dirtyAttributes.Remove(attributeType);
            }
            
            return _cachedValues.TryGetValue(attributeType, out float value) ? value : 0f;
        }
        
        /// <summary>
        /// 添加属性修饰符
        /// </summary>
        public void AddModifier(AttributeModifier modifier)
        {
            if (modifier == null) return;
            
            // 检查是否已有相同ID的修饰符
            var existingIndex = _modifiers[modifier.AttributeType].FindIndex(m => m.Id == modifier.Id);
            
            if (existingIndex >= 0)
            {
                // 移除旧的修饰符
                _modifiers[modifier.AttributeType].RemoveAt(existingIndex);
            }
            
            // 添加新修饰符
            _modifiers[modifier.AttributeType].Add(modifier);
            
            // 标记属性为脏，需要重新计算
            MarkAttributeDirty(modifier.AttributeType);
            
            Log.Info($"添加属性修饰符: {modifier.Id} 到 {modifier.AttributeType}，值: {modifier.Value}，类型: {modifier.ModifierType}");
        }
        
        /// <summary>
        /// 移除属性修饰符
        /// </summary>
        public bool RemoveModifier(string modifierId, AttributeType attributeType)
        {
            var modifierList = _modifiers[attributeType];
            int index = modifierList.FindIndex(m => m.Id == modifierId);
            
            if (index >= 0)
            {
                modifierList.RemoveAt(index);
                MarkAttributeDirty(attributeType);
                
                Log.Info($"移除属性修饰符: {modifierId} 从 {attributeType}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 移除来源的所有修饰符
        /// </summary>
        public void RemoveModifiersFromSource(string source)
        {
            foreach (var attributeType in Enum.GetValues(typeof(AttributeType)))
            {
                var attrType = (AttributeType)attributeType;
                var modifierList = _modifiers[attrType];
                
                bool removed = false;
                for (int i = modifierList.Count - 1; i >= 0; i--)
                {
                    if (modifierList[i].Source == source)
                    {
                        modifierList.RemoveAt(i);
                        removed = true;
                    }
                }
                
                if (removed)
                {
                    MarkAttributeDirty(attrType);
                }
            }
            
            Log.Info($"移除所有来源为 {source} 的属性修饰符");
        }
        
        /// <summary>
        /// 清理所有过期的修饰符
        /// </summary>
        public void CleanupExpiredModifiers()
        {
            foreach (var attributeType in Enum.GetValues(typeof(AttributeType)))
            {
                var attrType = (AttributeType)attributeType;
                var modifierList = _modifiers[attrType];
                
                bool removed = false;
                for (int i = modifierList.Count - 1; i >= 0; i--)
                {
                    if (!modifierList[i].IsPermanent && modifierList[i].IsExpired)
                    {
                        Log.Info($"属性修饰符已过期: {modifierList[i].Id} 从 {attrType}");
                        modifierList.RemoveAt(i);
                        removed = true;
                    }
                }
                
                if (removed)
                {
                    MarkAttributeDirty(attrType);
                }
            }
        }
        
        /// <summary>
        /// 计算属性最终值
        /// </summary>
        private void CalculateAttributeValue(AttributeType attributeType)
        {
            float baseValue = _baseAttributes[attributeType];
            float flatBonus = 0f;
            float percentBonus = 0f;
            float finalMultiplier = 1f;
            
            // 首先应用所有修饰符
            foreach (var modifier in _modifiers[attributeType])
            {
                // 忽略过期的修饰符
                if (!modifier.IsPermanent && modifier.IsExpired) continue;
                
                switch (modifier.ModifierType)
                {
                    case AttributeModifierType.Flat:
                        flatBonus += modifier.Value;
                        break;
                    case AttributeModifierType.Percent:
                        percentBonus += modifier.Value;
                        break;
                    case AttributeModifierType.Final:
                        finalMultiplier *= (1 + modifier.Value);
                        break;
                }
            }
            
            // 计算最终值
            // 顺序: 基础值 + 固定加值，然后应用百分比加值，最后应用最终乘数
            float oldValue = _cachedValues[attributeType];
            float newValue = (baseValue + flatBonus) * (1 + percentBonus) * finalMultiplier;
            
            // 确保某些属性不会小于0
            switch (attributeType)
            {
                case AttributeType.Health:
                case AttributeType.Mana:
                case AttributeType.Stamina:
                case AttributeType.Level:
                case AttributeType.Experience:
                case AttributeType.Gold:
                case AttributeType.Attack:
                case AttributeType.Defense:
                case AttributeType.MagicAttack:
                case AttributeType.MagicDefense:
                    newValue = Mathf.Max(0, newValue);
                    break;
            }
            
            // 更新缓存值
            _cachedValues[attributeType] = newValue;
            
            // 如果值发生了变化，触发事件
            if (!Mathf.Approximately(oldValue, newValue))
            {
                Log.Info($"属性 {attributeType} 变更: {oldValue} -> {newValue}");
                OnAttributeChanged?.Invoke(attributeType, oldValue, newValue);
            }
        }
        
        /// <summary>
        /// 标记属性需要重新计算
        /// </summary>
        private void MarkAttributeDirty(AttributeType attributeType)
        {
            _dirtyAttributes.Add(attributeType);
        }
        
        /// <summary>
        /// 标记所有属性需要重新计算
        /// </summary>
        public void MarkAllAttributesDirty()
        {
            foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            {
                _dirtyAttributes.Add(type);
            }
        }
        
        /// <summary>
        /// 计算所有脏属性的值
        /// </summary>
        public void CalculateAllDirtyAttributes()
        {
            foreach (var attributeType in _dirtyAttributes)
            {
                CalculateAttributeValue(attributeType);
            }
            _dirtyAttributes.Clear();
        }
        
        /// <summary>
        /// 更新属性集
        /// </summary>
        public void Update()
        {
            // 清理过期的修饰符
            CleanupExpiredModifiers();
            
            // 计算所有需要更新的属性
            CalculateAllDirtyAttributes();
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            OnAttributeChanged = null;
            _baseAttributes.Clear();
            
            foreach (var list in _modifiers.Values)
            {
                list.Clear();
            }
            _modifiers.Clear();
            
            _cachedValues.Clear();
            _dirtyAttributes.Clear();
        }
    }
}
