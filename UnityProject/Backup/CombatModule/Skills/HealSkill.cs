using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6cbbu7597u6280u80fd - u56deu590du751fu547du503cu7c7bu6280u80fd
    /// </summary>
    public class HealSkill : SkillBase
    {
        // u6cbbu7597u91cfu57fau7840u503c
        private float _baseHealAmount;
        
        // u6cbbu7597u7cfbu6570uff08u57fau4e8eu9b54u6cd5u653bu51fbu529bu7684u500du7387uff09
        private float _healCoefficient;
        
        // u9644u52a0u751fu547du56deu590du6301u7eedu6548u679c
        private bool _hasHealOverTimeEffect;
        
        // u751fu547du56deu590du6301u7eedu65f6u95f4
        private float _hotDuration;
        
        // u751fu547du56deu590du6bcfu6b21u6267u884cu7684u6cbbu7597u91cf
        private float _hotAmountPerTick;
        
        // u751fu547du56deu590du6267u884cu95f4u9694uff08u79d2uff09
        private float _hotTickInterval;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public HealSkill(string skillId, string name, string description) 
            : base(skillId, name, description, SkillType.Active, SkillTargetType.SingleTarget, SkillRangeType.Ranged)
        {
            // u8bbeu7f6eu9ed8u8ba4u6cbbu7597u6280u80fdu53c2u6570
            _cooldownTime = 3.0f;          // 3u79d2u51b7u5374
            _castTime = 1.0f;              // 1u79d2u65bdu6cd5u65f6u95f4
            _damageType = DamageType.Heal;  // u6cbbu7597u7c7bu578b
            _effectRange = 10.0f;           // u9ed8u8ba4u8303u56f4
            _baseHealAmount = 50f;          // u57fau7840u6cbbu7597u91cf
            _healCoefficient = 0.5f;        // u6cbbu7597u7cfbu6570
            
            // u8bbeu7f6eu6280u80fdu6d88u8017
            SetCost(AttributeType.Mana, 30f);
        }
        
        /// <summary>
        /// u8bbeu7f6eu6cbbu7597u91cfu53c2u6570
        /// </summary>
        public void SetHealingParameters(float baseHealAmount, float healCoefficient)
        {
            _baseHealAmount = baseHealAmount;
            _healCoefficient = healCoefficient;
        }
        
        /// <summary>
        /// u8bbeu7f6eu751fu547du56deu590du6548u679c
        /// </summary>
        public void SetHealOverTimeEffect(float duration, float amountPerTick, float tickInterval)
        {
            _hasHealOverTimeEffect = true;
            _hotDuration = duration;
            _hotAmountPerTick = amountPerTick;
            _hotTickInterval = tickInterval;
        }
        
        /// <summary>
        /// u7b49u7ea7u53d8u5316u65f6u8c03u7528
        /// </summary>
        protected override void OnLevelChanged()
        {
            // u968fu7b49u7ea7u63d0u5347u6cbbu7597u91cf
            _baseHealAmount = 50f + (_level - 1) * 20f; // u6bcfu7ea7u589eu52a020u70b9u57fau7840u6cbbu7597u91cf
            
            // u6cbbu7597u7cfbu6570u53efu80fdu968fu7b49u7ea7u589eu52a0
            _healCoefficient = 0.5f + (_level - 1) * 0.1f; // u6bcfu7ea7u589eu52a0 0.1 u6cbbu7597u7cfbu6570
            
            // u51b7u5374u65f6u95f4u53efu80fdu968fu7b49u7ea7u51cfu5c11
            _cooldownTime = Mathf.Max(1.5f, 3.0f - (_level - 1) * 0.3f); // u6bcfu7ea7u51cfu5c11 0.3 u79d2u51b7u5374uff0cu6700u4f4e 1.5 u79d2
        }
        
        /// <summary>
        /// u6267u884cu6280u80fdu6548u679c
        /// </summary>
        protected override async UniTask<bool> ExecuteSkillEffect(CombatEntityBase caster, CombatEntityBase target)
        {
            if (caster == null) return false;
            
            if (_targetType == SkillTargetType.SingleTarget)
            {
                // u5355u4f53u6cbbu7597
                if (target == null || !target.IsAlive)
                {
                    Log.Warning($"{caster.Name} u4f7fu7528u6280u80fd {_name} u5931u8d25uff0cu76eeu6807u65e0u6548");
                    return false;
                }
                
                return await HealSingleTarget(caster, target);
            }
            else if (_targetType == SkillTargetType.Area || _targetType == SkillTargetType.AllAllies)
            {
                // u533au57dfu6cbbu7597
                return await HealAreaTargets(caster, target);
            }
            
            return false;
        }
        
        /// <summary>
        /// u6cbbu7597u5355u4e2au76eeu6807
        /// </summary>
        private async UniTask<bool> HealSingleTarget(CombatEntityBase caster, CombatEntityBase target)
        {
            // u8ba1u7b97u6cbbu7597u91cf
            float healAmount = CalculateHealAmount(caster, target);
            
            // u8ba1u7b97u6cbbu7597u6548u679cu52a0u6210
            bool isHealBoosted = CalculateHealBoost(caster);
            if (isHealBoosted)
            {
                // u6cbbu7597u6548u679cu63d0u5347uff0cu4f8bu5982u589eu52a050%
                healAmount *= 1.5f;
                Log.Info($"{caster.Name} u7684u6280u80fd {_name} u51fau73b0u4e86u589eu5f3au6548u679cuff01");
            }
            
            // u5e94u7528u6cbbu7597
            target.TakeDamage(caster, healAmount, DamageType.Heal);
            Log.Info($"{caster.Name} u7684u6280u80fd {_name} u4e3a {target.Name} u6062u590du4e86 {healAmount:F1} u70b9u751fu547du503c");
            
            // u5982u679cu6709u751fu547du56deu590du6548u679cuff0cu5e94u7528u751fu547du56deu590du6548u679c
            if (_hasHealOverTimeEffect)
            {
                ApplyHealOverTimeEffect(caster, target);
            }
            
            // u7b49u5f85u4e00u70b9u65f6u95f4u786eu4fddu52a8u753bu548cu6548u679cu5b8cu6210
            await UniTask.Delay(100);
            
            return true;
        }
        
        /// <summary>
        /// u6cbbu7597u533au57dfu76eeu6807
        /// </summary>
        private async UniTask<bool> HealAreaTargets(CombatEntityBase caster, CombatEntityBase mainTarget)
        {
            bool healedAny = false;
            
            // u83b7u53d6u6218u6597ID
            string combatId = caster.IsInCombat ? caster.CurrentCombatId : null;
            if (string.IsNullOrEmpty(combatId))
            {
                Log.Warning($"{caster.Name} u4e0du5728u6218u6597u4e2duff0cu65e0u6cd5u4f7fu7528u533au57dfu6cbbu7597u6280u80fd");
                return false;
            }
            
            // u83b7u53d6u6240u6709u53efu80fdu7684u76eeu6807
            List<CombatEntityBase> targets = GetValidTargets(caster, combatId);
            
            // u8fc7u6ee4u6389u8d85u51fau8303u56f4u7684u76eeu6807
            List<CombatEntityBase> inRangeTargets = new List<CombatEntityBase>();
            
            // u5982u679cu6709u4e3bu76eeu6807uff0cu786eu4fddu5b83u5728u76eeu6807u5217u8868u4e2d
            if (mainTarget != null && mainTarget.IsAlive && !targets.Contains(mainTarget))
            {
                targets.Add(mainTarget);
            }
            
            // u8fc7u6ee4u6709u6548u8303u56f4u5185u7684u76eeu6807
            foreach (var potentialTarget in targets)
            {
                if (potentialTarget == null || !potentialTarget.IsAlive) continue;
                
                // u8ba1u7b97u4e0eu65bdu6cd5u8005u7684u8dddu79bb
                float distance = CalculateDistance(caster, potentialTarget);
                
                // u5982u679cu5728u8303u56f4u5185uff0cu6dfbu52a0u5230u76eeu6807u5217u8868
                if (distance <= _effectRange)
                {
                    inRangeTargets.Add(potentialTarget);
                }
            }
            
            // u4f9du6b21u6cbbu7597u6bcfu4e2au76eeu6807
            foreach (var target in inRangeTargets)
            {
                // u8ba1u7b97u6cbbu7597u91cfuff08u5bf9u533au57dfu76eeu6807u53efu80fdu6709u6548u679cu8870u51cfuff09
                float healMultiplier = (target == mainTarget) ? 1.0f : 0.7f; // u975eu4e3bu76eeu6807u6cbbu7597u91cfu964du4f4e30%
                float healAmount = CalculateHealAmount(caster, target) * healMultiplier;
                
                // u8ba1u7b97u6cbbu7597u6548u679cu52a0u6210
                bool isHealBoosted = CalculateHealBoost(caster);
                if (isHealBoosted)
                {
                    // u6cbbu7597u6548u679cu63d0u5347uff0cu4f8bu5982u589eu52a050%
                    healAmount *= 1.5f;
                    Log.Info($"{caster.Name} u7684u6280u80fd {_name} u5bf9 {target.Name} u51fau73b0u4e86u589eu5f3au6548u679cuff01");
                }
                
                // u5e94u7528u6cbbu7597
                target.TakeDamage(caster, healAmount, DamageType.Heal);
                Log.Info($"{caster.Name} u7684u6280u80fd {_name} u4e3a {target.Name} u6062u590du4e86 {healAmount:F1} u70b9u751fu547du503c");
                
                // u5982u679cu6709u751fu547du56deu590du6548u679cuff0cu5e94u7528u751fu547du56deu590du6548u679c(u53efu80fdu6709u6982u7387u5224u5b9a)
                if (_hasHealOverTimeEffect)
                {
                    float hotChance = (target == mainTarget) ? 1.0f : 0.5f; // u975eu4e3bu76eeu6807HOTu6548u679cu51e0u7387u964du4f4e
                    if (Random.value <= hotChance)
                    {
                        ApplyHealOverTimeEffect(caster, target);
                    }
                }
                
                healedAny = true;
                
                // u77edu6682u5ef6u8fdfuff0cu4f7fu6548u679cu770bu8d77u6765u66f4u81eau7136
                await UniTask.Delay(50);
            }
            
            return healedAny;
        }
        
        /// <summary>
        /// u8ba1u7b97u6cbbu7597u91cf
        /// </summary>
        protected virtual float CalculateHealAmount(CombatEntityBase caster, CombatEntityBase target)
        {
            if (caster == null) return 0;
            
            // u57fau7840u6cbbu7597u91cfu8ba1u7b97
            float baseHeal = _baseHealAmount;
            
            // u9b54u6cd5u653bu51fbu529bu52a0u6210
            float magicAttack = caster.Attributes.GetAttributeValue(AttributeType.MagicAttack);
            float magicBonus = magicAttack * _healCoefficient;
            
            // u6839u636eu6280u80fdu7b49u7ea7u8c03u6574u6cbbu7597u91cf
            float levelMultiplier = 1.0f + (_level - 1) * 0.2f; // u6bcfu7ea7u589eu52a020%u6cbbu7597u91cf
            
            // u8ba1u7b97u6700u7ec8u6cbbu7597u91cf
            float finalHeal = (baseHeal + magicBonus) * levelMultiplier;
            
            return finalHeal;
        }
        
        /// <summary>
        /// u8ba1u7b97u6cbbu7597u6548u679cu52a0u6210
        /// </summary>
        private bool CalculateHealBoost(CombatEntityBase caster)
        {
            if (caster == null) return false;
            
            // u83b7u53d6u65bdu6cd5u8005u7684u6cbbu7597u6548u679cu52a0u6210u5c5eu6027uff08u5047u8bbeu4e3au7279u6b8au5c5eu6027uff09
            float healBoostRate = caster.Attributes.GetAttributeValue(AttributeType.HealBoost);
            
            // u968fu673au5224u5b9au662fu5426u51fau73b0u6cbbu7597u6548u679cu52a0u6210
            return Random.value <= healBoostRate;
        }
        
        /// <summary>
        /// u5e94u7528u751fu547du56deu590du6548u679c
        /// </summary>
        private void ApplyHealOverTimeEffect(CombatEntityBase caster, CombatEntityBase target)
        {
            if (!_hasHealOverTimeEffect || _hotDuration <= 0 || _hotAmountPerTick <= 0) return;
            
            // u521bu5efau751fu547du56deu590du6548u679c
            // u5728u5b9eu9645u5b9eu73b0u4e2duff0cu5e94u8be5u4f7fu7528u4e13u95e8u7684HOTu6548u679cu7c7b
            var hotEffect = new DamageOverTimeEffect(
                $"hot_{_skillId}",   // u6548u679cID
                $"{_name}u751fu547du56deu590d", // u6548u679cu540du79f0
                _hotDuration,        // u6301u7eedu65f6u95f4
                _hotTickInterval,    // u6267u884cu95f4u9694
                -_hotAmountPerTick,  // u8d1fu4f24u5bb3u4ee3u8868u6cbbu7597
                DamageType.Heal,     // u6cbbu7597u7c7bu578b
                caster,              // u65bdu6cd5u8005
                1,                   // u6700u5927u5c42u6570
                true                 // u53efu5237u65b0
            );
            
            // u6dfbu52a0u6548u679cu56feu6807
            hotEffect.SetIcon("hot_icon");
            
            // u5e94u7528u6548u679c
            target.ApplyStatusEffect(hotEffect, caster);
            Log.Info($"{caster.Name} u7684u6280u80fd {_name} u5bf9 {target.Name} u65bdu52a0u4e86u751fu547du56deu590du6548u679cuff0cu6301u7eed {_hotDuration} u79d2");
        }
        
        /// <summary>
        /// u8ba1u7b97u4e24u4e2au5b9eu4f53u4e4bu95f4u7684u8dddu79bb
        /// </summary>
        private float CalculateDistance(CombatEntityBase entity1, CombatEntityBase entity2)
        {
            if (entity1 == null || entity2 == null || 
                entity1.GameObject == null || entity2.GameObject == null)
            {
                return float.MaxValue;
            }
            
            return Vector3.Distance(
                entity1.GameObject.transform.position, 
                entity2.GameObject.transform.position);
        }
    }
}
