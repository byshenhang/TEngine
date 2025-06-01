using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR || ENABLE_XR
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
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

        // XR Origin预制体路径
        private const string XR_ORIGIN_PREFAB_PATH = "Prefabs/XR/XROrigin";
        
        // XR Camera引用
        private Camera _xrCamera;
        /// <summary> 获取XR相机 </summary>
        public Camera XRCamera => _xrCamera;
        
        // 后处理管理器
        private XRPostProcessManager _postProcessManager;
        /// <summary> 获取后处理管理器 </summary>
        public XRPostProcessManager PostProcessManager => _postProcessManager;

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
            
            // 初始化后处理管理器
            InitializePostProcessing();

            // 注册应用退出回调
            Application.quitting += OnApplicationQuit;

            Log.Info("XRPlayerModule 初始化完成");
        }
        
        /// <summary> 初始化后处理管理器 </summary>
        private void InitializePostProcessing()
        {
            if (_xrCamera != null)
            {
                _postProcessManager = new XRPostProcessManager();
                _postProcessManager.Initialize(_xrCamera);
                Log.Info("后处理管理器初始化完成");
            }
            else
            {
                Log.Warning("无法初始化后处理管理器：XR相机为空");
            }
        }

        /// <summary> 查找并缓存XR组件，如果不存在则尝试创建 </summary>
        /// <param name="forceDynamicCreation">强制动态创建，即使场景中已存在也会替换</param>
        private void FindXRComponents(bool forceDynamicCreation = false)
        {
#if UNITY_EDITOR || ENABLE_XR
            if (!forceDynamicCreation)
            {
                // 查找现有XR Origin
                _xrOrigin = GameObject.FindObjectOfType<XROrigin>();
            }

            // 如果需要强制创建或没有找到现有的XR Origin
            if (forceDynamicCreation || _xrOrigin == null)
            {
                // 尝试动态创建XR Origin
                TryCreateXROrigin();
            }

            // 确认XR Origin是否存在并设置引用
            if (_xrOrigin != null)
            {
                _xrRig = _xrOrigin.transform;
                _xrCamera = _xrOrigin.Camera;
                
                // 确保XR Origin在场景切换时不被销毁
                if (_xrOrigin.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    GameObject.DontDestroyOnLoad(_xrOrigin.gameObject);
                    Log.Info("XR Origin设置为跨场景持久化");
                }
                
                Log.Info($"XR Origin已准备就绪: {_xrOrigin.name}");
            }
            else
            {
                Log.Warning("无法找到或创建XR Origin，尝试使用备用方案");
                // 使用主相机作为备用
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _xrRig = mainCamera.transform.parent != null ? mainCamera.transform.parent : mainCamera.transform;
                    _xrCamera = mainCamera;
                    Log.Info("使用主相机作为XR Camera备用方案");
                }
                else
                {
                    Log.Error("未找到用于XR的相机 - 严重错误");
                }
            }

            // 查找并关联控制器
            FindAndSetupControllers();
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
                Log.Info($"已传送玩家到位置: {position}");
            }
        }

        /// <summary> 旋转玩家 </summary>
        /// <param name="degrees">旋转角度（度）</param>
        /// <param name="smooth">是否平滑旋转</param>
        /// <param name="duration">平滑旋转持续时间（秒）</param>
        public void RotatePlayer(float degrees, bool smooth = false, float duration = 0.5f)
        {
            if (_xrRig != null)
            {
                if (!smooth)
                {
                    _xrRig.Rotate(Vector3.up, degrees);
                    Log.Info($"玩家旋转 {degrees} 度");
                }
                else
                {
                    // 平滑旋转
                    SmoothRotateAsync(degrees, duration).Forget();
                }
            }
        }
        
        /// <summary> 平滑旋转玩家 </summary>
        private async UniTaskVoid SmoothRotateAsync(float degrees, float duration)
        {
            if (_xrRig == null || duration <= 0) return;
            
            float elapsedTime = 0;
            Quaternion startRotation = _xrRig.rotation;
            Quaternion targetRotation = startRotation * Quaternion.Euler(0, degrees, 0);
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                // 使用平滑插值曲线
                t = t * t * (3f - 2f * t); // 平滑步进函数
                
                _xrRig.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                await UniTask.Yield();
            }
            
            // 确保最终旋转精确
            _xrRig.rotation = targetRotation;
            Log.Info($"玩家平滑旋转 {degrees} 度完成");
        }
        
