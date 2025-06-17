using LyricFX.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace LyricFX.Implementations.Effect
{
    /// <summary>
    /// 从左往右依次渐变显示效果 - 字符从左到右逐个淡入显示
    /// </summary>
    public class LeftToRightFadeEffect : ILyricEffect
    {
        private float characterDelay = 0.15f;        // 每个字符之间的延迟
        private float fadeInDuration = 0.4f;         // 每个字符的淡入时间
        private float holdDuration = 2.0f;           // 全部显示后的保持时间
        private float fadeOutDuration = 0.3f;        // 淡出时间
        private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        public bool IsCompleted { get; private set; } = false;
        public float Progress { get; private set; } = 0f;
        public string EffectId => "left_to_right_fade";
        
        // 内部状态
        private List<GameObject> characterObjects = new List<GameObject>();
        private List<TextMeshProUGUI> textComponents = new List<TextMeshProUGUI>();
        private List<Color> originalColors = new List<Color>();
        private CancellationTokenSource effectCts;
        
        /// <summary>
        /// 无参数构造函数
        /// </summary>
        public LeftToRightFadeEffect()
        {
            // 使用默认值
        }
        
        /// <summary>
        /// 构造函数，可以传入配置参数
        /// </summary>
        public LeftToRightFadeEffect(float charDelay = 0.15f, float fadeIn = 0.4f, float hold = 2.0f, float fadeOut = 0.3f)
        {
            characterDelay = charDelay;
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
            
            // 获取行容器
            Transform lineContainer = target.transform;
            
            // 清理之前的数据
            characterObjects.Clear();
            textComponents.Clear();
            originalColors.Clear();
            
            // 收集所有字符对象（子对象）
            for (int i = 0; i < lineContainer.childCount; i++)
            {
                GameObject charObj = lineContainer.GetChild(i).gameObject;
                TextMeshProUGUI textComp = charObj.GetComponent<TextMeshProUGUI>();
                
                if (textComp != null)
                {
                    characterObjects.Add(charObj);
                    textComponents.Add(textComp);
                    originalColors.Add(textComp.color);
                    
                    // 初始时设置为完全透明
                    var color = textComp.color;
                    color.a = 0f;
                    textComp.color = color;
                    
                    // 确保字符对象是激活的
                    charObj.SetActive(true);
                }
            }
            
            // 应用配置（如果有）
            if (config is LeftToRightFadeConfig fadeConfig)
            {
                characterDelay = fadeConfig.CharacterDelay;
                fadeInDuration = fadeConfig.FadeInDuration;
                holdDuration = fadeConfig.HoldDuration;
                fadeOutDuration = fadeConfig.FadeOutDuration;
                
                if (fadeConfig.FadeInCurve != null)
                    fadeInCurve = fadeConfig.FadeInCurve;
                    
                if (fadeConfig.FadeOutCurve != null)
                    fadeOutCurve = fadeConfig.FadeOutCurve;
            }
            
            // 重置状态
            IsCompleted = false;
            Progress = 0f;
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 播放效果
        /// </summary>
        public async UniTask Play(CancellationToken cancellationToken = default)
        {
            if (characterObjects.Count == 0)
                return;
                
            // 创建效果的取消令牌
            StopEffectInternal();
            effectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            try
            {
                var token = effectCts.Token;
                
                // 第一阶段：从左到右依次淡入显示
                await FadeInSequentially(token);
                
                // 第二阶段：保持显示
                await Hold(token);
                
                // 第三阶段：全部淡出
                await FadeOutAll(token);
                
                IsCompleted = true;
                Progress = 1f;
            }
            catch (OperationCanceledException)
            {
                // 效果被取消
            }
            finally
            {
                effectCts?.Dispose();
                effectCts = null;
            }
        }
        
        /// <summary>
        /// 停止效果
        /// </summary>
        public async UniTask Stop(CancellationToken cancellationToken = default)
        {
            StopEffectInternal();
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 重置效果
        /// </summary>
        public async UniTask Reset(CancellationToken cancellationToken = default)
        {
            StopEffectInternal();
            
            // 重置所有字符的透明度
            for (int i = 0; i < textComponents.Count; i++)
            {
                if (textComponents[i] != null)
                {
                    var color = originalColors[i];
                    color.a = 0f;
                    textComponents[i].color = color;
                }
            }
            
            IsCompleted = false;
            Progress = 0f;
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 内部停止效果方法
        /// </summary>
        private void StopEffectInternal()
        {
            effectCts?.Cancel();
            effectCts?.Dispose();
            effectCts = null;
        }
        
        /// <summary>
        /// 从左到右依次淡入显示
        /// </summary>
        private async UniTask FadeInSequentially(CancellationToken cancellationToken)
        {
            float totalFadeInTime = characterObjects.Count * characterDelay + fadeInDuration;
            
            for (int i = 0; i < characterObjects.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 启动当前字符的淡入动画（不等待完成）
                _ = FadeInCharacter(i, cancellationToken);
                
                // 更新进度（淡入阶段占总进度的60%）
                Progress = (float)(i + 1) / characterObjects.Count * 0.6f;
                
                // 等待下一个字符的延迟时间
                if (i < characterObjects.Count - 1)
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
            var originalColor = originalColors[index];
            
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeInDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                float t = elapsedTime / fadeInDuration;
                float alpha = fadeInCurve.Evaluate(t);
                
                var color = originalColor;
                color.a = alpha;
                textComp.color = color;
                
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }
            
            // 确保最终透明度正确
            var finalColor = originalColor;
            finalColor.a = 1f;
            textComp.color = finalColor;
        }
        
        /// <summary>
        /// 保持显示阶段
        /// </summary>
        private async UniTask Hold(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: cancellationToken);
            Progress = 0.8f; // 保持阶段完成后进度为80%
        }
        
        /// <summary>
        /// 全部淡出
        /// </summary>
        private async UniTask FadeOutAll(CancellationToken cancellationToken)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                float t = elapsedTime / fadeOutDuration;
                float alpha = fadeOutCurve.Evaluate(t);
                
                // 同时淡出所有字符
                for (int i = 0; i < textComponents.Count; i++)
                {
                    if (textComponents[i] != null)
                    {
                        var color = originalColors[i];
                        color.a = alpha;
                        textComponents[i].color = color;
                    }
                }
                
                // 更新进度（淡出阶段占剩余的20%）
                Progress = 0.8f + (t * 0.2f);
                
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(cancellationToken);
            }
            
            // 确保最终完全透明
            for (int i = 0; i < textComponents.Count; i++)
            {
                if (textComponents[i] != null)
                {
                    var color = originalColors[i];
                    color.a = 0f;
                    textComponents[i].color = color;
                }
            }
        }
    }
    
    /// <summary>
    /// 从左到右淡入效果配置
    /// </summary>
    [Serializable]
    public class LeftToRightFadeConfig
    {
        public float CharacterDelay = 0.15f;
        public float FadeInDuration = 0.4f;
        public float HoldDuration = 2.0f;
        public float FadeOutDuration = 0.3f;
        public AnimationCurve FadeInCurve;
        public AnimationCurve FadeOutCurve;
    }
}