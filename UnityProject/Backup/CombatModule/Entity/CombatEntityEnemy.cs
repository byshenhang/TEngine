using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u654cu4ebau6218u6597u5b9eu4f53
    /// </summary>
    public class CombatEntityEnemy : CombatEntityBase
    {
        // u654cu4eba AI u884cu4e3au6811ID
        private string _behaviorTreeId;
        
        // u6700u8fd1u7684u76eeu6807u5b9eu4f53ID
        private string _currentTargetId;
        
        // u901au7528u51b7u5374u8ba1u65f6u5668u5b57u5178
        private Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        
        // u7279u6b8au5224u5b9au7684u51fau751fu70b9
        private Vector3 _spawnPoint;
        
        // u5df2u7ecfu88abu6fc0u6d3buff08u8fdbu5165u6218u6597uff09
        private bool _isActivated = false;
        
        // u89c6u91ceu8303u56f4
        private float _visionRange = 10f;
        
        // u653bu51fbu8303u56f4
        private float _attackRange = 2f;
        
        // u5f3au5316u7cfbu6570
        private float _powerMultiplier = 1.0f;
        
        /// <summary>
        /// u5f53u524du76eeu6807ID
        /// </summary>
        public string CurrentTargetId => _currentTargetId;
        
        /// <summary>
        /// u884cu4e3au6811ID
        /// </summary>
        public string BehaviorTreeId => _behaviorTreeId;
        
        /// <summary>
        /// u89c6u91ceu8303u56f4
        /// </summary>
        public float VisionRange => _visionRange;
        
        /// <summary>
        /// u653bu51fbu8303u56f4
        /// </summary>
        public float AttackRange => _attackRange;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public CombatEntityEnemy(string entityId, string name, string entityType, Dictionary<AttributeType, float> attributes) 
            : base(entityId, name, attributes)
        {
            // u8bbeu7f6eu4e3au654cu4ebau9635u8425
            Faction = CombatFaction.Enemy;
            EntityType = entityType;
            _spawnPoint = Vector3.zero;
            
            // u6839u636eu5b9eu4f53u7c7bu578bu521du59cbu5316u884cu4e3au6811u548cu6280u80fd
            InitializeByEntityType();
        }
        
        /// <summary>
        /// u6839u636eu654cu4ebau7c7bu578bu521du59cbu5316
        /// </summary>
        private void InitializeByEntityType()
        {
            // u9ed8u8ba4u8bbeu7f6e
            _behaviorTreeId = "default_enemy";
            
            // u6839u636eu5b9eu4f53u7c7bu578bu8bbeu7f6eu4e0du540cu7684u884cu4e3au6811u3001u6280u80fdu548cu5c5eu6027
            switch (EntityType.ToLower())
            {
                case "melee_enemy":
                    _behaviorTreeId = "melee_enemy";
                    _attackRange = 2.0f;
                    _visionRange = 8.0f;
                    AddSkill("enemy_melee_attack");
                    break;
                    
                case "ranged_enemy":
                    _behaviorTreeId = "ranged_enemy";
                    _attackRange = 8.0f;
                    _visionRange = 12.0f;
                    AddSkill("enemy_ranged_attack");
                    break;
                    
                case "boss_enemy":
                    _behaviorTreeId = "boss_enemy";
                    _attackRange = 3.0f;
                    _visionRange = 15.0f;
                    _powerMultiplier = 2.0f;
                    AddSkill("enemy_melee_attack");
                    AddSkill("enemy_ranged_attack");
                    AddSkill("enemy_special_attack");
                    
                    // u5f3au5316u5c5eu6027
                    foreach (var attrType in new[] { AttributeType.MaxHealth, AttributeType.CurrentHealth, AttributeType.Attack, AttributeType.Defense })
                    {
                        SetAttributeValue(attrType, GetAttributeValue(attrType) * _powerMultiplier);
                    }
                    break;
                    
                default:
                    // u9ed8u8ba4u654cu4ebau6280u80fd
                    AddSkill("enemy_basic_attack");
                    break;
            }
            
            Log.Info($"u654cu4eba {EntityId} u7c7bu578b {EntityType} u521du59cbu5316u5b8cu6210uff0cu884cu4e3au6811: {_behaviorTreeId}");
        }
        
        /// <summary>
        /// u66f4u65b0u654cu4ebau5b9eu4f53
        /// </summary>
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            if (!_isActive || !IsAlive || !_isInCombat) return;
            
            // u66f4u65b0u51b7u5374u8ba1u65f6u5668
            UpdateCooldowns(deltaTime);
            
            // u5982u679cu4f7fu7528u884cu4e3au6811uff0cu5219u4e0du9700u8981u5728u8fd9u91ccu5b9eu73b0AIu903bu8f91
            // u884cu4e3au6811u7684u66f4u65b0u7531BehaviorTreeManageru5904u7406
        }
        
        /// <summary>
        /// u66f4u65b0u51b7u5374u8ba1u65f6u5668
        /// </summary>
        private void UpdateCooldowns(float deltaTime)
        {
            List<string> expiredKeys = new List<string>();
            
            foreach (var key in _cooldowns.Keys)
            {
                _cooldowns[key] -= deltaTime;
                
                if (_cooldowns[key] <= 0)
                {
                    expiredKeys.Add(key);
                }
            }
            
            foreach (var key in expiredKeys)
            {
                _cooldowns.Remove(key);
            }
        }
        
        /// <summary>
        /// u8bbeu7f6eu51fau751fu70b9
        /// </summary>
        public void SetSpawnPoint(Vector3 position)
        {
            _spawnPoint = position;
            Position = position;
        }
        
        /// <summary>
        /// u8bbeu7f6eu5f53u524du76eeu6807
        /// </summary>
        public void SetCurrentTarget(string targetEntityId)
        {
            _currentTargetId = targetEntityId;
        }
        
        /// <summary>
        /// u6fc0u6d3bu654cu4ebauff08u8fdbu5165u6218u6597u72b6u6001uff09
        /// </summary>
        public void Activate()
        {
            if (!_isActive || !IsAlive) return;
            
            _isActivated = true;
            _isInCombat = true;
            
            Log.Info($"u654cu4eba {EntityId} u5df2u6fc0u6d3buff01");
        }
        
        /// <summary>
        /// u8bbeu7f6eu51b7u5374
        /// </summary>
        public void SetCooldown(string key, float duration)
        {
            _cooldowns[key] = duration;
        }
        
        /// <summary>
        /// u68c0u67e5u662fu5426u5728u51b7u5374u4e2d
        /// </summary>
        public bool IsOnCooldown(string key)
        {
            return _cooldowns.ContainsKey(key) && _cooldowns[key] > 0;
        }
        
        /// <summary>
        /// u68c0u67e5u76eeu6807u662fu5426u5728u8dddu79bbu8303u56f4u5185
        /// </summary>
        public bool IsTargetInRange(Vector3 targetPosition, float range)
        {
            float distance = Vector3.Distance(Position, targetPosition);
            return distance <= range;
        }
        
        /// <summary>
        /// u8ba1u7b97u5230u76eeu6807u7684u65b9u5411
        /// </summary>
        public Vector3 DirectionToTarget(Vector3 targetPosition)
        {
            return (targetPosition - Position).normalized;
        }
        
        /// <summary>
        /// u654cu4ebau6b7bu4ea1u5904u7406
        /// </summary>
        protected override void OnDeath(CombatEntityBase killer)
        {
            base.OnDeath(killer);
            
            // u5728u6b7bu4ea1u65f6u53efu80fdu9700u8981u7279u6b8au5904u7406uff0cu4f8bu5982u6389u843du7269u54c1u3001u89e6u53d1u4e8bu4ef6u7b49
            _isActivated = false;
            _isInCombat = false;
            
            Log.Info($"u654cu4eba {EntityId} u6b7bu4ea1");
        }
    }
}
