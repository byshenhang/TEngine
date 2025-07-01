using UnityEngine;
using UnityEngine.UI;
using TEngine;
using LyricFX.Managers;
using Cysharp.Threading.Tasks;
using TelePresent.AudioSyncPro;

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

        public string LRCFile = "";

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
        /// <summary>
        /// 调试按钮点击事件 - 使用AudioLyricCoordinator协调器进行音频歌词同步播放
        /// </summary>
        private async void OnClick_debugBtn()
        {
            Debug.Log("---------------------------------- AudioLyricCoordinator 同步播放测试 ----------------------------------");

            // ========== 注释掉的原有调用方式 ==========
            /*
            Debug.Log("开始使用单行复用模式播放测试字幕内容");
            //var config = LyricExtensions.GetBouncyScaleConfig();
            //GameModule.Lyric.PlaySimpleText("I saw you dancing in the moonlight", 0f, config);
            //var config = LyricExtensions.GetFlyInFromTopConfig();
            //GameModule.Lyric.PlaySimpleText("I saw you dancing in the moonlight", 0f, config);

            //var config = LyricExtensions.GetFlyInFromTopConfig();
            //// 设置为单行复用模式
            //config.DisplayMode = LyricDisplayMode.SingleLineReuse;
            ////await GameModule.Lyric.PlaySimpleText("Every word tells a story", 0f, config);
            //string testLrcPath = "Assets/AssetArt/LRC/test.lrc";
            //await GameModule.Lyric.LoadAndPlayLyric(testLrcPath, config);

            var manager = GameModule.LYRIC.GetLyricManager();
            var root = GameObject.Find("InstanceRoot");
            var pool = GameObject.Find("InstancePool");
            GameObject prefabInstance = GameModule.Resource.LoadGameObject("DefaultText");
            var AudioSourceTest = GameModule.Resource.LoadGameObject("AudioSourceTest").GetComponent<AudioSource>();
            AudioSourceTest.Stop();

            GameModule.LYRIC.EnableDebugger(true);

            var text = GameModule.Resource.LoadAsset<TextAsset>("XUNZHANG").text;
            manager.SetupAsync(root.transform, prefabInstance, pool.transform);

            string currentEffectId = "left_to_right_fade";
            string currentLayoutId = "default_linear";
            Vector3 position = new Vector3(0, 0, 0);
            //int id = await GameModule.LYRIC.CreateLyricLine("Hello Wolrd", position,  currentEffectId, currentLayoutId);
            //await GameModule.LYRIC.PlayLyricLine(id);
            GameModule.LYRIC.PlayLrcFile(text, position, AudioSourceTest, 0.1f, currentEffectId, currentLayoutId);
            */

            // ========== 使用自动发现的AudioLyricCoordinator同步调用方式 ==========
            GameObject prefabInstance = GameModule.Resource.LoadGameObject("DefaultText");
            var root = GameObject.Find("InstanceRoot");
            var pool = GameObject.Find("InstancePool");
            GameModule.UI3D.CloseUI3D<BattleMainUI>();

            try
            {
                Debug.Log("开始使用AudioLyricCoordinator进行音频歌词同步播放");
                
                // 1. 获取协调器实例
                var coordinator = GameModule.AUDIO_LYRIC;
                if (coordinator == null)
                {
                    Debug.LogError("AudioLyricCoordinator实例获取失败");
                    return;
                }
                
                // 2. 准备音频和歌词资源
                var audioClip = GameModule.Resource.LoadAsset<AudioClip>("XUNZHANG_AUDIO");
                var lrcText = GameModule.Resource.LoadAsset<TextAsset>("XUNZHANG").text;
                
                if (audioClip == null)
                {
                    Debug.LogError("音频资源加载失败: 勋章 - 鹿晗");
                    return;
                }
                
                if (string.IsNullOrEmpty(lrcText))
                {
                    Debug.LogError("歌词资源加载失败: XUNZHANG");
                    return;
                }
                
                // 3. 启用调试模式
                coordinator.EnableDebugger(true);
                // 4. 设置同步偏移（可选）
                coordinator.SetLyric(root.transform, prefabInstance, pool.transform);
                coordinator.SetSyncOffset(0.1f); // 歌词提前0.1秒显示

                // 5. 自动发现和初始化协调器
                Debug.Log("自动发现AudioReactor并初始化...");
                bool initSuccess = await coordinator.AutoInitializeAsync();
                
                if (!initSuccess)
                {
                    Debug.LogError("AudioLyricCoordinator自动初始化失败，请确保场景中存在AudioReactor组件");
                    return;
                }
                
                // 显示当前使用的AudioReactor
                var currentReactor = coordinator.GetCurrentAudioReactor();
                Debug.Log($"初始化成功，使用AudioReactor: {currentReactor.name} (ID: {currentReactor.id})");
                
                // 显示所有发现的AudioReactor信息
                var discoveredReactors = coordinator.GetDiscoveredAudioReactors();
                Debug.Log($"发现了 {discoveredReactors.Count} 个AudioReactor:");
                foreach (var kvp in discoveredReactors)
                {
                    Debug.Log($"  - {kvp.Value} (ID: {kvp.Key})");
                }
                
                // 6. 订阅事件（可选）
                coordinator.OnPlaybackStarted += () => Debug.Log("[事件] 同步播放已开始");
                coordinator.OnPlaybackStopped += () => Debug.Log("[事件] 同步播放已停止");
                coordinator.OnAudioDataReceived += (rms, spectrum) => {
                    if (rms > 0.1f) // 只在音量较大时输出
                    {
                        Debug.Log($"[音频数据] RMS: {rms:F3}, 频谱长度: {spectrum?.Length ?? 0}");
                    }
                };
                coordinator.OnLyricLineChanged += (lyricLine) => Debug.Log($"当前歌词: {lyricLine}");
                
                // 7. 准备音频和歌词资源
                bool prepareSuccess = await coordinator.PrepareAudioAndLyrics(audioClip, lrcText);
                if (!prepareSuccess)
                {
                    Debug.LogError("准备音频和歌词资源失败");
                    return;
                }
                
                // 8. 开始同步播放
                coordinator.PlaySynchronized();
                Debug.Log("开始音频歌词同步播放");
                
                Debug.Log($"当前播放状态: {(coordinator.IsPlaying() ? "播放中" : "已停止")}");
                Debug.Log($"音频长度: {coordinator.GetAudioLength():F2}秒");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"AudioLyricCoordinator同步播放过程中发生错误: {ex}");
            }
            
        }
        #endregion

    }
}
