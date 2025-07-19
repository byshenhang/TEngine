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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
        
        /// <summary>
        /// 获取字符工厂实例
        /// </summary>
        public CharacterFactory CharacterFactory => characterFactory;

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

        // 反射缓存系统
        private readonly Dictionary<string, object> configCache = new Dictionary<string, object>();
        private readonly Dictionary<string, object> typeCache = new Dictionary<string, object>();

        // 管道实例
        private CharacterProcessingPipeline pipeline;

        // 播放会话开始时间
        private float playSessionStartTime;

        public LyricManager()
        {
            pipeline = new CharacterProcessingPipeline();
        }
        public LyricManager(Transform root, GameObject charPrefab, Transform poolRoot, int initPool = 20, int maxPool = 100) : base()
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
        /// 更新歌词行文本内容（优化版本 - 重用字符对象）
        /// </summary>
        /// <param name="lineId">要更新的行ID</param>
        /// <param name="newText">新的文本内容</param>
        /// <param name="layoutId">布局ID</param>
        /// <param name="effectId">效果ID</param>
        /// <param name="config">布局配置</param>
        /// <returns>是否更新成功</returns>
        public async UniTask<bool> UpdateLyricLineText(int lineId, string newText, string layoutId = null, string effectId = null, ILayoutConfig config = null)
        {
            if (!activeLines.TryGetValue(lineId, out var line))
            {
                Debug.LogError($"[歌词管理器] 未找到要更新的行: {lineId}");
                return false;
            }

            // 记录性能监控
            LyricFX.Utils.PerformanceMonitor.Instance.RecordLineCreation();

            try
            {
                var oldText = line.Text;
                var oldLayoutId = line.LayoutId;
                var oldEffectId = line.EffectId;
                
                // 检查是否需要更新
                bool layoutChanged = !string.IsNullOrEmpty(layoutId) && layoutId != oldLayoutId;
                bool effectChanged = !string.IsNullOrEmpty(effectId) && effectId != oldEffectId;
                bool textChanged = newText != oldText;
                
                if (!textChanged && !layoutChanged && !effectChanged)
                {
                    Debug.Log($"[歌词管理器] 行内容无变化，跳过更新: {lineId}");
                    return true;
                }

                // 更新行信息
                line.Text = newText;
                if (!string.IsNullOrEmpty(layoutId)) line.LayoutId = layoutId;
                if (!string.IsNullOrEmpty(effectId)) line.EffectId = effectId;

                // 获取布局提供器
                var layoutProvider = LayoutRegistry.GetLayoutProvider(line.LayoutId);
                if (layoutProvider == null)
                {
                    Debug.LogError($"[歌词管理器] 未找到布局: {line.LayoutId}, 使用默认布局");
                    layoutProvider = LayoutRegistry.GetLayoutProvider("default");
                }

                // 创建新的取消令牌（仅在需要时）
                CancellationTokenSource cts = null;
                if (lineCancellations.TryGetValue(lineId, out var oldCts))
                {
                    if (effectChanged)
                    {
                        oldCts.Cancel();
                        oldCts.Dispose();
                        cts = new CancellationTokenSource();
                        lineCancellations[lineId] = cts;
                    }
                    else
                    {
                        cts = oldCts; // 重用现有的取消令牌
                    }
                }
                else
                {
                    cts = new CancellationTokenSource();
                    lineCancellations[lineId] = cts;
                }

                // 优化：智能字符重用
                await OptimizedCharacterUpdate(line, newText, oldText, layoutProvider, config, cts.Token);

                // 优化：仅在效果改变时重新处理效果
                if (effectChanged)
                {
                    await UpdateEffects(line, cts.Token);
                }

                // 触发行更新事件
                LyricEvents.TriggerLineCreated(new LineEventArgs
                {
                    LineId = lineId,
                    Content = newText,
                    EffectId = line.EffectId,
                    LayoutId = line.LayoutId
                });

                Debug.Log($"[歌词管理器] 优化更新完成, ID: {lineId}, 新文本: {newText}, 字符数: {line.Characters.Count}");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[歌词管理器] 更新行文本被取消: {lineId}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[歌词管理器] 更新行文本失败: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 优化的字符更新方法 - 重用现有字符对象
        /// </summary>
        private async UniTask OptimizedCharacterUpdate(LyricLine line, string newText, string oldText, 
            ILayoutProvider layoutProvider, ILayoutConfig config, CancellationToken cancellationToken)
        {
            var newLength = newText.Length;
            var oldLength = oldText.Length;
            var existingCharacters = line.Characters;

            // 计算布局位置
            var positions = await layoutProvider.CalculateLayout(
                newText,
                line.GameObject.transform,
                config,
                characterPrefab,
                cancellationToken
            );

            // 触发布局计算事件
            LyricEvents.TriggerLayoutCalculated(new LayoutEventArgs
            {
                LineId = line.Id,
                Positions = positions
            });

            // 字符重用策略
            var reusedCharacters = new List<GameObject>();
            var charactersToRelease = new List<GameObject>();
            var newCharactersNeeded = Math.Max(0, newLength - existingCharacters.Count);

            // 重用现有字符
            for (int i = 0; i < Math.Min(newLength, existingCharacters.Count); i++)
            {
                var character = existingCharacters[i];
                if (character != null)
                {
                    // 更新字符内容和位置
                    await UpdateCharacterContent(character, newText[i], positions[i], i, line.Id);
                    reusedCharacters.Add(character);
                }
            }

            // 释放多余的字符
            for (int i = newLength; i < existingCharacters.Count; i++)
            {
                if (existingCharacters[i] != null)
                {
                    charactersToRelease.Add(existingCharacters[i]);
                }
            }

            // 批量释放字符（异步处理避免卡顿）
            if (charactersToRelease.Count > 0)
            {
                _ = UniTask.Run(() =>
                {
                    foreach (var character in charactersToRelease)
                    {
                        characterFactory.ReleaseCharacter(character);
                    }
                });
            }

            // 创建新字符（如果需要）
            if (newCharactersNeeded > 0)
            {
                var newContexts = new List<ProcessingContext>();
                for (int i = existingCharacters.Count; i < newLength; i++)
                {
                    var context = ProcessingContext.Create(
                        null,
                        i,
                        newText[i],
                        line.Id,
                        positions[i]
                    );

                    context.SetMetadata("layoutId", line.LayoutId);
                    context.SetMetadata("effectId", line.EffectId);
                    newContexts.Add(context);
                }

                // 处理新字符
                var results = await pipeline.ProcessCharacters(newContexts, true, cancellationToken);
                foreach (var result in results)
                {
                    if (result.CharacterObject != null)
                    {
                        reusedCharacters.Add(result.CharacterObject);
                    }
                }
            }

            // 更新行的字符列表
            line.Characters = reusedCharacters;

            // 应用布局（仅对新字符或位置改变的字符）
            if (line.Characters.Count > 0)
            {
                await layoutProvider.ApplyLayout(line.Characters.ToArray(), positions, cancellationToken);
            }
        }

        /// <summary>
        /// 更新单个字符的内容和位置
        /// </summary>
        private async UniTask UpdateCharacterContent(GameObject character, char newChar, Vector3 position, int index, int lineId)
        {
            try
            {
                // 更新字符文本内容
                var textComponent = character.GetComponent<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = newChar.ToString();
                }

                // 更新位置
                character.transform.position = position;

                // 可以在这里添加其他字符属性的更新逻辑
                // 例如：字符索引、动画状态等
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[歌词管理器] 更新字符内容失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新效果（仅在效果改变时调用）
        /// </summary>
        private async UniTask UpdateEffects(LyricLine line, CancellationToken cancellationToken)
        {
            try
            {
                // 停止现有效果
                if (line.EffectCoordinator != null)
                {
                    await line.EffectCoordinator.Stop(cancellationToken);
                    line.EffectCoordinator = null;
                }

                foreach (var effect in line.CharacterEffects)
                {
                    await effect.Stop(cancellationToken);
                }
                line.CharacterEffects.Clear();

                // 重新应用效果（这部分可以进一步优化）
                // 注意：这里可以考虑实现效果的增量更新
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[歌词管理器] 更新效果失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建歌词行
        /// </summary>
        public async UniTask<int> CreateLyricLine(string text, string layoutId, string effectId, Vector3 position, ILayoutConfig config = null)
        {
            // 记录性能监控
            LyricFX.Utils.PerformanceMonitor.Instance.RecordLineCreation();
            
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
                var results = await pipeline.ProcessCharacters(contexts, true, cts.Token);

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
            // 记录性能监控
            LyricFX.Utils.PerformanceMonitor.Instance.RecordEffectExecution();
            
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

                // 重置重用行ID，为新的播放会话做准备
                reusableLineId = -1;

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
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("开始预热对象池");
                }

                // 创建新的全局取消令牌
                globalCts = new CancellationTokenSource();

                // 预热对象池
                await WarmupCharacterPool(lyrics, globalCts.Token);

                if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                {
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("对象池预热完成");
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordStageDuration("开始预热对象池", "对象池预热完成", "对象池预热耗时");
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("LRC序列准备播放");
                }

                // 播放歌词序列
                PlayLyricSequence(lyrics, layoutId, effectId, position, globalCts.Token);
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
        /// 预热字符对象池
        /// </summary>
        private async UniTask WarmupCharacterPool(List<LrcLine> lyrics, CancellationToken cancellationToken)
        {
            try
            {
                // 记录性能监控
                LyricFX.Utils.PerformanceMonitor.Instance.RecordPoolWarmup();
                
                // 计算最大字符数和总字符数
                int maxCharCount = 0;
                
                foreach (var line in lyrics)
                {
                    int charCount = line.Text?.Length ?? 0;
                    maxCharCount = Mathf.Max(maxCharCount, charCount);
                }
                
                // 预估需要的字符数量（考虑同时显示的行数和缓冲）
                int estimatedCharCount = maxCharCount ;
                
                Debug.Log($"[歌词管理器] 开始预热对象池，预估字符数: {estimatedCharCount}, 最大单行字符数: {maxCharCount}");
                
                // 预热对象池
                await characterFactory.WarmupPool(estimatedCharCount, 1.2f);
                
                Debug.Log($"[歌词管理器] 对象池预热完成: {characterFactory.GetPoolStatus()}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[歌词管理器] 对象池预热失败: {ex.Message}");
            }
        }

        private int nextPlayID;
        /// <summary>
        /// 播放歌词序列
        /// </summary>
        private async UniTask PlayLyricSequence(List<LrcLine> lyrics, string layoutId, string effectId, Vector3 position, CancellationToken cancellationToken)
        {
            float startTime = playSessionStartTime;

            for (int i = 0; i < lyrics.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var line = lyrics[i];
                Debug.Log("----------");
                // 等待到播放时间
                await WaitForLyricTime(line, startTime, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                    break;
              
                // 处理当前歌词行
                await ProcessLyricLine(line, lyrics, i, layoutId, effectId, position, startTime, cancellationToken);
              
            }

            // 记录播放完成信息
            RecordPlaybackCompletion(lyrics);
        }

        /// <summary>
        /// 等待到歌词播放时间
        /// </summary>
        private async UniTask WaitForLyricTime(LrcLine line, float startTime, CancellationToken cancellationToken)
        {
            float adjustedTimestamp = (float)line.TimeStamp + syncOffset;
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            float waitTime = adjustedTimestamp - elapsedTime;

            // 记录时间调试信息
            RecordTimingDebugInfo(line, adjustedTimestamp, elapsedTime, waitTime);

            if (waitTime > 0)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }

            // 记录实际播放时间
            RecordActualPlayTime(startTime, adjustedTimestamp);
        }

        // 重用的行ID，避免每次创建新行
        private int reusableLineId = -1;

        /// <summary>
        /// 处理单行歌词
        /// </summary>
        private async UniTask ProcessLyricLine(LrcLine line, List<LrcLine> lyrics, int index, string layoutId, string effectId, Vector3 position, float startTime, CancellationToken cancellationToken)
        {
            int lineId;
            
            // 检查是否可以重用现有行
            if (reusableLineId >= 0 && activeLines.ContainsKey(reusableLineId))
            {
                // 重用现有行，更新文本内容（优化版本）
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                bool updateSuccess = await UpdateLyricLineText(reusableLineId, line.Text, layoutId, effectId);
                // 停止计时
                stopwatch.Stop();
                // 获取耗时
                TimeSpan elapsedTime = stopwatch.Elapsed;
                Debug.Log($"[优化版本] 执行耗时: {elapsedTime.TotalMilliseconds} 毫秒 (目标: <50ms)");
                
                // 性能警告
                if (elapsedTime.TotalMilliseconds > 50)
                {
                    Debug.LogWarning($"[性能警告] 更新耗时过长: {elapsedTime.TotalMilliseconds}ms，可能影响VR体验");
                }

                if (updateSuccess)
                {
                    lineId = reusableLineId;
                    Debug.Log($"[歌词管理器] 重用行 {lineId}, 新文本: {line.Text}");
                }
                else
                {
                    // 更新失败，创建新行
                    lineId = await CreateAndRecordLyricLine(line.Text, layoutId, effectId, position);
                    reusableLineId = lineId;
                }
            }
            else
            {
                // 异步清理之前的行（仅在首次创建时）
                _ = ClearPreviousLinesAsync();
                
                // 创建新的歌词行
                lineId = await CreateAndRecordLyricLine(line.Text, layoutId, effectId, position);
                reusableLineId = lineId; // 记录可重用的行ID
            }

            if (lineId >= 0)
            {
                // 获取效果配置并播放
                await PlayLyricLineWithConfig(lineId, line, lyrics, index, effectId);
                
                // 等待到下一行或结束
                await WaitForNextLine(lyrics, index, startTime, cancellationToken);
            }
        }

        /// <summary>
        /// 创建歌词行并记录调试信息
        /// </summary>
        private async UniTask<int> CreateAndRecordLyricLine(string text, string layoutId, string effectId, Vector3 position)
        {
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("开始创建当前行");
            }

            int lineId = await CreateLyricLine(text, layoutId, effectId, position);

            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("行创建完成");
                LyricFX.Utils.LyricFXDebugger.Instance.RecordStageDuration("开始创建当前行", "行创建完成", "行创建耗时");
            }

            return lineId;
        }

        /// <summary>
        /// 使用配置播放歌词行
        /// </summary>
        private async UniTask PlayLyricLineWithConfig(int lineId, LrcLine line, List<LrcLine> lyrics, int index, string effectId)
        {
            float availableDuration = CalculateAvailableDuration(lyrics, index);
            var config = GetConfigForEffect(effectId, availableDuration, line.Text.Length);

            // 记录配置调试信息
            RecordEffectConfigDebugInfo(effectId, availableDuration, config);

            // 转换配置对象
            var (effectConfig, coordinatorConfig) = ConvertEffectConfig(config);

            await PlayLyricLine(lineId, effectConfig, coordinatorConfig);
        }

        /// <summary>
        /// 等待到下一行歌词或结束
        /// </summary>
        private async UniTask WaitForNextLine(List<LrcLine> lyrics, int currentIndex, float startTime, CancellationToken cancellationToken)
        {
            if (currentIndex + 1 < lyrics.Count)
            {
                await WaitForNextLyricLine(lyrics, currentIndex, startTime, cancellationToken);
            }
            else
            {
                await WaitForLastLine(cancellationToken);
            }
        }

        /// <summary>
        /// 等待到下一行歌词
        /// </summary>
        private async UniTask WaitForNextLyricLine(List<LrcLine> lyrics, int currentIndex, float startTime, CancellationToken cancellationToken)
        {
            float nextAdjustedTimestamp = (float)lyrics[currentIndex + 1].TimeStamp + syncOffset;
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
                    await UniTask.Delay(TimeSpan.FromSeconds(waitDuration), cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 等待最后一行显示时间
        /// </summary>
        private async UniTask WaitForLastLine(CancellationToken cancellationToken)
        {
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
                throw;
            }
        }

        /// <summary>
        /// 计算可用时间
        /// </summary>
        private float CalculateAvailableDuration(List<LrcLine> lyrics, int currentIndex)
        {
            if (currentIndex + 1 < lyrics.Count)
            {
                return (float)(lyrics[currentIndex + 1].TimeStamp - lyrics[currentIndex].TimeStamp);
            }
            else
            {
                return 3.0f; // 最后一行默认时间
            }
        }

        /// <summary>
        /// 转换效果配置对象
        /// </summary>
        private (IEffectConfig effectConfig, ICoordinatorConfig coordinatorConfig) ConvertEffectConfig(object config)
        {
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

            return (effectConfig, coordinatorConfig);
        }

        /// <summary>
        /// 记录时间调试信息
        /// </summary>
        private void RecordTimingDebugInfo(LrcLine line, float adjustedTimestamp, float elapsedTime, float waitTime)
        {
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                LyricFX.Utils.LyricFXDebugger.Instance.SetCurrentLyric(line.Text);
                string timeInfo = $"预期时间: {adjustedTimestamp:F3}s, 实际时间: {elapsedTime:F3}s, 等待: {waitTime:F3}s, 原始LRC时间: {line.TimeStamp:F3}s";
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"行准备: {timeInfo}");
            }
        }

        /// <summary>
        /// 记录实际播放时间
        /// </summary>
        private void RecordActualPlayTime(float startTime, float adjustedTimestamp)
        {
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                float actualPlayTime = Time.realtimeSinceStartup - startTime;
                float timeDiff = actualPlayTime - adjustedTimestamp;
                string debugInfo = $"实际播放: {actualPlayTime:F3}s, 预期: {adjustedTimestamp:F3}s, 差异: {timeDiff:F3}s";
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"行播放: {debugInfo}");
            }
        }

        /// <summary>
        /// 记录效果配置调试信息
        /// </summary>
        private void RecordEffectConfigDebugInfo(string effectId, float availableDuration, object config)
        {
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug && config != null)
            {
                string configInfo = $"效果ID: {effectId}, 配置类型: {config.GetType().Name}, 可用时间: {availableDuration:F3}s";
                LyricFX.Utils.LyricFXDebugger.Instance.RecordEffectConfig(effectId, availableDuration, configInfo);
            }
        }

        /// <summary>
        /// 记录播放完成信息
        /// </summary>
        private void RecordPlaybackCompletion(List<LrcLine> lyrics)
        {
            Debug.Log("[歌词管理器] 歌词序列播放完成");

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
                // 检查类型缓存
                var cacheKey = $"type_{effectId}";
                Type targetType = null;
                Type configType = null;

                if (typeCache.TryGetValue(cacheKey, out var cachedTypeInfo))
                {
                    var typeInfo = ((Type, Type))cachedTypeInfo;
                    targetType = typeInfo.Item1;
                    configType = typeInfo.Item2;
                }
                else
                {
                    // 获取效果提供器或协调器类型
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

                    // 如果有配置特性，使用特性中指定的配置类型
                    if (configAttribute != null && configAttribute.Length > 0)
                    {
                        configType = (configAttribute[0] as LyricFX.Core.Attributes.EffectConfigAttribute)?.ConfigType;
                    }

                    // 缓存类型信息
                    typeCache[cacheKey] = (targetType, configType);
                }

                // 如果找到配置类型，创建实例并调整持续时间
                if (configType != null)
                {
                    // 检查配置缓存
                    var configCacheKey = $"{effectId}_{availableDuration:F3}_{characterCount}";
                    if (configCache.TryGetValue(configCacheKey, out var cachedConfig))
                    {
                        return cachedConfig;
                    }
                    
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

                    // 缓存配置对象
                    configCache[configCacheKey] = config;
                    
                    return config;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[歌词管理器] 创建配置对象失败: {ex}");
            }

            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"未找到效果配置类型: {effectId}");
            }
            
            return null;
        }

        /// <summary>
        /// 清理特定行
        /// </summary>
        private void CleanupLine(int lineId)
        {
            // 记录性能监控
            LyricFX.Utils.PerformanceMonitor.Instance.RecordLineCleanup();
            
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
        /// 清理之前的所有行（同步版本，保留用于紧急清理）
        /// </summary>
        private void ClearPreviousLines()
        {
            foreach (var lineId in new List<int>(activeLines.Keys))
            {
                CleanupLine(lineId);
            }
        }

        /// <summary>
        /// 异步清理之前的所有行，分批处理避免卡顿
        /// </summary>
        private async UniTask ClearPreviousLinesAsync()
        {
            var linesToClear = new List<int>(activeLines.Keys);
            
            if (linesToClear.Count == 0) return;
            
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"开始异步清理 {linesToClear.Count} 行歌词");
            }
            
            // VR优化：分批清理，每批处理1行，避免一次性清理造成卡顿
            for (int i = 0; i < linesToClear.Count; i++)
            {
                CleanupLine(linesToClear[i]);
                
                // 每清理一行就让出一帧，保持VR环境流畅性
                await UniTask.Yield();
            }
            
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint("异步清理完成");
            }
        }

        /// <summary>
        /// 延迟清理特定行，用于性能优化
        /// </summary>
        /// <param name="lineId">要清理的行ID</param>
        /// <param name="delay">延迟时间（秒）</param>
        private async UniTask DelayedCleanupLine(int lineId, float delay = 0.3f)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
                CleanupLine(lineId);
                
                if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
                {
                    LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"延迟清理行 {lineId} 完成");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[歌词管理器] 延迟清理行失败: {ex.Message}");
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

            // 重置重用行ID
            reusableLineId = -1;

            // 清理缓存以释放内存
            ClearCaches();

            Debug.Log("[歌词管理器] 停止所有活动并清理缓存");

            // 记录结束会话
            if (LyricFX.Utils.LyricFXDebugger.Instance.EnableDebug)
            {
                float sessionDuration = Time.realtimeSinceStartup - playSessionStartTime;
                LyricFX.Utils.LyricFXDebugger.Instance.RecordTimePoint($"歌词播放会话结束, 持续时间: {sessionDuration:F3}s");
                LyricFX.Utils.LyricFXDebugger.Instance.EndSession("手动停止");
            }
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        private void ClearCaches()
        {
            configCache.Clear();
            typeCache.Clear();
            Debug.Log("[歌词管理器] 已清理反射缓存");
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StopAll();
            
            characterFactory?.Dispose();
            pipeline?.Dispose();
            
            globalCts?.Cancel();
            globalCts?.Dispose();
            globalCts = null;
            
            activeLines.Clear();
            reusableLineId = -1;
            ClearCaches();
            
            Debug.Log("[歌词管理器] 已释放资源");
        }

        private void OnDestroy()
        {
            StopAll();
        }

    }
}
