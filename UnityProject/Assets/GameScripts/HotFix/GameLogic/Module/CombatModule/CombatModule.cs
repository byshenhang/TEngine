using System.Collections.Generic;
using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗模块 - 作为战斗系统的主要接口
    /// </summary>
    public sealed class CombatModule : Singleton<CombatModule>, IUpdate
    {
        private EntityManager _entityManager;
        private CombatStateManager _stateManager;
        private SkillManager _skillManager;
        private StatusEffectManager _statusEffectManager;
        private ProjectileManager _projectileManager;
        private MockDataProvider _mockDataProvider;
        
        /// <summary>
        /// 初始化战斗模块
        /// </summary>
        public void Initialize()
        {
            // 初始化模拟数据提供者
            _mockDataProvider = MockDataProvider.Instance;
            _mockDataProvider.Initialize();
            
            // 初始化实体管理器和状态管理器
            _entityManager = new EntityManager();
            _stateManager = new CombatStateManager();
            
            // 初始化技能管理器
            _skillManager = new SkillManager();
            _skillManager.Initialize();
            
            // 初始化状态效果管理器
            _statusEffectManager = new StatusEffectManager();
            _statusEffectManager.Initialize();
            
            // 初始化投射物管理器
            _projectileManager = new ProjectileManager();
            _projectileManager.Initialize();
            
            Log.Info("[CombatModule] 初始化完成");
        }
        
        /// <summary>
        /// 更新战斗模块 - 只在战斗激活状态下更新
        /// </summary>
        public void OnUpdate()
        {
            // 只有在战斗激活状态下才执行更新
            if (!IsCombatActive())
            {
                return;
            }
            
            float deltaTime = Time.deltaTime;
            
            if (_entityManager != null)
            {
                _entityManager.Update(deltaTime);
            }
            
            if (_stateManager != null)
            {
                _stateManager.Update();
            }
            
            if (_skillManager != null)
            {
                _skillManager.Update(deltaTime);
            }
            
            if (_statusEffectManager != null)
            {
                _statusEffectManager.Update(deltaTime);
            }
            
            if (_projectileManager != null)
            {
                _projectileManager.Update(deltaTime);
            }
        }
        
        /// <summary>
        /// 检查战斗是否处于激活状态
        /// </summary>
        /// <returns>如果战斗处于激活状态则返回true</returns>
        private bool IsCombatActive()
        {
            // 检查当前战斗状态
            return _stateManager != null && 
                   (_stateManager.CurrentState == CombatStateType.Combat || 
                    _stateManager.CurrentState == CombatStateType.Preparing);
        }
        
        /// <summary>
        /// 关闭战斗模块
        /// </summary>
        public void Shutdown()
        {
            if (_entityManager != null)
            {
                _entityManager.Clear();
                _entityManager = null;
            }
            
            if (_stateManager != null)
            {
                _stateManager = null;
            }
            
            if (_skillManager != null)
            {
                _skillManager.Clear();
                _skillManager = null;
            }
            
            if (_statusEffectManager != null)
            {
                _statusEffectManager.Clear();
                _statusEffectManager = null;
            }
            
            if (_projectileManager != null)
            {
                _projectileManager.ClearAllProjectiles();
                _projectileManager = null;
            }
            
            Log.Info("[CombatModule] 关闭完成");
        }
        
        /// <summary>
        /// 创建实体
        /// </summary>
        public T CreateEntity<T>(EntityData data) where T : Entity, new()
        {
            return _entityManager.CreateEntity<T>(data);
        }
        
        /// <summary>
        /// 获取实体
        /// </summary>
        public Entity GetEntity(int entityId)
        {
            return _entityManager.GetEntity(entityId);
        }
        
        /// <summary>
        /// 移除实体
        /// </summary>
        public void RemoveEntity(int entityId)
        {
            _entityManager.RemoveEntity(entityId);
        }
        
        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartCombat()
        {
            _stateManager.ChangeState(CombatStateType.Combat);
        }
        
        /// <summary>
        /// 结束战斗
        /// </summary>
        public void EndCombat()
        {
            _stateManager.ChangeState(CombatStateType.Idle);
        }
        
        /// <summary>
        /// 获取当前战斗状态
        /// </summary>
        /// <returns>当前战斗状态类型</returns>
        public CombatStateType GetCombatState()
        {
            return _stateManager?.CurrentState ?? CombatStateType.Idle;
        }
        
        #region 技能系统接口
        
        /// <summary>
        /// 为实体添加技能
        /// </summary>
        public Skill AddSkillToEntity(Entity entity, int skillId)
        {
            return _skillManager?.AddSkillToEntity(entity, skillId) ?? null;
        }
        
        /// <summary>
        /// 获取实体的所有技能
        /// </summary>
        public List<Skill> GetEntitySkills(Entity entity)
        {
            return _skillManager?.GetEntitySkills(entity) ?? new List<Skill>();
        }
        
        /// <summary>
        /// 获取实体的指定类型技能
        /// </summary>
        public List<Skill> GetEntitySkillsByType(Entity entity, SkillType type)
        {
            return _skillManager?.GetEntitySkillsByType(entity, type) ?? new List<Skill>();
        }
        
        /// <summary>
        /// 获取范围内的所有实体
        /// </summary>
        public List<Entity> GetEntitiesInRange(Vector3 position, float range)
        {
            List<Entity> entitiesInRange = new List<Entity>();
            List<Entity> allEntities = _entityManager.GetAllEntities();
            
            foreach (var entity in allEntities)
            {
                if (entity.IsActive && Vector3.Distance(position, entity.Transform.position) <= range)
                {
                    entitiesInRange.Add(entity);
                }
            }
            
            return entitiesInRange;
        }
        
        /// <summary>
        /// 获取技能配置
        /// </summary>
        public SkillConfig GetSkillConfig(int skillId)
        {
            return _mockDataProvider?.GetSkillConfig(skillId);
        }
        
        #endregion
        
        #region 状态效果系统接口
        
        /// <summary>
        /// 添加属性修改状态效果
        /// </summary>
        public StatusEffect AddAttributeModifier(Entity source, Entity target, AttributeType attributeType, float value, float duration)
        {
            return _statusEffectManager?.ApplyAttributeModifier(source, target, attributeType, value, duration);
        }
        
        /// <summary>
        /// 添加持续伤害状态效果
        /// </summary>
        public StatusEffect AddDamageOverTime(Entity source, Entity target, float damagePerTick, float duration, float tickInterval, DamageType damageType)
        {
            return _statusEffectManager?.ApplyDamageOverTime(source, target, damagePerTick, duration, tickInterval, damageType);
        }
        
        /// <summary>
        /// 获取实体的所有状态效果
        /// </summary>
        public List<StatusEffect> GetEntityStatusEffects(Entity entity)
        {
            return _statusEffectManager?.GetEntityEffects(entity) ?? new List<StatusEffect>();
        }
        
        /// <summary>
        /// 获取实体的指定类型状态效果
        /// </summary>
        public List<StatusEffect> GetEntityStatusEffectsByType(Entity entity, StatusEffectType type)
        {
            return _statusEffectManager?.GetEntityEffectsByType(entity, type) ?? new List<StatusEffect>();
        }
        
        /// <summary>
        /// 移除实体的指定状态效果
        /// </summary>
        public bool RemoveStatusEffect(Entity entity, int effectId)
        {
            return _statusEffectManager != null && _statusEffectManager.RemoveEffect(entity, effectId);
        }
        
        /// <summary>
        /// 移除实体的所有状态效果
        /// </summary>
        public void RemoveAllStatusEffects(Entity entity)
        {
            _statusEffectManager?.RemoveAllEffects(entity);
        }
        
        #endregion
        
        #region 投射物系统接口
        
        /// <summary>
        /// 创建子弹投射物
        /// </summary>
        public Projectile CreateBullet(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType = DamageType.Physical, float speed = 15f, float lifetime = 5f)
        {
            return _projectileManager?.CreateBullet(source, position, direction, damage, damageType, speed, lifetime);
        }
        
        /// <summary>
        /// 创建箭投射物
        /// </summary>
        public Projectile CreateArrow(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType = DamageType.Physical, float speed = 20f, float lifetime = 8f)
        {
            return _projectileManager?.CreateArrow(source, position, direction, damage, damageType, speed, lifetime);
        }
        
        /// <summary>
        /// 创建魔法投射物
        /// </summary>
        public Projectile CreateMagicProjectile(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType = DamageType.Magic, float speed = 12f, float lifetime = 6f, float effectRadius = 0f)
        {
            return _projectileManager?.CreateMagicProjectile(source, position, direction, damage, damageType, speed, lifetime, effectRadius);
        }
        
        /// <summary>
        /// 创建追踪投射物
        /// </summary>
        public Projectile CreateHomingProjectile(Entity source, Entity target, Vector3 position, float damage, DamageType damageType = DamageType.Magic, float speed = 8f, float lifetime = 10f, float turnRate = 2.0f)
        {
            return _projectileManager?.CreateHomingProjectile(source, target, position, damage, damageType, speed, lifetime, turnRate);
        }
        
        /// <summary>
        /// 创建光束投射物
        /// </summary>
        public Projectile CreateBeam(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType = DamageType.Energy, float lifetime = 0.5f, float maxDistance = 50f)
        {
            return _projectileManager?.CreateBeam(source, position, direction, damage, damageType, lifetime, maxDistance);
        }
        
        /// <summary>
        /// 获取所有活动的投射物
        /// </summary>
        public List<Projectile> GetActiveProjectiles()
        {
            return _projectileManager?.GetActiveProjectiles() ?? new List<Projectile>();
        }
        
        /// <summary>
        /// 清除所有投射物
        /// </summary>
        public void ClearAllProjectiles()
        {
            _projectileManager?.ClearAllProjectiles();
        }
        
        /// <summary>
        /// 获取所有实体
        /// </summary>
        public List<Entity> GetAllEntities()
        {
            return _entityManager?.GetAllEntities() ?? new List<Entity>();
        }
        
        #endregion
    }
}
