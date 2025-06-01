using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u72b6u6001u6548u679cu7c7bu578b
    /// </summary>
    public enum StatusEffectType
    {
        None = 0,
        AttributeModifier = 1,  // u5c5eu6027u4feeu6539u5668
        DamageOverTime = 2,     // u6301u7eedu4f24u5bb3
        HealOverTime = 3,       // u6301u7eedu6cbbu7597
        MovementModifier = 4,   // u79fbu52a8u901fu5ea6u4feeu6539
        CrowdControl = 5,       // u63a7u5236u6548u679cuff08u6655u773cu3001u51bbu7ed3u7b49uff09
        Special = 6              // u7279u6b8au6548u679c
    }
    
    /// <summary>
    /// u72b6u6001u6548u679cu5806u53e0u7b56u7565
    /// </summary>
    public enum StackingPolicy
    {
        None = 0,              // u4e0du5806u53e0uff0cu65b0u6548u679cu66ffu6362u65e7u6548u679c
        Stack = 1,             // u5b8cu5168u5806u53e0uff0cu65b0u6548u679cu548cu65e7u6548u679cu540cu65f6u5b58u5728
        Refresh = 2,           // u5237u65b0u6301u7eedu65f6u95f4uff0cu4fddu7559u65e7u6548u679cu7684u5f3au5ea6
        TakeStrongest = 3,     // u53d6u6700u5f3au6548u679c
        TakeMostRecent = 4     // u53d6u6700u65b0u6548u679c
    }
    
    /// <summary>
    /// u72b6u6001u6548u679cu57fau7c7b - u5b9eu73b0Buff/Debuffu7cfbu7edf
    /// </summary>
    public abstract class StatusEffect
    {
        /// <summary>
        /// u6548u679cID
        /// </summary>
        public int EffectId { get; protected set; }
        
        /// <summary>
        /// u6548u679cu540du79f0
        /// </summary>
        public string Name { get; protected set; }
        
        /// <summary>
        /// u6548u679cu63cfu8ff0
        /// </summary>
        public string Description { get; protected set; }
        
        /// <summary>
        /// u6548u679cu7c7bu578b
        /// </summary>
        public StatusEffectType Type { get; protected set; }
        
        /// <summary>
        /// u6548u679cu65b9u5411uff08u6b63u9762u6216u8d1fu9762uff09
        /// </summary>
        public bool IsPositive { get; protected set; }
        
        /// <summary>
        /// u5806u53e0u7b56u7565
        /// </summary>
        public StackingPolicy StackPolicy { get; protected set; }
        
        /// <summary>
        /// u5f53u524du5806u53e0u5c42u6570
        /// </summary>
        public int CurrentStacks { get; protected set; }
        
        /// <summary>
        /// u6700u5927u5806u53e0u5c42u6570
        /// </summary>
        public int MaxStacks { get; protected set; }
        
        /// <summary>
        /// u6301u7eedu65f6u95f4uff08u79d2uff09
        /// </summary>
        public float Duration { get; protected set; }
        
        /// <summary>
        /// u5269u4f59u6301u7eedu65f6u95f4
        /// </summary>
        public float RemainingTime { get; protected set; }
        
        /// <summary>
        /// u6548u679cu6765u6e90
        /// </summary>
        public Entity Source { get; protected set; }
        
        /// <summary>
        /// u6548u679cu76eeu6807
        /// </summary>
        public Entity Target { get; protected set; }
        
        /// <summary>
        /// u6548u679cu56feu6807u8defu5f84
        /// </summary>
        public string IconPath { get; protected set; }
        
        /// <summary>
        /// VFXu7279u6548u8defu5f84
        /// </summary>
        public string VfxPath { get; protected set; }
        
        /// <summary>
        /// u6548u679cu5f3au5ea6u503c
        /// </summary>
        public float Magnitude { get; protected set; }
        
        /// <summary>
        /// u662fu5426u53efu88abu79fbu9664
        /// </summary>
        public bool CanBeRemoved { get; protected set; }
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        protected StatusEffect(int effectId, Entity source, Entity target, float duration, float magnitude = 1.0f)
        {
            EffectId = effectId;
            Name = "Unknown Effect";
            Description = "";
            Type = StatusEffectType.None;
            IsPositive = false;
            StackPolicy = StackingPolicy.None;
            CurrentStacks = 1;
            MaxStacks = 1;
            Duration = duration;
            RemainingTime = duration;
            Source = source;
            Target = target;
            IconPath = "";
            VfxPath = "";
            Magnitude = magnitude;
            CanBeRemoved = true;
        }
        
        /// <summary>
        /// u521du59cbu5316u72b6u6001u6548u679c
        /// </summary>
        public virtual void Initialize()
        {
            // u5728u5b50u7c7bu4e2du5b9eu73b0u7279u5b9au7684u521du59cbu5316u903bu8f91
            OnApply();
        }
        
        /// <summary>
        /// u66f4u65b0u72b6u6001u6548u679c
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            // u66f4u65b0u5269u4f59u65f6u95f4
            if (Duration > 0) // u6c38u4e45u6548u679cu4e0du8ba1u7b97u65f6u95f4
            {
                RemainingTime -= deltaTime;
                
                if (RemainingTime <= 0)
                {
                    RemainingTime = 0;
                    // u6548u679cu5230u671f
                    OnExpire();
                }
                else
                {
                    // u5b9au671fu6267u884cu6548u679c
                    OnTick(deltaTime);
                }
            }
            else
            {
                // u6c38u4e45u6548u679cu4ecdu7136u9700u8981tick
                OnTick(deltaTime);
            }
        }
        
        /// <summary>
        /// u5c1du8bd5u6dfbu52a0u5806u53e0
        /// </summary>
        public virtual bool TryAddStack()
        {
            if (CurrentStacks < MaxStacks)
            {
                CurrentStacks++;
                OnStackAdded();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// u5237u65b0u6301u7eedu65f6u95f4
        /// </summary>
        public virtual void RefreshDuration(float newDuration)
        {
            if (newDuration > RemainingTime)
            {
                RemainingTime = newDuration;
            }
        }
        
        /// <summary>
        /// u79fbu9664u6548u679c
        /// </summary>
        public virtual void Remove()
        {
            if (CanBeRemoved)
            {
                OnRemove();
            }
        }
        
        /// <summary>
        /// u5f53u6548u679cu88abu5e94u7528u65f6u8c03u7528
        /// </summary>
        protected virtual void OnApply()
        {
            // u5728u5b50u7c7bu4e2du5b9eu73b0
        }
        
        /// <summary>
        /// u5f53u6548u679cu6bcfu5e27u66f4u65b0u65f6u8c03u7528
        /// </summary>
        protected virtual void OnTick(float deltaTime)
        {
            // u5728u5b50u7c7bu4e2du5b9eu73b0
        }
        
        /// <summary>
        /// u5f53u6548u679cu5230u671fu65f6u8c03u7528
        /// </summary>
        protected virtual void OnExpire()
        {
            // u5728u5b50u7c7bu4e2du5b9eu73b0
        }
        
        /// <summary>
        /// u5f53u6548u679cu88abu79fbu9664u65f6u8c03u7528
        /// </summary>
        protected virtual void OnRemove()
        {
            // u5728u5b50u7c7bu4e2du5b9eu73b0
        }
        
        /// <summary>
        /// u5f53u6548u679cu589eu52a0u5806u53e0u5c42u6570u65f6u8c03u7528
        /// </summary>
        protected virtual void OnStackAdded()
        {
            // u5728u5b50u7c7bu4e2du5b9eu73b0
        }
    }
}
