using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.XR;

namespace GameLogic
{
    /// <summary>
    /// u6218u6597u4ea4u4e92u5904u7406u5668 - u8d1fu8d23u5904u7406VRu63a7u5236u5668u8f93u5165u548cu8f6cu6362u4e3au6218u6597u6307u4ee4
    /// </summary>
    public class CombatInteractionHandler : IDisposable
    {
        // u6280u80fdu624bu52bfu6620u5c04 <u624bu52bfID, u6280u80fdID>
        private Dictionary<string, string> _gestureToSkillMap = new Dictionary<string, string>();
        
        // u5f53u524du73a9u5bb6u5b9eu4f53ID
        private string _playerEntityId;
        
        // u662fu5426u5df2u521du59cbu5316
        private bool _isInitialized = false;
        
        // u624bu90e8u8ddfu8e2au5668
        private XRNode _leftHandNode = XRNode.LeftHand;
        private XRNode _rightHandNode = XRNode.RightHand;
        
        // u5f53u524du9009u4e2du7684u76eeu6807
        private CombatEntityBase _currentTarget = null;
        
        // u6280u80fdu9009u62e9u6a21u5f0fu6fc0u6d3bu72b6u6001
        private bool _skillSelectionModeActive = false;
        
        // u6280u80fdu8f6eu76d8u9009u9879
        private List<string> _skillWheelOptions = new List<string>();
        
        // u5f53u524du9009u4e2du7684u6280u80fdID
        private string _currentSelectedSkillId = null;
        
        /// <summary>
        /// u521du59cbu5316u6218u6597u4ea4u4e92u5904u7406u5668
        /// </summary>
        public void Initialize(string playerEntityId)
        {
            if (_isInitialized)
            {
                Log.Warning("u6218u6597u4ea4u4e92u5904u7406u5668u5df2u7ecfu521du59cbu5316");
                return;
            }
            
            _playerEntityId = playerEntityId;
            
            // u6ce8u518cu624bu52bfu5230u6280u80fdu7684u6620u5c04
            RegisterDefaultGestureSkillMappings();
            
            // u6ce8u518cXRu8f93u5165u4e8bu4ef6
            RegisterXRInputEvents();
            
            _isInitialized = true;
            Log.Info("u6218u6597u4ea4u4e92u5904u7406u5668u521du59cbu5316u5b8cu6210");
        }
        
        /// <summary>
        /// u6ce8u518cu9ed8u8ba4u624bu52bfu5230u6280u80fdu7684u6620u5c04
        /// </summary>
        private void RegisterDefaultGestureSkillMappings()
        {
            // u5c06u624bu52bfIDu6620u5c04u5230u6280u80fdID
            // u8fd9u4e9bu6620u5c04u53efu4ee5u4eceu914du7f6eu6587u4ef6u52a0u8f7duff0cu8fd9u91ccu7b80u5316u76f4u63a5u786cu7f16u7801
            _gestureToSkillMap.Clear();
            
            // u53f3u624bu57fau7840u653bu51fbu624bu52bf
            _gestureToSkillMap["right_punch"] = "basic_attack";
            
            // u53f3u624bu91cdu51fbu624bu52bf
            _gestureToSkillMap["right_heavy_punch"] = "heavy_attack";
            
            // u53f3u624bu8303u56f4u653bu51fbu624bu52bf
            _gestureToSkillMap["right_sweep"] = "aoe_attack";
            
            // u5de6u624bu6cbbu7597u624bu52bf
            _gestureToSkillMap["left_palm_up"] = "basic_heal";
            
            // u5de6u624bu8303u56f4u6cbbu7597u624bu52bf
            _gestureToSkillMap["left_palm_wide"] = "aoe_heal";
            
            // u5176u4ed6u624bu52bf-u6280u80fdu6620u5c04
            _gestureToSkillMap["cross_arms"] = "shield";
            _gestureToSkillMap["both_hands_forward"] = "force_push";
            
            Log.Info("u6ce8u518cu4e86u9ed8u8ba4u624bu52bfu5230u6280u80fdu7684u6620u5c04");
        }
        
        /// <summary>
        /// u6ce8u518cXRu8f93u5165u4e8bu4ef6
        /// </summary>
        private void RegisterXRInputEvents()
        {
            // u5728u5b9eu9645u5b9eu73b0u4e2duff0cu5e94u4f7fu7528Unity XR Interaction Toolkitu6ce8u518cu4e8bu4ef6
            // u8fd9u91ccu53eau662fu63d0u4f9bu793au4f8bu4ee3u7801u6846u67b6
            
            // u4f8bu5982uff1au6ce8u518cu6273u673au6309u94aeu4e8bu4ef6uff0cu9009u62e9u76eeu6807
            // XRIModule.Instance.RegisterButtonEvent(XRButton.Trigger, XRNode.RightHand, OnRightTriggerPressed);
            
            // u6ce8u518cu6293u53d6u6309u94aeu4e8bu4ef6uff0cu6fc0u6d3bu6280u80fdu9009u62e9u6a21u5f0f
            // XRIModule.Instance.RegisterButtonEvent(XRButton.Grip, XRNode.LeftHand, OnLeftGripPressed);
            
            Log.Info("u6ce8u518cu4e86XRu8f93u5165u4e8bu4ef6");
        }
