using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// XR UIu63a7u5236u5668uff0cu7ba1u7406u57fau4e8eu89c6u7ebf/u63a7u5236u5668u6307u5411u548cu8dddu79bbu7684UIu663eu793au548cu9690u85cfu3002
    /// u96c6u6210Unity XR Interaction Toolkitu6846u67b6u3002
    /// </summary>
    public class XRUIController : MonoSingleton<XRUIController>
    {
        [Header("XR Interaction Settings")]
        [Tooltip("The XR Ray Interactor to use for UI interaction detection")]
        [SerializeField] private XRRayInteractor _leftHandRayInteractor;
        [SerializeField] private XRRayInteractor _rightHandRayInteractor;
        [Tooltip("The XR Controller components for both hands")]
        [SerializeField] private XRController _leftHandController;
        [SerializeField] private XRController _rightHandController;
        
        [Header("UI Detection Settings")]
        [Tooltip("Maximum distance for UI detection")]
        [SerializeField] private float _maxUIDetectionDistance = 5f;
        [Tooltip("Maximum angle (degrees) from forward direction for UI detection")]
        [SerializeField] private float _maxUIDetectionAngle = 30f;
        [Tooltip("How often to update UI visibility (seconds)")]
        [SerializeField] private float _updateInterval = 0.1f;
        
        // u8ddfu8e2au5f53u524du663eu793au7684u6240u6709UIu9501u70b9
        private readonly List<UI3DAnchorPoint> _activeAnchors = new List<UI3DAnchorPoint>();
        // u7f13u5b58u573au666fu4e2du7684u6240u6709UIu9501u70b9
        private readonly List<UI3DAnchorPoint> _allAnchors = new List<UI3DAnchorPoint>();
        
        // u66f4u65b0u8ba1u65f6u5668
        private float _updateTimer = 0f;
        
        // XR原点（头部/相机）
        private Transform _xrOrigin;
        
        /// <summary>
        /// 获取XR原点参考（通常是相机或头部位置）
        /// </summary>
        public Transform XROrigin => _xrOrigin;
        // XRu4ea4u4e92u7ba1u7406u5668
        private XRInteractionManager _interactionManager;
        
        protected override void OnAwake()
        {
            base.OnAwake();
            
            // u67e5u627eu5fc5u8981u7684XRu7ec4u4ef6
            FindXRComponents();
            
            // u83b7u53d6u573au666fu4e2du7684u6240u6709UIu9501u70b9
            RefreshUIAnchors();
            
            // u6ce8u518cu4ea4u4e92u4e8bu4ef6
            RegisterInteractionEvents();
        }
        
        private void OnEnable()
        {
            // u6bcfu6b21u542fu7528u65f6u5237u65b0UIu9501u70b9
            RefreshUIAnchors();
        }
        
        private void OnDisable()
        {
            // u9690u85cfu6240u6709u6d3bu52a8u7684UI
            HideAllActiveUIs();
        }
        
        private void OnDestroy()
        {
            // u53d6u6d88u6ce8u518cu4ea4u4e92u4e8bu4ef6
            UnregisterInteractionEvents();
        }
        
        private void Update()
        {
            // u6309u7167u8bbeu5b9au7684u95f4u9694u66f4u65b0UIu53efu89c1u6027
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= _updateInterval)
            {
                UpdateUIVisibility();
                _updateTimer = 0f;
            }
        }
        
        /// <summary>
        /// u67e5u627eu5fc5u8981u7684XRu7ec4u4ef6
        /// </summary>
        private void FindXRComponents()
        {
            // u67e5u627eXRu4ea4u4e92u7ba1u7406u5668
            _interactionManager = FindObjectOfType<XRInteractionManager>();
            if (_interactionManager == null)
            {
                Log.Warning("XRInteractionManager not found in the scene. Creating one.");
                GameObject managerObj = new GameObject("XR Interaction Manager");
                _interactionManager = managerObj.AddComponent<XRInteractionManager>();
            }
            
            // u67e5u627eXRu539fu70b9
            var xrRig = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrRig != null)
            {                
                _xrOrigin = xrRig.Camera.transform;
            }
            else
            {
                _xrOrigin = Camera.main.transform;
                Log.Warning("XROrigin not found. Using main camera as XR origin.");
            }
            
            // u5982u679cu6ca1u6709u8bbeu7f6eRay Interactorsuff0cu5c1du8bd5u67e5u627e
            if (_leftHandRayInteractor == null || _rightHandRayInteractor == null)
            {
                var rayInteractors = FindObjectsOfType<XRRayInteractor>();
                if (rayInteractors.Length > 0)
                {
                    if (_leftHandRayInteractor == null && rayInteractors.Length > 0)
                    {
                        foreach (var interactor in rayInteractors)
                        {
                            if (interactor.name.ToLower().Contains("left"))
                            {
                                _leftHandRayInteractor = interactor;
                                break;
                            }
                        }
                    }
                    
                    if (_rightHandRayInteractor == null && rayInteractors.Length > 0)
                    {
                        foreach (var interactor in rayInteractors)
                        {
                            if (interactor.name.ToLower().Contains("right"))
                            {
                                _rightHandRayInteractor = interactor;
                                break;
                            }
                        }
                    }
                }
            }
            
            // u5982u679cu6ca1u6709u8bbeu7f6eXR Controllersuff0cu5c1du8bd5u67e5u627e
            if (_leftHandController == null || _rightHandController == null)
            {
                var controllers = FindObjectsOfType<XRController>();
                if (controllers.Length > 0)
                {
                    if (_leftHandController == null)
                    {
                        foreach (var controller in controllers)
                        {
                            if (controller.name.ToLower().Contains("left"))
                            {
                                _leftHandController = controller;
                                break;
                            }
                        }
                    }
                    
                    if (_rightHandController == null)
                    {
                        foreach (var controller in controllers)
                        {
                            if (controller.name.ToLower().Contains("right"))
                            {
                                _rightHandController = controller;
                                break;
                            }
                        }
                    }
                }
            }
            
            // u8f93u51fau53d1u73b0u7684u7ec4u4ef6u60c5u51b5
            Log.Info($"XR Components found: InteractionManager={_interactionManager != null}, " +
                    $"XROrigin={_xrOrigin != null}, LeftRay={_leftHandRayInteractor != null}, " +
                    $"RightRay={_rightHandRayInteractor != null}");
        }
        
        /// <summary>
        /// u5237u65b0u573au666fu4e2du7684u6240u6709UIu9501u70b9
        /// </summary>
        public void RefreshUIAnchors()
        {
            _allAnchors.Clear();
            var anchors = FindObjectsOfType<UI3DAnchorPoint>();
            _allAnchors.AddRange(anchors);
            Log.Info($"Found {_allAnchors.Count} UI anchor points in scene.");
        }
        
        /// <summary>
        /// u6ce8u518cXRu4ea4u4e92u4e8bu4ef6
        /// </summary>
        private void RegisterInteractionEvents()
        {
            if (_leftHandRayInteractor != null)
            {
                _leftHandRayInteractor.selectEntered.AddListener(OnRaySelectEntered);
                _leftHandRayInteractor.selectExited.AddListener(OnRaySelectExited);
                _leftHandRayInteractor.hoverEntered.AddListener(OnRayHoverEntered);
                _leftHandRayInteractor.hoverExited.AddListener(OnRayHoverExited);
            }
            
            if (_rightHandRayInteractor != null)
            {
                _rightHandRayInteractor.selectEntered.AddListener(OnRaySelectEntered);
                _rightHandRayInteractor.selectExited.AddListener(OnRaySelectExited);
                _rightHandRayInteractor.hoverEntered.AddListener(OnRayHoverEntered);
                _rightHandRayInteractor.hoverExited.AddListener(OnRayHoverExited);
            }
        }
        
        /// <summary>
        /// u53d6u6d88u6ce8u518cXRu4ea4u4e92u4e8bu4ef6
        /// </summary>
        private void UnregisterInteractionEvents()
        {
            if (_leftHandRayInteractor != null)
            {
                _leftHandRayInteractor.selectEntered.RemoveListener(OnRaySelectEntered);
                _leftHandRayInteractor.selectExited.RemoveListener(OnRaySelectExited);
                _leftHandRayInteractor.hoverEntered.RemoveListener(OnRayHoverEntered);
                _leftHandRayInteractor.hoverExited.RemoveListener(OnRayHoverExited);
            }
            
            if (_rightHandRayInteractor != null)
            {
                _rightHandRayInteractor.selectEntered.RemoveListener(OnRaySelectEntered);
                _rightHandRayInteractor.selectExited.RemoveListener(OnRaySelectExited);
                _rightHandRayInteractor.hoverEntered.RemoveListener(OnRayHoverEntered);
                _rightHandRayInteractor.hoverExited.RemoveListener(OnRayHoverExited);
            }
        }
        
        /// <summary>
        /// u5f53u5c04u7ebfu60acu505cu5728u5bf9u8c61u4e0au65f6u89e6u53d1
        /// </summary>
        private void OnRayHoverEntered(HoverEnterEventArgs args)
        {
            // u68c0u67e5u662fu5426u4e3aUIu9501u70b9
            if (args.interactableObject is XRSimpleInteractable interactable)
            {
                UI3DAnchorPoint anchorPoint = interactable.GetComponent<UI3DAnchorPoint>();
                if (anchorPoint != null && !anchorPoint.hasUIVisible)
                {
                    ShowUI(anchorPoint);
                }
            }
        }
        
        /// <summary>
        /// u5f53u5c04u7ebfu79fbu51fau5bf9u8c61u65f6u89e6u53d1
        /// </summary>
        private void OnRayHoverExited(HoverExitEventArgs args)
        {
            // u68c0u67e5u662fu5426u4e3aUIu9501u70b9
            if (args.interactableObject is XRSimpleInteractable interactable)
            {
                UI3DAnchorPoint anchorPoint = interactable.GetComponent<UI3DAnchorPoint>();
                if (anchorPoint != null && anchorPoint.hasUIVisible && !anchorPoint.keepVisibleAfterHoverExit)
                {
                    HideUI(anchorPoint);
                }
            }
        }
        
        /// <summary>
        /// u5f53u9009u62e9u5bf9u8c61u65f6u89e6u53d1
        /// </summary>
        private void OnRaySelectEntered(SelectEnterEventArgs args)
        {
            // u68c0u67e5u662fu5426u4e3aUIu9501u70b9
            if (args.interactableObject is XRSimpleInteractable interactable)
            {
                UI3DAnchorPoint anchorPoint = interactable.GetComponent<UI3DAnchorPoint>();
                if (anchorPoint != null)
                {
                    if (!anchorPoint.hasUIVisible)
                    {
                        ShowUI(anchorPoint);
                    }
                    else if (anchorPoint.toggleOnSelect)
                    {
                        HideUI(anchorPoint);
                    }
                }
            }
        }
        
        /// <summary>
        /// u5f53u53d6u6d88u9009u62e9u5bf9u8c61u65f6u89e6u53d1
        /// </summary>
        private void OnRaySelectExited(SelectExitEventArgs args)
        {
            // u5728u8fd9u91ccu53efu4ee5u6dfbu52a0u9009u62e9u9000u51fau903bu8f91uff0cu5982u679cu9700u8981u7684u8bdd
        }
        
        /// <summary>
        /// u66f4u65b0UIu53efu89c1u6027u57fau4e8eu8dddu79bbu548cu89c6u7ebfu65b9u5411
        /// </summary>
        private void UpdateUIVisibility()
        {
            if (_xrOrigin == null) return;
            
            // u5148u5c06u6240u6709u4e0du5728u6d3bu52a8u5217u8868u4e2du7684u9501u70b9u68c0u67e5u662fu5426u5e94u8be5u663eu793a
            foreach (var anchor in _allAnchors)
            {
                if (!_activeAnchors.Contains(anchor) && !anchor.hasUIVisible)
                {
                    // u68c0u67e5u662fu5426u5728u8dddu79bbu8303u56f4u5185
                    float distance = Vector3.Distance(_xrOrigin.position, anchor.transform.position);
                    if (distance <= anchor.interactionDistance && distance <= _maxUIDetectionDistance)
                    {
                        // u68c0u67e5u662fu5426u5728u89c6u89d2u8303u56f4u5185
                        Vector3 directionToAnchor = (anchor.transform.position - _xrOrigin.position).normalized;
                        float angle = Vector3.Angle(_xrOrigin.forward, directionToAnchor);
                        
                        if (angle <= _maxUIDetectionAngle || anchor.ignoreViewAngle)
                        {
                            // u5982u679cu53eau6839u636eu8dddu79bbu81eau52a8u663eu793auff0cu6216u8005u8fd8u9700u8981u68c0u67e5u6307u5411
                            if (anchor.showOnPlayerProximity || (anchor.showOnGaze && IsGazingAt(anchor)))
                            {
                                ShowUI(anchor);
                            }
                        }
                    }
                }
            }
            
            // u7136u540eu68c0u67e5u6240u6709u6d3bu52a8u7684UIu662fu5426u5e94u8be5u9690u85cf
            for (int i = _activeAnchors.Count - 1; i >= 0; i--)
            {
                UI3DAnchorPoint anchor = _activeAnchors[i];
                
                // u68c0u67e5u662fu5426u8d85u51fau8dddu79bbu8303u56f4
                float distance = Vector3.Distance(_xrOrigin.position, anchor.transform.position);
                bool outOfRange = distance > anchor.interactionDistance || distance > _maxUIDetectionDistance;
                
                // u68c0u67e5u662fu5426u8d85u51fau89c6u89d2u8303u56f4
                Vector3 directionToAnchor = (anchor.transform.position - _xrOrigin.position).normalized;
                float angle = Vector3.Angle(_xrOrigin.forward, directionToAnchor);
                bool outOfAngle = angle > _maxUIDetectionAngle && !anchor.ignoreViewAngle;
                
                // u5982u679cu8d85u51fau8303u56f4u6216u89c6u89d2uff0cu4e14u9700u8981u81eau52a8u9690u85cf
                if ((outOfRange || outOfAngle) && anchor.hideWhenOutOfRange && !anchor.isPinned)
                {
                    HideUI(anchor);
                }
                // u5982u679cu8981u6c42u6301u7eedu51ddu89c6u5e76u4e14u4e0du518du51ddu89c6
                else if (anchor.requireContinuousGaze && !IsGazingAt(anchor) && !anchor.isPinned)
                {
                    HideUI(anchor);
                }
            }
        }
        
        /// <summary>
        /// u68c0u67e5u662fu5426u6b63u5728u51ddu89c6u7279u5b9au9501u70b9
        /// </summary>
        private bool IsGazingAt(UI3DAnchorPoint anchor)
        {
            if (_xrOrigin == null || anchor == null) return false;
            
            // u4f7fu7528u5c04u7ebfu68c0u6d4bu662fu5426u6b63u5728u51ddu89c6u6b64u9501u70b9
            Ray gazeRay = new Ray(_xrOrigin.position, _xrOrigin.forward);
            if (Physics.Raycast(gazeRay, out RaycastHit hit, _maxUIDetectionDistance))
            {
                // u68c0u67e5u662fu5426u51ddu89c6u5230u4e86u8fd9u4e2au9501u70b9u6216u5176u5b50u5bf9u8c61
                return hit.transform == anchor.transform || hit.transform.IsChildOf(anchor.transform);
            }
            
            return false;
        }
        
        /// <summary>
        /// u663eu793aUI
        /// </summary>
        public void ShowUI(UI3DAnchorPoint anchor)
        {
            if (anchor == null) return;
            
            // u8c03u7528u9501u70b9u7684u663eu793au65b9u6cd5
            anchor.ShowUI();
            
            // u6dfbu52a0u5230u6d3bu52a8u5217u8868
            if (!_activeAnchors.Contains(anchor))
            {
                _activeAnchors.Add(anchor);
            }
        }
        
        /// <summary>
        /// u9690u85cfUI
        /// </summary>
        public void HideUI(UI3DAnchorPoint anchor)
        {
            if (anchor == null) return;
            
            // u8c03u7528u9501u70b9u7684u9690u85cfu65b9u6cd5
            anchor.HideUI();
            
            // u4eceu6d3bu52a8u5217u8868u4e2du79fbu9664
            _activeAnchors.Remove(anchor);
        }
        
        /// <summary>
        /// u9690u85cfu6240u6709u6d3bu52a8u7684UI
        /// </summary>
        public void HideAllActiveUIs()
        {
            for (int i = _activeAnchors.Count - 1; i >= 0; i--)
            {
                HideUI(_activeAnchors[i]);
            }
            _activeAnchors.Clear();
        }
        
        /// <summary>
        /// u5207u6362UIu7684u56fau5b9au72b6u6001
        /// </summary>
        public void TogglePinUI(UI3DAnchorPoint anchor)
        {
            if (anchor == null) return;
            
            anchor.isPinned = !anchor.isPinned;
            if (anchor.isPinned)
            {
                // u5982u679cu56fau5b9auff0cu786eu4fddu663eu793a
                ShowUI(anchor);
            }
        }
        
        /// <summary>
        /// u83b7u53d6u5f53u524du6d3bu52a8u7684UIu9501u70b9u5217u8868
        /// </summary>
        public List<UI3DAnchorPoint> GetActiveAnchors()
        {
            return new List<UI3DAnchorPoint>(_activeAnchors);
        }
    }
}
