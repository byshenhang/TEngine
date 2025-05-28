using System;
using System.Collections.Generic;
using System.Linq;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// XR场景交互管理器 - 管理特定场景的交互逻辑
    /// </summary>
    public class XRSceneInteractionManager : MonoBehaviour
    {
        // 场景唯一标识符
        [SerializeField] private string _sceneId;
        /// <summary>
        /// 场景标识符
        /// </summary>
        public string SceneId => string.IsNullOrEmpty(_sceneId) ? gameObject.scene.name : _sceneId;
        
        // 场景交互规则
        [SerializeField] private List<XRInteractionRule> _interactionRules = new List<XRInteractionRule>();
        
        // 当前场景中的交互物体集合
        private readonly Dictionary<string, IXRInteractable> _sceneInteractables = new Dictionary<string, IXRInteractable>();
        
        // 交互组（用于批量管理相关交互物体）
        private readonly Dictionary<string, HashSet<string>> _interactionGroups = new Dictionary<string, HashSet<string>>();
        
        private void Awake()
        {
            // 注册到全局交互管理器
            if (XRInteractionModule.Instance != null)
            {
                XRInteractionModule.Instance.RegisterSceneManager(this);
            }
            else
            {
                Log.Error("XRInteraction模块未初始化，无法注册场景交互管理器");
            }
            
            // 向交互事件总线注册频道
            XRInteractionEventBus.Instance.RegisterSceneChannel(SceneId);
            
            // 扫描场景中的交互物体
            ScanSceneInteractables();
        }
        
        private void OnDestroy()
        {
            // 从全局交互管理器注销
            if (XRInteractionModule.Instance != null)
            {
                XRInteractionModule.Instance.UnregisterSceneManager(this);
            }
            
            // 注销事件总线频道
            XRInteractionEventBus.Instance.UnregisterSceneChannel(SceneId);
            
            // 清理资源
            _sceneInteractables.Clear();
            _interactionGroups.Clear();
        }
        
        /// <summary>
        /// 扫描场景中的交互物体
        /// </summary>
        public void ScanSceneInteractables()
        {
            var interactables = FindObjectsOfType<MonoBehaviour>().OfType<IXRInteractable>();
            foreach (var interactable in interactables)
            {
                // 只注册当前场景中的交互物体
                MonoBehaviour monoBehaviour = interactable as MonoBehaviour;
                if (monoBehaviour != null && monoBehaviour.gameObject.scene == gameObject.scene)
                {
                    RegisterInteractable(interactable);
                }
            }
            
            Log.Info($"场景 {SceneId} 中找到 {_sceneInteractables.Count} 个交互物体");
        }
        
        /// <summary>
        /// 注册交互物体
        /// </summary>
        public void RegisterInteractable(IXRInteractable interactable)
        {
            if (interactable == null) return;
            
            string id = interactable.InteractableID;
            if (string.IsNullOrEmpty(id))
            {
                // 生成一个唯一ID
                MonoBehaviour mono = interactable as MonoBehaviour;
                id = mono != null 
                    ? $"{mono.gameObject.name}_{Guid.NewGuid().ToString("N")}" 
                    : $"Interactable_{Guid.NewGuid().ToString("N")}";
            }
            
            if (!_sceneInteractables.ContainsKey(id))
            {
                _sceneInteractables.Add(id, interactable);
                
                // 添加到相应的交互组
                foreach (var tag in interactable.InteractionTypes)
                {
                    if (!_interactionGroups.TryGetValue(tag, out var group))
                    {
                        group = new HashSet<string>();
                        _interactionGroups[tag] = group;
                    }
                    group.Add(id);
                }
                
                // 应用相关规则
                ApplyRulesToInteractable(interactable);
                
                Log.Info($"场景 {SceneId} 注册交互物体: {id}");
            }
        }
        
        /// <summary>
        /// 注销交互物体
        /// </summary>
        public void UnregisterInteractable(IXRInteractable interactable)
        {
            if (interactable == null) return;
            
            string id = interactable.InteractableID;
            if (!string.IsNullOrEmpty(id) && _sceneInteractables.ContainsKey(id))
            {
                _sceneInteractables.Remove(id);
                
                // 从交互组中移除
                foreach (var group in _interactionGroups.Values)
                {
                    group.Remove(id);
                }
                
                Log.Info($"场景 {SceneId} 注销交互物体: {id}");
            }
        }
        
        /// <summary>
        /// 添加交互规则
        /// </summary>
        public void AddInteractionRule(XRInteractionRule rule)
        {
            if (rule != null && !_interactionRules.Contains(rule))
            {
                _interactionRules.Add(rule);
                
                // 对现有的交互物体应用新规则
                foreach (var interactable in _sceneInteractables.Values)
                {
                    if (rule.MatchesInteractable(interactable))
                    {
                        rule.ApplyTo(interactable);
                    }
                }
                
                Log.Info($"添加交互规则: {rule.GetType().Name}");
            }
        }
        
        /// <summary>
        /// 移除交互规则
        /// </summary>
        public void RemoveInteractionRule(XRInteractionRule rule)
        {
            if (rule != null && _interactionRules.Contains(rule))
            {
                _interactionRules.Remove(rule);
                Log.Info($"移除交互规则: {rule.GetType().Name}");
            }
        }
        
        /// <summary>
        /// 应用规则到交互物体
        /// </summary>
        private void ApplyRulesToInteractable(IXRInteractable interactable)
        {
            foreach (var rule in _interactionRules)
            {
                if (rule.MatchesInteractable(interactable))
                {
                    rule.ApplyTo(interactable);
                }
            }
        }
        
        /// <summary>
        /// 创建交互组
        /// </summary>
        public void CreateInteractionGroup(string groupName, bool initiallyActive = true)
        {
            if (!string.IsNullOrEmpty(groupName) && !_interactionGroups.ContainsKey(groupName))
            {
                _interactionGroups[groupName] = new HashSet<string>();
                Log.Info($"创建交互组: {groupName}, 初始状态: {(initiallyActive ? "激活" : "禁用")}");
            }
        }
        
        /// <summary>
        /// 获取交互组中的所有交互物体
        /// </summary>
        public IEnumerable<IXRInteractable> GetInteractablesInGroup(string groupName)
        {
            if (_interactionGroups.TryGetValue(groupName, out var group))
            {
                foreach (var id in group)
                {
                    if (_sceneInteractables.TryGetValue(id, out var interactable))
                    {
                        yield return interactable;
                    }
                }
            }
        }
        
        /// <summary>
        /// 设置交互组激活状态
        /// </summary>
        public void SetGroupActive(string groupName, bool active)
        {
            foreach (var interactable in GetInteractablesInGroup(groupName))
            {
                MonoBehaviour mono = interactable as MonoBehaviour;
                if (mono != null)
                {
                    mono.gameObject.SetActive(active);
                }
            }
            
            Log.Info($"场景 {SceneId} 设置交互组 {groupName} 状态: {(active ? "激活" : "禁用")}");
        }
        
        /// <summary>
        /// 处理场景特定的交互事件
        /// </summary>
        public bool HandleInteractionEvent(XRInteractionEventBase evt)
        {
            // 检查是否有规则可以处理此事件
            foreach (var rule in _interactionRules)
            {
                if (rule.CanHandleEvent(evt))
                {
                    rule.HandleEvent(evt);
                    return true;
                }
            }
            
            return false;
        }
    }
}
