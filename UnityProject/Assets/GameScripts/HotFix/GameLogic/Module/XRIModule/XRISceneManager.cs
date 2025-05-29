using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// XRI场景管理器 - 管理特定场景中的XR交互
    /// </summary>
    [DisallowMultipleComponent]
    public class XRISceneManager : MonoBehaviour
    {
        // 场景唯一标识符
        [SerializeField] private string _sceneId;
        
        /// <summary>
        /// 场景标识符
        /// </summary>
        public string SceneId => string.IsNullOrEmpty(_sceneId) ? gameObject.scene.name : _sceneId;
        
        // 场景中的XR交互组件
        private readonly Dictionary<string, XRBaseInteractable> _interactables = new Dictionary<string, XRBaseInteractable>();
        
        // 交互组（用于批量管理相关交互物体）
        private readonly Dictionary<string, HashSet<string>> _interactionGroups = new Dictionary<string, HashSet<string>>();
        
        private void Awake()
        {
            // 注册到XRIModule
            if (XRIModule.Instance != null)
            {
                XRIModule.Instance.RegisterSceneManager(this);
            }
            else
            {
                Log.Error("XRIModule未初始化，无法注册场景管理器");
            }
            
            // 扫描场景中的交互物体
            ScanSceneInteractables();
        }
        
        private void OnDestroy()
        {
            // 注销管理器
            if (XRIModule.Instance != null)
            {
                XRIModule.Instance.UnregisterSceneManager(this);
            }
            
            // 清理资源
            _interactables.Clear();
            _interactionGroups.Clear();
        }
        
        /// <summary>
        /// 扫描场景中的交互物体
        /// </summary>
        public void ScanSceneInteractables()
        {
            var interactables = FindObjectsOfType<XRBaseInteractable>();
            foreach (var interactable in interactables)
            {
                // 只注册当前场景中的交互物体
                if (interactable.gameObject.scene == gameObject.scene)
                {
                    RegisterInteractable(interactable);
                }
            }
            
            Log.Info($"场景 {SceneId} 中找到 {_interactables.Count} 个交互物体");
        }
        
        /// <summary>
        /// 注册交互物体
        /// </summary>
        public void RegisterInteractable(XRBaseInteractable interactable)
        {
            if (interactable == null) return;
            
            string id = interactable.gameObject.name;
            if (!_interactables.ContainsKey(id))
            {
                _interactables.Add(id, interactable);
                Log.Info($"场景 {SceneId} 注册交互物体: {id}");
            }
        }
        
        /// <summary>
        /// 注销交互物体
        /// </summary>
        public void UnregisterInteractable(XRBaseInteractable interactable)
        {
            if (interactable == null) return;
            
            string id = interactable.gameObject.name;
            if (_interactables.ContainsKey(id))
            {
                _interactables.Remove(id);
                
                // 从交互组中移除
                foreach (var group in _interactionGroups.Values)
                {
                    group.Remove(id);
                }
                
                Log.Info($"场景 {SceneId} 注销交互物体: {id}");
            }
        }
        
        /// <summary>
        /// 创建交互组
        /// </summary>
        public void CreateInteractionGroup(string groupName)
        {
            if (!string.IsNullOrEmpty(groupName) && !_interactionGroups.ContainsKey(groupName))
            {
                _interactionGroups[groupName] = new HashSet<string>();
                Log.Info($"创建交互组: {groupName}");
            }
        }
        
        /// <summary>
        /// 添加交互物体到交互组
        /// </summary>
        public bool AddToGroup(string groupName, XRBaseInteractable interactable)
        {
            if (interactable == null || string.IsNullOrEmpty(groupName))
                return false;
            
            string interactableId = interactable.gameObject.name;
            
            // 确保交互组存在
            if (!_interactionGroups.TryGetValue(groupName, out var group))
            {
                group = new HashSet<string>();
                _interactionGroups[groupName] = group;
            }
            
            group.Add(interactableId);
            return true;
        }
        
        /// <summary>
        /// 从交互组中移除交互物体
        /// </summary>
        public bool RemoveFromGroup(string groupName, XRBaseInteractable interactable)
        {
            if (interactable == null || string.IsNullOrEmpty(groupName))
                return false;
            
            string interactableId = interactable.gameObject.name;
            
            if (_interactionGroups.TryGetValue(groupName, out var group))
            {
                return group.Remove(interactableId);
            }
            
            return false;
        }
        
        /// <summary>
        /// 设置交互组的激活状态
        /// </summary>
        public void SetGroupActive(string groupName, bool active)
        {
            if (string.IsNullOrEmpty(groupName) || !_interactionGroups.TryGetValue(groupName, out var group))
                return;
            
            foreach (var interactableId in group)
            {
                if (_interactables.TryGetValue(interactableId, out var interactable))
                {
                    interactable.enabled = active;
                }
            }
            
            Log.Info($"设置交互组 {groupName} 状态: {(active ? "激活" : "禁用")}");
        }
        
        /// <summary>
        /// 根据ID获取交互物体
        /// </summary>
        public XRBaseInteractable GetInteractable(string id)
        {
            return _interactables.TryGetValue(id, out var interactable) ? interactable : null;
        }
        
        /// <summary>
        /// 根据标签获取交互物体
        /// </summary>
        public List<XRBaseInteractable> GetInteractablesByTag(string tag)
        {
            var result = new List<XRBaseInteractable>();
            
            foreach (var interactable in _interactables.Values)
            {
                if (interactable.CompareTag(tag))
                {
                    result.Add(interactable);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 根据组名获取交互物体
        /// </summary>
        public List<XRBaseInteractable> GetInteractablesInGroup(string groupName)
        {
            var result = new List<XRBaseInteractable>();
            
            if (_interactionGroups.TryGetValue(groupName, out var group))
            {
                foreach (var interactableId in group)
                {
                    if (_interactables.TryGetValue(interactableId, out var interactable))
                    {
                        result.Add(interactable);
                    }
                }
            }
            
            return result;
        }
    }
}
