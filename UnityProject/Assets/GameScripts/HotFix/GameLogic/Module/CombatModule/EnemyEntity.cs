using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u654cu4ebau5b9eu4f53 - u5b9eu73b0u654cu4ebau7279u6709u903bu8f91
    /// </summary>
    public class EnemyEntity : Entity
    {
        private float _attackCooldown;
        private float _attackTimer;
        private float _aiUpdateTimer;
        private const float AI_UPDATE_INTERVAL = 0.2f; // AIu51b3u7b56u95f4u9694
        
        /// <summary>
        /// u521du59cbu5316u654cu4ebau5b9eu4f53
        /// </summary>
        public override void Init(int entityId, EntityData data)
        {
            base.Init(entityId, data);
            _entityType = EntityType.Enemy;
            
            // u8bbeu7f6eu9ed8u8ba4u653bu51fbu51b7u537ju65f6u95f4
            _attackCooldown = 2f; // u9ed8u8ba42u79d2u51b7u537ju65f6u95f4
            _attackTimer = 0f;
            _aiUpdateTimer = 0f;
            
            Log.Info($"[u654cu4ebau5b9eu4f53{_entityId}] u521du59cbu5316u5b8cu6210");
        }
        
        /// <summary>
        /// u521du59cbu5316u72b6u6001u673a
        /// </summary>
        protected override void InitStateMachine()
        {
            _stateMachine.AddState(EntityStateType.Idle, new EnemyIdleState());
            _stateMachine.AddState(EntityStateType.Combat, new EnemyCombatState());
            _stateMachine.AddState(EntityStateType.Dead, new EnemyDeadState());
            _stateMachine.AddState(EntityStateType.Attacking, new EnemyAttackingState());
            
            // u9ed8u8ba4u8bbeu7f6eu4e3au7a7au95f2u72b6u6001
            _stateMachine.ChangeState(EntityStateType.Idle);
        }
        
        /// <summary>
        /// u66f4u65b0u654cu4ebau5b9eu4f53
        /// </summary>
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            
            // u66f4u65b0u653bu51fbu51b7u537ju65f6u95f4
            if (_attackTimer > 0)
            {
                _attackTimer -= deltaTime;
            }
            
            // u66f4u65b0AIu51b3u7b56
            _aiUpdateTimer += deltaTime;
            if (_aiUpdateTimer >= AI_UPDATE_INTERVAL)
            {
                _aiUpdateTimer = 0f;
                UpdateAI();
            }
        }
        
        /// <summary>
        /// u66f4u65b0AIu51b3u7b56
        /// </summary>
        private void UpdateAI()
        {
            // u57fau7840AIu903bu8f91uff0cu5728u672au6765u5c06u66ffu6362u4e3au884cu4e3au6811
            switch (_stateMachine.CurrentStateType)
            {
                case EntityStateType.Idle:
                    // u5728u7a7au95f2u72b6u6001u4e0bu68c0u6d4bu662fu5426u53d1u73b0u73a9u5bb6
                    if (IsPlayerInRange(10f)) // u5047u8bbeu68c0u6d4b10u7c73u8303u56f4u5185u662fu5426u6709u73a9u5bb6
                    {
                        _stateMachine.ChangeState(EntityStateType.Combat);
                    }
                    break;
                    
                case EntityStateType.Combat:
                    // u5728u6218u6597u72b6u6001u4e0bu68c0u6d4bu662fu5426u53efu4ee5u653bu51fb
                    if (_attackTimer <= 0 && IsPlayerInRange(3f)) // u5047u8bbeu68c0u6d4b3u7c73u8303u56f4u5185u662fu5426u6709u73a9u5bb6
                    {
                        _stateMachine.ChangeState(EntityStateType.Attacking);
                    }
                    else if (!IsPlayerInRange(15f)) // u5982u679cu73a9u5bb6u8d85u51fa15u7c73uff0cu8fd4u56deu7a7au95f2u72b6u6001
                    {
                        _stateMachine.ChangeState(EntityStateType.Idle);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// u68c0u6d4bu662fu5426u6709u73a9u5bb6u5728u8303u56f4u5185
        /// </summary>
        private bool IsPlayerInRange(float range)
        {
            // u8fd9u91ccu53efu4ee5u5b9eu73b0u5b9eu9645u7684u73a9u5bb6u68c0u6d4bu903bu8f91
            // u76eeu524du4f7fu7528u6a21u62dfu5b9eu73b0
            return true; // u6a21u62dfu59cbu7ec8u8fd4u56de
        }
        
        /// <summary>
        /// u8bbeu7f6eu653bu51fbu51b7u537ju65f6u95f4
        /// </summary>
        public void SetAttackOnCooldown()
        {
            _attackTimer = _attackCooldown;
        }
        
        /// <summary>
        /// u6b7bu4ea1u5904u7406
        /// </summary>
        protected override void Die(Entity killer)
        {
            base.Die(killer);
            
            // u654cu4ebau7279u6709u6b7bu4ea1u903bu8f91
            Log.Info($"[u654cu4ebau5b9eu4f53{_entityId}] u6b7bu4ea1");
            
            // u53efu4ee5u6dfbu52a0u6389u843du7269u54c1u6216u7ecfu9a8cu503cu7b49u903bu8f91
        }
    }
    
    /// <summary>
    /// u654cu4ebau7a7au95f2u72b6u6001
    /// </summary>
    public class EnemyIdleState : StateBase
    {
        public override void Enter(Entity entity)
        {
            base.Enter(entity);
            
            // u654cu4ebau8fdbu5165u7a7au95f2u72b6u6001u7684u903bu8f91
        }
    }
    
    /// <summary>
    /// u654cu4ebau6218u6597u72b6u6001
    /// </summary>
    public class EnemyCombatState : StateBase
    {
        public override void Enter(Entity entity)
        {
            base.Enter(entity);
            
            // u654cu4ebau8fdbu5165u6218u6597u72b6u6001u7684u903bu8f91
        }
    }
    
    /// <summary>
    /// u654cu4ebau653bu51fbu72b6u6001
    /// </summary>
    public class EnemyAttackingState : StateBase
    {
        private float _attackTimer;
        private const float ATTACK_DURATION = 1f; // u653bu51fbu52a8u4f5cu6301u7eedu65f6u95f4
        
        public override void Enter(Entity entity)
        {
            base.Enter(entity);
            
            _attackTimer = 0f;
            
            // u5f00u59cbu653bu51fbu52a8u4f5c
            if (entity is EnemyEntity enemy)
            {
                // u89e6u53d1u653bu51fbu52a8u753bu6216u6548u679c
                Log.Info($"[u654cu4ebau5b9eu4f53{entity.EntityId}] u5f00u59cbu653bu51fb");
                
                // TODO: u5b9eu73b0u5b9eu9645u7684u4f24u5bb3u903bu8f91
                // u627eu5230u73a9u5bb6u5e76u9020u6210u4f24u5bb3
                // PlayerEntity player = ...
                // if (player != null)
                // {
                //     player.TakeDamage(enemy.Attributes.GetAttribute(AttributeType.Attack), enemy);
                // }
            }
        }
        
        public override void Update(Entity entity)
        {
            base.Update(entity);
            
            _attackTimer += Time.deltaTime;
            
            // u68c0u6d4bu653bu51fbu662fu5426u5b8cu6210
            if (_attackTimer >= ATTACK_DURATION)
            {
                if (entity is EnemyEntity enemy)
                {
                    // u8bbeu7f6eu653bu51fbu51b7u537ju65f6u95f4
                    enemy.SetAttackOnCooldown();
                }
                
                // u653bu51fbu7ed3u675fuff0cu8fd4u56deu6218u6597u72b6u6001
                entity.StateMachine.ChangeState(EntityStateType.Combat);
            }
        }
    }
    
    /// <summary>
    /// u654cu4ebau6b7bu4ea1u72b6u6001
    /// </summary>
    public class EnemyDeadState : DeadState
    {
        public override void Enter(Entity entity)
        {
            base.Enter(entity);
            
            // u654cu4ebau7279u6709u6b7bu4ea1u903bu8f91
        }
    }
}
