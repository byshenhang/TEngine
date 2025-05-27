using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace GameLogic
{
    /// <summary>
    /// 3D UI管理模块，专门用于VR环境中的3D界面管理。
    /// </summary>
    public sealed class UI3DModule : Singleton<UI3DModule>, IUpdate
    {
        // 核心字段
        private static Transform _instanceRoot = null;          // 3D UI根节点变换组件
        private bool _enableErrorLog = true;                    // 是否启用错误日志
        private Camera _xrCamera = null;                        // XR摄像机
        private readonly List<UI3DWindow> _ui3DWindows = new List<UI3DWindow>(32); // 3D窗口列表
        private ErrorLogger _errorLogger;                       // 错误日志记录器

        // 常量定义
        public const int LAYER_DEEP = 2000;
        public const int WINDOW_DEEP = 100;
        public const int WINDOW_HIDE_LAYER = 2; // Ignore Raycast
        public const int WINDOW_SHOW_LAYER = 5; // UI

        /// <summary>
        /// UI根节点访问属性
        /// </summary>
        public static Transform UIRoot => _instanceRoot;

        /// <summary>
        /// XR摄像机访问属性
        /// </summary>
        public Camera XRCamera => _xrCamera;

        /// <summary>
        /// 模块初始化（自动调用）。
        /// 1. 查找场景中的UI3DRoot
        /// 2. 配置错误日志系统
        /// 3. 初始化XR交互系统
        /// </summary>
        protected override void OnInit()
        {
            // 查找或创建3D UI根节点
            var ui3DRoot = GameObject.Find("UI3DRoot");
            if (ui3DRoot == null)
            {
                ui3DRoot = new GameObject("UI3DRoot");
                UnityEngine.Object.DontDestroyOnLoad(ui3DRoot);
            }
            
            _instanceRoot = ui3DRoot.transform;
            _instanceRoot.gameObject.layer = LayerMask.NameToLayer("UI");

            // 查找XR相机
            var xrRig = GameObject.Find("XR Rig");
            if (xrRig != null)
            {
                _xrCamera = xrRig.GetComponentInChildren<Camera>();
            }
            else
            {
                // 如果没有找到XR Rig，使用主相机
                _xrCamera = Camera.main;
            }

            // 配置错误日志
            if (Debugger.Instance != null)
            {
                switch (Debugger.Instance.ActiveWindowType)
                {
                    case DebuggerActiveWindowType.AlwaysOpen:
                        _enableErrorLog = true;
                        break;

                    case DebuggerActiveWindowType.OnlyOpenWhenDevelopment:
                        _enableErrorLog = Debug.isDebugBuild;
                        break;

                    case DebuggerActiveWindowType.OnlyOpenInEditor:
                        _enableErrorLog = Application.isEditor;
                        break;

                    default:
                        _enableErrorLog = false;
                        break;
                }
                if (_enableErrorLog)
                {
                    _errorLogger = new ErrorLogger(this);
                }
            }
            
            // 初始化XR交互系统
            InitializeXRInteraction();
        }

        /// <summary>
        /// 初始化XR交互系统
        /// </summary>
        private void InitializeXRInteraction()
        {
            // 检查是否启用了XR
            if (XRSettings.isDeviceActive)
            {
                Log.Info("XR设备已激活: " + XRSettings.loadedDeviceName);
            }
            else
            {
                Log.Warning("未检测到XR设备，3D UI可能无法正确交互");
            }

            // 在这里可以初始化XR交互系统组件
            // 例如创建射线交互器等
            // 注：具体实现取决于项目使用的XR框架
        }

        /// <summary>
        /// 模块释放（自动调用）。
        /// 1. 清理错误日志系统
        /// 2. 关闭所有3D窗口
        /// 3. 销毁UI根节点
        /// </summary>
        protected override void OnRelease()
        {
            if (_errorLogger != null)
            {
                _errorLogger.Dispose();
                _errorLogger = null;
            }
            CloseAll();
            if (_instanceRoot != null)
            {
                UnityEngine.Object.Destroy(_instanceRoot.gameObject);
            }
        }

        /// <summary>
        /// 显示3D UI窗口
        /// </summary>
        /// <typeparam name="T">3D窗口类型</typeparam>
        /// <param name="position">世界空间位置</param>
        /// <param name="rotation">世界空间旋转</param>
        /// <param name="userDatas">用户自定义数据</param>
        /// <returns>窗口实例</returns>
        public T ShowUI3D<T>(Vector3 position, Quaternion rotation, params System.Object[] userDatas) where T : UI3DWindow, new()
        {
            T window = CreateInstance<T>();
            window.SetWorldPosition(position);
            window.SetWorldRotation(rotation);
            
            _ui3DWindows.Add(window);
            window.InternalLoad(window.AssetName, OnWindow3DPrepare, false, userDatas).Forget();
            
            return window;
        }

        /// <summary>
        /// 在场景固定位置显示3D UI窗口
        /// </summary>
        /// <param name="uiIdentifier">UI标识符</param>
        /// <param name="userDatas">用户自定义数据</param>
        public void ShowSceneUI(string uiIdentifier, params System.Object[] userDatas)
        {
            // 查找场景中的UI锚点
            var anchorPoint = GameObject.FindObjectsOfType<UI3DAnchorPoint>()
                .FirstOrDefault(a => a.uiIdentifier == uiIdentifier);
                
            if (anchorPoint == null)
            {
                Log.Error($"未找到ID为{uiIdentifier}的3D UI锚点");
                return;
            }
            
            // 获取UI类型
            Type uiType = GetUITypeFromIdentifier(uiIdentifier);
            if (uiType == null)
            {
                Log.Error($"未找到对应的UI类型: {uiIdentifier}");
                return;
            }
            
            // 使用反射创建并显示UI
            var methodInfo = typeof(UI3DModule).GetMethod("ShowUI3D").MakeGenericMethod(uiType);
            methodInfo.Invoke(this, new object[] { 
                anchorPoint.transform.position, 
                anchorPoint.transform.rotation,
                userDatas
            });
        }

        /// <summary>
        /// 根据UI标识符获取对应的UI类型
        /// </summary>
        private Type GetUITypeFromIdentifier(string uiIdentifier)
        {
            // 尝试直接通过命名约定查找
            string typeName = $"GameLogic.{uiIdentifier}UI";
            Type type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }
            
            // 查找带有SceneUIAttribute特性的类型
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    var attr = t.GetCustomAttribute<SceneUIAttribute>();
                    if (attr != null && attr.Identifier == uiIdentifier)
                    {
                        return t;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// 3D窗口准备完成回调
        /// </summary>
        private void OnWindow3DPrepare(UIWindow window)
        {
            var ui3DWindow = window as UI3DWindow;
            if (ui3DWindow == null) return;
            
            // 设置为世界空间Canvas
            var canvas = ui3DWindow.Canvas;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                
                // 配置Canvas大小和缩放
                var rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 根据VR可读性调整大小
                    // 这里的具体值需要根据项目需求调整
                    rectTransform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                }
            }
            
            // 添加XR交互组件
            SetupXRInteraction(ui3DWindow);
            
            // 调用窗口的OnCreate方法
            ui3DWindow.TryInvoke(null);
        }

        /// <summary>
        /// 为UI窗口设置XR交互组件
        /// </summary>
        private void SetupXRInteraction(UI3DWindow window)
        {
            // 添加碰撞体以支持物理射线交互
            var collider = window.gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = window.gameObject.AddComponent<BoxCollider>();
                // 根据UI大小设置碰撞体
                var rectTransform = window.gameObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    collider.size = new Vector3(
                        rectTransform.rect.width,
                        rectTransform.rect.height,
                        0.01f);
                }
            }
            
            // 注：这里可以添加更多XR特定的交互组件
            // 具体实现取决于项目使用的XR框架
        }

        /// <summary>
        /// 关闭3D UI窗口
        /// </summary>
        public void CloseUI3D<T>() where T : UI3DWindow
        {
            for (int i = _ui3DWindows.Count - 1; i >= 0; i--)
            {
                var window = _ui3DWindows[i];
                if (window is T)
                {
                    window.InternalDestroy();
                    _ui3DWindows.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 关闭场景中的指定UI
        /// </summary>
        public void CloseSceneUI(string uiIdentifier)
        {
            Type uiType = GetUITypeFromIdentifier(uiIdentifier);
            if (uiType == null) return;
            
            for (int i = _ui3DWindows.Count - 1; i >= 0; i--)
            {
                var window = _ui3DWindows[i];
                if (window.GetType() == uiType)
                {
                    window.InternalDestroy();
                    _ui3DWindows.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 关闭所有3D UI窗口
        /// </summary>
        public void CloseAll()
        {
            for (int i = 0; i < _ui3DWindows.Count; i++)
            {
                var window = _ui3DWindows[i];
                window.InternalDestroy();
            }
            _ui3DWindows.Clear();
        }

        /// <summary>
        /// 创建3D UI窗口实例
        /// </summary>
        private T CreateInstance<T>() where T : UI3DWindow, new()
        {
            return new T();
        }

        /// <summary>
        /// 更新函数，每帧调用
        /// </summary>
        public void OnUpdate()
        {
            // 更新所有3D窗口
            for (int i = 0; i < _ui3DWindows.Count; i++)
            {
                var window = _ui3DWindows[i];
                if (window != null && window.IsLoadDone)
                {
                    window.InternalUpdate();
                }
            }
        }
    }
}
