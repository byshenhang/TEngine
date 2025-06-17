using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using LyricFX.Implementations.Effect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LyricFX.Registry
{
    /// <summary>
    /// 效果注册表 - 管理和提供可用的视觉效果实现
    /// </summary>
    public static class EffectRegistry
    {
        private static Dictionary<string, ILyricEffect> effectProviders = new Dictionary<string, ILyricEffect>();
        private static ILyricEffect defaultProvider;
        
        /// <summary>
        /// 初始化效果注册表
        /// </summary>
        public static async UniTask Initialize()
        {
            // 手动注册效果提供器
            RegisterDefaultEffects();
            
            await UniTask.CompletedTask;
            
            Debug.Log($"[效果注册表] 初始化完成，注册效果数: {effectProviders.Count}");
        }
        
        /// <summary>
        /// 注册一个效果提供器
        /// </summary>
        public static void RegisterEffectProvider(ILyricEffect provider)
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
        public static ILyricEffect GetEffectProvider(string effectId)
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
        /// 手动注册默认效果提供器
        /// </summary>
        private static void RegisterDefaultEffects()
        {
            // 手动注册默认效果
            //RegisterCustomEffect<DefaultFadeEffect>();
            //RegisterCustomEffect<SequentialBlurEffect>();
            RegisterCustomEffect<RandomColorFadeEffect>();
            RegisterCustomEffect<LeftToRightFadeEffect>();
        }
        
        /// <summary>
        /// 注册自定义效果（泛型方法，用于手动注册普通类型的效果）
        /// </summary>
        public static void RegisterCustomEffect<T>() where T : class, ILyricEffect, new()
        {
            var provider = new T();
            RegisterEffectProvider(provider);
            Debug.Log($"[效果注册表] 手动注册效果: {provider.EffectId}");
        }
        
        /// <summary>
        /// 注册普通类效果提供器
        /// </summary>
        public static void RegisterEffect(ILyricEffect provider)
        {
            if (provider != null)
            {
                RegisterEffectProvider(provider);
                Debug.Log($"[效果注册表] 手动注册效果: {provider.EffectId}");
            }
        }
        
        /// <summary>
        /// 创建默认效果提供器
        /// </summary>
        private static void CreateDefaultEffectProvider()
        {
            var defaultEffect = new DefaultFadeEffect();
            RegisterEffectProvider(defaultEffect);
            
            Debug.Log("[效果注册表] 创建默认淡入淡出效果");
        }
    }
}
