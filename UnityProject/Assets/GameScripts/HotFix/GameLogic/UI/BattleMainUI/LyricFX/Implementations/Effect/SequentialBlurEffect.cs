using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

namespace LyricFX.Implementations.Effect
{
    /// <summary>
    /// 顺序模糊效果 - 字符逐个出现的模糊到清晰效果
    /// </summary>
    public class SequentialBlurEffect : ILyricEffect
    {
        private float characterDelay = 0.1f;
        private float blurDuration = 0.5f;
        private float maxBlurStrength = 5.0f;
        private AnimationCurve blurCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        private Material blurMaterial;
        private float blurStart = 30.0f;
        private float blurThreshold = 10f;
        private float blurFadeDuration = 1.0f;
        private float finalFadeDuration = 0.5f;
        private float delayBetweenRounds = 0.5f;
        
        public bool IsCompleted { get; private set; } = false;
        public float Progress { get; private set; } = 0f;
        public string EffectId => "sequential_blur";
        
        private static readonly int BlurAmountProperty = Shader.PropertyToID("_BlurAmount");
        
        // 内部状态
        private List<GameObject> characterObjects = new List<GameObject>();
        private List<TextMeshProUGUI> textComponents = new List<TextMeshProUGUI>();
        private List<Material> originalMaterials = new List<Material>();
        private List<Material> blurMaterials = new List<Material>();
        private CancellationTokenSource effectCts;
        
        /// <summary>
        /// 构造函数，可以传入配置参数
        /// </summary>
        public SequentialBlurEffect(float charDelay = 0.1f, float blurDur = 0.5f, float maxBlur = 5.0f)
        {
            characterDelay = charDelay;
            blurDuration = blurDur;
            maxBlurStrength = maxBlur;
            
            // 检查是否有模糊材质
            if (blurMaterial == null)
            {
                Debug.LogWarning("[序列模糊效果] 未设置模糊材质，将使用默认材质");
            }
        }
        
        /// <summary>
        /// 初始化效果
        /// </summary>
        public async UniTask Initialize(GameObject target, IEffectConfig config, CancellationToken cancellationToken = default)
        {
            // 取消之前的效果
            StopEffectInternal();
            
            // 在初始化时不期望传入单个字符，而是行容器
            Transform lineContainer = target.transform;
            
            // 清理之前的数据
            characterObjects.Clear();
            textComponents.Clear();
            originalMaterials.Clear();
            blurMaterials.Clear();
            
            // 收集所有字符对象（子对象）
            for (int i = 0; i < lineContainer.childCount; i++)
            {
                GameObject charObj = lineContainer.GetChild(i).gameObject;
                TextMeshProUGUI textComp = charObj.GetComponent<TextMeshProUGUI>();
                
                if (textComp != null)
                {
                    characterObjects.Add(charObj);
                    textComponents.Add(textComp);
                    originalMaterials.Add(textComp.fontMaterial);
                    
                    // 创建模糊材质实例
                    Material blurMatInstance = null;
                    if (blurMaterial != null)
                    {
                        blurMatInstance = new Material(blurMaterial);
                        blurMatInstance.SetFloat(BlurAmountProperty, 0);
                    }
                    blurMaterials.Add(blurMatInstance);
                    
                    // 初始时隐藏所有字符
                    charObj.SetActive(false);
                }
            }
            
            // 应用配置（如果有）
            if (config is SequentialBlurConfig blurConfig)
            {
                blurStart = blurConfig.BlurStart;
                blurThreshold = blurConfig.BlurThreshold;
                blurFadeDuration = blurConfig.BlurFadeDuration;
                finalFadeDuration = blurConfig.FinalFadeDuration;
                delayBetweenRounds = blurConfig.DelayBetweenRounds;
                
                if (blurConfig.BlurCurve != null && blurConfig.BlurCurve.keys.Length > 0)
                {
                    blurCurve = blurConfig.BlurCurve;
                }
                
                if (blurConfig.BlurMaterial != null)
                {
                    blurMaterial = blurConfig.BlurMaterial;
                }
            }
            
            // 重置状态
            IsCompleted = false;
            Progress = 0f;
            
            Debug.Log($"[序列模糊效果] 初始化完成，字符数: {characterObjects.Count}");
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 播放效果
        /// </summary>
        public async UniTask Play(CancellationToken cancellationToken = default)
        {
            if (characterObjects.Count == 0)
            {
                Debug.LogWarning("[序列模糊效果] 没有字符对象可播放");
                return;
            }
            
            // 创建效果的取消令牌
            StopEffectInternal();
            effectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            try
            {
                // 完全模拟原始代码的行为
                
                // 第一轮：显示偶数位置字符 (0, 2, 4...)
                Debug.Log("[序列模糊效果] 开始第一轮（偶数位置字符）");
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: effectCts.Token); // 初始延迟
                
                for (int i = 0; i < characterObjects.Count; i += 2)
                {
                    if (effectCts.Token.IsCancellationRequested) break;
                    
                    await ActivateAndFade(i, effectCts.Token);
                    await WaitForBlurBelowThreshold(i, effectCts.Token);
                    
                    // 更新进度 - 第一轮占总进度的40%
                    Progress = (i / 2) / (float)((characterObjects.Count + 1) / 2) * 0.4f;
                }
                
                // 第二轮：显示奇数位置字符 (1, 3, 5...)
                Debug.Log("[序列模糊效果] 开始第二轮（奇数位置字符）");
                for (int i = 1; i < characterObjects.Count; i += 2)
                {
                    if (effectCts.Token.IsCancellationRequested) break;
                    
                    await ActivateAndFade(i, effectCts.Token);
                    await WaitForBlurBelowThreshold(i, effectCts.Token);
                    
                    // 更新进度 - 第二轮占总进度的40%
                    Progress = 0.4f + (i / 2) / (float)(characterObjects.Count / 2) * 0.4f;
                }
                
                // 所有字符显示完后，延迟一段时间
                await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenRounds), cancellationToken: effectCts.Token);
                
