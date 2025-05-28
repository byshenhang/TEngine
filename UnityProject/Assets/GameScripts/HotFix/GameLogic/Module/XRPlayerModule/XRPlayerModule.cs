using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using Unity.XR.CoreUtils;
using UnityEngine;

#if UNITY_EDITOR || ENABLE_XR
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
#endif

namespace GameLogic
{
    /// <summary>
    /// XR玩家模块 - 管理XR Origin (XR Rig)和相关交互功能
    /// </summary>
    public sealed class XRPlayerModule : Singleton<XRPlayerModule>, IUpdate
    {
        // XR Rig引用
        private Transform _xrRig;
        /// <summary> 获取XR Rig Transform </summary>
        public Transform XRRig => _xrRig;

#if UNITY_EDITOR || ENABLE_XR
        // XR Origin组件引用
        private XROrigin _xrOrigin;
        /// <summary> 获取XR Origin组件 </summary>
        public XROrigin XROrigin => _xrOrigin;

        // XR Camera引用
        private Camera _xrCamera;
        /// <summary> 获取XR相机 </summary>
        public Camera XRCamera => _xrCamera;

        // 手部控制器引用
        private XRController _leftHandController;
        private XRController _rightHandController;
        /// <summary> 左手控制器 </summary>
        public XRController LeftHandController => _leftHandController;
        /// <summary> 右手控制器 </summary>
        public XRController RightHandController => _rightHandController;
#endif

        // 交互事件字典
        private Dictionary<XRInteractionEventType, List<Action<object, object>>> _interactionEvents =
            new Dictionary<XRInteractionEventType, List<Action<object, object>>>();

        /// <summary> 模块初始化 </summary>
        protected override void OnInit()
        {
            Log.Info("XRPlayerModule 初始化中...");

            // 查找XR相关组件
            FindXRComponents();

            // 注册应用退出回调
            Application.quitting += OnApplicationQuit;

            Log.Info("XRPlayerModule 初始化完成");
        }

        /// <summary> 查找并缓存XR组件 </summary>
        private void FindXRComponents()
        {
#if UNITY_EDITOR || ENABLE_XR
            // 查找XR Origin
            _xrOrigin = GameObject.FindObjectOfType<XROrigin>();
            if (_xrOrigin != null)
            {
                _xrRig = _xrOrigin.transform;
                _xrCamera = _xrOrigin.Camera;
                Log.Info($"找到 XR Origin: {_xrOrigin.name}");
            }
            else
            {
                Log.Warning("场景中未找到 XR Origin");
                // 使用主相机作为备用
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _xrRig = mainCamera.transform.parent != null ? mainCamera.transform.parent : mainCamera.transform;
                    _xrCamera = mainCamera;
                    Log.Info("使用主相机作为XR Camera");
                }
                else
                {
                    Log.Error("未找到用于XR的相机 - 严重错误");
                }
            }

            // 查找控制器
            var controllers = GameObject.FindObjectsOfType<XRController>();
            foreach (var controller in controllers)
            {
                if (controller.name.Contains("Left") || controller.controllerNode == XRNode.LeftHand)
                {
                    _leftHandController = controller;
                    Log.Info($"找到左手控制器: {controller.name}");
                }
                else if (controller.name.Contains("Right") || controller.controllerNode == XRNode.RightHand)
                {
                    _rightHandController = controller;
                    Log.Info($"找到右手控制器: {controller.name}");
                }
            }
#else
            // 非XR模式下使用主相机
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _xrRig = mainCamera.transform.parent != null ? mainCamera.transform.parent : mainCamera.transform;
                Log.Info("非XR模式下使用主相机");
            }
#endif
        }

        /// <summary> 每帧更新 </summary>
        public void OnUpdate()
        {
#if UNITY_EDITOR || ENABLE_XR
            // 更新控制器输入
            UpdateControllerInput();
#endif
        }

