using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using System.Threading;

namespace LyricFX.Core.Interfaces
{
    /// <summary>
    /// 歌词效果接口 - 定义所有效果必须实现的功能
    /// </summary>
    public interface ILyricEffect
    {
        /// <summary>
        /// 效果是否已完成
        /// </summary>
        bool IsCompleted { get; }
        
        /// <summary>
        /// 当前效果进度 (0-1)
        /// </summary>
        float Progress { get; }
        
        /// <summary>
        /// 效果唯一标识符
        /// </summary>
        string EffectId { get; }
        
        /// <summary>
        /// 初始化效果
        /// </summary>
        /// <param name="target">目标游戏对象</param>
        /// <param name="config">效果配置</param>
        UniTask Initialize(GameObject target, IEffectConfig config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 播放效果
        /// </summary>
        UniTask Play(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 停止效果
        /// </summary>
        UniTask Stop(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 重置效果状态
        /// </summary>
        UniTask Reset(CancellationToken cancellationToken = default);
    }
}
