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
    public static class LayoutRegistry
    {
        private static Dictionary<string, ILayoutProvider> layoutProviders = new Dictionary<string, ILayoutProvider>();
        private static ILayoutProvider defaultProvider;
        
        /// <summary>
        /// 初始化布局注册表
        /// </summary>
        public static async UniTask Initialize()
        {
            // 手动注册布局提供器
            RegisterDefaultLayouts();
            
            await UniTask.CompletedTask;
            
            Debug.Log($"[布局注册表] 初始化完成，注册布局数: {layoutProviders.Count}");
        }
        
        /// <summary>
        /// 注册一个布局提供器
        /// </summary>
        public static void RegisterLayoutProvider(ILayoutProvider provider)
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
        public static ILayoutProvider GetLayoutProvider(string layoutId)
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
        /// 手动注册默认布局提供器
        /// </summary>
        private static void RegisterDefaultLayouts()
        {
            // 手动注册 DefaultLinearLayout
            CreateDefaultLayoutProvider();
            
            // 注册多行布局
            CreateMultiLineLayoutProvider();
            
            // 在这里可以手动添加其他布局提供器
            // 例如：
            // RegisterCustomLayout<SomeOtherLayout>();
        }
        
        /// <summary>
        /// 注册普通类布局提供器
        /// </summary>
        public static void RegisterLayout(ILayoutProvider provider)
        {
            if (provider != null)
            {
                RegisterLayoutProvider(provider);
                Debug.Log($"[布局注册表] 手动注册布局: {provider.LayoutId}");
            }
        }
        
        /// <summary>
        /// 创建默认布局提供器
        /// </summary>
        private static void CreateDefaultLayoutProvider()
        {
            var defaultLayout = new DefaultLinearLayout();
            RegisterLayoutProvider(defaultLayout);
            
            Debug.Log("[布局注册表] 创建默认线性布局");
        }
        
        /// <summary>
        /// 创建多行布局提供器
        /// </summary>
        private static void CreateMultiLineLayoutProvider()
        {
            var multiLineLayout = new MultiLineLayout();
            RegisterLayoutProvider(multiLineLayout);
            
            Debug.Log("[布局注册表] 创建多行布局");
        }
    }
}
