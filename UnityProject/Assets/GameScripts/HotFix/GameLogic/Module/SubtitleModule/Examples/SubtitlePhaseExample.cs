//using System.Collections.Generic;
//using UnityEngine;
//using Cysharp.Threading.Tasks;
//using TEngine;

//namespace GameLogic
//{
//    /// <summary>
//    /// 字幕效果阶段示例
//    /// 展示如何使用进场、停留、离场三个阶段的效果
//    /// </summary>
//    public class SubtitlePhaseExample : MonoBehaviour
//    {
//        [Header("字幕模块")]
//        public SubtitleModule _subtitleModule;
        
//        [Header("UI 容器")]
//        public Transform _uiContainer;
        
//        private void Start()
//        {
//            // 如果没有指定字幕模块，尝试获取
//            if (_subtitleModule == null)
//            {
//                _subtitleModule = GameModule.GetModule<SubtitleModule>();
//            }
//        }
        
//        /// <summary>
//        /// 播放完整阶段效果示例
//        /// </summary>
//        public async void PlayCompletePhaseExample()
//        {
//            var config = new SubtitleSequenceConfig
//            {
//                SequenceType = SubtitleSequenceType.Complex,
//                DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
//                Text = "完整阶段效果演示",
//                Position = new Vector3(0, 0, 0),
//                FontSize = 36,
//                TextColor = Color.white,
                
//                // 时序配置
//                StartDelay = 0.5f,
//                CharacterInterval = 0.1f,
//                HoldDuration = 3f,
//                FadeOutDuration = 1f,
                
//                // 效果配置 - 三个阶段的效果
//                Effects = new List<SubtitleEffectConfig>
//                {
//                    // 进场阶段：缩放 + 模糊
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 0.8f,
//                        Delay = 0f,
//                        ScaleStart = Vector3.zero,
//                        ScaleEnd = Vector3.one,
//                        Bounce = true
//                    },
//                    new BlurEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 1f,
//                        Delay = 0.2f,
//                        BlurStart = 20f,
//                        BlurEnd = 0f
//                    },
                    
//                    // 停留阶段：轻微缩放呼吸效果
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Stay,
//                        Duration = 2f,
//                        Delay = 0f,
//                        ScaleStart = Vector3.one,
//                        ScaleEnd = Vector3.one * 1.1f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
                    
//                    // 离场阶段：淡出 + 向上移动
//                    new FadeEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1f,
//                        Delay = 0f,
//                        AlphaStart = 1f,
//                        AlphaEnd = 0f
//                    },
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1f,
//                        Delay = 0f,
//                        ScaleStart = Vector3.one,
//                        ScaleEnd = Vector3.one * 0.5f
//                    }
//                }
//            };
            
//            try
//            {
//                await _subtitleModule.PlaySubtitleSequenceAsync("phase_example", config);
//                Log.Info("[SubtitlePhaseExample] 完整阶段效果播放完成");
//            }
//            catch (System.Exception e)
//            {
//                Log.Error($"[SubtitlePhaseExample] 播放失败: {e.Message}");
//            }
//        }
        
//        /// <summary>
//        /// 播放进场效果示例
//        /// </summary>
//        public async void PlayEnterEffectExample()
//        {
//            var config = new SubtitleSequenceConfig
//            {
//                SequenceType = SubtitleSequenceType.Simple,
//                DisplayMode = SubtitleDisplayMode.WordByWord,
//                Text = "华丽的进场效果",
//                Position = new Vector3(0, 100, 0),
//                FontSize = 32,
//                TextColor = Color.cyan,
                
//                StartDelay = 0.2f,
//                WordInterval = 0.3f,
//                HoldDuration = 2f,
                
//                Effects = new List<SubtitleEffectConfig>
//                {
//                    // 只有进场效果
//                    new BlurEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 1.2f,
//                        BlurStart = 25f,
//                        BlurEnd = 0f
//                    },
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 0.8f,
//                        Delay = 0.3f,
//                        ScaleStart = Vector3.one * 2f,
//                        ScaleEnd = Vector3.one,
//                        Bounce = true
//                    }
//                }
//            };
            
//            await _subtitleModule.PlaySubtitleSequenceAsync("enter_example", config);
//        }
        
//        /// <summary>
//        /// 播放停留效果示例
//        /// </summary>
//        public async void PlayStayEffectExample()
//        {
//            var config = new SubtitleSequenceConfig
//            {
//                SequenceType = SubtitleSequenceType.Complex,
//                DisplayMode = SubtitleDisplayMode.All,
//                Text = "持续闪烁的文字",
//                Position = new Vector3(0, -100, 0),
//                FontSize = 28,
//                TextColor = Color.yellow,
                
