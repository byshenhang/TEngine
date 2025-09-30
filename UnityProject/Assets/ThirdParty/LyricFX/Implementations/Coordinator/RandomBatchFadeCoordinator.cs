using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic;
using LyricFX.Core;
using LyricFX.Core.Attributes;
using LyricFX.Implementations.Effect;
using LyricFX.Managers;
using UnityEngine;

namespace LyricFX.Implementations.Coordinator
{
    /// <summary>
    /// 随机批量显示协调器
    /// 实现随机字符分批显示（一次最多5个），简化版本无淡入淡出效果
    /// </summary>
    [EffectConfig(typeof(RandomBatchFadeConfig))]
    public class RandomBatchFadeCoordinator : LineEffectCoordinator
    {
        // 配置参数
        private int maxBatchSize = 5;           // 每批最多显示的字符数
        private float batchInterval = 0.3f;     // 批次间隔时间
        private float holdDuration = 2.0f;      // 保持显示时间
        
        // 运行时数据
        private List<DefaultFadeEffect> characterEffects = new List<DefaultFadeEffect>();
        private List<int> remainingIndices = new List<int>();
        
        /// <summary>
        /// 创建字符效果实例
        /// </summary>
        protected override async UniTask CreateCharacterEffects(ICoordinatorConfig config, CancellationToken cancellationToken)
        {
            // 应用配置
            if (config is RandomBatchFadeConfig batchConfig)
            {
                maxBatchSize = batchConfig.MaxBatchSize;
                batchInterval = batchConfig.BatchInterval;
                holdDuration = batchConfig.HoldDuration;
            }
            
            // 初始化所有字符为隐藏状态
            for (int i = 0; i < textComponents.Count; i++)
            {
                var color = textComponents[i].color;
                color.a = 0f;
                textComponents[i].color = color;
            }
            
            // 初始化剩余字符索引列表
            remainingIndices.Clear();
            for (int i = 0; i < characterObjects.Count; i++)
            {
                remainingIndices.Add(i);
            }
            
            await UniTask.Yield(cancellationToken);
        }
        
        /// <summary>
        /// 协调效果播放
        /// </summary>
        protected override async UniTask CoordinateEffects(CancellationToken cancellationToken)
        {
            // 第一阶段：随机分批显示
            await RandomBatchShow(cancellationToken);
            
            // 第二阶段：保持显示
            await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: cancellationToken);
            
            // 标记完成
            UpdateProgress(1.0f);
            IsCompleted = true;
        }
        
        /// <summary>
        /// 随机分批显示
        /// </summary>
        private async UniTask RandomBatchShow(CancellationToken cancellationToken)
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
                
                // 直接显示当前批次的所有字符
                foreach (int charIndex in batchIndices)
                {
                    ShowCharacter(charIndex);
                }
                
                // 更新进度（显示阶段占总进度的80%）
                currentBatch++;
                float showProgress = (float)currentBatch / totalBatches * 0.8f;
                UpdateProgress(showProgress);
                
                // 如果还有剩余字符，等待批次间隔
                if (remainingIndices.Count > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(batchInterval), cancellationToken: cancellationToken);
                }
            }
        }
        
        /// <summary>
        /// 直接显示单个字符
        /// </summary>
        private void ShowCharacter(int charIndex)
        {
            if (charIndex >= 0 && charIndex < textComponents.Count)
            {
                var textComponent = textComponents[charIndex];
                var color = textComponent.color;
                color.a = 1f;
                textComponent.color = color;
            }
        }
    }
    
    /// <summary>
    /// 随机分批显示效果配置
    /// </summary>
    [System.Serializable]
    public class RandomBatchFadeConfig : ICoordinatorConfig
    {
        [Header("批次设置")]
        [Range(1, 10)]
        public int MaxBatchSize = 3;
        
        [Range(0.1f, 2.0f)]
        public float BatchInterval = 0.3f;

        public float HoldDuration { get; set; } = 2.0f;

        public float GetTotalDuration(int characterCount)
        {
            if (characterCount <= 0) return HoldDuration;
            
            // 计算批次数量
            int batchCount = Mathf.CeilToInt((float)characterCount / MaxBatchSize);
            
            // 批次阶段时间：(批次数-1) × 间隔时间
            float batchPhaseTime = (batchCount - 1) * BatchInterval;
            
            // 总时间 = 批次阶段时间 + 保持时间
            return batchPhaseTime + HoldDuration;
        }

        public void AdjustDuration(float availableDuration, int characterCount)
        {
            if (availableDuration <= 0.1f) // 最小时间保护
                return;
            
            // 简化调整逻辑：按比例缩放所有时间参数
            float currentTotal = GetTotalDuration(characterCount);
            
            if (availableDuration < currentTotal)
            {
                // 时间不足，按比例缩放
                float scale = availableDuration / currentTotal;
                scale = Mathf.Max(scale, 0.2f); // 最小缩放比例，避免过度压缩
                
                BatchInterval = Mathf.Max(BatchInterval * scale, 0.05f);
                HoldDuration = Mathf.Max(HoldDuration * scale, 0.2f);
                
                Debug.Log($"[RandomBatchFadeConfig] 时间调整: {currentTotal:F2}s -> {GetTotalDuration(characterCount):F2}s (目标: {availableDuration:F2}s)");
            }
            else
            {
                // 时间充足，延长保持时间
                HoldDuration += (availableDuration - currentTotal);
            }
        }
    }
}