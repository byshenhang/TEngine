using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 字幕流程测试脚本
    /// 用于验证字幕播放的完整流程是否正常工作
    /// </summary>
    public class SubtitleFlowTest : MonoBehaviour
    {
        [Header("测试配置")]
        public Transform subtitleParent;
        public bool autoStartTest = true;
        public float testInterval = 3f;
        
        private SubtitleModule _subtitleModule;
        
        private void Start()
        {
            if (autoStartTest)
            {
                StartFlowTest().Forget();
            }
        }
        
        /// <summary>
        /// 开始流程测试
        /// </summary>
        [ContextMenu("开始流程测试")]
        public async UniTaskVoid StartFlowTest()
        {
            // 初始化字幕模块
            _subtitleModule = SubtitleModule.Instance;
            if (_subtitleModule == null)
            {
                Log.Error("[SubtitleFlowTest] 字幕模块未找到");
                return;
            }
            
            Log.Info("[SubtitleFlowTest] 开始字幕流程测试");
            
            // 测试1: 基础字幕播放
            await TestBasicSubtitle();
            await UniTask.Delay((int)(testInterval * 1000));
            
            // 测试2: 带淡入效果的字幕
            await TestFadeEffect();
            await UniTask.Delay((int)(testInterval * 1000));
            
            // 测试3: 逐字符显示
            await TestCharacterByCharacter();
            await UniTask.Delay((int)(testInterval * 1000));
            
            // 测试4: 多效果组合
            await TestMultipleEffects();
            
            Log.Info("[SubtitleFlowTest] 字幕流程测试完成");
        }
        
        /// <summary>
        /// 测试基础字幕播放
        /// </summary>
        private async UniTask TestBasicSubtitle()
        {
            Log.Info("[SubtitleFlowTest] 测试1: 基础字幕播放");
            
            var config = new SubtitleSequenceConfig
            {
                Text = "Hello World!",
                DisplayMode = SubtitleDisplayMode.All,
                Position = Vector3.zero,
                FontSize = 24,
                TextColor = Color.white,
                HoldDuration = 2f
            };
            
            try
            {
                await _subtitleModule.PlaySubtitleSequenceAsync("test_basic", config);
                Log.Info("[SubtitleFlowTest] 基础字幕播放成功");
            }
            catch (System.Exception e)
            {
                Log.Error($"[SubtitleFlowTest] 基础字幕播放失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 测试淡入效果
        /// </summary>
        private async UniTask TestFadeEffect()
        {
            Log.Info("[SubtitleFlowTest] 测试2: 淡入效果");
            
            var config = new SubtitleSequenceConfig
            {
                Text = "Fade In Effect",
                DisplayMode = SubtitleDisplayMode.All,
                Position = new Vector3(0, 50, 0),
                FontSize = 28,
                TextColor = Color.cyan,
                HoldDuration = 2f
            };
            
            // 添加淡入效果
            config.Effects.Add(new FadeEffectConfig
            {
                Phase = SubtitleEffectPhase.Enter,
                Duration = 1f,
                Delay = 0f,
                AlphaStart = 0f,
                AlphaEnd = 1f
            });
            
            try
            {
                await _subtitleModule.PlaySubtitleSequenceAsync("test_fade", config);
                Log.Info("[SubtitleFlowTest] 淡入效果测试成功");
            }
            catch (System.Exception e)
            {
                Log.Error($"[SubtitleFlowTest] 淡入效果测试失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 测试逐字符显示
        /// </summary>
        private async UniTask TestCharacterByCharacter()
        {
            Log.Info("[SubtitleFlowTest] 测试3: 逐字符显示");
            
            var config = new SubtitleSequenceConfig
            {
                Text = "Character by Character",
                DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
                CharacterInterval = 0.1f,
                Position = new Vector3(0, -50, 0),
                FontSize = 26,
                TextColor = Color.yellow,
                HoldDuration = 1f
            };
            
            try
            {
                await _subtitleModule.PlaySubtitleSequenceAsync("test_character", config);
                Log.Info("[SubtitleFlowTest] 逐字符显示测试成功");
            }
            catch (System.Exception e)
            {
                Log.Error($"[SubtitleFlowTest] 逐字符显示测试失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 测试多效果组合
        /// </summary>
        private async UniTask TestMultipleEffects()
        {
            Log.Info("[SubtitleFlowTest] 测试4: 多效果组合");
            
            var config = new SubtitleSequenceConfig
            {
                Text = "Multiple Effects!",
                DisplayMode = SubtitleDisplayMode.WordByWord,
                WordInterval = 0.2f,
                Position = new Vector3(0, 100, 0),
                FontSize = 32,
                TextColor = Color.magenta,
                HoldDuration = 2f
            };
            
            // 添加进场效果：淡入 + 缩放
            config.Effects.Add(new FadeEffectConfig
            {
                Phase = SubtitleEffectPhase.Enter,
                Duration = 0.5f,
                Delay = 0f,
                AlphaStart = 0f,
                AlphaEnd = 1f
            });
            
            config.Effects.Add(new ScaleEffectConfig
            {
                Phase = SubtitleEffectPhase.Enter,
                Duration = 0.5f,
                Delay = 0f,
                ScaleStart = Vector3.zero,
                ScaleEnd = Vector3.one
            });
            
            // 添加离场效果：淡出
            config.Effects.Add(new FadeEffectConfig
            {
                Phase = SubtitleEffectPhase.Exit,
                Duration = 0.5f,
                Delay = 0f,
                AlphaStart = 1f,
                AlphaEnd = 0f
            });
            
            try
            {
                await _subtitleModule.PlaySubtitleSequenceAsync("test_multiple", config);
                Log.Info("[SubtitleFlowTest] 多效果组合测试成功");
            }
            catch (System.Exception e)
            {
                Log.Error($"[SubtitleFlowTest] 多效果组合测试失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 停止所有测试
        /// </summary>
        [ContextMenu("停止所有测试")]
        public void StopAllTests()
        {
            if (_subtitleModule != null)
            {
                _subtitleModule.StopAllSequences();
                Log.Info("[SubtitleFlowTest] 已停止所有字幕测试");
            }
        }
    }
}