using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// LRC歌词播放测试脚本
    /// 演示如何使用字幕模块播放LRC格式的歌词文件
    /// </summary>
    public class LRCPlayTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool autoStart = false;
        [SerializeField] private float displayDuration = 3f;
        
        [Header("测试LRC内容")]
        [TextArea(10, 20)]
        [SerializeField] private string testLRCContent = @"[ar:测试艺术家]
[ti:测试歌曲]
[al:测试专辑]
[by:LRC制作者]
[offset:0]

[00:12.00]当真相被发现是谎言
[00:17.20]F: 你心中所有的快乐都消失了
[00:21.10]M: 难道你不想要有人来爱吗
[00:24.00]有人来爱
[00:28.25]D: 有人来爱
[00:32.50]你需要有人来爱
[00:36.75]我也需要有人来爱";
        
        private SubtitleModule _subtitleModule;
        
        private void Start()
        {
            // 获取字幕模块实例
            _subtitleModule = SubtitleModule.Instance;
            
            if (autoStart)
            {
                StartCoroutine(DelayedStart());
            }
        }
        
        private IEnumerator DelayedStart()
        {
            // 等待1秒后开始测试
            yield return new WaitForSeconds(1f);
            TestLRCPlayback().Forget();
        }
        
        /// <summary>
        /// 测试LRC播放功能
        /// </summary>
        public async UniTask TestLRCPlayback()
        {
            Log.Info("[LRCPlayTest] 开始LRC播放测试");
            
            try
            {
                // 测试1: 播放LRC内容
                await TestPlayLRCContent();
                
                // 等待一段时间
                await UniTask.Delay(2000);
                
                // 测试2: 测试时序控制器功能
                await TestTimingController();
                
                // 等待一段时间
                await UniTask.Delay(2000);
                
                // 测试3: 测试解析功能
                TestLRCParsing();
                
                Log.Info("[LRCPlayTest] LRC播放测试完成");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[LRCPlayTest] 测试过程中发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试播放LRC内容
        /// </summary>
        private async UniTask TestPlayLRCContent()
        {
            Log.Info("[LRCPlayTest] === 测试1: 播放LRC内容 ===");
            
            // 停止所有现有序列
            _subtitleModule.StopAllSequences();
            
            // 播放LRC内容
            await _subtitleModule.PlayLRCContentAsync(testLRCContent, displayDuration);
        }
        
        /// <summary>
        /// 测试时序控制器功能
        /// </summary>
        private async UniTask TestTimingController()
        {
            Log.Info("[LRCPlayTest] === 测试2: 时序控制器功能 ===");
            
            // 停止所有现有序列
            _subtitleModule.StopAllSequences();
            
            // 解析LRC内容
            var parseResult = LRCLoader.LoadFromString(testLRCContent);
            
            if (parseResult.isValid)
            {
                // 添加到时序控制器
                _subtitleModule.AddLRCToTimingController(parseResult, displayDuration, "test_lrc");
                
                // 开始播放定时字幕
                _subtitleModule.TimingController.PlayTimedSubtitles();
                
                Log.Info("[LRCPlayTest] 时序控制器开始播放，可以通过SeekTo方法跳转到指定时间");
                
                // 演示跳转功能
                await UniTask.Delay(5000); // 等待5秒
                
                Log.Info("[LRCPlayTest] 跳转到20秒位置");
                _subtitleModule.TimingController.SeekTo(20f);
                
                await UniTask.Delay(3000); // 等待3秒
                
                Log.Info("[LRCPlayTest] 暂停播放");
                _subtitleModule.TimingController.PauseTimedSubtitles();
                
                await UniTask.Delay(2000); // 等待2秒
                
                Log.Info("[LRCPlayTest] 恢复播放");
                _subtitleModule.TimingController.ResumeTimedSubtitles();
                
                await UniTask.Delay(3000); // 等待3秒
                
                Log.Info("[LRCPlayTest] 停止播放");
                _subtitleModule.TimingController.StopTimedSubtitles();
            }
        }
        
        /// <summary>
        /// 测试LRC解析功能
        /// </summary>
        private void TestLRCParsing()
        {
            Log.Info("[LRCPlayTest] === 测试3: LRC解析功能 ===");
            
            // 解析LRC内容
            var parseResult = LRCLoader.LoadFromString(testLRCContent);
            
            if (parseResult.isValid)
            {
                Log.Info($"[LRCPlayTest] 解析成功！");
                Log.Info($"[LRCPlayTest] 元数据信息:");
                Log.Info($"[LRCPlayTest]   标题: {parseResult.metadata.title}");
                Log.Info($"[LRCPlayTest]   艺术家: {parseResult.metadata.artist}");
                Log.Info($"[LRCPlayTest]   专辑: {parseResult.metadata.album}");
                Log.Info($"[LRCPlayTest]   制作者: {parseResult.metadata.createdBy}");
                Log.Info($"[LRCPlayTest]   偏移量: {parseResult.metadata.offset}秒");
                
                Log.Info($"[LRCPlayTest] 歌词条目 (共{parseResult.entries.Count}条):");
                foreach (var entry in parseResult.entries)
                {
                    string genderInfo = string.IsNullOrEmpty(entry.gender) ? "" : $" [{entry.gender}]";
                    Log.Info($"[LRCPlayTest]   {entry.timeInSeconds:F2}s{genderInfo}: {entry.lyricText}");
                }
                
                // 转换为字幕配置
                var configs = LRCParser.ConvertToSubtitleConfigs(parseResult, displayDuration);
                Log.Info($"[LRCPlayTest] 转换为{configs.Count}个字幕配置");
                
                // 转换为定时字幕
                var timedSubtitles = LRCParser.ConvertToTimedSubtitles(parseResult, _subtitleModule, displayDuration);
                Log.Info($"[LRCPlayTest] 转换为{timedSubtitles.Count}个定时字幕条目");
            }
            else
            {
                Log.Error($"[LRCPlayTest] 解析失败: {parseResult.errorMessage}");
            }
        }
        
        /// <summary>
        /// 手动触发测试（可在Inspector中调用）
        /// </summary>
        [ContextMenu("开始LRC测试")]
        public void StartTest()
        {
            TestLRCPlayback().Forget();
        }
        
        /// <summary>
        /// 停止所有字幕
        /// </summary>
        [ContextMenu("停止所有字幕")]
        public void StopAllSubtitles()
        {
            _subtitleModule?.StopAllSequences();
            _subtitleModule?.TimingController?.StopTimedSubtitles();
            Log.Info("[LRCPlayTest] 已停止所有字幕");
        }
        
        /// <summary>
        /// 测试从文件加载LRC（需要提供实际文件路径）
        /// </summary>
        [ContextMenu("测试从文件加载LRC")]
        public async void TestLoadFromFile()
        {
            // 示例文件路径，实际使用时需要替换为真实路径
            string filePath = Application.streamingAssetsPath + "/test.lrc";
            
            Log.Info($"[LRCPlayTest] 尝试从文件加载LRC: {filePath}");
            
            try
            {
                await _subtitleModule.PlayLRCFileAsync(filePath, displayDuration);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[LRCPlayTest] 从文件加载失败（这是正常的，因为测试文件可能不存在）: {ex.Message}");
                
                // 如果文件不存在，回退到播放测试内容
                Log.Info("[LRCPlayTest] 回退到播放测试内容");
                await _subtitleModule.PlayLRCContentAsync(testLRCContent, displayDuration);
            }
        }
        
        /// <summary>
        /// 测试从Resources加载LRC
        /// </summary>
        [ContextMenu("测试从Resources加载LRC")]
        public async void TestLoadFromResources()
        {
            // 示例Resources路径
            string resourcePath = "Lyrics/test_song";
            
            Log.Info($"[LRCPlayTest] 尝试从Resources加载LRC: {resourcePath}");
            
            try
            {
                await _subtitleModule.PlayLRCFromResourcesAsync(resourcePath, displayDuration);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[LRCPlayTest] 从Resources加载失败（这是正常的，因为测试资源可能不存在）: {ex.Message}");
                
                // 如果资源不存在，回退到播放测试内容
                Log.Info("[LRCPlayTest] 回退到播放测试内容");
                await _subtitleModule.PlayLRCContentAsync(testLRCContent, displayDuration);
            }
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            
            GUILayout.Label("LRC播放测试控制面板", GUI.skin.box);
            
            if (GUILayout.Button("开始LRC测试"))
            {
                StartTest();
            }
            
            if (GUILayout.Button("播放LRC内容"))
            {
                _subtitleModule?.PlayLRCContentAsync(testLRCContent, displayDuration).Forget();
            }
            
            if (GUILayout.Button("停止所有字幕"))
            {
                StopAllSubtitles();
            }
            
            if (GUILayout.Button("测试从文件加载"))
            {
                TestLoadFromFile();
            }
            
            if (GUILayout.Button("测试从Resources加载"))
            {
                TestLoadFromResources();
            }
            
            GUILayout.Space(10);
            
            GUILayout.Label($"显示时长: {displayDuration:F1}秒");
            displayDuration = GUILayout.HorizontalSlider(displayDuration, 1f, 10f);
            
            GUILayout.EndArea();
        }
    }
}