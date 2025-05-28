using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR || ENABLE_XR
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace GameLogic
{
    /// <summary>
    /// 3D UI窗口类。
    /// </summary>
    public abstract class UI3DWindow : UI3DBase
    {
        #region 属性

        // 窗口名称
        public string WindowName { get; private set; }
        
        // 资源定位地址
        public string AssetName { get; private set; }
        
        // 窗口深度值
        private int _depth = 0;
        
        /// <summary>
        /// 窗口深度值。
        /// </summary>
        public int Depth
        {
            get => _depth;
            set
            {
                if (_depth != value)
                {
                    _depth = value;
                    OnSortDepth(value);
                }
            }
        }
        
        /// <summary>
        /// 窗口可见性。
        /// </summary>
        public bool Visible
        {
            get => gameObject != null && gameObject.activeSelf;
            set
            {
                if (gameObject != null && gameObject.activeSelf != value)
                {
                    gameObject.SetActive(value);
                    OnSetVisible(value);
                }
            }
        }
        
        /// <summary>
        /// 窗口是否创建完成
        /// </summary>
        private bool _isCreated = false;
        
        /// <summary>
        /// 是否已经创建
        /// </summary>
        public bool IsCreated => _isCreated;
        
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading { get; private set; } = false;
        
#if UNITY_EDITOR || ENABLE_XR
        // 交互组件
        private XRGrabInteractable _grabInteractable;
#endif
        
        // 窗口状态
        private bool _isGrabbable = false; // 默认不可抽取
        private bool _isFollowingUser = false;
        private Vector3 _relativePosition;
        private Quaternion _relativeRotation;
        private UI3DPositionMode _positionMode = UI3DPositionMode.WorldFixed;
        private Transform _referenceTransform;
        
        // UI类型
        public override UI3DType Type => UI3DType.Window;
        
        /// <summary>
        /// 窗口的当前定位模式
        /// </summary>
        public UI3DPositionMode PositionMode => _positionMode;
        
        /// <summary>
        /// 窗口参考变换组件（用于锚点模式）
        /// </summary>
        public Transform ReferenceTransform => _referenceTransform;
        
        #endregion

        /// <summary>
        /// 准备窗口资源
        /// </summary>
        public async UniTask PrepareAsync(string assetName, Transform parent, object[] userDatas)
        {
            AssetName = assetName;
            WindowName = GetType().Name;
            _userDatas = userDatas;
            IsLoading = true;
            
            try
            {
                // 使用UI3D特定的资源加载器
                GameObject prefab = await UI3DModule.Resource.LoadGameObjectAsync(assetName, parent);
                if (prefab == null)
                {
                    Log.Error($"Load 3D UI prefab failed: {assetName}");
                    IsLoading = false;
                    return;
                }
                
                // 实例化
                gameObject = prefab;
                transform = gameObject.transform;
                
                // 添加交互组件
                SetupInteraction();
                
                // 修复TMP输入字段，确保它们在VR中正常工作
                SetupTextMeshProInputFields();
                
                // 调用内部创建方法
                InternalCreate();
                
                IsPrepare = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error preparing UI3D window {WindowName}: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 内部创建方法，用于初始化窗口
        /// </summary>
        internal void InternalCreate()
        {
            if (!_isCreated)
            {
                _isCreated = true;
                
                // 按照UI系统的生命周期调用
                ScriptGenerator();
                BindMemberProperty();
                RegisterEvent();
                
                // 调用原有创建方法保持兼容性
                OnCreate(transform.parent, _userDatas);
            }
        }
        
        /// <summary>
        /// 内部刷新方法
        /// </summary>
        internal void InternalRefresh()
        {
            OnRefresh();
        }
        
        /// <summary>
        /// 内部更新方法
        /// </summary>
        internal bool InternalUpdate()
        {
            if (!IsPrepare || !Visible)
            {
                return false;
            }
            
            OnUpdate();
            return true;
        }
        

        
        /// <summary>
        /// 设置TextMeshPro输入字段
        /// </summary>
        private void SetupTextMeshProInputFields()
        {
#if UNITY_EDITOR || ENABLE_XR
            try
            {
                // 查找所有TMPro.TMP_InputField组件
                var tmpInputFields = gameObject.GetComponentsInChildren<TMPro.TMP_InputField>(true);
                if (tmpInputFields != null && tmpInputFields.Length > 0)
                {
                    foreach (var inputField in tmpInputFields)
                    {
                        // 防止多行输入问题
                        inputField.lineType = TMPro.TMP_InputField.LineType.SingleLine;
                        
                        // 添加软键盘关闭处理
                        inputField.onEndEdit.AddListener(CloseKeyboardIfNeeded);
                        
                        Log.Info($"Setup TMP_InputField in {WindowName}: {inputField.name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Error setting up TMP input fields: {ex.Message}");
            }
#endif
        }
        
        /// <summary>
        /// 处理软键盘关闭问陒
        /// </summary>
        private void CloseKeyboardIfNeeded(string text)
        {
#if UNITY_ANDROID && (UNITY_EDITOR || ENABLE_XR)
            // Quest平台特定处理
            try
            {
                // 尝试强制隐藏软键盘
                TouchScreenKeyboard.hideInput = true;
            }
            catch (Exception ex)
            {
                Log.Warning($"Error closing keyboard: {ex.Message}");
            }
#endif
        }
        
        /// <summary>
        /// 设置交互组件
        /// </summary>
        private void SetupInteraction()
        {
#if UNITY_EDITOR || ENABLE_XR
            // 确保UI组件使用正确的交互组件
            // 先删除已存在的交互组件，避免冲突
            var existingInteractable = gameObject.GetComponent<XRBaseInteractable>();
            if (existingInteractable != null && !(existingInteractable is XRGrabInteractable))
            {
                GameObject.DestroyImmediate(existingInteractable);
            }
            
            // 对于可抓取的窗口，使用XRGrabInteractable
            if (_isGrabbable)
            {
                if (_grabInteractable == null)
                {
                    _grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
                }
                
                _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
                _grabInteractable.throwOnDetach = false;
                _grabInteractable.trackPosition = true;
                _grabInteractable.trackRotation = true;
                _grabInteractable.smoothPosition = false; // 实时跟踪，不平滑
                
                // 添加事件监听
                _grabInteractable.selectEntered.AddListener(OnGrab);
                _grabInteractable.selectExited.AddListener(OnRelease);
                
                // 确保 Rigidbody 不受重力影响
                var rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true;  // 防止物理引擎影响位置
                    rigidbody.useGravity = false;  // 禁用重力
                    rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                }
                
                // 配置抓取交互区域
                ConfigureGrabInteractionArea();
            }
            else if (_grabInteractable != null)
            {
                // 如果不可抓取，删除组件
                GameObject.DestroyImmediate(_grabInteractable);
                _grabInteractable = null;
            }
            
            // 配置Canvas以支持UI交互
            EnsureProperCanvasSetup();
#endif
        }
        
#if UNITY_EDITOR || ENABLE_XR
        /// <summary>
        /// 抓取事件
        /// </summary>
        private void OnGrab(SelectEnterEventArgs args)
        {
            _isFollowingUser = false;
            _positionMode = UI3DPositionMode.WorldFixed;
            
            // 在抓取时将窗口移到前层
            BringWindowToFront();
        }
        
        /// <summary>
        /// 释放事件
        /// </summary>
        private void OnRelease(SelectExitEventArgs args)
        {
            // 释放后尝试吸附到最近的锚点
            TrySnapToNearestAnchor();
        }
        
        /// <summary>
        /// 将窗口移到前层
        /// </summary>
        private void BringWindowToFront()
        {
            // 请求UI3DModule将此窗口移到最前层
            // 实际实现可以根据项目需求添加
        }
        
        /// <summary>
        /// 配置抓取交互区域
        /// </summary>
        private void ConfigureGrabInteractionArea()
        {
#if UNITY_EDITOR || ENABLE_XR
            if (_grabInteractable == null) return;
            
            // 查找窗口标题栏或边框
            Transform titleBar = transform.Find("TitleBar");
            if (titleBar == null) titleBar = transform.Find("Panel/TitleBar");
            
            if (titleBar != null)
            {
                // 创建一个碰撩体用于抓取
                var collider = titleBar.gameObject.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = titleBar.gameObject.AddComponent<BoxCollider>();
                    // 调整碰撩体大小以适应标题栏
                    var rectTrans = titleBar.GetComponent<RectTransform>();
                    if (rectTrans != null)
                    {
                        // 根据RectTransform设置适合的大小
                        collider.size = new Vector3(rectTrans.rect.width, rectTrans.rect.height, 0.01f);
                        collider.center = Vector3.zero;
                    }
                    else
                    {
                        // 默认大小
                        collider.size = new Vector3(1, 0.1f, 0.01f);
                        collider.center = Vector3.zero;
                    }
                }
            }
            else
            {
                // 如果没有标题栏，使用整个窗口作为抓取区域
                var collider = gameObject.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = gameObject.AddComponent<BoxCollider>();
                    // 设置一个薄的碰撩体
                    collider.size = new Vector3(1, 1, 0.01f);
                    collider.center = Vector3.zero;
                }
            }
#endif
        }
#endif
        
        /// <summary>
        /// 确保Canvas正确配置以支持UI交互
        /// </summary>
        private void EnsureProperCanvasSetup()
        {
#if UNITY_EDITOR || ENABLE_XR
            // 查找或添加Canvas组件
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                Log.Info($"Added Canvas component to {WindowName}");
            }
            
            // 确保Canvas设置为World Space
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                Log.Info($"Set Canvas render mode to WorldSpace for {WindowName}");
            }
            
            // 添加CanvasScaler组件以确保正确的尺寸
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f; // 同时考虑宽度和高度
                Log.Info($"Added CanvasScaler component to {WindowName}");
            }
            
            // 添加TrackedDeviceGraphicRaycaster组件以支持XR交互
            var graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
            var trackedDeviceRaycaster = gameObject.GetComponent<TrackedDeviceGraphicRaycaster>();
            
            // 如果有普通的GraphicRaycaster但没有TrackedDeviceGraphicRaycaster，则替换
            if (graphicRaycaster != null && !(graphicRaycaster is TrackedDeviceGraphicRaycaster))
            {
                GameObject.DestroyImmediate(graphicRaycaster);
                graphicRaycaster = null;
            }
            
            // 添加TrackedDeviceGraphicRaycaster如果需要
            if (trackedDeviceRaycaster == null)
            {
                trackedDeviceRaycaster = gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                // 配置遗挡检查
                trackedDeviceRaycaster.checkFor3DOcclusion = true; // 允许3D对象遗挡UI
                Log.Info($"Added TrackedDeviceGraphicRaycaster to {WindowName}");
            }
            
            // 注册到Canvas优化器
            if (UI3DModule.Instance != null)
            {
                UI3DModule.Instance.RegisterCanvasToOptimizer(canvas);
            }
#endif
        }
        
        /// <summary>
        /// 尝试吸附到最近的锚点
        /// </summary>
        private void TrySnapToNearestAnchor()
        {
            if (UI3DModule.Instance != null)
            {
                UI3DAnchorPoint nearestAnchor = UI3DModule.Instance.FindNearestAnchor(transform.position, 1.0f);
                if (nearestAnchor != null)
                {
                    SetPositionMode(UI3DPositionMode.AnchorBased, nearestAnchor.transform);
                }
            }
        }
        
        /// <summary>
        /// 设置是否可抓取
        /// </summary>
        public void SetGrabbable(bool canGrab)
        {
            _isGrabbable = canGrab;
            SetupInteraction();
            
            // 如果设置为不可抓取，尝试移除Rigidbody
            if (!canGrab)
            {
                var rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    GameObject.DestroyImmediate(rigidbody);
                }
            }
        }
        
        /// <summary>
        /// 设置位置模式
        /// </summary>
        public void SetPositionMode(UI3DPositionMode mode, Transform reference = null, Vector3 offset = default)
        {
            _positionMode = mode;
            _referenceTransform = reference;
            
            switch (mode)
            {
                case UI3DPositionMode.WorldFixed:
                    // 保持当前世界位置不变
                    _isFollowingUser = false;
                    transform.SetParent(UI3DModule.Instance.transform);
                    break;
                    
                case UI3DPositionMode.UserRelative:
                    // 设置相对于用户的位置
                    _isFollowingUser = true;
                    _relativePosition = offset;
                    _relativeRotation = Quaternion.identity;
                    transform.SetParent(UI3DModule.Instance.transform);
                    break;
                    
                case UI3DPositionMode.AnchorBased:
                    // 设置到指定锚点
                    _isFollowingUser = false;
                    if (reference != null)
                    {
                        // 检查是否已经是锚点的子对象，避免重复操作
                        if (transform.parent != reference)
                        {
                            transform.position = reference.position + reference.TransformDirection(offset);
                            transform.rotation = reference.rotation;
                            transform.SetParent(reference);
                            
                            // 确保只有一个实例
                            Log.Info($"UI3D window {WindowName} attached to anchor {reference.name}");
                        }
                    }
                    break;
                    
                case UI3DPositionMode.HandAttached:
                    // 附着到手上
                    _isFollowingUser = false;
                    if (reference != null) // reference为手部控制器Transform
                    {
                        transform.SetParent(reference);
                        transform.localPosition = offset;
                        transform.localRotation = Quaternion.identity;
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 设置相对于用户的位置（跟随模式）
        /// </summary>
        public override void SetRelativeToUser(Vector3 relativePosition, Quaternion relativeRotation)
        {
            SetPositionMode(UI3DPositionMode.UserRelative, null, relativePosition);
            _relativeRotation = relativeRotation;
        }
        
        /// <summary>
        /// 设置世界空间位置
        /// </summary>
        public override void SetWorldPosition(Vector3 position, Quaternion rotation)
        {
            SetPositionMode(UI3DPositionMode.WorldFixed);
            transform.position = position;
            transform.rotation = rotation;
        }
        
        /// <summary>
        /// 停止跟随
        /// </summary>
        public void StopFollowing()
        {
            if (_isFollowingUser)
            {
                _isFollowingUser = false;
                _positionMode = UI3DPositionMode.WorldFixed;
            }
        }
        
        /// <summary>
        /// 更新位置（如果在跟随模式）
        /// </summary>
        public override void OnUpdate()
        {
            if (_isFollowingUser && UI3DModule.Instance.XRRig != null)
            {
                Transform userTransform = UI3DModule.Instance.XRRig;
                transform.position = userTransform.TransformPoint(_relativePosition);
                transform.rotation = userTransform.rotation * _relativeRotation;
            }
        }
        
        /// <summary>
        /// 当触发窗口的层级排序。
        /// </summary>
        protected override void OnSortDepth(int depth)
        {
            // 3D UI窗口的深度排序实现
            // 可以通过调整Canvas组件的sortingOrder来实现
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = depth;
                
                // 遍历子Canvas设置深度
                Canvas[] childCanvases = gameObject.GetComponentsInChildren<Canvas>(true);
                if (childCanvases != null && childCanvases.Length > 0)
                {
                    int childDepth = depth;
                    foreach (var childCanvas in childCanvases)
                    {
                        if (childCanvas != canvas)
                        {
                            childDepth += 5; // 递增值
                            childCanvas.sortingOrder = childDepth;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 当设置窗口的显隐状态
        /// </summary>
        protected override void OnSetVisible(bool visible)
        {
            // 处理显隐状态变化
            if (visible)
            {
                OnShow();
            }
            else
            {
                OnHide();
            }
        }
        
        /// <summary>
        /// 内部销毁方法
        /// </summary>
        internal void InternalDestroy()
        {
            // 从 CanvasOptimizer 取消注册 Canvas
#if UNITY_EDITOR || ENABLE_XR
            try
            {
                // 先获取 Canvas
                var canvas = gameObject?.GetComponent<Canvas>();
                if (canvas != null && UI3DModule.Instance != null)
                {
                    // 从优化器取消注册
                    var optimizer = GameObject.FindObjectOfType<CanvasOptimizer>();
                    if (optimizer != null)
                    {
                        var unregisterMethod = optimizer.GetType().GetMethod("UnregisterCanvas");
                        if (unregisterMethod != null)
                        {
                            unregisterMethod.Invoke(optimizer, new object[] { canvas });
                            Log.Info($"Unregistered canvas {WindowName} from optimizer");
                        }
                    }
                }
                
                // 清理交互组件
                if (_grabInteractable != null)
                {
                    _grabInteractable.selectEntered.RemoveListener(OnGrab);
                    _grabInteractable.selectExited.RemoveListener(OnRelease);
                    _grabInteractable = null;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Error unregistering canvas from optimizer: {ex.Message}");
            }
#endif
            
            // 先隐藏再调用销毁方法
            OnHide();
            OnDestroy();
            
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
                gameObject = null;
                transform = null;
            }
            
            _isCreated = false;
            IsPrepare = false;
            
            Log.Info($"UI3D window {WindowName} destroyed");
        }
        
        /// <summary>
        /// 销毁UI
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
                gameObject = null;
                transform = null;
            }
        }
    }
}
