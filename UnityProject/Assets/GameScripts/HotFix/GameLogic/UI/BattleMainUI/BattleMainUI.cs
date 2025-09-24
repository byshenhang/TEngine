using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.UI;
using AudioType = UnityEngine.AudioType;

namespace GameLogic
{
    /// <summary>
    /// BattleMainUI 负责主战斗界面的UI逻辑，包括音频列表、分享窗口与场景选择等功能。
    /// </summary>
    [Window(UILayer.UI)]
    class BattleMainUI : UI3DWindow
    {
        private Button _btn_debug;
        private ScrollRect _scrollView;
        private Transform _content;
        private GameObject _itemTemplate;
        private Button _btn_share;
        private Button _btn_scene;

        private GameObject _shareWindow;
        private TextMeshProUGUI _shareIPText;

        // SceneWindow 相关
        private GameObject _sceneWindow;
        private Transform _sceneContent;
        private GameObject _sceneItemTemplate;

        public string LRCFile = "";
        private List<string> audioFiles = new List<string>();
        private string uploadPath;

        /// <summary>
        /// 初始化BattleMainUI的关键UI组件与状态，移除冗余判空与调试输出，仅保留精简的组件查找与事件绑定。
        /// </summary>
        protected override void ScriptGenerator()
        {
            try
            {
                // 可选：调试按钮存在则绑定
                _btn_debug = FindChildComponent<Button>("m_btn_debug") ?? FindChild("m_btn_debug")?.GetComponent<Button>();
                _btn_debug?.onClick.AddListener(OnClick_debugBtn);
                // 场景按钮存在则绑定
                _btn_scene = FindChildComponent<Button>("m_btn_scene") ?? FindChild("m_btn_scene")?.GetComponent<Button>();
                _btn_scene?.onClick.AddListener(OnClick_sceneBtn);

                // 必需组件：直接查找与使用
                _scrollView = FindChildComponent<ScrollRect>("Scroll View") ?? FindChild("Scroll View")?.GetComponent<ScrollRect>();
                _content = FindChildComponent<Transform>("Scroll View/Viewport/Content")
                           ?? FindChild("Scroll View/Viewport")?.Find("Content");
                var itemTransform = FindChildComponent<Transform>("Scroll View/Viewport/Content/Item")
                                    ?? _content?.Find("Item");
                _itemTemplate = itemTransform.gameObject;

                // 初始化上传路径
                uploadPath = Path.Combine(Application.persistentDataPath, "Upload");

                // 初始化时隐藏Scroll View与模板Item
                _scrollView.gameObject.SetActive(false);
                _itemTemplate.SetActive(false);

                // 分享窗口与按钮
                _shareIPText = FindChildComponent<TextMeshProUGUI>("ShareInternet/m_text_ip");
                _btn_share = FindChildComponent<Button>("m_btn_share");
                _btn_share?.onClick.AddListener(OnClick_shareBtn);
                _shareWindow = FindChildComponent<Transform>("ShareInternet").gameObject;
                _shareWindow.SetActive(false);

                // SceneWindow 结构
                var sceneWinTf = FindChildComponent<Transform>("SceneWindow") ?? FindChild("SceneWindow");
                _sceneWindow = sceneWinTf?.gameObject;
                _sceneContent = FindChildComponent<Transform>("SceneWindow/Viewport/Content")
                                 ?? sceneWinTf?.Find("Viewport/Content");
                var sceneItemTf = FindChildComponent<Transform>("SceneWindow/Viewport/Content/Item")
                                   ?? _sceneContent?.Find("Item");
                _sceneItemTemplate = sceneItemTf?.gameObject;

                // 初始隐藏SceneWindow与模板
                _sceneWindow?.SetActive(false);
                _sceneItemTemplate?.SetActive(false);
            }
            catch
            {
                // 保持原有异常抛出行为
                throw;
            }
        }

