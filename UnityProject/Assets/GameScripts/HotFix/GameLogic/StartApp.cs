using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class StartApp : MonoBehaviour
    {
        private Button button;
        private Text text;
        void Start()
        {
            button = GetComponentInChildren<Button>();
            text = GetComponentInChildren<Text>();
            text.text += "-注册绑定";
            button.onClick.AddListener( () =>
            {
                text.text += "-触发点击"; 
                PlayAsync().Forget();
            });
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
                var audioClip = GameModule.Resource.LoadAsset<AudioClip>("XUNZHANG_AUDIO");
                var lrcText = GameModule.Resource.LoadAsset<TextAsset>("XUNZHANG").text;
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
                coordinator.EnableDebugger(false);
                // 4. 设置同步偏移（可选）
                coordinator.SetLyric(root.transform, prefabInstance, pool.transform);
                coordinator.SetSyncOffset(0.1f); // 歌词提前0.1秒显示

                // 5. 自动发现和初始化协调器
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
                //Debug.Log($"发现了 {discoveredReactors.Count} 个AudioReactor:");
                //foreach (var kvp in discoveredReactors)
                //{
                //    Debug.Log($"  - {kvp.Value} (ID: {kvp.Key})");
                //}

                // 6. 订阅事件（可选）
                coordinator.OnPlaybackStarted += () => Debug.Log("[事件] 同步播放已开始");
                coordinator.OnPlaybackStopped += () => Debug.Log("[事件] 同步播放已停止");
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
            }
            catch (System.Exception ex)
            {
                text.text += "\n" + ex.ToString();
                Debug.LogError($"AudioLyricCoordinator同步播放过程中发生错误: {ex}");
            }
        }
      
    }
}
