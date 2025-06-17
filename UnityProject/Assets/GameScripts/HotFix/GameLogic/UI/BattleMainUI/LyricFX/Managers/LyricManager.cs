using Cysharp.Threading.Tasks;
using LyricFX.Core;
using LyricFX.Core.Interfaces;
using LyricFX.Core.Pipeline;
using LyricFX.Factory;
using LyricFX.Parser;
using LyricFX.Processors;
using LyricFX.Registry;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LyricFX.Managers
{
    /// <summary>
    /// 歌词管理器 - 框架核心类，但只负责高层次协调
    /// </summary>
    public class LyricManager : MonoBehaviour
    {
        [SerializeField] private Transform lyricsContainer;
        [SerializeField] private CharacterFactory characterFactory;
        [SerializeField] private LayoutRegistry layoutRegistry;
        [SerializeField] private EffectRegistry effectRegistry;
        
        // 包含各种处理器的工厂
        [SerializeField] private ProcessorFactory processorFactory;
        
        // LRC解析器
        [SerializeField] private LrcParser lrcParser;
        
        // 活动的歌词行
        private Dictionary<int, LyricLine> activeLines = new Dictionary<int, LyricLine>();
        
        // 每行的取消令牌
        private Dictionary<int, CancellationTokenSource> lineCancellations = new Dictionary<int, CancellationTokenSource>();
        
        // 全局取消令牌
        private CancellationTokenSource globalCts;
        
        // 行ID计数器
        private int lineIdCounter = 0;
        
        // 管道实例
        private CharacterProcessingPipeline pipeline;
        
        private void Awake()
        {
            pipeline = new CharacterProcessingPipeline();
        }
        
        public async UniTask Initialize()
        {
            if (characterFactory == null) characterFactory = GetComponentInChildren<CharacterFactory>();
            if (layoutRegistry == null) layoutRegistry = GetComponentInChildren<LayoutRegistry>();
            if (effectRegistry == null) effectRegistry = GetComponentInChildren<EffectRegistry>();
            if (processorFactory == null) processorFactory = GetComponentInChildren<ProcessorFactory>();
            if (lrcParser == null) lrcParser = GetComponentInChildren<LrcParser>();
            
            // 初始化工厂和注册表
            await characterFactory.Initialize();
            await layoutRegistry.Initialize();
            await effectRegistry.Initialize();
            await processorFactory.Initialize();
            
            // 注册默认处理器
            await RegisterDefaultProcessors();
            
            // 通知初始化完成
            LyricEvents.InitializationCompleted.TrySetResult(true);
            
            Debug.Log("[歌词管理器] 初始化完成");
        }
        
        private async UniTask RegisterDefaultProcessors()
        {
            // 注册默认处理器 - 通常顺序为：创建 -> 布局 -> 效果
            pipeline.RegisterProcessor(await processorFactory.CreateProcessor<CharacterCreationProcessor>());
            pipeline.RegisterProcessor(await processorFactory.CreateProcessor<LayoutApplicationProcessor>());
            pipeline.RegisterProcessor(await processorFactory.CreateProcessor<EffectApplicationProcessor>());
            
            // 其他可选处理器
            // pipeline.RegisterProcessor(await processorFactory.CreateProcessor<DelayProcessor>());
            // pipeline.RegisterProcessor(await processorFactory.CreateProcessor<ProgressUpdateProcessor>());
        }
        
        /// <summary>
        /// 创建歌词行
        /// </summary>
        public async UniTask<int> CreateLyricLine(string text, string layoutId, string effectId, Vector3 position)
        {
            // 创建行容器
            GameObject lineContainer = new GameObject($"LyricLine_{lineIdCounter}");
            lineContainer.transform.SetParent(lyricsContainer);
            lineContainer.transform.position = position;
            
            // 获取布局和效果
            var layoutProvider = layoutRegistry.GetLayoutProvider(layoutId);
            var effectProvider = effectRegistry.GetEffectProvider(effectId);
            
            if (layoutProvider == null)
            {
                Debug.LogError($"[歌词管理器] 未找到布局: {layoutId}, 使用默认布局");
                layoutProvider = layoutRegistry.GetLayoutProvider("default");
            }
            
            if (effectProvider == null)
            {
                Debug.LogError($"[歌词管理器] 未找到效果: {effectId}, 使用默认效果");
                effectProvider = effectRegistry.GetEffectProvider("default");
            }
            
            // 创建行对象
            var lineId = lineIdCounter++;
            var line = new LyricLine
            {
                Id = lineId,
                Text = text,
                LayoutId = layoutProvider.LayoutId,
                EffectId = effectProvider.EffectId,
                GameObject = lineContainer,
                Characters = new List<GameObject>()
            };
            
            activeLines[lineId] = line;
            
            // 触发行创建事件
            LyricEvents.TriggerLineCreated(new LineEventArgs 
            { 
                LineId = lineId, 
                Content = text,
                EffectId = effectProvider.EffectId,
                LayoutId = layoutProvider.LayoutId
            });
            
            // 创建取消令牌
            var cts = new CancellationTokenSource();
            lineCancellations[lineId] = cts;
            
            try
            {
                // 计算布局位置
                var positions = await layoutProvider.CalculateLayout(
                    text, 
                    lineContainer.transform, 
                    null,  // 这里可以传入布局配置
                    cts.Token
                );
                
                // 触发布局计算事件
                LyricEvents.TriggerLayoutCalculated(new LayoutEventArgs
                {
                    LineId = lineId,
                    Positions = positions
                });
                
                // 为每个字符创建上下文
                var contexts = new List<ProcessingContext>();
                for (int i = 0; i < text.Length; i++)
                {
                    var context = ProcessingContext.Create(
                        null,  // 字符对象稍后由管道创建
                        i,
                        text[i],
                        lineId,
                        positions[i]
                    );
                    
                    // 添加布局和效果信息
                    context.SetMetadata("layoutId", layoutProvider.LayoutId);
                    context.SetMetadata("effectId", effectProvider.EffectId);
                    
                    contexts.Add(context);
                }
                
                // 逐个处理字符
                var results = await pipeline.ProcessCharacters(contexts, false, cts.Token);
                
                // 收集处理后的字符对象
                foreach (var result in results)
                {
                    if (result.CharacterObject != null)
                    {
                        line.Characters.Add(result.CharacterObject);
                    }
                }
                
                Debug.Log($"[歌词管理器] 创建行完成, ID: {lineId}, 字符数: {line.Characters.Count}");
                return lineId;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[歌词管理器] 创建行被取消: {lineId}");
                CleanupLine(lineId);
                return -1;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[歌词管理器] 创建行失败: {ex}");
                CleanupLine(lineId);
                return -1;
            }
        }
        
        /// <summary>
        /// 播放歌词行
        /// </summary>
        public async UniTask PlayLyricLine(int lineId)
        {
            if (!activeLines.TryGetValue(lineId, out var line))
            {
                Debug.LogError($"[歌词管理器] 未找到行: {lineId}");
                return;
            }
            
            if (!lineCancellations.TryGetValue(lineId, out var cts))
            {
                cts = new CancellationTokenSource();
                lineCancellations[lineId] = cts;
            }
            
            try
            {
                // 触发行开始事件
                LyricEvents.TriggerLineStarted(new LineEventArgs
                {
                    LineId = lineId,
                    Content = line.Text,
                    EffectId = line.EffectId,
                    LayoutId = line.LayoutId
                });
                
                // 应用效果
                var effectProvider = effectRegistry.GetEffectProvider(line.EffectId);
                if (effectProvider != null)
                {
                    foreach (var character in line.Characters)
                    {
                        await effectProvider.Initialize(character, null, cts.Token);
                        UniTask.Void(async () => 
                        {
                            try 
                            {
                                await effectProvider.Play(cts.Token);
                            }
                            catch (OperationCanceledException) 
                            { 
                                // 忽略取消异常 
                            }
                        });
                    }
                }
                
                Debug.Log($"[歌词管理器] 行播放开始, ID: {lineId}");
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[歌词管理器] 行播放被取消: {lineId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[歌词管理器] 行播放失败: {ex}");
            }
        }
        
        /// <summary>
        /// 停止歌词行
        /// </summary>
        public async UniTask StopLyricLine(int lineId)
        {
            if (!activeLines.TryGetValue(lineId, out var line))
            {
                Debug.LogError($"[歌词管理器] 未找到行: {lineId}");
                return;
            }
            
            // 取消当前操作
            if (lineCancellations.TryGetValue(lineId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                lineCancellations.Remove(lineId);
            }
            
            try
            {
                // 停止效果
                var effectProvider = effectRegistry.GetEffectProvider(line.EffectId);
                if (effectProvider != null)
                {
                    foreach (var character in line.Characters)
                    {
                        await effectProvider.Stop(CancellationToken.None);
                    }
                }
                
                // 触发行完成事件
                LyricEvents.TriggerLineCompleted(new LineEventArgs
                {
                    LineId = lineId,
                    Content = line.Text,
                    EffectId = line.EffectId,
                    LayoutId = line.LayoutId
                });
                
                Debug.Log($"[歌词管理器] 行停止, ID: {lineId}");
                
                // 清理行
                CleanupLine(lineId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[歌词管理器] 停止行失败: {ex}");
                // 强制清理
                CleanupLine(lineId);
            }
        }
        
        /// <summary>
        /// 播放LRC文件
        /// </summary>
        public async UniTask PlayLrcFile(string filePath, string layoutId, string effectId)
        {
            try
            {
                // 停止所有当前活动
                StopAll();
                
                // 解析LRC
                var lyrics = await lrcParser.ParseLrcFile(filePath);
                if (lyrics == null || lyrics.Count == 0)
                {
                    Debug.LogError("[歌词管理器] LRC解析失败或为空");
                    return;
                }
                
                // 创建新的全局取消令牌
                globalCts = new CancellationTokenSource();
                
                // 播放歌词序列
                await PlayLyricSequence(lyrics, layoutId, effectId, globalCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[歌词管理器] LRC播放被取消");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[歌词管理器] 播放LRC失败: {ex}");
            }
        }
        
        /// <summary>
        /// 播放歌词序列
        /// </summary>
        private async UniTask PlayLyricSequence(List<LrcLine> lyrics, string layoutId, string effectId, CancellationToken cancellationToken)
        {
            float currentTime = 0;
            
            for (int i = 0; i < lyrics.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var line = lyrics[i];
                
                // 等待到播放时间
                float waitTime = (float)line.TimeStamp - currentTime;
                if (waitTime > 0)
                {
                    try
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                
                currentTime = (float)line.TimeStamp;
                
                // 清理之前的行
                ClearPreviousLines();
                
                // 创建并播放当前行
                Vector3 position = new Vector3(0, 0, 0); // 可以根据需要调整位置
                int lineId = await CreateLyricLine(line.Text, layoutId, effectId, position);
                
                if (lineId >= 0)
                {
                    await PlayLyricLine(lineId);
                    
                    // 如果有下一行，计算显示时长
                    if (i + 1 < lyrics.Count)
                    {
                        float duration = (float)(lyrics[i + 1].TimeStamp - line.TimeStamp);
                        if (duration > 0)
                        {
                            try
                            {
                                // 保持显示一段时间
                                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        // 最后一行，显示一段默认时间
                        try
                        {
                            await UniTask.Delay(3000, cancellationToken: cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            
            Debug.Log("[歌词管理器] 歌词序列播放完成");
        }
        
        /// <summary>
        /// 清理特定行
        /// </summary>
        private void CleanupLine(int lineId)
        {
            if (activeLines.TryGetValue(lineId, out var line))
            {
                // 回收字符对象
                foreach (var character in line.Characters)
                {
                    characterFactory.ReleaseCharacter(character);
                }
                
                // 销毁行对象
                if (line.GameObject != null)
                {
                    Destroy(line.GameObject);
                }
                
                activeLines.Remove(lineId);
            }
            
            // 清理取消令牌
            if (lineCancellations.TryGetValue(lineId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                lineCancellations.Remove(lineId);
            }
        }
        
        /// <summary>
        /// 清理之前的所有行
        /// </summary>
        private void ClearPreviousLines()
        {
            foreach (var lineId in new List<int>(activeLines.Keys))
            {
                CleanupLine(lineId);
            }
        }
        
        /// <summary>
        /// 停止所有活动
        /// </summary>
        public void StopAll()
        {
            // 取消全局操作
            if (globalCts != null)
            {
                globalCts.Cancel();
                globalCts.Dispose();
                globalCts = null;
            }
            
            // 清理所有行
            ClearPreviousLines();
            
            Debug.Log("[歌词管理器] 停止所有活动");
        }
        
        private void OnDestroy()
        {
            StopAll();
        }
        
        // 内部类 - 表示一行歌词
        private class LyricLine
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public string LayoutId { get; set; }
            public string EffectId { get; set; }
            public GameObject GameObject { get; set; }
            public List<GameObject> Characters { get; set; }
        }
    }
}
