using System;
using System.Collections.Generic;
using System.Linq; // 添加LINQ引用以使用FirstOrDefault和First方法
using TEngine;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// 新版XR交互模块 - 基于Unity XR Interaction Toolkit的实现
    /// </summary>
    public sealed class XRInteractionModule : Singleton<XRInteractionModule>, IUpdate
    {
        // Unity XRI的交互管理器引用
        private UnityEngine.XR.Interaction.Toolkit.XRInteractionManager _unityXRInteractionManager;
        
        // XR原点 (XR Rig)
        private XROrigin _xrOrigin;
        
        // 左右手控制器
        private XRController _leftController;
        private XRController _rightController;
        
        // 控制器交互器
        private XRDirectInteractor _leftDirectInteractor;
        private XRDirectInteractor _rightDirectInteractor;
        private XRRayInteractor _leftRayInteractor;
        private XRRayInteractor _rightRayInteractor;
        
        // 事件总线引用
        private XRInteractionEventBus _eventBus;
        
        // 注册的交互器和交互物体
        private readonly List<XRBaseInteractor> _registeredInteractors = new List<XRBaseInteractor>();
        private readonly List<XRBaseInteractable> _registeredInteractables = new List<XRBaseInteractable>();
        
        // 场景交互管理器集合
        private readonly Dictionary<string, XRSceneInteractionManager> _sceneManagers = new Dictionary<string, XRSceneInteractionManager>();
        
        // 当前活动场景
        private string _activeSceneId;

        /// <summary>
        /// 获取Unity XRI交互管理器
        /// </summary>
        public UnityEngine.XR.Interaction.Toolkit.XRInteractionManager UnityXRInteractionManager => _unityXRInteractionManager;
        
        /// <summary>
        /// 获取XR原点
        /// </summary>
        public XROrigin XROrigin => _xrOrigin;
        
        /// <summary>
        /// 获取左手控制器
        /// </summary>
        public XRController LeftController => _leftController;
        
        /// <summary>
        /// 获取右手控制器
        /// </summary>
        public XRController RightController => _rightController;
        
        /// <summary>
        /// 获取左手直接交互器
        /// </summary>
        public XRDirectInteractor LeftDirectInteractor => _leftDirectInteractor;
        
        /// <summary>
        /// 获取右手直接交互器
        /// </summary>
        public XRDirectInteractor RightDirectInteractor => _rightDirectInteractor;
        
        /// <summary>
        /// 获取左手射线交互器
        /// </summary>
        public XRRayInteractor LeftRayInteractor => _leftRayInteractor;
        
        /// <summary>
        /// 获取右手射线交互器
        /// </summary>
        public XRRayInteractor RightRayInteractor => _rightRayInteractor;
        
        /// <summary>
        /// 模块初始化
        /// </summary>
        protected override void OnInit()
        {
            // 初始化XR交互系统组件
            InitializeXRComponents();
            
            // 初始化事件总线
            _eventBus = XRInteractionEventBus.Instance;
            
            // 设置事件监听
            SetupEventListeners();
            
            // 监听场景加载事件以更新交互器和交互物体
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Log.Info("NewXRInteractionModule 初始化完成");
        }
        
        /// <summary>
        /// 模块释放
        /// </summary>
        protected override void OnRelease()
        {
            // 注销XRI事件监听
            if (_unityXRInteractionManager != null)
            {
                _unityXRInteractionManager.interactorRegistered -= OnInteractorRegistered;
                _unityXRInteractionManager.interactorUnregistered -= OnInteractorUnregistered;
                _unityXRInteractionManager.interactableRegistered -= OnInteractableRegistered;
                _unityXRInteractionManager.interactableUnregistered -= OnInteractableUnregistered;
            }
            
            // 注销场景加载事件
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // 清理场景管理器
            _sceneManagers.Clear();
            
            // 清理注册列表
            _registeredInteractors.Clear();
            _registeredInteractables.Clear();
            
            Log.Info("NewXRInteractionModule 已释放");
        }
        
        /// <summary>
        /// 每帧更新
        /// </summary>
        public void OnUpdate()
        {
            // XRPlayerModule已经处理了控制器输入更新，这里只处理本模块特定的更新逻辑
            
            // 可以根据需要添加自定义逻辑，例如检测特定交互状态等
        }
        
        /// <summary>
        /// 初始化XR交互组件，尝试复用XRPlayerModule中已有的引用
        /// </summary>
        private void InitializeXRComponents()
        {
            // 1. 尝试从XRPlayerModule获取组件引用
            var xrPlayerModule = GameModule.XRPlayer;
            if (xrPlayerModule != null)
            {
                // 如果能够获取其引用，可以在这里进行，以避免重复创建
                Log.Info("从XRPlayerModule获取组件引用");
                
                // 这部分需要根据XRPlayerModule实际提供的API进行调整
            }
            
            // 2. 获取或创建Unity XRI交互管理器
            _unityXRInteractionManager = GameObject.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            if (_unityXRInteractionManager == null)
            {
                var managerObj = new GameObject("XR Interaction Manager");
                _unityXRInteractionManager = managerObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
                GameObject.DontDestroyOnLoad(managerObj);
                Log.Info("创建了新的XR Interaction Manager");
            }
            else
            {
                Log.Info("使用现有的XR Interaction Manager");
            }
            
            // 3. 获取XR原点和控制器
            _xrOrigin = GameObject.FindObjectOfType<XROrigin>();
            if (_xrOrigin == null)
            {
                Log.Warning("未找到XROrigin，某些功能可能无法正常工作");
            }
            else
            {
                // 查找左右手控制器
                var controllers = _xrOrigin.GetComponentsInChildren<XRController>(true);
                foreach (var controller in controllers)
                {
                    if (controller.controllerNode == UnityEngine.XR.XRNode.LeftHand)
                    {
                        _leftController = controller;
                        _leftDirectInteractor = controller.GetComponent<XRDirectInteractor>();
                        _leftRayInteractor = controller.GetComponent<XRRayInteractor>();
                    }
                    else if (controller.controllerNode == UnityEngine.XR.XRNode.RightHand)
                    {
                        _rightController = controller;
                        _rightDirectInteractor = controller.GetComponent<XRDirectInteractor>();
                        _rightRayInteractor = controller.GetComponent<XRRayInteractor>();
                    }
                }
                
                Log.Info($"找到控制器 - 左手: {_leftController != null}, 右手: {_rightController != null}");
            }
        }
        
        /// <summary>
        /// 设置交互事件监听
        /// </summary>
        private void SetupEventListeners()
        {
            if (_unityXRInteractionManager != null)
            {
                // 监听交互器和交互物体的注册/注销事件
                _unityXRInteractionManager.interactorRegistered += OnInteractorRegistered;
                _unityXRInteractionManager.interactorUnregistered += OnInteractorUnregistered;
                _unityXRInteractionManager.interactableRegistered += OnInteractableRegistered;
                _unityXRInteractionManager.interactableUnregistered += OnInteractableUnregistered;
                
                Log.Info("设置了XRI交互事件监听");
            }
        }
        
        /// <summary>
        /// 注册场景交互管理器
        /// </summary>
        public void RegisterSceneManager(XRSceneInteractionManager sceneManager)
        {
            if (sceneManager == null) return;
            
            string sceneId = sceneManager.SceneId;
            if (!_sceneManagers.ContainsKey(sceneId))
            {
                _sceneManagers.Add(sceneId, sceneManager);
                
                // 如果这是第一个场景，设为活动场景
                if (string.IsNullOrEmpty(_activeSceneId))
                {
                    _activeSceneId = sceneId;
                }
                
                Log.Info($"注册场景交互管理器: {sceneId}");
            }
        }
        
        /// <summary>
        /// 注销场景交互管理器
        /// </summary>
        public void UnregisterSceneManager(XRSceneInteractionManager sceneManager)
        {
            if (sceneManager == null) return;
            
            string sceneId = sceneManager.SceneId;
            if (_sceneManagers.ContainsKey(sceneId))
            {
                _sceneManagers.Remove(sceneId);
                
                // 如果注销的是当前活动场景，尝试激活其他场景
                if (_activeSceneId == sceneId && _sceneManagers.Count > 0)
                {
                    _activeSceneId = _sceneManagers.Keys.FirstOrDefault();
                }
                else if (_sceneManagers.Count == 0)
                {
                    _activeSceneId = null;
                }
                
                Log.Info($"注销场景交互管理器: {sceneId}");
            }
        }
        
        /// <summary>
        /// 设置活动场景
        /// </summary>
        public void SetActiveScene(string sceneId)
        {
            if (_sceneManagers.ContainsKey(sceneId))
            {
                _activeSceneId = sceneId;
                Log.Info($"设置活动交互场景: {sceneId}");
            }
        }
        
        /// <summary>
        /// 获取当前活动场景管理器
        /// </summary>
        public XRSceneInteractionManager GetActiveSceneManager()
        {
            if (!string.IsNullOrEmpty(_activeSceneId) && _sceneManagers.TryGetValue(_activeSceneId, out var manager))
            {
                return manager;
            }
            return null;
        }
        
        /// <summary>
        /// 分发交互事件到相应场景
        /// </summary>
        public void DispatchInteractionEvent(XRInteractionEventBase evt)
        {
            bool handled = false;
            
            // 首先尝试让当前活动场景处理
            var activeManager = GetActiveSceneManager();
            if (activeManager != null)
            {
                handled = activeManager.HandleInteractionEvent(evt);
            }
            
            // 如果活动场景没有处理，尝试其他场景
            if (!handled)
            {
                foreach (var manager in _sceneManagers.Values)
                {
                    if (manager != activeManager && manager.HandleInteractionEvent(evt))
                    {
                        handled = true;
                        break;
                    }
                }
            }
            
            // 如果没有场景处理，使用默认处理逻辑
            if (!handled)
            {
                HandleDefaultInteraction(evt);
            }
        }
        
        /// <summary>
        /// 默认交互事件处理
        /// </summary>
        private void HandleDefaultInteraction(XRInteractionEventBase evt)
        {
            // 默认处理逻辑 - 可以根据事件类型进行处理
            Log.Info($"默认处理交互事件: {evt.GetType().Name}");
        }
        
        #region Unity XRI 事件回调
        
        private void OnInteractorRegistered(InteractorRegisteredEventArgs args)
        {
            if (args.interactorObject is XRBaseInteractor interactor)
            {
                _registeredInteractors.Add(interactor);
                Log.Info($"注册交互器: {interactor.name}");
                
                // 设置交互器事件监听
                SetupInteractorEvents(interactor);
                
                // 创建并发送自定义事件
                var evt = new XRInteractorRegisteredEvent
                {
                    Interactor = interactor.gameObject,
                    Timestamp = Time.time,
                    InteractionID = Guid.NewGuid().ToString()
                };
                
                _eventBus.Publish(evt);
            }
        }
        
        private void OnInteractorUnregistered(InteractorUnregisteredEventArgs args)
        {
            if (args.interactorObject is XRBaseInteractor interactor)
            {
                _registeredInteractors.Remove(interactor);
                Log.Info($"注销交互器: {interactor.name}");
                
                // 移除交互器事件监听
                RemoveInteractorEvents(interactor);
            }
        }
        
        private void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {
            if (args.interactableObject is XRBaseInteractable interactable)
            {
                _registeredInteractables.Add(interactable);
                Log.Info($"注册交互物体: {interactable.name}");
                
                // 设置交互物体事件监听
                SetupInteractableEvents(interactable);
                
                // 创建并发送自定义事件
                var evt = new XRInteractableRegisteredEvent
                {
                    Interactable = interactable.gameObject,
                    Timestamp = Time.time,
                    InteractionID = Guid.NewGuid().ToString()
                };
                
                _eventBus.Publish(evt);
            }
        }
        
        private void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            if (args.interactableObject is XRBaseInteractable interactable)
            {
                _registeredInteractables.Remove(interactable);
                Log.Info($"注销交互物体: {interactable.name}");
                
                // 移除交互物体事件监听
                RemoveInteractableEvents(interactable);
            }
        }
        
        #endregion
        
        #region 交互组件事件设置
        
        /// <summary>
        /// 为交互器设置事件监听
        /// </summary>
        private void SetupInteractorEvents(XRBaseInteractor interactor)
        {
            // XRBaseInteractor事件监听
            interactor.hoverEntered.AddListener(OnHoverEntered);
            interactor.hoverExited.AddListener(OnHoverExited);
            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.selectExited.AddListener(OnSelectExited);
            
            // 特定类型的交互器可以添加更多监听
            if (interactor is XRDirectInteractor directInteractor)
            {
                // 直接交互器特定事件
            }
            else if (interactor is XRRayInteractor rayInteractor)
            {
                // 射线交互器特定事件
            }
        }
        
        /// <summary>
        /// 移除交互器事件监听
        /// </summary>
        private void RemoveInteractorEvents(XRBaseInteractor interactor)
        {
            interactor.hoverEntered.RemoveListener(OnHoverEntered);
            interactor.hoverExited.RemoveListener(OnHoverExited);
            interactor.selectEntered.RemoveListener(OnSelectEntered);
            interactor.selectExited.RemoveListener(OnSelectExited);
            
            // 特定类型的交互器移除更多监听
            if (interactor is XRDirectInteractor)
            {
                // 移除直接交互器特定事件
            }
            else if (interactor is XRRayInteractor)
            {
                // 移除射线交互器特定事件
            }
        }
        
        /// <summary>
        /// 为交互物体设置事件监听
        /// </summary>
        private void SetupInteractableEvents(XRBaseInteractable interactable)
        {
            // XRBaseInteractable事件监听
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
            
            // 特定类型的交互物体可以添加更多监听
            if (interactable is XRGrabInteractable grabInteractable)
            {
                // 可抓取物体特定事件
                grabInteractable.activated.AddListener(OnActivated);
                grabInteractable.deactivated.AddListener(OnDeactivated);
            }
        }
        
        /// <summary>
        /// 移除交互物体事件监听
        /// </summary>
        private void RemoveInteractableEvents(XRBaseInteractable interactable)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
            
            // 特定类型的交互物体移除更多监听
            if (interactable is XRGrabInteractable grabInteractable)
            {
                grabInteractable.activated.RemoveListener(OnActivated);
                grabInteractable.deactivated.RemoveListener(OnDeactivated);
            }
        }
        
        #endregion
        
        #region XRI标准事件处理方法
        
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // 创建并发送自定义事件
            var evt = new XRHoverEnterEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = args.interactableObject.transform.gameObject,
                Timestamp = Time.time,
                InteractionID = Guid.NewGuid().ToString()
            };
            
            _eventBus.Publish(evt);
            DispatchInteractionEvent(evt);
        }
        
        private void OnHoverExited(HoverExitEventArgs args)
        {
            // 创建并发送自定义事件
            var evt = new XRHoverExitEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = args.interactableObject.transform.gameObject,
                Timestamp = Time.time,
                InteractionID = Guid.NewGuid().ToString()
            };
            
            _eventBus.Publish(evt);
            DispatchInteractionEvent(evt);
        }
        
        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // 创建并发送自定义事件
            var evt = new XRSelectEnterEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = args.interactableObject.transform.gameObject,
                Timestamp = Time.time,
                InteractionID = Guid.NewGuid().ToString(),
                SelectType = 0 // 固定值，兼容不同版本
            };
            
            _eventBus.Publish(evt);
            DispatchInteractionEvent(evt);
        }
        
        private void OnSelectExited(SelectExitEventArgs args)
        {
            // 创建并发送自定义事件
            var evt = new XRSelectExitEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = args.interactableObject.transform.gameObject,
                Timestamp = Time.time,
                InteractionID = Guid.NewGuid().ToString(),
                ExitType = 0 // 固定值，兼容不同版本
            };
            
            _eventBus.Publish(evt);
            DispatchInteractionEvent(evt);
        }
        
        private void OnActivated(ActivateEventArgs args)
        {
            // 创建并发送自定义事件
            var evt = new XRActivateEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = args.interactableObject.transform.gameObject,
                Timestamp = Time.time,
                InteractionID = Guid.NewGuid().ToString()
            };
            
            _eventBus.Publish(evt);
            DispatchInteractionEvent(evt);
        }
        
        private void OnDeactivated(DeactivateEventArgs args)
        {
            // 创建并发送自定义事件
            var evt = new XRDeactivateEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = args.interactableObject.transform.gameObject,
                Timestamp = Time.time,
                InteractionID = Guid.NewGuid().ToString()
            };
            
            _eventBus.Publish(evt);
            DispatchInteractionEvent(evt);
        }
        
        #endregion
        
        /// <summary>
        /// 场景加载时更新交互系统
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 等待一帧以确保所有物体都已实例化
            // 在新场景中寻找场景交互管理器
            var managers = GameObject.FindObjectsOfType<XRSceneInteractionManager>();
            XRSceneInteractionManager newManager = null;
            
            foreach (var manager in managers)
            {
                if (manager.gameObject.scene == scene)
                {
                    newManager = manager;
                    break;
                }
            }
            
            if (newManager != null)
            {
                // 如果找到，设置为活动场景
                SetActiveScene(newManager.SceneId);
            }
        }
        
        /// <summary>
        /// 旋转玩家
        /// </summary>
        /// <param name="degrees">旋转角度（度）</param>
        public void RotatePlayer(float degrees)
        {
            if (_xrOrigin != null)
            {
                _xrOrigin.transform.Rotate(Vector3.up, degrees);
                Log.Info($"玩家旋转 {degrees} 度");
            }
        }
        
        /// <summary>
        /// 移动玩家
        /// </summary>
        /// <param name="direction">移动方向</param>
        /// <param name="distance">移动距离</param>
        public void MovePlayer(Vector3 direction, float distance)
        {
            if (_xrOrigin != null)
            {
                _xrOrigin.transform.position += direction.normalized * distance;
                Log.Info($"玩家移动 {distance} 米");
            }
        }
        
        /// <summary>
        /// 设置玩家位置
        /// </summary>
        /// <param name="position">世界坐标位置</param>
        public void SetPlayerPosition(Vector3 position)
        {
            if (_xrOrigin != null)
            {
                _xrOrigin.transform.position = position;
                Log.Info($"设置玩家位置到 {position}");
            }
        }
        
        /// <summary>
        /// 设置玩家旋转
        /// </summary>
        /// <param name="rotation">世界坐标旋转</param>
        public void SetPlayerRotation(Quaternion rotation)
        {
            if (_xrOrigin != null)
            {
                _xrOrigin.transform.rotation = rotation;
                Log.Info($"设置玩家旋转");
            }
        }
        
        /// <summary>
        /// 触发控制器震动
        /// </summary>
        /// <param name="isLeft">是否左手控制器</param>
        /// <param name="amplitude">振幅 (0-1)</param>
        /// <param name="duration">持续时间 (秒)</param>
        public void TriggerHapticFeedback(bool isLeft, float amplitude, float duration)
        {
            XRController controller = isLeft ? _leftController : _rightController;
            if (controller != null)
            {
                controller.SendHapticImpulse(amplitude, duration);
                Log.Info($"触发{(isLeft ? "左手" : "右手")}控制器震动");
            }
        }
    }
}
