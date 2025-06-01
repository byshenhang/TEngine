using System.Collections;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u5b9eu4f53u57fau7c7b - u6240u6709u6218u6597u5b9eu4f53u7684u57fau7c7b
    /// </summary>
    public abstract class Entity
    {
        protected int _entityId;
        protected string _name;
        protected EntityType _entityType;
        protected GameObject _gameObject;
        protected Transform _transform;
        protected AttributeComponent _attributeComponent;
        protected StateMachine _stateMachine;
        
        /// <summary>
        /// u5b9eu4f53ID
        /// </summary>
        public int EntityId => _entityId;
        
        /// <summary>
        /// u5b9eu4f53u540du79f0
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// u5b9eu4f53u7c7bu578b
        /// </summary>
        public EntityType EntityType => _entityType;
        
        /// <summary>
        /// u662fu5426u5e94u8be5u53c2u4e0eu66f4u65b0
        /// </summary>
        public bool IsActive { get; protected set; } = true;
        
        /// <summary>
        /// u5b9eu4f53GameObject
        /// </summary>
        public GameObject GameObject => _gameObject;
        
        /// <summary>
        /// u5b9eu4f53Transform
        /// </summary>
        public Transform Transform => _transform;
        
        /// <summary>
        /// u5b9eu4f53u4f4du7f6e
        /// </summary>
        public Vector3 Position => _transform != null ? _transform.position : Vector3.zero;
        
        /// <summary>
        /// u78b0u649eu68c0u6d4bu534au5f84
        /// </summary>
        public float CollisionRadius { get; protected set; } = 0.5f;
        
        /// <summary>
        /// u5224u65adu662fu5426u662fu654cu4eba
        /// </summary>
        public virtual bool IsEnemy(Entity other)
        {
            if (other == null) return false;
            
            // u9ed8u8ba4u5b9eu73b0uff0cu4e0du540cu7c7bu578bu5b9eu4f53u4e92u76f8u654cu5bf9
            return this.EntityType != other.EntityType;
        }
        
        /// <summary>
        /// u5224u65adu662fu5426u53efu4ee5u4f24u5bb3u76dfu53cb
        /// </summary>
        public virtual bool CanDamageAllies => false; // u9ed8u8ba4u4e0du80fdu4f24u5bb3u76dfu53cb
        
        /// <summary>
        /// u542fu52a8u534fu7a0b
        /// </summary>
        /// <param name="routine">u8981u6267u884cu7684u534fu7a0b</param>
        /// <returns>u534fu7a0bu5b9eu4f8b</returns>
        public virtual Coroutine StartCoroutine(IEnumerator routine)
        {
            if (_gameObject != null)
            {
                MonoBehaviour monoBehaviour = _gameObject.GetComponent<MonoBehaviour>();
                if (monoBehaviour != null)
                {
                    return monoBehaviour.StartCoroutine(routine);
                }
            }
            
            Log.Warning($"[u5b9eu4f53{_entityId}] u5c1du8bd5u542fu52a8u534fu7a0bu5931u8d25uff0cu65e0u6cd5u627eu5230u53efu7528u7684MonoBehaviour");
            return null;
        }
        
        /// <summary>
        /// u5c5eu6027u7ec4u4ef6
        /// </summary>
        public AttributeComponent Attributes => _attributeComponent;
        
        /// <summary>
        /// u72b6u6001u673a
        /// </summary>
        public StateMachine StateMachine => _stateMachine;
        
        /// <summary>
        /// u5f53u524du72b6u6001
        /// </summary>
        public EntityStateType CurrentState => _stateMachine?.CurrentStateType ?? EntityStateType.None;
        
        /// <summary>
        /// u521du59cbu5316u5b9eu4f53
        /// </summary>
        public virtual void Init(int entityId, EntityData data)
        {
            _entityId = entityId;
            _name = data.Name;
            _entityType = EntityType.None; // u5b50u7c7bu9700u8981u8bbeu7f6eu5177u4f53u7c7bu578b
            
            // u521bu5efau5c5eu6027u7ec4u4ef6
            _attributeComponent = new AttributeComponent();
            _attributeComponent.Init(data.BaseAttributes);
            
            // u521bu5efau72b6u6001u673a
            _stateMachine = new StateMachine(this);
            InitStateMachine();
            
            // u521bu5efaGameObject(u5728u5b9eu9645u5b9eu73b0u4e2du53efu4ee5u4f7fu7528u5f02u6b65u52a0u8f7d)
            // TODO: u5f02u6b65u52a0u8f7du9884u5236u4f53
            // _gameObject = LoadPrefab(data.PrefabPath);
            // _transform = _gameObject.transform;
            // _transform.position = data.Position;
            // _transform.rotation = data.Rotation;
            
            Log.Info($"[u5b9eu4f53{_entityId}] u521du59cbu5316u5b8cu6210: {_name}");
        }
        
        /// <summary>
        /// u521du59cbu5316u72b6u6001u673a - u5b50u7c7bu9700u8981u5b9eu73b0u6b64u65b9u6cd5u6765u6dfbu52a0u7279u5b9au72b6u6001
        /// </summary>
        protected abstract void InitStateMachine();
        
        /// <summary>
        /// u66f4u65b0u5b9eu4f53
        /// </summary>
        public virtual void OnUpdate(float deltaTime)
        {
            // u66f4u65b0u72b6u6001u673a
            _stateMachine?.Update();
            
            // u66f4u65b0u5c5eu6027u4feeu9970u7b26
            _attributeComponent?.UpdateModifiers(deltaTime);
        }
        
        /// <summary>
        /// u9500u6bc1u5b9eu4f53
        /// </summary>
        public virtual void OnDestroy()
        {
            if (_gameObject != null)
            {
                GameObject.Destroy(_gameObject);
                _gameObject = null;
            }
            
            Log.Info($"[u5b9eu4f53{_entityId}] u9500u6bc1: {_name}");
        }
        
        /// <summary>
        /// u53d7u5230u4f24u5bb3 - u7b80u5355u7248u672c
        /// </summary>
        public virtual void TakeDamage(float damage, Entity attacker)
        {
            // u8c03u7528u5b8cu6574u7248u672cu7684u4f24u5bb3u5904u7406
            TakeDamage(damage, attacker, DamageType.Physical, false);
        }
        
        /// <summary>
        /// u53d7u5230u4f24u5bb3 - u5b8cu6574u7248u672c
        /// </summary>
        /// <param name="damage">u4f24u5bb3u503c</param>
        /// <param name="attacker">u653bu51fbu8005</param>
        /// <param name="damageType">u4f24u5bb3u7c7bu578b</param>
        /// <param name="isCritical">u662fu5426u66b4u51fb</param>
        public virtual void TakeDamage(float damage, Entity attacker, DamageType damageType, bool isCritical)
        {
            if (_attributeComponent != null)
            {
                // u8ba1u7b97u5b9eu9645u4f24u5bb3
                float defense = _attributeComponent.GetAttribute(AttributeType.Defense);
                float actualDamage = Mathf.Max(1f, damage - defense); // u6700u5c0fu4f24u5bb31u70b9
                
                // u6263u9664u751fu547du503c
                float currentHealth = _attributeComponent.GetAttribute(AttributeType.Health);
                float newHealth = Mathf.Max(0f, currentHealth - actualDamage);
                _attributeComponent.SetCurrentAttribute(AttributeType.Health, newHealth);
                
                string critText = isCritical ? "(u66b4u51fb)" : "";
                Log.Info($"[u5b9eu4f53{_entityId}] u53d7u5230{damageType}u4f24u5bb3{critText}: {actualDamage}, u5f53u524du751fu547d: {newHealth}");
                
                // u68c0u67e5u662fu5426u6b7bu4ea1
                if (newHealth <= 0f)
                {
                    Die(attacker);
                }
            }
        }
        
        /// <summary>
        /// u6b7bu4ea1
        /// </summary>
        protected virtual void Die(Entity killer)
        {
            _stateMachine?.ChangeState(EntityStateType.Dead);
            Log.Info($"[u5b9eu4f53{_entityId}] u6b7bu4ea1");
        }
    }
}
