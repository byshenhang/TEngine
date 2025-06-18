using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using LyricFX.Core;
using LyricFX.Implementations.Effect;
using LyricFX.Implementations.Coordinator;
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
        private static Dictionary<string, EffectMetadata> effectMetadata = new Dictionary<string, EffectMetadata>();
        private static Dictionary<EffectScope, List<string>> effectsByScope = new Dictionary<EffectScope, List<string>>();
        private static ILyricEffect defaultProvider;

        /// <summary>
        /// 初始化效果注册表
        /// </summary>
        public static async UniTask Initialize()
        {
            // 初始化作用域字典
            foreach (EffectScope scope in Enum.GetValues(typeof(EffectScope)))
            {
                effectsByScope[scope] = new List<string>();
            }

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
                    Debug.LogError($"[效果注册表] 未找到效果'{effectId}'，使用默认效果");
                }
                return defaultProvider;
            }

            return provider;
        }

        /// <summary>
        /// 获取效果元数据
        /// </summary>
        public static EffectMetadata GetEffectMetadata(string effectId)
        {
            effectMetadata.TryGetValue(effectId, out var metadata);
            return metadata;
        }

        /// <summary>
        /// 创建效果协调器
        /// </summary>
        public static ILineEffectCoordinator CreateCoordinator(string effectId)
        {
            if (effectMetadata.TryGetValue(effectId, out var metadata) && metadata.CoordinatorType != null)
            {
                return (ILineEffectCoordinator)Activator.CreateInstance(metadata.CoordinatorType);
            }
            return null;
        }

        /// <summary>
        /// 检查效果是否需要协调器
        /// </summary>
        public static bool RequiresCoordinator(string effectId)
        {
            var metadata = GetEffectMetadata(effectId);
            return metadata?.RequiresCoordinator ?? false;
        }

        /// <summary>
        /// 获取指定作用域的所有效果ID
        /// </summary>
        public static List<string> GetEffectsByScope(EffectScope scope)
        {
            return effectsByScope.TryGetValue(scope, out var effects) ? new List<string>(effects) : new List<string>();
        }

        /// <summary>
        /// 手动注册默认效果提供器
        /// </summary>
        private static void RegisterDefaultEffects()
        {
            // 注册字符级效果
            RegisterEffect<DefaultFadeEffect>("default_fade", EffectScope.Character);
            RegisterEffect<RandomColorFadeEffect>("random_color_fade", EffectScope.Character);

            // 注册行级效果（需要协调器）
            RegisterCoordinatorEffect<LeftToRightFadeCoordinator>("left_to_right_fade", EffectScope.Line);
            RegisterCoordinatorEffect<RandomBatchFadeCoordinator>("random_batch_fade", EffectScope.Line);
        }

        /// <summary>
        /// 注册字符级效果
        /// </summary>
        public static void RegisterEffect<TEffect>(string effectId, EffectScope scope)
            where TEffect : class, ILyricEffect, new()
        {
            var provider = new TEffect();
            RegisterEffectProvider(provider);

            // 注册元数据
            var metadata = new EffectMetadata
            {
                Id = effectId,
                Scope = scope,
                EffectType = typeof(TEffect),
                CoordinatorType = null
            };

            effectMetadata[effectId] = metadata;
            effectsByScope[scope].Add(effectId);

            Debug.Log($"[效果注册表] 注册{scope}级效果: {effectId}");
        }

        /// <summary>
        /// 注册行级效果（需要协调器）
        /// </summary>
        public static void RegisterCoordinatorEffect<TCoordinator>(string effectId, EffectScope scope)
            where TCoordinator : class, ILineEffectCoordinator, new()
        {
            // 注册元数据
            var metadata = new EffectMetadata
            {
                Id = effectId,
                Scope = scope,
                EffectType = null,
                CoordinatorType = typeof(TCoordinator)
            };

            effectMetadata[effectId] = metadata;
            effectsByScope[scope].Add(effectId);

            Debug.Log($"[效果注册表] 注册{scope}级效果: {effectId}，协调器: {typeof(TCoordinator).Name}");
        }

        /// <summary>
        /// 注册自定义效果（向后兼容）
        /// </summary>
        public static void RegisterCustomEffect<T>() where T : class, ILyricEffect, new()
        {
            var provider = new T();
            RegisterEffectProvider(provider);

            // 默认为字符级效果
            var metadata = new EffectMetadata
            {
                Id = provider.EffectId,
                Scope = EffectScope.Character,
                EffectType = typeof(T),
                CoordinatorType = null
            };

            effectMetadata[provider.EffectId] = metadata;
            effectsByScope[EffectScope.Character].Add(provider.EffectId);

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

    }
}
