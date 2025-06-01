using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u72b6u6001u63a5u53e3 - u5b9eu4f53u72b6u6001u7684u57fau7840u63a5u53e3
    /// </summary>
    public interface IState
    {
        void Enter(Entity entity);
        void Update(Entity entity);
        void Exit(Entity entity);
    }
    
    /// <summary>
    /// u72b6u6001u673au7cfbu7edf - u7ba1u7406u5b9eu4f53u72b6u6001u8f6cu6362
    /// </summary>
    public class StateMachine
    {
        private Entity _owner;
        private Dictionary<EntityStateType, IState> _states = new Dictionary<EntityStateType, IState>();
        private IState _currentState;
        private EntityStateType _currentStateType = EntityStateType.None;
        
        /// <summary>
        /// u5f53u524du72b6u6001u7c7bu578b
        /// </summary>
        public EntityStateType CurrentStateType => _currentStateType;
        
        /// <summary>
        /// u521bu5efau72b6u6001u673a
        /// </summary>
        public StateMachine(Entity owner)
        {
            _owner = owner;
        }
        
        /// <summary>
        /// u6dfbu52a0u72b6u6001
        /// </summary>
        public void AddState(EntityStateType stateType, IState state)
        {
            _states[stateType] = state;
        }
        
        /// <summary>
        /// u6539u53d8u5f53u524du72b6u6001
        /// </summary>
        public void ChangeState(EntityStateType newStateType)
        {
            // u68c0u67e5u65b0u72b6u6001u662fu5426u5b58u5728
            if (!_states.TryGetValue(newStateType, out IState newState))
            {
                Log.Warning($"[StateMachine] u5c1du8bd5u8f6cu6362u5230u4e0du5b58u5728u7684u72b6u6001: {newStateType}");
                return;
            }
            
            // u5982u679cu5f53u524du72b6u6001u4e0eu65b0u72b6u6001u76f8u540cuff0cu4e0du8fdbu884cu53d8u5316
            if (_currentStateType == newStateType)
            {
                return;
            }
            
            // u79fbu51fau5f53u524du72b6u6001
            if (_currentState != null)
            {
                _currentState.Exit(_owner);
            }
            
            // u8f6cu6362u5230u65b0u72b6u6001
            _currentState = newState;
            _currentStateType = newStateType;
            
            Log.Info($"[u5b9eu4f53{_owner.EntityId}] u72b6u6001u6539u53d8u4e3a: {_currentStateType}");
            
            // u8fbeu5230u65b0u72b6u6001
            _currentState.Enter(_owner);
        }
        
        /// <summary>
        /// u66f4u65b0u5f53u524du72b6u6001
        /// </summary>
        public void Update()
        {
            if (_currentState != null)
            {
                _currentState.Update(_owner);
            }
        }
    }
    
    /// <summary>
    /// u72b6u6001u57fau7c7b - u5b9eu73b0u57fau672cu7684u72b6u6001u903bu8f91
    /// </summary>
    public abstract class StateBase : IState
    {
        public virtual void Enter(Entity entity)
        {
            // u57fau7c7bu9ed8u8ba4u5b9eu73b0
        }
        
        public virtual void Update(Entity entity)
        {
            // u57fau7c7bu9ed8u8ba4u5b9eu73b0
        }
        
        public virtual void Exit(Entity entity)
        {
            // u57fau7c7bu9ed8u8ba4u5b9eu73b0
        }
    }
    
    /// <summary>
    /// u7a7au95f2u72b6u6001 - u57fau7840u72b6u6001u5b9eu73b0
    /// </summary>
    public class IdleState : StateBase
    {
        public override void Enter(Entity entity)
        {
            Log.Info($"[u5b9eu4f53{entity.EntityId}] u8fbeu5230u7a7au95f2u72b6u6001");
        }
        
        public override void Update(Entity entity)
        {
            // u7a7au95f2u72b6u6001u903bu8f91
        }
    }
    
    /// <summary>
    /// u6b7bu4ea1u72b6u6001 - u57fau7840u72b6u6001u5b9eu73b0
    /// </summary>
    public class DeadState : StateBase
    {
        public override void Enter(Entity entity)
        {
            Log.Info($"[u5b9eu4f53{entity.EntityId}] u8fbeu5230u6b7bu4ea1u72b6u6001");
            
            // u53efu4ee5u6dfbu52a0u6b7bu4ea1u52a8u753bu6216u6548u679c
        }
        
        public override void Update(Entity entity)
        {
            // u6b7bu4ea1u72b6u6001u903bu8f91
        }
    }
}
