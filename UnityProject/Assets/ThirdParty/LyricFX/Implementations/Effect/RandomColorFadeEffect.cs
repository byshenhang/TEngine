using Cysharp.Threading.Tasks;
using LyricFX.Core.Attributes;
using LyricFX.Core.Interfaces;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

namespace LyricFX.Implementations.Effect
{
    /// <summary>
    /// 随机变换颜色效果 - 随机变换颜色然后渐变消失
    /// </summary>
    [EffectConfig(typeof(RandomColorFadeEffect))]
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
        
        // 配色主题枚举
        public enum ColorTheme
        {
            Warm,      // 暖色调
            Cool,      // 冷色调
            Sunset,    // 日落色调
            Ocean,     // 海洋色调
            Forest,    // 森林色调
            Neon,      // 霓虹色调
            Pastel,    // 柔和色调
            Autumn     // 秋季色调
        }
        
        private ColorTheme currentTheme = ColorTheme.Warm;
        
        // 和谐配色方案 - 每个主题包含协调的颜色组合
        private readonly Dictionary<ColorTheme, Color[]> colorSchemes = new Dictionary<ColorTheme, Color[]>
        {
            [ColorTheme.Warm] = new Color[]
            {
                new Color(1f, 0.6f, 0.2f),      // 温暖橙色
                new Color(1f, 0.4f, 0.3f),      // 珊瑚红
                new Color(1f, 0.8f, 0.4f),      // 金黄色
                new Color(0.9f, 0.5f, 0.4f),    // 赤陶色
                new Color(1f, 0.7f, 0.5f)       // 桃色
            },
            
            [ColorTheme.Cool] = new Color[]
            {
                new Color(0.3f, 0.7f, 1f),      // 天蓝色
                new Color(0.4f, 0.6f, 0.9f),    // 蓝紫色
                new Color(0.2f, 0.8f, 0.8f),    // 青色
                new Color(0.5f, 0.7f, 1f),      // 浅蓝色
                new Color(0.3f, 0.5f, 0.8f)     // 深蓝色
            },
            
            [ColorTheme.Sunset] = new Color[]
            {
                new Color(1f, 0.5f, 0.2f),      // 日落橙
                new Color(1f, 0.3f, 0.4f),      // 日落红
                new Color(1f, 0.7f, 0.3f),      // 日落黄
                new Color(0.9f, 0.4f, 0.6f),    // 日落粉
                new Color(0.8f, 0.3f, 0.5f)     // 日落紫
            },
            
            [ColorTheme.Ocean] = new Color[]
            {
                new Color(0.1f, 0.6f, 0.8f),    // 深海蓝
                new Color(0.3f, 0.8f, 0.9f),    // 海水蓝
                new Color(0.2f, 0.7f, 0.7f),    // 海绿色
                new Color(0.4f, 0.9f, 1f),      // 浅海蓝
                new Color(0.1f, 0.5f, 0.6f)     // 深青色
            },
            
            [ColorTheme.Forest] = new Color[]
            {
                new Color(0.3f, 0.7f, 0.3f),    // 森林绿
                new Color(0.5f, 0.8f, 0.4f),    // 嫩绿色
                new Color(0.2f, 0.6f, 0.2f),    // 深绿色
                new Color(0.6f, 0.9f, 0.5f),    // 浅绿色
                new Color(0.4f, 0.6f, 0.3f)     // 橄榄绿
            },
            
            [ColorTheme.Neon] = new Color[]
            {
                new Color(1f, 0.2f, 0.8f),      // 霓虹粉
                new Color(0.2f, 1f, 0.8f),      // 霓虹青
                new Color(0.8f, 0.2f, 1f),      // 霓虹紫
                new Color(1f, 0.8f, 0.2f),      // 霓虹黄
                new Color(0.2f, 0.8f, 1f)       // 霓虹蓝
            },
            
            [ColorTheme.Pastel] = new Color[]
            {
                new Color(1f, 0.8f, 0.9f),      // 柔和粉
                new Color(0.8f, 0.9f, 1f),      // 柔和蓝
                new Color(0.9f, 1f, 0.8f),      // 柔和绿
                new Color(1f, 0.9f, 0.8f),      // 柔和橙
                new Color(0.9f, 0.8f, 1f)       // 柔和紫
            },
            
            [ColorTheme.Autumn] = new Color[]
            {
                new Color(0.8f, 0.4f, 0.2f),    // 秋叶橙
                new Color(0.7f, 0.3f, 0.1f),    // 秋叶红
                new Color(0.9f, 0.7f, 0.3f),    // 秋叶黄
                new Color(0.6f, 0.3f, 0.2f),    // 秋叶棕
                new Color(0.8f, 0.5f, 0.3f)     // 秋叶金
            }
        };
        