#if UNITY_EDITOR || ENABLE_XR
        /// <summary> 尝试动态创建XR Origin </summary>
        private bool TryCreateXROrigin()
        {
            try
            {
                // 尝试从Resources加载预制体
                GameObject xrOriginPrefab = Resources.Load<GameObject>(XR_ORIGIN_PREFAB_PATH);
                
                if (xrOriginPrefab != null)
                {
                    // 实例化XR Origin
                    GameObject instance = GameObject.Instantiate(xrOriginPrefab);
                    instance.name = "XR Origin (Dynamic)";
                    _xrOrigin = instance.GetComponent<XROrigin>();
                    
                    if (_xrOrigin == null)
                    {
                        Log.Error("创建的预制体没有XROrigin组件");
                        GameObject.Destroy(instance);
                        return false;
                    }
                    
                    Log.Info("已动态创建XR Origin");
                    return true;
                }
                else
                {
                    Log.Warning($"找不到XR Origin预制体: {XR_ORIGIN_PREFAB_PATH}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"动态创建XR Origin失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary> 查找并设置控制器 </summary>
        private void FindAndSetupControllers()
        {
            // 重置控制器引用
            _leftHandController = null;
            _rightHandController = null;
            
            // 查找控制器
            var controllers = GameObject.FindObjectsOfType<XRController>();
            if (controllers.Length == 0)
            {
                Log.Warning("未找到XR控制器");
                return;
            }
            
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
            
            // 确保光线交互器正确设置
            SetupRayInteractors();
        }
        
        /// <summary> 设置光线交互器 </summary>
        private void SetupRayInteractors()
        {
            // 为左右手控制器设置光线交互器
            if (_leftHandController != null)
            {
                XRRayInteractor leftRayInteractor = _leftHandController.GetComponent<XRRayInteractor>();
                if (leftRayInteractor != null)
                {
                    // 可以在这里设置左手光线交互器的属性
                    Log.Info("左手光线交互器已配置");
                }
            }
            
            if (_rightHandController != null)
            {
                XRRayInteractor rightRayInteractor = _rightHandController.GetComponent<XRRayInteractor>();
                if (rightRayInteractor != null)
                {
                    // 可以在这里设置右手光线交互器的属性
                    Log.Info("右手光线交互器已配置");
                }
            }
        }
#endif
        
        /// <summary> 获取当前玩家位置 </summary>
        public Vector3 GetPlayerPosition()
        {
            return _xrRig != null ? _xrRig.position : Vector3.zero;
        }
        
        /// <summary> 获取当前玩家旋转 </summary>
        public Quaternion GetPlayerRotation()
        {
            return _xrRig != null ? _xrRig.rotation : Quaternion.identity;
        }
        
        /// <summary> 获取玩家头部/相机位置 </summary>
        public Vector3 GetHeadPosition()
        {
#if UNITY_EDITOR || ENABLE_XR
            return _xrCamera != null ? _xrCamera.transform.position : (_xrRig != null ? _xrRig.position : Vector3.zero);
#else
            return _xrRig != null ? _xrRig.position : Vector3.zero;
#endif
        }
        
        /// <summary> 获取玩家头部前方方向 </summary>
        public Vector3 GetHeadForward()
        {
#if UNITY_EDITOR || ENABLE_XR
            return _xrCamera != null ? _xrCamera.transform.forward : (_xrRig != null ? _xrRig.forward : Vector3.forward);
#else
            return _xrRig != null ? _xrRig.forward : Vector3.forward;
#endif
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
            CleanupResources();
            Log.Info("XR资源清理完成");
        }
        
        /// <summary> 清理所有XR资源 </summary>
        private void CleanupResources()
        {
            _interactionEvents.Clear();
            Application.quitting -= OnApplicationQuit;
            
            // 清理后处理管理器
            if (_postProcessManager != null)
            {
                _postProcessManager.Release();
                _postProcessManager = null;
            }
            
            _xrRig = null;
#if UNITY_EDITOR || ENABLE_XR
            _xrOrigin = null;
            _xrCamera = null;
            _leftHandController = null;
            _rightHandController = null;
#endif
        }
        
        /// <summary> 刷新XR Origin和控制器 </summary>
        /// <param name="forceDynamicCreation">是否强制重新创建XR Origin</param>
        public void RefreshXRComponents(bool forceDynamicCreation = false)
        {
            Log.Info("开始刷新XR组件...");
            FindXRComponents(forceDynamicCreation);
            
            // 重新初始化后处理管理器
            if (_xrCamera != null)
            {
                InitializePostProcessing();
            }
            
            Log.Info("XR组件刷新完成");
        }
        
        #region 后处理相关方法
        
        /// <summary> 切换后处理配置文件 </summary>
        /// <param name="profileName">配置文件名称</param>
        /// <returns>是否切换成功</returns>
        public bool SwitchPostProcessingProfile(string profileName)
        {
            if (_postProcessManager != null)
            {
                return _postProcessManager.SwitchProfile(profileName);
            }
            return false;
        }
        
        /// <summary> 启用或禁用后处理效果 </summary>
        /// <param name="enabled">是否启用</param>
        public void EnablePostProcessing(bool enabled)
        {
            if (_postProcessManager != null)
            {
                _postProcessManager.SetEnabled(enabled);
            }
        }
        
        /// <summary> 获取当前使用的后处理配置文件名称 </summary>
        /// <returns>当前配置文件名称，如果未设置则返回null</returns>
        public string GetCurrentPostProcessingProfile()
        {
            if (_postProcessManager != null)
            {
                return _postProcessManager.GetCurrentProfileName();
            }
            return null;
        }
        
        /// <summary> 设置后处理效果权重 </summary>
        /// <param name="weight">权重值(0-1)</param>
        public void SetPostProcessingWeight(float weight)
        {
            if (_postProcessManager != null)
            {
                _postProcessManager.SetVolumeWeight(weight);
            }
        }
        
        /// <summary> 平滑过渡到指定后处理配置文件 </summary>
        /// <param name="profileName">目标配置文件名称</param>
        /// <param name="duration">过渡时长(秒)</param>
        public void TransitionToPostProcessingProfile(string profileName, float duration = 1.0f)
        {
            if (_postProcessManager != null)
            {
                _postProcessManager.TransitionToProfile(profileName, duration).Forget();
            }
        }
        
        #endregion

        /// <summary> 模块释放 </summary>
        protected override void OnRelease()
        {
            _interactionEvents.Clear();
            Application.quitting -= OnApplicationQuit;
            Log.Info("XRPlayerModule 已释放");
        }
    }
}
