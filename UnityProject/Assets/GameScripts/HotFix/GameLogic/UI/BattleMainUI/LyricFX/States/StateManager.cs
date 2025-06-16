using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LyricFX.States
{
    /// <summary>
    /// 状态管理器，负责状态转换和条件判断
    /// </summary>
    public class StateManager
    {
        private CharacterState _currentState = CharacterState.Waiting;
        private Dictionary<CharacterState, List<StateTransition>> _transitions = new Dictionary<CharacterState, List<StateTransition>>();
        
        public CharacterState CurrentState => _currentState;
        
        // 状态变化事件
        public event Action<CharacterState, CharacterState> OnStateChanged;
        
        /// <summary>
        /// 添加状态转换条件
        /// </summary>
        public void AddTransition(CharacterState fromState, CharacterState toState, 
            Func<bool> condition = null)
        {
            if (!_transitions.ContainsKey(fromState))
                _transitions[fromState] = new List<StateTransition>();

            _transitions[fromState].Add(new StateTransition(toState, condition ?? (() => true)));
        }
        
        /// <summary>
        /// 转换到新状态
        /// </summary>
        public async UniTask<bool> TransitionTo(CharacterState newState, CancellationToken token = default)
        {
            if (newState == _currentState) return false;
            
            var oldState = _currentState;
            _currentState = newState;
            
            OnStateChanged?.Invoke(oldState, newState);
            
            // 检查是否有自动转换
            if (_transitions.TryGetValue(newState, out var possibleTransitions))
            {
                // 查找可以自动转换的状态
                foreach (var transition in possibleTransitions)
                {
                    if (await transition.ShouldTransition(token))
                    {
                        await TransitionTo(transition.TargetState, token);
                        break;
                    }
                }
            }
            
            return true;
        }
    }

    /// <summary>
    /// 状态转换类，包含目标状态和转换条件
    /// </summary>
    public class StateTransition
    {
        private Func<bool> _condition;
        private UniTask<bool> _asyncCondition;
        private bool _isAsyncCondition;
        
        public CharacterState TargetState { get; }
        
        public StateTransition(CharacterState targetState, Func<bool> condition)
        {
            TargetState = targetState;
            _condition = condition;
            _isAsyncCondition = false;
        }
        
        public StateTransition(CharacterState targetState, Func<UniTask<bool>> asyncCondition)
        {
            TargetState = targetState;
            _asyncCondition = asyncCondition();
            _isAsyncCondition = true;
        }
        
        public async UniTask<bool> ShouldTransition(CancellationToken token = default)
        {
            if (_isAsyncCondition)
                return await _asyncCondition.AttachExternalCancellation(token);
                
            return _condition();
        }
    }
}
