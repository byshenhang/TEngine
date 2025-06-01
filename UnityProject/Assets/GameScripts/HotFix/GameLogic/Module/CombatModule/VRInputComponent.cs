using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// VRu8f93u5165u7ec4u4ef6 - u5904u7406VRu63a7u5236u5668u7684u8f93u5165u72b6u6001
    /// </summary>
    public class VRInputComponent
    {
        private Entity _owner;
        private Dictionary<VRInputType, VRInputState> _inputStates = new Dictionary<VRInputType, VRInputState>();
        
        /// <summary>
        /// u521du59cbu5316VRu8f93u5165u7ec4u4ef6
        /// </summary>
        public void Init(Entity owner)
        {
            _owner = owner;
            
            // u521du59cbu5316u8f93u5165u72b6u6001
            _inputStates[VRInputType.LeftGrip] = new VRInputState();
            _inputStates[VRInputType.RightGrip] = new VRInputState();
            _inputStates[VRInputType.LeftTrigger] = new VRInputState();
            _inputStates[VRInputType.RightTrigger] = new VRInputState();
            _inputStates[VRInputType.LeftPrimary] = new VRInputState();
            _inputStates[VRInputType.RightPrimary] = new VRInputState();
            _inputStates[VRInputType.LeftSecondary] = new VRInputState();
            _inputStates[VRInputType.RightSecondary] = new VRInputState();
            
            Log.Info($"[VRInputComponent] u521du59cbu5316u5b8cu6210");
        }
        
        /// <summary>
        /// u66f4u65b0VRu8f93u5165u72b6u6001
        /// </summary>
        public void Update(float deltaTime)
        {
            // u66f4u65b0u6240u6709u8f93u5165u72b6u6001
            UpdateInputState(VRInputType.LeftGrip, GetInputValue(VRInputType.LeftGrip));
            UpdateInputState(VRInputType.RightGrip, GetInputValue(VRInputType.RightGrip));
            UpdateInputState(VRInputType.LeftTrigger, GetInputValue(VRInputType.LeftTrigger));
            UpdateInputState(VRInputType.RightTrigger, GetInputValue(VRInputType.RightTrigger));
            UpdateInputState(VRInputType.LeftPrimary, GetInputValue(VRInputType.LeftPrimary));
            UpdateInputState(VRInputType.RightPrimary, GetInputValue(VRInputType.RightPrimary));
            UpdateInputState(VRInputType.LeftSecondary, GetInputValue(VRInputType.LeftSecondary));
            UpdateInputState(VRInputType.RightSecondary, GetInputValue(VRInputType.RightSecondary));
            
            // u68c0u6d4bu624bu52bf
            DetectGestures();
        }
        
        /// <summary>
        /// u66f4u65b0u8f93u5165u72b6u6001
        /// </summary>
        private void UpdateInputState(VRInputType type, float currentValue)
        {
            if (_inputStates.TryGetValue(type, out VRInputState state))
            {
                state.PreviousValue = state.CurrentValue;
                state.CurrentValue = currentValue;
                state.IsPressed = currentValue > 0.5f;
                state.IsDown = !state.WasPressed && state.IsPressed;
                state.IsUp = state.WasPressed && !state.IsPressed;
                state.WasPressed = state.IsPressed;
            }
        }
        
        /// <summary>
        /// u83b7u53d6u8f93u5165u503c
        /// </summary>
        private float GetInputValue(VRInputType type)
        {
            // u4eceu5b9eu9645u7684XRu8f93u5165u7cfbu7edfu83b7u53d6u503c
            // u8fd9u91ccu6682u65f6u4f7fu7528u6a21u62dfu503cuff0cu540eu7eedu9700u8981u96c6u6210u5230u5b9eu9645u7684XRu8f93u5165u7cfbu7edf
            
            // u6a21u62dfu968fu673au8f93u5165u505au6d4bu8bd5
            if (UnityEngine.Random.value > 0.95f) // 5%u7684u6982u7387u8fd4u56deu6309u4e0bu72b6u6001
            {
                return 1.0f;
            }
            return 0.0f;
        }
        
        /// <summary>
        /// u8f93u5165u6309u94aeu662fu5426u5f53u524du5904u4e8eu6309u4e0bu72b6u6001
        /// </summary>
        public bool IsButtonPressed(VRInputType type)
        {
            if (_inputStates.TryGetValue(type, out VRInputState state))
            {
                return state.IsPressed;
            }
            return false;
        }
        
        /// <summary>
        /// u8f93u5165u6309u94aeu662fu5426u521au521au6309u4e0b
        /// </summary>
        public bool IsButtonDown(VRInputType type)
        {
            if (_inputStates.TryGetValue(type, out VRInputState state))
            {
                return state.IsDown;
            }
            return false;
        }
        
        /// <summary>
        /// u8f93u5165u6309u94aeu662fu5426u521au521au677eu5f00
        /// </summary>
        public bool IsButtonUp(VRInputType type)
        {
            if (_inputStates.TryGetValue(type, out VRInputState state))
            {
                return state.IsUp;
            }
            return false;
        }
        
        /// <summary>
        /// u83b7u53d6u6309u94aeu5f53u524du503c
        /// </summary>
        public float GetButtonValue(VRInputType type)
        {
            if (_inputStates.TryGetValue(type, out VRInputState state))
            {
                return state.CurrentValue;
            }
            return 0f;
        }
        
        /// <summary>
        /// u68c0u6d4bu624bu52bf
        /// </summary>
        private void DetectGestures()
        {
            // u5b9eu73b0u624bu52bfu8bc6u522bu903bu8f91
            // u8fd9u91ccu53efu4ee5u5b9eu73b0u590du6742u7684u624bu52bfu68c0u6d4buff0cu4f8bu5982u63d0u524du624bu638fu3001u62f3u5934u3001u6307u5411u7b49
            // u57fau4e8eu624bu90e8u59ffu6001u548cu63a7u5236u5668u6309u94aeu7ec4u5408u5b9eu73b0u4e0du540cu7684u624bu52bf
            
            // TODO: u5b9eu73b0u5b9eu9645u7684u624bu52bfu8bc6u522bu903bu8f91
        }
    }
    
    /// <summary>
    /// VRu8f93u5165u72b6u6001 - u8bb0u5f55u6309u94aeu7684u5f53u524du72b6u6001
    /// </summary>
    public class VRInputState
    {
        public float PreviousValue = 0f; // u4e0au4e00u5e27u7684u503c
        public float CurrentValue = 0f;  // u5f53u524du503c
        public bool IsPressed = false;   // u5f53u524du662fu5426u6309u4e0b
        public bool WasPressed = false;  // u4e0au4e00u5e27u662fu5426u6309u4e0b
        public bool IsDown = false;      // u662fu5426u521au521au6309u4e0b
        public bool IsUp = false;        // u662fu5426u521au521au677eu5f00
    }
}
