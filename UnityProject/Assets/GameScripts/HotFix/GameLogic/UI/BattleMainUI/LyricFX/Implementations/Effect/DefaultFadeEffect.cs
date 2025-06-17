using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

namespace LyricFX.Implementations.Effect
{
    /// <summary>
    /// 默认淡入淡出效果 - 简单的透明度变化效果
    /// </summary>
    public class DefaultFadeEffect : ILyricEffect
    {
        private float fadeInDuration = 0.3f;
        private float holdDuration = 1.0f;
        private float fadeOutDuration = 0.3f;
        private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        private TextMeshProUGUI textComponent;
        private float effectProgress = 0f;
        private bool isCompleted = false;
        private GameObject targetObject;
        
        public bool IsCompleted => isCompleted;
        public float Progress => effectProgress;
        public string EffectId => "default_fade";
        
        private CancellationTokenSource effectCts;
        
        /// <summary>
        /// 构造函数，可以传入配置参数
        /// </summary>
        public DefaultFadeEffect(float fadeIn = 0.3f, float hold = 1.0f, float fadeOut = 0.3f)
        {
            fadeInDuration = fadeIn;
            holdDuration = hold;
            fadeOutDuration = fadeOut;
        }
        
        /// <summary>
        /// 初始化效果
        /// </summary>
        public async UniTask Initialize(GameObject target, object config, CancellationToken cancellationToken = default)
        {
            // 取消之前的效果
            StopEffectInternal();
            
            targetObject = target;
            textComponent = target.GetComponent<TextMeshProUGUI>();
            
            if (textComponent == null)
            {
                Debug.LogError("[默认淡入淡出效果] 目标对象没有TextMeshProUGUI组件");
                return;
            }
            
            // 应用配置（如果有）
            if (config is FadeEffectConfig fadeConfig)
            {
                fadeInDuration = fadeConfig.FadeInDuration;
                holdDuration = fadeConfig.HoldDuration;
                fadeOutDuration = fadeConfig.FadeOutDuration;
                
                if (fadeConfig.FadeInCurve != null)
                    fadeInCurve = fadeConfig.FadeInCurve;
                    
                if (fadeConfig.FadeOutCurve != null)
                    fadeOutCurve = fadeConfig.FadeOutCurve;
            }
            
            // 重置状态
            isCompleted = false;
            effectProgress = 0f;
            
            // 初始透明度为0
            if (textComponent != null)
            {
                var color = textComponent.color;
                color.a = 0f;
                textComponent.color = color;
            }
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 播放效果
        /// </summary>
        public async UniTask Play(CancellationToken cancellationToken = default)
        {
            if (textComponent == null || targetObject == null)
                return;
                
            // 创建效果的取消令牌
            StopEffectInternal();
            effectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            try
            {
                // 淡入阶段
                await FadeIn(effectCts.Token);
                
                // 保持阶段
                await Hold(effectCts.Token);
                
                // 淡出阶段
                await FadeOut(effectCts.Token);
                
                isCompleted = true;
                effectProgress = 1.0f;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[默认淡入淡出效果] 效果被取消");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[默认淡入淡出效果] 播放出错: {ex}");
            }
        }
        
        /// <summary>
        /// 停止效果
        /// </summary>
        public async UniTask Stop(CancellationToken cancellationToken = default)
        {
            StopEffectInternal();
            
            // 立即显示效果结果
            if (textComponent != null)
            {
                var color = textComponent.color;
                color.a = 1.0f;
                textComponent.color = color;
            }
            
            isCompleted = true;
            effectProgress = 1.0f;
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 重置效果状态
        /// </summary>
        public async UniTask Reset(CancellationToken cancellationToken = default)
        {
            StopEffectInternal();
            
            if (textComponent != null)
            {
                var color = textComponent.color;
                color.a = 0f;
                textComponent.color = color;
            }
            
            isCompleted = false;
            effectProgress = 0f;
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 淡入阶段
        /// </summary>
        private async UniTask FadeIn(CancellationToken cancellationToken)
        {
            float startTime = Time.time;
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeInDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                elapsedTime = Time.time - startTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / fadeInDuration);
                float alpha = fadeInCurve.Evaluate(normalizedTime);
                
                if (textComponent != null)
                {
                    var color = textComponent.color;
                    color.a = alpha;
                    textComponent.color = color;
                }
                
                effectProgress = normalizedTime * 0.33f; // 淡入占总进度的三分之一
                
                await UniTask.Yield();
            }
        }
        
        /// <summary>
        /// 保持阶段
        /// </summary>
        private async UniTask Hold(CancellationToken cancellationToken)
        {
            float startTime = Time.time;
            float elapsedTime = 0f;
            
            while (elapsedTime < holdDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                elapsedTime = Time.time - startTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / holdDuration);
                
                // 保持透明度为1
                if (textComponent != null)
                {
                    var color = textComponent.color;
                    color.a = 1.0f;
                    textComponent.color = color;
                }
                
                effectProgress = 0.33f + normalizedTime * 0.33f; // 保持阶段占总进度的三分之一
                
                await UniTask.Yield();
            }
        }
        
        /// <summary>
        /// 淡出阶段
        /// </summary>
        private async UniTask FadeOut(CancellationToken cancellationToken)
        {
            float startTime = Time.time;
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                elapsedTime = Time.time - startTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / fadeOutDuration);
                float alpha = fadeOutCurve.Evaluate(normalizedTime);
                
                if (textComponent != null)
                {
                    var color = textComponent.color;
                    color.a = alpha;
                    textComponent.color = color;
                }
                
                effectProgress = 0.66f + normalizedTime * 0.34f; // 淡出占总进度的三分之一多一点
                
                await UniTask.Yield();
            }
        }
        
        /// <summary>
        /// 停止内部效果
        /// </summary>
        private void StopEffectInternal()
        {
            if (effectCts != null)
            {
                if (!effectCts.IsCancellationRequested)
                {
                    effectCts.Cancel();
                }
                
                effectCts.Dispose();
                effectCts = null;
            }
        }
        
        private void OnDestroy()
        {
            StopEffectInternal();
        }
    }
    
    /// <summary>
    /// 淡入淡出效果配置
    /// </summary>
    [System.Serializable]
    public class FadeEffectConfig
    {
        public float FadeInDuration = 0.3f;
        public float HoldDuration = 1.0f;
        public float FadeOutDuration = 0.3f;
        public AnimationCurve FadeInCurve;
        public AnimationCurve FadeOutCurve;
    }
}
