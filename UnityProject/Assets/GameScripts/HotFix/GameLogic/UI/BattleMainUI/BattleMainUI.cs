using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TEngine;
using LyricFX.Managers;
using Cysharp.Threading.Tasks;
using TelePresent.AudioSyncPro;
using System.Collections.Generic;
using System.IO;
using TMPro;
using AudioType = UnityEngine.AudioType;
using System.Net;
using System.Net.Sockets;
using System;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class BattleMainUI : UI3DWindow
    {
        #region 脚本工具生成的代码
        private Button _btn_debug;
        private ScrollRect _scrollView;
        private Transform _content;
        private GameObject _itemTemplate;
        private Button _btn_share;

        private GameObject _shareWindow;
        private TextMeshProUGUI _shareIPText;

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
            }
            catch
            {
                // 保持原有异常抛出行为
                throw;
            }
        }

        private void OnClick_shareBtn()
        {
            _shareWindow.SetActive(!_shareWindow.activeSelf);
            _scrollView.gameObject.SetActive(false);
            _shareIPText.text = GetLocalIPAddress();
        }

        /// <summary>
        /// 打印子对象层级结构
        /// </summary>
        private void LogChildrenHierarchy(Transform parent, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth || parent == null) return;
            
            string indent = new string(' ', currentDepth * 2);
            Debug.Log($"[BattleMainUI] {indent}├─ {parent.name} (激活: {parent.gameObject.activeInHierarchy})");
            
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                LogChildrenHierarchy(child, currentDepth + 1, maxDepth);
            }
        }
        #endregion

        #region 事件


        private async UniTask PlayAsync()
        {
            GameObject prefabInstance = GameModule.Resource.LoadGameObject("DefaultText");
            var root = GameObject.Find("InstanceRoot");
            var pool = GameObject.Find("InstancePool");

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
                //var audioClip = GameModule.Resource.LoadAsset<AudioClip>("XUNZHANG_AUDIO");
                var audioClip = GameModule.Resource.LoadAsset<AudioClip>("KIDDO - My 100_AUDIO");
                //var audioClip = loadclip;
                //var lrcText = GameModule.Resource.LoadAsset<TextAsset>("XUNZHANG").text;
                var lrcText = GameModule.Resource.LoadAsset<TextAsset>("My 100 - KIDDO LRC").text;

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

                // 显示所有发现的AudioReactor信息
                var discoveredReactors = coordinator.GetDiscoveredAudioReactors();

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
                //coordinator.PlaySynchronized(new Vector3(90, 4, 105), effectID);
                coordinator.PlaySynchronized(new Vector3(90, 4, 105), effectID, "multi_line");
                Debug.Log("开始音频歌词同步播放");

                // 显示优化后的对象池状态
                Debug.Log($"播放开始后对象池状态: {GameModule.LYRIC_FX.GetPoolStatus()}");

                Debug.Log($"当前播放状态: {(coordinator.IsPlaying() ? "播放中" : "已停止")}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"AudioLyricCoordinator同步播放过程中发生错误: {ex}");
            }
        }


        /// <summary>
        /// 调试按钮点击事件 - 激活/关闭 Scroll View
        /// </summary>
        private void OnClick_debugBtn()
        {
            bool isActive = _scrollView.gameObject.activeSelf;
            _scrollView.gameObject.SetActive(!isActive);
            
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
                
                // 设置文本
                TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = fileName;
                }
                
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
            
            try
            {
                // 使用UnityWebRequest加载音频文件
                using (var request = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip($"file://{filePath}", AudioType.MPEG))
                {
                    await request.SendWebRequest();
                    
                    if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        AudioClip audioClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
                        audioClip.name = fileName;
                        
                        Debug.Log($"成功加载音频: {fileName}, 长度: {audioClip.length}秒");
                        
                        // 开始播放音频（这里可以调用原来的PlayAsync逻辑，但需要传入加载的audioClip）
                        await PlayAudioWithLyrics(audioClip, fileName);
                    }
                    else
                    {
                        Debug.LogError($"加载音频失败: {request.error}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"播放音频时发生错误: {ex.Message}");
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

            try
            {
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
            catch (System.Exception ex)
            {
                Debug.LogError($"AudioLyricCoordinator同步播放过程中发生错误: {ex}");
            }
        }
        #endregion

    }
}
