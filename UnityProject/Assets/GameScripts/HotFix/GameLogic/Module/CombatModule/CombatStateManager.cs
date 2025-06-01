using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6218u6597u72b6u6001u7ba1u7406u5668 - u7ba1u7406u6574u4f53u6218u6597u72b6u6001u8f6cu6362
    /// </summary>
    public class CombatStateManager
    {
        private CombatStateType _currentState = CombatStateType.Idle;
        private Action<CombatStateType> _onStateChanged;
        private float _stateTimer;
        
        /// <summary>
        /// u5f53u524du6218u6597u72b6u6001
        /// </summary>
        public CombatStateType CurrentState => _currentState;
        
        /// <summary>
        /// u6539u53d8u6218u6597u72b6u6001
        /// </summary>
        public void ChangeState(CombatStateType newState)
        {
            if (_currentState == newState)
            {
                return;
            }
            
            CombatStateType oldState = _currentState;
            _currentState = newState;
            _stateTimer = 0f;
            
            Log.Info($"[CombatStateManager] u6218u6597u72b6u6001u6539u53d8: {oldState} -> {newState}");
            
            OnStateChanged(oldState, newState);
            _onStateChanged?.Invoke(newState);
        }
        
        /// <summary>
        /// u66f4u65b0u6218u6597u72b6u6001
        /// </summary>
        public void Update()
        {
            _stateTimer += Time.deltaTime;
            
            // u6839u636eu5f53u524du72b6u6001u8fdbu884cu66f4u65b0
            switch (_currentState)
            {
                case CombatStateType.Idle:
                    UpdateIdleState();
                    break;
                case CombatStateType.Preparing:
                    UpdatePreparingState();
                    break;
                case CombatStateType.Combat:
                    UpdateCombatState();
                    break;
                case CombatStateType.Ending:
                    UpdateEndingState();
                    break;
            }
        }
        
        /// <summary>
        /// u66f4u65b0u7a7au95f2u72b6u6001
        /// </summary>
        private void UpdateIdleState()
        {
            // u7a7au95f2u72b6u6001u903bu8f91
        }
        
        /// <summary>
        /// u66f4u65b0u51c6u5907u72b6u6001
        /// </summary>
        private void UpdatePreparingState()
        {
            // u51c6u5907u72b6u6001u903bu8f91
            // u5982u679cu51c6u5907u5b8cu6210uff0cu5207u6362u5230u6218u6597u72b6u6001
            if (_stateTimer >= 2f) // u5047u8bbeu51c6u5907u65f6u95f42u79d2
            {
                ChangeState(CombatStateType.Combat);
            }
        }
        
        /// <summary>
        /// u66f4u65b0u6218u6597u72b6u6001
        /// </summary>
        private void UpdateCombatState()
        {
            // u6218u6597u72b6u6001u903bu8f91
        }
        
        /// <summary>
        /// u66f4u65b0u7ed3u675fu72b6u6001
        /// </summary>
        private void UpdateEndingState()
        {
            // u7ed3u675fu72b6u6001u903bu8f91
            // u5982u679cu7ed3u675fu5b8cu6210uff0cu5207u6362u56deu7a7au95f2u72b6u6001
            if (_stateTimer >= 3f) // u5047u8bbeu7ed3u675fu65f6u95f43u79d2
            {
                ChangeState(CombatStateType.Idle);
            }
        }
        
        /// <summary>
        /// u72b6u6001u6539u53d8u65f6u8c03u7528
        /// </summary>
        private void OnStateChanged(CombatStateType oldState, CombatStateType newState)
        {
            // u5904u7406u72b6u6001u8f6cu6362u903bu8f91
            switch (newState)
            {
                case CombatStateType.Preparing:
                    // u51c6u5907u6218u6597u65f6u7684u521du59cbu5316u903bu8f91
                    break;
                case CombatStateType.Combat:
                    // u8fdbu5165u6218u6597u72b6u6001u7684u521du59cbu5316u903bu8f91
                    break;
                case CombatStateType.Ending:
                    // u7ed3u675fu6218u6597u72b6u6001u7684u521du59cbu5316u903bu8f91
                    break;
                case CombatStateType.Idle:
                    // u8fd4u56deu7a7au95f2u72b6u6001u7684u521du59cbu5316u903bu8f91
                    break;
            }
        }
        
        /// <summary>
        /// u6ce8u518cu72b6u6001u6539u53d8u56deu8c03
        /// </summary>
        public void RegisterStateChangedCallback(Action<CombatStateType> callback)
        {
            _onStateChanged += callback;
        }
        
        /// <summary>
        /// u53d6u6d88u6ce8u518cu72b6u6001u6539u53d8u56deu8c03
        /// </summary>
        public void UnregisterStateChangedCallback(Action<CombatStateType> callback)
        {
            _onStateChanged -= callback;
        }
    }
}
