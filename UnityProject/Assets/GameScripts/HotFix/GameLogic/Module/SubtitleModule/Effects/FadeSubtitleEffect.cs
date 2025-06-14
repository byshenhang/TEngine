using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 淡入淡出字幕效果
    /// </summary>
    public class FadeSubtitleEffect : ISubtitleEffect
    {
        private FadeEffectConfig _config;
        private GameObject _target;
        private TextMeshProUGUI _textComponent;
        private CanvasGroup _canvasGroup;
        private bool _isCompleted;
        private bool _isRunning;
        private bool _isPaused;
        private float _currentTime;
        
        public bool IsCompleted => _isCompleted;
        public bool IsRunning => _isRunning;
        
        /// <summary>
        /// 初始化效果
        /// </summary>
        public void Initialize(ISubtitleEffectConfig config)
        {
            _config = config as FadeEffectConfig;
            if (_config == null)
            {
                Log.Error("[FadeSubtitleEffect] 配置类型错误");
                return;
            }
            
            _target = _config.Target;
            if (_target == null)
            {
                Log.Error("[FadeSubtitleEffect] 目标对象为空");
                return;
            }
            
            // 获取或添加组件
            _textComponent = _target.GetComponent<TextMeshProUGUI>();
            _canvasGroup = _target.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _target.AddComponent<CanvasGroup>();
            }
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
            
            // 执行淡入淡出效果
            await ExecuteFadeEffect();
            
            _isRunning = false;
            _isCompleted = true;
        }
        
        /// <summary>
        /// 执行淡入淡出效果
        /// </summary>
        private async UniTask ExecuteFadeEffect()
        {
            float alphaStart = _config.GetParameter("AlphaStart", 0f);
            float alphaEnd = _config.GetParameter("AlphaEnd", 1f);
            
            // 设置初始透明度
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alphaStart;
            }
            else if (_textComponent != null)
            {
                Color color = _textComponent.color;
                color.a = alphaStart;
                _textComponent.color = color;
            }
            
            float elapsed = 0f;
            
            while (elapsed < _config.Duration && !_isCompleted)
            {
                if (!_isPaused)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _config.Duration);
                    
                    // 使用动画曲线计算透明度
                    float curveValue = _config.AnimationCurve.Evaluate(t);
                    float alpha = Mathf.Lerp(alphaStart, alphaEnd, curveValue);
                    
                    // 应用透明度
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = alpha;
                    }
                    else if (_textComponent != null)
                    {
                        Color color = _textComponent.color;
                        color.a = alpha;
                        _textComponent.color = color;
                    }
                }
                
                await UniTask.Yield();
            }
            
            // 确保最终透明度
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alphaEnd;
            }
            else if (_textComponent != null)
            {
                Color color = _textComponent.color;
                color.a = alphaEnd;
                _textComponent.color = color;
            }
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
            _canvasGroup = null;
        }
    }
    
    /// <summary>
    /// 淡入淡出效果工厂
    /// </summary>
    public class FadeSubtitleEffectFactory : ISubtitleEffectFactory
    {
        public ISubtitleEffect CreateEffect(ISubtitleEffectConfig config)
        {
            var effect = new FadeSubtitleEffect();
            effect.Initialize(config);
            return effect;
        }
    }
}