using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// u6218u6597u72b6u6001u57fau7c7b - u6240u6709u6218u6597u72b6u6001u7684u62bdu8c61u57fau7c7b
    /// </summary>
    public abstract class CombatStateBase : IDisposable
    {
        // u7236u72b6u6001u7ba1u7406u5668u5f15u7528
        protected CombatStateManager _manager;
        
        // u5f53u524du72b6u6001u4e0au4e0bu6587
        protected CombatStateContext _context;
        
        /// <summary>
        /// u8bbeu7f6eu7236u72b6u6001u7ba1u7406u5668
        /// </summary>
        public void SetManager(CombatStateManager manager)
        {
            _manager = manager;
        }
        
        /// <summary>
        /// u8fdbu5165u72b6u6001u65f6u8c03u7528
        /// </summary>
        public virtual void OnEnter(CombatStateContext context)
        {
            _context = context;
            Log.Info($"u8fdbu5165u6218u6597u72b6u6001: {GetType().Name}");
        }
        
        /// <summary>
        /// u72b6u6001u66f4u65b0u65f6u8c03u7528
        /// </summary>
        public virtual void OnUpdate()
        {
            // u7531u5b50u7c7bu5b9eu73b0
        }
        
        /// <summary>
        /// u9000u51fau72b6u6001u65f6u8c03u7528
        /// </summary>
        public virtual void OnExit()
        {
            Log.Info($"u9000u51fau6218u6597u72b6u6001: {GetType().Name}");
        }
        
        /// <summary>
        /// u8f6cu6362u5230u65b0u72b6u6001
        /// </summary>
        protected void TransitionTo(CombatStateType newState, CombatStateContext context = null)
        {
            _manager?.TransitionTo(newState, context);
        }
        
        /// <summary>
        /// u91cau653eu8d44u6e90
        /// </summary>
        public virtual void Dispose()
        {
            _manager = null;
            _context = null;
        }
    }
}
