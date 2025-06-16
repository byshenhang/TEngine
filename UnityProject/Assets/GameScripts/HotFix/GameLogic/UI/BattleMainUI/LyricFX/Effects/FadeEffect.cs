using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using LyricFX.Core;

namespace LyricFX.Effects
{
    /// <summary>
    /// 淡入淡出效果
    /// </summary>
    public class FadeEffect : BaseEffect
    {
        private FadeParameters _params;
        private float _currentAlpha;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public FadeEffect(FadeParameters parameters) : base(parameters)
        {
            _params = parameters;
        }
        
        /// <summary>
        /// 执行淡入淡出效果
        /// </summary>
        public override async UniTask ExecuteAsync(
            TextMeshProUGUI target, 
            CharacterContext context,
            CancellationToken token)
        {
            float startTime = Time.time;
            float elapsedTime = 0;
            
            _currentAlpha = _params.StartAlpha;
            
            // 设置初始透明度
            Color color = target.color;
            color.a = _currentAlpha;
            target.color = color;
            
            // 日志记录初始透明度
            LyricLogger.Log($"淡入淡出效果开始 - 字符:{target.text}, 初始透明度:{_currentAlpha:F2}, 目标透明度:{_params.EndAlpha:F2}, 持续:{_params.Duration:F2}秒");
            
            while (elapsedTime < _params.Duration)
            {
                if (token.IsCancellationRequested) break;
                
                elapsedTime = Time.time - startTime;
                float progress = Mathf.Clamp01(elapsedTime / _params.Duration);
                float curveValue = _params.Curve.Evaluate(progress);
                
                // 计算当前透明度
                _currentAlpha = Mathf.Lerp(_params.StartAlpha, _params.EndAlpha, curveValue);
                
                // 应用到文本
                color = target.color;
                color.a = _currentAlpha;
                target.color = color;
                
                // 每0.2秒记录一次日志(减少日志量)
                if (elapsedTime % 0.2f < 0.02f)
                {
                    LyricLogger.Log($"淡入淡出进度 - 字符:{target.text}, 透明度:{_currentAlpha:F2}, 进度:{progress:P2}");
                }
                
                // 报告进度
                ReportProgress(progress);
                context.NormalizedProgress = progress;
                
                await UniTask.Yield();
            }
            
            // 设置最终状态
            color = target.color;
            color.a = _params.EndAlpha;
            target.color = color;
            
            _currentAlpha = _params.EndAlpha;
            ReportProgress(1.0f);
            context.NormalizedProgress = 1.0f;
            
            // 记录效果完成日志
            LyricLogger.Log($"淡入淡出效果完成 - 字符:{target.text}, 最终透明度:{_currentAlpha:F2}, 耗时:{elapsedTime:F2}秒");
            
            // 稍后再检查透明度是否真的生效
            await UniTask.Delay(50);
            LyricLogger.Log($"效果完成后再次检查 - 字符:{target.text}, 当前透明度:{target.color.a:F2}");
        }
        
        /// <summary>
        /// 创建反向效果
        /// </summary>
        public override BaseEffect CreateReversed()
        {
            var reversedParams = new FadeParameters
            {
                Duration = _params.Duration,
                Curve = _params.Curve,
                StartAlpha = _params.EndAlpha,
                EndAlpha = _params.StartAlpha
            };
            
            return new FadeEffect(reversedParams);
        }
        
        /// <summary>
        /// 获取当前透明度值
        /// </summary>
        public override float GetCurrentValue()
        {
            return _currentAlpha;
        }
    }
}
