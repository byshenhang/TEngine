using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// 场景中3D UI锚点组件，用于标记场景中固定位置的3D UI位置。
    /// 集成Unity XR Interaction Toolkit，支持基于视线、距离和控制器指向的交互。
    /// </summary>
    public class UI3DAnchorPoint : MonoBehaviour
    {
        [Header("Basic Settings")]
        [Tooltip("UI的标识符（用于匹配UI类型）")]
        public string uiIdentifier;
        
        [Tooltip("是否在场景加载时自动显示此UI")]
        public bool showOnLoad = false;
        
        [Header("Interaction Settings")]
        [Tooltip("是否需要交互触发")]
        public bool requiresInteraction = false;
        
        [Tooltip("交互距离")]
        public float interactionDistance = 2.0f;
        
        [Tooltip("当玩家靠近时是否自动显示")]
        public bool showOnPlayerProximity = false;
        
        [Tooltip("当UI显示时是否隐藏锚点模型")]
        public bool hideModelWhenUIVisible = true;
        
        [Header("Gaze & View Settings")]
        [Tooltip("是否在用户视线指向时显示UI")]
        public bool showOnGaze = false;
        
        [Tooltip("是否需要持续注视才保持UI显示")]
        public bool requireContinuousGaze = false;
        
        [Tooltip("是否忽略视角限制（即使不在视野中也可能显示）")]
        public bool ignoreViewAngle = false;
        
        [Header("Advanced Behavior")]
        [Tooltip("当超出范围或视角时是否自动隐藏")]
        public bool hideWhenOutOfRange = true;
        
        [Tooltip("悬停退出时是否保持UI可见")]
        public bool keepVisibleAfterHoverExit = false;
        
        [Tooltip("选择时是否切换UI显示状态")]
        public bool toggleOnSelect = true;
        
        [Tooltip("是否已固定（固定的UI不会自动隐藏）")]
        public bool isPinned = false;
        
        /// <summary>
        /// 当前是否有UI显示
        /// </summary>
        [HideInInspector]
        public bool hasUIVisible = false;
        
        // XR交互组件
        private XRSimpleInteractable _interactable;
        
        private void Awake()
        {
            // 检查或添加XR交互组件
            SetupXRInteraction();
        }
        
        private void Start()
        {
            // 检查XRUIController是否存在，如果不存在则创建
            if (XRUIController.Instance == null)
            {
                GameObject controllerObj = new GameObject("XR UI Controller");
                controllerObj.AddComponent<XRUIController>();
                Log.Info("Created XRUIController instance");
            }
            
            // 如果配置为自动显示，则显示UI
            if (showOnLoad)
            {
                ShowUI();
            }
        }
        
        /// <summary>
        /// 设置XR交互组件
        /// </summary>
        private void SetupXRInteraction()
        {
            // 添加XR简单交互组件
            _interactable = GetComponent<XRSimpleInteractable>();
            if (_interactable == null)
            {
                _interactable = gameObject.AddComponent<XRSimpleInteractable>();
            }
            
            // 确保有碰撞体供交互使用
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                // 添加一个适合的碰撞体
                // 如果有MeshRenderer，根据网格大小设置碰撞体
                MeshRenderer renderer = GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                    boxCollider.size = renderer.bounds.size;
                    boxCollider.center = renderer.bounds.center - transform.position;
                    boxCollider.isTrigger = true;  // 设置为触发器以避免物理碰撞
                }
                else
                {
                    // 如果没有网格渲染器，添加一个默认大小的碰撞体
                    SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                    sphereCollider.radius = 0.1f;
                    sphereCollider.isTrigger = true;
                }
            }
        }
        
        /// <summary>
        /// 获取玩家Transform
        /// </summary>
        private Transform GetPlayerTransform()
        {
            // 优先使用XRUIController中的相机
            if (XRUIController.Instance != null && XRUIController.Instance.XROrigin != null)
            {
                return XRUIController.Instance.XROrigin;
            }
            
            // 然后尝试获取UI3DModule中的相机
            if (UI3DModule.Instance != null && UI3DModule.Instance.XRCamera != null)
            {
                return UI3DModule.Instance.XRCamera.transform;
            }
            
            // 如果没有XR相机，则使用主相机
            return Camera.main?.transform;
        }
        
        /// <summary>
        /// 显示UI
        /// </summary>
        public void ShowUI()
        {
            // 如果已经显示则不重复操作
            if (hasUIVisible) return;
            
            if (UI3DModule.Instance != null)
            {
                UI3DModule.Instance.ShowSceneUI(uiIdentifier);
                hasUIVisible = true;
                
                // 如果需要隐藏锚点模型
                if (hideModelWhenUIVisible)
                {
                    // 隐藏除UI3DAnchorPoint组件外的所有渲染器
                    foreach (var renderer in GetComponentsInChildren<Renderer>())
                    {
                        renderer.enabled = false;
                    }
                }
                
                // 分发UI显示事件
                OnUIShown();
            }
        }
        
        /// <summary>
        /// 隐藏UI
        /// </summary>
        public void HideUI()
        {
            // 如果已经隐藏则不重复操作
            if (!hasUIVisible) return;
            
            // 如果UI被固定，且不是手动调用隐藏，则不隐藏
            if (isPinned) return;
            
            if (UI3DModule.Instance != null)
            {
                UI3DModule.Instance.CloseSceneUI(uiIdentifier);
                hasUIVisible = false;
                
                // 如果之前隐藏了锚点模型
                if (hideModelWhenUIVisible)
                {
                    // 显示所有渲染器
                    foreach (var renderer in GetComponentsInChildren<Renderer>())
                    {
                        renderer.enabled = true;
                    }
                }
                
                // 分发UI隐藏事件
                OnUIHidden();
            }
        }
        
        /// <summary>
        /// 切换UI显示状态
        /// </summary>
        public void ToggleUI()
        {
            if (hasUIVisible)
            {
                // 强制隐藏，即使是固定的UI
                isPinned = false;
                HideUI();
            }
            else
            {
                ShowUI();
            }
        }
        
        /// <summary>
        /// 切换UI固定状态
        /// </summary>
        public void TogglePinned()
        {
            isPinned = !isPinned;
            
            // 如果固定且UI当前未显示，则显示UI
            if (isPinned && !hasUIVisible)
            {
                ShowUI();
            }
        }
        
        /// <summary>
        /// 当UI显示时的回调
        /// </summary>
        protected virtual void OnUIShown()
        {
            // 子类可以重写此方法实现自定义逻辑
        }
        
        /// <summary>
        /// 当UI隐藏时的回调
        /// </summary>
        protected virtual void OnUIHidden()
        {
            // 子类可以重写此方法实现自定义逻辑
        }
        
        /// <summary>
        /// u7528u4e8eu8c03u8bd5u53efu89c6u5316
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.2f, 0.2f, 0.2f));
            Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
            
            if (showOnPlayerProximity)
            {
                Gizmos.color = new Color(0, 1, 1, 0.2f);
                Gizmos.DrawSphere(transform.position, interactionDistance);
            }
        }
    }
}
