using Cysharp.Threading.Tasks;
using GameLogic;
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

        // 包含各种处理器的工厂
        [SerializeField] private ProcessorFactory processorFactory;

        // LRC解析器
        [SerializeField] private LrcParser lrcParser;

        public GameObject characterPrefab;
        public Transform poolContainer;
        public int initialPoolSize = 20;
        public int maxPoolSize = 100;

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
            if (characterFactory == null) characterFactory = new CharacterFactory();
            if (processorFactory == null) processorFactory = new ProcessorFactory();
            if (lrcParser == null) lrcParser = new LrcParser();

            // 初始化工厂和注册表
            await characterFactory.Initialize(characterPrefab, poolContainer, initialPoolSize, maxPoolSize);
            await LayoutRegistry.Initialize();
            await EffectRegistry.Initialize();
            await processorFactory.Initialize(characterFactory);

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

        }

        /// <summary>
        /// 创建歌词行
        /// </summary>
        public async UniTask<int> CreateLyricLine(string text, string layoutId, string effectId, Vector3 position, ILayoutConfig config = null)
        {
            // 创建行容器
            GameObject lineContainer = new GameObject($"LyricLine_{lineIdCounter}");
            lineContainer.transform.SetParent(lyricsContainer);
            lineContainer.transform.position = position;

            // 获取布局和效果
            var layoutProvider = LayoutRegistry.GetLayoutProvider(layoutId);

            if (layoutProvider == null)
            {
                Debug.LogError($"[歌词管理器] 未找到布局: {layoutId}, 使用默认布局");
                layoutProvider = LayoutRegistry.GetLayoutProvider("default");
            }

            // 创建行对象
            var lineId = lineIdCounter++;
            var line = new LyricLine
            {
                Id = lineId,
                Text = text,
                LayoutId = layoutProvider.LayoutId,
                EffectId = effectId,
                GameObject = lineContainer,
                Characters = new List<GameObject>()
            };

            activeLines[lineId] = line;

            // 触发行创建事件
            LyricEvents.TriggerLineCreated(new LineEventArgs
            {
                LineId = lineId,
                Content = text,
                EffectId = effectId,
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
                    config,  // 这里可以传入布局配置
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
                    context.SetMetadata("effectId", effectId);

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
        public async UniTask PlayLyricLine(int lineId, IEffectConfig config = null, ICoordinatorConfig coordinatorConfig = null)
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
                line.State = LineState.Playing;

                // 触发行开始事件
                LyricEvents.TriggerLineStarted(new LineEventArgs
                {
                    LineId = lineId,
                    Content = line.Text,
                    EffectId = line.EffectId,
                    LayoutId = line.LayoutId
                });

                // 检查是否需要使用行级协调器
                if (EffectRegistry.RequiresCoordinator(line.EffectId))
                {
                    await PlayWithCoordinator(line,  cts.Token, coordinatorConfig);
                }
                else
                {
                    // 向后兼容：使用原有的字符级效果逻辑
                    await PlayWithCharacterEffects(line, cts.Token, config);
                }

                line.State = LineState.Completed;
                Debug.Log($"[歌词管理器] 行播放完成, ID: {lineId}");
            }
            catch (OperationCanceledException)
            {
                line.State = LineState.Stopped;
                Debug.Log($"[歌词管理器] 行播放被取消: {lineId}");
            }
            catch (Exception ex)
            {
                line.State = LineState.Stopped;
                Debug.LogError($"[歌词管理器] 行播放失败: {ex}");
            }
        }

        /// <summary>
        /// 使用行级协调器播放效果
        /// </summary>
        private async UniTask PlayWithCoordinator(LyricLine line, CancellationToken cancellationToken, ICoordinatorConfig coordinatorConfig)
        {
            // 创建或获取协调器
            if (line.EffectCoordinator == null)
            {
                line.EffectCoordinator = EffectRegistry.CreateCoordinator(line.EffectId);
                if (line.EffectCoordinator == null)
                {
                    Debug.LogError($"[歌词管理器] 无法创建协调器: {line.EffectId}");
                    return;
                }
            }

            // 初始化协调器
            await line.EffectCoordinator.Initialize(line.Container, coordinatorConfig, cancellationToken);

            // 播放效果
            await line.EffectCoordinator.Play(cancellationToken);
        }

        /// <summary>
        /// 使用传统字符级效果播放（向后兼容）
        /// </summary>
        private async UniTask PlayWithCharacterEffects(LyricLine line, CancellationToken cancellationToken, IEffectConfig config)
        {
            var effectProvider = EffectRegistry.GetEffectProvider(line.EffectId);
            if (effectProvider == null)
            {
                Debug.LogWarning($"[歌词管理器] 未找到效果提供器: {line.EffectId}");
                return;
            }

            // 清理之前的效果
            line.CharacterEffects.Clear();

            foreach (var character in line.Characters)
            {
                // 为每个字符创建独立的效果实例
                ILyricEffect characterEffect = CreateCharacterEffect(effectProvider);

                if (characterEffect != null)
                {
                    line.CharacterEffects.Add(characterEffect);
                    await characterEffect.Initialize(character, config, cancellationToken);

                    // 异步播放字符效果
                    UniTask.Void(async () =>
                    {
                        try
                        {
                            await characterEffect.Play(cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // 忽略取消异常 
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 创建字符级效果实例（向后兼容）
        /// </summary>
        private ILyricEffect CreateCharacterEffect(ILyricEffect effectProvider)
        {
            // 使用反射自动创建相同类型的实例，无需硬编码每种效果类型
            Type effectType = effectProvider.GetType();

            try
            {
                // 尝试创建无参构造函数的实例
                return (ILyricEffect)Activator.CreateInstance(effectType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[歌词管理器] 创建效果实例失败: {effectType.Name}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 停止字符级效果（向后兼容）
        /// </summary>
        private async UniTask StopCharacterEffects(LyricLine line)
        {
            // 停止所有字符效果
            foreach (var effect in line.CharacterEffects)
            {
                try
                {
                    await effect.Stop(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[歌词管理器] 停止字符效果失败: {ex.Message}");
                }
            }

            // 清理效果列表
            line.CharacterEffects.Clear();
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
                line.State = LineState.Stopped;

                // 停止效果 - 优先使用协调器
                if (line.EffectCoordinator != null)
                {
                    await line.EffectCoordinator.Stop(CancellationToken.None);
                }
                else
                {
                    // 向后兼容：停止字符级效果
                    await StopCharacterEffects(line);
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
        public async UniTask PlayLrcFile(string content, string layoutId, string effectId)
        {
            try
            {
                // 停止所有当前活动
                StopAll();

                // 解析LRC
                var lyrics = await lrcParser.ParseLrcFile(content);
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
                // 清理协调器
                if (line.EffectCoordinator != null)
                {
                    try
                    {
                        line.EffectCoordinator.Reset();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[歌词管理器] 重置协调器失败: {ex.Message}");
                    }
                    line.EffectCoordinator = null;
                }

                // 清理字符级效果
                foreach (var effect in line.CharacterEffects)
                {
                    try
                    {
                        effect.Reset();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[歌词管理器] 重置字符效果失败: {ex.Message}");
                    }
                }
                line.CharacterEffects.Clear();

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

    }
}
