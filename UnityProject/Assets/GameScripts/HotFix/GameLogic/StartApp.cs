using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace GameLogic
{
    public class StartApp : MonoBehaviour
    {
        private Button button;
        private Text text;
        public string audioURL = "https://music.163.com/song/media/outer/url?id=1403318151.mp3"; // 替换成你的在线音频地址
        private AudioClip loadclip;

        void Start()
        {
            //GameModule.UI3D.ShowUI3DAtAnchor<BattleMainUI>("MainUI", null);
            //button = GetComponentInChildren<Button>();
            //text = GetComponentInChildren<Text>();
            //text.text += "-注册绑定";
            //button.onClick.AddListener( () =>
            //{
            //    text.text += "-触发点击"; 
            //    PlayAsync().Forget();
            //});
            //StartCoroutine(DownloadAudioToLocal(audioURL));
        }


        private async UniTask PlayAsync()
        {
            text.text += "-PlayAsync";
            GameObject prefabInstance = GameModule.Resource.LoadGameObject("DefaultText");
            text.text += "-加载对象";
            var root = GameObject.Find("InstanceRoot");
            var pool = GameObject.Find("InstancePool");

            try
            {
                Debug.Log("开始使用AudioLyricCoordinator进行音频歌词同步播放");
                text.text += "-开始使用AudioLyric";
                // 1. 获取协调器实例
                var coordinator = GameModule.AUDIO_LYRIC;
                if (coordinator == null)
                {
                    text.text += "-AudioLyric实例获取失败";
                    Debug.LogError("AudioLyricCoordinator实例获取失败");
                    return;
                }

                // 2. 准备音频和歌词资源
                //var audioClip = GameModule.Resource.LoadAsset<AudioClip>("XUNZHANG_AUDIO");
                var audioClip = GameModule.Resource.LoadAsset<AudioClip>("KIDDO - My 100_AUDIO");
                //var audioClip = loadclip;
                //var lrcText = GameModule.Resource.LoadAsset<TextAsset>("XUNZHANG").text;
                var lrcText = GameModule.Resource.LoadAsset<TextAsset>("My 100 - KIDDO LRC").text;
                text.text += "-加载音频";

                if (audioClip == null)
                {
                    text.text += "-音频资源加载失败";
                    Debug.LogError("音频资源加载失败: 勋章 - 鹿晗");
                    return;
                }

                if (string.IsNullOrEmpty(lrcText))
                {
                    text.text += "-歌词资源加载失败";
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
                    text.text += "-AudioLyricCoordinator自动初始化失败";
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
                text.text += "\n" + ex.ToString();
                Debug.LogError($"AudioLyricCoordinator同步播放过程中发生错误: {ex}");
            }
        }

        IEnumerator DownloadAudioToLocal(string url)
        {
            string fileName = Path.GetFileName(new System.Uri(url).AbsolutePath);
            var audioFolder = Path.Combine(Application.persistentDataPath, "AudioCache");
            string localPath = Path.Combine(audioFolder, fileName);

            if (!Directory.Exists(audioFolder))
            {
                Directory.CreateDirectory(audioFolder);
            }


            if (!File.Exists(localPath))
            {

                using (UnityWebRequest uwr = UnityWebRequest.Get(url))
                {
                    yield return uwr.SendWebRequest();

                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError("下载失败: " + uwr.error);
                        yield break;
                    }

                    File.WriteAllBytes(localPath, uwr.downloadHandler.data);
                    Debug.Log("下载完成: " + localPath);
                }

            }



            string fileUrl = "file://" + localPath;

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.MPEG))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("加载失败: " + uwr.error);
                    yield break;
                }

                loadclip = DownloadHandlerAudioClip.GetContent(uwr);
            }


        }


    }
}
