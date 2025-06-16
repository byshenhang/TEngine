using System.Collections;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System;

namespace GameLogic
{
    /// <summary>
    /// 歌词效果实现类
    /// </summary>
    public static class LyricEffects
    {
        #region 基础效果实现
        
        /// <summary>
        /// 执行淡入淡出效果
        /// </summary>
        /// <param name="character">字符对象</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayFadeEffect(LyricCharacter character, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (character?.TextComponent == null || config?.FadeParams == null)
                return;
            
            var textComponent = character.TextComponent;
            var fadeParams = config.FadeParams;
            var originalColor = textComponent.color;
            
            float elapsed = 0f;
            
            while (elapsed < config.Duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.Duration);
                
                if (config.Curve != null)
                {
                    t = config.Curve.Evaluate(t);
                }
                
                float alpha = Mathf.Lerp(fadeParams.StartAlpha, fadeParams.EndAlpha, t);
                
                Color newColor = originalColor;
                newColor.a = alpha;
                textComponent.color = newColor;
                
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            
            // 确保最终状态
            if (!cancellationToken.IsCancellationRequested)
            {
                Color finalColor = originalColor;
                finalColor.a = fadeParams.EndAlpha;
                textComponent.color = finalColor;
            }
        }
        
        /// <summary>
        /// 执行缩放效果
        /// </summary>
        /// <param name="character">字符对象</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayScaleEffect(LyricCharacter character, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (character?.Transform == null || config?.ScaleParams == null)
                return;
            
            var transform = character.Transform;
            var scaleParams = config.ScaleParams;
            
            float elapsed = 0f;
            
            while (elapsed < config.Duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.Duration);
                
                if (config.Curve != null)
                {
                    t = config.Curve.Evaluate(t);
                }
                
