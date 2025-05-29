using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// XR场景状态管理器 - 用于保存和恢复场景中交互物体的状态
    /// </summary>
    public class XRSceneStateManager : MonoBehaviour
    {
        [Tooltip("是否在场景卸载时自动保存状态")]
        public bool AutoSaveOnUnload = true;
        
        [Tooltip("是否在场景加载时自动恢复状态")]
        public bool AutoRestoreOnLoad = true;
        
        [Tooltip("是否保存到本地存储 (PlayerPrefs)")]
        public bool SaveToPlayerPrefs = true;
        
        // 场景标识符
        private string _sceneId;
        
        // 场景管理器引用
        private XRISceneManager _sceneManager;
        
        // 保存的状态数据
        private Dictionary<string, InteractableState> _savedStates = new Dictionary<string, InteractableState>();
        
        private void Awake()
        {
            // 获取场景管理器
            _sceneManager = GetComponent<XRISceneManager>();
            if (_sceneManager == null)
            {
                _sceneManager = FindObjectOfType<XRISceneManager>();
            }
            
            if (_sceneManager != null)
            {
                _sceneId = _sceneManager.SceneId;
                
                // 如果启用了自动恢复，尝试从保存的数据中恢复
                if (AutoRestoreOnLoad)
                {
                    LoadSavedStates();
                    RestoreInteractableStates();
                }
            }
            else
            {
                Log.Error("未找到 XRISceneManager，状态管理将无法工作");
                enabled = false;
            }
        }
        
        private void OnDestroy()
        {
            // 在场景卸载时自动保存状态
            if (AutoSaveOnUnload)
            {
                SaveInteractableStates();
                SaveStatesToStorage();
            }
        }
        
        /// <summary>
        /// 保存所有交互物体的状态
        /// </summary>
        public void SaveInteractableStates()
        {
            if (_sceneManager == null) return;
            
            _savedStates.Clear();
            
            // 遍历场景中的交互物体
            var interactables = FindObjectsOfType<XRBaseInteractable>();
            foreach (var interactable in interactables)
            {
                // 仅处理当前场景中的物体
                if (interactable.gameObject.scene.name == gameObject.scene.name)
                {
                    string objectId = GetInteractableId(interactable);
                    
                    // 创建并保存状态
                    var state = CreateInteractableState(interactable);
                    _savedStates[objectId] = state;
                    
                    Log.Info($"已保存 {interactable.name} 的状态");
                }
            }
            
            Log.Info($"场景 {_sceneId} 中共保存了 {_savedStates.Count} 个物体的状态");
        }
        
        /// <summary>
        /// 恢复所有交互物体的状态
        /// </summary>
        public void RestoreInteractableStates()
        {
            if (_sceneManager == null || _savedStates.Count == 0) return;
            
            // 遍历场景中的交互物体
            var interactables = FindObjectsOfType<XRBaseInteractable>();
            int restoredCount = 0;
            
            foreach (var interactable in interactables)
            {
                // 仅处理当前场景中的物体
                if (interactable.gameObject.scene.name == gameObject.scene.name)
                {
                    string objectId = GetInteractableId(interactable);
                    
                    // 如果找到保存的状态，则恢复
                    if (_savedStates.TryGetValue(objectId, out var state))
                    {
                        ApplyInteractableState(interactable, state);
                        restoredCount++;
                        
                        Log.Info($"已恢复 {interactable.name} 的状态");
                    }
                }
            }
            
            Log.Info($"场景 {_sceneId} 中共恢复了 {restoredCount} 个物体的状态");
        }
        
        /// <summary>
        /// 将状态保存到本地存储
        /// </summary>
        public void SaveStatesToStorage()
        {
            if (_savedStates.Count == 0) return;
            
            if (SaveToPlayerPrefs)
            {
                // 将状态序列化为 JSON
                string statesJson = JsonUtility.ToJson(new SceneStateData { States = new List<InteractableState>(_savedStates.Values) });
                
                // 保存到 PlayerPrefs
                string key = $"XRScene_{_sceneId}_States";
                PlayerPrefs.SetString(key, statesJson);
                PlayerPrefs.Save();
                
                Log.Info($"已将场景 {_sceneId} 的状态保存到 PlayerPrefs");
            }
            else
            {
                // 这里可以扩展其他存储方式，如文件系统、云存储等
                Log.Warning("未实现其他存储方式");
            }
        }
        
        /// <summary>
        /// 从存储中加载保存的状态
        /// </summary>
        public void LoadSavedStates()
        {
            _savedStates.Clear();
            
            if (SaveToPlayerPrefs)
            {
                // 从 PlayerPrefs 加载
                string key = $"XRScene_{_sceneId}_States";
                string statesJson = PlayerPrefs.GetString(key, "");
                
                if (!string.IsNullOrEmpty(statesJson))
                {
                    try
                    {
                        SceneStateData stateData = JsonUtility.FromJson<SceneStateData>(statesJson);
                        if (stateData != null && stateData.States != null)
                        {
                            foreach (var state in stateData.States)
                            {
                                _savedStates[state.ObjectId] = state;
                            }
                            
                            Log.Info($"已从存储中加载 {_savedStates.Count} 个物体状态");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"加载状态数据时出错: {e.Message}");
                    }
                }
            }
            else
            {
                // 这里可以扩展其他存储方式，如文件系统、云存储等
                Log.Warning("未实现其他存储方式");
            }
        }
        
        /// <summary>
        /// 创建交互物体的状态对象
        /// </summary>
        private InteractableState CreateInteractableState(XRBaseInteractable interactable)
        {
            var state = new InteractableState
            {
                ObjectId = GetInteractableId(interactable),
                ObjectName = interactable.name,
                IsActive = interactable.gameObject.activeSelf,
                Position = interactable.transform.localPosition,
                Rotation = interactable.transform.localRotation.eulerAngles,
                Scale = interactable.transform.localScale
            };
            
            // 保存自定义属性
            var customProperties = new Dictionary<string, string>();
            
            // 检查是否有自定义值提取器
            var valueExtractor = interactable.GetComponent<MonoBehaviour>() as IXRValueExtractor;
            if (valueExtractor != null)
            {
                float value = valueExtractor.ExtractValue(interactable);
                customProperties["ExtractedValue"] = value.ToString();
            }
            
            // 检查是否有 Animator
            var animator = interactable.GetComponent<Animator>();
            if (animator != null)
            {
                // 保存主要参数
                foreach (var param in animator.parameters)
                {
                    switch (param.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            customProperties[$"Anim_{param.name}"] = animator.GetFloat(param.name).ToString();
                            break;
                        case AnimatorControllerParameterType.Int:
                            customProperties[$"Anim_{param.name}"] = animator.GetInteger(param.name).ToString();
                            break;
                        case AnimatorControllerParameterType.Bool:
                            customProperties[$"Anim_{param.name}"] = animator.GetBool(param.name).ToString();
                            break;
                    }
                }
            }
            
            // 保存其他自定义组件属性
            var customStateComponents = interactable.GetComponents<IXRStateSerializable>();
            foreach (var component in customStateComponents)
            {
                var componentProperties = component.SaveState();
                foreach (var property in componentProperties)
                {
                    customProperties[$"{component.GetType().Name}_{property.Key}"] = property.Value;
                }
            }
            
            if (customProperties.Count > 0)
            {
                state.CustomProperties = new List<CustomProperty>();
                foreach (var prop in customProperties)
                {
                    state.CustomProperties.Add(new CustomProperty { Key = prop.Key, Value = prop.Value });
                }
            }
            
            return state;
        }
        
        /// <summary>
        /// 将状态应用到交互物体
        /// </summary>
        private void ApplyInteractableState(XRBaseInteractable interactable, InteractableState state)
        {
            // 应用基础变换
            interactable.gameObject.SetActive(state.IsActive);
            interactable.transform.localPosition = state.Position;
            interactable.transform.localRotation = Quaternion.Euler(state.Rotation);
            interactable.transform.localScale = state.Scale;
            
            // 处理自定义属性
            if (state.CustomProperties != null && state.CustomProperties.Count > 0)
            {
                // 转换为字典便于查找
                var customProperties = new Dictionary<string, string>();
                foreach (var prop in state.CustomProperties)
                {
                    customProperties[prop.Key] = prop.Value;
                }
                
                // 检查是否有 Animator
                var animator = interactable.GetComponent<Animator>();
                if (animator != null)
                {
                    // 恢复动画参数
                    foreach (var param in animator.parameters)
                    {
                        string key = $"Anim_{param.name}";
                        if (customProperties.TryGetValue(key, out string valueStr))
                        {
                            switch (param.type)
                            {
                                case AnimatorControllerParameterType.Float:
                                    if (float.TryParse(valueStr, out float floatValue))
                                        animator.SetFloat(param.name, floatValue);
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    if (int.TryParse(valueStr, out int intValue))
                                        animator.SetInteger(param.name, intValue);
                                    break;
                                case AnimatorControllerParameterType.Bool:
                                    if (bool.TryParse(valueStr, out bool boolValue))
                                        animator.SetBool(param.name, boolValue);
                                    break;
                                case AnimatorControllerParameterType.Trigger:
                                    if (bool.TryParse(valueStr, out bool triggerValue) && triggerValue)
                                        animator.SetTrigger(param.name);
                                    break;
                            }
                        }
                    }
                }
                
                // 恢复自定义组件状态
                var customStateComponents = interactable.GetComponents<IXRStateSerializable>();
                foreach (var component in customStateComponents)
                {
                    var componentProperties = new Dictionary<string, string>();
                    string prefix = $"{component.GetType().Name}_";
                    
                    foreach (var prop in customProperties)
                    {
                        if (prop.Key.StartsWith(prefix))
                        {
                            string actualKey = prop.Key.Substring(prefix.Length);
                            componentProperties[actualKey] = prop.Value;
                        }
                    }
                    
                    if (componentProperties.Count > 0)
                    {
                        component.LoadState(componentProperties);
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取交互物体的唯一标识符
        /// </summary>
        private string GetInteractableId(XRBaseInteractable interactable)
        {
            // 首先检查是否有唯一 ID 组件
            var idComponent = interactable.GetComponent<XRObjectIdentifier>();
            if (idComponent != null && !string.IsNullOrEmpty(idComponent.UniqueId))
            {
                return idComponent.UniqueId;
            }
            
            // 如果没有 ID 组件，则使用层级路径作为唯一标识
            return GetObjectPath(interactable.gameObject);
        }
        
        /// <summary>
        /// 获取游戏对象的层级路径
        /// </summary>
        private string GetObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
    }
    
    /// <summary>
    /// 交互物体状态接口 - 用于自定义组件状态序列化
    /// </summary>
    public interface IXRStateSerializable
    {
        /// <summary>
        /// 保存组件状态
        /// </summary>
        Dictionary<string, string> SaveState();
        
        /// <summary>
        /// 加载组件状态
        /// </summary>
        void LoadState(Dictionary<string, string> state);
    }
    
    /// <summary>
    /// 对象唯一标识组件
    /// </summary>
    public class XRObjectIdentifier : MonoBehaviour
    {
        [Tooltip("对象的唯一标识符")]
        public string UniqueId;
        
        private void Awake()
        {
            if (string.IsNullOrEmpty(UniqueId))
            {
                // 生成一个新的唯一 ID
                UniqueId = System.Guid.NewGuid().ToString();
            }
        }
    }
    
    /// <summary>
    /// 自定义属性类 - 用于序列化
    /// </summary>
    [Serializable]
    public class CustomProperty
    {
        public string Key;
        public string Value;
    }
    
    /// <summary>
    /// 交互物体状态类 - 用于序列化
    /// </summary>
    [Serializable]
    public class InteractableState
    {
        public string ObjectId;
        public string ObjectName;
        public bool IsActive;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public List<CustomProperty> CustomProperties;
    }
    
    /// <summary>
    /// 场景状态数据类 - 用于 JSON 序列化
    /// </summary>
    [Serializable]
    public class SceneStateData
    {
        public List<InteractableState> States;
    }
}
