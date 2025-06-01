using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u73a9u5bb6u5b9eu4f53 - u5b9eu73b0VRu73a9u5bb6u7279u6709u903bu8f91
    /// </summary>
    public class PlayerEntity : Entity
    {
        private VRInputComponent _vrInput;
        
        /// <summary>
        /// VRu8f93u5165u7ec4u4ef6
        /// </summary>
        public VRInputComponent VRInput => _vrInput;
        
        /// <summary>
        /// u521du59cbu5316u73a9u5bb6u5b9eu4f53
        /// </summary>
        public override void Init(int entityId, EntityData data)
        {
            base.Init(entityId, data);
            _entityType = EntityType.Player;
            
            // u521bu5efaVRu8f93u5165u7ec4u4ef6
            _vrInput = new VRInputComponent();
            _vrInput.Init(this);
            
            Log.Info($"[u73a9u5bb6u5b9eu4f53{_entityId}] u521du59cbu5316u5b8cu6210");
        }
        
        /// <summary>
        /// u521du59cbu5316u72b6u6001u673a
        /// </summary>
        protected override void InitStateMachine()
        {
            _stateMachine.AddState(EntityStateType.Idle, new PlayerIdleState());
            _stateMachine.AddState(EntityStateType.Combat, new PlayerCombatState());
            _stateMachine.AddState(EntityStateType.Dead, new PlayerDeadState());
            _stateMachine.AddState(EntityStateType.Attacking, new PlayerAttackingState());
            
            // u9ed8u8ba4u8bbeu7f6eu4e3au7a7au95f2u72b6u6001
            _stateMachine.ChangeState(EntityStateType.Idle);
        }
        
        /// <summary>
        /// u66f4u65b0u73a9u5bb6u5b9eu4f53
        /// </summary>
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            
            // u66f4u65b0VRu8f93u5165
            _vrInput?.Update(deltaTime);
        }
        
        /// <summary>
        /// u6b7bu4ea1u5904u7406
        /// </summary>
        protected override void Die(Entity killer)
        {
            base.Die(killer);
            
            // u73a9u5bb6u7279u6709u6b7bu4ea1u903bu8f91
            Log.Info($"[u73a9u5bb6u5b9eu4f53{_entityId}] u6b7bu4ea1");
            
            // u89e6u53d1u6e38u620fu7ed3u675fu4e8bu4ef6u7b49
        }
    }
    
    /// <summary>
    /// u73a9u5bb6u7a7au95f2u72b6u6001
    /// </summary>
    public class PlayerIdleState : StateBase
    {
        public override void Enter(Entity entity)
        {
            base.Enter(entity);
            
            // u73a9u5bb6u8fdbu5165u7a7au95f2u72b6u6001u7684u903bu8f91
            if (entity is PlayerEntity player)
            {
                // u53efu4ee5u64cdu4f5cu73a9u5bb6u7279u6709u5c5eu6027
            }
        }
        
        public override void Update(Entity entity)
        {
            base.Update(entity);
            
            // u5728u7a7au95f2u72b6u6001u4e0bu68c0u6d4bu662fu5426u9700u8981u5207u6362u5230u6218u6597u72b6u6001
            if (entity is PlayerEntity player)
            {
                // u4f8bu5982uff0cu68c0u6d4bu662fu5426u6709u654cu4ebau5728u8303u56f4u5185u6216u73a9u5bb6u662fu5426u5df2u7ecfu51c6u5907u597du653bu51fb
            }
        }
    }
    
    /// <summary>
    /// u73a9u5bb6u6218u6597u72b6u6001
    /// </summary>
    public class PlayerCombatState : StateBase
    {
        public override void Enter(Entity entity)
        {
            base.Enter(entity);
            
            // u73a9u5bb6u8fdbu5165u6218u6597u72b6u6001u7684u903bu8f91
        }
        
        public override void Update(Entity entity)
        {
            base.Update(entity);
            
            // u6218u6597u72b6u6001u4e0bu7684u66f4u65b0u903bu8f91
            if (entity is PlayerEntity player)
            {
                // u68c0u6d4bu653bu51fbu8f93u5165u7b49
                VRInputComponent input = player.VRInput;
                
                // u4f8bu5982uff0cu68c0u6d4bu662fu5426u6309u4e0bu53f3u624bu6273u673au8fdbu884cu653bu51fb
                if (input.IsButtonDown(VRInputType.RightTrigger))
                {
                    player.StateMachine.ChangeState(EntityStateType.Attacking);
                }
            }
        }
    }
    
    /// <summary>
    /// u73a9u5bb6u653bu51fbu72b6u6001
    /// </summary>
    public class PlayerAttackingState : StateBase
    {
        private float _attackTimer;
        private const float ATTACK_DURATION = 0.5f; // u653bu51fbu52a8u4f5cu6301u7eedu65f6u95f4
        
        public override void Enter(Entity entity)
        {
            base.Enter(entity);
            
            _attackTimer = 0f;
            
            // u5f00u59cbu653bu51fbu52a8u4f5c
            if (entity is PlayerEntity player)
            {
                // u89e6u53d1u653bu51fbu52a8u753bu6216u6548u679c
                Log.Info($"[u73a9u5bb6u5b9eu4f53{entity.EntityId}] u5f00u59cbu653bu51fb");
            }
        }
        
        public override void Update(Entity entity)
        {
            base.Update(entity);
            
            _attackTimer += Time.deltaTime;
            
            // u68c0u6d4bu653bu51fbu662fu5426u5b8cu6210
            if (_attackTimer >= ATTACK_DURATION)
            {
                // u653bu51fbu7ed3u675fuff0cu8fd4u56deu6218u6597u72b6u6001
                entity.StateMachine.ChangeState(EntityStateType.Combat);
            }
        }
    }
    
    /// <summary>
    /// u73a9u5bb6u6b7bu4ea1u72b6u6001
    /// </summary>
    public class PlayerDeadState : DeadState
    {
        public override void Enter(Entity entity)
        {
            base.Enter(entity);
            
            // u73a9u5bb6u7279u6709u6b7bu4ea1u903bu8f91
        }
    }
}
