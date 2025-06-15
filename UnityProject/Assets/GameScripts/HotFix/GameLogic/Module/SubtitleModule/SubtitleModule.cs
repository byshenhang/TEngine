using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 字幕效果模块 - 支持复杂歌词和多种动画效果
    /// </summary>
    public sealed class SubtitleModule : Singleton<SubtitleModule>, IUpdate
    {
        private readonly Dictionary<string, ISubtitleSequence> _activeSequences = new();
        private readonly Dictionary<Type, ISubtitleEffectFactory> _effectFactories = new();
        private readonly List<ISubtitleEffect> _activeEffects = new();
        private SubtitleResourceLoader _resourceLoader;
        private Transform _subtitleRoot;
        private SubtitleTimingController _timingController;
        
        /// <summary>
        /// 字幕根节点
        /// </summary>
        public Transform SubtitleRoot => _subtitleRoot;
        
        /// <summary>
        /// 时序控制器
        /// </summary>
        public SubtitleTimingController TimingController => _timingController;
        
        /// <summary>
        /// 模块初始化
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();
            
            // 创建字幕根节点
            GameObject rootObj = new GameObject("SubtitleRoot");
            
            // 添加Canvas组件
            var canvas = rootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 确保字幕显示在最前面
            
            // 添加CanvasScaler组件
            var canvasScaler = rootObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // 添加GraphicRaycaster组件（用于UI交互）
            rootObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // 只在运行时模式下调用DontDestroyOnLoad
            if (UnityEngine.Application.isPlaying)
            {
                GameObject.DontDestroyOnLoad(rootObj);
            }
            
            _subtitleRoot = rootObj.transform;
            
            // 初始化资源加载器
            _resourceLoader = new SubtitleResourceLoader();
            
            // 注册默认效果工厂
            RegisterDefaultEffectFactories();
            
            // 初始化时序控制器
            _timingController = new SubtitleTimingController();
            
            Log.Info("[SubtitleModule] 字幕模块初始化完成");
        }
        
        /// <summary>
        /// 模块更新
        /// </summary>
        public void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            
            // 更新时序控制器
            _timingController?.Update();
            
            // 更新所有活动的字幕序列
            foreach (var sequence in _activeSequences.Values)
            {
                sequence.Update(deltaTime);
            }
            
            // 更新所有活动的效果
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.Update(deltaTime);
                
                // 移除已完成的效果
                if (effect.IsCompleted)
                {
                    effect.Release();
                    _activeEffects.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 播放字幕序列
        /// </summary>
        /// <param name="sequenceId">序列ID</param>
        /// <param name="config">字幕配置</param>
        public async UniTask PlaySubtitleSequenceAsync(string sequenceId, SubtitleSequenceConfig config, bool autoRelease = true)
        {
            if (_activeSequences.ContainsKey(sequenceId))
            {
                // 如果序列已存在，并且不是由 StopAllSequences 管理的（即 autoRelease 为 true），则警告并返回
                // 如果是 StopAllSequences 管理的（autoRelease 为 false），则允许覆盖，因为 StopAllSequences 会确保清理
                if (autoRelease) 
                {
                    Log.Warning($"[SubtitleModule] 字幕序列 {sequenceId} 已在播放中，且配置为自动释放。如需覆盖请确保正确管理序列生命周期。");
                    return;
                }
                else
                {
                    // 如果是LRC播放等情况，旧的序列会被StopAllSequences清理，这里可以安全地覆盖
                    if(_activeSequences.TryGetValue(sequenceId, out var oldSequence))
                    {
                        oldSequence.Stop();
                        oldSequence.Release(); // 确保旧的被正确释放
                        _activeSequences.Remove(sequenceId); // 显式移除旧序列的引用
                    }
                }
            }
            
            // 创建字幕序列
            var sequence = CreateSubtitleSequence(config);
            _activeSequences[sequenceId] = sequence;
            
            try
            {
                // 播放序列
                await sequence.PlayAsync();
            }
            finally
            {
                if (autoRelease)
                {
                    // 清理序列
                    if (_activeSequences.ContainsKey(sequenceId) && _activeSequences[sequenceId] == sequence)
                    {
                         _activeSequences.Remove(sequenceId);
                    }
                    sequence.Release();
                }
                // 如果 autoRelease 为 false，则序列的释放由 StopAllSequences 或其他外部逻辑管理
            }
        }
        
        /// <summary>
        /// 停止字幕序列
        /// </summary>
        /// <param name="sequenceId">序列ID</param>
        public void StopSubtitleSequence(string sequenceId)
        {
            if (_activeSequences.TryGetValue(sequenceId, out var sequence))
            {
                sequence.Stop();
                _activeSequences.Remove(sequenceId);
                sequence.Release();
            }
        }
        
        /// <summary>
        /// 停止所有字幕序列
        /// </summary>
        public void StopAllSequences()
        {
            foreach (var kvp in _activeSequences)
            {
                kvp.Value.Stop();
                kvp.Value.Release();
            }
            _activeSequences.Clear();
        }
        
        /// <summary>
        /// 创建单个字幕效果
        /// </summary>
        /// <typeparam name="T">效果类型</typeparam>
        /// <param name="config">效果配置</param>
        /// <returns>字幕效果实例</returns>
        public T CreateSubtitleEffect<T>(ISubtitleEffectConfig config) where T : class, ISubtitleEffect
        {
            var effectType = typeof(T);
            if (_effectFactories.TryGetValue(effectType, out var factory))
            {
                var effect = factory.CreateEffect(config) as T;
                if (effect != null)
                {
                    _activeEffects.Add(effect);
                    return effect;
                }
            }
            
            Log.Error($"[SubtitleModule] 无法创建字幕效果: {effectType.Name}");
            return null;
        }
        
        /// <summary>
        /// 创建字幕效果（通用方法）
        /// </summary>
        /// <param name="config">效果配置</param>
        /// <returns>字幕效果实例</returns>
        public ISubtitleEffect CreateEffect(ISubtitleEffectConfig config)
        {
            // 根据配置类型创建对应的效果
            return config switch
            {
                FadeEffectConfig fadeConfig => CreateSubtitleEffect<FadeSubtitleEffect>(fadeConfig),
                ScaleEffectConfig scaleConfig => CreateSubtitleEffect<ScaleSubtitleEffect>(scaleConfig),
                BlurEffectConfig blurConfig => CreateSubtitleEffect<BlurSubtitleEffect>(blurConfig),
                TypewriterEffectConfig typewriterConfig => CreateSubtitleEffect<TypewriterSubtitleEffect>(typewriterConfig),
                _ => null
            };
        }
        
        /// <summary>
        /// 注册效果工厂
        /// </summary>
        /// <typeparam name="T">效果类型</typeparam>
        /// <param name="factory">效果工厂</param>
        public void RegisterEffectFactory<T>(ISubtitleEffectFactory factory) where T : ISubtitleEffect
        {
            _effectFactories[typeof(T)] = factory;
        }
        
        /// <summary>
        /// 创建字幕序列
        /// </summary>
        private ISubtitleSequence CreateSubtitleSequence(SubtitleSequenceConfig config)
        {
            return config.SequenceType switch
            {
                SubtitleSequenceType.Simple => new SimpleSubtitleSequence(config, this),
                SubtitleSequenceType.Complex => new ComplexSubtitleSequence(config, this),
                SubtitleSequenceType.Lyric => new LyricSubtitleSequence(config, this),
                _ => new SimpleSubtitleSequence(config, this)
            };
        }
        
        /// <summary>
        /// 添加定时字幕
        /// </summary>
        /// <param name="id">字幕ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="duration">持续时间</param>
        /// <param name="config">字幕配置</param>
        public void AddTimedSubtitle(string id, float startTime, float duration, SubtitleSequenceConfig config)
        {
            var sequence = CreateSubtitleSequence(config);
            _timingController.AddTimedEntry(id, startTime, duration, sequence);
        }
        
        /// <summary>
        /// 移除定时字幕
        /// </summary>
        /// <param name="id">字幕ID</param>
        public void RemoveTimedSubtitle(string id)
        {
            _timingController.RemoveTimedEntry(id);
        }
        
        /// <summary>
        /// 开始播放时序字幕
        /// </summary>
        public void PlayTimedSubtitles()
        {
            _timingController.Play();
        }
        
        /// <summary>
        /// 暂停时序字幕
        /// </summary>
        public void PauseTimedSubtitles()
        {
            _timingController.Pause();
        }
        
        /// <summary>
        /// 恢复时序字幕
        /// </summary>
        public void ResumeTimedSubtitles()
        {
            _timingController.Resume();
        }
        
        /// <summary>
        /// 停止时序字幕
        /// </summary>
        public void StopTimedSubtitles()
        {
            _timingController.Stop();
        }
        
        /// <summary>
        /// 跳转到指定时间
        /// </summary>
        /// <param name="time">目标时间</param>
        public void SeekTo(float time)
        {
            _timingController.SeekTo(time);
        }
        
        /// <summary>
        /// 注册默认效果工厂
        /// </summary>
        private void RegisterDefaultEffectFactories()
        {
            RegisterEffectFactory<BlurSubtitleEffect>(new BlurSubtitleEffectFactory());
            RegisterEffectFactory<FadeSubtitleEffect>(new FadeSubtitleEffectFactory());
            RegisterEffectFactory<TypewriterSubtitleEffect>(new TypewriterSubtitleEffectFactory());
            RegisterEffectFactory<ScaleSubtitleEffect>(new ScaleSubtitleEffectFactory());
        }
        
        /// <summary>
        /// 播放LRC歌词文件
        /// </summary>
        /// <param name="lrcFilePath">LRC文件路径</param>
        /// <param name="displayDuration">每行歌词的默认显示时长（秒）</param>
        /// <param name="enableEffects">是否启用默认效果</param>
        /// <returns>播放任务</returns>
        public async UniTask PlayLRCFileAsync(string lrcFilePath, float displayDuration = 3f, bool enableEffects = true)
        {
            try
            {
                Log.Info($"[SubtitleModule] 开始播放LRC文件: {lrcFilePath}");
                
                // 加载并解析LRC文件
                var parseResult = await LRCLoader.LoadFromFileAsync(lrcFilePath);
                
                if (!parseResult.isValid)
                {
                    Log.Error($"[SubtitleModule] LRC文件解析失败: {parseResult.errorMessage}");
                    return;
                }
                
                await PlayLRCAsync(parseResult, displayDuration, enableEffects);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[SubtitleModule] 播放LRC文件时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 播放LRC歌词内容
        /// </summary>
        /// <param name="lrcContent">LRC文件内容</param>
        /// <param name="displayDuration">每行歌词的默认显示时长（秒）</param>
        /// <param name="enableEffects">是否启用默认效果</param>
        /// <returns>播放任务</returns>
        public async UniTask PlayLRCContentAsync(string lrcContent, float displayDuration = 3f, bool enableEffects = true)
        {
            try
            {
                Log.Info($"[SubtitleModule] 开始播放LRC内容");
                
                // 解析LRC内容
                var parseResult = LRCLoader.LoadFromString(lrcContent);
                
                if (!parseResult.isValid)
                {
                    Log.Error($"[SubtitleModule] LRC内容解析失败: {parseResult.errorMessage}");
                    return;
                }
                
                await PlayLRCAsync(parseResult, displayDuration, enableEffects);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[SubtitleModule] 播放LRC内容时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 使用配置文件播放LRC内容
        /// </summary>
        /// <param name="lrcContent">LRC文件内容</param>
        /// <param name="configAsset">字幕配置资源</param>
        /// <returns>播放任务</returns>
        public async UniTask PlayLRCWithConfigAsync(string lrcContent, SubtitleSequenceAsset configAsset)
        {
            if (configAsset == null)
            {
                Log.Error($"[SubtitleModule] 配置资源为空");
                return;
            }
            
            // 清理所有活动的字幕序列，避免堆叠
            StopAllSequences();
            
            try
            {
                Log.Info($"[SubtitleModule] 使用配置文件播放LRC内容: {configAsset.SequenceName}");
                
                // 解析LRC内容
                var parseResult = LRCLoader.LoadFromString(lrcContent);
                
                if (!parseResult.isValid)
                {
                    Log.Error($"[SubtitleModule] LRC内容解析失败: {parseResult.errorMessage}");
                    return;
                }
                
                await PlayLRCWithConfigAsync(parseResult, configAsset);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[SubtitleModule] 使用配置文件播放LRC内容时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 使用配置文件播放LRC解析结果
        /// </summary>
        /// <param name="parseResult">LRC解析结果</param>
        /// <param name="configAsset">字幕配置资源</param>
        /// <returns>播放任务</returns>
        private async UniTask PlayLRCWithConfigAsync(LRCParser.LRCParseResult parseResult, SubtitleSequenceAsset configAsset)
        {
            if (!parseResult.isValid)
            {
                Log.Error($"[SubtitleModule] LRC解析结果无效");
                return;
            }
            
            // 清理所有活动的字幕序列，避免堆叠
            StopAllSequences();

            
            Log.Info($"[SubtitleModule] 使用配置文件播放LRC，共{parseResult.entries.Count}个条目");
            Log.Info($"[SubtitleModule] LRC信息 - 标题: {parseResult.metadata.title}, 艺术家: {parseResult.metadata.artist}");
            Log.Info($"[SubtitleModule] 配置文件: {configAsset.SequenceName} - {configAsset.Description}");
            
            // 逐个播放歌词，使用配置文件中的效果
            for (int i = 0; i < parseResult.entries.Count; i++)
            {
                var entry = parseResult.entries[i];
                
                // 克隆配置并设置当前歌词文本
                var config = configAsset.Config.Clone();
                config.Text = entry.lyricText;
                
                // 等待到指定时间
                if (i == 0)
                {
                    // 第一行歌词，等待到开始时间
                    if (entry.timeInSeconds > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(entry.timeInSeconds));
                    }
                }
                else
                {
                    // 后续歌词，等待时间间隔
                    var prevEntry = parseResult.entries[i - 1];
                    float waitTime = entry.timeInSeconds - prevEntry.timeInSeconds;
                    if (waitTime > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(waitTime));
                    }
                }
                
                // 播放当前歌词
                string sequenceId = $"lrc_config_line_{i}";
                // LRC播放的序列由StopAllSequences统一管理释放，此处autoRelease传false
                await PlaySubtitleSequenceAsync(sequenceId, config, false);
            }
            
            Log.Info($"[SubtitleModule] 配置文件LRC播放完成");
        }
        
        /// <summary>
        /// 从Resources加载并播放LRC文件
        /// </summary>
        /// <param name="resourcePath">Resources中的路径（不包含扩展名）</param>
        /// <param name="displayDuration">每行歌词的默认显示时长（秒）</param>
        /// <param name="enableEffects">是否启用默认效果</param>
        /// <returns>播放任务</returns>
        public async UniTask PlayLRCFromResourcesAsync(string resourcePath, float displayDuration = 3f, bool enableEffects = true)
        {
            try
            {
                Log.Info($"[SubtitleModule] 开始从Resources播放LRC文件: {resourcePath}");
                
                // 从Resources加载并解析LRC文件
                var parseResult = await LRCLoader.LoadFromResourcesAsync(resourcePath);
                
                if (!parseResult.isValid)
                {
                    Log.Error($"[SubtitleModule] LRC文件解析失败: {parseResult.errorMessage}");
                    return;
                }
                
                await PlayLRCAsync(parseResult, displayDuration, enableEffects);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[SubtitleModule] 从Resources播放LRC文件时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从URL加载并播放LRC文件
        /// </summary>
        /// <param name="url">LRC文件的URL</param>
        /// <param name="displayDuration">每行歌词的默认显示时长（秒）</param>
        /// <param name="enableEffects">是否启用默认效果</param>
        /// <returns>播放任务</returns>
        public async UniTask PlayLRCFromUrlAsync(string url, float displayDuration = 3f, bool enableEffects = true)
        {
            try
            {
                Log.Info($"[SubtitleModule] 开始从URL播放LRC文件: {url}");
                
                // 从URL加载并解析LRC文件
                var parseResult = await LRCLoader.LoadFromUrlAsync(url);
                
                if (!parseResult.isValid)
                {
                    Log.Error($"[SubtitleModule] LRC文件解析失败: {parseResult.errorMessage}");
                    return;
                }
                
                await PlayLRCAsync(parseResult, displayDuration, enableEffects);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[SubtitleModule] 从URL播放LRC文件时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 添加LRC歌词到时序控制器
        /// </summary>
        /// <param name="parseResult">LRC解析结果</param>
        /// <param name="displayDuration">每行歌词的默认显示时长（秒）</param>
        /// <param name="lrcId">LRC标识符，用于后续控制</param>
        /// <param name="enableEffects">是否启用默认效果</param>
        public void AddLRCToTimingController(LRCParser.LRCParseResult parseResult, float displayDuration = 3f, string lrcId = "default_lrc", bool enableEffects = true)
        {
            try
            {
                if (!parseResult.isValid)
                {
                    Log.Error($"[SubtitleModule] LRC解析结果无效，无法添加到时序控制器");
                    return;
                }
                
                Log.Info($"[SubtitleModule] 添加LRC到时序控制器: {lrcId}, 共{parseResult.entries.Count}个条目，效果启用: {enableEffects}");
                
                // 转换为定时字幕条目并直接添加到时序控制器
                LRCParser.ConvertToTimedSubtitles(parseResult, this, displayDuration, enableEffects);
                
                Log.Info($"[SubtitleModule] LRC添加到时序控制器完成: {lrcId}");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[SubtitleModule] 添加LRC到时序控制器时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 播放LRC解析结果
        /// </summary>
        /// <param name="parseResult">LRC解析结果</param>
        /// <param name="displayDuration">每行歌词的默认显示时长（秒）</param>
        /// <param name="enableEffects">是否启用默认效果</param>
        /// <returns>播放任务</returns>
        private async UniTask PlayLRCAsync(LRCParser.LRCParseResult parseResult, float displayDuration, bool enableEffects = true)
        {
            if (!parseResult.isValid)
            {
                Log.Error($"[SubtitleModule] LRC解析结果无效");
                return;
            }
            
            // 清理所有活动的字幕序列，避免堆叠
            StopAllSequences();

            
            Log.Info($"[SubtitleModule] 开始播放LRC，共{parseResult.entries.Count}个条目，效果启用: {enableEffects}");
            Log.Info($"[SubtitleModule] LRC信息 - 标题: {parseResult.metadata.title}, 艺术家: {parseResult.metadata.artist}");
            
            // 转换为字幕配置
            var configs = LRCParser.ConvertToSubtitleConfigs(parseResult, displayDuration, enableEffects);
            
            // 逐个播放歌词
            for (int i = 0; i < parseResult.entries.Count; i++)
            {
                var entry = parseResult.entries[i];
                var config = configs[i];
                
                // 等待到指定时间
                if (i == 0)
                {
                    // 第一行歌词，等待到开始时间
                    if (entry.timeInSeconds > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(entry.timeInSeconds));
                    }
                }
                else
                {
                    // 后续歌词，等待时间间隔
                    var prevEntry = parseResult.entries[i - 1];
                    float waitTime = entry.timeInSeconds - prevEntry.timeInSeconds;
                    if (waitTime > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(waitTime));
                    }
                }
                
                // 播放当前歌词
                string sequenceId = $"lrc_line_{i}";
                // LRC播放的序列由StopAllSequences统一管理释放，此处autoRelease传false
                await PlaySubtitleSequenceAsync(sequenceId, config, false);
            }
            
            Log.Info($"[SubtitleModule] LRC播放完成");
        }
        
        /// <summary>
        /// 模块释放
        /// </summary>
        public override void Release()
        {
            // 停止所有序列
            foreach (var sequence in _activeSequences.Values)
            {
                sequence.Stop();
                sequence.Release();
            }
            _activeSequences.Clear();
            
            // 释放所有效果
            foreach (var effect in _activeEffects)
            {
                effect.Release();
            }
            _activeEffects.Clear();
            
            // 清理工厂
            _effectFactories.Clear();
            
            // 释放时序控制器
            _timingController?.Dispose();
            _timingController = null;
            
            // 销毁根节点
            if (_subtitleRoot != null)
            {
                GameObject.Destroy(_subtitleRoot.gameObject);
                _subtitleRoot = null;
            }
            
            base.Release();
            Log.Info("[SubtitleModule] 字幕模块已释放");
        }
    }
}