using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6218u6597u5b9eu4f53u57fau7c7b
    /// </summary>
    public abstract class CombatEntityBase
    {
        // u5b9eu4f53ID
        public string EntityId { get; protected set; }
        
        // u5b9eu4f53u540du79f0
        public string Name { get; protected set; }
        
        // u5b9eu4f53u7c7bu578b
        public string EntityType { get; protected set; }
        
        // u9635u8425
        public CombatFaction Faction { get; protected set; }
        
        // u4f4du7f6e
        public Vector3 Position { get; protected set; }
        
        // u65cbu8f6c
        public Quaternion Rotation { get; protected set; }
        
        // u5c5eu6027u5b57u5178
        protected Dictionary<AttributeType, float> _attributes = new Dictionary<AttributeType, float>();
        
        // u72b6u6001u6548u679cu5217u8868
        protected List<StatusEffect> _statusEffects = new List<StatusEffect>();
        
        // u6280u80fdu5217u8868
        protected List<string> _skillIds = new List<string>();
        
        // u662fu5426u5904u4e8eu6218u6597u4e2d
        protected bool _isInCombat = false;
        
        // u662fu5426u6d3bu8dc3
        protected bool _isActive = true;
        
        /// <summary>
        /// u662fu5426u6d3bu8dc3
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// u662fu5426u5904u4e8eu6218u6597u4e2d
        /// </summary>
        public bool IsInCombat => _isInCombat;
        
        /// <summary>
        /// u662fu5426u5b58u6d3b
        /// </summary>
        public bool IsAlive => GetAttributeValue(AttributeType.CurrentHealth) > 0;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        protected CombatEntityBase(string entityId, string name, Dictionary<AttributeType, float> attributes)
        {
            EntityId = entityId;
            Name = name;
            EntityType = "base";
            Faction = CombatFaction.Neutral;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            
            InitializeAttributes(attributes);
        }
        
        /// <summary>
        /// u521du59cbu5316u5c5eu6027
        /// </summary>
        protected virtual void InitializeAttributes(Dictionary<AttributeType, float> attributes)
        {
            _attributes.Clear();
            
            if (attributes != null)
            {
                foreach (var kvp in attributes)
                {
                    _attributes[kvp.Key] = kvp.Value;
                }
            }
            
            // u786eu4fddu57fau7840u5c5eu6027u5b58u5728
            if (!_attributes.ContainsKey(AttributeType.MaxHealth))
                _attributes[AttributeType.MaxHealth] = 100;
                
            if (!_attributes.ContainsKey(AttributeType.CurrentHealth))
                _attributes[AttributeType.CurrentHealth] = _attributes[AttributeType.MaxHealth];
                
            if (!_attributes.ContainsKey(AttributeType.MaxMana))
                _attributes[AttributeType.MaxMana] = 100;
                
            if (!_attributes.ContainsKey(AttributeType.CurrentMana))
                _attributes[AttributeType.CurrentMana] = _attributes[AttributeType.MaxMana];
                
            if (!_attributes.ContainsKey(AttributeType.Attack))
                _attributes[AttributeType.Attack] = 10;
                
            if (!_attributes.ContainsKey(AttributeType.Defense))
                _attributes[AttributeType.Defense] = 5;
        }
        
        /// <summary>
        /// u66f4u65b0u5b9eu4f53
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            if (!_isActive) return;
            
            // u66f4u65b0u72b6u6001u6548u679c
            UpdateStatusEffects(deltaTime);
        }
        
        /// <summary>
        /// u66f4u65b0u72b6u6001u6548u679c
        /// </summary>
        protected virtual void UpdateStatusEffects(float deltaTime)
        {
            // u66f4u65b0u5e76u79fbu9664u8fc7u671fu7684u72b6u6001u6548u679c
            for (int i = _statusEffects.Count - 1; i >= 0; i--)
            {
                var effect = _statusEffects[i];
                effect.Update(deltaTime);
                
                if (effect.IsExpired)
                {
                    effect.OnRemove(this);
                    _statusEffects.RemoveAt(i);
                    Log.Info($"u72b6u6001u6548u679c {effect.EffectId} u4eceu5b9eu4f53 {EntityId} u4e0au79fbu9664");
                }
            }
        }
        
        /// <summary>
        /// u8bbeu7f6eu5b9eu4f53u4f4du7f6e
        /// </summary>
        public virtual void SetPosition(Vector3 position)
        {
            Position = position;
        }
        
        /// <summary>
        /// u8bbeu7f6eu5b9eu4f53u65cbu8f6c
        /// </summary>
        public virtual void SetRotation(Quaternion rotation)
        {
            Rotation = rotation;
        }
        
        /// <summary>
        /// u8bbeu7f6eu5b9eu4f53u6d3bu8dc3u72b6u6001
        /// </summary>
        public virtual void SetActive(bool active)
        {
            _isActive = active;
        }
        
        /// <summary>
        /// u8bbeu7f6eu6218u6597u72b6u6001
        /// </summary>
        public virtual void SetInCombat(bool inCombat)
        {
            _isInCombat = inCombat;
        }
        
        /// <summary>
        /// u83b7u53d6u5c5eu6027u503c
        /// </summary>
        public virtual float GetAttributeValue(AttributeType attributeType)
        {
            if (_attributes.TryGetValue(attributeType, out float value))
            {
                return value;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// u8bbeu7f6eu5c5eu6027u503c
        /// </summary>
        public virtual void SetAttributeValue(AttributeType attributeType, float value)
        {
            _attributes[attributeType] = value;
            
            // u786eu4fddu751fu547du503cu548cu6cd5u529bu503cu4e0du8d85u8fc7u6700u5927u503c
            if (attributeType == AttributeType.CurrentHealth)
            {
                float maxHealth = GetAttributeValue(AttributeType.MaxHealth);
                _attributes[AttributeType.CurrentHealth] = Mathf.Clamp(value, 0, maxHealth);
            }
            else if (attributeType == AttributeType.CurrentMana)
            {
                float maxMana = GetAttributeValue(AttributeType.MaxMana);
                _attributes[AttributeType.CurrentMana] = Mathf.Clamp(value, 0, maxMana);
            }
        }
        
        /// <summary>
        /// u6539u53d8u5c5eu6027u503c
        /// </summary>
        public virtual void ModifyAttributeValue(AttributeType attributeType, float delta)
        {
            float currentValue = GetAttributeValue(attributeType);
            SetAttributeValue(attributeType, currentValue + delta);
        }
        
        /// <summary>
        /// u53d7u5230u4f24u5bb3
        /// </summary>
        public virtual void TakeDamage(float damage, CombatEntityBase attacker, bool isCritical = false)
        {
            if (!_isActive || !IsAlive) return;
            
            // u8003u8651u9632u5fa1u529bu964du4f4eu4f24u5bb3
            float defense = GetAttributeValue(AttributeType.Defense);
            float finalDamage = Mathf.Max(1, damage - defense * 0.5f);
            
            // u5e94u7528u4f24u5bb3
            ModifyAttributeValue(AttributeType.CurrentHealth, -finalDamage);
            
            Log.Info($"u5b9eu4f53 {EntityId} u53d7u5230 {finalDamage} u70b9u4f24u5bb3"+ 
                   (isCritical ? "(u66b4u51fb)" : "") + $"uff0cu5269u4f59u751fu547d: {GetAttributeValue(AttributeType.CurrentHealth)}");
            
            // u5982u679cu751fu547du503cu4e3a0uff0cu5904u7406u6b7bu4ea1
            if (GetAttributeValue(AttributeType.CurrentHealth) <= 0)
            {
                OnDeath(attacker);
            }
        }
        
        /// <summary>
        /// u6cbbu7597u5b9eu4f53
        /// </summary>
        public virtual void Heal(float amount, CombatEntityBase healer)
        {
            if (!_isActive || !IsAlive) return;
            
            // u5e94u7528u6cbbu7597
            ModifyAttributeValue(AttributeType.CurrentHealth, amount);
            
            Log.Info($"u5b9eu4f53 {EntityId} u6062u590d {amount} u70b9u751fu547duff0cu5f53u524du751fu547d: {GetAttributeValue(AttributeType.CurrentHealth)}");
        }
        
        /// <summary>
        /// u6d88u8017u6cd5u529b
        /// </summary>
        public virtual bool ConsumeMana(float amount)
        {
            if (!_isActive || !IsAlive) return false;
            
            float currentMana = GetAttributeValue(AttributeType.CurrentMana);
            
            // u5224u65adu6cd5u529bu662fu5426u8db3u591f
            if (currentMana < amount)
            {
                Log.Warning($"u5b9eu4f53 {EntityId} u6cd5u529bu4e0du8db3uff0cu65e0u6cd5u6d88u8017 {amount} u70b9u6cd5u529b");
                return false;
            }
            
            // u6d88u8017u6cd5u529b
            ModifyAttributeValue(AttributeType.CurrentMana, -amount);
            
            Log.Info($"u5b9eu4f53 {EntityId} u6d88u8017 {amount} u70b9u6cd5u529buff0cu5269u4f59u6cd5u529b: {GetAttributeValue(AttributeType.CurrentMana)}");
            return true;
        }
        
        /// <summary>
        /// u6dfbu52a0u72b6u6001u6548u679c
        /// </summary>
        public virtual bool AddStatusEffect(StatusEffect effect)
        {
            if (!_isActive || effect == null) return false;
            
            // u68c0u67e5u662fu5426u5df2u5b58u5728u76f8u540cu7c7bu578bu7684u72b6u6001u6548u679c
            for (int i = 0; i < _statusEffects.Count; i++)
            {
                if (_statusEffects[i].EffectId == effect.EffectId)
                {
                    // u5982u679cu5df2u5b58u5728uff0cu5219u66f4u65b0u6301u7eedu65f6u95f4
                    _statusEffects[i].Refresh(effect.Duration);
                    Log.Info($"u72b6u6001u6548u679c {effect.EffectId} u5728u5b9eu4f53 {EntityId} u4e0au5df2u5237u65b0");
                    return true;
                }
            }
            
            // u6dfbu52a0u65b0u7684u72b6u6001u6548u679c
            _statusEffects.Add(effect);
            effect.OnApply(this);
            
            Log.Info($"u72b6u6001u6548u679c {effect.EffectId} u5df2u6dfbu52a0u5230u5b9eu4f53 {EntityId} u4e0a");
            return true;
        }
        
        /// <summary>
        /// u79fbu9664u72b6u6001u6548u679c
        /// </summary>
        public virtual bool RemoveStatusEffect(string effectId)
        {
            if (!_isActive || string.IsNullOrEmpty(effectId)) return false;
            
            for (int i = 0; i < _statusEffects.Count; i++)
            {
                if (_statusEffects[i].EffectId == effectId)
                {
                    _statusEffects[i].OnRemove(this);
                    _statusEffects.RemoveAt(i);
                    
                    Log.Info($"u72b6u6001u6548u679c {effectId} u4eceu5b9eu4f53 {EntityId} u4e0au79fbu9664");
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// u68c0u67e5u662fu5426u6709u6307u5b9au7684u72b6u6001u6548u679c
        /// </summary>
        public virtual bool HasStatusEffect(string effectId, bool prefixMatch = false)
        {
            if (!_isActive || string.IsNullOrEmpty(effectId)) return false;
            
            foreach (var effect in _statusEffects)
            {
                if (prefixMatch)
                {
                    if (effect.EffectId.StartsWith(effectId))
                    {
                        return true;
                    }
                }
                else
                {
                    if (effect.EffectId == effectId)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// u6dfbu52a0u6280u80fd
        /// </summary>
        public virtual bool AddSkill(string skillId)
        {
            if (!_isActive || string.IsNullOrEmpty(skillId)) return false;
            
            if (!_skillIds.Contains(skillId))
            {
                _skillIds.Add(skillId);
                Log.Info($"u6280u80fd {skillId} u5df2u6dfbu52a0u5230u5b9eu4f53 {EntityId}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// u79fbu9664u6280u80fd
        /// </summary>
        public virtual bool RemoveSkill(string skillId)
        {
            if (!_isActive || string.IsNullOrEmpty(skillId)) return false;
            
            if (_skillIds.Contains(skillId))
            {
                _skillIds.Remove(skillId);
                Log.Info($"u6280u80fd {skillId} u4eceu5b9eu4f53 {EntityId} u79fbu9664");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// u83b7u53d6u6240u6709u6280u80fdID
        /// </summary>
        public virtual List<string> GetAllSkillIds()
        {
            return new List<string>(_skillIds);
        }
        
        /// <summary>
        /// u4f7fu7528u6280u80fd
        /// </summary>
        public virtual async UniTask<bool> UseSkill(string skillId, string targetEntityId)
        {
            if (!_isActive || !IsAlive || string.IsNullOrEmpty(skillId)) return false;
            
            // u68c0u67e5u662fu5426u62e5u6709u8be5u6280u80fd
            if (!_skillIds.Contains(skillId))
            {
                Log.Warning($"u5b9eu4f53 {EntityId} u4e0du62e5u6709u6280u80fd {skillId}");
                return false;
            }
            
            // u8c03u7528u6218u6597u6a21u5757u4f7fu7528u6280u80fd
            bool result = await GameModule.Combat.UseSkill(EntityId, skillId, targetEntityId);
            return result;
        }
        
        /// <summary>
        /// u5904u7406u6b7bu4ea1
        /// </summary>
        protected virtual void OnDeath(CombatEntityBase killer)
        {
            if (!_isActive) return;
            
            // u6e05u9664u6240u6709u72b6u6001u6548u679c
            for (int i = _statusEffects.Count - 1; i >= 0; i--)
            {
                _statusEffects[i].OnRemove(this);
            }
            _statusEffects.Clear();
            
            Log.Info($"u5b9eu4f53 {EntityId} u6b7bu4ea1" + (killer != null ? $"uff0cu51fbu6740u8005: {killer.EntityId}" : ""));
        }
    }
    
    /// <summary>
    /// u72b6u6001u6548u679cu57fau7c7b
    /// </summary>
    public class StatusEffect
    {
        // u6548u679cID
        public string EffectId { get; protected set; }
        
        // u6301u7eedu65f6u95f4
        public float Duration { get; protected set; }
        
        // u5f53u524du8fd0u884cu65f6u95f4
        protected float _currentTime = 0f;
        
        // u662fu5426u8fc7u671f
        public bool IsExpired => _currentTime >= Duration;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public StatusEffect(string effectId, float duration)
        {
            EffectId = effectId;
            Duration = duration;
            _currentTime = 0f;
        }
        
        /// <summary>
        /// u66f4u65b0u6548u679c
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            _currentTime += deltaTime;
        }
        
        /// <summary>
        /// u5e94u7528u6548u679c
        /// </summary>
        public virtual void OnApply(CombatEntityBase target)
        {
        }
        
        /// <summary>
        /// u79fbu9664u6548u679c
        /// </summary>
        public virtual void OnRemove(CombatEntityBase target)
        {
        }
        
        /// <summary>
        /// u5237u65b0u6301u7eedu65f6u95f4
        /// </summary>
        public virtual void Refresh(float newDuration)
        {
            Duration = newDuration;
            _currentTime = 0f;
        }
    }
}