                Vector3 scale = Vector3.Lerp(scaleParams.StartScale, scaleParams.EndScale, t);
                transform.localScale = scale;
                
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            
            // 确保最终状态
            if (!cancellationToken.IsCancellationRequested)
            {
                transform.localScale = scaleParams.EndScale;
            }
        }
        
        /// <summary>
        /// 执行移动效果
        /// </summary>
        /// <param name="character">字符对象</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayMoveEffect(LyricCharacter character, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (character?.Transform == null || config?.MoveParams == null)
                return;
            
            var transform = character.Transform;
            var moveParams = config.MoveParams;
            
            Vector3 originalPosition = transform.localPosition;
            Vector3 startPos = moveParams.UseRelativePosition ? originalPosition + moveParams.StartPosition : moveParams.StartPosition;
            Vector3 endPos = moveParams.UseRelativePosition ? originalPosition + moveParams.EndPosition : moveParams.EndPosition;
            
            float elapsed = 0f;
            
            while (elapsed < config.Duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.Duration);
                
                if (config.Curve != null)
                {
                    t = config.Curve.Evaluate(t);
                }
                
                Vector3 position = Vector3.Lerp(startPos, endPos, t);
                transform.localPosition = position;
                
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            
            // 确保最终状态
            if (!cancellationToken.IsCancellationRequested)
            {
                transform.localPosition = endPos;
            }
        }
        
        /// <summary>
        /// 执行模糊效果（需要BlurFilter组件）
        /// </summary>
        /// <param name="character">字符对象</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayBlurEffect(LyricCharacter character, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (character?.GameObject == null || config?.BlurParams == null)
                return;
            
            // 尝试获取BlurFilter组件
            var blurFilter = character.GameObject.GetComponent<BlurFilter>();
            if (blurFilter == null)
            {
                // 如果没有BlurFilter组件，尝试添加（需要确保有相关依赖）
                Debug.LogWarning($"BlurFilter component not found on {character.GameObject.name}, blur effect will be skipped.");
                return;
            }
            
            var blurParams = config.BlurParams;
            
            float elapsed = 0f;
            
            while (elapsed < config.Duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.Duration);
                
                if (config.Curve != null)
                {
                    t = config.Curve.Evaluate(t);
                }
                
                float blurValue = Mathf.Lerp(blurParams.StartBlur, blurParams.EndBlur, t);
                blurFilter.Blur = blurValue;
                
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            
            // 确保最终状态
            if (!cancellationToken.IsCancellationRequested)
            {
                blurFilter.Blur = blurParams.EndBlur;
            }
        }
        
        /// <summary>
        /// 执行旋转效果
        /// </summary>
        /// <param name="character">字符对象</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayRotateEffect(LyricCharacter character, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (character?.Transform == null || config?.RotateParams == null)
                return;
            
            var transform = character.Transform;
            var rotateParams = config.RotateParams;
            
            Vector3 originalRotation = transform.localEulerAngles;
            Vector3 startRot = rotateParams.UseRelativeRotation ? originalRotation + rotateParams.StartRotation : rotateParams.StartRotation;
            Vector3 endRot = rotateParams.UseRelativeRotation ? originalRotation + rotateParams.EndRotation : rotateParams.EndRotation;
            
            float elapsed = 0f;
            
            while (elapsed < config.Duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.Duration);
                
                if (config.Curve != null)
                {
                    t = config.Curve.Evaluate(t);
                }
                
                Vector3 rotation = Vector3.Lerp(startRot, endRot, t);
                transform.localEulerAngles = rotation;
                
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            
            // 确保最终状态
            if (!cancellationToken.IsCancellationRequested)
            {
                transform.localEulerAngles = endRot;
            }
        }
        
        #endregion
        
        #region 复合效果实现
        
        /// <summary>
        /// 执行模糊淡入效果（复合效果）
        /// </summary>
        /// <param name="character">字符对象</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayBlurFadeEffect(LyricCharacter character, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (character?.GameObject == null)
                return;
            
            // 同时执行模糊和淡入效果
            var blurTask = PlayBlurEffect(character, config, cancellationToken);
            var fadeTask = PlayFadeEffect(character, config, cancellationToken);
            
            await UniTask.WhenAll(blurTask, fadeTask);
        }
        
        /// <summary>
        /// 执行缩放淡入效果（复合效果）
        /// </summary>
        /// <param name="character">字符对象</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayScaleFadeEffect(LyricCharacter character, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (character?.GameObject == null)
                return;
            
            // 同时执行缩放和淡入效果
            var scaleTask = PlayScaleEffect(character, config, cancellationToken);
            var fadeTask = PlayFadeEffect(character, config, cancellationToken);
            
            await UniTask.WhenAll(scaleTask, fadeTask);
        }
        
        /// <summary>
        /// 执行移动淡入效果（复合效果）
        /// </summary>
        /// <param name="character">字符对象</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayMoveFadeEffect(LyricCharacter character, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (character?.GameObject == null)
                return;
            
            // 同时执行移动和淡入效果
            var moveTask = PlayMoveEffect(character, config, cancellationToken);
            var fadeTask = PlayFadeEffect(character, config, cancellationToken);
            
            await UniTask.WhenAll(moveTask, fadeTask);
        }
        
        #endregion
        
        #region 效果调度器
        
        /// <summary>
        /// 根据效果类型执行相应的效果
        /// </summary>
        /// <param name="character">字符对象</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayEffect(LyricCharacter character, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (character == null || config == null)
                return;
            
            try
            {
                switch (config.EffectType)
                {
                    case LyricEffectType.None:
                        break;
                    
                    case LyricEffectType.Fade:
                        await PlayFadeEffect(character, config, cancellationToken);
                        break;
                    
                    case LyricEffectType.Scale:
                        await PlayScaleEffect(character, config, cancellationToken);
                        break;
                    
                    case LyricEffectType.Move:
                        await PlayMoveEffect(character, config, cancellationToken);
                        break;
                    
                    case LyricEffectType.Blur:
                        await PlayBlurEffect(character, config, cancellationToken);
                        break;
                    
                    case LyricEffectType.Rotate:
                        await PlayRotateEffect(character, config, cancellationToken);
                        break;
                    
                    case LyricEffectType.BlurFade:
                        await PlayBlurFadeEffect(character, config, cancellationToken);
                        break;
                    
                    case LyricEffectType.ScaleFade:
                        await PlayScaleFadeEffect(character, config, cancellationToken);
                        break;
                    
                    case LyricEffectType.MoveFade:
                        await PlayMoveFadeEffect(character, config, cancellationToken);
                        break;
                    
                    default:
                        Debug.LogWarning($"Unknown effect type: {config.EffectType}");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // 效果被取消，正常情况
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error playing effect {config.EffectType} on character {character.Character}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 批量执行字符效果（支持延迟）
        /// </summary>
        /// <param name="characters">字符列表</param>
        /// <param name="config">效果配置</param>
        /// <param name="delayBetweenCharacters">字符间延迟</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask PlayBatchEffect(LyricCharacter[] characters, LyricEffectConfig config, float delayBetweenCharacters = 0f, System.Threading.CancellationToken cancellationToken = default)
        {
            if (characters == null || characters.Length == 0 || config == null)
                return;
            
            for (int i = 0; i < characters.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                var character = characters[i];
                if (character != null)
                {
                    // 不等待单个字符效果完成，让它们并行执行
                    _ = PlayEffect(character, config, cancellationToken);
                    
                    // 字符间延迟
                    if (delayBetweenCharacters > 0f && i < characters.Length - 1)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenCharacters), cancellationToken: cancellationToken);
                    }
                }
            }
        }
        
        /// <summary>
        /// 等待所有字符效果完成
        /// </summary>
        /// <param name="characters">字符列表</param>
        /// <param name="config">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask WaitForAllEffectsComplete(LyricCharacter[] characters, LyricEffectConfig config, System.Threading.CancellationToken cancellationToken = default)
        {
            if (characters == null || characters.Length == 0 || config == null)
                return;
            
            var tasks = new UniTask[characters.Length];
            
            for (int i = 0; i < characters.Length; i++)
            {
                var character = characters[i];
                if (character != null)
                {
                    tasks[i] = PlayEffect(character, config, cancellationToken);
                }
                else
                {
                    tasks[i] = UniTask.CompletedTask;
                }
            }
            
            await UniTask.WhenAll(tasks);
        }
        
        #endregion
    }
    
    /// <summary>
    /// BlurFilter组件的简化接口（如果项目中没有相关组件，可以创建一个简单的实现）
    /// </summary>
    public class BlurFilter : MonoBehaviour
    {
        [SerializeField] private float _blur = 0f;
        
        public float Blur
        {
            get => _blur;
            set
            {
                _blur = value;
                ApplyBlur();
            }
        }
        
        private void ApplyBlur()
        {
            // 这里应该实现实际的模糊效果
            // 如果项目中有ChocDino.UIFX.BlurFilter，可以直接使用
            // 否则可以通过Material属性或其他方式实现模糊效果
            
            var textComponent = GetComponent<TextMeshProUGUI>();
            if (textComponent != null && textComponent.material != null)
            {
                // 示例：通过修改材质属性实现模糊效果
                // 具体实现取决于使用的Shader
                if (textComponent.material.HasProperty("_BlurAmount"))
                {
                    textComponent.material.SetFloat("_BlurAmount", _blur);
                }
            }
        }
    }
}