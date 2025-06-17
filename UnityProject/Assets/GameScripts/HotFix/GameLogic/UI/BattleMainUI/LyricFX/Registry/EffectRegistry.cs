using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using LyricFX.Implementations.Effect;
using System.Collections.Generic;
using UnityEngine;

namespace LyricFX.Registry
{
    /// <summary>
    /// 效果注册表 - 管理和提供可用的视觉效果实现
    /// </summary>
    public class EffectRegistry : MonoBehaviour
    {
        [SerializeField] private List<GameObject> effectProviderPrefabs = new List<GameObject>();
        
        private Dictionary<string, ILyricEffect> effectProviders = new Dictionary<string, ILyricEffect>();
        private ILyricEffect defaultProvider;
        
        /// <summary>
        /// 初始化效果注册表
        /// </summary>
        public async UniTask Initialize()
        {
            // 注册所有配置的效果提供器
            foreach (var prefab in effectProviderPrefabs)
            {
                if (prefab != null)
                {
                    var instance = Instantiate(prefab, transform);
                    var provider = instance.GetComponent<ILyricEffect>();
                    
                    if (provider != null)
                    {
                        RegisterEffectProvider(provider);
                    }
                    else
                    {
                        Debug.LogError($"[效果注册表] 预制体 {prefab.name} 没有实现 ILyricEffect 接口");
                        Destroy(instance);
                    }
                }
            }
            
            // 如果没有效果提供器，创建一个默认的
            if (effectProviders.Count == 0)
            {
                CreateDefaultEffectProvider();
            }
            
            await UniTask.CompletedTask;
            
            Debug.Log($"[效果注册表] 初始化完成，注册效果数: {effectProviders.Count}");
        }
        
        /// <summary>
        /// 注册一个效果提供器
        /// </summary>
        public void RegisterEffectProvider(ILyricEffect provider)
        {
            if (provider == null)
                return;
                
            string id = provider.EffectId;
            
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[效果注册表] 效果ID不能为空");
                return;
            }
            
            if (effectProviders.ContainsKey(id))
            {
                Debug.LogWarning($"[效果注册表] 效果ID'{id}'已存在，将被覆盖");
            }
            
            effectProviders[id] = provider;
            
            // 第一个注册的设为默认
            if (defaultProvider == null)
            {
                defaultProvider = provider;
            }
            
            Debug.Log($"[效果注册表] 注册效果: {id}");
        }
        
        /// <summary>
        /// 获取效果提供器
        /// </summary>
        public ILyricEffect GetEffectProvider(string effectId)
        {
            // 如果找不到请求的效果，返回默认效果
            if (string.IsNullOrEmpty(effectId) || !effectProviders.TryGetValue(effectId, out var provider))
            {
                if (effectId != "default") 
                {
                    Debug.LogWarning($"[效果注册表] 未找到效果'{effectId}'，使用默认效果");
                }
                return defaultProvider;
            }
            
            return provider;
        }
        
        /// <summary>
        /// 创建默认效果提供器
        /// </summary>
        private void CreateDefaultEffectProvider()
        {
            var defaultObj = new GameObject("DefaultFadeEffect");
            defaultObj.transform.SetParent(transform);
            
            var defaultEffect = defaultObj.AddComponent<DefaultFadeEffect>();
            RegisterEffectProvider(defaultEffect);
            
            Debug.Log("[效果注册表] 创建默认淡入淡出效果");
        }
    }
}
