using System.Collections;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using TEngine;

#if UIFX_AVAILABLE
using ChocDino.UIFX;
#endif

namespace GameLogic
{
    /// <summary>
    /// 模糊字幕效果 - 基于ChocDino.UIFX的BlurFilter实现
    /// </summary>
    public class BlurSubtitleEffect : ISubtitleEffect
    {
        private BlurEffectConfig _config;
        private GameObject _target;
        private TextMeshProUGUI _textComponent;
        private bool _isCompleted;
        private bool _isRunning;
        private bool _isPaused;
        private float _currentTime;
        
#if UIFX_AVAILABLE
        private BlurFilter _blurFilter;
#endif
        
        public bool IsCompleted => _isCompleted;
        public bool IsRunning => _isRunning;
        
        /// <summary>
        /// 初始化效果
        /// </summary>
        public void Initialize(ISubtitleEffectConfig config)
        {
            _config = config as BlurEffectConfig;
            if (_config == null)
            {
                Log.Error("[BlurSubtitleEffect] 配置类型错误");
                return;
            }
            
            _target = _config.Target;
            if (_target == null)
            {
                Log.Error("[BlurSubtitleEffect] 目标对象为空");
                return;
            }
            
            // 获取或添加TextMeshProUGUI组件
            _textComponent = _target.GetComponent<TextMeshProUGUI>();
            if (_textComponent == null)
            {
                _textComponent = _target.AddComponent<TextMeshProUGUI>();
            }
            
#if UIFX_AVAILABLE
            // 获取或添加BlurFilter组件
            _blurFilter = _target.GetComponent<BlurFilter>();
            if (_blurFilter == null)
            {
                _blurFilter = _target.AddComponent<BlurFilter>();
            }
#endif
        }
        
        /// <summary>
        /// 播放效果
        /// </summary>
        public async UniTask PlayAsync()
        {
            if (_config == null || _target == null)
            {
                _isCompleted = true;
                return;
            }
            
            _isRunning = true;
            _isCompleted = false;
            _currentTime = 0f;
            
            // 等待延迟
            if (_config.Delay > 0)
            {
                await UniTask.Delay((int)(_config.Delay * 1000));
            }
            
            // 执行模糊效果
            await ExecuteBlurEffect();
            
            _isRunning = false;
            _isCompleted = true;
        }
        
        /// <summary>
        /// 执行模糊效果
        /// </summary>
        private async UniTask ExecuteBlurEffect()
        {
#if UIFX_AVAILABLE
            if (_blurFilter == null) return;
            
            float blurStart = _config.GetParameter("BlurStart", 30f);
            float blurEnd = _config.GetParameter("BlurEnd", 0f);
            
            // 设置初始模糊值
            _blurFilter.Blur = blurStart;
            
            float elapsed = 0f;
            
            while (elapsed < _config.Duration && !_isCompleted)
            {
                if (!_isPaused)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _config.Duration);
                    
                    // 使用动画曲线计算模糊值
                    float curveValue = _config.AnimationCurve.Evaluate(t);
                    _blurFilter.Blur = Mathf.Lerp(blurStart, blurEnd, curveValue);
                }
                
                await UniTask.Yield();
            }
            
            // 确保最终值
            _blurFilter.Blur = blurEnd;
#else
            // 如果没有UIFX插件，使用替代效果（如透明度变化）
            await ExecuteFallbackEffect();
#endif
        }
        
        /// <summary>
        /// 备用效果（当UIFX不可用时）
        /// </summary>
        private async UniTask ExecuteFallbackEffect()
        {
            if (_textComponent == null) return;
            
            Color originalColor = _textComponent.color;
            float elapsed = 0f;
            
            while (elapsed < _config.Duration && !_isCompleted)
            {
                if (!_isPaused)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _config.Duration);
                    
                    // 使用动画曲线计算透明度
                    float curveValue = _config.AnimationCurve.Evaluate(t);
                    float alpha = Mathf.Lerp(0f, 1f, curveValue);
                    
                    Color newColor = originalColor;
                    newColor.a = alpha;
                    _textComponent.color = newColor;
                }
                
                await UniTask.Yield();
            }
            
            // 确保最终透明度
            Color finalColor = originalColor;
            finalColor.a = 1f;
            _textComponent.color = finalColor;
        }
        
        /// <summary>
        /// 停止效果
        /// </summary>
        public void Stop()
        {
            _isCompleted = true;
            _isRunning = false;
        }
        
        /// <summary>
        /// 暂停效果
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }
        
        /// <summary>
        /// 恢复效果
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
        }
        
        /// <summary>
        /// 更新效果
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isRunning || _isPaused) return;
            
            _currentTime += deltaTime;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release()
        {
            _config = null;
            _target = null;
            _textComponent = null;
#if UIFX_AVAILABLE
            _blurFilter = null;
#endif
        }
    }
}