#if UNITY_EDITOR || ENABLE_XR
        /// <summary> 更新XR控制器输入并触发交互事件 </summary>
        private void UpdateControllerInput()
        {
            // 更新左手输入
            if (_leftHandController != null)
            {
                var device = _leftHandController.inputDevice;
                // 检测扳机按钮
                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                {
                    TriggerInteractionEvent(XRInteractionEventType.TriggerPressed, _leftHandController, null);
                }
                // 检测抓取按钮
                if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed) && gripPressed)
                {
                    TriggerInteractionEvent(XRInteractionEventType.GripPressed, _leftHandController, null);
                }
            }
            // 更新右手输入，同理
            if (_rightHandController != null)
            {
                var device = _rightHandController.inputDevice;
                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                {
                    TriggerInteractionEvent(XRInteractionEventType.TriggerPressed, _rightHandController, null);
                }
                if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed) && gripPressed)
                {
                    TriggerInteractionEvent(XRInteractionEventType.GripPressed, _rightHandController, null);
                }
            }
        }
#endif

        /// <summary> 传送玩家到指定位置 </summary>
        /// <param name="position">目标位置</param>
        /// <param name="rotation">目标旋转（可选）</param>
        public void TeleportTo(Vector3 position, Quaternion? rotation = null)
        {
#if UNITY_EDITOR || ENABLE_XR
            var teleportProvider = GameObject.FindObjectOfType<TeleportationProvider>();
            if (teleportProvider != null && _xrRig != null)
            {
                var rotationValue = rotation.HasValue ? rotation.Value : _xrRig.rotation;
                teleportProvider.QueueTeleportRequest(new TeleportRequest()
                {
                    destinationPosition = position,
                    destinationRotation = rotationValue,
                    matchOrientation = MatchOrientation.TargetUp,
                });
                Log.Info($"已传送玩家到位置: {position}");
                return;
            }
#endif
            // 非XR或未找到TeleportProvider时直接移动
            if (_xrRig != null)
            {
                _xrRig.position = position;
                if (rotation.HasValue)
                    _xrRig.rotation = rotation.Value;
                Log.Info($"直接移动玩家到位置: {position}");
            }
        }

        /// <summary> 旋转玩家 </summary>
        /// <param name="degrees">旋转角度（度）</param>
        public void RotatePlayer(float degrees)
        {
            if (_xrRig != null)
            {
                _xrRig.Rotate(Vector3.up, degrees);
                Log.Info($"玩家旋转 {degrees} 度");
            }
        }

        /// <summary> 注册交互事件 </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="callback">回调函数</param>
        public void RegisterInteractionEvent(XRInteractionEventType eventType, Action<object, object> callback)
        {
            if (!_interactionEvents.ContainsKey(eventType))
                _interactionEvents[eventType] = new List<Action<object, object>>();
            _interactionEvents[eventType].Add(callback);
            Log.Info($"已注册 {eventType} 交互事件");
        }

        /// <summary> 取消注册交互事件 </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="callback">回调函数</param>
        public void UnregisterInteractionEvent(XRInteractionEventType eventType, Action<object, object> callback)
        {
            if (_interactionEvents.ContainsKey(eventType))
            {
                _interactionEvents[eventType].Remove(callback);
                Log.Info($"已取消 {eventType} 交互事件");
            }
        }

        /// <summary> 触发交互事件 </summary>
        private void TriggerInteractionEvent(XRInteractionEventType eventType, object interactor, object interactable)
        {
            if (_interactionEvents.TryGetValue(eventType, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback(interactor, interactable);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"{eventType} 事件回调错误: {ex.Message}");
                    }
                }
            }
        }

        /// <summary> 应用程序退出时的清理 </summary>
        private void OnApplicationQuit()
        {
            Log.Info("应用退出，清理XRPlayerModule资源...");
            _interactionEvents.Clear();
            Application.quitting -= OnApplicationQuit;
            _xrRig = null;
#if UNITY_EDITOR || ENABLE_XR
            _xrOrigin = null;
            _xrCamera = null;
            _leftHandController = null;
            _rightHandController = null;
#endif
            Log.Info("清理完成");
        }

        /// <summary> 模块释放 </summary>
        protected override void OnRelease()
        {
            _interactionEvents.Clear();
            Application.quitting -= OnApplicationQuit;
            Log.Info("XRPlayerModule 已释放");
        }
    }
}
