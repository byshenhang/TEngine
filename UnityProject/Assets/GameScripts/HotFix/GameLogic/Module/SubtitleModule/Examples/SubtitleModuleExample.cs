using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 字幕模块使用示例
    /// </summary>
    public class SubtitleModuleExample : MonoBehaviour
    {
        [Header("字幕配置")]
        public Transform subtitleParent;
        public string exampleText = "I saw you";
        public float startDelay = 1f;
        
        [Header("模糊效果配置")]
        public float blurStart = 30f;
        public float blurThreshold = 10f;
        public float blurFadeDuration = 1f;
        public AnimationCurve blurCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("其他效果配置")]
        public float finalFadeDuration = 0.5f;
        public float scaleAmount = 1.2f;
        public float typewriterSpeed = 0.1f;
        
        private SubtitleModule _subtitleModule;
        
        private void Start()
        {
            InitializeSubtitleModule();
        }
        
        /// <summary>
        /// 初始化字幕模块
        /// </summary>
        private void InitializeSubtitleModule()
        {
            _subtitleModule = SubtitleModule.Instance;
            if (_subtitleModule == null)
            {
                Log.Error("[SubtitleModuleExample] 未找到字幕模块");
                return;
            }
            
            Log.Info("[SubtitleModuleExample] 字幕模块初始化完成");
        }
        
        /// <summary>
        /// 播放简单字幕示例
        /// </summary>
        [ContextMenu("播放简单字幕")]
        public async void PlaySimpleSubtitle()
        {
            if (_subtitleModule == null)
            {
                Log.Error("[SubtitleModuleExample] 字幕模块未初始化");
                return;
            }
            
            var config = new SubtitleSequenceConfig
            {
                Text = "Hello World!",
                DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
                CharacterInterval = 0.1f
            };
            
            // 添加淡入效果
            config.Effects.Add(new FadeEffectConfig
            {
                EffectType = "Fade",
                Duration = 0.5f,
                Delay = 0f,
                FadeIn = true,
                AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            });
            
            await _subtitleModule.PlaySubtitleSequenceAsync("simple_subtitle", config);
            Log.Info("[SubtitleModuleExample] 简单字幕播放完成");
        }
        
        /// <summary>
        /// 播放复杂模糊字幕示例（模拟原始代码效果）
        /// </summary>
        [ContextMenu("播放模糊字幕")]
        public async void PlayBlurSubtitle()
        {
            if (_subtitleModule == null)
            {
                Log.Error("[SubtitleModuleExample] 字幕模块未初始化");
                return;
            }
            
            var config = new SubtitleSequenceConfig
            {
                Text = exampleText,
                DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
                CharacterInterval = 0.2f
            };
            
            // 添加模糊效果
            config.Effects.Add(new BlurEffectConfig
            {
                EffectType = "Blur",
                Duration = blurFadeDuration,
                Delay = 0f,
                BlurStart = blurStart,
                BlurEnd = 0f,
                BlurThreshold = blurThreshold,
                AnimationCurve = blurCurve
            });
            
            // 添加最终淡出效果
            config.Effects.Add(new FadeEffectConfig
            {
                EffectType = "Fade",
                Duration = finalFadeDuration,
                Delay = blurFadeDuration + 0.5f, // 在模糊效果完成后延迟执行
                FadeIn = false,
                AnimationCurve = AnimationCurve.Linear(0, 0, 1, 1)
            });
            
            await _subtitleModule.PlaySubtitleSequenceAsync("blur_subtitle", config);
            Log.Info("[SubtitleModuleExample] 模糊字幕播放完成");
        }
        
        /// <summary>
        /// 播放多效果组合字幕
        /// </summary>
        [ContextMenu("播放多效果字幕")]
        public async void PlayMultiEffectSubtitle()
        {
            if (_subtitleModule == null)
            {
                Log.Error("[SubtitleModuleExample] 字幕模块未初始化");
                return;
            }
            
            var config = new SubtitleSequenceConfig
            {
                Text = "Amazing Effects!",
                DisplayMode = SubtitleDisplayMode.WordByWord,
                WordInterval = 0.3f
            };
            
            // 添加缩放效果
            config.Effects.Add(new ScaleEffectConfig
            {
                Duration = 0.5f,
                Delay = 0f,
                ScaleStart = Vector3.zero,
                ScaleEnd = Vector3.one * scaleAmount,
                AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                Bounce = true
            });
            
            // 添加打字机效果
            config.Effects.Add(new TypewriterEffectConfig
            {
                Duration = 1f,
                Delay = 0.2f,
                CharacterSpeed = typewriterSpeed,
                RandomSpeed = true,
                SpeedVariation = 0.05f
            });
            
            // 添加淡入效果
            config.Effects.Add(new FadeEffectConfig
            {
                Duration = 0.3f,
                Delay = 0f,
                FadeIn = true,
                AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            });
            
            await _subtitleModule.PlaySubtitleSequenceAsync("multi_effect_subtitle", config);
            Log.Info("[SubtitleModuleExample] 多效果字幕播放完成");
        }
        
        /// <summary>
        /// 播放定时字幕序列
        /// </summary>
        [ContextMenu("播放定时字幕序列")]
        public async void PlayTimedSubtitleSequence()
        {
            if (_subtitleModule == null)
            {
                Log.Error("[SubtitleModuleExample] 字幕模块未初始化");
                return;
            }
            
            // 创建多个字幕序列
            var subtitles = new List<(string text, float startTime, float duration)>
            {
                ("First subtitle", 0f, 2f),
                ("Second subtitle", 2.5f, 2f),
                ("Third subtitle", 5f, 2f),
                ("Final subtitle", 8f, 3f)
            };
            
            foreach (var (text, startTime, duration) in subtitles)
            {
                var config = new SubtitleSequenceConfig
                {
                    Text = text,
                    DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
                    CharacterInterval = 0.05f
                };
                
                // 添加淡入淡出效果
                config.Effects.Add(new FadeEffectConfig
                {
                    Duration = 0.5f,
                    Delay = 0f,
                    FadeIn = true
                });
                
                config.Effects.Add(new FadeEffectConfig
                {
                    Duration = 0.5f,
                    Delay = duration - 0.5f,
                    FadeIn = false
                });
                
                await _subtitleModule.PlaySubtitleSequenceAsync($"timed_subtitle_{startTime}", config);
            }
            
            Log.Info("[SubtitleModuleExample] 定时字幕序列开始播放");
        }
        
        /// <summary>
        /// 停止所有字幕
        /// </summary>
        [ContextMenu("停止所有字幕")]
        public void StopAllSubtitles()
        {
            if (_subtitleModule != null)
            {
                _subtitleModule.StopSubtitleSequence("simple_subtitle");
                _subtitleModule.StopSubtitleSequence("blur_subtitle");
                _subtitleModule.StopSubtitleSequence("multi_effect_subtitle");
                Log.Info("[SubtitleModuleExample] 停止所有字幕");
            }
        }
        
        /// <summary>
        /// 暂停字幕播放
        /// </summary>
        [ContextMenu("暂停字幕")]
        public void PauseSubtitles()
        {
            // 注意：当前SubtitleModule API不支持暂停/恢复功能
            // 可以通过停止序列来实现类似效果
            Log.Info("[SubtitleModuleExample] 暂停功能需要扩展API支持");
        }
        
        /// <summary>
        /// 恢复字幕播放
        /// </summary>
        [ContextMenu("恢复字幕")]
        public void ResumeSubtitles()
        {
            // 注意：当前SubtitleModule API不支持暂停/恢复功能
            Log.Info("[SubtitleModuleExample] 恢复功能需要扩展API支持");
        }
        
        /// <summary>
        /// 测试自定义效果
        /// </summary>
        [ContextMenu("测试自定义效果")]
        public async void TestCustomEffect()
        {
            if (_subtitleModule == null)
            {
                Log.Error("[SubtitleModuleExample] 字幕模块未初始化");
                return;
            }
            
            // 注册自定义效果工厂
            _subtitleModule.RegisterEffectFactory<CustomSubtitleEffect>(new CustomEffectFactory());
            
            var config = new SubtitleSequenceConfig
            {
                Text = "Custom Effect!",
                DisplayMode = SubtitleDisplayMode.All
            };
            
            // 添加自定义效果
            config.Effects.Add(new CustomEffectConfig
            {
                EffectType = "Custom",
                Duration = 2f,
                Delay = 0f,
                CustomParameter = "test_value"
            });
            
            await _subtitleModule.PlaySubtitleSequenceAsync("custom_effect_subtitle", config);
            Log.Info("[SubtitleModuleExample] 自定义效果测试完成");
        }
        
        private void OnDestroy()
        {
            // 清理资源
            if (_subtitleModule != null)
            {
                _subtitleModule.StopSubtitleSequence("simple_subtitle");
                _subtitleModule.StopSubtitleSequence("blur_subtitle");
                _subtitleModule.StopSubtitleSequence("multi_effect_subtitle");
                _subtitleModule.StopSubtitleSequence("custom_effect_subtitle");
            }
        }
    }
    
    /// <summary>
    /// 自定义效果配置示例
    /// </summary>
    [System.Serializable]
    public class CustomEffectConfig : SubtitleEffectConfig
    {
        public string CustomParameter { get; set; }
        
        public CustomEffectConfig()
        {
            EffectType = "Custom";
        }
        
        public new T GetParameter<T>(string paramName, T defaultValue = default)
        {
            if (paramName == "CustomParameter")
                return (T)(object)CustomParameter;
            
            return base.GetParameter(paramName, defaultValue);
        }
    }
    
    /// <summary>
    /// 自定义效果工厂示例
    /// </summary>
    public class CustomEffectFactory : ISubtitleEffectFactory
    {
        public ISubtitleEffect CreateEffect(ISubtitleEffectConfig config)
        {
            var effect = new CustomSubtitleEffect();
            effect.Initialize(config);
            return effect;
        }
    }
    
    /// <summary>
    /// 自定义字幕效果示例
    /// </summary>
    public class CustomSubtitleEffect : ISubtitleEffect
    {
        private ISubtitleEffectConfig _config;
        private bool _isCompleted;
        private bool _isRunning;
        private float _elapsedTime;
        
        public bool IsCompleted => _isCompleted;
        public bool IsRunning => _isRunning;
        
        public void Initialize(ISubtitleEffectConfig config)
        {
            _config = config;
            _isCompleted = false;
            _isRunning = false;
            _elapsedTime = 0f;
        }
        
        public async UniTask PlayAsync()
        {
            _isRunning = true;
            
            // 获取自定义参数
            string customParam = _config.GetParameter<string>("CustomParameter", "默认值");
            
            Log.Info($"[CustomSubtitleEffect] 开始执行自定义效果，参数: {customParam}");
            
            // 简单的效果实现
            while (_elapsedTime < _config.Duration && _isRunning)
            {
                await UniTask.Yield();
                // 效果逻辑在Update中处理
            }
            
            _isCompleted = true;
            _isRunning = false;
        }
        
        public void Stop()
        {
            _isRunning = false;
            _isCompleted = true;
        }
        
        public void Pause()
        {
            _isRunning = false;
        }
        
        public void Resume()
        {
            if (!_isCompleted)
            {
                _isRunning = true;
            }
        }
        
        public void Update(float deltaTime)
        {
            if (!_isRunning || _isCompleted)
                return;
                
            _elapsedTime += deltaTime;
            
            // 简单的效果逻辑
            if (_elapsedTime >= _config.Duration)
            {
                _isCompleted = true;
                _isRunning = false;
            }
        }
        
        public void Release()
        {
            _isCompleted = true;
            _isRunning = false;
            _config = null;
        }
    }
}