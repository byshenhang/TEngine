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
using System.Threading.Tasks;
using UnityEngine;

namespace LyricFX.Managers
{
    /// <summary>
    /// 歌词管理器 - 框架核心类，但只负责高层次协调
    /// </summary>
    public class LyricManager 
    {
        private CharacterFactory characterFactory;
        // 包含各种处理器的工厂
        private ProcessorFactory processorFactory;
        // LRC解析器
        private LrcParser lrcParser;
        // 歌词同步偏移(秒)：正值延迟歌词，负值提前歌词
        private float syncOffset = 0.0f;

        private Transform lyricsContainer;
        private GameObject characterPrefab;
        private Transform poolContainer;
        private int initialPoolSize = 20;
        private int maxPoolSize = 100;

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

        // 播放会话开始时间
        private float playSessionStartTime;

        public LyricManager()
        {
            pipeline = new CharacterProcessingPipeline();
        }
        public LyricManager(Transform root, GameObject charPrefab, Transform poolRoot, int initPool = 20, int maxPool = 100):base()
        {
            lyricsContainer = root;
            characterPrefab = charPrefab;
            poolContainer = poolRoot;
            initialPoolSize = initPool;
            maxPoolSize = maxPool;

            pipeline = new CharacterProcessingPipeline();
        }


        public async Task SetupAsync(Transform root, GameObject charPrefab, Transform poolRoot)
        {
            lyricsContainer = root;
            characterPrefab = charPrefab;
            poolContainer = poolRoot;

            await characterFactory.UpdateCharacter(characterPrefab, poolContainer);
        }

        /// <summary>
        /// 设置同步偏移量
        /// </summary>
        /// <param name="offset">偏移量(秒)：正值延迟歌词，负值提前歌词</param>
        public void SetSyncOffset(float offset)
        {
            syncOffset = offset;
            Debug.Log($"[歌词管理器] 设置同步偏移: {syncOffset:F3}秒");
        }

