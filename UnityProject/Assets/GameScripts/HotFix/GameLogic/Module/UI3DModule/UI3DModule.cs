using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR || ENABLE_XR
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace GameLogic
{
    /// <summary>
    /// 3D UI 管理模块
    /// </summary>
    public sealed class UI3DModule : Singleton<UI3DModule>, IUpdate
    {
        // 3D UI 根节点
        private Transform _uiRoot;
        public Transform transform => _uiRoot;
        
        // 场景中的锚点集合
        private readonly Dictionary<string, UI3DAnchorPoint> _anchorPoints = new();
        
        // 活动的 3D UI 窗口集合
        private readonly Dictionary<Type, UI3DWindow> _activeWindows = new();
        
        // XR Rig 引用
        private Transform _xrRig;
        /// <summary>
        /// XR Rig 引用
        /// </summary>
        public Transform XRRig => _xrRig;
        
#if UNITY_EDITOR || ENABLE_XR
        // 手部射线交互器引用
        private XRRayInteractor _leftHandInteractor;
        private XRRayInteractor _rightHandInteractor;
        
        /// <summary>
        /// 左手射线交互器
        /// </summary>
        public XRRayInteractor LeftHandInteractor => _leftHandInteractor;
        
        /// <summary>
        /// 右手射线交互器
        /// </summary>
        public XRRayInteractor RightHandInteractor => _rightHandInteractor;
#endif
        
        /// <summary>
        /// 模块初始化
        /// </summary>
        protected override void OnInit()
        {
            // 创建根节点
            GameObject rootObj = new GameObject("UI3DRoot");
            GameObject.DontDestroyOnLoad(rootObj);
            _uiRoot = rootObj.transform;
            
            // 使用已有的 ResourceModule，无需额外资源加载器
            
            // 确保事件系统和 Canvas 优化器初始化
            EnsureXREventSystem();
            SetupCanvasOptimizer();
            
            // 查找并缓存 XR Rig 及交互器
            FindXRComponents();
            
            // 扫描场景中的锚点
            ScanSceneAnchors();
            
            Log.Info("UI3DModule initialized");
        }
        
        /// <summary>
        /// 模块释放
        /// </summary>
        protected override void OnRelease()
        {
            // 关闭所有窗口
            CloseAllWindows();
            
            // 资源无需手动释放，由 ResourceModule 管理
            
            // 销毁根节点
            if (_uiRoot != null)
            {
                GameObject.Destroy(_uiRoot.gameObject);
                _uiRoot = null;
            }
            
            Log.Info("UI3DModule released");
        }
        
        /// <summary>
        /// 查找 XR 相关组件
        /// </summary>
        private void FindXRComponents()
        {
#if UNITY_EDITOR || ENABLE_XR
            // 查找 XR Rig
            var xrRigs = GameObject.FindObjectsOfType<XRRig>();
            if (xrRigs != null && xrRigs.Length > 0)
            {
                _xrRig = xrRigs[0].transform;
            }
            else
            {
                var cameras = GameObject.FindObjectsOfType<Camera>();
                foreach (var camera in cameras)
                {
                    if (camera.CompareTag("MainCamera"))
                    {
                        _xrRig = camera.transform;
                        break;
                    }
                }
            }
            
            // 查找交互器
            var interactors = GameObject.FindObjectsOfType<XRRayInteractor>();
            foreach (var interactor in interactors)
            {
                if (interactor.name.Contains("Left"))
                    _leftHandInteractor = interactor;
                else if (interactor.name.Contains("Right"))
                    _rightHandInteractor = interactor;
            }
#else
            // 非 XR 平台，使用主摄像机作为 XR Rig
            var cameras = GameObject.FindObjectsOfType<Camera>();
            foreach (var camera in cameras)
            {
                if (camera.CompareTag("MainCamera"))
                {
                    _xrRig = camera.transform;
                    break;
                }
            }
#endif
            
            if (_xrRig == null)
            {
                Log.Warning("XR Rig not found, using main camera instead");
                _xrRig = Camera.main?.transform;
            }
        }
        
        #region 锚点管理
        
        /// <summary>
        /// 扫描场景中的锚点
        /// </summary>
        public void ScanSceneAnchors()
        {
            var anchors = GameObject.FindObjectsOfType<UI3DAnchorPoint>();
            foreach (var anchor in anchors)
            {
                RegisterAnchor(anchor);
            }
            
            Log.Info($"Found {_anchorPoints.Count} UI3D anchor points in scene");
        }
        
        /// <summary>
        /// 注册锚点
        /// </summary>
        public void RegisterAnchor(UI3DAnchorPoint anchor)
        {
            if (anchor != null && !string.IsNullOrEmpty(anchor.AnchorId))
            {
                _anchorPoints[anchor.AnchorId] = anchor;
            }
        }
        
        /// <summary>
        /// 取消注册锚点
        /// </summary>
        public void UnregisterAnchor(UI3DAnchorPoint anchor)
        {
            if (anchor != null && !string.IsNullOrEmpty(anchor.AnchorId))
            {
                _anchorPoints.Remove(anchor.AnchorId);
            }
        }
        
        /// <summary>
        /// 获取指定锚点
        /// </summary>
        public UI3DAnchorPoint GetAnchor(string anchorId)
        {
            if (string.IsNullOrEmpty(anchorId))
                return null;
                
            _anchorPoints.TryGetValue(anchorId, out var anchor);
            return anchor;
        }
        
        /// <summary>
        /// 获取指定分组的所有锚点
        /// </summary>
        public List<UI3DAnchorPoint> GetAnchorsInGroup(string groupName)
        {
            List<UI3DAnchorPoint> result = new List<UI3DAnchorPoint>();
            
            foreach (var anchor in _anchorPoints.Values)
            {
                if (anchor.AnchorGroup == groupName)
                {
                    result.Add(anchor);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 查找最近的锚点
        /// </summary>
        public UI3DAnchorPoint FindNearestAnchor(Vector3 position, float maxDistance = float.MaxValue, string groupFilter = null)
        {
            UI3DAnchorPoint nearest = null;
            float nearestDistance = maxDistance;
            int highestPriority = int.MinValue;
            
            foreach (var anchor in _anchorPoints.Values)
            {
                // 过滤分组
                if (!string.IsNullOrEmpty(groupFilter) && anchor.AnchorGroup != groupFilter)
                    continue;
                    
                float distance = Vector3.Distance(position, anchor.transform.position);
                
                // 优先最近距离，其次优先级
                if (distance < nearestDistance || 
                    (Math.Abs(distance - nearestDistance) < 0.01f && anchor.Priority > highestPriority))
                {
                    nearest = anchor;
                    nearestDistance = distance;
                    highestPriority = anchor.Priority;
                }
            }
            
            return nearest;
        }
        
        #endregion
        
        #region 窗口管理
        
        /// <summary>
        /// 创建窗口
        /// </summary>
        public async UniTask<T> CreateWindow<T>(Vector3 position, Quaternion rotation, object[] userDatas = null) where T : UI3DWindow, new()
        {
            Type windowType = typeof(T);
            
            // 检查是否已存在相同类型窗口
            if (_activeWindows.TryGetValue(windowType, out var existingWindow))
            {
                // 已存在则更新位置
                existingWindow.SetWorldPosition(position, rotation);
                return existingWindow as T;
            }
            
            // 获取资源路径
            string assetPath = GetWindowAssetPath<T>();
            if (string.IsNullOrEmpty(assetPath))
            {
                Log.Error($"UI3D window {windowType.Name} has no asset path defined");
                return null;
            }
            
            // 创建窗口实例
            T window = new T();
            
            // 准备资源
            await window.PrepareAsync(assetPath, _uiRoot, userDatas);
            
            // 设置位置
            window.SetWorldPosition(position, rotation);
            
            // 配置交互模式
            SetupWindowInteractionMode(window);
            
            // 添加到活动窗口集合
            _activeWindows[windowType] = window;
            
            // 调用显示回调
            window.OnShow();
            
            return window;
        }

        /// <summary>
        /// 在锚点处创建窗口
        /// </summary>
        public async UniTask<T> CreateWindowAtAnchor<T>(string anchorId, object[] userDatas = null) where T : UI3DWindow, new()
        {
            UI3DAnchorPoint anchor = GetAnchor(anchorId);
            if (anchor == null)
            {
                Log.Error($"UI3D anchor {anchorId} not found");
                return null;
            }
            
            T window = await CreateWindow<T>(anchor.transform.position, anchor.transform.rotation, userDatas);
            
            if (window != null)
            {
                // 设置为基于锚点位置模式
                window.SetPositionMode(UI3DPositionMode.AnchorBased, anchor.transform);
            }
            
            return window;
        }

        /// <summary>
        /// 根据类型名在锚点处创建窗口
        /// </summary>
        public async UniTask<UI3DWindow> CreateWindowAtAnchorByType(string anchorId, string windowTypeName, object[] userDatas = null)
        {
            // 查找类型
            Type windowType = FindWindowType(windowTypeName);
            if (windowType == null)
            {
                Log.Error($"UI3D window type {windowTypeName} not found");
                return null;
            }
            
            // 反射调用通用方法
            MethodInfo method = typeof(UI3DModule).GetMethod(nameof(CreateWindowAtAnchor));
            MethodInfo genericMethod = method.MakeGenericMethod(windowType);
            
            var task = (UniTask<UI3DWindow>)genericMethod.Invoke(this, new object[] { anchorId, userDatas });
            return await task;
        }

        /// <summary>
        /// 在用户前方创建窗口
        /// </summary>
        public async UniTask<T> CreateWindowInFrontOfUser<T>(float distance = 1.5f, object[] userDatas = null) where T : UI3DWindow, new()
        {
            if (_xrRig == null)
            {
                Log.Error("Cannot create window in front of user: XR Rig not found");
                return null;
            }
            
            // 计算位置和旋转
            Vector3 position = _xrRig.position + _xrRig.forward * distance;
            Quaternion rotation = _xrRig.rotation;
            
            // 创建窗口
            T window = await CreateWindow<T>(position, rotation, userDatas);
            
            if (window != null)
            {
                // 设置为相对用户位置模式
                window.SetRelativeToUser(new Vector3(0, 0, distance), Quaternion.identity);
            }
            
            return window;
        }

        /// <summary>
        /// 关闭指定窗口
        /// </summary>
        public void CloseWindow<T>() where T : UI3DWindow
        {
            Type windowType = typeof(T);
            if (_activeWindows.TryGetValue(windowType, out var window))
            {
                window.OnHide();
                window.OnDestroy();
                _activeWindows.Remove(windowType);
            }
        }

        /// <summary>
        /// 关闭所有窗口
        /// </summary>
        public void CloseAllWindows()
        {
            foreach (var window in _activeWindows.Values)
            {
                window.OnHide();
                window.OnDestroy();
            }
            _activeWindows.Clear();
        }

        /// <summary>
        /// 获取已打开的窗口实例
        /// </summary>
        public T GetWindow<T>() where T : UI3DWindow
        {
            Type windowType = typeof(T);
            _activeWindows.TryGetValue(windowType, out var window);
            return window as T;
        }

        /// <summary>
        /// 获取窗口资源路径
        /// </summary>
        private string GetWindowAssetPath<T>() where T : UI3DWindow
        {
            Type windowType = typeof(T);
            
            // 获取 SceneUIAttribute 特性
            SceneUIAttribute attribute = windowType.GetCustomAttribute<SceneUIAttribute>();
            if (attribute != null)
            {
                return attribute.AssetPath;
            }
            
            // 默认命名规则
            return $"UI3D/{windowType.Name}";
        }

        /// <summary>
        /// 设置窗口交互模式
        /// </summary>
        private void SetupWindowInteractionMode(UI3DWindow window)
        {
            Type windowType = window.GetType();
            
            SceneUIAttribute attribute = windowType.GetCustomAttribute<SceneUIAttribute>();
            if (attribute != null)
            {
                window.SetGrabbable(attribute.Grabbable);
                window.SetInteractionMode(attribute.InteractionMode);
            }
        }

        /// <summary>
        /// 根据类型名查找窗口类型
        /// </summary>
        private Type FindWindowType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null && typeof(UI3DWindow).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    return type;
                }
            }
            
            // 在命名空间 GameLogic 中查找
            return Type.GetType($"GameLogic.{typeName}") ?? Type.GetType(typeName);
        }
        
        #endregion
        
        /// <summary>
        /// 更新方法
        /// </summary>
        public void OnUpdate()
        {
            // 更新所有活动窗口
            foreach (var window in _activeWindows.Values)
            {
                window.OnUpdate();
            }
        }
        
        #region XR UI 交互系统支持
        
        /// <summary>
        /// 确保 XR 事件系统正确配置
        /// </summary>
        private void EnsureXREventSystem()
        {
#if UNITY_EDITOR || ENABLE_XR
            var eventSystem = GameObject.FindObjectOfType<EventSystem>();
            
            // 如果没有事件系统，创建一个
            if (eventSystem == null)
            {
                var eventSystemGO = new GameObject("XR Event System");
                eventSystem = eventSystemGO.AddComponent<EventSystem>();
                GameObject.DontDestroyOnLoad(eventSystemGO);
            }
            
            // 移除 StandaloneInputModule，避免与 XR UI Input Module 冲突
            var standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
            {
                Log.Warning("Removing StandaloneInputModule as it conflicts with XR UI Input Module");
                GameObject.DestroyImmediate(standaloneInputModule);
            }
            
            // 确保存在 XR UI Input Module
            var xrUIInputModule = eventSystem.GetComponent<XRUIInputModule>();
            if (xrUIInputModule == null)
            {
                Log.Info("Adding XRUIInputModule to Event System");
                xrUIInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }
#endif
        }
        
        /// <summary>
        /// 设置 Canvas 优化器
        /// </summary>
        private void SetupCanvasOptimizer()
        {
#if UNITY_EDITOR || ENABLE_XR
            // 查找或创建 Canvas 优化器
            CanvasOptimizer optimizer = GameObject.FindObjectOfType<CanvasOptimizer>();
            
            if (optimizer == null)
            {
                var optimizerGO = new GameObject("UI3D Canvas Optimizer");
                GameObject.DontDestroyOnLoad(optimizerGO);
                
                try
                {
                    optimizer = optimizerGO.AddComponent<CanvasOptimizer>();
                    Log.Info("Canvas Optimizer created for UI3D performance optimization");
                }
                catch (Exception e)
                {
                    Log.Warning($"Canvas Optimizer not supported in current Unity version: {e.Message}");
                    GameObject.Destroy(optimizerGO);
                }
            }
#endif
        }
        
        /// <summary>
        /// 注册 Canvas 到优化器
        /// </summary>
        public void RegisterCanvasToOptimizer(Canvas canvas)
        {
#if UNITY_EDITOR || ENABLE_XR
            try
            {
                var optimizer = GameObject.FindObjectOfType<CanvasOptimizer>();
                if (optimizer != null && canvas != null)
                {
                    var method = optimizer.GetType().GetMethod("RegisterCanvas");
                    if (method != null)
                    {
                        method.Invoke(optimizer, new object[] { canvas });
                        Log.Info($"Registered canvas {canvas.name} to optimizer");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to register canvas to optimizer: {e.Message}");
            }
#endif
        }
        
        #endregion
    }
}
