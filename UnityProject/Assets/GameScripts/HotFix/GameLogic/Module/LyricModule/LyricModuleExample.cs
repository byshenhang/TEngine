using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 歌词模块使用示例
    /// </summary>
    public class LyricModuleExample : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private string testLrcPath = "Assets/StreamingAssets/Lyrics/test.lrc";
        [SerializeField] private Transform lyricParent;
        [SerializeField] private GameObject characterPrefab;
        
        [Header("效果测试")]
        [SerializeField] private bool autoPlayOnStart = false;
        [SerializeField] private float testDelay = 2f;
        
        private LyricModule _lyricModule;
        
        private void Start()
        {
            InitializeLyricModule();
            
            if (autoPlayOnStart)
            {
                _ = TestLyricEffects();
            }
        }
        
        private void InitializeLyricModule()
        {
            _lyricModule = LyricModule.Instance;
            
            if (lyricParent != null)
            {
                _lyricModule.SetLyricParent(lyricParent);
            }
            
            if (characterPrefab != null)
            {
                _lyricModule.SetCharacterPrefab(characterPrefab);
            }
        }
        
        #region 测试方法
        
        /// <summary>
        /// 测试各种歌词效果
        /// </summary>
        private async UniTask TestLyricEffects()
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(testDelay));
            
            Debug.Log("开始测试歌词效果...");
            
            // 测试1：经典模糊效果
            await TestClassicBlurEffect();
            
            await UniTask.Delay(System.TimeSpan.FromSeconds(3f));
            
            // 测试2：弹性缩放效果
            await TestBouncyScaleEffect();
            
            await UniTask.Delay(System.TimeSpan.FromSeconds(3f));
            
            // 测试3：飞入效果
            await TestFlyInEffect();
            
            await UniTask.Delay(System.TimeSpan.FromSeconds(3f));
            
            // 测试4：打字机效果
            await TestTypewriterEffect();
            
            await UniTask.Delay(System.TimeSpan.FromSeconds(3f));
            
            // 测试5：从LRC文件播放
            await TestLrcFilePlayback();
        }
        
        /// <summary>
        /// 测试经典模糊效果
        /// </summary>
        private async UniTask TestClassicBlurEffect()
        {
            Debug.Log("测试经典模糊效果");
            
            var config = LyricExtensions.GetClassicBlurConfig();
            await _lyricModule.PlaySimpleText("I saw you dancing in the moonlight", 0f, config);
        }
        
        /// <summary>
        /// 测试弹性缩放效果
        /// </summary>
        private async UniTask TestBouncyScaleEffect()
        {
            Debug.Log("测试弹性缩放效果");
            
            var config = LyricExtensions.GetBouncyScaleConfig();
            await _lyricModule.PlaySimpleText("Your smile lights up the world", 0f, config);
        }
        
        /// <summary>
        /// 测试飞入效果
        /// </summary>
        private async UniTask TestFlyInEffect()
        {
            Debug.Log("测试飞入效果");
            
            var config = LyricExtensions.GetFlyInFromTopConfig();
            await _lyricModule.PlaySimpleText("Like stars falling from the sky", 0f, config);
        }
        
        /// <summary>
        /// 测试打字机效果
        /// </summary>
        private async UniTask TestTypewriterEffect()
        {
            Debug.Log("测试打字机效果");
            
            var config = LyricExtensions.GetTypewriterConfig();
            await _lyricModule.PlaySimpleText("Every word tells a story", 0f, config);
        }
        
        /// <summary>
        /// 测试LRC文件播放
        /// </summary>
        private async UniTask TestLrcFilePlayback()
        {
            Debug.Log("测试LRC文件播放");
            
            if (System.IO.File.Exists(testLrcPath))
            {
                var config = LyricExtensions.GetClassicBlurConfig();
                await _lyricModule.LoadAndPlayLyric(testLrcPath, config);
            }
            else
            {
                Debug.LogWarning($"LRC文件不存在: {testLrcPath}");
                
                // 创建示例歌词数据
                await TestMultipleLinesEffect();
            }
        }
        
        /// <summary>
        /// 测试多行歌词效果
        /// </summary>
        private async UniTask TestMultipleLinesEffect()
        {
            Debug.Log("测试多行歌词效果");
            
            var lines = new List<(float time, string text)>
            {
                (0f, "In the silence of the night"),
                (3f, "I hear your whispered dreams"),
                (6f, "Dancing through the starlight"),
                (9f, "Nothing is quite what it seems")
            };
            
            var config = LyricExtensions.GetClassicBlurConfig();
            await _lyricModule.PlayMultipleLines(lines, config);
        }
        
        #endregion
        
        #region UI按钮方法（可在Inspector中绑定）
        
        [ContextMenu("播放经典模糊效果")]
        public void PlayClassicBlur()
        {
            _ = TestClassicBlurEffect();
        }
        
        [ContextMenu("播放弹性缩放效果")]
        public void PlayBouncyScale()
        {
            _ = TestBouncyScaleEffect();
        }
        
        [ContextMenu("播放飞入效果")]
        public void PlayFlyIn()
        {
            _ = TestFlyInEffect();
        }
        
        [ContextMenu("播放打字机效果")]
        public void PlayTypewriter()
        {
            _ = TestTypewriterEffect();
        }
        
        [ContextMenu("停止所有歌词")]
        public void StopAllLyrics()
        {
            _lyricModule?.StopAllLyrics();
        }
        
        [ContextMenu("清除所有歌词")]
        public void ClearAllLyrics()
        {
            _lyricModule?.ClearAllLyrics();
        }
        
        #endregion
        
        #region 自定义效果示例
        
        /// <summary>
        /// 创建自定义效果配置示例
        /// </summary>
        /// <returns>自定义配置</returns>
        public LyricConfig CreateCustomEffectConfig()
        {
            var config = LyricConfig.Default;
            
            // 自定义进入效果：旋转+缩放+淡入
            config.EnterEffect = new LyricEffectConfig
            {
                EffectType = LyricEffectType.ScaleFade,
                Duration = 1.5f,
                ScaleParams = new ScaleEffectParams
                {
                    StartScale = new Vector3(0.1f, 0.1f, 1f),
                    EndScale = Vector3.one
                },
                FadeParams = new FadeEffectParams
                {
                    StartAlpha = 0f,
                    EndAlpha = 1f
                },
                Curve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };
            
            // 自定义字符效果：模糊+移动
            config.CharacterEffect = new LyricEffectConfig
            {
                EffectType = LyricEffectType.BlurFade,
                Duration = 0.8f,
                BlurParams = new BlurEffectParams
                {
                    StartBlur = 20f,
                    EndBlur = 0f
                },
                FadeParams = new FadeEffectParams
                {
                    StartAlpha = 0f,
                    EndAlpha = 1f
                },
                Curve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };
            
            // 自定义离开效果：向下移动+淡出
            config.ExitEffect = new LyricEffectConfig
            {
                EffectType = LyricEffectType.MoveFade,
                Duration = 1f,
                MoveParams = new MoveEffectParams
                {
                    StartPosition = Vector3.zero,
                    EndPosition = new Vector3(0, -50, 0),
                    UseRelativePosition = true
                },
                FadeParams = new FadeEffectParams
                {
                    StartAlpha = 1f,
                    EndAlpha = 0f
                },
                Curve = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };
            
            return config;
        }
        
        [ContextMenu("播放自定义效果")]
        public void PlayCustomEffect()
        {
            _ = TestCustomEffect();
        }
        
        private async UniTask TestCustomEffect()
        {
            Debug.Log("测试自定义效果");
            
            var config = CreateCustomEffectConfig();
            await _lyricModule.PlaySimpleText("Custom effect showcase", 0f, config);
        }
        
        #endregion
        
        #region 调试信息
        
        private void OnGUI()
        {
            if (_lyricModule == null)
                return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("歌词模块调试信息", GUI.skin.box);
            GUILayout.Label($"当前播放状态: {(_lyricModule.IsPlaying ? "播放中" : "停止")}");
            GUILayout.Label($"活跃歌词行数: {_lyricModule.GetActiveLyricCount()}");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("播放经典模糊效果"))
            {
                PlayClassicBlur();
            }
            
            if (GUILayout.Button("播放弹性缩放效果"))
            {
                PlayBouncyScale();
            }
            
            if (GUILayout.Button("播放飞入效果"))
            {
                PlayFlyIn();
            }
            
            if (GUILayout.Button("播放打字机效果"))
            {
                PlayTypewriter();
            }
            
            if (GUILayout.Button("停止所有歌词"))
            {
                StopAllLyrics();
            }
            
            if (GUILayout.Button("清除所有歌词"))
            {
                ClearAllLyrics();
            }
            
            GUILayout.EndArea();
        }
        
        #endregion
    }
}