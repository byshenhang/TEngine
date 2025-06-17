using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using LyricFX.Implementations.Layout;
using System.Collections.Generic;
using UnityEngine;

namespace LyricFX.Registry
{
    /// <summary>
    /// 布局注册表 - 管理和提供可用的布局实现
    /// </summary>
    public class LayoutRegistry : MonoBehaviour
    {
        [SerializeField] private List<GameObject> layoutProviderPrefabs = new List<GameObject>();
        
        private Dictionary<string, ILayoutProvider> layoutProviders = new Dictionary<string, ILayoutProvider>();
        private ILayoutProvider defaultProvider;
        
        /// <summary>
        /// 初始化布局注册表
        /// </summary>
        public async UniTask Initialize()
        {
            // 注册所有配置的布局提供器
            foreach (var prefab in layoutProviderPrefabs)
            {
                if (prefab != null)
                {
                    var instance = Instantiate(prefab, transform);
                    var provider = instance.GetComponent<ILayoutProvider>();
                    
                    if (provider != null)
                    {
                        RegisterLayoutProvider(provider);
                    }
                    else
                    {
                        Debug.LogError($"[布局注册表] 预制体 {prefab.name} 没有实现 ILayoutProvider 接口");
                        Destroy(instance);
                    }
                }
            }
            
            // 如果没有布局提供器，创建一个默认的
            if (layoutProviders.Count == 0)
            {
                CreateDefaultLayoutProvider();
            }
            
            await UniTask.CompletedTask;
            
            Debug.Log($"[布局注册表] 初始化完成，注册布局数: {layoutProviders.Count}");
        }
        
        /// <summary>
        /// 注册一个布局提供器
        /// </summary>
        public void RegisterLayoutProvider(ILayoutProvider provider)
        {
            if (provider == null)
                return;
                
            string id = provider.LayoutId;
            
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[布局注册表] 布局ID不能为空");
                return;
            }
            
            if (layoutProviders.ContainsKey(id))
            {
                Debug.LogWarning($"[布局注册表] 布局ID'{id}'已存在，将被覆盖");
            }
            
            layoutProviders[id] = provider;
            
            // 第一个注册的设为默认
            if (defaultProvider == null)
            {
                defaultProvider = provider;
            }
            
            Debug.Log($"[布局注册表] 注册布局: {id}");
        }
        
        /// <summary>
        /// 获取布局提供器
        /// </summary>
        public ILayoutProvider GetLayoutProvider(string layoutId)
        {
            // 如果找不到请求的布局，返回默认布局
            if (string.IsNullOrEmpty(layoutId) || !layoutProviders.TryGetValue(layoutId, out var provider))
            {
                if (layoutId != "default") 
                {
                    Debug.LogWarning($"[布局注册表] 未找到布局'{layoutId}'，使用默认布局");
                }
                return defaultProvider;
            }
            
            return provider;
        }
        
        /// <summary>
        /// 创建默认布局提供器
        /// </summary>
        private void CreateDefaultLayoutProvider()
        {
            var defaultObj = new GameObject("DefaultLinearLayout");
            defaultObj.transform.SetParent(transform);
            
            var defaultLayout = defaultObj.AddComponent<DefaultLinearLayout>();
            RegisterLayoutProvider(defaultLayout);
            
            Debug.Log("[布局注册表] 创建默认线性布局");
        }
    }
}
