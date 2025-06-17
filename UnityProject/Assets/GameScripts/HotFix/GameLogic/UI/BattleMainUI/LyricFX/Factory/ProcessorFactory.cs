using Cysharp.Threading.Tasks;
using LyricFX.Core.Pipeline;
using LyricFX.Processors;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LyricFX.Factory
{
    /// <summary>
    /// 处理器工厂 - 负责创建各种管道处理器
    /// </summary>
    public class ProcessorFactory 
    {
        private CharacterFactory characterFactory;
        
        // 存储处理器类型和预制体的映射
        private Dictionary<string, GameObject> processorPrefabs = new Dictionary<string, GameObject>();
        
        /// <summary>
        /// 初始化处理器工厂
        /// </summary>
        public async UniTask Initialize(CharacterFactory character)
        {
            if (characterFactory == null)
            {
                characterFactory = character;
                if (characterFactory == null)
                {
                    Debug.LogError("[处理器工厂] 未找到字符工厂，处理器可能无法正常工作");
                }
            }
            
            // 可以从Resources加载处理器预制体
            // 或者通过Inspector配置
            
            await UniTask.CompletedTask;
            
            Debug.Log("[处理器工厂] 初始化完成");
        }
        
        /// <summary>
        /// 创建处理器实例
        /// </summary>
        public async UniTask<T> CreateProcessor<T>() where T : ICharacterProcessor
        {
            string processorName = typeof(T).Name;
            
            // 尝试从预制体创建
            if (processorPrefabs.TryGetValue(processorName, out var prefab))
            {
                
                var processor = Activator.CreateInstance(typeof(T));
            }
            
            // 自动创建内置处理器
            if (typeof(T) == typeof(CharacterCreationProcessor))
            {
                var processor = Activator.CreateInstance(typeof(CharacterCreationProcessor)) as CharacterCreationProcessor;
                processor.Initialize(characterFactory);
                return (T)(ICharacterProcessor)processor;
            }
            else if (typeof(T) == typeof(LayoutApplicationProcessor))
            {
                var processor = Activator.CreateInstance(typeof(LayoutApplicationProcessor)) as LayoutApplicationProcessor;
                return (T)(ICharacterProcessor)processor;
            }
            else if (typeof(T) == typeof(EffectApplicationProcessor))
            {
                var processor = Activator.CreateInstance(typeof(EffectApplicationProcessor)) as EffectApplicationProcessor;
                return (T)(ICharacterProcessor)processor;
            }
            
            // 如果是自定义处理器，尝试创建实例
            try
            {
                var processor = Activator.CreateInstance(typeof(T)) as ICharacterProcessor;  
                
                if (processor != null)
                {
                    Debug.Log($"[处理器工厂] 创建处理器: {processorName}");
                    return (T)processor;
                }
                else
                {
                    Debug.LogError($"[处理器工厂] 无法添加组件: {processorName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[处理器工厂] 创建处理器失败: {processorName}, 错误: {ex.Message}");
            }
            
            Debug.LogError($"[处理器工厂] 无法创建处理器: {processorName}");
            return default;
        }
        
        /// <summary>
        /// 注册处理器预制体
        /// </summary>
        public void RegisterProcessorPrefab(string processorName, GameObject prefab)
        {
            if (prefab == null)
                return;
                
            processorPrefabs[processorName] = prefab;
            Debug.Log($"[处理器工厂] 注册处理器预制体: {processorName}");
        }
    }
}
