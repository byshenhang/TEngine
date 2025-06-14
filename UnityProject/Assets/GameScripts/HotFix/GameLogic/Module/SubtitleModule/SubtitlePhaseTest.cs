// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Cysharp.Threading.Tasks;
// using TEngine;

// namespace GameLogic
// {
//     /// <summary>
//     /// 字幕阶段效果测试脚本
//     /// </summary>
//     public class SubtitlePhaseTest : MonoBehaviour
// //    {
// //        [Header("测试配置")]
// //        public string testText = "测试字幕效果阶段";
// //        public Transform parentTransform;
        
// //        private SubtitleModule _subtitleModule;
        
// //        private void Start()
// //        {
// //            _subtitleModule = GameModule.GetModule<SubtitleModule>();
// //            if (_subtitleModule == null)
// //            {
// //                Log.Error("[SubtitlePhaseTest] 未找到字幕模块");
// //                return;
// //            }
// //        }
        
// //        /// <summary>
// //        /// 测试进场阶段多个效果
// //        /// </summary>
// //        [ContextMenu("测试进场多效果")]
// //        public async void TestEnterPhaseMultipleEffects()
// //        {
// //            try
// //            {
// //                Log.Info("[SubtitlePhaseTest] 开始测试进场阶段多个效果");
                
// //                // 创建字幕配置
// //                var config = new SubtitleSequenceConfig
// //                {
// //                    Text = testText,
// //                    DisplayMode = SubtitleDisplayMode.Character,
// //                    CharacterInterval = 0.1f,
// //                    HoldTime = 3f,
// //                    StartDelay = 0.5f,
// //                    ParentTransform = parentTransform ?? transform,
                    
// //                    // 配置多个进场效果
// //                    Effects = new List<SubtitleEffectConfig>
// //                    {
// //                        // 淡入效果
// //                        new FadeEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Enter,
// //                            Duration = 0.8f,
// //                            Delay = 0f,
// //                            StartAlpha = 0f,
// //                            EndAlpha = 1f,
// //                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
// //                        },
                        
// //                        // 缩放效果
// //                        new ScaleEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Enter,
// //                            Duration = 0.6f,
// //                            Delay = 0.2f,
// //                            StartScale = Vector3.one * 0.5f,
// //                            EndScale = Vector3.one,
// //                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
// //                        },
                        
// //                        // 模糊效果（从模糊到清晰）
// //                        new BlurEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Enter,
// //                            Duration = 1f,
// //                            Delay = 0f,
// //                            StartBlurRadius = 5f,
// //                            EndBlurRadius = 0f,
// //                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
// //                        },
                        
// //                        // 停留阶段的轻微缩放
// //                        new ScaleEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Stay,
// //                            Duration = 2f,
// //                            Delay = 0f,
// //                            StartScale = Vector3.one,
// //                            EndScale = Vector3.one * 1.05f,
// //                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
// //                        },
                        
// //                        // 离场阶段的淡出
// //                        new FadeEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Exit,
// //                            Duration = 0.5f,
// //                            Delay = 0f,
// //                            StartAlpha = 1f,
// //                            EndAlpha = 0f,
// //                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
// //                        }
// //                    }
// //                };
                
// //                // 播放字幕序列
// //                await _subtitleModule.PlaySubtitleSequenceAsync("test_multi_enter", config);
                
// //                Log.Info("[SubtitlePhaseTest] 进场多效果测试完成");
// //            }
// //            catch (Exception ex)
// //            {
// //                Log.Error($"[SubtitlePhaseTest] 测试失败: {ex.Message}");
// //            }
// //        }
        
// //        /// <summary>
// //        /// 测试单独的进场效果
// //        /// </summary>
// //        [ContextMenu("测试单独进场效果")]
// //        public async void TestSingleEnterEffect()
// //        {
// //            try
// //            {
// //                Log.Info("[SubtitlePhaseTest] 开始测试单独进场效果");
                
// //                var config = new SubtitleSequenceConfig
// //                {
// //                    Text = "单独进场效果测试",
// //                    DisplayMode = SubtitleDisplayMode.All,
// //                    HoldTime = 2f,
// //                    ParentTransform = parentTransform ?? transform,
                    
