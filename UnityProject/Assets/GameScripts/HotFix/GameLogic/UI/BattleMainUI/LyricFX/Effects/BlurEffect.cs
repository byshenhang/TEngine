using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using LyricFX.Core;
using ChocDino.UIFX;  // 引用原示例代码中使用的BlurFilter所在命名空间

namespace LyricFX.Effects
{
    /// <summary>
    /// 模糊效果实现
    /// </summary>
    public class BlurEffect : BaseEffect
    {
        private BlurParameters _params;
        private float _currentBlur;
        private BlurFilter _blurFilter;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public BlurEffect(BlurParameters parameters) : base(parameters)
        {
            _params = parameters;
        }
        
        /// <summary>
        /// 执行模糊效果
        /// </summary>
        public override async UniTask ExecuteAsync(
            TextMeshProUGUI target,
            CharacterContext context,
            CancellationToken token)
        {
            // 获取或创建模糊滤镜组件
            _blurFilter = context.GetOrCreateComponent<BlurFilter>();
            
            if (_blurFilter == null)
            {
                Debug.LogError("BlurFilter component not found and could not be created");
                return;
            }
            
            float startTime = Time.time;
            float elapsedTime = 0;
            
            _currentBlur = _params.StartBlur;
            _blurFilter.Blur = _currentBlur;
            
            while (elapsedTime < _params.Duration)
            {
                if (token.IsCancellationRequested) break;
                
                elapsedTime = Time.time - startTime;
                float progress = Mathf.Clamp01(elapsedTime / _params.Duration);
                float curveValue = _params.Curve.Evaluate(progress);
                
                // 计算当前模糊度
                _currentBlur = Mathf.Lerp(_params.StartBlur, _params.EndBlur, curveValue);
                
                // 应用到滤镜
                _blurFilter.Blur = _currentBlur;
                
                // 报告进度
                ReportProgress(progress);
                context.NormalizedProgress = progress;
                
                await UniTask.Yield();
            }
            
            // 设置最终状态
            _blurFilter.Blur = _params.EndBlur;
            _currentBlur = _params.EndBlur;
            
            ReportProgress(1.0f);
            context.NormalizedProgress = 1.0f;
        }
        
        /// <summary>
        /// 创建反向效果
        /// </summary>
        public override BaseEffect CreateReversed()
        {
            var reversedParams = new BlurParameters
            {
                Duration = _params.Duration,
                Curve = _params.Curve,
                StartBlur = _params.EndBlur,
                EndBlur = _params.StartBlur,
                BlurThreshold = _params.BlurThreshold
            };
            
            return new BlurEffect(reversedParams);
        }
        
        /// <summary>
        /// 获取当前模糊值
        /// </summary>
        public override float GetCurrentValue()
        {
            return _currentBlur;
        }
    }
}
