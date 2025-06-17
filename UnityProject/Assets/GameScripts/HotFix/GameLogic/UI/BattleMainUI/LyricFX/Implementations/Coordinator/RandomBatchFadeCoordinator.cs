using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LyricFX.Core;
using LyricFX.Implementations.Effect;
using UnityEngine;

namespace LyricFX.Implementations.Coordinator
{
    /// <summary>
    /// 随机批量淡入淡出协调器
    /// 实现随机字符分批显示（一次最多5个），最后整体淡出的效果
    /// </summary>
    public class RandomBatchFadeCoordinator : LineEffectCoordinator
    {
        // 配置参数
        private int maxBatchSize = 5;           // 每批最多显示的字符数
        private float batchInterval = 0.3f;     // 批次间隔时间
        private float fadeInDuration = 0.5f;    // 单个字符淡入时间
        private float holdDuration = 2.0f;      // 保持显示时间
        private float fadeOutDuration = 1.0f;   // 整体淡出时间
        private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        // 运行时数据
        private List<DefaultFadeEffect> characterEffects = new List<DefaultFadeEffect>();
        private List<int> remainingIndices = new List<int>();
        
        /// <summary>
        /// 创建字符效果实例
        /// </summary>
        protected override async UniTask CreateCharacterEffects(object config, CancellationToken cancellationToken)
        {
            // 应用配置
            if (config is RandomBatchFadeConfig batchConfig)
            {
                maxBatchSize = batchConfig.MaxBatchSize;
                batchInterval = batchConfig.BatchInterval;
                fadeInDuration = batchConfig.FadeInDuration;
                holdDuration = batchConfig.HoldDuration;
                fadeOutDuration = batchConfig.FadeOutDuration;
                
                if (batchConfig.FadeInCurve != null)
                    fadeInCurve = batchConfig.FadeInCurve;
                    
                if (batchConfig.FadeOutCurve != null)
                    fadeOutCurve = batchConfig.FadeOutCurve;
            }
            
            // 为每个字符创建独立的淡入淡出效果
            characterEffects.Clear();
            for (int i = 0; i < characterObjects.Count; i++)
            {
                var charEffect = new DefaultFadeEffect();
                await charEffect.Initialize(characterObjects[i], null, cancellationToken);
                characterEffects.Add(charEffect);
            }
            
            // 初始化剩余字符索引列表
            remainingIndices.Clear();
            for (int i = 0; i < characterObjects.Count; i++)
            {
                remainingIndices.Add(i);
            }
        }
        
        /// <summary>
        /// 协调效果播放
        /// </summary>
        protected override async UniTask CoordinateEffects(CancellationToken cancellationToken)
        {
            // 第一阶段：随机分批淡入显示
            await RandomBatchFadeIn(cancellationToken);
            
            // 第二阶段：保持显示
            await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: cancellationToken);
            
            // 第三阶段：整体淡出
            await FadeOutAll(cancellationToken);
            
            // 标记完成
            UpdateProgress(1.0f);
            IsCompleted = true;
        }
        
        /// <summary>
        /// 随机分批淡入显示
        /// </summary>
        private async UniTask RandomBatchFadeIn(CancellationToken cancellationToken)
        {
            var random = new System.Random();
            int totalBatches = Mathf.CeilToInt((float)characterObjects.Count / maxBatchSize);
            int currentBatch = 0;
            
            while (remainingIndices.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                // 确定当前批次的字符数量
                int batchSize = Mathf.Min(maxBatchSize, remainingIndices.Count);
                
                // 随机选择字符索引
                var batchIndices = new List<int>();
                for (int i = 0; i < batchSize; i++)
                {
                    int randomIndex = random.Next(remainingIndices.Count);
                    batchIndices.Add(remainingIndices[randomIndex]);
                    remainingIndices.RemoveAt(randomIndex);
                }
                
                // 同时淡入当前批次的所有字符
                var fadeInTasks = new List<UniTask>();
                foreach (int charIndex in batchIndices)
                {
                    fadeInTasks.Add(FadeInCharacter(charIndex, cancellationToken));
                }
                
                // 等待当前批次完成
                await UniTask.WhenAll(fadeInTasks);
                
                // 更新进度（淡入阶段占总进度的70%）
                currentBatch++;
                float fadeInProgress = (float)currentBatch / totalBatches * 0.7f;
                UpdateProgress(fadeInProgress);
                
                // 如果还有剩余字符，等待批次间隔
                if (remainingIndices.Count > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(batchInterval), cancellationToken: cancellationToken);
                }
            }
        }
        
        /// <summary>
        /// 淡入单个字符
        /// </summary>
        private async UniTask FadeInCharacter(int charIndex, CancellationToken cancellationToken)
        {
            var textComponent = textComponents[charIndex];
            var startColor = textComponent.color;
            startColor.a = 0f;
            textComponent.color = startColor;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeInDuration && !cancellationToken.IsCancellationRequested)
            {
                float progress = elapsedTime / fadeInDuration;
                float curveValue = fadeInCurve.Evaluate(progress);
                
                var currentColor = startColor;
                currentColor.a = curveValue;
                textComponent.color = currentColor;
                
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            
            // 确保最终状态
            if (!cancellationToken.IsCancellationRequested)
            {
                var finalColor = startColor;
                finalColor.a = 1f;
                textComponent.color = finalColor;
            }
        }
        
        /// <summary>
        /// 整体淡出所有字符
        /// </summary>
        private async UniTask FadeOutAll(CancellationToken cancellationToken)
        {
            // 获取所有字符的当前颜色
            var startColors = new Color[textComponents.Count];
            for (int i = 0; i < textComponents.Count; i++)
            {
                startColors[i] = textComponents[i].color;
            }
            
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration && !cancellationToken.IsCancellationRequested)
            {
                float progress = elapsedTime / fadeOutDuration;
                float curveValue = fadeOutCurve.Evaluate(progress);
                
                // 同时更新所有字符的透明度
                for (int i = 0; i < textComponents.Count; i++)
                {
                    var currentColor = startColors[i];
                    currentColor.a = curveValue;
                    textComponents[i].color = currentColor;
                }
                
                // 更新进度（淡出阶段占总进度的30%，从70%到100%）
                float fadeOutProgress = 0.7f + progress * 0.3f;
                UpdateProgress(fadeOutProgress);
                
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            
            // 确保最终状态
            if (!cancellationToken.IsCancellationRequested)
            {
                for (int i = 0; i < textComponents.Count; i++)
                {
                    var finalColor = startColors[i];
                    finalColor.a = 0f;
                    textComponents[i].color = finalColor;
                }
            }
        }
    }
    
    /// <summary>
    /// 随机批量淡入淡出配置
    /// </summary>
    [Serializable]
    public class RandomBatchFadeConfig
    {
        [Header("批次设置")]
        [Range(1, 10)]
        public int MaxBatchSize = 5;            // 每批最多显示的字符数
        
        [Range(0.1f, 2.0f)]
        public float BatchInterval = 0.3f;      // 批次间隔时间
        
        [Header("动画时间")]
        [Range(0.1f, 3.0f)]
        public float FadeInDuration = 0.5f;     // 单个字符淡入时间
        
        [Range(0.5f, 5.0f)]
        public float HoldDuration = 2.0f;       // 保持显示时间
        
        [Range(0.5f, 3.0f)]
        public float FadeOutDuration = 1.0f;    // 整体淡出时间
        
        [Header("动画曲线")]
        public AnimationCurve FadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve FadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    }
}