        public async UniTask Initialize()
        {
            if (characterFactory == null) characterFactory = new CharacterFactory();
            if (processorFactory == null) processorFactory = new ProcessorFactory();
            if (lrcParser == null) lrcParser = new LrcParser();

            // 初始化工厂和注册表
            await characterFactory.Initialize(initialPoolSize, maxPoolSize);
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
            lineContainer.AddComponent<RectTransform>();
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
                    characterPrefab,
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

                // 应用布局到字符对象
                if (line.Characters.Count > 0)
                {
                    await layoutProvider.ApplyLayout(line.Characters.ToArray(), positions, cts.Token);
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
                    await PlayWithCoordinator(line, cts.Token, coordinatorConfig);
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
        public async UniTask PlayLrcFile(string content, string layoutId, string effectId, Vector3 position)
        {
            try
            {
                // 停止所有当前活动
                StopAll();

                // 记录会话开始时间
                playSessionStartTime = Time.realtimeSinceStartup;

                // 记录调试信息
                if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                {
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("开始解析LRC");
                }

                // 解析LRC
                var lyrics = await lrcParser.ParseLrcFile(content);
                if (lyrics == null || lyrics.Count == 0)
                {
                    Debug.LogError("[歌词管理器] LRC解析失败或为空");
                    return;
                }

                if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                {
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("LRC解析完成");
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordStageDuration("开始解析LRC", "LRC解析完成", "LRC解析耗时");
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("LRC序列准备播放");
                }

                // 创建新的全局取消令牌
                globalCts = new CancellationTokenSource();

                // 播放歌词序列
                await PlayLyricSequence(lyrics, layoutId, effectId, position, globalCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[歌词管理器] LRC播放被取消");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[歌词管理器] 播放LRC失败: {ex}");

                if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                {
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordError("播放LRC失败", ex);
                }
            }
        }

        /// <summary>
        /// 播放歌词序列
        /// </summary>
        private async UniTask PlayLyricSequence(List<LrcLine> lyrics, string layoutId, string effectId, Vector3 position, CancellationToken cancellationToken)
        {
            // 使用实际时间计时，而不是理论时间
            float startTime = playSessionStartTime;

            for (int i = 0; i < lyrics.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var line = lyrics[i];

                // 应用同步偏移
                float adjustedTimestamp = (float)line.TimeStamp + syncOffset;

                // 计算实际经过的时间
                float elapsedTime = Time.realtimeSinceStartup - startTime;

                // 计算需要等待的时间（基于实际时间）
                float waitTime = adjustedTimestamp - elapsedTime;

                // 记录调试信息
                if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                {
                    LyricFX.Utils.LyricFXDebugger.Instance.SetCurrentLyric(line.Text);
                    string timeInfo = $"预期时间: {adjustedTimestamp:F3}s, 实际时间: {elapsedTime:F3}s, 等待: {waitTime:F3}s, 原始LRC时间: {line.TimeStamp:F3}s";
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"行准备: {timeInfo}");
                }

                // 等待到播放时间
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

                // 记录实际播放时的时间，用于调试
                float actualPlayTime = Time.realtimeSinceStartup - startTime;

                if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                {
                    float timeDiff = actualPlayTime - adjustedTimestamp;
                    string debugInfo = $"实际播放: {actualPlayTime:F3}s, 预期: {adjustedTimestamp:F3}s, 差异: {timeDiff:F3}s";
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"行播放: {debugInfo}");
                }

                // 清理之前的行
                ClearPreviousLines();

                if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                {
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("开始创建当前行");
                }

                // 创建并播放当前行
                int lineId = await CreateLyricLine(line.Text, layoutId, effectId, position);

                if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                {
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("行创建完成");
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordStageDuration("开始创建当前行", "行创建完成", "行创建耗时");
                }

                if (lineId >= 0)
                {
                    // 计算可用时间（到下一行歌词的时间）
                    float availableDuration;
                    if (i + 1 < lyrics.Count)
                    {
                        availableDuration = (float)(lyrics[i + 1].TimeStamp - line.TimeStamp);
                    }
                    else
                    {
                        // 最后一行，给一个默认时间
                        availableDuration = 3.0f;
                    }

                    // 使用反射获取配置对象
                    var config = GetConfigForEffect(effectId, availableDuration, line.Text.Length);

                    // 记录配置调试信息
                    if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug && config != null)
                    {
                        string configInfo = $"效果ID: {effectId}, 配置类型: {config.GetType().Name}, 可用时间: {availableDuration:F3}s";
                        LyricFX.Utils.LyricFXDebugger.Instance.RecordEffectConfig(effectId, availableDuration, configInfo);
                    }

                    // 根据类型转换为对应的配置对象
                    IEffectConfig effectConfig = null;
                    ICoordinatorConfig coordinatorConfig = null;

                    if (config is IEffectConfig eConfig)
                    {
                        effectConfig = eConfig;
                    }
                    else if (config is ICoordinatorConfig cConfig)
                    {
                        coordinatorConfig = cConfig;
                    }

                    await PlayLyricLine(lineId, effectConfig, coordinatorConfig);

                    // 如果有下一行，计算显示时长
                    if (i + 1 < lyrics.Count)
                    {
                        // 计算到下一行的时间（考虑偏移）
                        float nextAdjustedTimestamp = (float)lyrics[i + 1].TimeStamp + syncOffset;
                        float currentElapsedTime = Time.realtimeSinceStartup - startTime;
                        float waitDuration = nextAdjustedTimestamp - currentElapsedTime;

                        if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                        {
                            string waitInfo = $"当前时间: {currentElapsedTime:F3}s, 下一行时间: {nextAdjustedTimestamp:F3}s, 等待: {waitDuration:F3}s";
                            LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"等待下一行: {waitInfo}");
                        }

                        if (waitDuration > 0)
                        {
                            try
                            {
                                // 保持显示直到下一行时间
                                await UniTask.Delay(TimeSpan.FromSeconds(waitDuration), cancellationToken: cancellationToken);
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
                        if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                        {
                            LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("最后一行显示3秒");
                        }

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

            // 记录歌词序列播放完成
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                float totalPlaybackTime = Time.realtimeSinceStartup - playSessionStartTime;
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"歌词序列播放完成, 总播放时长: {totalPlaybackTime:F3}s");

                if (lyrics.Count > 0)
                {
                    float lrcDuration = (float)(lyrics[lyrics.Count - 1].TimeStamp - lyrics[0].TimeStamp);
                    float timeDiff = totalPlaybackTime - (lrcDuration + syncOffset);
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"LRC理论时长: {lrcDuration:F3}s, 实际播放时长: {totalPlaybackTime:F3}s, 差异: {timeDiff:F3}s");
                }
            }
        }

