using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6295u5c04u7269u7ba1u7406u5668 - u7ba1u7406u6240u6709u6218u6597u4e2du7684u6295u5c04u7269
    /// </summary>
    public class ProjectileManager
    {
        // u6240u6709u6d3bu52a8u7684u6295u5c04u7269
        private List<Projectile> _activeProjectiles = new List<Projectile>();
        
        // u5f85u9500u6bc1u7684u6295u5c04u7269
        private List<Projectile> _projectilesToRemove = new List<Projectile>();
        
        // u4e0bu4e00u4e2au6295u5c04u7269ID
        private int _nextProjectileId = 1;
        
        /// <summary>
        /// u521du59cbu5316u6295u5c04u7269u7ba1u7406u5668
        /// </summary>
        public void Initialize()
        {
            _activeProjectiles.Clear();
            _projectilesToRemove.Clear();
            _nextProjectileId = 1;
            
            Log.Info("[ProjectileManager] u521du59cbu5316u5b8cu6210");
        }
        
        /// <summary>
        /// u66f4u65b0u6240u6709u6295u5c04u7269
        /// </summary>
        public void Update(float deltaTime)
        {
            // u66f4u65b0u6bcfu4e2au6295u5c04u7269
            _projectilesToRemove.Clear();
            
            foreach (var projectile in _activeProjectiles)
            {
                projectile.Update(deltaTime);
                
                // u5982u679cu6295u5c04u7269u5df2u7ed3u675fuff0cu6807u8bb0u4e3au5f85u9500u6bc1
                if (projectile.IsFinished)
                {
                    _projectilesToRemove.Add(projectile);
                }
            }
            
            // u79fbu9664u5df2u7ed3u675fu7684u6295u5c04u7269
            foreach (var projectile in _projectilesToRemove)
            {
                _activeProjectiles.Remove(projectile);
            }
        }
        
        /// <summary>
        /// u521bu5efau6807u51c6u5f39u9053u6295u5c04u7269
        /// </summary>
        public Projectile CreateBullet(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType = DamageType.Physical, float speed = 15f, float lifetime = 5f)
        {
            // u4f7fu7528u65b0u7684u6784u9020u51fdu6570u521bu5efau5f39u9053u6295u5c04u7269uff0cu76f4u63a5u4f20u5165u6240u6709u53c2u6570
            Projectile bullet = new Projectile(
                projectileId: _nextProjectileId++, 
                source: source, 
                position: position, 
                direction: direction, 
                damage: damage, 
                damageType: damageType, 
                type: ProjectileType.Bullet, 
                name: "Bullet", 
                speed: speed, 
                maxLifetime: lifetime, 
                collisionRadius: 0.1f);
            
            // u521du59cbu5316u5e76u6dfbu52a0u5230u6d3bu52a8u5217u8868
            bullet.Initialize();
            _activeProjectiles.Add(bullet);
            
            // u65e5u5fd7
            Log.Info($"[ProjectileManager] Created bullet projectile from {source?.Name} with damage {damage}");
            
            return bullet;
        }
        
        /// <summary>
        /// u521bu5efau7bad
        /// </summary>
        public Projectile CreateArrow(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType = DamageType.Physical, float speed = 20f, float lifetime = 8f)
        {
            // 使用新的构造函数创建箭矢投射物，直接传入所有参数
            Projectile arrow = new Projectile(
                projectileId: _nextProjectileId++, 
                source: source, 
                position: position, 
                direction: direction, 
                damage: damage, 
                damageType: damageType, 
                type: ProjectileType.Arrow, 
                name: "Arrow", 
                speed: speed, 
                maxLifetime: lifetime, 
                collisionRadius: 0.15f,
                criticalChance: 0.15f  // 箭比子弹更高暴击率
            );
            
            // u521du59cbu5316u5e76u6dfbu52a0u5230u6d3bu52a8u5217u8868
            arrow.Initialize();
            _activeProjectiles.Add(arrow);
            
            // u65e5u5fd7
            Log.Info($"[ProjectileManager] Created arrow projectile from {source?.Name} with damage {damage}");
            
            return arrow;
        }
        
        /// <summary>
        /// u521bu5efau9b54u6cd5u6295u5c04u7269
        /// </summary>
        public Projectile CreateMagicProjectile(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType = DamageType.Magic, float speed = 12f, float lifetime = 6f, float effectRadius = 0f)
        {
            // 使用新的构造函数创建魔法投射物，直接传入所有参数
            Projectile magicProjectile = new Projectile(
                projectileId: _nextProjectileId++, 
                source: source, 
                position: position, 
                direction: direction, 
                damage: damage, 
                damageType: damageType, 
                type: ProjectileType.Magic, 
                name: "Magic Projectile", 
                speed: speed, 
                maxLifetime: lifetime, 
                collisionRadius: 0.2f,
                effectRadius: effectRadius  // 魔法投射物可能有范围效果
            );
            
            // u521du59cbu5316u5e76u6dfbu52a0u5230u6d3bu52a8u5217u8868
            magicProjectile.Initialize();
            _activeProjectiles.Add(magicProjectile);
            
            // u65e5u5fd7
            Log.Info($"[ProjectileManager] Created magic projectile from {source?.Name} with damage {damage} and effect radius {effectRadius}");
            
            return magicProjectile;
        }
        
        /// <summary>
        /// u521bu5efau8fddu8e2au6295u5c04u7269
        /// </summary>
        public Projectile CreateHomingProjectile(Entity source, Entity target, Vector3 position, float damage, DamageType damageType = DamageType.Magic, float speed = 8f, float lifetime = 10f, float turnRate = 2.0f)
        {
            if (target == null)
            {
                Log.Warning("[ProjectileManager] Cannot create homing projectile without a target");
                return null;
            }
            
            // 使用修改后的构造函数方式创建追踪投射物
            HomingProjectile homingProjectile = new HomingProjectile(
                projectileId: _nextProjectileId++, 
                source: source, 
                target: target, 
                position: position, 
                damage: damage, 
                damageType: damageType,
                speed: speed,
                maxLifetime: lifetime,
                collisionRadius: 0.3f,
                turnRate: turnRate
            );
            
            // u521du59cbu5316u5e76u6dfbu52a0u5230u6d3bu52a8u5217u8868
            homingProjectile.Initialize();
            _activeProjectiles.Add(homingProjectile);
            
            // u65e5u5fd7
            Log.Info($"[ProjectileManager] Created homing projectile from {source?.Name} targeting {target.Name} with damage {damage}");
            
            return homingProjectile;
        }
        
        /// <summary>
        /// u521bu5efau5149u675fu6295u5c04u7269
        /// </summary>
        public Projectile CreateBeam(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType = DamageType.Energy, float lifetime = 0.5f, float maxDistance = 50f)
        {
            // 使用修改后的构造函数方式创建光束投射物
            BeamProjectile beam = new BeamProjectile(
                projectileId: _nextProjectileId++, 
                source: source, 
                position: position, 
                direction: direction, 
                damage: damage, 
                damageType: damageType,
                maxLifetime: lifetime,
                collisionRadius: 0.5f,
                maxDistance: maxDistance,
                maxPenetrations: 999  // 光束可以穿过多个目标
            );
            
            // u521du59cbu5316u5e76u6dfbu52a0u5230u6d3bu52a8u5217u8868
            beam.Initialize();
            _activeProjectiles.Add(beam);
            
            // u65e5u5fd7
            Log.Info($"[ProjectileManager] Created beam projectile from {source?.Name} with damage {damage} and distance {maxDistance}");
            
            return beam;
        }
        
        /// <summary>
        /// u521bu5efau81eau5b9au4e49u6295u5c04u7269
        /// </summary>
        public Projectile CreateCustomProjectile(Entity source, Vector3 position, Vector3 direction, float damage, Action<Projectile, Entity> onHitCallback = null, DamageType damageType = DamageType.Physical)
        {
            // 创建自定义投射物，使用构造函数设置类型
            Projectile projectile = new Projectile(
                projectileId: _nextProjectileId++, 
                source: source, 
                position: position, 
                direction: direction, 
                damage: damage, 
                damageType: damageType, 
                type: ProjectileType.Custom, 
                name: "Custom Projectile"
            );
            
            // 设置回调函数
            projectile.OnHitCallback = onHitCallback;
            
            // u521du59cbu5316u5e76u6dfbu52a0u5230u6d3bu52a8u5217u8868
            projectile.Initialize();
            _activeProjectiles.Add(projectile);
            
            // u65e5u5fd7
            Log.Info($"[ProjectileManager] Created custom projectile from {source?.Name} with damage {damage}");
            
            return projectile;
        }
        
        /// <summary>
        /// u6e05u9664u6240u6709u6295u5c04u7269
        /// </summary>
        public void ClearAllProjectiles()
        {
            foreach (var projectile in _activeProjectiles)
            {
                projectile.Destroy();
            }
            
            _activeProjectiles.Clear();
            _projectilesToRemove.Clear();
            
            Log.Info("[ProjectileManager] Cleared all projectiles");
        }
        
        /// <summary>
        /// u83b7u53d6u5f53u524du6d3bu52a8u7684u6295u5c04u7269u6570u91cf
        /// </summary>
        public int GetActiveProjectileCount()
        {
            return _activeProjectiles.Count;
        }
        
        /// <summary>
        /// u83b7u53d6u5f53u524du6d3bu52a8u7684u6295u5c04u7269
        /// </summary>
        public List<Projectile> GetActiveProjectiles()
        {
            return new List<Projectile>(_activeProjectiles);
        }
    }
    
    /// <summary>
    /// u8fddu8e2au6295u5c04u7269u7c7bu - u8ffdU8e2au76eeu6807
    /// </summary>
    public class HomingProjectile : Projectile
    {
        /// <summary>
        /// u8f6cu5f2fu901fu7387
        /// </summary>
        public float TurnRate { get; set; }
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public HomingProjectile(int projectileId, Entity source, Entity target, Vector3 position, float damage, DamageType damageType, 
            float speed = 8f, float maxLifetime = 10f, float collisionRadius = 0.3f, float turnRate = 2.0f) 
            : base(projectileId, source, position, Vector3.zero, damage, damageType, 
                  ProjectileType.Homing, "Homing Projectile", speed, maxLifetime, collisionRadius)
        {
            Target = target;
            TurnRate = turnRate;
            
            // u8fddu8e2au5f39u9053u9700u8981u521du59cbu65b9u5411
            if (Target != null)
            {
                Direction = (Target.Position - Position).normalized;
            }
        }
        
        /// <summary>
        /// u91cdu5199u66f4u65b0u4f4du7f6eu65b9u6cd5uff0cu5b9eu73b0u8fddu8e2au903bu8f91
        /// </summary>
        protected override void UpdatePosition(float deltaTime)
        {
            if (Target != null && Target.CurrentState != EntityStateType.Dead)
            {
                // u8ba1u7b97u76eeu6807u65b9u5411
                Vector3 targetDirection = (Target.Position - Position).normalized;
                
                // u5e73u6ed1u63a5u8fddu76eeu6807u65b9u5411
                Direction = Vector3.Slerp(Direction, targetDirection, TurnRate * deltaTime).normalized;
            }
            
            // u79fbu52a8u6295u5c04u7269
            base.UpdatePosition(deltaTime);
        }
    }
    
    /// <summary>
    /// u5149u675fu6295u5c04u7269u7c7bu - u7acbu5373u51fbu4e2du6240u6709u6d3bu52a8u8303u56f4u5185u7684u76eeu6807
    /// </summary>
    public class BeamProjectile : Projectile
    {
        /// <summary>
        /// u6700u5927u5c04u7a0b
        /// </summary>
        public float MaxDistance { get; set; }
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public BeamProjectile(int projectileId, Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType,
            float maxLifetime = 0.5f, float collisionRadius = 0.5f, float maxDistance = 50f, int maxPenetrations = 999) 
            : base(projectileId, source, position, direction, damage, damageType, 
                  ProjectileType.Beam, "Beam", 0f, maxLifetime, collisionRadius, 0.05f, 0f, maxPenetrations)
        {
            MaxDistance = maxDistance;
        }
        
        /// <summary>
        /// u91cdu5199u66f4u65b0u65b9u6cd5uff0cu5b9eu73b0u7acbu5373u68c0u6d4bu78b0u649eu903bu8f91
        /// </summary>
        public override void Update(float deltaTime)
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
            
            // u5149u675fu53efu4ee5u7acbu5373u68c0u6d4bu5168u7ebfu6240u6709u78b0u649eu76eeu6807
            RaycastBeam();
        }
        
        /// <summary>
        /// u5149u675fu7ebfu6027u68c0u6d4b
        /// </summary>
        private void RaycastBeam()
        {
            // u83b7u53d6u53efu80fdu4f1au88abu5149u675fu51fbu4e2du7684u6240u6709u5b9eu4f53
            List<Entity> entities = GameModule.Combat.GetAllEntities();
            List<Entity> hitEntities = new List<Entity>();
            
            foreach (var entity in entities)
            {
                // u8df3u8fc7u81eau5df1u6216u5df2u6b7bu4ea1u7684u5b9eu4f53
                if (entity == Source || entity.CurrentState == EntityStateType.Dead)
                    continue;
                
                // u5982u679cu662fu53cbu65b9u5b9eu4f53u4e14u6295u5c04u7269u4e0du4f24u5bb3u53cbu65b9uff0cu8df3u8fc7
                if (Source != null && !Source.IsEnemy(entity) && !Source.CanDamageAllies)
                    continue;
                
                // u8ba1u7b97u5b9eu4f53u4e0eu5149u675fu7ebfu7684u8dddu79bb
                Vector3 directionToEntity = entity.Position - Position;
                float distanceToEntity = directionToEntity.magnitude;
                
                // u5982u679cu8dddu79bbu8d85u8fc7u6700u5927u5c04u7a0buff0cu8df3u8fc7
                if (distanceToEntity > MaxDistance)
                    continue;
                
                // u8ba1u7b97u70b9u5230u76f4u7ebfu7684u8dddu79bb
                Vector3 projection = Vector3.Project(directionToEntity, Direction);
                Vector3 perpendicular = directionToEntity - projection;
                float perpendicularDistance = perpendicular.magnitude;
                
                // u5982u679cu5b9eu4f53u5728u5149u675fu8303u56f4u5185uff0cu8ba4u4e3au88abu51fbu4e2d
                if (perpendicularDistance <= CollisionRadius + entity.CollisionRadius)
                {
                    // u6dfbu52a0u5230u51fbu4e2du5217u8868
                    hitEntities.Add(entity);
                }
            }
            
            // u6309u8dddu79bbu6392u5e8fu5904u7406u6240u6709u51fbu4e2du76eeu6807
            hitEntities.Sort((a, b) => Vector3.Distance(Position, a.Position).CompareTo(Vector3.Distance(Position, b.Position)));
            
            // u5904u7406u6240u6709u51fbu4e2du76eeu6807
            foreach (var entity in hitEntities)
            {
                OnHit(entity);
                
                // u5982u679cu4e0du80fdu7a7fu900fu6216u5df2u8fbeu5230u6700u5927u7a7fu900fu6570uff0cu9500u6bc1u5149u675f
                CurrentPenetrations++;
                if (MaxPenetrations <= 0 || CurrentPenetrations >= MaxPenetrations)
                {
                    Destroy();
                    break;
                }
            }
            
            // u5982u679cu6ca1u6709u51fbu4e2du4efbu4f55u76eeu6807uff0cu4eF7u4e3au51fbu4e2du6700u5927u8dddu79bb
            if (hitEntities.Count == 0 && ElapsedTime >= MaxLifetime * 0.9f)
            {
                Destroy(); // u5c06u5728u6700u540eu65f6u523bu9500u6bc1
            }
        }
    }
}
