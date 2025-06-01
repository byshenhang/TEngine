using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6295u5c04u7269u7c7bu578b
    /// </summary>
    public enum ProjectileType
    {
        Bullet,       // u5b50u5f39
        Arrow,        // u7bad
        Magic,        // u9b54u6cd5
        Thrown,       // u6295u63b7
        Beam,         // u5149u7ebf
        Homing,       // u8ffdU8e2a
        Custom        // u81eau5b9a
    }
    
    /// <summary>
    /// u6295u5c04u7269u57fau7c7bu7c7b - u7528u4e8eu8fddcu79fbu8f68u8ff9u548cu68c0u6d4bu78b0u649e
    /// </summary>
    public class Projectile
    {
        /// <summary>
        /// u6295u5c04u7269u552fu4e00ID
        /// </summary>
        public int ProjectileId { get; private set; }
        
        /// <summary>
        /// u6295u5c04u7269u7c7bu578b
        /// </summary>
        public ProjectileType Type { get; protected set; }
        
        /// <summary>
        /// u6295u5c04u7269u540du79f0
        /// </summary>
        public string Name { get; protected set; }
        
        /// <summary>
        /// u6295u5c04u7269u5b9eu4f53 - u7528u4e8eu6e32u67d3u548cu7269u7406u663eu793a
        /// </summary>
        public GameObject GameObjectRef { get; protected set; }
        
        /// <summary>
        /// u53d1u5c04u8005
        /// </summary>
        public Entity Source { get; protected set; }
        
        /// <summary>
        /// u76eeu6807u5b9eu4f53 - u5982u679cu662fu8fddu8e2au5c0fu5f39
        /// </summary>
        public Entity Target { get; protected set; }
        
        /// <summary>
        /// u5f53u524du4f4du7f6e
        /// </summary>
        public Vector3 Position { get; protected set; }
        
        /// <summary>
        /// u5f53u524du65b9u5411
        /// </summary>
        public Vector3 Direction { get; protected set; }
        
        /// <summary>
        /// u79fbu52a8u901fu5ea6
        /// </summary>
        public float Speed { get; protected set; }
        
        /// <summary>
        /// u4f24u5bb3u503c
        /// </summary>
        public float Damage { get; protected set; }
        
        /// <summary>
        /// u4f24u5bb3u7c7bu578b
        /// </summary>
        public DamageType DamageType { get; protected set; }
        
        /// <summary>
        /// u66b4u51fbu7387
        /// </summary>
        public float CriticalChance { get; protected set; }
        
        /// <summary>
        /// u5df2u6d88u8017u65f6u95f4
        /// </summary>
        public float ElapsedTime { get; protected set; }
        
        /// <summary>
        /// u6700u5927u751fu547du65f6u95f4
        /// </summary>
        public float MaxLifetime { get; protected set; }
        
        /// <summary>
        /// u78b0u649eu534au5f84
        /// </summary>
        public float CollisionRadius { get; protected set; }
        
        /// <summary>
        /// u662fu5426u6fc0u6d3b
        /// </summary>
        public bool IsActive { get; protected set; }
        
        /// <summary>
        /// u662fu5426u5df2u7ed3u675f
        /// </summary>
        public bool IsFinished { get; protected set; }
        
        /// <summary>
        /// u6700u5927u7a7fu900fu76eeu6807u6570
        /// </summary>
        public int MaxPenetrations { get; protected set; }
        
        /// <summary>
        /// u5f53u524du7a7fu900fu76eeu6807u6570
        /// </summary>
        public int CurrentPenetrations { get; protected set; }
        
        /// <summary>
        /// u6548u679cu4f5cu7528u8303u56f4
        /// </summary>
        public float EffectRadius { get; protected set; }
        
        /// <summary>
        /// u547du4e2du56deu8c03
        /// </summary>
        public Action<Projectile, Entity> OnHitCallback { get; set; }
        
        /// <summary>
        /// u9500u6bc1u56deu8c03
        /// </summary>
        public Action<Projectile> OnDestroyCallback { get; set; }
        
        /// <summary>
        /// u6784u9020u51fdu6570 - u652fu6301u8bbeu7f6eu66f4u591au5c5eu6027
        /// </summary>
        public Projectile(int projectileId, Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType, 
            ProjectileType type = ProjectileType.Bullet, string name = "Projectile", float speed = 10f, float maxLifetime = 5f, 
            float collisionRadius = 0.5f, float criticalChance = 0.05f, float effectRadius = 0f, int maxPenetrations = 0)
        {
            ProjectileId = projectileId;
            Source = source;
            Position = position;
            Direction = direction.normalized;
            Damage = damage;
            DamageType = damageType;
            
            // 设置其他属性
            Type = type;
            Name = name;
            Speed = speed;
            MaxLifetime = maxLifetime;
            CollisionRadius = collisionRadius;
            CriticalChance = criticalChance;
            EffectRadius = effectRadius;
            MaxPenetrations = maxPenetrations;
            
            // 初始化其他状态
            GameObjectRef = null;
            Target = null;
            ElapsedTime = 0f;
            IsActive = true;
            IsFinished = false;
            CurrentPenetrations = 0;
        }
        
        /// <summary>
        /// u521du59cbu5316u6295u5c04u7269
        /// </summary>
        public virtual void Initialize(GameObject projectileObj = null)
        {
            GameObjectRef = projectileObj;
            IsActive = true;
            IsFinished = false;
            ElapsedTime = 0f;
            CurrentPenetrations = 0;
            
            // u5982u679cu6709u5b9eu4f53u5bf9u8c61uff0cu8bbeu7f6eu5176u4f4du7f6eu548cu65b9u5411
            if (GameObjectRef != null)
            {
                GameObjectRef.transform.position = Position;
                GameObjectRef.transform.forward = Direction;
            }
        }
        
        /// <summary>
        /// u66f4u65b0u6295u5c04u7269
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            if (!IsActive || IsFinished)
                return;
            
            // u66f4u65b0u751fu547du65f6u95f4
            ElapsedTime += deltaTime;
            if (ElapsedTime >= MaxLifetime)
            {
                Destroy();
                return;
            }
            
            // u66f4u65b0u4f4du7f6e
            UpdatePosition(deltaTime);
            
            // u68c0u6d4bu78b0u649e
            DetectCollision();
        }
        
        /// <summary>
        /// u66f4u65b0u4f4du7f6e
        /// </summary>
        protected virtual void UpdatePosition(float deltaTime)
        {
            // u9ed8u8ba4u76f4u7ebfu79fbu52a8u903bu8f91
            Position += Direction * Speed * deltaTime;
            
            // u66f4u65b0u6e32u67d3u5bf9u8c61u4f4du7f6e
            if (GameObjectRef != null)
            {
                GameObjectRef.transform.position = Position;
            }
        }
        
        /// <summary>
        /// u68c0u6d4bu78b0u649e
        /// </summary>
        protected virtual void DetectCollision()
        {
            // u83b7u53d6u53efu80fdu4e0eu5f53u524du6295u5c04u7269u78b0u649eu7684u5b9eu4f53
            List<Entity> entities = GameModule.Combat.GetEntitiesInRange(Position, CollisionRadius);
            
            foreach (var entity in entities)
            {
                // u5982u679cu5b9eu4f53u662fu81eau5df1u6216u5df2u7ecfu6b7bu4ea1uff0cu8df3u8fc7
                if (entity == Source || entity.CurrentState == EntityStateType.Dead)
                    continue;
                
                // u5982u679cu662fu53cbu65b9u5b9eu4f53u4e14u6295u5c04u7269u4e0du4f24u5bb3u53cbu65b9uff0cu8df3u8fc7
                if (Source != null && !Source.IsEnemy(entity) && !Source.CanDamageAllies)
                    continue;
                
                // u68c0u6d4bu5230u78b0u649euff0cu5904u7406u78b0u649eu903bu8f91
                OnHit(entity);
                
                // u5982u679cu4e0du80fdu7a7fu900fu6216u5df2u8fbeu5230u6700u5927u7a7fu900fu6570uff0cu9500u6bc1u6295u5c04u7269
                CurrentPenetrations++;
                if (MaxPenetrations <= 0 || CurrentPenetrations >= MaxPenetrations)
                {
                    Destroy();
                    break;
                }
            }
        }
        
        /// <summary>
        /// u5f53u6295u5c04u7269u51fbu4e2du76eeu6807u65f6
        /// </summary>
        protected virtual void OnHit(Entity hitEntity)
        {
            if (hitEntity != null)
            {
                // u8ba1u7b97u5e76u5e94u7528u4f24u5bb3
                ApplyDamage(hitEntity);
                
                // u5982u679cu6709u8303u56f4u6548u679cuff0cu5904u7406u8303u56f4u6548u679c
                if (EffectRadius > 0)
                {
                    ApplyAreaEffect(hitEntity.Position, EffectRadius);
                }
                
                // u89e6u53d1u547du4e2du56deu8c03
                OnHitCallback?.Invoke(this, hitEntity);
            }
        }
        
        /// <summary>
        /// u5e94u7528u4f24u5bb3u5230u76eeu6807
        /// </summary>
        protected virtual void ApplyDamage(Entity target)
        {
            if (target != null && Source != null)
            {
                // u8ba1u7b97u6700u7ec8u4f24u5bb3
                float finalDamage = Damage;
                bool isCritical = UnityEngine.Random.value <= CriticalChance;
                
                // u5e94u7528u4f24u5bb3
                target.TakeDamage(finalDamage, Source, DamageType, isCritical);
                
                // u65e5u5fd7
                Log.Info($"[Projectile] {Name} hit {target.Name} for {finalDamage} damage. Critical: {isCritical}");
            }
        }
        
        /// <summary>
        /// u5e94u7528u8303u56f4u6548u679c
        /// </summary>
        protected virtual void ApplyAreaEffect(Vector3 center, float radius)
        {
            // u83b7u53d6u8303u56f4u5185u7684u6240u6709u5b9eu4f53
            List<Entity> entities = GameModule.Combat.GetEntitiesInRange(center, radius);
            
            foreach (var entity in entities)
            {
                // u8df3u8fc7u81eau5df1u548cu4e3bu76eeu6807
                if (entity == Source)
                    continue;
                
                // u5982u679cu662fu53cbu65b9u5b9eu4f53u4e14u6295u5c04u7269u4e0du4f24u5bb3u53cbu65b9uff0cu8df3u8fc7
                if (Source != null && !Source.IsEnemy(entity) && !Source.CanDamageAllies)
                    continue;
                
                // u8ba1u7b97u4f24u5bb3u8870u51cfuff08u8dddu79bbu8d8au8fdcu4f24u5bb3u8d8au5c0fuff09
                float distance = Vector3.Distance(center, entity.Position);
                float damageMultiplier = 1.0f - (distance / radius);
                float areaDamage = Damage * damageMultiplier;
                
                // u5e94u7528u4f24u5bb3
                entity.TakeDamage(areaDamage, Source, DamageType, false); // 区域效果不触发暴击
                
                // u65e5u5fd7
                Log.Info($"[Projectile] {Name} area effect hit {entity.Name} for {areaDamage} damage");
            }
        }
        
        /// <summary>
        /// u9500u6bc1u6295u5c04u7269
        /// </summary>
        public virtual void Destroy()
        {
            if (IsFinished)
                return;
            
            IsActive = false;
            IsFinished = true;
            
            // u89e6u53d1u9500u6bc1u56deu8c03
            OnDestroyCallback?.Invoke(this);
        }
    }
}
