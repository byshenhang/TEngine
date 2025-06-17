using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using System.Threading;
using TMPro;
using UnityEngine;

namespace LyricFX.Implementations.Effect
{
    /// <summary>
    /// 随机变换颜色效果 - 随机变换颜色然后渐变消失
    /// </summary>
    public class RandomColorFadeEffect : ILyricEffect
    {
        public string EffectId => "random_color_fade";
        public bool IsCompleted { get; private set; }
        public float Progress => effectProgress;
        
        private TextMeshProUGUI textComponent;
        private Color originalColor;
        private float effectProgress;
        
        // 效果配置
        private float colorChangeDuration = 1.0f;  // 颜色变换持续时间
        private float holdDuration = 1.0f;         // 保持时间
        private float fadeOutDuration = 1.0f;      // 淡出时间
        private int colorChangeCount = 5;          // 颜色变换次数
        
        // 预定义的颜色数组
        private Color[] randomColors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.magenta,
            Color.cyan,
            new Color(1f, 0.5f, 0f),    // 橙色
            new Color(0.5f, 0f, 1f),    // 紫色
            new Color(1f, 0.75f, 0.8f), // 粉色
            new Color(0f, 1f, 0.5f)     // 春绿色
        };
        
        public RandomColorFadeEffect()
        {
            
        }
        
        public RandomColorFadeEffect(float colorChangeDuration, float holdDuration, float fadeOutDuration, int colorChangeCount = 5)
        {
            this.colorChangeDuration = colorChangeDuration;
            this.holdDuration = holdDuration;
            this.fadeOutDuration = fadeOutDuration;
            this.colorChangeCount = colorChangeCount;
        }
        
        public async UniTask Initialize(GameObject target, object config, CancellationToken cancellationToken = default)
        {
            textComponent = target.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                originalColor = textComponent.color;
                effectProgress = 0f;
                IsCompleted = false;
                
                // 初始设置为透明
                var color = originalColor;
                color.a = 0f;
                textComponent.color = color;
            }
            
            await UniTask.CompletedTask;
        }
        
        public void Initialize(GameObject characterObject)
        {
            Initialize(characterObject, null, CancellationToken.None).Forget();
        }
        
        public async UniTask Play(CancellationToken cancellationToken = default)
        {
            if (textComponent == null) return;
            
            try
            {
                // 阶段1：随机颜色变换
                await RandomColorChange(cancellationToken);
                
                // 阶段2：保持最后颜色
                await Hold(cancellationToken);
                
                // 阶段3：淡出
                await FadeOut(cancellationToken);
                
                IsCompleted = true;
            }
            catch (System.OperationCanceledException)
            {
                // 操作被取消，直接返回
            }
        }
        
        public async UniTask Stop(CancellationToken cancellationToken = default)
        {
            if (textComponent != null)
            {
                textComponent.color = originalColor;
                effectProgress = 1f;
                IsCompleted = true;
            }
            
            await UniTask.CompletedTask;
        }
        
        public void Stop()
        {
            Stop(CancellationToken.None).Forget();
        }
        
        public async UniTask Reset(CancellationToken cancellationToken = default)
        {
            if (textComponent != null)
            {
                var color = originalColor;
                color.a = 0f;
                textComponent.color = color;
                effectProgress = 0f;
                IsCompleted = false;
            }
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 随机颜色变换阶段
        /// </summary>
        private async UniTask RandomColorChange(CancellationToken cancellationToken)
        {
            float elapsed = 0f;
            float intervalTime = colorChangeDuration / colorChangeCount;
            
            // 先淡入到第一个随机颜色
            Color targetColor = GetRandomColor();
            targetColor.a = 1f;
            
            // 淡入效果
            float fadeInTime = intervalTime * 0.5f;
            while (elapsed < fadeInTime && !cancellationToken.IsCancellationRequested)
            {
                float progress = elapsed / fadeInTime;
                Color currentColor = Color.Lerp(new Color(targetColor.r, targetColor.g, targetColor.b, 0f), targetColor, progress);
                textComponent.color = currentColor;
                
                elapsed += Time.deltaTime;
                effectProgress = elapsed / (colorChangeDuration + holdDuration + fadeOutDuration) * 0.33f;
                
                await UniTask.Yield();
            }
            
            // 随机颜色变换
            for (int i = 1; i < colorChangeCount && !cancellationToken.IsCancellationRequested; i++)
            {
                Color newColor = GetRandomColor();
                newColor.a = 1f;
                
                float startTime = elapsed;
                while (elapsed < startTime + intervalTime && !cancellationToken.IsCancellationRequested)
                {
                    float progress = (elapsed - startTime) / intervalTime;
                    Color currentColor = Color.Lerp(targetColor, newColor, progress);
                    textComponent.color = currentColor;
                    
                    elapsed += Time.deltaTime;
                    effectProgress = elapsed / (colorChangeDuration + holdDuration + fadeOutDuration) * 0.33f;
                    
                    await UniTask.Yield();
                }
                
                targetColor = newColor;
            }
        }
        
        /// <summary>
        /// 保持阶段
        /// </summary>
        private async UniTask Hold(CancellationToken cancellationToken)
        {
            float elapsed = 0f;
            
            while (elapsed < holdDuration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                effectProgress = 0.33f + (elapsed / holdDuration) * 0.33f;
                
                await UniTask.Yield();
            }
        }
        
        /// <summary>
        /// 淡出阶段
        /// </summary>
        private async UniTask FadeOut(CancellationToken cancellationToken)
        {
            float elapsed = 0f;
            Color startColor = textComponent.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            
            while (elapsed < fadeOutDuration && !cancellationToken.IsCancellationRequested)
            {
                float progress = elapsed / fadeOutDuration;
                Color currentColor = Color.Lerp(startColor, endColor, progress);
                textComponent.color = currentColor;
                
                elapsed += Time.deltaTime;
                effectProgress = 0.66f + (elapsed / fadeOutDuration) * 0.34f;
                
                await UniTask.Yield();
            }
            
            // 确保完全透明
            if (!cancellationToken.IsCancellationRequested)
            {
                textComponent.color = endColor;
                effectProgress = 1f;
            }
        }
        
        /// <summary>
        /// 获取随机颜色
        /// </summary>
        private Color GetRandomColor()
        {
            int randomIndex = Random.Range(0, randomColors.Length);
            return randomColors[randomIndex];
        }
    }
}