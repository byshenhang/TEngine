using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

namespace LyricFX.Implementations.Effect
{
    /// <summary>
    /// 文字模糊效果 - 对文字应用模糊材质效果
    /// </summary>
    public class BlurFontEffect : MonoBehaviour, ILyricEffect
    {
        [SerializeField] private Material blurMaterial;
        [SerializeField] private float blurInDuration = 0.5f;
        [SerializeField] private float holdDuration = 1.0f;
        [SerializeField] private float blurOutDuration = 0.5f;
        [SerializeField] private float maxBlurAmount = 1.0f;
        [SerializeField] private AnimationCurve blurInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve blurOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        private TextMeshProUGUI textComponent;
        private Material originalMaterial;
        private Material instancedBlurMaterial;
        private float effectProgress = 0f;
        private bool isCompleted = false;
        private GameObject targetObject;
        
        private static readonly int BlurAmountProperty = Shader.PropertyToID("_BlurAmount");
        
        public bool IsCompleted => isCompleted;
        public float Progress => effectProgress;
        public string EffectId => "blur_font";
        
        private CancellationTokenSource effectCts;
        
        private void Awake()
        {
            // 检查是否有模糊材质
            if (blurMaterial == null)
            {
                // 尝试加载默认的模糊材质
                blurMaterial = Resources.Load<Material>("Materials/BlurFontMaterial");
                
                if (blurMaterial == null)
                {
                    Debug.LogError("[模糊文字效果] 未设置模糊材质，效果将不可用");
                }
            }
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
                Debug.LogError("[模糊文字效果] 目标对象没有TextMeshProUGUI组件");
                return;
            }
            
            // 应用配置（如果有）
            if (config is BlurEffectConfig blurConfig)
            {
                blurInDuration = blurConfig.BlurInDuration;
                holdDuration = blurConfig.HoldDuration;
                blurOutDuration = blurConfig.BlurOutDuration;
                maxBlurAmount = blurConfig.MaxBlurAmount;
                
                if (blurConfig.BlurInCurve != null)
                    blurInCurve = blurConfig.BlurInCurve;
                    
                if (blurConfig.BlurOutCurve != null)
                    blurOutCurve = blurConfig.BlurOutCurve;
                    
                if (blurConfig.BlurMaterial != null)
                    blurMaterial = blurConfig.BlurMaterial;
            }
            
            // 保存原始材质
            originalMaterial = textComponent.fontMaterial;
            
            // 创建模糊材质实例
            if (blurMaterial != null)
            {
                instancedBlurMaterial = new Material(blurMaterial);
                instancedBlurMaterial.SetFloat(BlurAmountProperty, 0); // 初始无模糊
            }
            else
            {
                Debug.LogError("[模糊文字效果] 未设置模糊材质，无法创建实例");
            }
            
            // 重置状态
            isCompleted = false;
            effectProgress = 0f;
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 播放效果
        /// </summary>
        public async UniTask Play(CancellationToken cancellationToken = default)
        {
            if (textComponent == null || targetObject == null || instancedBlurMaterial == null)
                return;
                
            // 创建效果的取消令牌
            StopEffectInternal();
            effectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            try
            {
                // 应用模糊材质
                textComponent.fontMaterial = instancedBlurMaterial;
                
                // 模糊淡入阶段
                await BlurIn(effectCts.Token);
                
                // 保持阶段
                await Hold(effectCts.Token);
                
                // 模糊淡出阶段
                await BlurOut(effectCts.Token);
                
                // 恢复原始材质
                if (textComponent != null && originalMaterial != null)
                {
                    textComponent.fontMaterial = originalMaterial;
                }
                
                isCompleted = true;
                effectProgress = 1.0f;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[模糊文字效果] 效果被取消");
                
                // 恢复原始材质
                if (textComponent != null && originalMaterial != null)
                {
                    textComponent.fontMaterial = originalMaterial;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[模糊文字效果] 播放出错: {ex}");
                
                // 恢复原始材质
                if (textComponent != null && originalMaterial != null)
                {
                    textComponent.fontMaterial = originalMaterial;
                }
            }
        }
        
        /// <summary>
        /// 停止效果
        /// </summary>
        public async UniTask Stop(CancellationToken cancellationToken = default)
        {
            StopEffectInternal();
            
            // 恢复原始材质
            if (textComponent != null && originalMaterial != null)
            {
                textComponent.fontMaterial = originalMaterial;
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
            
            // 恢复原始材质
            if (textComponent != null && originalMaterial != null)
            {
                textComponent.fontMaterial = originalMaterial;
            }
            
            // 重置模糊材质
            if (instancedBlurMaterial != null)
            {
                instancedBlurMaterial.SetFloat(BlurAmountProperty, 0);
            }
            
            isCompleted = false;
            effectProgress = 0f;
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 模糊淡入阶段
        /// </summary>
        private async UniTask BlurIn(CancellationToken cancellationToken)
        {
            float startTime = Time.time;
            float elapsedTime = 0f;
            
            while (elapsedTime < blurInDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                elapsedTime = Time.time - startTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / blurInDuration);
                float blurAmount = blurInCurve.Evaluate(normalizedTime) * maxBlurAmount;
                
                if (instancedBlurMaterial != null)
                {
                    instancedBlurMaterial.SetFloat(BlurAmountProperty, blurAmount);
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
                
                // 保持最大模糊
                if (instancedBlurMaterial != null)
                {
                    instancedBlurMaterial.SetFloat(BlurAmountProperty, maxBlurAmount);
                }
                
                effectProgress = 0.33f + normalizedTime * 0.33f; // 保持阶段占总进度的三分之一
                
                await UniTask.Yield();
            }
        }
        
        /// <summary>
        /// 模糊淡出阶段
        /// </summary>
        private async UniTask BlurOut(CancellationToken cancellationToken)
        {
            float startTime = Time.time;
            float elapsedTime = 0f;
            
            while (elapsedTime < blurOutDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                elapsedTime = Time.time - startTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / blurOutDuration);
                float blurAmount = blurOutCurve.Evaluate(normalizedTime) * maxBlurAmount;
                
                if (instancedBlurMaterial != null)
                {
                    instancedBlurMaterial.SetFloat(BlurAmountProperty, blurAmount);
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
            
            // 销毁实例化的材质
            if (instancedBlurMaterial != null)
            {
                Destroy(instancedBlurMaterial);
            }
        }
    }
    
    /// <summary>
    /// 模糊效果配置
    /// </summary>
    [Serializable]
    public class BlurEffectConfig
    {
        public float BlurInDuration = 0.5f;
        public float HoldDuration = 1.0f;
        public float BlurOutDuration = 0.5f;
        public float MaxBlurAmount = 1.0f;
        public Material BlurMaterial;
        public AnimationCurve BlurInCurve;
        public AnimationCurve BlurOutCurve;
    }
}
