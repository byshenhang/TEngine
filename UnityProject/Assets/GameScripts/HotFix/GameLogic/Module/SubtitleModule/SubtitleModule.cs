using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 字幕效果模块 - 支持复杂歌词和多种动画效果
    /// </summary>
    public sealed class SubtitleModule : Singleton<SubtitleModule>, IUpdate
    {
        private readonly Dictionary<string, ISubtitleSequence> _activeSequences = new();
        private readonly Dictionary<Type, ISubtitleEffectFactory> _effectFactories = new();
        private readonly List<ISubtitleEffect> _activeEffects = new();
        private SubtitleResourceLoader _resourceLoader;
        private Transform _subtitleRoot;
        
        /// <summary>
        /// 字幕根节点
        /// </summary>
        public Transform SubtitleRoot => _subtitleRoot;
        
        /// <summary>
        /// 模块初始化
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();
            
            // 创建字幕根节点
            GameObject rootObj = new GameObject("SubtitleRoot");
            
            // 只在运行时模式下调用DontDestroyOnLoad
            if (UnityEngine.Application.isPlaying)
            {
                GameObject.DontDestroyOnLoad(rootObj);
            }
            
            _subtitleRoot = rootObj.transform;
            
            // 初始化资源加载器
            _resourceLoader = new SubtitleResourceLoader();
            
            // 注册默认效果工厂
            RegisterDefaultEffectFactories();
            
            Log.Info("[SubtitleModule] 字幕模块初始化完成");
        }
        
        /// <summary>
        /// 模块更新
        /// </summary>
        public void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            
            // 更新所有活动的字幕序列
            foreach (var sequence in _activeSequences.Values)
            {
                sequence.Update(deltaTime);
            }
            
            // 更新所有活动的效果
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.Update(deltaTime);
                
                // 移除已完成的效果
                if (effect.IsCompleted)
                {
                    effect.Release();
                    _activeEffects.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 播放字幕序列
        /// </summary>
        /// <param name="sequenceId">序列ID</param>
        /// <param name="config">字幕配置</param>
        public async UniTask PlaySubtitleSequenceAsync(string sequenceId, SubtitleSequenceConfig config)
        {
            if (_activeSequences.ContainsKey(sequenceId))
            {
                Log.Warning($"[SubtitleModule] 字幕序列 {sequenceId} 已在播放中");
                return;
            }
            
            // 创建字幕序列
            var sequence = CreateSubtitleSequence(config);
            _activeSequences[sequenceId] = sequence;
            
            try
            {
                // 播放序列
                await sequence.PlayAsync();
            }
            finally
            {
                // 清理序列
                _activeSequences.Remove(sequenceId);
                sequence.Release();
            }
        }
        
        /// <summary>
        /// 停止字幕序列
        /// </summary>
        /// <param name="sequenceId">序列ID</param>
        public void StopSubtitleSequence(string sequenceId)
        {
            if (_activeSequences.TryGetValue(sequenceId, out var sequence))
            {
                sequence.Stop();
                _activeSequences.Remove(sequenceId);
                sequence.Release();
            }
        }
        
        /// <summary>
        /// 创建单个字幕效果
        /// </summary>
        /// <typeparam name="T">效果类型</typeparam>
        /// <param name="config">效果配置</param>
        /// <returns>字幕效果实例</returns>
        public T CreateSubtitleEffect<T>(ISubtitleEffectConfig config) where T : class, ISubtitleEffect
        {
            var effectType = typeof(T);
            if (_effectFactories.TryGetValue(effectType, out var factory))
            {
                var effect = factory.CreateEffect(config) as T;
                if (effect != null)
                {
                    _activeEffects.Add(effect);
                    return effect;
                }
            }
            
            Log.Error($"[SubtitleModule] 无法创建字幕效果: {effectType.Name}");
            return null;
        }
        
        /// <summary>
        /// 创建字幕效果（通用方法）
        /// </summary>
        /// <param name="config">效果配置</param>
        /// <returns>字幕效果实例</returns>
        public ISubtitleEffect CreateEffect(ISubtitleEffectConfig config)
        {
            // 根据配置类型创建对应的效果
            return config switch
            {
                FadeEffectConfig fadeConfig => CreateSubtitleEffect<FadeSubtitleEffect>(fadeConfig),
                ScaleEffectConfig scaleConfig => CreateSubtitleEffect<ScaleSubtitleEffect>(scaleConfig),
                BlurEffectConfig blurConfig => CreateSubtitleEffect<BlurSubtitleEffect>(blurConfig),
                TypewriterEffectConfig typewriterConfig => CreateSubtitleEffect<TypewriterSubtitleEffect>(typewriterConfig),
                _ => null
            };
        }
        
        /// <summary>
        /// 注册效果工厂
        /// </summary>
        /// <typeparam name="T">效果类型</typeparam>
        /// <param name="factory">效果工厂</param>
        public void RegisterEffectFactory<T>(ISubtitleEffectFactory factory) where T : ISubtitleEffect
        {
            _effectFactories[typeof(T)] = factory;
        }
        
        /// <summary>
        /// 创建字幕序列
        /// </summary>
        private ISubtitleSequence CreateSubtitleSequence(SubtitleSequenceConfig config)
        {
            return config.SequenceType switch
            {
                SubtitleSequenceType.Simple => new SimpleSubtitleSequence(config, this),
                SubtitleSequenceType.Complex => new ComplexSubtitleSequence(config, this),
                SubtitleSequenceType.Lyric => new LyricSubtitleSequence(config, this),
                _ => new SimpleSubtitleSequence(config, this)
            };
        }
        
        /// <summary>
        /// 注册默认效果工厂
        /// </summary>
        private void RegisterDefaultEffectFactories()
        {
            RegisterEffectFactory<BlurSubtitleEffect>(new BlurSubtitleEffectFactory());
            RegisterEffectFactory<FadeSubtitleEffect>(new FadeSubtitleEffectFactory());
            RegisterEffectFactory<TypewriterSubtitleEffect>(new TypewriterSubtitleEffectFactory());
            RegisterEffectFactory<ScaleSubtitleEffect>(new ScaleSubtitleEffectFactory());
        }
        
        /// <summary>
        /// 模块释放
        /// </summary>
        public override void Release()
        {
            // 停止所有序列
            foreach (var sequence in _activeSequences.Values)
            {
                sequence.Stop();
                sequence.Release();
            }
            _activeSequences.Clear();
            
            // 释放所有效果
            foreach (var effect in _activeEffects)
            {
                effect.Release();
            }
            _activeEffects.Clear();
            
            // 清理工厂
            _effectFactories.Clear();
            
            // 销毁根节点
            if (_subtitleRoot != null)
            {
                GameObject.Destroy(_subtitleRoot.gameObject);
                _subtitleRoot = null;
            }
            
            base.Release();
            Log.Info("[SubtitleModule] 字幕模块已释放");
        }
    }
}