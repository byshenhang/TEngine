using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 字幕效果管理器 - 负责管理和协调所有字幕效果
    /// </summary>
    public class SubtitleEffectManager : IDisposable
    {
        private readonly Dictionary<string, ISubtitleEffectFactory> _effectFactories;
        private readonly List<ISubtitleEffect> _activeEffects;
        private readonly Dictionary<GameObject, List<ISubtitleEffect>> _objectEffects;
        private bool _isDisposed;
        
        public SubtitleEffectManager()
        {
            _effectFactories = new Dictionary<string, ISubtitleEffectFactory>();
            _activeEffects = new List<ISubtitleEffect>();
            _objectEffects = new Dictionary<GameObject, List<ISubtitleEffect>>();
            
            RegisterDefaultFactories();
        }
        
        /// <summary>
        /// 注册默认效果工厂
        /// </summary>
        private void RegisterDefaultFactories()
        {
            RegisterEffectFactory("Blur", new BlurSubtitleEffectFactory());
            RegisterEffectFactory("Fade", new FadeSubtitleEffectFactory());
            RegisterEffectFactory("Typewriter", new TypewriterSubtitleEffectFactory());
            RegisterEffectFactory("Scale", new ScaleSubtitleEffectFactory());
        }
        
        /// <summary>
        /// 注册效果工厂
        /// </summary>
        public void RegisterEffectFactory(string effectType, ISubtitleEffectFactory factory)
        {
            if (string.IsNullOrEmpty(effectType))
            {
                Log.Warning("[SubtitleEffectManager] 效果类型不能为空");
                return;
            }
            
            if (factory == null)
            {
                Log.Warning($"[SubtitleEffectManager] 效果工厂不能为空: {effectType}");
                return;
            }
            
            _effectFactories[effectType] = factory;
            Log.Info($"[SubtitleEffectManager] 注册效果工厂: {effectType}");
        }
        
        /// <summary>
        /// 注销效果工厂
        /// </summary>
        public void UnregisterEffectFactory(string effectType)
        {
            if (_effectFactories.ContainsKey(effectType))
            {
                _effectFactories.Remove(effectType);
                Log.Info($"[SubtitleEffectManager] 注销效果工厂: {effectType}");
            }
        }
        
        /// <summary>
        /// 创建效果
        /// </summary>
        public ISubtitleEffect CreateEffect(string effectType, ISubtitleEffectConfig config)
        {
            if (!_effectFactories.TryGetValue(effectType, out var factory))
            {
                Log.Warning($"[SubtitleEffectManager] 未找到效果工厂: {effectType}");
                return null;
            }
            
            try
            {
                var effect = factory.CreateEffect(config);
                if (effect != null)
                {
                    _activeEffects.Add(effect);
                    Log.Debug($"[SubtitleEffectManager] 创建效果: {effectType}");
                }
                return effect;
            }
            catch (Exception ex)
            {
                Log.Error($"[SubtitleEffectManager] 创建效果失败: {effectType}, 错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 应用效果到游戏对象
        /// </summary>
        public async UniTask ApplyEffectAsync(GameObject target, ISubtitleEffect effect, float delay = 0f)
        {
            if (target == null || effect == null)
            {
                Log.Warning("[SubtitleEffectManager] 目标对象或效果为空");
                return;
            }
            
            // 记录对象的效果
            if (!_objectEffects.ContainsKey(target))
            {
                _objectEffects[target] = new List<ISubtitleEffect>();
            }
            _objectEffects[target].Add(effect);
            
            try
            {
                // 应用延迟
                if (delay > 0f)
                {
                    await UniTask.Delay((int)(delay * 1000));
                }
                
                // 执行效果
                await effect.PlayAsync();
                
                Log.Debug($"[SubtitleEffectManager] 效果执行完成: {effect.GetType().Name}");
            }
            catch (Exception ex)
            {
                Log.Error($"[SubtitleEffectManager] 效果执行失败: {effect.GetType().Name}, 错误: {ex.Message}");
            }
            finally
            {
                // 清理效果记录
                if (_objectEffects.ContainsKey(target))
                {
                    _objectEffects[target].Remove(effect);
                    if (_objectEffects[target].Count == 0)
                    {
                        _objectEffects.Remove(target);
                    }
                }
            }
        }
        
        /// <summary>
        /// 应用多个效果到游戏对象
        /// </summary>
        public async UniTask ApplyEffectsAsync(GameObject target, List<ISubtitleEffect> effects, float delay = 0f)
        {
            if (target == null || effects == null || effects.Count == 0)
            {
                Log.Warning("[SubtitleEffectManager] 目标对象或效果列表为空");
                return;
            }
            
            // 并行执行所有效果
            var tasks = new List<UniTask>();
            
            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    tasks.Add(ApplyEffectAsync(target, effect, delay));
                }
            }
            
            await UniTask.WhenAll(tasks);
        }
        
        /// <summary>
        /// 停止对象的所有效果
        /// </summary>
        public void StopEffects(GameObject target)
        {
            if (target == null || !_objectEffects.ContainsKey(target))
            {
                return;
            }
            
            var effects = _objectEffects[target];
            foreach (var effect in effects)
            {
                try
                {
                    effect?.Stop();
                }
                catch (Exception ex)
                {
                    Log.Error($"[SubtitleEffectManager] 停止效果失败: {effect?.GetType().Name}, 错误: {ex.Message}");
                }
            }
            
            _objectEffects.Remove(target);
            Log.Debug($"[SubtitleEffectManager] 停止对象的所有效果: {target.name}");
        }
        
        /// <summary>
        /// 停止所有效果
        /// </summary>
        public void StopAllEffects()
        {
            foreach (var effect in _activeEffects)
            {
                try
                {
                    effect?.Stop();
                }
                catch (Exception ex)
                {
                    Log.Error($"[SubtitleEffectManager] 停止效果失败: {effect?.GetType().Name}, 错误: {ex.Message}");
                }
            }
            
            _activeEffects.Clear();
            _objectEffects.Clear();
            
            Log.Info("[SubtitleEffectManager] 停止所有效果");
        }
        
        /// <summary>
        /// 获取支持的效果类型
        /// </summary>
        public string[] GetSupportedEffectTypes()
        {
            var types = new string[_effectFactories.Count];
            _effectFactories.Keys.CopyTo(types, 0);
            return types;
        }
        
        /// <summary>
        /// 检查效果类型是否支持
        /// </summary>
        public bool IsEffectTypeSupported(string effectType)
        {
            return _effectFactories.ContainsKey(effectType);
        }
        
        /// <summary>
        /// 获取活动效果数量
        /// </summary>
        public int GetActiveEffectCount()
        {
            return _activeEffects.Count;
        }
        
        /// <summary>
        /// 获取对象的效果数量
        /// </summary>
        public int GetObjectEffectCount(GameObject target)
        {
            if (target == null || !_objectEffects.ContainsKey(target))
            {
                return 0;
            }
            
            return _objectEffects[target].Count;
        }
        
        /// <summary>
        /// 清理已完成的效果
        /// </summary>
        public void CleanupCompletedEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                if (effect == null || effect.IsCompleted)
                {
                    _activeEffects.RemoveAt(i);
                }
            }
            
            // 清理对象效果映射中的已完成效果
            var keysToRemove = new List<GameObject>();
            foreach (var kvp in _objectEffects)
            {
                for (int i = kvp.Value.Count - 1; i >= 0; i--)
                {
                    var effect = kvp.Value[i];
                    if (effect == null || effect.IsCompleted)
                    {
                        kvp.Value.RemoveAt(i);
                    }
                }
                
                if (kvp.Value.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _objectEffects.Remove(key);
            }
        }
        
        /// <summary>
        /// 更新所有效果
        /// </summary>
        public void Update()
        {
            if (_isDisposed) return;
            
            // 定期清理已完成的效果
            if (Time.frameCount % 60 == 0) // 每60帧清理一次
            {
                CleanupCompletedEffects();
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            
            StopAllEffects();
            _effectFactories.Clear();
            
            _isDisposed = true;
            Log.Info("[SubtitleEffectManager] 已释放");
        }
    }
}