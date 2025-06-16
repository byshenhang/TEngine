using UnityEngine;
using GameLogic;
using Cysharp.Threading.Tasks;

namespace GameLogic.Examples
{
    /// <summary>
    /// 歌词单行复用模式示例
    /// </summary>
    public class LyricSingleLineReuseExample : MonoBehaviour
    {
        [Header("歌词配置")]
        [SerializeField] private bool useSingleLineReuse = true;
        [SerializeField] private string lrcFilePath = "Assets/StreamingAssets/Lyrics/example.lrc";
        
        private LyricModule _lyricModule;
        
        private void Start()
        {
            _lyricModule = LyricModule.Instance;
            
            // 创建示例歌词数据
            var lyricData = CreateExampleLyricData();
            
            // 创建配置
            var config = CreateLyricConfig();
            
            // 播放歌词
            PlayLyricAsync(lyricData, config).Forget();
        }
        
        /// <summary>
        /// 创建示例歌词数据
        /// </summary>
        private LyricData CreateExampleLyricData()
        {
            var lyricData = new LyricData();
            
            // 添加示例歌词行
            lyricData.Lines.Add(new LyricLineData { Text = "第一行歌词内容", Time = 0f });
            lyricData.Lines.Add(new LyricLineData { Text = "第二行歌词内容", Time = 3f });
            lyricData.Lines.Add(new LyricLineData { Text = "第三行歌词内容", Time = 6f });
            lyricData.Lines.Add(new LyricLineData { Text = "第四行歌词内容", Time = 9f });
            lyricData.Lines.Add(new LyricLineData { Text = "第五行歌词内容", Time = 12f });
            
            return lyricData;
        }
        
        /// <summary>
        /// 创建歌词配置
        /// </summary>
        private LyricConfig CreateLyricConfig()
        {
            var config = LyricConfig.Default;
            
            // 设置显示模式
            config.DisplayMode = useSingleLineReuse ? LyricDisplayMode.SingleLineReuse : LyricDisplayMode.MultiLine;
            
            // 自定义其他配置
            config.FontSize = 36;
            config.DefaultColor = Color.white;
            config.HighlightColor = Color.cyan;
            
            return config;
        }
        
        /// <summary>
        /// 播放歌词
        /// </summary>
        private async UniTask PlayLyricAsync(LyricData lyricData, LyricConfig config)
        {
            await _lyricModule.PlayLyric(lyricData, config);
        }
        
        private void Update()
        {
            // 简单的时间控制
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_lyricModule.IsPlaying)
                {
                    _lyricModule.PauseLyric();
                    Debug.Log("歌词已暂停");
                }
                else
                {
                    _lyricModule.ResumeLyric();
                    Debug.Log("歌词已恢复");
                }
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                _lyricModule.StopLyric();
                Debug.Log("歌词已停止");
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                // 重新播放
                var lyricData = CreateExampleLyricData();
                var config = CreateLyricConfig();
                PlayLyricAsync(lyricData, config).Forget();
                Debug.Log("重新播放歌词");
            }
            
            if (Input.GetKeyDown(KeyCode.M))
            {
                // 切换显示模式
                useSingleLineReuse = !useSingleLineReuse;
                var lyricData = CreateExampleLyricData();
                var config = CreateLyricConfig();
                PlayLyricAsync(lyricData, config).Forget();
                Debug.Log($"切换到 {(useSingleLineReuse ? "单行复用" : "多行")} 模式");
            }
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            
            GUILayout.Label($"当前模式: {(useSingleLineReuse ? "单行复用" : "多行")}");
            GUILayout.Label($"播放状态: {(_lyricModule.IsPlaying ? "播放中" : "已停止")}");
            GUILayout.Label($"当前时间: {_lyricModule.CurrentTime:F1}s");
            
            GUILayout.Space(10);
            GUILayout.Label("控制说明:");
            GUILayout.Label("空格键: 暂停/恢复");
            GUILayout.Label("S键: 停止");
            GUILayout.Label("R键: 重新播放");
            GUILayout.Label("M键: 切换模式");
            
            GUILayout.EndArea();
        }
    }
}