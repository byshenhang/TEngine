using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using ChocDino.UIFX;
using LyricFX.Core;
using LyricFX.Effects;
using LyricFX.States;
using LyricFX.Rendering;

namespace LyricFX.Module
{
    /// <summary>
    /// LyricFX框架的主模块，负责整体歌词效果的管理和控制
    /// </summary>
    public class LyricModule : MonoBehaviour
    {
        [Header("基础设置")]
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private Transform container;
        [SerializeField] private TextAsset lyricFile;

        [Header("文本设置")]
        [SerializeField] private string displayText = "I saw you";
        [SerializeField] private float characterSpacing = 1.0f;
        [SerializeField] private float lineSpacing = 1.5f;

        [Header("效果设置")]
        [SerializeField] private float blurStart = 30.0f;
        [SerializeField] private float blurThreshold = 10f;
        [SerializeField] private float blurFadeDuration = 1.0f;
        [SerializeField] private float finalFadeDuration = 0.5f;
        [SerializeField] private AnimationCurve blurCurve;

        // 内部变量
        private List<ICharacterRenderer> _characterRenderers = new List<ICharacterRenderer>();
        private List<EffectAdapter> _effectAdapters = new List<EffectAdapter>();
        private GroupEffectController _groupController;
        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            InitializeAndPlay().Forget();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts = null;
        }

        /// <summary>
        /// 初始化并播放歌词效果
        /// </summary>
        public async UniTask InitializeAndPlay()
        {
            // 取消先前的操作
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            // 清理之前的实例
            CleanupPreviousInstances();

            // 创建字符实例
            await CreateCharacterInstances();

            // 创建效果适配器
            CreateEffectAdapters();

            // 播放效果序列
            await PlayEffectSequence(_cts.Token);
        }

        /// <summary>
        /// 清理之前的实例
        /// </summary>
        private void CleanupPreviousInstances()
        {
            foreach (var renderer in _characterRenderers)
            {
                if (renderer is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _characterRenderers.Clear();
            _effectAdapters.Clear();
            _groupController = null;
        }

        /// <summary>
        /// 创建字符实例
        /// </summary>
        private async UniTask CreateCharacterInstances()
        {
            // 确保容器和预制体存在
            if (container == null)
            {
                container = transform;
            }

            if (characterPrefab == null)
            {
                Debug.LogError("Character prefab is not set!");
                return;
            }

            // 处理文本
            string textToDisplay = string.IsNullOrEmpty(displayText) ? "Empty Text" : displayText;
            
            // 创建字符对象
            for (int i = 0; i < textToDisplay.Length; i++)
            {
                // 创建渲染器
                var renderer = new CharacterRenderer(characterPrefab, container);
                renderer.SetText(textToDisplay[i].ToString());
                renderer.SetActive(false);
                
                // 计算位置
                float xPos = i * characterSpacing;
                renderer.SetPosition(new Vector3(xPos, 0, 0));
                
                // 添加到列表
                _characterRenderers.Add(renderer);
                
                // 添加模糊组件
                var blurFilter = renderer.GetOrCreateComponent<BlurFilter>();
                blurFilter.Blur = blurStart;
                
                // 防止频繁的实例化导致卡顿
                if (i % 5 == 0)
                {
                    await UniTask.Yield();
                }
            }
        }

        /// <summary>
        /// 创建效果适配器
        /// </summary>
        private void CreateEffectAdapters()
        {
            // 创建组控制器
            _groupController = new GroupEffectController();
            
            // 为每个字符创建效果适配器
            for (int i = 0; i < _characterRenderers.Count; i++)
            {
                // 创建字符数据
                var characterData = new LyricCharacter
                {
                    Character = displayText[i],
                    Index = i,
                    LineIndex = 0
                };
                
                // 创建适配器
                var adapter = new EffectAdapter(characterData, _characterRenderers[i]);
                
                // 配置效果
                ConfigureEffects(adapter);
                
                // 添加到集合
                _effectAdapters.Add(adapter);
                _groupController.AddAdapter(adapter);
            }
        }

        /// <summary>
        /// 配置字符效果
        /// </summary>
        private void ConfigureEffects(EffectAdapter adapter)
        {
            // 创建Enter阶段效果
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
                })
            };
            
            // 创建Exit阶段效果
            var exitEffects = new List<BaseEffect>
            {
                // 淡出效果
                new FadeEffect(new FadeParameters
                {
                    StartAlpha = 1.0f,
                    EndAlpha = 0.0f,
                    Duration = finalFadeDuration
                })
            };
            
            // 配置效果
            adapter.ConfigureEffects(CharacterState.Enter, enterEffects);
            adapter.ConfigureEffects(CharacterState.Exit, exitEffects);
        }

        /// <summary>
        /// 播放效果序列
        /// </summary>
        private async UniTask PlayEffectSequence(CancellationToken token)
        {
            try
            {
                // 等待1秒
                await UniTask.Delay(1000, cancellationToken: token);

                // 第一轮：激活偶数位置的字符
                await _groupController.ActivateInSequence(
                    CharacterState.Enter,
                    new GroupEffectController.SequenceOptions
                    {
                        StartIndex = 0,
                        Step = 2,
                        Delay = 0.1f,
                        WaitForCompletion = true,
                        CompletionCondition = "blur_below_threshold"
                    },
                    token);

                // 第二轮：激活奇数位置的字符
                await _groupController.ActivateInSequence(
                    CharacterState.Enter,
                    new GroupEffectController.SequenceOptions
                    {
                        StartIndex = 1,
                        Step = 2,
                        Delay = 0.1f,
                        WaitForCompletion = true,
                        CompletionCondition = "blur_below_threshold"
                    },
                    token);

                // 等待所有字符都完成显示
                await UniTask.Delay(500, cancellationToken: token);

                // 所有字符一起淡出
                await _groupController.ActivateAll(CharacterState.Exit, token);
            }
            catch (OperationCanceledException)
            {
                // 预期的取消
                Debug.Log("LyricFX sequence canceled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in LyricFX sequence: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置要显示的文本
        /// </summary>
        public void SetText(string text)
        {
            displayText = text;
            InitializeAndPlay().Forget();
        }
    }
}
