using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 缩放字幕效果
    /// </summary>
    public class ScaleSubtitleEffect : ISubtitleEffect
    {
        private ScaleEffectConfig _config;
        private GameObject _target;
        private Transform _transform;
        private Vector3 _originalScale;
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
            _config = config as ScaleEffectConfig;
            if (_config == null)
            {
                Log.Error("[ScaleSubtitleEffect] 配置类型错误");
                return;
            }
            
            _target = _config.Target;
            if (_target == null)
            {
                Log.Error("[ScaleSubtitleEffect] 目标对象为空");
                return;
            }
            
            _transform = _target.transform;
            _originalScale = _transform.localScale;
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
            
            // 执行缩放效果
            await ExecuteScaleEffect();
            
            _isRunning = false;
            _isCompleted = true;
        }
        
        /// <summary>
        /// 执行缩放效果
        /// </summary>
        private async UniTask ExecuteScaleEffect()
        {
            Vector3 scaleStart = _config.GetParameter("ScaleStart", Vector3.zero);
            Vector3 scaleEnd = _config.GetParameter("ScaleEnd", Vector3.one);
            bool bounce = _config.GetParameter("Bounce", false);
            
            // 设置初始缩放
            _transform.localScale = scaleStart;
            
            float elapsed = 0f;
            
            while (elapsed < _config.Duration && !_isCompleted)
            {
                if (!_isPaused)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _config.Duration);
                    
                    // 使用动画曲线计算缩放值
                    float curveValue = _config.AnimationCurve.Evaluate(t);
                    
                    Vector3 currentScale;
                    if (bounce && curveValue > 0.5f)
                    {
                        // 弹跳效果：先放大再回到目标大小
                        float bounceT = (curveValue - 0.5f) * 2f;
                        Vector3 bounceScale = Vector3.Lerp(scaleEnd, scaleEnd * 1.2f, Mathf.Sin(bounceT * Mathf.PI));
                        currentScale = Vector3.Lerp(scaleStart, bounceScale, curveValue);
                    }
                    else
                    {
                        currentScale = Vector3.Lerp(scaleStart, scaleEnd, curveValue);
                    }
                    
                    _transform.localScale = currentScale;
                }
                
                await UniTask.Yield();
            }
            
            // 确保最终缩放
            _transform.localScale = scaleEnd;
        }
        
        /// <summary>
        /// 停止效果
        /// </summary>
        public void Stop()
        {
            _isCompleted = true;
            _isRunning = false;
            
            // 恢复到目标缩放
            if (_transform != null)
            {
                Vector3 scaleEnd = _config.GetParameter("ScaleEnd", Vector3.one);
                _transform.localScale = scaleEnd;
            }
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
            // 恢复原始缩放
            if (_transform != null)
            {
                _transform.localScale = _originalScale;
            }
            
            _config = null;
            _target = null;
            _transform = null;
        }
    }
}