using LyricFX.Core;
using LyricFX.Core.Interfaces;
using LyricFX.Implementations.Effect;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using GameLogic;
using LyricFX.Core.Attributes;

namespace LyricFX.Implementations.Coordinator
{
    /// <summary>
    /// 从左到右渐变效果协调器
    /// 统一管理整行字符的从左到右渐变显示
    /// </summary>
    [EffectConfig(typeof(LeftToRightFadeConfig))]
    public class LeftToRightFadeCoordinator : LineEffectCoordinator
    {
        // 配置参数
        private float characterDelay = 0.15f;
        private float fadeInDuration = 0.4f;
        private float holdDuration = 2.0f;
        private float fadeOutDuration = 0.3f;
        private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        /// <summary>
        /// 创建字符效果实例
        /// </summary>
        protected override async UniTask CreateCharacterEffects(ICoordinatorConfig config, CancellationToken cancellationToken)
        {
            // 应用配置
            if (config is LeftToRightFadeConfig fadeConfig)
            {
                characterDelay = fadeConfig.CharacterDelay;
                fadeInDuration = fadeConfig.InDuration;
                holdDuration = fadeConfig.HoldDuration;
                fadeOutDuration = fadeConfig.OutDuration;
                
                if (fadeConfig.FadeInCurve != null)
                    fadeInCurve = fadeConfig.FadeInCurve;
                    
                if (fadeConfig.FadeOutCurve != null)
                    fadeOutCurve = fadeConfig.FadeOutCurve;
            }
            
            // 为每个字符创建独立的淡入淡出效果
            for (int i = 0; i < characterObjects.Count; i++)
            {
                var charEffect = new DefaultFadeEffect();
                await charEffect.Initialize(characterObjects[i], null, cancellationToken);
                characterEffects.Add(charEffect);
            }
        }
        
        /// <summary>
        /// 协调效果播放
        /// </summary>
        protected override async UniTask CoordinateEffects(CancellationToken cancellationToken)
        {
            // 第一阶段：从左到右依次淡入显示
            await FadeInSequentially(cancellationToken);
            
            // 第二阶段：保持显示
            await Hold(cancellationToken);
            
            // 第三阶段：全部淡出
            await FadeOutAll(cancellationToken);
        }
        
        /// <summary>
        /// 从左到右依次淡入显示
        /// </summary>
        private async UniTask FadeInSequentially(CancellationToken cancellationToken)
        {
            for (int i = 0; i < characterEffects.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 启动当前字符的淡入动画（不等待完成）
                _ = FadeInCharacter(i, cancellationToken);
                
                // 更新进度（淡入阶段占总进度的60%）
                UpdateProgress((float)(i + 1) / characterEffects.Count * 0.6f);
                
                // 等待下一个字符的延迟时间
                if (i < characterEffects.Count - 1)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(characterDelay), cancellationToken: cancellationToken);
                }
            }
            
