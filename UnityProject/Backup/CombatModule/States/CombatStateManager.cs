using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6218u6597u72b6u6001u7c7bu578b
    /// </summary>
    public enum CombatStateType
    {
        None,
        BattleStart,
        BattleActive,
        BattlePause,
        BattleResume,
        BattleEnd,
        PlayerTurn,
        EnemyTurn,
        SkillCasting,
        SkillExecuting,
        CalculatingDamage,
        Victory,
        Defeat
    }
    
    /// <summary>
    /// u6218u6597u72b6u6001u4e0au4e0bu6587
    /// </summary>
    public class CombatStateContext
    {
        /// <summary>
        /// u6218u6597ID
        /// </summary>
        public string CombatId { get; set; }
        
        /// <summary>
        /// u73a9u5bb6IDu5217u8868
        /// </summary>
        public List<string> PlayerIds { get; set; } = new List<string>();
        
        /// <summary>
        /// u654cu4eba IDu5217u8868
        /// </summary>
        public List<string> EnemyIds { get; set; } = new List<string>();
        
        /// <summary>
        /// u5f53u524du56deu5408u6570
        /// </summary>
        public int CurrentTurn { get; set; }
        
        /// <summary>
        /// u5f53u524du884cu52a8u7684u5b9eu4f53ID
        /// </summary>
        public string CurrentActorId { get; set; }
        
        /// <summary>
        /// u6218u6597u7ed3u679c
        /// </summary>
        public CombatResult Result { get; set; } = CombatResult.Unknown;
        
        /// <summary>
        /// u989du5916u6570u636eu5b57u5178
        /// </summary>
        public Dictionary<string, object> ExtraData { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// u8bbeu7f6eu989du5916u6570u636e
        /// </summary>
        public void SetExtraData(string key, object value)
        {
            ExtraData[key] = value;
        }
        
        /// <summary>
        /// u83b7u53d6u989du5916u6570u636e
        /// </summary>
        public T GetExtraData<T>(string key, T defaultValue = default)
        {
            if (ExtraData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
    }
    
    /// <summary>
    /// u6218u6597u72b6u6001u7ba1u7406u5668 - u57fau4e8eu72b6u6001u6a21u5f0fu7ba1u7406u6218u6597u6d41u7a0b
    /// </summary>
    public class CombatStateManager : IDisposable
    {
        // u72b6u6001u5b9eu4f8bu5b58u50a8
        private Dictionary<CombatStateType, CombatStateBase> _states = new Dictionary<CombatStateType, CombatStateBase>();
        
        // u5f53u524du72b6u6001
        private CombatStateBase _currentState;
        
        // u5f53u524du72b6u6001u7c7bu578b
        private CombatStateType _currentStateType = CombatStateType.None;
        
        // u6218u6597u4e0au4e0bu6587
        private CombatStateContext _context = new CombatStateContext();
        
        /// <summary>
        /// u83b7u53d6u5f53u524du72b6u6001u7c7bu578b
        /// </summary>
        public CombatStateType CurrentStateType => _currentStateType;
        
        /// <summary>
        /// u83b7u53d6u5f53u524du6218u6597u4e0au4e0bu6587
        /// </summary>
        public CombatStateContext Context => _context;
        
        /// <summary>
        /// u521du59cbu5316u72b6u6001u7ba1u7406u5668
        /// </summary>
        public void Initialize()
        {
            // u6ce8u518cu9ed8u8ba4u72b6u6001
            RegisterState(CombatStateType.BattleStart, new BattleStartState());
            RegisterState(CombatStateType.BattleActive, new BattleActiveState());
            RegisterState(CombatStateType.BattlePause, new BattlePauseState());
            RegisterState(CombatStateType.BattleResume, new BattleResumeState());
            RegisterState(CombatStateType.BattleEnd, new BattleEndState());
            RegisterState(CombatStateType.PlayerTurn, new PlayerTurnState());
            RegisterState(CombatStateType.EnemyTurn, new EnemyTurnState());
            RegisterState(CombatStateType.SkillCasting, new SkillCastingState());
            RegisterState(CombatStateType.SkillExecuting, new SkillExecutingState());
            RegisterState(CombatStateType.CalculatingDamage, new CalculatingDamageState());
            RegisterState(CombatStateType.Victory, new VictoryState());
            RegisterState(CombatStateType.Defeat, new DefeatState());
            
            Log.Info("u6218u6597u72b6u6001u7ba1u7406u5668u521du59cbu5316u5b8cu6210");
        }
        
        /// <summary>
        /// u6ce8u518cu6218u6597u72b6u6001
        /// </summary>
        public void RegisterState(CombatStateType stateType, CombatStateBase state)
        {
            if (state == null)
            {
                Log.Error($"u65e0u6cd5u6ce8u518cu7a7au7684u6218u6597u72b6u6001: {stateType}");
                return;
            }
            
            if (_states.ContainsKey(stateType))
            {
                Log.Warning($"u8986u76d6u73b0u6709u6218u6597u72b6u6001: {stateType}");
            }
            
            _states[stateType] = state;
            state.SetManager(this);
            Log.Info($"u6ce8u518cu6218u6597u72b6u6001: {stateType}");
        }
        
        /// <summary>
        /// u66f4u65b0u72b6u6001
        /// </summary>
        public void Update()
        {
            _currentState?.OnUpdate();
        }
        
        /// <summary>
        /// u8fdbu5165u6307u5b9au72b6u6001
        /// </summary>
        public void TransitionTo(CombatStateType stateType, CombatStateContext context = null)
        {
            if (!_states.TryGetValue(stateType, out var newState))
            {
                Log.Error($"u5c1du8bd5u8fdbu5165u4e0du5b58u5728u7684u72b6u6001: {stateType}");
                return;
            }
            
            // u66f4u65b0u4e0au4e0bu6587
            if (context != null)
            {
                _context = context;
            }
            
            // u9000u51fau5f53u524du72b6u6001
            _currentState?.OnExit();
            
            // u8bb0u5f55u72b6u6001u53d8u5316
            Log.Info($"u6218u6597u72b6u6001u8f6cu6362: {_currentStateType} -> {stateType}");
            
            // u66f4u65b0u5f53u524du72b6u6001
            _currentState = newState;
            _currentStateType = stateType;
            
            // u8fdbu5165u65b0u72b6u6001
            _currentState.OnEnter(_context);
        }
        
        /// <summary>
        /// u91cau653eu8d44u6e90
        /// </summary>
        public void Dispose()
        {
            // u9000u51fau5f53u524du72b6u6001
            _currentState?.OnExit();
            _currentState = null;
            
            // u6e05u7406u6240u6709u72b6u6001
            foreach (var state in _states.Values)
            {
                state.Dispose();
            }
            _states.Clear();
            
            _context = null;
        }
    }
}