        /// <summary>
        /// 场景选择按钮点击：打开/关闭 SceneWindow，并在打开时刷新场景项。
        /// </summary>
        private void OnClick_sceneBtn()
        {
            if (_sceneWindow == null || _sceneContent == null || _sceneItemTemplate == null) return;
            bool active = _sceneWindow.activeSelf;
            _sceneWindow.SetActive(!active);

            _shareWindow.SetActive(false);
            _scrollView.gameObject.SetActive(false);

            if (!active)
            {
                RefreshSceneItems();
            }
        }

        /// <summary>
        /// 刷新 SceneWindow 内的场景Item列表，来源于 SceneConfig.SceneItems
        /// </summary>
        private void RefreshSceneItems()
        {
            // 清除旧项（保留模板）
            for (int i = _sceneContent.childCount - 1; i >= 0; i--)
            {
                var child = _sceneContent.GetChild(i);
                if (child.gameObject != _sceneItemTemplate)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }

            // 生成新项
            foreach (var si in SceneConfig.SceneItems)
            {
                var go = GameObject.Instantiate(_sceneItemTemplate, _sceneContent);
                go.SetActive(true);

                // 设置图标
                var icon = go.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    var sprite = GameModule.Resource.LoadAsset<Sprite>(si.ImagePath);
                    if (sprite == null)
                    {
                        var tex = GameModule.Resource.LoadAsset<Texture2D>(si.ImagePath);
                        if (tex != null)
                        {
                            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                    }
                    if (sprite != null) icon.sprite = sprite;
                }

                // 设置名称（兼容 TextMeshProUGUI / Text）
                var nameTf = go.transform.Find("Name");
                var tmp = nameTf?.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = si.ShowName;
                var ugui = nameTf?.GetComponent<Text>();
                if (ugui != null) ugui.text = si.ShowName;

                // 绑定点击事件
                var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    var captured = si;
                    btn.onClick.AddListener(() => OnSceneItemClick(captured));
                }
            }
        }

        /// <summary>
        /// 场景Item点击：加载对应场景。
        /// </summary>
        private async void OnSceneItemClick(SceneItem item)
        {
            await GameModule.Scene.LoadSceneAsync(
                item.ScenePath,
                UnityEngine.SceneManagement.LoadSceneMode.Single,
                false,
                100,
                true,
                null
            );

            await UniTask.Delay(100).ContinueWith(() =>
            {
                // 加载XR玩家对象
                var XRPlayer = GameModule.Resource.LoadGameObject("XROrigin");
                // XRPlayer.transform.position = new Vector3(88.46f, 2.769f, 85.48f);
                XRPlayer.transform.position = Vector3.zero;
                // 计算在XR玩家前方的UI位置与朝向（优先使用主摄像机）
                var cam = XRPlayer.GetComponentInChildren<Camera>();
                var forward = cam != null ? cam.transform.forward : XRPlayer.transform.forward;
                var origin = cam != null ? cam.transform.position : XRPlayer.transform.position;
                var uiPos = origin + forward * 6.0f + new Vector3(0, 2, 0); // 距离玩家2米处
                var uiRot = Quaternion.LookRotation(forward, Vector3.up);

                // 在XR玩家前方展示BattleMainUI（UI3D）
                GameModule.UI3D.ShowUI3D<BattleMainUI>(uiPos, uiRot, null).Forget();
            });
        }

        /// <summary>
        /// 分享按钮点击事件：切换分享窗口显示，并显示本机IP，隐藏调试列表。
        /// </summary>
        private void OnClick_shareBtn()
        {
            _shareWindow.SetActive(!_shareWindow.activeSelf);
            _scrollView.gameObject.SetActive(false);
            _sceneWindow.gameObject.SetActive(false);
            _shareIPText.text = GetLocalIPAddress() + ":8080";
        }

        /// <summary>
        /// 调试按钮点击事件 - 激活/关闭 Scroll View
        /// </summary>
        private void OnClick_debugBtn()
        {
            bool isActive = _scrollView.gameObject.activeSelf;
            _scrollView.gameObject.SetActive(!isActive);
            _sceneWindow.gameObject.SetActive(false);
            _shareWindow.SetActive(false);
            if (!isActive)
            {
                // 激活时刷新音频文件列表
                RefreshAudioFileList();
            }
        }