//                StartDelay = 0f,
//                HoldDuration = 4f,
                
//                Effects = new List<SubtitleEffectConfig>
//                {
//                    // 只有停留效果 - 闪烁
//                    new FadeEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Stay,
//                        Duration = 0.5f,
//                        AlphaStart = 1f,
//                        AlphaEnd = 0.3f,
//                        AnimationCurve = AnimationCurve.Linear(0, 0, 1, 1)
//                    }
//                }
//            };
            
//            await _subtitleModule.PlaySubtitleSequenceAsync("stay_example", config);
//        }
        
//        /// <summary>
//        /// 播放离场效果示例
//        /// </summary>
//        public async void PlayExitEffectExample()
//        {
//            var config = new SubtitleSequenceConfig
//            {
//                SequenceType = SubtitleSequenceType.Simple,
//                DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
//                Text = "优雅的离场",
//                Position = new Vector3(0, 0, 0),
//                FontSize = 30,
//                TextColor = Color.magenta,
                
//                StartDelay = 0f,
//                CharacterInterval = 0.05f,
//                HoldDuration = 1.5f,
//                FadeOutDuration = 2f,
                
//                Effects = new List<SubtitleEffectConfig>
//                {
//                    // 只有离场效果
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1.5f,
//                        ScaleStart = Vector3.one,
//                        ScaleEnd = Vector3.zero,
//                        AnimationCurve = AnimationCurve.EaseIn(0, 0, 1, 1)
//                    },
//                    new BlurEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1.8f,
//                        Delay = 0.2f,
//                        BlurStart = 0f,
//                        BlurEnd = 30f
//                    }
//                }
//            };
            
//            await _subtitleModule.PlaySubtitleSequenceAsync("exit_example", config);
//        }
        
//        /// <summary>
//        /// 播放歌词效果示例
//        /// </summary>
//        public async void PlayLyricPhaseExample()
//        {
//            var config = new SubtitleSequenceConfig
//            {
//                SequenceType = SubtitleSequenceType.Complex,
//                DisplayMode = SubtitleDisplayMode.WordByWord,
//                Text = "♪ 这是一句美妙的歌词 ♪",
//                Position = new Vector3(0, 200, 0),
//                FontSize = 34,
//                TextColor = Color.white,
                
//                StartDelay = 0.5f,
//                WordInterval = 0.4f,
//                HoldDuration = 3f,
//                FadeOutDuration = 2f,
                
//                Effects = new List<SubtitleEffectConfig>
//                {
//                    // 进场：打字机 + 缩放
//                    new TypewriterEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 2f,
//                        CharacterSpeed = 0.08f,
//                        RandomSpeed = true,
//                        SpeedVariation = 0.03f
//                    },
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 0.6f,
//                        Delay = 0.1f,
//                        ScaleStart = Vector3.one * 0.8f,
//                        ScaleEnd = Vector3.one,
//                        Bounce = true
//                    },
                    
//                    // 停留：轻微发光效果（通过缩放模拟）
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Stay,
//                        Duration = 1.5f,
//                        ScaleStart = Vector3.one,
//                        ScaleEnd = Vector3.one * 1.05f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
                    
//                    // 离场：模糊消失
//                    new BlurEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1.5f,
//                        BlurStart = 0f,
//                        BlurEnd = 25f
//                    },
//                    new FadeEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1.8f,
//                        Delay = 0.3f,
//                        AlphaStart = 1f,
//                        AlphaEnd = 0f
//                    }
//                }
//            };
            
//            await _subtitleModule.PlaySubtitleSequenceAsync("lyric_example", config);
//        }
        
//        /// <summary>
//        /// 停止所有字幕
//        /// </summary>
//        public void StopAllSubtitles()
//        {
//            _subtitleModule?.StopAllSubtitles();
//            Log.Info("[SubtitlePhaseExample] 已停止所有字幕");
//        }
        
//        /// <summary>
//        /// 测试所有阶段效果
//        /// </summary>
//        public async void TestAllPhases()
//        {
//            Log.Info("[SubtitlePhaseExample] 开始测试所有阶段效果");
            
//            // 依次播放不同的效果示例
//            await PlayEnterEffectExample();
//            await UniTask.Delay(3000);
            
//            await PlayStayEffectExample();
//            await UniTask.Delay(5000);
            
//            await PlayExitEffectExample();
//            await UniTask.Delay(4000);
            
//            await PlayCompletePhaseExample();
//            await UniTask.Delay(6000);
            
//            await PlayLyricPhaseExample();
            
//            Log.Info("[SubtitlePhaseExample] 所有阶段效果测试完成");
//        }
//    }
//}