                // 整体渐隐
                Debug.Log("[序列模糊效果] 开始整体淡出");
                await FadeOutAllText(effectCts.Token);
                
                IsCompleted = true;
                Progress = 1.0f;
                Debug.Log("[序列模糊效果] 播放完成");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[序列模糊效果] 效果被取消");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[序列模糊效果] 播放异常: {ex}");
            }
        }
        
        /// <summary>
        /// 激活并淡入单个字符
        /// </summary>
        private async UniTask ActivateAndFade(int index, CancellationToken cancellationToken)
        {
            if (index >= characterObjects.Count || textComponents[index] == null)
                return;
            
            Debug.Log($"[序列模糊效果] 激活并淡入字符 {index}");
            
            // 激活字符对象
            characterObjects[index].SetActive(true);
            
            // 应用模糊材质
            if (blurMaterials[index] != null)
            {
                textComponents[index].fontMaterial = blurMaterials[index];
                blurMaterials[index].SetFloat(BlurAmountProperty, blurStart);
            }
            
            float elapsed = 0f;
            
            // 模糊效果动画
            while (elapsed < blurFadeDuration)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                    
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / blurFadeDuration);
                float curveValue = blurCurve.Evaluate(t);
                float blurAmount = Mathf.Lerp(blurStart, 0f, curveValue);
                
                if (blurMaterials[index] != null)
                {
                    blurMaterials[index].SetFloat(BlurAmountProperty, blurAmount);
                }
                
                await UniTask.Yield();
            }
            
            // 确保最终模糊度为0
            if (blurMaterials[index] != null)
            {
                blurMaterials[index].SetFloat(BlurAmountProperty, 0f);
            }
        }
        
        /// <summary>
        /// 等待模糊度低于阈值
        /// </summary>
        private async UniTask WaitForBlurBelowThreshold(int index, CancellationToken cancellationToken)
        {
            if (index >= blurMaterials.Count || blurMaterials[index] == null)
                return;
                
            float currentBlur = blurStart;
            
            while (currentBlur > blurThreshold)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                    
                if (blurMaterials[index] != null)
                {
                    currentBlur = blurMaterials[index].GetFloat(BlurAmountProperty);
                }
                
                await UniTask.Yield();
            }
            
            Debug.Log($"[序列模糊效果] 字符 {index} 模糊度低于阈值");
        }
        
        /// <summary>
        /// 淡出所有文本
        /// </summary>
        private async UniTask FadeOutAllText(CancellationToken cancellationToken)
        {
            float elapsed = 0f;
            Color[] originalColors = new Color[textComponents.Count];
            
            // 保存初始颜色
            for (int i = 0; i < textComponents.Count; i++)
            {
                if (textComponents[i] != null)
                {
                    originalColors[i] = textComponents[i].color;
                }
            }
            
            // 渐变透明度
            while (elapsed < finalFadeDuration)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                    
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / finalFadeDuration);
                
                for (int i = 0; i < textComponents.Count; i++)
                {
                    if (textComponents[i] != null)
                    {
                        Color c = originalColors[i];
                        c.a = alpha;
                        textComponents[i].color = c;
                    }
                }
                
                // 更新进度 - 淡出阶段占总进度的20%
                Progress = 0.8f + (elapsed / finalFadeDuration) * 0.2f;
                
                await UniTask.Yield();
            }
            
            // 最终设置为完全透明
            for (int i = 0; i < textComponents.Count; i++)
            {
                if (textComponents[i] != null)
                {
                    Color c = textComponents[i].color;
                    c.a = 0f;
                    textComponents[i].color = c;
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
            for (int i = 0; i < textComponents.Count; i++)
            {
                if (textComponents[i] != null && i < originalMaterials.Count && originalMaterials[i] != null)
                {
                    textComponents[i].fontMaterial = originalMaterials[i];
                }
            }
            
            // 隐藏所有字符
            foreach (var charObj in characterObjects)
            {
                if (charObj != null)
                {
                    charObj.SetActive(false);
                }
            }
            
            IsCompleted = true;
            Progress = 1.0f;
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 重置效果状态
        /// </summary>
        public async UniTask Reset(CancellationToken cancellationToken = default)
        {
            StopEffectInternal();
            
            // 恢复原始材质
            for (int i = 0; i < textComponents.Count; i++)
            {
                if (textComponents[i] != null && i < originalMaterials.Count && originalMaterials[i] != null)
                {
                    textComponents[i].fontMaterial = originalMaterials[i];
                }
                
                // 隐藏字符
                if (i < characterObjects.Count && characterObjects[i] != null)
                {
                    characterObjects[i].SetActive(false);
                }
            }
            
            IsCompleted = false;
            Progress = 0f;
            
            await UniTask.CompletedTask;
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
            
            // 销毁所有实例化的材质
            foreach (var material in blurMaterials)
            {
                if (material != null)
                {
                    GameObject.Destroy(material);
                }
            }
            
            blurMaterials.Clear();
        }
    }
    
    /// <summary>
    /// 序列模糊效果配置
    /// </summary>
    [Serializable]
    public class SequentialBlurConfig
    {
        public float BlurStart = 30.0f;
        public float BlurThreshold = 10f;
        public float BlurFadeDuration = 1.0f;
        public float FinalFadeDuration = 0.5f;
        public float DelayBetweenRounds = 0.5f;
        public AnimationCurve BlurCurve;
        public Material BlurMaterial;
    }
}