// //                    Effects = new List<SubtitleEffectConfig>
// //                    {
// //                        new FadeEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Enter,
// //                            Duration = 1f,
// //                            StartAlpha = 0f,
// //                            EndAlpha = 1f
// //                        }
// //                    }
// //                };
                
// //                await _subtitleModule.PlaySubtitleSequenceAsync("test_single_enter", config);
                
// //                Log.Info("[SubtitlePhaseTest] 单独进场效果测试完成");
// //            }
// //            catch (Exception ex)
// //            {
// //                Log.Error($"[SubtitlePhaseTest] 测试失败: {ex.Message}");
// //            }
// //        }
        
// //        /// <summary>
// //        /// 测试效果阶段冲突处理
// //        /// </summary>
// //        [ContextMenu("测试阶段冲突")]
// //        public async void TestPhaseConflict()
// //        {
// //            try
// //            {
// //                Log.Info("[SubtitlePhaseTest] 开始测试阶段冲突处理");
                
// //                var config = new SubtitleSequenceConfig
// //                {
// //                    Text = "阶段冲突测试",
// //                    DisplayMode = SubtitleDisplayMode.All,
// //                    HoldTime = 1f,
// //                    ParentTransform = parentTransform ?? transform,
                    
// //                    Effects = new List<SubtitleEffectConfig>
// //                    {
// //                        // 两个同时的淡入效果（应该都能正常执行）
// //                        new FadeEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Enter,
// //                            Duration = 0.8f,
// //                            StartAlpha = 0f,
// //                            EndAlpha = 1f
// //                        },
// //                        new FadeEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Enter,
// //                            Duration = 1.2f,
// //                            StartAlpha = 0.5f,
// //                            EndAlpha = 1f
// //                        }
// //                    }
// //                };
                
// //                await _subtitleModule.PlaySubtitleSequenceAsync("test_conflict", config);
                
// //                Log.Info("[SubtitlePhaseTest] 阶段冲突测试完成");
// //            }
// //            catch (Exception ex)
// //            {
// //                Log.Error($"[SubtitlePhaseTest] 测试失败: {ex.Message}");
// //            }
// //        }
        
// //        /// <summary>
// //        /// 测试所有阶段的完整流程
// //        /// </summary>
// //        [ContextMenu("测试完整阶段流程")]
// //        public async void TestCompletePhaseFlow()
// //        {
// //            try
// //            {
// //                Log.Info("[SubtitlePhaseTest] 开始测试完整阶段流程");
                
// //                var config = new SubtitleSequenceConfig
// //                {
// //                    Text = "完整阶段流程测试",
// //                    DisplayMode = SubtitleDisplayMode.Character,
// //                    CharacterInterval = 0.15f,
// //                    HoldTime = 2f,
// //                    ParentTransform = parentTransform ?? transform,
                    
// //                    Effects = new List<SubtitleEffectConfig>
// //                    {
// //                        // 进场：淡入 + 缩放
// //                        new FadeEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Enter,
// //                            Duration = 0.6f,
// //                            StartAlpha = 0f,
// //                            EndAlpha = 1f
// //                        },
// //                        new ScaleEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Enter,
// //                            Duration = 0.5f,
// //                            StartScale = Vector3.zero,
// //                            EndScale = Vector3.one
// //                        },
                        
// //                        // 停留：轻微摆动
// //                        new ScaleEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Stay,
// //                            Duration = 1.5f,
// //                            StartScale = Vector3.one,
// //                            EndScale = Vector3.one * 1.1f,
// //                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
// //                        },
                        
// //                        // 离场：淡出 + 缩小
// //                        new FadeEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Exit,
// //                            Duration = 0.4f,
// //                            StartAlpha = 1f,
// //                            EndAlpha = 0f
// //                        },
// //                        new ScaleEffectConfig
// //                        {
// //                            Phase = SubtitleEffectPhase.Exit,
// //                            Duration = 0.4f,
// //                            StartScale = Vector3.one,
// //                            EndScale = Vector3.zero
// //                        }
// //                    }
// //                };
                
// //                await _subtitleModule.PlaySubtitleSequenceAsync("test_complete_flow", config);
                
// //                Log.Info("[SubtitlePhaseTest] 完整阶段流程测试完成");
// //            }
// //            catch (Exception ex)
// //            {
// //                Log.Error($"[SubtitlePhaseTest] 测试失败: {ex.Message}");
// //            }
// //        }
// //    }
// //}