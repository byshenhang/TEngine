using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// XR Interaction直接绑定模块 - 直接与Unity XR Interaction Toolkit组件交互
    /// </summary>
    public class XRIModule : Singleton<XRIModule>, IUpdate
    {
        // 注册的场景交互管理器
        private readonly Dictionary<string, XRISceneManager> _sceneManagers = new Dictionary<string, XRISceneManager>();
        
        // 跟踪已注册的事件处理程序，以便清理
        private readonly Dictionary<GameObject, Dictionary<Type, List<Delegate>>> _registeredHandlers = 
            new Dictionary<GameObject, Dictionary<Type, List<Delegate>>>();

        protected override void OnInit()
        {
            base.OnInit();
            Log.Info("XRIModule initialized");
        }

        public override void Release()
        {
            base.Release();
            ClearAllEventHandlers();
            _sceneManagers.Clear();
        }
        
        /// <summary>
        /// 注册场景交互管理器
        /// </summary>
        public void RegisterSceneManager(XRISceneManager sceneManager)
        {
            if (sceneManager == null) return;
            
            string sceneId = sceneManager.SceneId;
            if (!string.IsNullOrEmpty(sceneId) && !_sceneManagers.ContainsKey(sceneId))
            {
                _sceneManagers.Add(sceneId, sceneManager);
                Log.Info($"场景交互管理器已注册: {sceneId}");
            }
        }
        
        /// <summary>
        /// 注销场景交互管理器
        /// </summary>
        public void UnregisterSceneManager(XRISceneManager sceneManager)
        {
            if (sceneManager == null) return;
            
            string sceneId = sceneManager.SceneId;
            if (!string.IsNullOrEmpty(sceneId) && _sceneManagers.ContainsKey(sceneId))
            {
                _sceneManagers.Remove(sceneId);
                Log.Info($"场景交互管理器已注销: {sceneId}");
            }
        }
        
        /// <summary>
        /// 根据场景ID获取场景管理器
        /// </summary>
        public XRISceneManager GetSceneManager(string sceneId)
        {
            return _sceneManagers.TryGetValue(sceneId, out var manager) ? manager : null;
        }
        
        #region 交互事件绑定
        
        /// <summary>
        /// 为XR Interactable注册Hover Enter事件处理程序
        /// </summary>
        public void RegisterHoverEnterHandler(XRBaseInteractable interactable, UnityAction<HoverEnterEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.hoverEntered.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(HoverEnterEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册HoverEnter处理程序");
        }
        
        /// <summary>
        /// 为XR Interactable注册Hover Exit事件处理程序
        /// </summary>
        public void RegisterHoverExitHandler(XRBaseInteractable interactable, UnityAction<HoverExitEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.hoverExited.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(HoverExitEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册HoverExit处理程序");
        }
        
        /// <summary>
        /// 为XR Interactable注册Select Enter事件处理程序
        /// </summary>
        public void RegisterSelectEnterHandler(XRBaseInteractable interactable, UnityAction<SelectEnterEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.selectEntered.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(SelectEnterEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册SelectEnter处理程序");
        }
        
        /// <summary>
        /// 为XR Interactable注册Select Exit事件处理程序
        /// </summary>
        public void RegisterSelectExitHandler(XRBaseInteractable interactable, UnityAction<SelectExitEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.selectExited.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(SelectExitEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册SelectExit处理程序");
        }
        
        /// <summary>
        /// 为XR Interactable注册Activate事件处理程序
        /// </summary>
        public void RegisterActivateHandler(XRBaseInteractable interactable, UnityAction<ActivateEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.activated.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(ActivateEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册Activate处理程序");
        }
        
        /// <summary>
        /// 为XR Interactable注册Deactivate事件处理程序
        /// </summary>
        public void RegisterDeactivateHandler(XRBaseInteractable interactable, UnityAction<DeactivateEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.deactivated.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(DeactivateEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册Deactivate处理程序");
        }
        
        /// <summary>
        /// 为XR Interactable注册First Hover Enter事件处理程序
        /// </summary>
        public void RegisterFirstHoverEnterHandler(XRBaseInteractable interactable, UnityAction<HoverEnterEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.firstHoverEntered.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(HoverEnterEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册FirstHoverEnter处理程序");
        }
        
        /// <summary>
        /// 为XR Interactable注册Last Hover Exit事件处理程序
        /// </summary>
        public void RegisterLastHoverExitHandler(XRBaseInteractable interactable, UnityAction<HoverExitEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.lastHoverExited.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(HoverExitEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册LastHoverExit处理程序");
        }
        
        /// <summary>
        /// 为XR Interactable注册First Select Enter事件处理程序
        /// </summary>
        public void RegisterFirstSelectEnterHandler(XRBaseInteractable interactable, UnityAction<SelectEnterEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.firstSelectEntered.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(SelectEnterEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册FirstSelectEnter处理程序");
        }
        
        /// <summary>
        /// 为XR Interactable注册Last Select Exit事件处理程序
        /// </summary>
        public void RegisterLastSelectExitHandler(XRBaseInteractable interactable, UnityAction<SelectExitEventArgs> handler)
        {
            if (interactable == null || handler == null) return;
            
            interactable.lastSelectExited.AddListener(handler);
            TrackRegisteredHandler(interactable.gameObject, typeof(SelectExitEventArgs), handler);
            Log.Info($"为{interactable.gameObject.name}注册LastSelectExit处理程序");
        }
        
        /// <summary>
        /// 取消注册指定类型的所有事件处理程序
        /// </summary>
        public void UnregisterAllHandlers(XRBaseInteractable interactable)
        {
            if (interactable == null) return;
            
            var gameObject = interactable.gameObject;
            if (_registeredHandlers.TryGetValue(gameObject, out var typeHandlers))
            {
                // 清理XR事件监听
                if (typeHandlers.TryGetValue(typeof(HoverEnterEventArgs), out var hoverEnterHandlers))
                {
                    foreach (var handler in hoverEnterHandlers)
                    {
                        interactable.hoverEntered.RemoveListener((UnityAction<HoverEnterEventArgs>)handler);
                        interactable.firstHoverEntered.RemoveListener((UnityAction<HoverEnterEventArgs>)handler);
                    }
                }
                
                if (typeHandlers.TryGetValue(typeof(HoverExitEventArgs), out var hoverExitHandlers))
                {
                    foreach (var handler in hoverExitHandlers)
                    {
                        interactable.hoverExited.RemoveListener((UnityAction<HoverExitEventArgs>)handler);
                        interactable.lastHoverExited.RemoveListener((UnityAction<HoverExitEventArgs>)handler);
                    }
                }
                
                if (typeHandlers.TryGetValue(typeof(SelectEnterEventArgs), out var selectEnterHandlers))
                {
                    foreach (var handler in selectEnterHandlers)
                    {
                        interactable.selectEntered.RemoveListener((UnityAction<SelectEnterEventArgs>)handler);
                        interactable.firstSelectEntered.RemoveListener((UnityAction<SelectEnterEventArgs>)handler);
                    }
                }
                
                if (typeHandlers.TryGetValue(typeof(SelectExitEventArgs), out var selectExitHandlers))
                {
                    foreach (var handler in selectExitHandlers)
                    {
                        interactable.selectExited.RemoveListener((UnityAction<SelectExitEventArgs>)handler);
                        interactable.lastSelectExited.RemoveListener((UnityAction<SelectExitEventArgs>)handler);
                    }
                }
                
                if (typeHandlers.TryGetValue(typeof(ActivateEventArgs), out var activateHandlers))
                {
                    foreach (var handler in activateHandlers)
                    {
                        interactable.activated.RemoveListener((UnityAction<ActivateEventArgs>)handler);
                    }
                }
                
                if (typeHandlers.TryGetValue(typeof(DeactivateEventArgs), out var deactivateHandlers))
                {
                    foreach (var handler in deactivateHandlers)
                    {
                        interactable.deactivated.RemoveListener((UnityAction<DeactivateEventArgs>)handler);
                    }
                }
                
                _registeredHandlers.Remove(gameObject);
                Log.Info($"已清理{gameObject.name}的所有事件处理程序");
            }
        }
        
        /// <summary>
        /// 追踪已注册的事件处理程序
        /// </summary>
        private void TrackRegisteredHandler(GameObject gameObject, Type eventType, Delegate handler)
        {
            if (!_registeredHandlers.TryGetValue(gameObject, out var typeHandlers))
            {
                typeHandlers = new Dictionary<Type, List<Delegate>>();
                _registeredHandlers[gameObject] = typeHandlers;
            }
            
            if (!typeHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                typeHandlers[eventType] = handlers;
            }
            
            handlers.Add(handler);
        }
        
        /// <summary>
        /// 清理所有注册的事件处理程序
        /// </summary>
        private void ClearAllEventHandlers()
        {
            foreach (var gameObjectEntry in _registeredHandlers)
            {
                var interactable = gameObjectEntry.Key.GetComponent<XRBaseInteractable>();
                if (interactable != null)
                {
                    UnregisterAllHandlers(interactable);
                }
            }
            
            _registeredHandlers.Clear();
        }

        public void OnUpdate()
        {
        }

        #endregion
    }
}