        /// <summary>
        /// 刷新音频文件列表
        /// </summary>
        private void RefreshAudioFileList()
        {
            // 清空现有列表
            ClearAudioList();

            // 读取上传目录中的音频文件
            LoadAudioFiles();

            // 创建UI列表项
            CreateAudioListItems();
        }


        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new System.Exception("No network adapters with an IPv4 address in the system!");
        }


        /// <summary>
        /// 清空音频列表UI
        /// </summary>
        private void ClearAudioList()
        {
            // 清除除模板外的所有子对象
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Transform child = _content.GetChild(i);
                if (child.gameObject != _itemTemplate)
                {
                    GameObject.DestroyImmediate(child.gameObject);
                }
            }
            audioFiles.Clear();
        }

        /// <summary>
        /// 从上传目录加载音频文件
        /// </summary>
        private void LoadAudioFiles()
        {
            try
            {
                if (Directory.Exists(uploadPath))
                {
                    string[] files = Directory.GetFiles(uploadPath, "*.mp3", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        audioFiles.Add(file);
                    }
                    Debug.Log($"找到 {audioFiles.Count} 个音频文件");
                }
                else
                {
                    Debug.LogWarning($"上传目录不存在: {uploadPath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"加载音频文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建音频列表UI项
        /// </summary>
        private void CreateAudioListItems()
        {
            foreach (string audioFile in audioFiles)
            {
                GameObject item = GameObject.Instantiate(_itemTemplate, _content);
                item.SetActive(true);

                // 获取文件名（不包含路径和扩展名）
                string fileName = Path.GetFileNameWithoutExtension(audioFile);

                var meta = AudioTagUtil.ReadMetaFromFile(audioFile, fileName);
                var artists = meta.Artists != null ? string.Join(", ", meta.Artists) : "(unknown)";
                Debug.Log($"[META] Title: {meta.Title} | Artist: {artists} | Album: {meta.Album} | Year: {meta.Year}");
                Debug.Log($"[META] Duration(tag): {meta.Duration} | {meta.SampleRate} Hz / {meta.Channels} ch / {meta.BitrateKbps} kbps");

                // 设置文本
                TextMeshProUGUI name = item.transform.Find("Content/Name").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI artist = item.transform.Find("Content/Artist").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI time = item.transform.Find("Content/Time").GetComponent<TextMeshProUGUI>();
                Image icon = item.transform.Find("Content/Icon").GetComponent<Image>();

                name.text = meta.Title;
                artist.text = meta.Artists[0];
                time.text = meta.Duration.ToString(@"mm\:ss");

                var texture = AudioTagUtil.CoverToTexture(meta.CoverBytes);
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                icon.sprite = sprite;

                // 设置按钮点击事件
                Button button = item.GetComponentInChildren<Button>();
                if (button != null)
                {
                    string filePath = audioFile; // 捕获当前文件路径
                    button.onClick.AddListener(() => OnAudioItemClick(filePath, fileName));
                }
            }
        }

        /// <summary>
        /// 音频项点击事件
        /// </summary>
        private async void OnAudioItemClick(string filePath, string fileName)
        {
            Debug.Log($"点击音频文件: {fileName}");
            Debug.Log($"文件路径: {filePath}");
            // 按原逻辑加载 AudioClip 并播放
            using (var request = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip($"file://{filePath}", AudioType.MPEG))
            {
                await request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    AudioClip audioClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
                    audioClip.name = fileName;

                    Debug.Log($"成功加载音频: {fileName}, 长度: {audioClip.length}秒");

                    // 开始播放（保持你的原有逻辑）
                    await PlayAudioWithLyrics(audioClip, fileName);
                }
                else
                {
                    Debug.LogError($"加载音频失败: {request.error}");
                }
            }
        }


        /// <summary>
        /// 加载歌词文件
        /// </summary>
        private async UniTask<string> LoadLyricsAsync(string fileName)
        {
            try
            {
                // 根据音频文件名查找对应的.lrc文件
                string lrcFilePath = Path.Combine(uploadPath, fileName + ".lrc");

#if UNITY_ANDROID && !UNITY_EDITOR
                // Android平台使用UnityWebRequest读取文件
                using (var request = UnityWebRequest.Get($"file://{lrcFilePath}"))
                {
                    await request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"找到对应歌词文件: {fileName}.lrc");
                        return request.downloadHandler.text;
                    }
                    else
                    {
                        Debug.LogWarning($"未找到歌词文件: {fileName}.lrc，将使用默认歌词");
                        return "[00:00.00]正在播放: " + fileName;
                    }
                }
#else
                // 其他平台使用File API
                if (File.Exists(lrcFilePath))
                {
                    string lrcText = File.ReadAllText(lrcFilePath);
                    Debug.Log($"找到对应歌词文件: {fileName}.lrc");
                    return lrcText;
                }
                else
                {
                    Debug.LogWarning($"未找到歌词文件: {fileName}.lrc，将使用默认歌词");
                    return "[00:00.00]正在播放: " + fileName;
                }
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"加载歌词文件失败: {ex.Message}，使用默认歌词");
                return "[00:00.00]正在播放: " + fileName;
            }
        }

        /// <summary>
        /// 使用指定音频和歌词进行播放
        /// </summary>
        private async UniTask PlayAudioWithLyrics(AudioClip audioClip, string fileName)
        {
            GameObject prefabInstance = GameModule.Resource.LoadGameObject("DefaultText");
            var root = GameObject.Find("InstanceRoot");
            var pool = GameObject.Find("InstancePool");


            Debug.Log($"开始播放音频: {fileName}");

            // 1. 获取协调器实例
            var coordinator = GameModule.AUDIO_LYRIC;
            if (coordinator == null)
            {
                Debug.LogError("AudioLyricCoordinator实例获取失败");
                return;
            }

            // 2. 加载对应的歌词文件
            string lrcText = await LoadLyricsAsync(fileName);

            // 3. 启用调试模式
            coordinator.EnableDebugger(true);

            // 4. 设置同步偏移（可选）
            coordinator.SetLyric(root.transform, prefabInstance, pool.transform);
            coordinator.SetSyncOffset(0.1f); // 歌词提前0.1秒显示

            // 5. 配置性能优化
            Debug.Log("配置歌词系统性能优化...");
            GameModule.LYRIC_FX.SetPerformanceConfig(true); // 启用异步回收

            // 启用性能监控
            LyricFX.Utils.PerformanceMonitor.Instance.SetMonitoringEnabled(true);
            Debug.Log($"对象池状态: {GameModule.LYRIC_FX.GetPoolStatus()}");
            Debug.Log($"性能监控状态: {LyricFX.Utils.PerformanceMonitor.Instance.GetCurrentStats()}");

            // 6. 自动发现和初始化协调器
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

            // 7. 订阅事件（可选）
            coordinator.OnPlaybackStarted += () => Debug.Log("[事件] 同步播放已开始");
            coordinator.OnPlaybackStopped += () => Debug.Log("[事件] 同步播放已停止");
            coordinator.OnLyricLineChanged += (lyricLine) => Debug.Log($"当前歌词: {lyricLine}");

            // 8. 准备音频和歌词资源
            bool prepareSuccess = await coordinator.PrepareAudioAndLyrics(audioClip, lrcText);
            if (!prepareSuccess)
            {
                Debug.LogError("准备音频和歌词资源失败");
                return;
            }

            // 9. 开始同步播放
            string effectID = "default_fade";
            coordinator.PlaySynchronized(new Vector3(90, 4, 105), effectID, "multi_line");
            Debug.Log($"开始音频歌词同步播放: {fileName}");

            // 显示优化后的对象池状态
            Debug.Log($"播放开始后对象池状态: {GameModule.LYRIC_FX.GetPoolStatus()}");
            Debug.Log($"当前播放状态: {(coordinator.IsPlaying() ? "播放中" : "已停止")}");

        }

    }
}
