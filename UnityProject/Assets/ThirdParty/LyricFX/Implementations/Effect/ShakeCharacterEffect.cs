using Cysharp.Threading.Tasks;
using LyricFX.Core.Attributes;
using LyricFX.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

namespace LyricFX.Implementations.Effect
{
    /// <summary>
    /// 字符抖动效果 - 每个字符有不同的抖动效果，最后整体渐变消失
    /// </summary>
    [EffectConfig(typeof(ShakeEffectConfig))]
    public class ShakeCharacterEffect : ILyricEffect
    {
        public string EffectId => "shake_character";
        public bool IsCompleted { get; private set; }
        public float Progress => effectProgress;
        
        private TextMeshProUGUI textComponent;
        private GameObject targetObject;
        private float effectProgress = 0f;
        private CancellationTokenSource effectCts;
        private Vector3 originalPosition;
        private Dictionary<int, Vector3> characterOffsets = new Dictionary<int, Vector3>();
        
        // 效果配置参数
        public float ShakeDuration { get; set; } = 1.5f;     // 抖动持续时间
        public float HoldDuration { get; set; } = 0.8f;      // 保持时间
        public float FadeOutDuration { get; set; } = 0.5f;   // 淡出持续时间
        public float ShakeIntensity { get; set; } = 0.1f;      // 抖动强度
        public float ShakeFrequency { get; set; } = 10f;     // 抖动频率
        public float CharTimeOffset { get; set; } = 0.01f;   // 字符间抖动时间偏移
        private bool isShaking = false;
        
        /// <summary>
        /// 初始化效果
        /// </summary>
        public async UniTask Initialize(GameObject target, IEffectConfig config, CancellationToken cancellationToken = default)
        {
            targetObject = target;
            textComponent = target.GetComponent<TextMeshProUGUI>();
            
            if (textComponent == null)
            {
                Debug.LogError("[字符抖动效果] 目标对象没有TextMeshProUGUI组件");
                return;
            }
            
            originalPosition = target.transform.localPosition;
            
            // 为每个字符生成一个随机抖动偏移方向向量
            characterOffsets.Clear();
            
            IsCompleted = false;
            effectProgress = 0f;
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 初始化抖动偏移方向
        /// </summary>
        private void InitializeCharacterOffsets()
        {
            characterOffsets.Clear();
            
            // 为每个字符生成随机的抖动偏移方向
            float angleRad = UnityEngine.Random.Range(0, Mathf.PI * 2);
            Vector3 direction = new Vector3(
                Mathf.Cos(angleRad),
                Mathf.Sin(angleRad),
                0
            ).normalized;
            
            characterOffsets[0] = direction;
        }
        
        /// <summary>
        /// 播放效果
        /// </summary>
        public async UniTask Play(CancellationToken cancellationToken = default)
        {
            if (textComponent == null)
            {
                Debug.LogError("[字符抖动效果] 文本组件为空，无法播放效果");
                IsCompleted = true;
                return;
            }
            
            StopEffectInternal();
            
            effectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = effectCts.Token;
            
            try
            {
                // 等待一帧确保布局已完全应用
                await UniTask.Yield(cancellationToken);
                
                // 保存原始位置
                originalPosition = targetObject.transform.localPosition;
                
                // 设置初始状态
                Color originalColor = textComponent.color;
                
                // 开始抖动动画
                isShaking = true;
                
                // 使用配置的总时间
                float totalDuration = ShakeDuration + HoldDuration + FadeOutDuration;
                
                // 初始化抖动偏移方向
                InitializeCharacterOffsets();
                
                // 启动持续抖动效果（不会自动停止）
                _ = ApplyShakeEffectContinuous(linkedToken);
                
                // 等待指定时间后仅设置完成状态，但不停止抖动
                await UniTask.Delay(TimeSpan.FromSeconds(totalDuration), cancellationToken: linkedToken);
                
                // 如果被取消，提前结束
                if (linkedToken.IsCancellationRequested) return;
                
                // 直接更新总体进度
                effectProgress = 1.0f;
                
                // 直接消失，不渐变
                if (textComponent != null)
                {
                    textComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 0f);
                }
                
                // 恢复原始状态
                IsCompleted = true;
                effectProgress = 1f;
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，忽略异常
            }
            catch (Exception ex)
            {
                Debug.LogError($"[字符抖动效果] 播放过程中发生错误: {ex.Message}");
            }
            finally
            {
                isShaking = false;
                if (textComponent != null)
                {
                    // 恢复原始颜色和位置
                    textComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 1f);
                    targetObject.transform.localPosition = originalPosition;
                }
            }
        }
        
