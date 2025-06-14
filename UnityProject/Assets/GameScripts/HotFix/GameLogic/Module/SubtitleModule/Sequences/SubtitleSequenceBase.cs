using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 字幕序列基类
    /// </summary>
    public abstract class SubtitleSequenceBase : ISubtitleSequence
    {
        protected SubtitleSequenceConfig _config;
        protected SubtitleModule _subtitleModule;
        protected GameObject _rootObject;
        protected List<GameObject> _characterObjects = new List<GameObject>();
        protected List<ISubtitleEffect> _activeEffects = new List<ISubtitleEffect>();
        protected bool _isCompleted;
        protected bool _isRunning;
        protected bool _isPaused;
        
        public bool IsCompleted => _isCompleted;
        public bool IsRunning => _isRunning;
        
        /// <summary>
        /// 暂停序列
        /// </summary>
        public virtual void Pause()
        {
            _isPaused = true;
            
            // 暂停所有活动效果
            foreach (var effect in _activeEffects)
            {
                effect.Pause();
            }
        }
        
        /// <summary>
        /// 恢复序列
        /// </summary>
        public virtual void Resume()
        {
            _isPaused = false;
            
            // 恢复所有活动效果
            foreach (var effect in _activeEffects)
            {
            effect.Resume();
            }
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected SubtitleSequenceBase(SubtitleSequenceConfig config, SubtitleModule subtitleModule)
        {
            _config = config;
            _subtitleModule = subtitleModule;
        }
        
        /// <summary>
        /// 播放序列
        /// </summary>
        public virtual async UniTask PlayAsync()
        {
            if (_config == null)
            {
                _isCompleted = true;
                return;
            }
            
            _isRunning = true;
            _isCompleted = false;
            
            try
            {
                // 创建根对象
                await CreateRootObject();
                
                // 等待开始延迟
                if (_config.StartDelay > 0)
                {
                    await UniTask.Delay((int)(_config.StartDelay * 1000));
                }
                
                // 执行具体的播放逻辑
                await ExecutePlayLogic();
                
                // 应用停留阶段效果
                ApplyStayEffects();
                
                // 等待保持时间
                if (_config.HoldDuration > 0)
                {
                    await UniTask.Delay((int)(_config.HoldDuration * 1000));
                }
                
                // 应用离场阶段效果
                ApplyExitEffects();
                
                // 执行淡出（如果没有自定义离场效果）
                await ExecuteFadeOut();
            }
            finally
            {
                _isRunning = false;
                _isCompleted = true;
            }
        }
        
        /// <summary>
        /// 停止序列
        /// </summary>
        public virtual void Stop()
        {
            _isCompleted = true;
            _isRunning = false;
            
            // 停止所有效果
            foreach (var effect in _activeEffects)
            {
                effect.Stop();
            }
        }
        
        /// <summary>
        /// 更新序列
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            if (!_isRunning) return;
            
            // 更新所有效果
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.Update(deltaTime);
                
                if (effect.IsCompleted)
                {
                    effect.Release();
                    _activeEffects.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 释放序列资源
        /// </summary>
        public virtual void Release()
        {
            // 释放所有效果
            foreach (var effect in _activeEffects)
            {
                effect.Release();
            }
            _activeEffects.Clear();
            
            // 销毁字符对象
            foreach (var charObj in _characterObjects)
            {
                if (charObj != null)
                {
                    GameObject.Destroy(charObj);
                }
            }
            _characterObjects.Clear();
            
            // 销毁根对象
            if (_rootObject != null)
            {
                GameObject.Destroy(_rootObject);
                _rootObject = null;
            }
        }
        
        /// <summary>
        /// 创建根对象
        /// </summary>
        protected virtual async UniTask CreateRootObject()
        {
            _rootObject = new GameObject($"SubtitleSequence_{GetHashCode()}");
            _rootObject.transform.SetParent(_subtitleModule.SubtitleRoot);
            _rootObject.transform.localPosition = _config.Position;
            _rootObject.transform.localRotation = _config.Rotation;
            _rootObject.transform.localScale = _config.Scale;
            
            // 如果是3D字幕，设置相应的层级
            if (_config.Is3D)
            {
                _rootObject.layer = (int)Mathf.Log(_config.UILayer, 2);
            }
            
            await UniTask.Yield();
        }
        
        /// <summary>
        /// 创建字符对象
        /// </summary>
        protected virtual GameObject CreateCharacterObject(char character, int index)
        {
            GameObject charObj = new GameObject($"Char_{index}_{character}");
            charObj.transform.SetParent(_rootObject.transform);
            
            // 添加TextMeshProUGUI组件
            var textComponent = charObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = character.ToString();
            textComponent.font = _config.Font;
            textComponent.fontSize = _config.FontSize;
            textComponent.color = _config.TextColor;
            
            if (_config.TextMaterial != null)
            {
                textComponent.material = _config.TextMaterial;
            }
            
            // 设置位置（简单的水平排列）
            float charWidth = textComponent.preferredWidth;
            Vector3 position = new Vector3(index * charWidth, 0, 0);
            charObj.transform.localPosition = position;
            
            // 初始状态设为不可见
            charObj.SetActive(false);
            
            return charObj;
        }
        
        /// <summary>
        /// 应用效果到字符对象
        /// </summary>
        protected virtual void ApplyEffectsToCharacter(GameObject charObj, float delay = 0f)
        {
            if (charObj == null || _config.Effects == null) return;
            
            // 只应用进场效果
            ApplyEffectsByPhase(charObj, SubtitleEffectPhase.Enter, delay);
        }
        
        /// <summary>
        /// 按阶段应用效果到字符对象
        /// </summary>
        protected virtual void ApplyEffectsByPhase(GameObject charObj, SubtitleEffectPhase phase, float delay = 0f)
        {
            if (charObj == null || _config.Effects == null) return;
            
            foreach (var effectConfig in _config.Effects)
            {
                // 只应用指定阶段的效果
                if (effectConfig.Phase != phase) continue;
                
                // 创建效果配置副本并设置目标
                var configCopy = CreateEffectConfigCopy(effectConfig);
                configCopy.Target = charObj;
                
                // 添加延迟
                var originalDelay = configCopy.Delay;
                configCopy.Delay = originalDelay + delay;
                
                // 创建并启动效果
                var effect = CreateEffectByType(configCopy);
                if (effect != null)
                {
                    _activeEffects.Add(effect);
                    effect.PlayAsync().Forget();
                }
            }
        }
        
        /// <summary>
        /// 应用停留阶段效果
        /// </summary>
        protected virtual void ApplyStayEffects()
        {
            for (int i = 0; i < _characterObjects.Count; i++)
            {
                var charObj = _characterObjects[i];
                if (charObj != null && charObj.activeInHierarchy)
                {
                    ApplyEffectsByPhase(charObj, SubtitleEffectPhase.Stay);
                }
            }
        }
        
        /// <summary>
        /// 应用离场阶段效果
        /// </summary>
        protected virtual void ApplyExitEffects()
        {
            for (int i = 0; i < _characterObjects.Count; i++)
            {
                var charObj = _characterObjects[i];
                if (charObj != null && charObj.activeInHierarchy)
                {
                    ApplyEffectsByPhase(charObj, SubtitleEffectPhase.Exit);
                }
            }
        }
        
        /// <summary>
        /// 根据类型创建效果
        /// </summary>
        protected virtual ISubtitleEffect CreateEffectByType(SubtitleEffectConfig config)
        {
            return config.EffectType switch
            {
                "Blur" => _subtitleModule.CreateSubtitleEffect<BlurSubtitleEffect>(config),
                "Fade" => _subtitleModule.CreateSubtitleEffect<FadeSubtitleEffect>(config),
                "Typewriter" => _subtitleModule.CreateSubtitleEffect<TypewriterSubtitleEffect>(config),
                "Scale" => _subtitleModule.CreateSubtitleEffect<ScaleSubtitleEffect>(config),
                _ => null
            };
        }
        
        /// <summary>
        /// 创建效果配置副本
        /// </summary>
        protected virtual SubtitleEffectConfig CreateEffectConfigCopy(SubtitleEffectConfig original)
        {
            var copy = new SubtitleEffectConfig
            {
                EffectType = original.EffectType,
                Duration = original.Duration,
                Delay = original.Delay,
                Phase = original.Phase,
                AnimationCurve = original.AnimationCurve,
                Parameters = new Dictionary<string, object>(original.Parameters)
            };
            return copy;
        }
        
        /// <summary>
        /// 执行淡出效果
        /// </summary>
        protected virtual async UniTask ExecuteFadeOut()
        {
            if (_config.FadeOutDuration <= 0) return;
            
            // 检查是否有自定义的离场效果
            bool hasCustomExitEffects = _config.Effects?.Any(e => e.Phase == SubtitleEffectPhase.Exit) ?? false;
            
            // 如果有自定义离场效果，则等待其完成；否则执行默认淡出
            if (hasCustomExitEffects)
            {
                // 等待自定义离场效果完成
                await UniTask.Delay((int)(_config.FadeOutDuration * 1000));
            }
            else
            {
                // 为所有字符对象添加默认淡出效果
                foreach (var charObj in _characterObjects)
                {
                    if (charObj != null && charObj.activeInHierarchy)
                    {
                        var fadeConfig = new FadeEffectConfig
                        {
                            Duration = _config.FadeOutDuration,
                            Target = charObj,
                            Phase = SubtitleEffectPhase.Exit
                        };
                        fadeConfig.SetParameter("AlphaStart", 1f);
                        fadeConfig.SetParameter("AlphaEnd", 0f);
                        
                        var fadeEffect = _subtitleModule.CreateSubtitleEffect<FadeSubtitleEffect>(fadeConfig);
                        if (fadeEffect != null)
                        {
                            _activeEffects.Add(fadeEffect);
                            fadeEffect.PlayAsync().Forget();
                        }
                    }
                }
                
                // 等待淡出完成
                await UniTask.Delay((int)(_config.FadeOutDuration * 1000));
            }
        }
        
        /// <summary>
        /// 执行具体的播放逻辑（由子类实现）
        /// </summary>
        protected abstract UniTask ExecutePlayLogic();
    }
}