        public RandomColorFadeEffect()
        {
            // 随机选择一个主题
            var themes = System.Enum.GetValues(typeof(ColorTheme));
            currentTheme = (ColorTheme)themes.GetValue(Random.Range(0, themes.Length));
        }
        
        public RandomColorFadeEffect(float colorChangeDuration, float holdDuration, float fadeOutDuration, int colorChangeCount = 5, ColorTheme theme = ColorTheme.Warm)
        {
            this.colorChangeDuration = colorChangeDuration;
            this.holdDuration = holdDuration;
            this.fadeOutDuration = fadeOutDuration;
            this.colorChangeCount = colorChangeCount;
            this.currentTheme = theme;
        }
        
        /// <summary>
        /// 设置配色主题
        /// </summary>
        public void SetColorTheme(ColorTheme theme)
        {
            currentTheme = theme;
        }
        
        /// <summary>
        /// 随机选择一个配色主题
        /// </summary>
        public void SetRandomTheme()
        {
            var themes = System.Enum.GetValues(typeof(ColorTheme));
            currentTheme = (ColorTheme)themes.GetValue(Random.Range(0, themes.Length));
        }
        
        public async UniTask Initialize(GameObject target, IEffectConfig config, CancellationToken cancellationToken = default)
        {
            textComponent = target.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                originalColor = textComponent.color;
                effectProgress = 0f;
                IsCompleted = false;
                
                // 应用配置
                if (config is RandomColorConfig colorConfig)
                {
                    colorChangeDuration = colorConfig.colorChangeDuration;
                    holdDuration = colorConfig.holdDuration;
                    fadeOutDuration = colorConfig.fadeOutDuration;
                    colorChangeCount = colorConfig.colorChangeCount;
                    
                    if (colorConfig.useRandomTheme)
                    {
                        SetRandomTheme();
                    }
                    else
                    {
                        SetColorTheme(colorConfig.colorTheme);
                    }
                }
                
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
        /// 获取当前主题的随机颜色
        /// </summary>
        private Color GetRandomColor()
        {
            if (colorSchemes.ContainsKey(currentTheme))
            {
                Color[] themeColors = colorSchemes[currentTheme];
                int randomIndex = Random.Range(0, themeColors.Length);
                return themeColors[randomIndex];
            }
            
            // 如果主题不存在，返回默认暖色调的第一个颜色
            return colorSchemes[ColorTheme.Warm][0];
        }
        
        /// <summary>
        /// 获取当前主题的所有颜色
        /// </summary>
        public Color[] GetCurrentThemeColors()
        {
            return colorSchemes.ContainsKey(currentTheme) ? colorSchemes[currentTheme] : colorSchemes[ColorTheme.Warm];
        }
        
        /// <summary>
        /// 获取指定主题的颜色
        /// </summary>
        public Color[] GetThemeColors(ColorTheme theme)
        {
            return colorSchemes.ContainsKey(theme) ? colorSchemes[theme] : colorSchemes[ColorTheme.Warm];
        }
    }


    /// <summary>
    /// 随机颜色渐变效果配置
    /// </summary>
    [System.Serializable]
    public class RandomColorConfig : IEffectConfig, IAdjustConfig
    {
        [UnityEngine.SerializeField]
        public RandomColorFadeEffect.ColorTheme colorTheme = RandomColorFadeEffect.ColorTheme.Warm;
        
        [UnityEngine.SerializeField]
        public float colorChangeDuration = 1.0f;
        
        [UnityEngine.SerializeField]
        public float holdDuration = 1.0f;
        
        [UnityEngine.SerializeField]
        public float fadeOutDuration = 1.0f;
        
        [UnityEngine.SerializeField]
        public int colorChangeCount = 5;
        
        [UnityEngine.SerializeField]
        public bool useRandomTheme = false;  // 是否使用随机主题
        
        public void AdjustDuration(float availableDuration, int characterCount)
        {
            // 根据可用时间调整各阶段持续时间
            float totalDuration = colorChangeDuration + holdDuration + fadeOutDuration;
            if (totalDuration > availableDuration && availableDuration > 0)
            {
                float ratio = availableDuration / totalDuration;
                colorChangeDuration *= ratio;
                holdDuration *= ratio;
                fadeOutDuration *= ratio;
            }
        }

        public float GetTotalDuration(int characterCount)
        {
            return colorChangeDuration + holdDuration + fadeOutDuration;
        }
    }
}