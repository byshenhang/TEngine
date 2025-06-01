using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6301u7eedu4f24u5bb3u72b6u6001u6548u679c - u5982u706bu7130u3001u4e2du6bd2u7b49
    /// </summary>
    public class DamageOverTimeEffect : StatusEffect
    {
        /// <summary>
        /// u4f24u5bb3u7c7bu578b
        /// </summary>
        private DamageType _damageType;
        
        /// <summary>
        /// u6bcfu6b21tick u4f24u5bb3u503c
        /// </summary>
        private float _damagePerTick;
        
        /// <summary>
        /// tick u95f4u9694uff08u79d2uff09
        /// </summary>
        private float _tickInterval;
        
        /// <summary>
        /// u4e0bu4e00u6b21 tick u65f6u95f4
        /// </summary>
        private float _nextTickTime;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public DamageOverTimeEffect(int effectId, Entity source, Entity target, float damage, float duration, float tickInterval, DamageType damageType) 
            : base(effectId, source, target, duration, damage)
        {
            _damageType = damageType;
            _damagePerTick = damage;
            _tickInterval = tickInterval;
            _nextTickTime = 0;
            
            // u8bbeu7f6eu57fau672cu5c5eu6027
            Type = StatusEffectType.DamageOverTime;
            IsPositive = false;
            
            // u8bbeu7f6eu540du79f0u548cu63cfu8ff0
            switch (damageType)
            {
                case DamageType.Fire:
                    Name = "Burning";
                    Description = $"Takes {_damagePerTick} fire damage every {_tickInterval} seconds";
                    IconPath = "Icons/Debuffs/FireDot";
                    VfxPath = "Effects/Debuffs/FireDot";
                    break;
                
                case DamageType.Ice:
                    Name = "Frostbite";
                    Description = $"Takes {_damagePerTick} ice damage every {_tickInterval} seconds";
                    IconPath = "Icons/Debuffs/IceDot";
                    VfxPath = "Effects/Debuffs/IceDot";
                    break;
                
                case DamageType.Lightning:
                    Name = "Electrocution";
                    Description = $"Takes {_damagePerTick} lightning damage every {_tickInterval} seconds";
                    IconPath = "Icons/Debuffs/LightningDot";
                    VfxPath = "Effects/Debuffs/LightningDot";
                    break;
                
                default:
                    Name = "Poison";
                    Description = $"Takes {_damagePerTick} poison damage every {_tickInterval} seconds";
                    IconPath = "Icons/Debuffs/PoisonDot";
                    VfxPath = "Effects/Debuffs/PoisonDot";
                    break;
            }
            
            // u8bbeu7f6eu5806u53e0u7b56u7565
            StackPolicy = StackingPolicy.Stack;
            MaxStacks = 3;
        }
        
        /// <summary>
        /// u5f53u6548u679cu88abu5e94u7528u65f6
        /// </summary>
        protected override void OnApply()
        {
            // u7acbu5373u9020u6210u4e00u6b21u4f24u5bb3
            ApplyDamage();
            
            // u65e5u5fd7
            Log.Info($"[StatusEffect] {Name} applied to {Target.Name}. Damage: {_damagePerTick}, Interval: {_tickInterval}s, Duration: {Duration}s");
        }
        
        /// <summary>
        /// u5f53u6548u679cu6bcfu5e27u66f4u65b0u65f6
        /// </summary>
        protected override void OnTick(float deltaTime)
        {
            _nextTickTime -= deltaTime;
            
            if (_nextTickTime <= 0)
            {
                // u5e94u7528u4f24u5bb3
                ApplyDamage();
                
                // u91cdu7f6eu8ba1u65f6u5668
                _nextTickTime = _tickInterval;
            }
        }
        
        /// <summary>
        /// u5f53u589eu52a0u5806u53e0u65f6
        /// </summary>
        protected override void OnStackAdded()
        {
            // u6bcfu5c42u5806u53e0u589eu52a0u4e00u5b9au767eu5206u6bd4u7684u4f24u5bb3
            _damagePerTick *= 1.3f; // u589eu52a030%u4f24u5bb3
            
            // u66f4u65b0u6548u679cu5f3au5ea6
            Magnitude = _damagePerTick;
            
            // u66f4u65b0u63cfu8ff0
            Description = $"Takes {_damagePerTick} damage every {_tickInterval} seconds (Stack {CurrentStacks}/{MaxStacks})";
            
            // u65e5u5fd7
            Log.Info($"[StatusEffect] {Name} on {Target.Name} gained a stack ({CurrentStacks}/{MaxStacks}). New damage: {_damagePerTick}");
        }
        
        /// <summary>
        /// u5e94u7528u4f24u5bb3
        /// </summary>
        private void ApplyDamage()
        {
            if (Target != null && Source != null && Target.CurrentState != EntityStateType.Dead)
            {
                // u8ba1u7b97u5b9eu9645u4f24u5bb3
                float damage = _damagePerTick;
                
                // u5e94u7528u4f24u5bb3 - DoT u6548u679cu4e0du89e6u53d1u66b4u51fb
                Target.TakeDamage(damage, Source, _damageType, false);
                
                // u65e5u5fd7
                Log.Info($"[StatusEffect] {Name} dealt {damage} damage to {Target.Name}");
            }
        }
    }
}