        /// <summary>
        /// 根据效果ID获取对应的配置对象
        /// </summary>
        /// <param name="effectId">效果ID</param>
        /// <param name="availableDuration">可用时间</param>
        /// <param name="characterCount">字符数量（用于协调器效果）</param>
        /// <returns>配置对象（IEffectConfig 或 ICoordinatorConfig）</returns>
        private object GetConfigForEffect(string effectId, float availableDuration, int characterCount)
        {
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"获取效果配置: {effectId}, 可用时间: {availableDuration:F3}s, 字符数: {characterCount}");
            }
            try
            {
                // 获取效果提供器或协调器类型
                Type targetType = null;

                if (EffectRegistry.RequiresCoordinator(effectId))
                {
                    // 获取协调器类型
                    var metadata = EffectRegistry.GetEffectMetadata(effectId);
                    if (metadata?.CoordinatorType != null)
                    {
                        targetType = metadata.CoordinatorType;
                    }
                }
                else
                {
                    // 获取效果提供器类型
                    var provider = EffectRegistry.GetEffectProvider(effectId);
                    if (provider != null)
                    {
                        targetType = provider.GetType();
                    }
                }

                if (targetType == null)
                {
                    Debug.LogWarning($"[歌词管理器] 未找到效果类型: {effectId}");
                    return null;
                }

                // 查找配置特性
                var configAttribute = targetType.GetCustomAttributes(typeof(LyricFX.Core.Attributes.EffectConfigAttribute), true);
                Type configType = null;

                // 如果有配置特性，使用特性中指定的配置类型
                if (configAttribute != null && configAttribute.Length > 0)
                {
                    configType = (configAttribute[0] as LyricFX.Core.Attributes.EffectConfigAttribute)?.ConfigType;
                }

                // 如果找到配置类型，创建实例并调整持续时间
                if (configType != null)
                {
                    var config = Activator.CreateInstance(configType) as IAdjustConfig;

                    if (config != null)
                    {
                        try
                        {
                            config.AdjustDuration(availableDuration, characterCount);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[歌词管理器] 调用 AdjustDuration 失败: {ex.Message}");
                        }
                    }

                    return config;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[歌词管理器] 创建配置对象失败: {ex}");
            }

            return null;
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
                    GameObject.Destroy(line.GameObject);
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
            // 记录调试信息
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("停止所有歌词活动");

                // 记录活动行数量
                if (activeLines != null && activeLines.Count > 0)
                {
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"停止时活动歌词行数量: {activeLines.Count}");
                }
            }

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

            // 记录结束会话
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                float sessionDuration = Time.realtimeSinceStartup - playSessionStartTime;
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"歌词播放会话结束, 持续时间: {sessionDuration:F3}s");
                LyricFX.Utils.LyricFXDebugger.Instance.EndSession("手动停止");
            }
        }

        private void OnDestroy()
        {
            StopAll();
        }

    }
}
