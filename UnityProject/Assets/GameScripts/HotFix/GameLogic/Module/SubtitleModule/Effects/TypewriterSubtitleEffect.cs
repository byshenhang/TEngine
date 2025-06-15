using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 打字机字幕效果
    /// </summary>
    public class TypewriterSubtitleEffect : ISubtitleEffect
    {
        private TypewriterEffectConfig _config;
        private GameObject _target;
        private TextMeshProUGUI _textComponent;
        private string _fullText;
        private bool _isCompleted;
        private bool _isRunning;
        private bool _isPaused;
        private float _currentTime;
        private int _currentCharIndex;
        
        public bool IsCompleted => _isCompleted;
        public bool IsRunning => _isRunning;
        
        /// <summary>
        /// 初始化效果
        /// </summary>
        public void Initialize(ISubtitleEffectConfig config)
        {
            _config = config as TypewriterEffectConfig;
            if (_config == null)
            {
                Log.Error("[TypewriterSubtitleEffect] 配置类型错误");
                return;
            }
            
            _target = _config.Target;
            if (_target == null)
            {
                Log.Error("[TypewriterSubtitleEffect] 目标对象为空");
                return;
            }
            
            // 获取或添加TextMeshProUGUI组件
            _textComponent = _target.GetComponent<TextMeshProUGUI>();
            if (_textComponent == null)
            {
                _textComponent = _target.AddComponent<TextMeshProUGUI>();
            }
            
            // 保存完整文本
            _fullText = _textComponent.text;
        }
        
        /// <summary>
        /// 播放效果
        /// </summary>
        public async UniTask PlayAsync()
        {
            if (_config == null || _target == null || _textComponent == null)
            {
                _isCompleted = true;
                return;
            }
            
            _isRunning = true;
            _isCompleted = false;
            _currentTime = 0f;
            _currentCharIndex = 0;
            
            // 等待延迟
            if (_config.Delay > 0)
            {
                await UniTask.Delay((int)(_config.Delay * 1000));
            }
            
            // 执行打字机效果
            await ExecuteTypewriterEffect();
            
            _isRunning = false;
            _isCompleted = true;
        }
        
        /// <summary>
        /// 执行打字机效果
        /// </summary>
        private async UniTask ExecuteTypewriterEffect()
        {
            float characterSpeed = _config.GetParameter("CharacterSpeed", 0.05f);
            bool randomSpeed = _config.GetParameter("RandomSpeed", false);
            float speedVariation = _config.GetParameter("SpeedVariation", 0.02f);
            
            // 清空文本
            _textComponent.text = "";
            
            while (_currentCharIndex < _fullText.Length && !_isCompleted)
            {
                if (!_isPaused)
                {
                    // 添加下一个字符
                    _currentCharIndex++;
                    _textComponent.text = _fullText.Substring(0, _currentCharIndex);
                    
                    // 计算等待时间
                    float waitTime = characterSpeed;
                    if (randomSpeed)
                    {
                        waitTime += Random.Range(-speedVariation, speedVariation);
                        waitTime = Mathf.Max(0.01f, waitTime); // 确保最小等待时间
                    }
                    
                    await UniTask.Delay((int)(waitTime * 1000));
                }
                else
                {
                    await UniTask.Yield();
                }
            }
            
            // 确保显示完整文本
            _textComponent.text = _fullText;
        }
        
        /// <summary>
        /// 停止效果
        /// </summary>
        public void Stop()
        {
            _isCompleted = true;
            _isRunning = false;
            
            // 立即显示完整文本
            if (_textComponent != null)
            {
                _textComponent.text = _fullText;
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
            _config = null;
            _target = null;
            _textComponent = null;
            _fullText = null;
        }
    }
}