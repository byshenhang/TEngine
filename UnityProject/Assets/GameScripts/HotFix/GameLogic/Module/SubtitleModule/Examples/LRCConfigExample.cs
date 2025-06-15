using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// LRC配置文件播放示例
    /// 展示如何使用SubtitleSequenceAsset配置文件来播放字幕
    /// </summary>
    public class LRCConfigExample : MonoBehaviour
    {
        [Header("配置资源")]
        [SerializeField] private SubtitleSequenceAsset _lyricConfig;
        
        [Header("测试LRC内容")]
        [TextArea(10, 20)]
        [SerializeField] private string _testLRCContent = @"[ti:Beautiful Music]
[ar:Test Artist]
[al:Test Album]
[00:00.50]♪ Beautiful music flows ♪
[00:03.00]Through the air tonight
[00:06.50]Stars are dancing in the sky
[00:10.00]Everything feels so right";
        
        private SubtitleModule _subtitleModule;
        
        private void Start()
        {
            _subtitleModule = SubtitleModule.Instance;
            
            // 如果没有配置资源，创建一个默认的
            if (_lyricConfig == null)
            {
                CreateDefaultConfig();
            }
        }
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        private void CreateDefaultConfig()
        {
            var asset = ScriptableObject.CreateInstance<SubtitleSequenceAsset>();
            asset.SequenceName = "歌词效果";
            asset.Description = "专门用于歌词显示的效果配置";
            asset.Version = "1.0";
            asset.Config = new SubtitleSequenceConfig
            {
                SequenceType = SubtitleSequenceType.Lyric,
                DisplayMode = SubtitleDisplayMode.WordByWord,
                Text = "♪ Beautiful music flows ♪",
                Position = new Vector3(0, -2, 0),
                FontSize = 28,
                TextColor = Color.magenta,
                StartDelay = 0.3f,
                WordInterval = 0.4f,
                HoldDuration = 4f,
                FadeOutDuration = 2f,
                Effects = new List<SubtitleEffectConfig>
                {
                    // 音乐节拍感的进场
                    new ScaleEffectConfig
                    {
                        Phase = SubtitleEffectPhase.Enter,
                        Duration = 0.3f,
                        Delay = 0f,
                        ScaleStart = Vector3.one * 0.8f,
                        ScaleEnd = Vector3.one,
                        Bounce = true,
                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                    },
                    new FadeEffectConfig
                    {
                        Phase = SubtitleEffectPhase.Enter,
                        Duration = 0.4f,
                        Delay = 0f,
                        AlphaStart = 0f,
                        AlphaEnd = 1f,
                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                    },
                    // 停留时的轻微律动
                    new ScaleEffectConfig
                    {
                        Phase = SubtitleEffectPhase.Stay,
                        Duration = 3f,
                        Delay = 0f,
                        ScaleStart = Vector3.one,
                        ScaleEnd = Vector3.one * 1.05f,
                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                    },
                    // 优雅的离场
                    new FadeEffectConfig
                    {
                        Phase = SubtitleEffectPhase.Exit,
                        Duration = 1.5f,
                        Delay = 0f,
                        AlphaStart = 1f,
                        AlphaEnd = 0f,
                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                    },
                    new BlurEffectConfig
                    {
                        Phase = SubtitleEffectPhase.Exit,
                        Duration = 1.8f,
                        Delay = 0.2f,
                        BlurStart = 0f,
                        BlurEnd = 15f,
                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                    }
                }
            };
            
            _lyricConfig = asset;
            Log.Info("[LRCConfigExample] 创建了默认配置");
        }
        
        /// <summary>
        /// 使用配置文件播放LRC
        /// </summary>
        [ContextMenu("播放配置文件LRC")]
        public async void PlayLRCWithConfig()
        {
            if (_lyricConfig == null)
            {
                Log.Error("[LRCConfigExample] 配置资源为空");
                return;
            }
            
            Log.Info("[LRCConfigExample] 开始使用配置文件播放LRC");
            await _subtitleModule.PlayLRCWithConfigAsync(_testLRCContent, _lyricConfig);
        }
        
        /// <summary>
        /// 使用默认效果播放LRC（对比）
        /// </summary>
        [ContextMenu("播放默认效果LRC")]
        public async void PlayLRCWithDefaultEffects()
        {
            Log.Info("[LRCConfigExample] 开始使用默认效果播放LRC");
            await _subtitleModule.PlayLRCContentAsync(_testLRCContent, 3f, true);
        }
        
        /// <summary>
        /// 停止所有字幕
        /// </summary>
        [ContextMenu("停止所有字幕")]
        public void StopAllSubtitles()
        {
            _subtitleModule?.StopAllSequences();
            Log.Info("[LRCConfigExample] 已停止所有字幕");
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            
            GUILayout.Label("LRC配置文件播放示例", GUI.skin.box);
            
            if (GUILayout.Button("播放配置文件LRC"))
            {
                PlayLRCWithConfig();
            }
            
            if (GUILayout.Button("播放默认效果LRC"))
            {
                PlayLRCWithDefaultEffects();
            }
            
            if (GUILayout.Button("停止所有字幕"))
            {
                StopAllSubtitles();
            }
            
            GUILayout.Space(10);
            GUILayout.Label($"配置: {(_lyricConfig != null ? _lyricConfig.SequenceName : "无")}");
            
            GUILayout.EndArea();
        }
    }
}