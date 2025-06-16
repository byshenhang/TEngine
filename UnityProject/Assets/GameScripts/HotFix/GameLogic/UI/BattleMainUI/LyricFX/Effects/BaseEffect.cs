using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using LyricFX.Core;

namespace LyricFX.Effects
{
    /// <summary>
    /// 效果基类，所有具体效果都继承自此类
    /// </summary>
    public abstract class BaseEffect
    {
        /// <summary>
        /// 效果参数
        /// </summary>
        protected EffectParameters Parameters { get; private set; }
        
        /// <summary>
        /// 进度变化事件
        /// </summary>
        public event Action<float> OnProgressChanged;
        
        /// <summary>
        /// 效果完成事件
        /// </summary>
        public event Action OnEffectCompleted;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parameters">效果参数</param>
        public BaseEffect(EffectParameters parameters)
        {
            Parameters = parameters;
        }
        
        /// <summary>
        /// 执行效果
        /// </summary>
        /// <param name="target">目标TextMeshPro组件</param>
        /// <param name="context">字符上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        public abstract UniTask ExecuteAsync(
            TextMeshProUGUI target, 
            CharacterContext context,
            CancellationToken cancellationToken);
        
        /// <summary>
        /// 创建相反效果(用于Exit<->Enter)
        /// </summary>
        public abstract BaseEffect CreateReversed();
        
        /// <summary>
        /// 获取当前效果值
        /// </summary>
        public abstract float GetCurrentValue();
        
        /// <summary>
        /// 报告进度
        /// </summary>
        /// <param name="progress">进度值 (0-1)</param>
        protected void ReportProgress(float progress)
        {
            OnProgressChanged?.Invoke(progress);
            
            if (Mathf.Approximately(progress, 1.0f))
                OnEffectCompleted?.Invoke();
        }
    }
}
