using System;
using TEngine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// XRI场景交互逻辑基类 - 提供直接绑定到XR交互物体的事件处理方法
    /// </summary>
    public abstract class XRISceneInteractionLogic : MonoBehaviour
    {
        // 引用场景交互管理器
        protected XRISceneManager _sceneManager;
        
        protected virtual void Awake()
        {
            // 获取场景交互管理器
            _sceneManager = GetComponent<XRISceneManager>();
            if (_sceneManager == null)
            {
                _sceneManager = FindObjectOfType<XRISceneManager>();
            }
            
            if (_sceneManager == null)
            {
                Log.Error("未找到场景交互管理器，请确保场景中有XRISceneManager组件");
                return;
            }
            
            // 初始化交互逻辑
            InitializeInteractions();
        }
        
        protected virtual void OnDestroy()
        {
            // 清理事件绑定
            CleanupInteractions();
        }
        
        /// <summary>
        /// 初始化交互逻辑
        /// </summary>
        protected virtual void InitializeInteractions()
        {
            // 注册要绑定事件的XR交互物体
            RegisterXRInteractions();
        }
        
        /// <summary>
        /// 注册XR交互事件
        /// </summary>
        protected abstract void RegisterXRInteractions();
        
        /// <summary>
        /// 清理事件绑定
        /// </summary>
        protected virtual void CleanupInteractions() 
        {
            // 子类可以覆盖此方法以清理特定绑定
        }
        
        /// <summary>
        /// 为指定物体注册事件处理程序
        /// </summary>
        public void RegisterInteractable(XRBaseInteractable interactable, UnityAction<SelectEnterEventArgs> selectHandler = null,
            UnityAction<SelectExitEventArgs> deselectHandler = null, UnityAction<HoverEnterEventArgs> hoverEnterHandler = null, 
            UnityAction<HoverExitEventArgs> hoverExitHandler = null, UnityAction<ActivateEventArgs> activateHandler = null, 
            UnityAction<DeactivateEventArgs> deactivateHandler = null)
        {
            if (interactable == null) return;
            
            if (selectHandler != null)
            {
                XRIModule.Instance.RegisterSelectEnterHandler(interactable, selectHandler);
            }
            
            if (deselectHandler != null)
            {
                XRIModule.Instance.RegisterSelectExitHandler(interactable, deselectHandler);
            }
            
            if (hoverEnterHandler != null)
            {
                XRIModule.Instance.RegisterHoverEnterHandler(interactable, hoverEnterHandler);
            }
            
            if (hoverExitHandler != null)
            {
                XRIModule.Instance.RegisterHoverExitHandler(interactable, hoverExitHandler);
            }
            
            if (activateHandler != null)
            {
                XRIModule.Instance.RegisterActivateHandler(interactable, activateHandler);
            }
            
            if (deactivateHandler != null)
            {
                XRIModule.Instance.RegisterDeactivateHandler(interactable, deactivateHandler);
            }
            
            Log.Info($"为{interactable.gameObject.name}注册了交互事件处理程序");
        }
        
        /// <summary>
        /// 根据物体标签注册事件处理程序
        /// </summary>
        protected void RegisterInteractablesByTag(string tag, UnityAction<SelectEnterEventArgs> selectHandler = null,
            UnityAction<SelectExitEventArgs> deselectHandler = null, UnityAction<HoverEnterEventArgs> hoverEnterHandler = null, 
            UnityAction<HoverExitEventArgs> hoverExitHandler = null, UnityAction<ActivateEventArgs> activateHandler = null, 
            UnityAction<DeactivateEventArgs> deactivateHandler = null)
        {
            var interactables = _sceneManager.GetInteractablesByTag(tag);
            foreach (var interactable in interactables)
            {
                RegisterInteractable(interactable, selectHandler, deselectHandler, 
                    hoverEnterHandler, hoverExitHandler, activateHandler, deactivateHandler);
            }
            
            Log.Info($"为标签[{tag}]的{interactables.Count}个物体注册了交互事件处理程序");
        }
        
        /// <summary>
        /// 为指定组中的所有物体注册事件处理程序
        /// </summary>
        protected void RegisterInteractablesByGroup(string groupName, UnityAction<SelectEnterEventArgs> selectHandler = null,
            UnityAction<SelectExitEventArgs> deselectHandler = null, UnityAction<HoverEnterEventArgs> hoverEnterHandler = null, 
            UnityAction<HoverExitEventArgs> hoverExitHandler = null, UnityAction<ActivateEventArgs> activateHandler = null, 
            UnityAction<DeactivateEventArgs> deactivateHandler = null)
        {
            var interactables = _sceneManager.GetInteractablesInGroup(groupName);
            foreach (var interactable in interactables)
            {
                RegisterInteractable(interactable, selectHandler, deselectHandler, 
                    hoverEnterHandler, hoverExitHandler, activateHandler, deactivateHandler);
            }
            
            Log.Info($"为组[{groupName}]的{interactables.Count}个物体注册了交互事件处理程序");
        }
        
        /// <summary>
        /// 根据名称获取交互物体
        /// </summary>
        protected XRBaseInteractable GetInteractable(string name)
        {
            return _sceneManager.GetInteractable(name);
        }
    }
}
