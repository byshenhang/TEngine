using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// LRC歌词多效果播放示例
    /// 展示如何使用LRC解析器结合多种字幕效果
    /// </summary>
    public class LRCMultiEffectExample : MonoBehaviour
    {
        [Header("LRC播放设置")]
        [SerializeField] private string lrcContent = @"[ti:示例歌曲]
[ar:示例艺术家]
[al:示例专辑]
[00:10.00]第一行歌词
[00:15.00]第二行歌词
[00:20.00]第三行歌词
[00:25.00]第四行歌词";
        
        [SerializeField] private float displayDuration = 4f;
        [SerializeField] private bool enableEffects = true;
        
        [Header("控制按钮")]
        [SerializeField] private KeyCode playKey = KeyCode.P;
        [SerializeField] private KeyCode stopKey = KeyCode.S;
        [SerializeField] private KeyCode pauseKey = KeyCode.Space;
        
        private SubtitleModule _subtitleModule;
        private bool _isPlaying = false;
        
        private void Start()
        {
            // 获取字幕模块实例
            _subtitleModule = GameModule.Subtitle;
            
            if (_subtitleModule == null)
            {
                Log.Error("[LRCMultiEffectExample] 未找到SubtitleModule实例");
                return;
            }
            
            Log.Info("[LRCMultiEffectExample] LRC多效果示例已初始化");
            Log.Info($"[LRCMultiEffectExample] 按 {playKey} 播放LRC歌词");
            Log.Info($"[LRCMultiEffectExample] 按 {stopKey} 停止播放");
            Log.Info($"[LRCMultiEffectExample] 按 {pauseKey} 暂停/恢复播放");
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(playKey))
            {
                PlayLRCWithEffects();
            }
            
            if (Input.GetKeyDown(stopKey))
            {
                StopLRC();
            }
            
            if (Input.GetKeyDown(pauseKey))
            {
                TogglePause();
            }
        }
        
        /// <summary>
        /// 播放带效果的LRC歌词
        /// </summary>
        private async void PlayLRCWithEffects()
        {
            if (_isPlaying)
            {
                Log.Warning("[LRCMultiEffectExample] LRC正在播放中");
                return;
            }
            
            try
            {
                _isPlaying = true;
                Log.Info($"[LRCMultiEffectExample] 开始播放LRC歌词，效果启用: {enableEffects}");
                
                // 使用新的多效果播放方法
                await _subtitleModule.PlayLRCContentAsync(lrcContent, displayDuration, enableEffects);
                
                Log.Info("[LRCMultiEffectExample] LRC播放完成");
            }
            catch (Exception ex)
            {
                Log.Error($"[LRCMultiEffectExample] 播放LRC时发生错误: {ex.Message}");
            }
            finally
            {
                _isPlaying = false;
            }
        }
        
        /// <summary>
        /// 停止LRC播放
        /// </summary>
        private void StopLRC()
        {
            if (!_isPlaying)
            {
                Log.Warning("[LRCMultiEffectExample] 当前没有播放LRC");
                return;
            }
            
            _subtitleModule.StopTimedSubtitles();
            _isPlaying = false;
            Log.Info("[LRCMultiEffectExample] LRC播放已停止");
        }
        
        /// <summary>
        /// 切换暂停/恢复状态
        /// </summary>
        private void TogglePause()
        {
            if (!_isPlaying)
            {
                Log.Warning("[LRCMultiEffectExample] 当前没有播放LRC");
                return;
            }
            
            // 这里可以添加暂停/恢复逻辑
            // 注意：当前的PlayLRCContentAsync是基于UniTask.Delay的顺序播放
            // 如果需要真正的暂停/恢复功能，建议使用AddLRCToTimingController方法
            Log.Info("[LRCMultiEffectExample] 暂停/恢复功能需要使用TimingController模式");
        }
        
        /// <summary>
        /// 演示使用TimingController的LRC播放（支持暂停/恢复）
        /// </summary>
        [ContextMenu("演示TimingController模式")]
        public async void DemoTimingControllerMode()
        {
            try
            {
                Log.Info("[LRCMultiEffectExample] 演示TimingController模式");
                
                // 解析LRC内容
                var parseResult = LRCParser.ParseLRC(lrcContent);
                
                if (!parseResult.isValid)
                {
                    Log.Error($"[LRCMultiEffectExample] LRC解析失败: {parseResult.errorMessage}");
                    return;
                }
                
                // 添加到时序控制器
                _subtitleModule.AddLRCToTimingController(parseResult, displayDuration, "demo_lrc", enableEffects);
                
                // 开始播放
                _subtitleModule.PlayTimedSubtitles();
                
                Log.Info("[LRCMultiEffectExample] TimingController模式播放开始，现在可以使用暂停/恢复功能");
            }
            catch (Exception ex)
            {
                Log.Error($"[LRCMultiEffectExample] TimingController模式演示失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 演示自定义效果配置
        /// </summary>
        [ContextMenu("演示自定义效果")]
        public async void DemoCustomEffects()
        {
            try
            {
                Log.Info("[LRCMultiEffectExample] 演示自定义效果配置");
                
                // 解析LRC内容
                var parseResult = LRCParser.ParseLRC(lrcContent);
                
                if (!parseResult.isValid)
                {
                    Log.Error($"[LRCMultiEffectExample] LRC解析失败: {parseResult.errorMessage}");
                    return;
                }
                
                // 获取基础配置（不启用默认效果）
                var configs = LRCParser.ConvertToSubtitleConfigs(parseResult, displayDuration, false);
                
                // 为每个配置添加自定义效果
                foreach (var config in configs)
                {
                    // 自定义进场效果：打字机 + 模糊
                    config.Effects.Add(new TypewriterEffectConfig
                    {
                        Phase = SubtitleEffectPhase.Enter,
                        Duration = 1f,
                        Delay = 0f,
                        CharacterSpeed = 0.05f
                    });
                    
                    config.Effects.Add(new BlurEffectConfig
                    {
                        Phase = SubtitleEffectPhase.Enter,
                        Duration = 0.8f,
                        Delay = 0.2f,
                        BlurStart = 5f,
                        BlurEnd = 0f
                    });
                    
                    // 自定义离场效果：缩放 + 淡出
                    config.Effects.Add(new ScaleEffectConfig
                    {
                        Phase = SubtitleEffectPhase.Exit,
                        Duration = 0.6f,
                        Delay = 0f,
                        ScaleStart = Vector3.one,
                        ScaleEnd = new Vector3(1.2f, 1.2f, 1f)
                    });
                    
                    config.Effects.Add(new FadeEffectConfig
                    {
                        Phase = SubtitleEffectPhase.Exit,
                        Duration = 0.6f,
                        Delay = 0f,
                        AlphaStart = 1f,
                        AlphaEnd = 0f
                    });
                }
                
                // 逐个播放自定义效果的歌词
                for (int i = 0; i < parseResult.entries.Count; i++)
                {
                    var entry = parseResult.entries[i];
                    var config = configs[i];
                    
                    // 等待到指定时间
                    if (i == 0 && entry.timeInSeconds > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(entry.timeInSeconds));
                    }
                    else if (i > 0)
                    {
                        var prevEntry = parseResult.entries[i - 1];
                        float waitTime = entry.timeInSeconds - prevEntry.timeInSeconds;
                        if (waitTime > 0)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(waitTime));
                        }
                    }
                    
                    // 播放当前歌词
                    string sequenceId = $"custom_lrc_line_{i}";
                    await _subtitleModule.PlaySubtitleSequenceAsync(sequenceId, config);
                }
                
                Log.Info("[LRCMultiEffectExample] 自定义效果演示完成");
            }
            catch (Exception ex)
            {
                Log.Error($"[LRCMultiEffectExample] 自定义效果演示失败: {ex.Message}");
            }
        }
    }
}