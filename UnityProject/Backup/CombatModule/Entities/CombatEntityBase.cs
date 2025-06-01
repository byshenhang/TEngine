using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6218u6597u5b9eu4f53u7c7bu578b
    /// </summary>
    public enum CombatEntityType
    {
        None,
        Player,
        Enemy,
        NPC,
        Summoned
    }
    
    /// <summary>
    /// u6218u6597u5b9eu4f53u57fau7c7b - u6240u6709u6218u6597u53c2u4e0eu8005u7684u62bdu8c61u57fau7c7b
    /// </summary>
    public abstract class CombatEntityBase : IDisposable
    {
        // u5b9eu4f53u552fu4e00ID
        private string _entityId;
        
        // u5b9eu4f53u540d
        private string _name;
        
        // u5b9eu4f53u7c7bu578b
        private CombatEntityType _entityType;
        
        // u5f53u524du5b9eu4f53u5173u8054u7684u6e38u620fu5bf9u8c61
        private GameObject _gameObject;
        
        // u5c5eu6027u7cfbu7edf
        private AttributeSet _attributes;
        
        // u5f53u524du751fu547du503c
        private float _currentHealth;
        
        // u6218u6597u72b6u6001
        private bool _isInCombat;
        private string _currentCombatId;
        private bool _isAlive = true;
        
        // u6280u80fdu7cfbu7edf
        private List<string> _skillIds = new List<string>();
        private string _currentSkillId;
        
        // u884cu4e3au6811
        private BehaviorTreeNode _behaviorTree;
        private bool _behaviorEnabled = false;
        
        // u7279u6b8au72b6u6001u6548u679c
        private Dictionary<string, StatusEffect> _statusEffects = new Dictionary<string, StatusEffect>();
        
        #region u5c5eu6027u53cau8bbfu95ee
        
        /// <summary>
        /// u83b7u53d6u5b9eu4f53ID
        /// </summary>
        public string EntityId => _entityId;
        
        /// <summary>
        /// u83b7u53d6u5b9eu4f53u540d
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// u83b7u53d6u5b9eu4f53u7c7bu578b
        /// </summary>
        public CombatEntityType EntityType => _entityType;
        
        /// <summary>
        /// u83b7u53d6u5173u8054u7684u6e38u620fu5bf9u8c61
        /// </summary>
        public GameObject GameObject => _gameObject;
        
        /// <summary>
        /// u83b7u53d6u5c5eu6027u96c6
        /// </summary>
        public AttributeSet Attributes => _attributes;
        
        /// <summary>
        /// u83b7u53d6u5f53u524du751fu547du503c
        /// </summary>
        public float CurrentHealth => _currentHealth;
        
        /// <summary>
        /// u83b7u53d6u6700u5927u751fu547du503c
        /// </summary>
        public float MaxHealth => _attributes.GetAttributeValue(AttributeType.Health);
        
        /// <summary>
        /// u83b7u53d6u751fu547du767eu5206u6bd4
        /// </summary>
        public float HealthPercentage => MaxHealth > 0 ? _currentHealth / MaxHealth : 0;
        
        /// <summary>
        /// u662fu5426u5728u6218u6597u4e2d
        /// </summary>
        public bool IsInCombat => _isInCombat;
        
        /// <summary>
        /// u662fu5426u5b58u6d3b
        /// </summary>
        public bool IsAlive => _isAlive;
        
        /// <summary>
        /// u5f53u524du6218u6597ID
        /// </summary>
        public string CurrentCombatId => _currentCombatId;
        
        /// <summary>
        /// u662fu5426u5904u4e8eu884cu52a8u4e2d
        /// </summary>
        public bool IsActing { get; protected set; }
        
        /// <summary>
        /// u5f53u524du4f7fu7528u7684u6280u80fdID
        /// </summary>
        public string CurrentSkillId => _currentSkillId;
        
        /// <summary>
        /// u6240u6709u6280u80fdIDu5217u8868
        /// </summary>
        public List<string> SkillIds => _skillIds;
        
        #endregion
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public CombatEntityBase(string entityId, string name, CombatEntityType entityType)
        {
            _entityId = entityId;
            _name = name;
            _entityType = entityType;
            
            // u521du59cbu5316u5c5eu6027u96c6
            _attributes = new AttributeSet();
            
            Log.Info($"u521du59cbu5316u6218u6597u5b9eu4f53: {_entityId} ({_name})");
        }
        
        /// <summary>
        /// u521du59cbu5316u5b9eu4f53
        /// </summary>
        public virtual void Initialize(GameObject gameObject = null)
        {
            _gameObject = gameObject;
            
            // u521du59cbu5316u5c5eu6027
            InitializeAttributes();
            
            // u521du59cbu5316u6280u80fd
            InitializeSkills();
            
            // u521du59cbu5316u884cu4e3au6811
            InitializeBehaviorTree();
            
            // u8bbeu7f6eu751fu547du503c
            _currentHealth = MaxHealth;
            
            // u8bbeu7f6eu5b58u6d3bu72b6u6001
            _isAlive = true;
            
            // u7ed1u5b9au7ec4u4ef6
            if (_gameObject != null)
            {
                var component = _gameObject.GetComponent<CombatEntityComponent>();
                if (component == null)
                {
                    component = _gameObject.AddComponent<CombatEntityComponent>();
                }
                component.SetEntityId(_entityId);
            }
            
            Log.Info($"u6218u6597u5b9eu4f53u521du59cbu5316u5b8cu6210: {_entityId}");
        }
        
        /// <summary>
        /// u521du59cbu5316u5c5eu6027
        /// </summary>
        protected virtual void InitializeAttributes()
        {
            // u5b50u7c7bu9700u8981u5b9eu73b0
        }
        
        /// <summary>
        /// u521du59cbu5316u6280u80fd
        /// </summary>
        protected virtual void InitializeSkills()
        {
            // u5b50u7c7bu9700u8981u5b9eu73b0
        }
        
        /// <summary>
        /// u521du59cbu5316u884cu4e3au6811
        /// </summary>
        protected virtual void InitializeBehaviorTree()
        {
            // u5b50u7c7bu9700u8981u5b9eu73b0
        }
        
        /// <summary>
        /// u4e3au6218u6597u51c6u5907
        /// </summary>
        public virtual void InitForCombat(string combatId)
        {
            _isInCombat = true;
            _currentCombatId = combatId;
            
            // u5f00u542fu884cu4e3au6811u5982u679cu5b58u5728
            if (_behaviorTree != null)
            {
                _behaviorEnabled = true;
            }
            
            // u6e05u7406u72b6u6001u6548u679c
            _statusEffects.Clear();
            
            Log.Info($"u5b9eu4f53 {_entityId} u51c6u5907u8fdbu5165u6218u6597: {combatId}");
        }
        
        /// <summary>
        /// u6218u6597u7ed3u675fu540eu6e05u7406
        /// </summary>
        public virtual void CleanupAfterCombat(string combatId)
        {
            if (_currentCombatId != combatId) return;
            
            _isInCombat = false;
            _currentCombatId = null;
            
            // u5173u95edu884cu4e3au6811
            _behaviorEnabled = false;
            
            // u6e05u7406u72b6u6001u6548u679c
            foreach (var effect in _statusEffects.Values)
            {
                effect.OnRemove(this);
            }
            _statusEffects.Clear();
            
            Log.Info($"u5b9eu4f53 {_entityId} u6e05u7406u5b8cu6210u6218u6597: {combatId}");
        }
        
        /// <summary>
        /// u66f4u65b0u5b9eu4f53
        /// </summary>
        public virtual void OnUpdate()
        {
            // u5982u679cu4e0du572au6d3bu4e0du5728u6218u6597u4e2du5219u4e0du66f4u65b0
            if (!_isAlive || !_isInCombat) return;
            
            // u66f4u65b0u72b6u6001u6548u679c
            UpdateStatusEffects();
            
            // u5982u679cu5f00u542fu884cu4e3au6811u5219u66f4u65b0
            if (_behaviorEnabled && _behaviorTree != null)
            {
                _behaviorTree.Update(this);
            }
        }
        
        /// <summary>
        /// u66f4u65b0u72b6u6001u6548u679c
        /// </summary>
        private void UpdateStatusEffects()
        {
            List<string> expiredEffects = new List<string>();
            
            foreach (var effect in _statusEffects.Values)
            {
                effect.OnUpdate(this);
                
                if (effect.IsExpired)
                {
                    expiredEffects.Add(effect.EffectId);
                }
            }
            
            // u79fbu9664u8fc7u671fu7684u6548u679c
            foreach (var effectId in expiredEffects)
            {
                RemoveStatusEffect(effectId);
            }
        }
        
        #region u6218u6597u76f8u5173
        
        /// <summary>
        /// u53d7u5230u4f24u5bb3
        /// </summary>
        public virtual void TakeDamage(CombatEntityBase attacker, float damage, DamageType damageType, bool isCritical = false)
        {
            if (!_isAlive) return;
            
            // u8ba1u7b97u6700u7ec8u4f24u5bb3
            float finalDamage = CalculateFinalDamage(attacker, damage, damageType, isCritical);
            
            // u5e94u7528u4f24u5bb3
            _currentHealth -= finalDamage;
            
            Log.Info($"{_name} u53d7u5230 {finalDamage:F1} u70b9{damageType}u4f24u5bb3");
            
            // u68c0u67e5u751fu547du503c
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die(attacker);
            }
            
            // u89e6u53d1u4f24u5bb3u4e8bu4ef6
            OnDamaged(attacker, finalDamage, damageType, isCritical);
        }
        
        /// <summary>
        /// u8ba1u7b97u6700u7ec8u4f24u5bb3
        /// </summary>
        protected virtual float CalculateFinalDamage(CombatEntityBase attacker, float damage, DamageType damageType, bool isCritical)
        {
            // u57fau7840u8ba1u7b97
            float finalDamage = damage;
            
            // u66f4u590du6742u7684u8ba1u7b97u53efu4ee5u7531u5b50u7c7bu5b9eu73b0
            // u4f8bu5982u8003u8651u9632u5fa1u3001u5c5eu6027u3001u7279u6b8au72b6u6001u7b49
            
            // u6682u65f6u7684u57fau7840u9632u5fa1u8ba1u7b97
            float defense = 0;
            
            switch (damageType)
            {
                case DamageType.Physical:
                    defense = _attributes.GetAttributeValue(AttributeType.Defense);
                    break;
                case DamageType.Magical:
                    defense = _attributes.GetAttributeValue(AttributeType.MagicDefense);
                    break;
            }
            
            // u7b80u5355u7684u4f24u5bb3u516cu5f0f
            finalDamage = Mathf.Max(1, finalDamage * (100 / (100 + defense)));
            
            // u66f4u590du6742u7684u5b9eu73b0u53efu4ee5u8003u8651u66f4u591au56e0u7d20
            
            // u6682u65f6u5bfcu81f4u6982u7387u662fu7ba1u66f4u591au4f24u5bb3
            if (isCritical)
            {
                float critDamage = 1.5f; // u9ed8u8ba4u66f4u591a50%
                finalDamage *= critDamage;
            }
            
            return finalDamage;
        }
        
        /// <summary>
        /// u6062u590du751fu547du503c
        /// </summary>
        public virtual void HealHealth(float amount, CombatEntityBase healer = null)
        {
            if (!_isAlive) return;
            
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, MaxHealth);
            float actualHeal = _currentHealth - oldHealth;
            
            Log.Info($"{_name} u6062u590d {actualHeal:F1} u70b9u751fu547du503c");
            
            // u89e6u53d1u6062u590du4e8bu4ef6
            OnHealed(actualHeal, healer);
        }
        
        /// <summary>
        /// u6b7bu4ea1u5904u7406
        /// </summary>
        protected virtual void Die(CombatEntityBase killer = null)
        {
            if (!_isAlive) return;
            
            _isAlive = false;
            _currentHealth = 0;
            
            Log.Info($"{_name} u5df2u6b7bu4ea1");
            
            // u89e6u53d1u6b7bu4ea1u4e8bu4ef6
            OnDeath(killer);
        }
        
        /// <summary>
        /// u590du6d3bu5904u7406
        /// </summary>
        public virtual void Revive(float healthPercentage = 0.3f)
        {
            if (_isAlive) return;
            
            _isAlive = true;
            _currentHealth = MaxHealth * healthPercentage;
            
            Log.Info($"{_name} u5df2u590du6d3buff0cu751fu547du503c: {_currentHealth:F1}");
            
            // u89e6u53d1u590du6d3bu4e8bu4ef6
            OnRevived();
        }
        
        #endregion
        
        #region u6280u80fdu7cfbu7edf
        
        /// <summary>
        /// u6dfbu52a0u6280u80fd
        /// </summary>
        public virtual void AddSkill(string skillId)
        {
            if (!_skillIds.Contains(skillId))
            {
                _skillIds.Add(skillId);
                Log.Info($"{_name} u5b66u4e60u4e86u6280u80fd: {skillId}");
            }
        }
        
        /// <summary>
        /// u79fbu9664u6280u80fd
        /// </summary>
        public virtual void RemoveSkill(string skillId)
        {
            if (_skillIds.Contains(skillId))
            {
                _skillIds.Remove(skillId);
                Log.Info($"{_name} u5931u53d1u4e86u6280u80fd: {skillId}");
            }
        }
        
        /// <summary>
        /// u68c0u67e5u662fu5426u6709u6307u5b9au6280u80fd
        /// </summary>
        public bool HasSkill(string skillId)
        {
            return _skillIds.Contains(skillId);
        }
        
        /// <summary>
        /// u4f7fu7528u6280u80fd
        /// </summary>
        public virtual void UseSkill(string skillId, CombatEntityBase target = null)
        {
            if (!HasSkill(skillId))
            {
                Log.Warning($"{_name} u5c1du8bd5u4f7fu7528u4e0du6b63u5e38u7684u6280u80fd: {skillId}");
                return;
            }
            
            _currentSkillId = skillId;
            IsActing = true;
            
            Log.Info($"{_name} u5f00u59cbu4f7fu7528u6280u80fd: {skillId}");
            
            // u8fd9u91ccu5e94u8be5u7531u5916u90e8u6280u80fdu7cfbu7edfu6765u5904u7406u5177u4f53u7684u6280u80fdu6548u679c
            // u8fd9u91ccu53ebu5b9au4e49u4e86u63a5u53e3
        }
        
        /// <summary>
        /// u5b8cu6210u6280u80fdu4f7fu7528
        /// </summary>
        public virtual void CompleteSkill()
        {
            string skillId = _currentSkillId;
            _currentSkillId = null;
            IsActing = false;
            
            Log.Info($"{_name} u5b8cu6210u4f7fu7528u6280u80fd: {skillId}");
        }
        
        #endregion
        
        #region u72b6u6001u6548u679cu7cfbu7edf
        
        /// <summary>
        /// u6dfbu52a0u72b6u6001u6548u679c
        /// </summary>
        public virtual void AddStatusEffect(StatusEffect effect)
        {
            if (effect == null) return;
            
            // u5982u679cu5df2u5b58u5728u76f8u540cu7c7bu578bu7684u6548u679cu5219u79fbu9664u5b83
            if (_statusEffects.TryGetValue(effect.EffectId, out var existingEffect))
            {
                existingEffect.OnRemove(this);
            }
            
            // u6dfbu52a0u65b0u6548u679c
            _statusEffects[effect.EffectId] = effect;
            effect.OnApply(this);
            
            Log.Info($"{_name} u83b7u5f97u72b6u6001: {effect.EffectId}");
        }
        
        /// <summary>
        /// u79fbu9664u72b6u6001u6548u679c
        /// </summary>
        public virtual void RemoveStatusEffect(string effectId)
        {
            if (_statusEffects.TryGetValue(effectId, out var effect))
            {
                effect.OnRemove(this);
                _statusEffects.Remove(effectId);
                
                Log.Info($"{_name} u79fbu9664u72b6u6001: {effectId}");
            }
        }
        
        /// <summary>
        /// u68c0u67e5u662fu5426u5177u6709u6307u5b9au72b6u6001
        /// </summary>
        public bool HasStatusEffect(string effectId)
        {
            return _statusEffects.ContainsKey(effectId);
        }
        
        /// <summary>
        /// u83b7u53d6u6240u6709u72b6u6001
        /// </summary>
        public List<StatusEffect> GetAllStatusEffects()
        {
            return new List<StatusEffect>(_statusEffects.Values);
        }
        
        #endregion
        
        #region u4e8bu4ef6u56deu8c03
        
        /// <summary>
        /// u53d7u4f24u4e8bu4ef6
        /// </summary>
        protected virtual void OnDamaged(CombatEntityBase attacker, float damage, DamageType damageType, bool isCritical)
        {
            // u5b50u7c7bu5b9eu73b0
        }
        
        /// <summary>
        /// u6062u590du4e8bu4ef6
        /// </summary>
        protected virtual void OnHealed(float amount, CombatEntityBase healer)
        {
            // u5b50u7c7bu5b9eu73b0
        }
        
        /// <summary>
        /// u6b7bu4ea1u4e8bu4ef6
        /// </summary>
        protected virtual void OnDeath(CombatEntityBase killer)
        {
            // u5b50u7c7bu5b9eu73b0
        }
        
        /// <summary>
        /// u590du6d3bu4e8bu4ef6
        /// </summary>
        protected virtual void OnRevived()
        {
            // u5b50u7c7bu5b9eu73b0
        }
        
        #endregion
        
        /// <summary>
        /// u91cau653eu8d44u6e90
        /// </summary>
        public virtual void Dispose()
        {
            // u6e05u7406u72b6u6001u6548u679c
            foreach (var effect in _statusEffects.Values)
            {
                effect.OnRemove(this);
            }
            _statusEffects.Clear();
            
            // u6e05u7406u884cu4e3au6811
            _behaviorTree = null;
            
            // u6e05u7406u5c5eu6027
            _attributes?.Dispose();
            _attributes = null;
            
            // u6e05u7406u5173u8054u6e38u620fu5bf9u8c61
            if (_gameObject != null)
            {
                var component = _gameObject.GetComponent<CombatEntityComponent>();
                if (component != null)
                {
                    component.SetEntityId(null);
                }
            }
            _gameObject = null;
            
            Log.Info($"u6218u6597u5b9eu4f53u5df2u91cau653e: {_entityId}");
        }
    }
    
    /// <summary>
    /// u4f24u5bb3u7c7bu578b
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical,
        True,    // u771fu5b9eu4f24u5bb3uff0cu65e0u89c6u9632u5fa1
        Healing  // u6062u590du6027u4f24u5bb3uff08u8d1fu6570u4f24u5bb3uff09
    }
}