            // 等待最后一个字符完成淡入
            await UniTask.Delay(TimeSpan.FromSeconds(fadeInDuration), cancellationToken: cancellationToken);
        }
        
        /// <summary>
        /// 单个字符淡入动画
        /// </summary>
        private async UniTask FadeInCharacter(int index, CancellationToken cancellationToken)
        {
            if (index >= textComponents.Count || textComponents[index] == null)
                return;
                
            var textComp = textComponents[index];
            var originalColor = textComp.color;
            
            // 设置初始透明度为0
            var color = originalColor;
            color.a = 0f;
            textComp.color = color;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeInDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                float t = elapsedTime / fadeInDuration;
                float alpha = fadeInCurve.Evaluate(t);
                
                color = originalColor;
                color.a = alpha;
                textComp.color = color;
                
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }
            
            // 确保最终透明度为1
            color = originalColor;
            color.a = 1f;
            textComp.color = color;
        }
        
        /// <summary>
        /// 保持显示
        /// </summary>
        private async UniTask Hold(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: cancellationToken);
            
            // 更新进度（保持阶段占总进度的30%）
            UpdateProgress(0.9f);
        }
        
        /// <summary>
        /// 全部淡出
        /// </summary>
        private async UniTask FadeOutAll(CancellationToken cancellationToken)
        {
            // 同时启动所有字符的淡出动画
            var fadeOutTasks = new UniTask[characterEffects.Count];
            
            for (int i = 0; i < characterEffects.Count; i++)
            {
                fadeOutTasks[i] = FadeOutCharacter(i, cancellationToken);
            }
            
            // 等待所有字符完成淡出
            await UniTask.WhenAll(fadeOutTasks);
            
            // 更新进度到100%
            UpdateProgress(1f);
        }
        
        /// <summary>
        /// 单个字符淡出动画
        /// </summary>
        private async UniTask FadeOutCharacter(int index, CancellationToken cancellationToken)
        {
            if (index >= textComponents.Count || textComponents[index] == null)
                return;
                
            var textComp = textComponents[index];
            var originalColor = textComp.color;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                float t = elapsedTime / fadeOutDuration;
                float alpha = fadeOutCurve.Evaluate(t);
                
                var color = originalColor;
                color.a = alpha;
                textComp.color = color;
                
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }
            
            // 确保最终透明度为0
            var finalColor = originalColor;
            finalColor.a = 0f;
            textComp.color = finalColor;
        }
    }
    
    /// <summary>
    /// 从左到右渐变效果配置
    /// </summary>
    public class LeftToRightFadeConfig : ICoordinatorConfig, IAdjustConfig
    {
        public float CharacterDelay { get; set; } = 0.15f;
        public float InDuration { get; set; } = 0.4f;
        public float OutDuration { get; set; } = 0.3f;
        public float HoldDuration { get; set; } = 2.0f;
        public AnimationCurve FadeInCurve { get; set; }
        public AnimationCurve FadeOutCurve { get; set; }
        
        /// <summary>
        /// 获取总持续时间（包括所有字符的延迟时间）
        /// </summary>
        /// <param name="characterCount">字符数量</param>
        /// <returns>总持续时间</returns>
        public float GetTotalDuration(int characterCount = 10)
        {
            // 估算字符延迟总时间（默认假设10个字符）
            float totalCharacterDelay = CharacterDelay * (characterCount - 1);
            
            // 总时间 = 字符延迟总时间 + 最后一个字符的淡入时间 + 保持时间 + 淡出时间
            return totalCharacterDelay + InDuration + HoldDuration + OutDuration;
        }
        
        /// <summary>
        /// 根据可用时间调整持续时间，保持原有比例
        /// </summary>
        /// <param name="availableDuration">可用的总时间</param>
        /// <param name="characterCount">字符数量</param>
        public void AdjustDuration(float availableDuration, int characterCount = 10)
        {
            if (availableDuration <= 0)
                return;
                
            // 计算当前总时长
            float totalDuration = GetTotalDuration(characterCount);
            // 如果可用时间小于总时长，按比例缩放
            if (availableDuration < totalDuration)
            {
                var oldTime = GetTotalDuration(characterCount);

                float ratio = availableDuration / totalDuration;
                
                // 保持最小时间
                float minDuration = 0.05f;
                float minDelay = 0.02f;
                
                // 计算新的时间，确保各阶段至少有最小时间
                CharacterDelay = Mathf.Max(CharacterDelay * ratio, minDelay);
                InDuration = Mathf.Max(InDuration * ratio, minDuration);
                OutDuration = Mathf.Max(OutDuration * ratio, minDuration);
                
                // 剩余时间分配给保持阶段
                float totalCharacterDelay = CharacterDelay * (characterCount - 1);
                float remainingTime = availableDuration - totalCharacterDelay - InDuration - OutDuration;
                HoldDuration = Mathf.Max(remainingTime, 0);

                Debug.Log("修正时间 >> :" + GetTotalDuration(characterCount) + "  : 修正时间 >> : " + oldTime);
            }
            else
            {
                // 如果可用时间充足，增加保持时间
                HoldDuration += (availableDuration - totalDuration);
            }
        }

       
    }
}