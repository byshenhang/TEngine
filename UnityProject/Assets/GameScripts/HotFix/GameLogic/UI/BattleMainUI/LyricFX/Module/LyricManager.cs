using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using LyricFX.Core;
using LyricFX.Effects;
using LyricFX.Parser;
using LyricFX.Rendering;
using LyricFX.States;

namespace LyricFX.Module
{
    /// <summary>
    /// LyricFX框架的管理器，负责整体歌词处理流程
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class LyricManager : MonoBehaviour
    {
        [Header("资源配置")]
        [SerializeField] private TextAsset lyricFile;
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private Transform container;

        [Header("布局配置")]
        [SerializeField] private float characterSpacing = 30f;
        [SerializeField] private float lineSpacing = 50f;
        [SerializeField] private float lineWidth = 800f;
        [SerializeField] private TextAlignmentOptions alignment = TextAlignmentOptions.Center;
        [SerializeField] private bool autoWrap = true;

        [Header("效果配置")]
        [SerializeField] private float blurStart = 30.0f;
        [SerializeField] private float blurThreshold = 10f;
        [SerializeField] private float blurFadeDuration = 1.0f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private AnimationCurve blurCurve;
        [SerializeField] private AnimationCurve fadeCurve;
        [SerializeField] private bool evenFirstThenOdd = true;
        [SerializeField] private float effectDelay = 0.1f;

        // 内部变量
        private Dictionary<int, List<EffectAdapter>> _lineAdapters = new Dictionary<int, List<EffectAdapter>>();
        private List<GroupEffectController> _groupControllers = new List<GroupEffectController>();
        private List<ICharacterRenderer> _allRenderers = new List<ICharacterRenderer>();
        private LyricSequence _sequence;
        private CancellationTokenSource _cts;
        private float _currentTime = 0f;
        private int _currentLineIndex = -1;

        private void OnEnable()
        {
            if (lyricFile != null)
            {
                LoadAndPlaySequence().Forget();
            }
        }
        
        /// <summary>
        /// 加载并自动播放歌词序列
        /// </summary>
        private async UniTaskVoid LoadAndPlaySequence()
        {
            LyricLogger.Initialize();
            LyricLogger.Log("开始加载和播放歌词序列");
            await LoadAndPrepare();
            await PlaySequence();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts = null;
        }

        /// <summary>
        /// 加载歌词并准备显示
        /// </summary>
        public async UniTask LoadAndPrepare()
        {
            // 取消先前的操作
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                // 清理之前的实例
                CleanupPreviousInstances();

                // 确保容器存在
                EnsureContainer();

                // 解析歌词
                await ParseLyricFile();

                // 创建字符实例并排列
                await CreateAndArrangeCharacters();

                // 准备效果
                PrepareEffects();

                Debug.Log($"LyricFX准备完成: {_sequence.Lines.Count}行歌词, " +
                    $"{_allRenderers.Count}个字符, " +
                    $"时长: {_sequence.TotalDuration}秒");
            }
            catch (Exception ex)
            {
                Debug.LogError($"LyricFX初始化错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 清理之前的实例
        /// </summary>
        private void CleanupPreviousInstances()
        {
            foreach (var renderer in _allRenderers)
            {
                if (renderer is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _allRenderers.Clear();
            _lineAdapters.Clear();

            foreach (var controller in _groupControllers)
            {
                controller.Clear();
            }
            _groupControllers.Clear();

            _currentTime = 0f;
            _currentLineIndex = -1;
        }

        /// <summary>
        /// 确保容器存在
        /// </summary>
        private void EnsureContainer()
        {
            if (container == null)
            {
                container = transform;
            }

            // 清理容器中的子对象
            while (container.childCount > 0)
            {
                DestroyImmediate(container.GetChild(0).gameObject);
            }
        }

        /// <summary>
        /// 解析歌词文件
        /// </summary>
        private async UniTask ParseLyricFile()
        {
            if (lyricFile == null)
            {
                throw new Exception("Lyric file is not set!");
            }

            var parser = new LRCParser();
            _sequence = await parser.ParseAsync(lyricFile.text, _cts.Token);

            if (_sequence == null || _sequence.Lines.Count == 0)
            {
                throw new Exception("Failed to parse lyric file or file is empty!");
            }
        }

        /// <summary>
        /// 创建字符实例并排列它们
        /// </summary>
        private async UniTask CreateAndArrangeCharacters()
        {
            if (_sequence == null || characterPrefab == null)
            {
                return;
            }

            // 为每行歌词创建一个RectTransform作为行容器
            for (int lineIndex = 0; lineIndex < _sequence.Lines.Count; lineIndex++)
            {
                var line = _sequence.Lines[lineIndex];
                
                // 创建行容器
                GameObject lineObject = new GameObject($"Line_{lineIndex}");
                RectTransform lineRect = lineObject.AddComponent<RectTransform>();
                lineRect.SetParent(container, false);
                
                // 设置行位置
                lineRect.anchoredPosition = new Vector2(0, -lineIndex * lineSpacing);
                lineRect.sizeDelta = new Vector2(lineWidth, 40);
                
                // 创建行内容
                await CreateLineCharacters(line, lineRect, lineIndex);
                
                // 每处理几行等待一帧，避免卡顿
                if (lineIndex % 3 == 0)
                {
                    await UniTask.Yield();
                }
            }
        }

        /// <summary>
        /// 为一行创建字符
        /// </summary>
        private async UniTask CreateLineCharacters(LyricLine line, RectTransform lineContainer, int lineIndex)
        {
            // 创建组控制器
            var groupController = new GroupEffectController();
            _groupControllers.Add(groupController);
            
            // 适配器列表
            List<EffectAdapter> lineAdapters = new List<EffectAdapter>();
            _lineAdapters[lineIndex] = lineAdapters;
            
            // 计算字符位置
            float totalWidth = autoWrap 
                ? lineWidth 
                : line.Text.Length * characterSpacing;
                
            float startX = CalculateStartX(totalWidth, alignment);
            
            // 创建字符
            for (int charIndex = 0; charIndex < line.Characters.Count; charIndex++)
            {
                var character = line.Characters[charIndex];
                
                // 创建渲染器
                var renderer = new CharacterRenderer(characterPrefab, lineContainer);
                renderer.SetText(character.Character.ToString());
                
                // 确保字符有BlurFilter组件
                var blurFilter = renderer.GetOrCreateComponent<ChocDino.UIFX.BlurFilter>();
                if (blurFilter != null)
                {
                    blurFilter.Blur = blurStart;
                }
                
                // 初始设置为不可见
                renderer.SetAlpha(0f);
                renderer.SetActive(true);
                
                // 添加到列表
                _allRenderers.Add(renderer);
                
                // 计算位置
                float xPos = CalculateCharacterX(charIndex, startX, totalWidth, alignment, line.Text.Length);
                renderer.SetPosition(new Vector3(xPos, 0, 0));
                
                // 创建适配器
                var adapter = new EffectAdapter(character, renderer);
                lineAdapters.Add(adapter);
                
                // 添加到组控制器
                groupController.AddAdapter(adapter);
                
                // 每创建几个字符等待一帧，避免卡顿
                if (charIndex % 10 == 0 && charIndex > 0)
                {
                    await UniTask.Yield();
                }
            }
        }

        /// <summary>
        /// 计算起始X坐标
        /// </summary>
        private float CalculateStartX(float totalWidth, TextAlignmentOptions alignment)
        {
            if ((alignment & TextAlignmentOptions.Left) != 0)
            {
                return 0;
            }
            else if ((alignment & TextAlignmentOptions.Right) != 0)
            {
                return lineWidth - totalWidth;
            }
            else // Center
            {
                return (lineWidth - totalWidth) / 2;
            }
        }

        /// <summary>
        /// 计算字符X坐标
        /// </summary>
        private float CalculateCharacterX(int index, float startX, float totalWidth, TextAlignmentOptions alignment, int charCount)
        {
            if (autoWrap)
            {
                return startX + (index * (totalWidth / (charCount > 1 ? charCount - 1 : 1)));
            }
            else
            {
                return startX + (index * characterSpacing);
            }
        }

        /// <summary>
        /// 准备效果
        /// </summary>
        private void PrepareEffects()
        {
            foreach (var lineAdapters in _lineAdapters.Values)
            {
                foreach (var adapter in lineAdapters)
                {
                    // 配置入场效果
                    var enterEffects = new List<BaseEffect>
                    {
                        // 模糊效果
                        new BlurEffect(new BlurParameters
                        {
                            StartBlur = blurStart,
                            EndBlur = 0f,
                            Duration = blurFadeDuration,
                            Curve = blurCurve,
                            BlurThreshold = blurThreshold
                        }),
                        
                        // 淡入效果
                        new FadeEffect(new FadeParameters
                        {
                            StartAlpha = 0.0f,
                            EndAlpha = 1.0f,
                            Duration = fadeInDuration,
                            Curve = fadeCurve
                        })
                    };
                    
                    // 配置退场效果
                    var exitEffects = new List<BaseEffect>
                    {
                        // 淡出效果
                        new FadeEffect(new FadeParameters
                        {
                            StartAlpha = 1.0f,
                            EndAlpha = 0.0f,
                            Duration = fadeOutDuration,
                            Curve = fadeCurve
                        })
                    };
                    
                    // 设置效果
                    adapter.ConfigureEffects(CharacterState.Enter, enterEffects);
                    adapter.ConfigureEffects(CharacterState.Exit, exitEffects);
                }
            }
        }

        /// <summary>
        /// 播放特定行
        /// </summary>
        public async UniTask PlayLine(int lineIndex, CancellationToken token)
        {
            LyricLogger.Log($"开始播放行 {lineIndex}");
            
            if (!_lineAdapters.ContainsKey(lineIndex))
            {
                LyricLogger.LogError($"行索引 {lineIndex} 未找到!");
                return;
            }
            
            try
            {
                // 获取该行的组控制器
                if (lineIndex >= 0 && lineIndex < _groupControllers.Count)
                {
                    var groupController = _groupControllers[lineIndex];
                    
                    // 检查第一个字符的初始状态
                    CharacterState firstCharState = groupController.GetFirstCharacterState();
                    bool firstCharActive = groupController.IsFirstCharacterActive();
                    LyricLogger.Log($"播放前第一个字符状态: {firstCharState}, Active={firstCharActive}");
                    
                    // 输出所有字符的状态
                    groupController.LogCharacterStatus(lineIndex);
                    
                    // 计算行播放的总时间
                    float lineDisplayTime = 0;
                    if (lineIndex < _sequence.Lines.Count)
                    {
                        var line = _sequence.Lines[lineIndex];
                        lineDisplayTime = line.EndTime - line.StartTime;
                    }
                    
                    // 字符显示总时间为行的时间包2秒，留出时间给退场效果
                    float displayDuration = Mathf.Max(lineDisplayTime - 2.0f, 1.5f);
                    LyricLogger.Log($"行{lineIndex}的播放时间: {lineDisplayTime:F2}秒, 字符显示时间: {displayDuration:F2}秒");
                    
                    // 使用交替效果
                    //if (evenFirstThenOdd)
                    //{
                    //    LyricLogger.Log($"使用交替效果(先偶后奇) - 行 {lineIndex}");
                        
                    //    // 偶数字符
                    //    LyricLogger.Log("开始激活偶数索引字符");
                    //    await groupController.ActivateInSequence(
                    //        CharacterState.Enter,
                    //        new GroupEffectController.SequenceOptions
                    //        {
                    //            StartIndex = 0,
                    //            Step = 2,
                    //            Delay = effectDelay,
                    //            WaitForCompletion = true,
                    //            CompletionCondition = "blur_below_threshold",
                    //            TotalDuration = displayDuration / 2 // 偶数字符使用一半时间
                    //        },
                    //        token);
                        
                    //    // 再次检查第一个字符状态
                    //    firstCharState = groupController.GetFirstCharacterState();
                    //    firstCharActive = groupController.IsFirstCharacterActive();
                    //    LyricLogger.Log($"激活偶数字符后第一个字符状态: {firstCharState}, Active={firstCharActive}");
                            
                    //    // 奇数字符
                    //    LyricLogger.Log("开始激活奇数索引字符");
                    //    await groupController.ActivateInSequence(
                    //        CharacterState.Enter,
                    //        new GroupEffectController.SequenceOptions
                    //        {
                    //            StartIndex = 1,
                    //            Step = 2,
                    //            Delay = effectDelay,
                    //            WaitForCompletion = true,
                    //            CompletionCondition = "blur_below_threshold",
                    //            TotalDuration = displayDuration / 2 // 奇数字符使用另一半时间
                    //        },
                    //        token);
                    //}
                    //else
                    {
                        LyricLogger.Log($"使用顺序效果 - 行 {lineIndex}");
                        
                        // 顺序执行所有字符
                        await groupController.ActivateInSequence(
                            CharacterState.Enter,
                            new GroupEffectController.SequenceOptions
                            {
                                StartIndex = 0,
                                Step = 1,
                                Delay = effectDelay,
                                WaitForCompletion = false,
                                TotalDuration = displayDuration // 顺序模式使用整个显示时间
                            },
                            token);
                    }
                    
                    // 播放完成后再次检查
                    firstCharState = groupController.GetFirstCharacterState();
                    firstCharActive = groupController.IsFirstCharacterActive();
                    LyricLogger.Log($"播放结束后第一个字符状态: {firstCharState}, Active={firstCharActive}");
                    
                    // 检查如果第一个字符没有激活，尝试强制激活
                    if (!firstCharActive && lineIndex == 0)
                    {
                        LyricLogger.Log("检测到第一行第一个字符未激活，尝试强制激活");
                        
                        // 获取第一个适配器（如果存在）
                        var adapters = _lineAdapters[lineIndex];
                        if (adapters != null && adapters.Count > 0)
                        {
                            try {
                                // 先尝试设置渲染器直接激活
                                var firstAdapter = adapters[0];
                                var rendererField = firstAdapter.GetType().GetField("_renderer",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                
                                if (rendererField != null)
                                {
                                    var renderer = rendererField.GetValue(firstAdapter) as Rendering.ICharacterRenderer;
                                    if (renderer != null)
                                    {
                                        LyricLogger.Log("直接调用SetActive强制激活第一个字符");
                                        renderer.SetActive(true);
                                    }
                                }
                                
                                // 然后执行状态转换
                                await firstAdapter.TransitionTo(CharacterState.Enter, token);
                                await UniTask.Delay(600, cancellationToken: token); // 给Enter状态足够的时间执行淡入效果
                                
                                // 再执行一次状态更新，确保状态完整
                                await firstAdapter.TransitionTo(CharacterState.Stay, token);
                                await UniTask.Delay(300, cancellationToken: token); // 让Stay状态稳定一段时间
                                
                                // 最后检查强制激活结果
                                firstCharActive = groupController.IsFirstCharacterActive();
                                LyricLogger.Log($"强制激活后第一个字符状态: Active={firstCharActive}");
                            }
                            catch (Exception ex) {
                                LyricLogger.LogError($"强制激活失败: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    LyricLogger.LogError($"组控制器索引错误: {lineIndex}, 总数: {_groupControllers.Count}");
                }
            }
            catch (OperationCanceledException)
            {
                LyricLogger.Log("播放行操作被取消");
            }
            catch (Exception ex)
            {
                LyricLogger.LogError($"播放行错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 退出特定行
        /// </summary>
        public async UniTask ExitLine(int lineIndex, CancellationToken token)
        {
            if (!_lineAdapters.ContainsKey(lineIndex))
            {
                return;
            }
            
            try
            {
                if (lineIndex >= 0 && lineIndex < _groupControllers.Count)
                {
                    await _groupControllers[lineIndex].ActivateAll(CharacterState.Exit, token);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exit line error: {ex.Message}");
            }
        }

        /// <summary>
        /// 播放整个歌词序列
        /// </summary>
        public async UniTask PlaySequence()
        {
            if (_sequence == null || _sequence.Lines.Count == 0)
            {
                LyricLogger.LogError("没有可用的歌词序列!");
                return;
            }
            
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            
            _currentTime = 0f;
            _currentLineIndex = -1;
            
            try
            {
                LyricLogger.Log($"开始播放歌词序列... 总行数: {_sequence.Lines.Count}, 总时长: {_sequence.TotalDuration}秒");
                
                // 先确保所有字符都是隐藏状态
                LyricLogger.Log($"设置所有 {_allRenderers.Count} 个字符为隐藏状态");
                
                foreach (var renderer in _allRenderers)
                {
                    renderer.SetAlpha(0f);
                    renderer.SetActive(false);
                }
                LyricLogger.Log("字符隐藏完成");
                
                // 播放第一行，立即启动
                if (_sequence.Lines.Count > 0 && _groupControllers.Count > 0)
                {
                    _currentLineIndex = 0;
                    var firstLine = _sequence.Lines[0];
                    LyricLogger.Log($"直接播放第一行: '{firstLine.Text}', 时间: {firstLine.StartTime}-{firstLine.EndTime}");
                    
                    // 添加短暂延迟以确保初始化完成
                    await UniTask.Delay(100, cancellationToken: _cts.Token); 
                    
                    // 确保第一行字符准备就绪
                    var firstLineAdapters = _lineAdapters[0];
                    LyricLogger.Log($"第一行有 {firstLineAdapters.Count} 个字符适配器");
                    
                    await PlayLine(0, _cts.Token);
                }
                
                float startTime = Time.time;
                
                while (_currentTime < _sequence.TotalDuration)
                {
                    // 更新当前时间
                    _currentTime = Time.time - startTime;
                    
                    // 检查是否有新行需要显示
                    await CheckAndShowLines();
                    
                    await UniTask.Yield(_cts.Token);
                }
                
                // 确保最后一行也有机会退出
                if (_currentLineIndex >= 0)
                {
                    await ExitLine(_currentLineIndex, _cts.Token);
                    _currentLineIndex = -1;
                }
                
                Debug.Log("歌词序列播放完成");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("歌词序列播放已取消");
            }
            catch (Exception ex)
            {
                Debug.LogError($"歌词序列播放错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 检查并显示/隐藏行
        /// </summary>
        private async UniTask CheckAndShowLines()
        {
            Debug.Log($"Checking lines at time: {_currentTime}");
            
            for (int i = 0; i < _sequence.Lines.Count; i++)
            {
                var line = _sequence.Lines[i];
                
                // 检查是否该显示这一行
                if (_currentTime >= line.StartTime && _currentTime < line.EndTime)
                {
                    // 如果是新行
                    if (i != _currentLineIndex)
                    {
                        Debug.Log($"Showing line {i}: '{line.Text}' at time {_currentTime}");
                        
                        // 隐藏之前的行
                        if (_currentLineIndex >= 0)
                        {
                            await ExitLine(_currentLineIndex, _cts.Token);
                        }
                        
                        _currentLineIndex = i;
                        await PlayLine(i, _cts.Token);
                    }
                }
                // 检查是否需要隐藏这一行
                else if (i == _currentLineIndex && _currentTime >= line.EndTime)
                {
                    Debug.Log($"Hiding line {i} at time {_currentTime}");
                    await ExitLine(i, _cts.Token);
                    _currentLineIndex = -1;
                }
            }
        }

        /// <summary>
        /// 设置新的歌词文件
        /// </summary>
        public void SetLyricFile(TextAsset newLyricFile)
        {
            lyricFile = newLyricFile;
            LoadAndPrepare().Forget();
        }

        /// <summary>
        /// 跳转到指定时间
        /// </summary>
        public void SeekTo(float time)
        {
            _currentTime = time;
        }
    }
}
