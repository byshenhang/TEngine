using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if True
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
        /// <summary>
        /// 3D UI资源加载器
        /// </summary>
        public static IUI3DResourceLoader Resource;
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
        
#if True
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
            
            // 添加应用程序退出时的清理逻辑
            Application.quitting += OnApplicationQuit;
            
            // 初始化资源加载器
            Resource = new UI3DResourceLoader();
            
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
            CloseAllUI3D();
            
            // 资源无需手动释放，由 ResourceModule 管理
            
            // 销毁 XR Event System
#if True
            var eventSystem = GameObject.FindObjectOfType<EventSystem>();
            if (eventSystem != null && eventSystem.gameObject.name == "XR Event System")
            {
                Log.Info("Destroying XR Event System");
                GameObject.DestroyImmediate(eventSystem.gameObject);
            }
            
            // 销毁 Canvas Optimizer
            var optimizer = GameObject.FindObjectOfType<CanvasOptimizer>();
            if (optimizer != null)
            {
                Log.Info("Destroying UI3D Canvas Optimizer");
                GameObject.DestroyImmediate(optimizer.gameObject);
            }
#endif
            
            // 销毁根节点
            if (_uiRoot != null)
            {
                Log.Info("Destroying UI3DRoot");
                GameObject.DestroyImmediate(_uiRoot.gameObject);
                _uiRoot = null;
            }
            
            // 清理锁定点和窗口引用
            _anchorPoints.Clear();
            _activeWindows.Clear();
            
            Log.Info("UI3DModule released");
        }
        
        /// <summary>
        /// 查找 XR 相关组件
        /// </summary>
        private void FindXRComponents()
        {
#if True
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
        /// 显示/创建窗口
        /// </summary>
        /// <remarks>如果窗口已创建则更新位置，否则创建新窗口</remarks>
        private async UniTask<T> ShowUI3DWindowInternal<T>(Vector3 position, Quaternion rotation, bool isAsync = true, object[] userDatas = null) where T : UI3DWindow, new()
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
            
            // 设置窗口深度
            window.Depth = CalculateWindowDepth();
            
            // 确保窗口可见
            window.Visible = true;
            
            return window;
        }

        /// <summary>
        /// 显示窗口（在指定位置）
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <param name="position">世界坐标位置</param>
        /// <param name="rotation">世界旋转</param>
        /// <param name="userDatas">用户数据</param>
        /// <returns>窗口实例</returns>
        public async UniTask<T> ShowUI3D<T>(Vector3 position, Quaternion rotation, object[] userDatas = null) where T : UI3DWindow, new()
        {
            return await ShowUI3DWindowInternal<T>(position, rotation, true, userDatas);
        }

        /// <summary>
        /// 同步显示窗口（在指定位置）
        /// </summary>
        /// <typeparam name="T">窗口类型</typeparam>
        /// <param name="position">世界坐标位置</param>
        /// <param name="rotation">世界旋转</param>
        /// <param name="userDatas">用户数据</param>
        /// <returns>窗口实例</returns>
        public T ShowUI3DSync<T>(Vector3 position, Quaternion rotation, object[] userDatas = null) where T : UI3DWindow, new()
        {
            return ShowUI3DWindowInternal<T>(position, rotation, false, userDatas).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 在锚点处显示窗口
        /// </summary>
        public async UniTask<T> ShowUI3DAtAnchor<T>(string anchorId, object[] userDatas = null) where T : UI3DWindow, new()
        {
            UI3DAnchorPoint anchor = GetAnchor(anchorId);
            if (anchor == null)
            {
                Log.Error($"UI3D anchor {anchorId} not found");
                return null;
            }
            
            Type windowType = typeof(T);
            
            // 检查是否已存在相同类型窗口
            if (_activeWindows.TryGetValue(windowType, out var existingWindow))
            {
                // 已存在则直接设置为锚点模式
                existingWindow.SetPositionMode(UI3DPositionMode.AnchorBased, anchor.transform);
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
            
            // 准备资源 - 在创建时将锚点作为父级
            await window.PrepareAsync(assetPath, anchor.transform, userDatas);
            
            // 直接设置为锚点模式
            window.SetPositionMode(UI3DPositionMode.AnchorBased, anchor.transform);
            
            // 配置交互模式
            SetupWindowInteractionMode(window);
            
            // 添加到活动窗口集合
            _activeWindows[windowType] = window;
            
            // 设置窗口深度
            window.Depth = CalculateWindowDepth();
            
            // 确保窗口可见
            window.Visible = true;
            
            Log.Info($"UI3D window {windowType.Name} created at anchor {anchorId}");
            
            return window;
        }

        /// <summary>
        /// 同步在锚点处显示窗口
        /// </summary>
        public T ShowUI3DAtAnchorSync<T>(string anchorId, object[] userDatas = null) where T : UI3DWindow, new()
        {
            UI3DAnchorPoint anchor = GetAnchor(anchorId);
            if (anchor == null)
            {
                Log.Error($"UI3D anchor {anchorId} not found");
                return null;
            }
            
            Type windowType = typeof(T);
            
            // 检查是否已存在相同类型窗口
            if (_activeWindows.TryGetValue(windowType, out var existingWindow))
            {
                // 已存在则直接设置为锚点模式
                existingWindow.SetPositionMode(UI3DPositionMode.AnchorBased, anchor.transform);
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
            
            // 准备资源 - 直接使用锚点作为父级
            window.PrepareAsync(assetPath, anchor.transform, userDatas).GetAwaiter().GetResult();
            
            // 设置为锚点模式
            window.SetPositionMode(UI3DPositionMode.AnchorBased, anchor.transform);
            
            // 配置交互模式
            SetupWindowInteractionMode(window);
            
            // 添加到活动窗口集合
            _activeWindows[windowType] = window;
            
            // 设置窗口深度
            window.Depth = CalculateWindowDepth();
            
            // 确保窗口可见
            window.Visible = true;
            
            Log.Info($"UI3D window {windowType.Name} created at anchor {anchorId} (sync)");
            
            return window;
        }

        /// <summary>
        /// 根据类型名在锚点处显示窗口
        /// </summary>
        public async UniTask<UI3DWindow> ShowUI3DAtAnchorByType(string anchorId, string windowTypeName, object[] userDatas = null)
        {
            // 查找类型
            Type windowType = FindWindowType(windowTypeName);
            if (windowType == null)
            {
                Log.Error($"UI3D window type {windowTypeName} not found");
                return null;
            }
            
            // 反射调用通用方法
            MethodInfo method = typeof(UI3DModule).GetMethod(nameof(ShowUI3DAtAnchor));
            MethodInfo genericMethod = method.MakeGenericMethod(windowType);
            
            var task = (UniTask<UI3DWindow>)genericMethod.Invoke(this, new object[] { anchorId, userDatas });
            return await task;
        }

        /// <summary>
        /// 在用户前方显示窗口
        /// </summary>
        public async UniTask<T> ShowUI3DInFrontOfUser<T>(float distance = 1.5f, object[] userDatas = null) where T : UI3DWindow, new()
        {
            if (_xrRig == null)
            {
                Log.Error("Cannot show window in front of user: XR Rig not found");
                return null;
            }
            
            // 计算位置和旋转
            Vector3 position = _xrRig.position + _xrRig.forward * distance;
            Quaternion rotation = _xrRig.rotation;
            
            // 显示窗口
            T window = await ShowUI3DWindowInternal<T>(position, rotation, true, userDatas);
            
            if (window != null)
            {
                // 设置为相对用户位置模式
                window.SetRelativeToUser(new Vector3(0, 0, distance), Quaternion.identity);
            }
            
            return window;
        }

        /// <summary>
        /// 同步在用户前方显示窗口
        /// </summary>
        public T ShowUI3DInFrontOfUserSync<T>(float distance = 1.5f, object[] userDatas = null) where T : UI3DWindow, new()
        {
            if (_xrRig == null)
            {
                Log.Error("Cannot show window in front of user: XR Rig not found");
                return null;
            }
            
            // 计算位置和旋转
            Vector3 position = _xrRig.position + _xrRig.forward * distance;
            Quaternion rotation = _xrRig.rotation;
            
            // 显示窗口
            T window = ShowUI3DWindowInternal<T>(position, rotation, false, userDatas).GetAwaiter().GetResult();
            
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
        public void CloseUI3D<T>() where T : UI3DWindow
        {
            Type windowType = typeof(T);
            if (_activeWindows.TryGetValue(windowType, out var window))
            {
                window.InternalDestroy();
                _activeWindows.Remove(windowType);
            }
        }
        


        /// <summary>
        /// 关闭所有窗口
        /// </summary>
        public void CloseAllUI3D()
        {
            foreach (var window in _activeWindows.Values)
            {
                window.InternalDestroy();
            }
            _activeWindows.Clear();
        }
        


        /// <summary>
        /// 获取已打开的窗口实例
        /// </summary>
        public T GetUI3D<T>() where T : UI3DWindow
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
            return $"{windowType.Name}";
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
                window.InternalUpdate();
            }
        }
        
        /// <summary>
        /// 计算窗口深度值
        /// </summary>
        private int CalculateWindowDepth()
        {
            // 基础深度值
            int baseDepth = 1000;
            
            // 窗口数量影响深度
            return baseDepth + _activeWindows.Count * 10;
        }
        
        #region XR UI 交互系统支持
        
        /// <summary>
        /// 确保 XR 事件系统正确配置
        /// </summary>
        private void EnsureXREventSystem()
        {
#if True
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
#if True
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
#if True
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
        
        /// <summary>
        /// 应用程序退出时清理
        /// </summary>
        private void OnApplicationQuit()
        {
            Log.Info("UI3DModule: Application is quitting, cleaning up resources...");
            
            // 关闭所有窗口
            CloseAllUI3D();
            
            // 手动清理所有DontDestroyOnLoad对象
#if True
            // 清理XR事件系统
            var eventSystem = GameObject.FindObjectOfType<EventSystem>();
            if (eventSystem != null && eventSystem.gameObject.name == "XR Event System")
            {
                Log.Info("Application quitting: Destroying XR Event System");
                GameObject.DestroyImmediate(eventSystem.gameObject);
            }
            
            // 清理Canvas Optimizer
            var optimizer = GameObject.FindObjectOfType<CanvasOptimizer>();
            if (optimizer != null)
            {
                Log.Info("Application quitting: Destroying UI3D Canvas Optimizer");
                GameObject.DestroyImmediate(optimizer.gameObject);
            }
#endif
            
            // 清理UI3DRoot
            if (_uiRoot != null)
            {
                Log.Info("Application quitting: Destroying UI3DRoot");
                GameObject.DestroyImmediate(_uiRoot.gameObject);
                _uiRoot = null;
            }
            
            // 移除退出事件监听
            Application.quitting -= OnApplicationQuit;
            
            // 清理对象引用
            _anchorPoints.Clear();
            _activeWindows.Clear();
            _leftHandInteractor = null;
            _rightHandInteractor = null;
            _xrRig = null;
            
            Log.Info("UI3DModule cleanup completed");
        }
    }
}
