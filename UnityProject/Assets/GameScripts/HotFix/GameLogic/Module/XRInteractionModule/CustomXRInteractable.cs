using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// u81eau5b9au4e49u4ea4u4e92u7269u4f53 - u6865u63a5Unity XRu4ea4u4e92u7cfbu7edfu4e0eu73b0u6709IXRInteractableu63a5u53e3
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class CustomXRInteractable : MonoBehaviour, IXRInteractable
    {
        [SerializeField] private string _id;
        [SerializeField] private InteractableType _type = InteractableType.Grabbable;
        [SerializeField] private int _priority = 0;
        [SerializeField] private List<string> _interactionTypes = new List<string>();
        
        // u5185u90e8Unity XRu4ea4u4e92u7ec4u4ef6u5f15u7528
        private XRBaseInteractable _xrInteractable;
        
        // IXRInteractableu63a5u53e3u5b9eu73b0
        public string InteractableID => _id;
        public HashSet<string> InteractionTypes { get; private set; } = new HashSet<string>();
        public int InteractionPriority => _priority;
        
        /// <summary>
        /// u83b7u53d6u5185u90e8Unity XRu4ea4u4e92u7ec4u4ef6
        /// </summary>
        public XRBaseInteractable XRInteractable => _xrInteractable;
        
        private void Awake()
        {
            // u83b7u53d6u6216u6dfbu52a0Unity XRu4ea4u4e92u7ec4u4ef6
            _xrInteractable = GetComponent<XRBaseInteractable>();
            if (_xrInteractable == null)
            {
                // u6839u636eu4ea4u4e92u7c7bu578bu6dfbu52a0u5408u9002u7684u7ec4u4ef6
                _xrInteractable = AddInteractableComponent();
            }
            
            // u8bbeu7f6eID
            if (string.IsNullOrEmpty(_id))
            {
                _id = System.Guid.NewGuid().ToString();
            }
            
            // u521du59cbu5316u4ea4u4e92u7c7bu578bu96c6u5408
            InitializeInteractionTypes();
            
            // u914du7f6eu4ea4u4e92u7ec4u4ef6
            ConfigureInteractable();
        }
        
        private void Start()
        {
            // u6ce8u518cu5230u573au666fu4ea4u4e92u7ba1u7406u5668
            var sceneManager = FindSceneManager();
            if (sceneManager != null)
            {
                sceneManager.RegisterInteractable(this);
            }
        }
        
        private void OnDestroy()
        {
            // u4eceu573au666fu4ea4u4e92u7ba1u7406u5668u6ce8u9500
            var sceneManager = FindSceneManager();
            if (sceneManager != null)
            {
                sceneManager.UnregisterInteractable(this);
            }
        }
        
        /// <summary>
        /// u6839u636eu4ea4u4e92u7c7bu578bu6dfbu52a0u5408u9002u7684u4ea4u4e92u7ec4u4ef6
        /// </summary>
        private XRBaseInteractable AddInteractableComponent()
        {
            switch (_type)
            {
                case InteractableType.Grabbable:
                    return gameObject.AddComponent<XRGrabInteractable>();
                    
                case InteractableType.Button:
                    return gameObject.AddComponent<XRSimpleInteractable>();
                    
                case InteractableType.Lever:
                    // u6dfbu52a0u4e00u4e2au53efu629bu7269u4f53u5e76u914du7f6eu4e3au62c9u6746
                    return gameObject.AddComponent<XRGrabInteractable>();
                    
                case InteractableType.Touchable:
                    return gameObject.AddComponent<XRSimpleInteractable>();
                    
                default:
                    return gameObject.AddComponent<XRSimpleInteractable>();
            }
        }
        
        /// <summary>
        /// u914du7f6eu4ea4u4e92u7ec4u4ef6u5c5eu6027
        /// </summary>
        private void ConfigureInteractable()
        {
            if (_xrInteractable == null) return;
            
            switch (_type)
            {
                case InteractableType.Grabbable:
                    if (_xrInteractable is XRGrabInteractable grabInteractable)
                    {
                        grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                        grabInteractable.throwOnDetach = true;
                    }
                    break;
                    
                case InteractableType.Button:
                    // u6309u94aeu4e13u7528u8bbeu7f6e
                    break;
                    
                case InteractableType.Lever:
                    if (_xrInteractable is XRGrabInteractable leverGrab)
                    {
                        leverGrab.movementType = XRBaseInteractable.MovementType.Kinematic;
                        leverGrab.trackPosition = true;
                        leverGrab.trackRotation = true;
                        leverGrab.throwOnDetach = false;
                    }
                    break;
                    
                case InteractableType.Touchable:
                    // u89e6u6478u7269u4f53u8bbeu7f6e
                    break;
                    
                default:
                    break;
            }
            
            // u6dfbu52a0u4e8bu4ef6u76d1u542c
            SetupEvents();
        }
        
        /// <summary>
        /// u8bbeu7f6eu4e8bu4ef6u76d1u542c
        /// </summary>
        private void SetupEvents()
        {
            if (_xrInteractable is XRGrabInteractable grabInteractable)
            {
                grabInteractable.selectEntered.AddListener(OnGrabbed);
                grabInteractable.selectExited.AddListener(OnReleased);
                grabInteractable.activated.AddListener(OnActivated);
                grabInteractable.deactivated.AddListener(OnDeactivated);
            }
            else if (_xrInteractable is XRSimpleInteractable simpleInteractable)
            {
                simpleInteractable.selectEntered.AddListener(OnSelectEntered);
                simpleInteractable.selectExited.AddListener(OnSelectExited);
            }
            
            _xrInteractable.hoverEntered.AddListener(OnHoverEntered);
            _xrInteractable.hoverExited.AddListener(OnHoverExited);
        }
        
        /// <summary>
        /// u5224u65adu662fu5426u53efu4ee5u4e0eu6307u5b9au4ea4u4e92u5668u8fdbu884cu4ea4u4e92
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            // u9ed8u8ba4u5b9eu73b0uff0cu53efu4ee5u88abu5b50u7c7bu91cdu5199u4ee5u6dfbu52a0u81eau5b9au4e49u903bu8f91
            return true;
        }
        
        /// <summary>
        /// u5f53u7269u4f53u88abu6293u53d6u65f6u89e6u53d1
        /// </summary>
        private void OnGrabbed(SelectEnterEventArgs args)
        {
            // u521bu5efau5e76u5206u53d1u81eau5b9au4e49u4e8bu4ef6
            var evt = new XRGrabEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = gameObject,
                Timestamp = Time.time,
                InteractionID = System.Guid.NewGuid().ToString(),
                GrabPosition = transform.position,
                GrabRotation = transform.rotation,
                IsSecondaryGrab = false // u53efu4ee5u6839u636eu9700u8981u68c0u6d4bu662fu5426u662fu7b2cu4e8cu53eau624b
            };
            
            // u4f7fu7528u4e8bu4ef6u603bu7ebfu5206u53d1
            XRInteractionEventBus.Instance.Publish(evt);
        }
        
        /// <summary>
        /// u5f53u7269u4f53u88abu91cau653eu65f6u89e6u53d1
        /// </summary>
        private void OnReleased(SelectExitEventArgs args)
        {
            // u521bu5efau5e76u5206u53d1u81eau5b9au4e49u4e8bu4ef6
            var evt = new XRReleaseEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = gameObject,
                Timestamp = Time.time,
                InteractionID = System.Guid.NewGuid().ToString(),
                ReleaseVelocity = transform.position // u5982u679cu6709u901fu5ea6u53efu4ee5u4eceu5f00u53d1u5305u91ccu83b7u53d6
            };
            
            // u4f7fu7528u4e8bu4ef6u603bu7ebfu5206u53d1
            XRInteractionEventBus.Instance.Publish(evt);
        }
        
        /// <summary>
        /// u5f53u7269u4f53u88abu6fc0u6d3bu65f6u89e6u53d1 (u4f8bu5982u6293u53d6u72b6u6001u4e0bu6309u4e0bu6263u673a)
        /// </summary>
        private void OnActivated(ActivateEventArgs args)
        {
            // u521bu5efau5e76u5206u53d1u81eau5b9au4e49u4e8bu4ef6
            var evt = new XRButtonPressEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = gameObject,
                Timestamp = Time.time,
                InteractionID = System.Guid.NewGuid().ToString(),
                ButtonID = "trigger" // u53efu4ee5u6839u636eu9700u8981u8bbeu7f6eu4e0du540cu7684u6309u94aeu6807u8bc6
            };
            
            // u4f7fu7528u4e8bu4ef6u603bu7ebfu5206u53d1
            XRInteractionEventBus.Instance.Publish(evt);
        }
        
        /// <summary>
        /// u5f53u7269u4f53u53d6u6d88u6fc0u6d3bu65f6u89e6u53d1
        /// </summary>
        private void OnDeactivated(DeactivateEventArgs args)
        {
            // u53efu4ee5u6839u636eu9700u8981u5b9eu73b0u53d6u6d88u6fc0u6d3bu4e8bu4ef6
        }
        
        /// <summary>
        /// u5f53u9f20u6807u60acu505cu5728u7269u4f53u4e0au65f6u89e6u53d1
        /// </summary>
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // u521bu5efau5e76u5206u53d1u81eau5b9au4e49u4e8bu4ef6
            var evt = new XRHoverEnterEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = gameObject,
                Timestamp = Time.time,
                InteractionID = System.Guid.NewGuid().ToString()
            };
            
            // u4f7fu7528u4e8bu4ef6u603bu7ebfu5206u53d1
            XRInteractionEventBus.Instance.Publish(evt);
        }
        
        /// <summary>
        /// u5f53u9f20u6807u79fbu51fau7269u4f53u65f6u89e6u53d1
        /// </summary>
        private void OnHoverExited(HoverExitEventArgs args)
        {
            // u521bu5efau5e76u5206u53d1u81eau5b9au4e49u4e8bu4ef6
            var evt = new XRHoverExitEvent
            {
                Interactor = args.interactorObject.transform.gameObject,
                Interactable = gameObject,
                Timestamp = Time.time,
                InteractionID = System.Guid.NewGuid().ToString()
            };
            
            // u4f7fu7528u4e8bu4ef6u603bu7ebfu5206u53d1
            XRInteractionEventBus.Instance.Publish(evt);
        }
        
        /// <summary>
        /// u5f53u7b80u5355u4ea4u4e92u7269u4f53u88abu9009u62e9u65f6u89e6u53d1
        /// </summary>
        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // u5bf9u4e8eu6309u94aeu7c7bu578buff0cu53efu4ee5u89e6u53d1u6309u94aeu4e8bu4ef6
            if (_type == InteractableType.Button)
            {
                var evt = new XRButtonPressEvent
                {
                    Interactor = args.interactorObject.transform.gameObject,
                    Interactable = gameObject,
                    Timestamp = Time.time,
                    InteractionID = System.Guid.NewGuid().ToString(),
                    ButtonID = _id // u4f7fu7528u7269u4f53IDu4f5cu4e3au6309u94aeID
                };
                
                // u4f7fu7528u4e8bu4ef6u603bu7ebfu5206u53d1
                XRInteractionEventBus.Instance.Publish(evt);
            }
            // u5bf9u4e8eu89e6u6478u7c7bu578buff0cu53efu4ee5u89e6u53d1u89e6u6478u4e8bu4ef6
            else if (_type == InteractableType.Touchable)
            {
                var evt = new XRTouchEvent
                {
                    Interactor = args.interactorObject.transform.gameObject,
                    Interactable = gameObject,
                    Timestamp = Time.time,
                    InteractionID = System.Guid.NewGuid().ToString(),
                    TouchPosition = transform.position // u5982u679cu6709u5177u4f53u89e6u6478u70b9u53efu4ee5u66f4u65b0
                };
                
                // u4f7fu7528u4e8bu4ef6u603bu7ebfu5206u53d1
                XRInteractionEventBus.Instance.Publish(evt);
            }
        }
        
        /// <summary>
        /// u5f53u7b80u5355u4ea4u4e92u7269u4f53u88abu53d6u6d88u9009u62e9u65f6u89e6u53d1
        /// </summary>
        private void OnSelectExited(SelectExitEventArgs args)
        {
            // u53efu4ee5u6839u636eu4ea4u4e92u7c7bu578bu5b9eu73b0u76f8u5e94u4e8bu4ef6
        }
        
        /// <summary>
        /// u521du59cbu5316u4ea4u4e92u7c7bu578bu96c6u5408
        /// </summary>
        private void InitializeInteractionTypes()
        {
            // u6e05u7a7au5e76u521du59cbu5316
            InteractionTypes.Clear();
            
            // u6839u636eu8bbeu7f6eu7684u4ea4u4e92u7c7bu578bu6dfbu52a0u9ed8u8ba4u7c7bu578bu6807u8bc6
            switch (_type)
            {
                case InteractableType.Grabbable:
                    InteractionTypes.Add("grabbable");
                    break;
                case InteractableType.Button:
                    InteractionTypes.Add("button");
                    break;
                case InteractableType.Lever:
                    InteractionTypes.Add("lever");
                    break;
                case InteractableType.Touchable:
                    InteractionTypes.Add("touchable");
                    break;
                case InteractableType.Scalable:
                    InteractionTypes.Add("scalable");
                    break;
                case InteractableType.UI:
                    InteractionTypes.Add("ui");
                    break;
            }
            
            // u6dfbu52a0u5b9au4e49u7684u989du5916u4ea4u4e92u7c7bu578b
            foreach (var type in _interactionTypes)
            {
                if (!string.IsNullOrEmpty(type))
                {
                    InteractionTypes.Add(type);
                }
            }
        }
        
        /// <summary>
        /// u5b9eu73b0IXRInteractableu63a5u53e3u7684u4ea4u4e92u5904u7406u65b9u6cd5
        /// </summary>
        public void ProcessInteraction(XRInteractionEventBase interactionEvent)
        {
            // u6839u636eu4ea4u4e92u4e8bu4ef6u7c7bu578bu5904u7406
            if (interactionEvent is XRGrabEvent grabEvent)
            {
                // u5904u7406u6293u53d6u4e8bu4ef6
                Debug.Log($"\"[{_id}]\" u5904u7406u6293u53d6u4e8bu4ef6");
            }
            else if (interactionEvent is XRReleaseEvent releaseEvent)
            {
                // u5904u7406u91cau653eu4e8bu4ef6
                Debug.Log($"\"[{_id}]\" u5904u7406u91cau653eu4e8bu4ef6");
            }
            else if (interactionEvent is XRButtonPressEvent buttonEvent)
            {
                // u5904u7406u6309u94aeu4e8bu4ef6
                Debug.Log($"\"[{_id}]\" u5904u7406u6309u94aeu4e8bu4ef6");
            }
            else if (interactionEvent is XRHoverEnterEvent hoverEnterEvent)
            {
                // u5904u7406u60acu505cu8fdbu5165u4e8bu4ef6
                Debug.Log($"\"[{_id}]\" u5904u7406u60acu505cu8fdbu5165u4e8bu4ef6");
            }
            else if (interactionEvent is XRHoverExitEvent hoverExitEvent)
            {
                // u5904u7406u60acu505cu9000u51fau4e8bu4ef6
                Debug.Log($"\"[{_id}]\" u5904u7406u60acu505cu9000u51fau4e8bu4ef6");
            }
        }
        
        /// <summary>
        /// u67e5u627eu5f53u524du573au666fu7684u4ea4u4e92u7ba1u7406u5668
        /// </summary>
        private XRSceneInteractionManager FindSceneManager()
        {
            // u5c1du8bd5u5728u5f53u524du573au666fu4e2du67e5u627e
            var sceneManagers = FindObjectsOfType<XRSceneInteractionManager>();
            foreach (var manager in sceneManagers)
            {
                if (manager.gameObject.scene == gameObject.scene)
                {
                    return manager;
                }
            }
            
            // u5982u679cu6ca1u6709u627eu5230uff0cu76f4u63a5u4f7fu7528NewXRInteractionModuleu83b7u53d6u6d3bu52a8u573au666fu7ba1u7406u5668
            if (XRInteractionModule.Instance != null)
            {
                return XRInteractionModule.Instance.GetActiveSceneManager();
            }
            
            return null;
        }
    }
}