        /// <summary>
        /// 应用持续性字符抖动效果，不会自动停止
        /// </summary>
        private async UniTask ApplyShakeEffectContinuous(CancellationToken cancellationToken)
        {
            if (targetObject == null) return;
            
            try
            {
                float elapsed = 0f;
                isShaking = true; // 确保抖动状态持续有效
                
                while (!cancellationToken.IsCancellationRequested && targetObject != null)
                {
                    // 计算抖动偏移
                    float characterTime = elapsed;
                    
                    // 生成抖动偏移
                    if (characterOffsets.TryGetValue(0, out Vector3 direction))
                    {
                        float shakeAmount = Mathf.Sin(characterTime * ShakeFrequency) * ShakeIntensity;
                        Vector3 offset = direction * shakeAmount;
                        
                        // 基于原始位置应用抖动偏移到Transform
                        targetObject.transform.localPosition = originalPosition + offset;
                    }
                    
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，忽略异常
            }
            catch (Exception ex)
            {
                Debug.LogError($"[字符抖动效果] 应用抖动效果时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 应用字符抖动效果（有时间限制）
        /// </summary>
        private async UniTask ApplyShakeEffect(float duration, CancellationToken cancellationToken)
        {
            if (targetObject == null) return;
            
            try
            {
                float elapsed = 0f;
                
                while (elapsed < duration && isShaking)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    // 计算抖动偏移
                    float characterTime = elapsed;
                    
                    // 生成抖动偏移
                    if (characterOffsets.TryGetValue(0, out Vector3 direction))
                    {
                        float shakeAmount = Mathf.Sin(characterTime * ShakeFrequency) * ShakeIntensity;
                        Vector3 offset = direction * shakeAmount;
                        
                        // 基于原始位置应用抖动偏移到Transform
                        targetObject.transform.localPosition = originalPosition + offset;
                    }
                    
                    // 更新进度
                    effectProgress = elapsed / duration;
                    
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，忽略异常
            }
            catch (Exception ex)
            {
                Debug.LogError($"[字符抖动效果] 应用抖动效果时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 内部方法：停止当前效果
        /// </summary>
        private void StopEffectInternal()
        {
            if (effectCts != null && !effectCts.IsCancellationRequested)
            {
                effectCts.Cancel();
                effectCts.Dispose();
                effectCts = null;
            }
        }
        
        /// <summary>
        /// 停止效果
        /// </summary>
        public async UniTask Stop(CancellationToken cancellationToken)
        {
            StopEffectInternal();
            
            // 立即恢复正常状态
            if (textComponent != null)
            {
                textComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 1f);
            }
            
            if (targetObject != null)
            {
                targetObject.transform.localPosition = originalPosition;
            }
            
            isShaking = false;
            IsCompleted = true;
            effectProgress = 1f;
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 重置效果状态
        /// </summary>
        public async UniTask Reset(CancellationToken cancellationToken)
        {
            StopEffectInternal();
            
            if (textComponent != null)
            {
                textComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 0f);
            }
            
            if (targetObject != null)
            {
                targetObject.transform.localPosition = originalPosition;
            }
            
            // 清理数据
            characterOffsets.Clear();
            
            isShaking = false;
            IsCompleted = false;
            effectProgress = 0f;
            
            await UniTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// 字符抖动效果配置
    /// </summary>
    [System.Serializable]
    public class ShakeEffectConfig : IEffectConfig, IAdjustConfig
    {
        // 各阶段持续时间
        public float ShakeDuration = 1.5f;
        public float HoldDuration = 0.8f;
        public float FadeOutDuration = 0.5f;
        
        // 抖动强度
        public float ShakeIntensity = 5f;
        
        // 抖动速度
        public float ShakeFrequency = 10f;
        
        // 字符间抖动时间偏移
        public float CharTimeOffset = 0.05f;
        
        public void AdjustDuration(float availableDuration, int characterCount)
        {
            if (availableDuration <= 0)
                return;
                
            // 计算当前总时长
            float totalDuration = GetTotalDuration(characterCount);
            
            // 如果可用时间小于总时长，按比例缩放
            if (availableDuration < totalDuration && totalDuration > 0)
            {
                float ratio = availableDuration / totalDuration;
                
                // 保持最小时间
                float minDuration = 0.05f;
                
                // 按比例调整各阶段时间，确保不小于最小时间
                ShakeDuration = Mathf.Max(ShakeDuration * ratio, minDuration);
                HoldDuration = Mathf.Max(HoldDuration * ratio, minDuration);
                FadeOutDuration = Mathf.Max(FadeOutDuration * ratio, minDuration);
                
                // 验证总持续时间是否符合期望
                float adjustedTotal = ShakeDuration + HoldDuration + FadeOutDuration;
                
                // 如果调整后总时间超过了可用时间，从保持阶段减去多余时间
                if (adjustedTotal > availableDuration)
                {
                    float excess = adjustedTotal - availableDuration;
                    HoldDuration = Mathf.Max(HoldDuration - excess, 0.01f);
                }
                
                Debug.Log($"[ShakeEffectConfig] 调整持续时间: {ShakeDuration:F2}+{HoldDuration:F2}+{FadeOutDuration:F2}={GetTotalDuration(characterCount):F2}s");
            }
            else if (availableDuration > totalDuration)
            {
                // 如果可用时间充足，增加保持时间
                HoldDuration += (availableDuration - totalDuration);
                Debug.Log($"[ShakeEffectConfig] 增加保持时间: {HoldDuration:F2}s, 总时间={GetTotalDuration(characterCount):F2}s");
            }
        }

        public float GetTotalDuration(int characterCount)
        {
            // 字符效果总持续时间是抖动+保持+淡出的总和
            // 字符数量在字符级效果里不影响总时间
            return ShakeDuration + HoldDuration + FadeOutDuration;
        }
    }
}
