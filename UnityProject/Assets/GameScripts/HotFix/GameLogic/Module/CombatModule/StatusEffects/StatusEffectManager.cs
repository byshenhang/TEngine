using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u72b6u6001u6548u679cu7ba1u7406u5668 - u7ba1u7406u6240u6709u5b9eu4f53u7684u72b6u6001u6548u679c
    /// </summary>
    public class StatusEffectManager
    {
        // u5b9eu4f53u548cu72b6u6001u6548u679cu7684u6620u5c04
        private Dictionary<Entity, List<StatusEffect>> _entityEffects = new Dictionary<Entity, List<StatusEffect>>();
        
        // u4e0bu4e00u4e2au6548u679cID
        private int _nextEffectId = 1;
        
        /// <summary>
        /// u521du59cbu5316u72b6u6001u6548u679cu7ba1u7406u5668
        /// </summary>
        public void Initialize()
        {
            _entityEffects.Clear();
            _nextEffectId = 1;
            
            Log.Info("[StatusEffectManager] u521du59cbu5316u5b8cu6210");
        }
        
        /// <summary>
        /// u66f4u65b0u6240u6709u72b6u6001u6548u679c
        /// </summary>
        public void Update(float deltaTime)
        {
            List<Entity> entitiesToClean = new List<Entity>();
            List<StatusEffect> effectsToRemove = new List<StatusEffect>();
            
            // u904du5386u6240u6709u5b9eu4f53u548cu72b6u6001u6548u679c
            foreach (var entityEffectPair in _entityEffects)
            {
                Entity entity = entityEffectPair.Key;
                List<StatusEffect> effects = entityEffectPair.Value;
                
                // u8df3u8fc7u5df2u79fbu9664u7684u5b9eu4f53
                if (entity == null || entity.CurrentState == EntityStateType.Dead)
                {
                    entitiesToClean.Add(entity);
                    continue;
                }
                
                // u66f4u65b0u6bcfu4e2au6548u679c
                effectsToRemove.Clear();
                foreach (var effect in effects)
                {
                    effect.Update(deltaTime);
                    
                    // u5982u679cu6548u679cu8fc7u671fu6216u5e94u8be5u88abu79fbu9664uff0cu6dfbu52a0u5230u5f85u79fbu9664u5217u8868
                    if (effect.Duration > 0 && effect.RemainingTime <= 0)
                    {
                        effectsToRemove.Add(effect);
                    }
                }
                
                // u79fbu9664u8fc7u671fu7684u6548u679c
                foreach (var effect in effectsToRemove)
                {
                    RemoveEffect(entity, effect.EffectId);
                }
            }
            
            // u6e05u7406u5df2u79fbu9664u7684u5b9eu4f53
            foreach (var entity in entitiesToClean)
            {
                _entityEffects.Remove(entity);
            }
        }
        
        /// <summary>
        /// u5e94u7528u5c5eu6027u4feeu6539u5668u72b6u6001u6548u679c
        /// </summary>
        public StatusEffect ApplyAttributeModifier(Entity source, Entity target, AttributeType attributeType, float value, float duration)
        {
            // u521bu5efau65b0u7684u5c5eu6027u4feeu6539u5668u6548u679c
            AttributeModifierEffect effect = new AttributeModifierEffect(
                _nextEffectId++, source, target, attributeType, value, duration);
            
            // u6dfbu52a0u5230u76eeu6807u5b9eu4f53
            AddEffectToEntity(target, effect);
            
            return effect;
        }
        
        /// <summary>
        /// u5e94u7528u6301u7eedu4f24u5bb3u72b6u6001u6548u679c
        /// </summary>
        public StatusEffect ApplyDamageOverTime(Entity source, Entity target, float damagePerTick, float duration, float tickInterval, DamageType damageType)
        {
            // u521bu5efau65b0u7684u6301u7eedu4f24u5bb3u6548u679c
            DamageOverTimeEffect effect = new DamageOverTimeEffect(
                _nextEffectId++, source, target, damagePerTick, duration, tickInterval, damageType);
            
            // u6dfbu52a0u5230u76eeu6807u5b9eu4f53
            AddEffectToEntity(target, effect);
            
            return effect;
        }
        
        /// <summary>
        /// u6dfbu52a0u6548u679cu5230u5b9eu4f53
        /// </summary>
        private void AddEffectToEntity(Entity entity, StatusEffect newEffect)
        {
            if (entity == null || entity.CurrentState == EntityStateType.Dead)
            {
                return;
            }
            
            // u786eu4fddu5b9eu4f53u5728u5b57u5178u4e2d
            if (!_entityEffects.TryGetValue(entity, out List<StatusEffect> effects))
            {
                effects = new List<StatusEffect>();
                _entityEffects[entity] = effects;
            }
            
            // u68c0u67e5u73b0u6709u76f8u540cu7c7bu578bu7684u6548u679cu5e76u5e94u7528u5806u53e0u7b56u7565
            StatusEffect existingEffect = effects.Find(e => e.GetType() == newEffect.GetType() && e.EffectId != newEffect.EffectId);
            
            if (existingEffect != null)
            {
                // u6839u636eu5806u53e0u7b56u7565u5904u7406
                switch (existingEffect.StackPolicy)
                {
                    case StackingPolicy.None:
                        // u79fbu9664u65e7u6548u679cuff0cu6dfbu52a0u65b0u6548u679c
                        RemoveEffect(entity, existingEffect.EffectId);
                        effects.Add(newEffect);
                        newEffect.Initialize();
                        break;
                    
                    case StackingPolicy.Stack:
                        // u5982u679cu53efu4ee5u5806u53e0uff0cu589eu52a0u5806u53e0u5c42u6570
                        if (existingEffect.TryAddStack())
                        {
                            // u5237u65b0u6301u7eedu65f6u95f4
                            if (newEffect.Duration > 0)
                            {
                                existingEffect.RefreshDuration(newEffect.Duration);
                            }
                        }
                        else
                        {
                            // u5982u679cu4e0du80fdu518du5806u53e0uff0cu6dfbu52a0u65b0u6548u679c
                            effects.Add(newEffect);
                            newEffect.Initialize();
                        }
                        break;
                    
                    case StackingPolicy.Refresh:
                        // u53eau5237u65b0u6301u7eedu65f6u95f4
                        if (newEffect.Duration > 0)
                        {
                            existingEffect.RefreshDuration(newEffect.Duration);
                        }
                        break;
                    
                    case StackingPolicy.TakeStrongest:
                        // u6bd4u8f83u5f3au5ea6uff0cu9009u62e9u6700u5f3au7684u6548u679c
                        if (newEffect.Magnitude > existingEffect.Magnitude)
                        {
                            RemoveEffect(entity, existingEffect.EffectId);
                            effects.Add(newEffect);
                            newEffect.Initialize();
                        }
                        break;
                    
                    case StackingPolicy.TakeMostRecent:
                        // u59bfu91cdu65b0u6548u679c
                        RemoveEffect(entity, existingEffect.EffectId);
                        effects.Add(newEffect);
                        newEffect.Initialize();
                        break;
                }
            }
            else
            {
                // u6ca1u6709u73b0u6709u76f8u540cu7c7bu578bu7684u6548u679cuff0cu76f4u63a5u6dfbu52a0
                effects.Add(newEffect);
                newEffect.Initialize();
            }
        }
        
        /// <summary>
        /// u79fbu9664u6307u5b9au5b9eu4f53u7684u6307u5b9aID u72b6u6001u6548u679c
        /// </summary>
        public bool RemoveEffect(Entity entity, int effectId)
        {
            if (entity == null || !_entityEffects.TryGetValue(entity, out List<StatusEffect> effects))
            {
                return false;
            }
            
            // u67e5u627eu5e76u79fbu9664u6307u5b9au6548u679c
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].EffectId == effectId)
                {
                    // u8c03u7528u6548u679cu7684u79fbu9664u65b9u6cd5
                    effects[i].Remove();
                    
                    // u4eceu5217u8868u4e2du79fbu9664
                    effects.RemoveAt(i);
                    
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// u79fbu9664u6307u5b9au5b9eu4f53u7684u6240u6709u72b6u6001u6548u679c
        /// </summary>
        public void RemoveAllEffects(Entity entity)
        {
            if (entity == null || !_entityEffects.TryGetValue(entity, out List<StatusEffect> effects))
            {
                return;
            }
            
            // u79fbu9664u6bcfu4e2au6548u679c
            foreach (var effect in effects)
            {
                effect.Remove();
            }
            
            // u6e05u7406u5217u8868
            effects.Clear();
        }
        
        /// <summary>
        /// u83b7u53d6u6307u5b9au5b9eu4f53u7684u6240u6709u72b6u6001u6548u679c
        /// </summary>
        public List<StatusEffect> GetEntityEffects(Entity entity)
        {
            if (entity == null || !_entityEffects.TryGetValue(entity, out List<StatusEffect> effects))
            {
                return new List<StatusEffect>();
            }
            
            return new List<StatusEffect>(effects);
        }
        
        /// <summary>
        /// u83b7u53d6u6307u5b9au5b9eu4f53u7684u6307u5b9au7c7bu578bu72b6u6001u6548u679c
        /// </summary>
        public List<StatusEffect> GetEntityEffectsByType(Entity entity, StatusEffectType type)
        {
            List<StatusEffect> result = new List<StatusEffect>();
            
            if (entity == null || !_entityEffects.TryGetValue(entity, out List<StatusEffect> effects))
            {
                return result;
            }
            
            // u8fc7u6ee4u6307u5b9au7c7bu578bu7684u6548u679c
            foreach (var effect in effects)
            {
                if (effect.Type == type)
                {
                    result.Add(effect);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// u6e05u7406u7ba1u7406u5668
        /// </summary>
        public void Clear()
        {
            // u79fbu9664u6240u6709u5b9eu4f53u7684u6240u6709u6548u679c
            foreach (var entityEffectPair in _entityEffects)
            {
                Entity entity = entityEffectPair.Key;
                RemoveAllEffects(entity);
            }
            
            // u6e05u7406u5b57u5178
            _entityEffects.Clear();
            _nextEffectId = 1;
        }
    }
}
