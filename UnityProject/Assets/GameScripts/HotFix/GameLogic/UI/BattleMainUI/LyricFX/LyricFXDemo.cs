/*
 * @Author: shenhang 2322974451@qq.com
 * @Date: 2025-06-17 18:25:23
 * @LastEditors: shenhang 2322974451@qq.com
 * @LastEditTime: 2025-06-19 11:20:57
 * @FilePath: \LyricFX\LyricFXDemo.cs
 * @Description: 歌词特效演示脚本，集成调试功能
 */
using Cysharp.Threading.Tasks;
using LyricFX.Core;
using LyricFX.Implementations.Effect;
using LyricFX.Implementations.Layout;
using LyricFX.Managers;
using LyricFX.Utils;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace LyricFX
{
    /// <summary>
    /// 歌词特效演示脚本
    /// </summary>
    public class LyricFXDemo : MonoBehaviour
    {
        [Header("管理器引用")]
        [SerializeField] private LyricManager lyricManager;
        
        [Header("调试设置")]
        [SerializeField] private bool enableDebugger = false;
        [SerializeField] private float audioStartDelay = 0.1f; // 音频延迟播放时间(秒)
        [SerializeField] private float syncOffset = 0.0f; // 歌词同步偏移(秒), 正值延迟歌词, 负值提前歌词
        
        [Header("示例文本")]
        [SerializeField] private string[] sampleLyrics = new string[]
        {
            "这是一行示例歌词",
            "这是第二行示例歌词",
            "这是带有特殊效果的歌词"
        };
        
        [Header("示例LRC文件")]
        [SerializeField] private string lrcFilePath = "Lyrics/Sample";
        
        [Header("UI控制")]
        [SerializeField] private Button playLineButton;
        [SerializeField] private Button playLrcButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Dropdown effectDropdown;
        [SerializeField] private Dropdown layoutDropdown;
        
        // 当前选择的效果和布局
        private string currentEffectId = "default_fade";
        private string currentLayoutId = "default_linear";
        
        // 活动行ID
        private int activeLyricLineId = -1;
        public TextAsset LRCFile;
        // 取消令牌
        private CancellationTokenSource demoCts;

        private string LRC_CONTENT
        {
            get
            {
                return LRCFile.text;
            }
        }
        public AudioSource Audio;

        private void Awake()
        {
            if (lyricManager == null)
                lyricManager = GetComponentInChildren<LyricManager>();
                
            // 初始化下拉菜单
            InitializeDropdowns();
            
            // 绑定按钮事件
            if (playLineButton != null)
                playLineButton.onClick.AddListener(PlaySampleLine);
                
            if (playLrcButton != null)
                playLrcButton.onClick.AddListener(PlaySampleLrc);
                
            if (stopButton != null)
                stopButton.onClick.AddListener(StopDemo);
                
            // 初始化调试工具
            LyricFXDebugger.Instance.EnableDebug = enableDebugger;
            if (enableDebugger)
            {
                Debug.Log("[歌词特效演示] 调试模式已启用，将记录详细执行时间");
            }
            
            // 配置LyricManager的同步偏移
            if (lyricManager != null && syncOffset != 0)
            {
                lyricManager.SetSyncOffset(syncOffset);
            }
        }
        
        private void Start()
        {
            // 初始化系统
            InitializeAsync().Forget();
        }
        
        /// <summary>
        /// 初始化系统
        /// </summary>
        private async UniTaskVoid InitializeAsync()
        {
            demoCts = new CancellationTokenSource();
            
            try
            {
                // 等待管理器初始化完成
                await lyricManager.Initialize();
                
                Debug.Log("[歌词特效演示] 初始化完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[歌词特效演示] 初始化失败: {ex}");
            }
        }
        
        /// <summary>
        /// 初始化下拉菜单
        /// </summary>
        private void InitializeDropdowns()
        {
            // 效果下拉菜单
            if (effectDropdown != null)
            {
                effectDropdown.ClearOptions();
                effectDropdown.AddOptions(new System.Collections.Generic.List<string> 
                { 
                    "默认淡入淡出 (default_fade)",
                    "随机颜色渐变 (random_color_fade)",
                    "从左到右渐变 (left_to_right_fade)",
                    "随机批量淡入淡出 (random_batch_fade)",
                    "字符抖动效果 (shake_character)",
                    "随机模糊 (random_blur)",
                });
                
                effectDropdown.onValueChanged.AddListener((index) => {
                    switch (index)
                    {
                        case 0: currentEffectId = "default_fade"; break;
                        case 1: currentEffectId = "random_color_fade"; break;
                        case 2: currentEffectId = "left_to_right_fade"; break;
                        case 3: currentEffectId = "random_batch_fade"; break;
                        case 4: currentEffectId = "shake_character"; break;
                        case 5: currentEffectId = "random_blur"; break;
                        default: currentEffectId = "default_fade"; break;
                    }
                });
            }
            
            // 布局下拉菜单
            if (layoutDropdown != null)
            {
                layoutDropdown.ClearOptions();
                layoutDropdown.AddOptions(new System.Collections.Generic.List<string> 
                { 
                    "默认线性布局 (default_linear)",
                    // 可以添加更多布局选项
                });
                
                layoutDropdown.onValueChanged.AddListener((index) => {
                    switch (index)
                    {
                        case 0: currentLayoutId = "default_linear"; break;
                        default: currentLayoutId = "default_linear"; break;
                    }
                });
            }
        }
        
        /// <summary>
        /// 播放示例行
        /// </summary>
        private async void PlaySampleLine()
        {
            StopCurrentLine();
            
            int randomIndex = Random.Range(0, sampleLyrics.Length);
            string lyric = sampleLyrics[randomIndex];
            
            Debug.Log($"[歌词特效演示] 播放单行歌词: '{lyric}', 效果: {currentEffectId}, 布局: {currentLayoutId}");
            
            // 开始调试会话
            if (enableDebugger)
            {
                LyricFXDebugger.Instance.StartSession("单行歌词播放");
                LyricFXDebugger.Instance.SetCurrentLyric(lyric);
                LyricFXDebugger.Instance.RecordTimePoint("开始创建歌词行");
            }
            
            // 创建歌词行
            Vector3 position = new Vector3(0, 0, 0);
            activeLyricLineId = await lyricManager.CreateLyricLine(lyric, currentLayoutId, currentEffectId, position);

            if (activeLyricLineId >= 0)
            {
                if (enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("歌词行创建完成");
                    LyricFXDebugger.Instance.RecordStageDuration("开始创建歌词行", "歌词行创建完成", "歌词行创建耗时");
                    LyricFXDebugger.Instance.RecordTimePoint("开始播放歌词行");
                }
                
                // 播放歌词行
                await lyricManager.PlayLyricLine(activeLyricLineId);
                
                if (enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("歌词行播放完成");
                    LyricFXDebugger.Instance.RecordStageDuration("开始播放歌词行", "歌词行播放完成", "歌词行播放耗时");
                }
                
                // 5秒后自动停止
                await UniTask.Delay(5000, cancellationToken: demoCts.Token);
                await lyricManager.StopLyricLine(activeLyricLineId);
                activeLyricLineId = -1;
                
                if (enableDebugger)
                {
                    LyricFXDebugger.Instance.EndSession("单行歌词播放完成");
                }
            }
            else if (enableDebugger)
            {
                LyricFXDebugger.Instance.RecordError("创建歌词行失败");
                LyricFXDebugger.Instance.EndSession("单行歌词播放失败");
            }
        }
        
        /// <summary>
        /// 播放示例LRC
        /// </summary>
        private async void PlaySampleLrc()
        {
            StopCurrentLine();
            
            Debug.Log($"[歌词特效演示] 播放LRC文件, 效果: {currentEffectId}, 布局: {currentLayoutId}");
            
            // 开始调试会话
            if (enableDebugger)
            {
                LyricFXDebugger.Instance.StartSession("LRC文件播放");
                LyricFXDebugger.Instance.RecordTimePoint("开始处理LRC");
            }
            
            try
            {
                // 在播放音频前开始处理LRC
                var processTask = lyricManager.PlayLrcFile(LRC_CONTENT, currentLayoutId, currentEffectId);
                
                if (enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("LRC处理开始");
                }
                
                // 延迟音频开始，以便为初始处理留出时间
                await UniTask.Delay(TimeSpan.FromSeconds(audioStartDelay));
                
                if (enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("音频开始播放");
                }
                
                // 开始播放音频
                Audio.Play();
                
                // 等待LRC处理完成
                await processTask;
                
                if (enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("LRC播放完成");
                    LyricFXDebugger.Instance.RecordStageDuration("开始处理LRC", "LRC播放完成", "LRC总播放耗时");
                    LyricFXDebugger.Instance.EndSession("LRC文件播放完成");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[歌词特效演示] 播放LRC文件失败: {ex}");
                
                if (enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordError("播放LRC文件失败", ex);
                    LyricFXDebugger.Instance.EndSession("LRC播放出错");
                }
            }
        }
        
        /// <summary>
        /// 停止当前行
        /// </summary>
        private void StopCurrentLine()
        {
            if (activeLyricLineId >= 0)
            {
                lyricManager.StopLyricLine(activeLyricLineId).Forget();
                activeLyricLineId = -1;
            }
        }
        
        /// <summary>
        /// 停止演示
        /// </summary>
        private void StopDemo()
        {
            Debug.Log("[歌词特效演示] 停止所有活动");
            
            lyricManager.StopAll();
            activeLyricLineId = -1;
        }
        
        /// <summary>
        /// 代码示例：如何编程方式使用框架
        /// </summary>
        public async UniTask ProgrammaticExample()
        {
            // 1. 从这个MonoBehaviour获取或找到LyricManager实例
            var manager = FindObjectOfType<LyricManager>();
            
            // 2. 初始化管理器（如果尚未初始化）
            await manager.Initialize();
            
            // 3. 创建和播放单行歌词
            int lineId = await manager.CreateLyricLine(
                "这是通过代码创建的歌词行",  // 文本内容
                "default_linear",            // 布局ID
                "blur_font",                 // 效果ID
                Vector3.zero                 // 位置
            );
            
            // 4. 播放歌词行
            await manager.PlayLyricLine(lineId);
            
            // 5. 等待一段时间
            await UniTask.Delay(3000);
            
            // 6. 停止歌词行
            await manager.StopLyricLine(lineId);
            
            // 7. 播放完整LRC文件
            await manager.PlayLrcFile(
                LRC_CONTENT,  // LRC文件路径
                "default_linear",                      // 布局ID
                "default_fade"                         // 效果ID
            );
            
            // 8. 停止所有活动
            manager.StopAll();
        }
        
        private void OnDestroy()
        {
            // 取消所有异步操作
            if (demoCts != null)
            {
                demoCts.Cancel();
                demoCts.Dispose();
            }
            
            // 解绑按钮事件
            if (playLineButton != null)
                playLineButton.onClick.RemoveAllListeners();
                
            if (playLrcButton != null)
                playLrcButton.onClick.RemoveAllListeners();
                
            if (stopButton != null)
                stopButton.onClick.RemoveAllListeners();
                
            if (effectDropdown != null)
                effectDropdown.onValueChanged.RemoveAllListeners();
                
            if (layoutDropdown != null)
                layoutDropdown.onValueChanged.RemoveAllListeners();
        }
    }
}
