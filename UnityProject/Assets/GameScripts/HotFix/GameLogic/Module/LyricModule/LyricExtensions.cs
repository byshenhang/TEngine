using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 歌词模块扩展方法
    /// </summary>
    public static class LyricExtensions
    {
        #region 便捷播放方法
        
        /// <summary>
        /// 快速播放歌词文件
        /// </summary>
        /// <param name="module">歌词模块</param>
        /// <param name="lrcPath">LRC文件路径</param>
        /// <param name="fontSize">字体大小</param>
        /// <param name="color">字体颜色</param>
        public static async UniTask QuickPlay(this LyricModule module, string lrcPath, float fontSize = 48f, Color? color = null)
        {
            var config = LyricConfig.Default;
            config.FontSize = fontSize;
            if (color.HasValue)
            {
                config.DefaultColor = color.Value;
            }
            
            await module.LoadAndPlayLyric(lrcPath, config);
        }
        
        /// <summary>
        /// 播放简单文本歌词
        /// </summary>
        /// <param name="module">歌词模块</param>
        /// <param name="text">歌词文本</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="config">配置</param>
        public static async UniTask PlaySimpleText(this LyricModule module, string text, float startTime = 0f, LyricConfig config = null)
        {
            var lyricData = new LyricData();
            lyricData.Lines.Add(new LyricLineData
            {
                Time = startTime,
                Text = text
            });
            
            await module.PlayLyric(lyricData, config);
        }
        
        /// <summary>
        /// 播放多行文本歌词
        /// </summary>
        /// <param name="module">歌词模块</param>
        /// <param name="lines">歌词行列表（时间，文本）</param>
        /// <param name="config">配置</param>
        public static async UniTask PlayMultipleLines(this LyricModule module, List<(float time, string text)> lines, LyricConfig config = null)
        {
            var lyricData = new LyricData();
            
            foreach (var (time, text) in lines)
            {
                lyricData.Lines.Add(new LyricLineData
                {
                    Time = time,
                    Text = text
                });
            }
            
            await module.PlayLyric(lyricData, config);
        }
        
        #endregion
        
        #region 配置创建辅助方法
        
        /// <summary>
        /// 创建模糊淡入效果配置
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="startBlur">起始模糊值</param>
        /// <param name="endBlur">结束模糊值</param>
        /// <param name="curve">动画曲线</param>
        /// <returns>效果配置</returns>
        public static LyricEffectConfig CreateBlurFadeConfig(float duration = 1f, float startBlur = 30f, float endBlur = 0f, AnimationCurve curve = null)
        {
            return new LyricEffectConfig
            {
                EffectType = LyricEffectType.BlurFade,
                Duration = duration,
                BlurParams = new BlurEffectParams { StartBlur = startBlur, EndBlur = endBlur },
                FadeParams = new FadeEffectParams { StartAlpha = 0f, EndAlpha = 1f },
                Curve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1)
            };
        }
        
        /// <summary>
        /// 创建缩放淡入效果配置
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="startScale">起始缩放</param>
        /// <param name="endScale">结束缩放</param>
        /// <param name="curve">动画曲线</param>
        /// <returns>效果配置</returns>
        public static LyricEffectConfig CreateScaleFadeConfig(float duration = 1f, Vector3? startScale = null, Vector3? endScale = null, AnimationCurve curve = null)
        {
            return new LyricEffectConfig
            {
                EffectType = LyricEffectType.ScaleFade,
                Duration = duration,
                ScaleParams = new ScaleEffectParams 
                { 
                    StartScale = startScale ?? Vector3.zero, 
                    EndScale = endScale ?? Vector3.one 
                },
                FadeParams = new FadeEffectParams { StartAlpha = 0f, EndAlpha = 1f },
                Curve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1)
            };
        }
        
        /// <summary>
        /// 创建移动淡入效果配置
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="startOffset">起始偏移</param>
        /// <param name="endOffset">结束偏移</param>
        /// <param name="useRelative">是否使用相对位置</param>
        /// <param name="curve">动画曲线</param>
        /// <returns>效果配置</returns>
        public static LyricEffectConfig CreateMoveFadeConfig(float duration = 1f, Vector3? startOffset = null, Vector3? endOffset = null, bool useRelative = true, AnimationCurve curve = null)
        {
            return new LyricEffectConfig
            {
                EffectType = LyricEffectType.MoveFade,
                Duration = duration,
                MoveParams = new MoveEffectParams 
                { 
                    StartPosition = startOffset ?? new Vector3(0, 100, 0), 
                    EndPosition = endOffset ?? Vector3.zero,
                    UseRelativePosition = useRelative
                },
                FadeParams = new FadeEffectParams { StartAlpha = 0f, EndAlpha = 1f },
                Curve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1)
            };
        }
        
        /// <summary>
        /// 创建淡出效果配置
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="curve">动画曲线</param>
        /// <returns>效果配置</returns>
        public static LyricEffectConfig CreateFadeOutConfig(float duration = 0.5f, AnimationCurve curve = null)
        {
            return new LyricEffectConfig
            {
                EffectType = LyricEffectType.Fade,
                Duration = duration,
                FadeParams = new FadeEffectParams { StartAlpha = 1f, EndAlpha = 0f },
                Curve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1)
            };
        }
        
        #endregion
        
        #region 预设配置
        
        /// <summary>
        /// 获取经典模糊效果配置（基于原始代码）
        /// </summary>
        /// <returns>歌词配置</returns>
        public static LyricConfig GetClassicBlurConfig()
        {
            var config = LyricConfig.Default;
            
            // 进入效果：模糊淡入
            config.EnterEffect = CreateBlurFadeConfig(
                duration: 1f,
                startBlur: 30f,
                endBlur: 0f,
                curve: AnimationCurve.EaseInOut(0, 0, 1, 1)
            );
            
            // 字符效果：模糊淡入
            config.CharacterEffect = CreateBlurFadeConfig(
                duration: 1f,
                startBlur: 30f,
                endBlur: 0f
            );
            
            // 离开效果：淡出
            config.ExitEffect = CreateFadeOutConfig(0.5f);
            
            return config;
        }
        
        /// <summary>
        /// 获取弹性缩放效果配置
        /// </summary>
        /// <returns>歌词配置</returns>
        public static LyricConfig GetBouncyScaleConfig()
        {
            var config = LyricConfig.Default;
            
            // 创建弹性曲线
            var bouncyCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.6f, 1.2f),
                new Keyframe(1f, 1f)
            );
            
            config.EnterEffect = CreateScaleFadeConfig(
                duration: 0.8f,
                startScale: Vector3.zero,
                endScale: Vector3.one,
                curve: bouncyCurve
            );
            
            config.CharacterEffect = CreateScaleFadeConfig(
                duration: 0.6f,
                startScale: Vector3.zero,
                endScale: Vector3.one,
                curve: bouncyCurve
            );
            
            config.ExitEffect = CreateScaleFadeConfig(
                duration: 0.4f,
                startScale: Vector3.one,
                endScale: Vector3.zero
            );
            
            return config;
        }
        
        /// <summary>
        /// 获取从上方飞入效果配置
        /// </summary>
        /// <returns>歌词配置</returns>
        public static LyricConfig GetFlyInFromTopConfig()
        {
            var config = LyricConfig.Default;
            
            config.EnterEffect = CreateMoveFadeConfig(
                duration: 1.2f,
                startOffset: new Vector3(0, 200, 0),
                endOffset: Vector3.zero,
                useRelative: true
            );
            
            config.CharacterEffect = CreateMoveFadeConfig(
                duration: 0.8f,
                startOffset: new Vector3(0, 100, 0),
                endOffset: Vector3.zero,
                useRelative: true
            );
            
            config.ExitEffect = CreateMoveFadeConfig(
                duration: 0.6f,
                startOffset: Vector3.zero,
                endOffset: new Vector3(0, -100, 0),
                useRelative: true
            );
            
            return config;
        }
        
        /// <summary>
        /// 获取打字机效果配置
        /// </summary>
        /// <returns>歌词配置</returns>
        public static LyricConfig GetTypewriterConfig()
        {
            var config = LyricConfig.Default;
            
            // 打字机效果：快速淡入
            config.CharacterEffect = new LyricEffectConfig
            {
                EffectType = LyricEffectType.Fade,
                Duration = 1f,
                FadeParams = new FadeEffectParams { StartAlpha = 0f, EndAlpha = 1f },
                Curve = AnimationCurve.Linear(0, 0, 1, 1)
            };
            
            // 行效果：无
            config.EnterEffect = new LyricEffectConfig
            {
                EffectType = LyricEffectType.None
            };
            
            config.ExitEffect = CreateFadeOutConfig(0.3f);
            
            return config;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 歌词工具类
    /// </summary>
    public static class LyricUtils
    {
        /// <summary>
        /// 解析时间字符串为秒数
        /// </summary>
        /// <param name="timeString">时间字符串 (mm:ss.ff)</param>
        /// <returns>秒数</returns>
        public static float ParseTimeString(string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return 0f;
            
            try
            {
                var parts = timeString.Split(':');
                if (parts.Length != 2)
                    return 0f;
                
                int minutes = int.Parse(parts[0]);
                var secondParts = parts[1].Split('.');
                int seconds = int.Parse(secondParts[0]);
                int centiseconds = secondParts.Length > 1 ? int.Parse(secondParts[1]) : 0;
                
                return minutes * 60f + seconds + centiseconds * 0.01f;
            }
            catch
            {
                return 0f;
            }
        }
        
        /// <summary>
        /// 将秒数转换为时间字符串
        /// </summary>
        /// <param name="seconds">秒数</param>
        /// <returns>时间字符串 (mm:ss.ff)</returns>
        public static string FormatTimeString(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int centiseconds = Mathf.FloorToInt((seconds % 1f) * 100f);
            
            return $"{minutes:D2}:{secs:D2}.{centiseconds:D2}";
        }
        
        /// <summary>
        /// 验证LRC文件格式
        /// </summary>
        /// <param name="content">文件内容</param>
        /// <returns>是否为有效的LRC格式</returns>
        public static bool ValidateLrcFormat(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;
            
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;
                
                // 检查是否包含时间标签
                if (trimmedLine.StartsWith("[") && trimmedLine.Contains("]"))
                {
                    return true; // 找到至少一个时间标签
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 清理歌词文本
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>清理后的文本</returns>
        public static string CleanLyricText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            
            // 移除多余的空白字符
            text = text.Trim();
            
            // 移除HTML标签（如果有）
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "");
            
            return text;
        }
        
        /// <summary>
        /// 计算歌词行的显示持续时间
        /// </summary>
        /// <param name="currentLine">当前行</param>
        /// <param name="nextLine">下一行</param>
        /// <param name="defaultDuration">默认持续时间</param>
        /// <returns>持续时间</returns>
        public static float CalculateLineDuration(LyricLineData currentLine, LyricLineData nextLine, float defaultDuration = 3f)
        {
            if (currentLine.Duration > 0f)
                return currentLine.Duration;
            
            if (nextLine != null)
                return nextLine.Time - currentLine.Time;
            
            return defaultDuration;
        }
    }
}