using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 歌词字幕序列
    /// 专门用于处理歌词显示，支持逐行显示和高亮当前行
    /// </summary>
    public class LyricSubtitleSequence : ISubtitleSequence
    {
        private readonly SubtitleSequenceConfig _config;
        private readonly SubtitleModule _subtitleModule;
        private readonly List<ISubtitleEffect> _activeEffects = new();
        private readonly List<GameObject> _lyricLines = new();
        
        private bool _isCompleted;
        private bool _isRunning;
        private bool _isPaused;
        private float _currentTime;
        private int _currentLineIndex = -1;
        
        public bool IsCompleted => _isCompleted;
        public bool IsRunning => _isRunning;
        
        /// <summary>
        /// 暂停歌词序列
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
            
            // 暂停所有活动效果
            foreach (var effect in _activeEffects)
            {
                effect.Pause();
            }
        }
        
        /// <summary>
        /// 恢复歌词序列
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
            
            // 恢复所有活动效果
            foreach (var effect in _activeEffects)
            {
                effect.Resume();
            }
        }
        
        public LyricSubtitleSequence(SubtitleSequenceConfig config, SubtitleModule subtitleModule)
        {
            _config = config;
            _subtitleModule = subtitleModule;
        }
        
        public async UniTask PlayAsync()
        {
            if (_isRunning)
                return;
                
            _isRunning = true;
            _isCompleted = false;
            _currentTime = 0f;
            _currentLineIndex = -1;
            
            Log.Info($"[LyricSubtitleSequence] 开始播放歌词序列: {_config.Text}");
            
            try
            {
                await CreateLyricLines();
                await PlayLyricSequence();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[LyricSubtitleSequence] 播放歌词序列时发生错误: {ex.Message}");
            }
            finally
            {
                _isCompleted = true;
                _isRunning = false;
            }
        }
        
        public void Stop()
        {
            if (!_isRunning)
                return;
                
            Log.Info($"[LyricSubtitleSequence] 停止歌词序列");
            
            // 停止所有效果
            foreach (var effect in _activeEffects)
            {
                effect.Stop();
            }
            
            // 清理歌词行
            foreach (var line in _lyricLines)
            {
                if (line != null)
                {
                    Object.Destroy(line);
                }
            }
            _lyricLines.Clear();
            
            _isRunning = false;
            _isCompleted = true;
        }
        
        public void Update(float deltaTime)
        {
            if (!_isRunning || _isCompleted)
                return;
                
            _currentTime += deltaTime;
            
            // 更新所有活动效果
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
        
        public void Release()
        {
            Stop();
            
            // 释放所有效果
            foreach (var effect in _activeEffects)
            {
                effect.Release();
            }
            _activeEffects.Clear();
        }
        
        /// <summary>
        /// 创建歌词行
        /// </summary>
        private async UniTask CreateLyricLines()
        {
            var lines = _config.Text.Split('\n');
            var startY = (lines.Length - 1) * 30f / 2f; // 居中显示
            
            for (int i = 0; i < lines.Length; i++)
            {
                var lineObj = new GameObject($"LyricLine_{i}");
                lineObj.transform.SetParent(_subtitleModule.SubtitleRoot);
                
                // 设置位置
                lineObj.transform.localPosition = new Vector3(0, startY - i * 30f, 0);
                
                // 添加TextMeshProUGUI组件
                var textComponent = lineObj.AddComponent<TMPro.TextMeshProUGUI>();
                textComponent.text = lines[i];
                textComponent.fontSize = _config.FontSize;
                textComponent.color = new Color(_config.TextColor.r, _config.TextColor.g, _config.TextColor.b, 1f); // 初始可见
                textComponent.alignment = TMPro.TextAlignmentOptions.Center;
                
                // 设置字体
                if (!string.IsNullOrEmpty(_config.FontPath))
                {
                    // 使用默认字体或通过其他方式加载字体
                    // 这里可以扩展为通过SubtitleModule的资源加载器加载字体
                    Log.Info($"[LyricSubtitleSequence] 需要加载字体: {_config.FontPath}");
                }
                
                _lyricLines.Add(lineObj);
            }
        }
        
        /// <summary>
        /// 播放歌词序列
        /// </summary>
        private async UniTask PlayLyricSequence()
        {
            var lineDuration = _config.Duration / _lyricLines.Count;
            
            for (int i = 0; i < _lyricLines.Count; i++)
            {
                if (!_isRunning)
                    break;
                    
                _currentLineIndex = i;
                
                // 高亮当前行
                await HighlightLine(i);
                
                // 等待行持续时间
                await UniTask.Delay((int)(lineDuration * 1000));
                
                // 淡出当前行
                await FadeOutLine(i);
            }
        }
        
        /// <summary>
        /// 高亮指定行
        /// </summary>
        private async UniTask HighlightLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _lyricLines.Count)
                return;
                
            var lineObj = _lyricLines[lineIndex];
            var textComponent = lineObj.GetComponent<TMPro.TextMeshProUGUI>();
            
            // 创建淡入效果
            var fadeConfig = new FadeEffectConfig
            {
                Duration = 0.5f,
                AlphaStart = 0f,
                AlphaEnd = 1f,
                Target = lineObj
            };
            
            var fadeEffect = _subtitleModule.CreateEffect(fadeConfig);
            if (fadeEffect != null)
            {
                _activeEffects.Add(fadeEffect);
                await fadeEffect.PlayAsync();
            }
        }
        
        /// <summary>
        /// 淡出指定行
        /// </summary>
        private async UniTask FadeOutLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _lyricLines.Count)
                return;
                
            var lineObj = _lyricLines[lineIndex];
            
            // 创建淡出效果
            var fadeConfig = new FadeEffectConfig
            {
                Duration = 0.3f,
                AlphaStart = 1f,
                AlphaEnd = 0.3f, // 保持一点透明度
                Target = lineObj
            };
            
            var fadeEffect = _subtitleModule.CreateEffect(fadeConfig);
            if (fadeEffect != null)
            {
                _activeEffects.Add(fadeEffect);
                await fadeEffect.PlayAsync();
            }
        }
    }
}