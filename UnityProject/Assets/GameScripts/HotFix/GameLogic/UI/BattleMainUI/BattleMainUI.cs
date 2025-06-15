using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class BattleMainUI : UI3DWindow
    {
        #region 脚本工具生成的代码
        private RectTransform _rectContainer;
        private GameObject _itemTouch;
        private GameObject _goTopInfo;
        private GameObject _itemRoleInfo;
        private GameObject _itemMonsterInfo;
        private Button _btn_debug;
        protected override void ScriptGenerator()
        {
            _rectContainer = FindChildComponent<RectTransform>("m_rectContainer");
            _itemTouch = FindChild("m_rectContainer/m_itemTouch").gameObject;
            _goTopInfo = FindChild("m_goTopInfo").gameObject;
            _itemRoleInfo = FindChild("m_goTopInfo/m_itemRoleInfo").gameObject;
            _itemMonsterInfo = FindChild("m_goTopInfo/m_itemMonsterInfo").gameObject;
            _btn_debug = FindChildComponent<Button>("m_btn_debug");
            _btn_debug.onClick.AddListener(OnClick_debugBtn);
        }
        #endregion

        #region 事件
        private void OnClick_debugBtn()
        {
            Debug.Log("---------------------------------- XR Event Action ----------------------------------");
            
            // 播放测试字幕内容
            var subtitleModule = GameModule.Subtitle;
            if (subtitleModule != null)
            {
                // 使用测试LRC内容播放带效果的字幕
                string testLrcContent = @"[ti:TestMusic]
[ar:MusicPlayer]
[al:Test]
[by:Test1]
[offset:0]

[00:00.00]This is the first line of test subtitles
[00:03.00]This is the second line of test subtitles
[00:06.00]Subtitle playback with multiple effects
[00:09.00]Fade in, zoom, fade out effects
[00:12.00]Test complete";
                
                var asset = ScriptableObject.CreateInstance<SubtitleSequenceAsset>();
                asset.SequenceName = "歌词效果";
                asset.Description = "专门用于歌词显示的效果配置";
                asset.Version = "1.0";
                asset.Config = new SubtitleSequenceConfig
                {
                    SequenceType = SubtitleSequenceType.Lyric,
                    DisplayMode = SubtitleDisplayMode.WordByWord,
                    Text = "♪ Beautiful music flows ♪", // 这个文本会被LRC内容覆盖
                    Position = new Vector3(0, -200, 0), // Y值调整到更适合屏幕底部的位置
                    FontSize = 48, // 字体调大一些
                    TextColor = Color.white,
                    StartDelay = 0.1f, // 减少初始延迟
                    WordInterval = 0.05f, // 逐字显示更快
                    HoldDuration = 3f, // 调整停留时间
                    FadeOutDuration = 0.5f, // 淡出快一些
                    Effects = new System.Collections.Generic.List<SubtitleEffectConfig>
                    {
                        // 音乐节拍感的进场
                        new ScaleEffectConfig
                        {
                            Phase = SubtitleEffectPhase.Enter,
                            Duration = 0.2f,
                            Delay = 0f,
                            ScaleStart = Vector3.one * 0.7f,
                            ScaleEnd = Vector3.one,
                            Bounce = true,
                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                        },
                        new FadeEffectConfig
                        {
                            Phase = SubtitleEffectPhase.Enter,
                            Duration = 0.2f,
                            Delay = 0f,
                            AlphaStart = 0f,
                            AlphaEnd = 1f,
                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                        },
                        // 停留时的轻微律动
                        new ScaleEffectConfig
                        {
                            Phase = SubtitleEffectPhase.Stay,
                            Duration = 2.5f, // 配合歌词时长
                            Delay = 0f,
                            ScaleStart = Vector3.one,
                            ScaleEnd = Vector3.one * 1.03f,
                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                        },
                        // 优雅的离场
                        new FadeEffectConfig
                        {
                            Phase = SubtitleEffectPhase.Exit,
                            Duration = 0.4f,
                            Delay = 0f,
                            AlphaStart = 1f,
                            AlphaEnd = 0f,
                            AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                        },
                        // new BlurEffectConfig // 暂时移除模糊，看是否是问题根源
                        // {
                        //     Phase = SubtitleEffectPhase.Exit,
                        //     Duration = 0.5f,
                        //     Delay = 0.0f, 
                        //     BlurStart = 0f,
                        //     BlurEnd = 5f,
                        //     AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                        // }
                    }
                };

               
                _ = subtitleModule.PlayLRCWithConfigAsync(testLrcContent, asset);
                Debug.Log("开始使用自定义配置播放测试字幕内容");
          
            }
            else
            {
                Debug.LogError("字幕模块未找到");
            }
            
            GameModule.UI3D.CloseUI3D<BattleMainUI>();
        }
        #endregion

    }
}
