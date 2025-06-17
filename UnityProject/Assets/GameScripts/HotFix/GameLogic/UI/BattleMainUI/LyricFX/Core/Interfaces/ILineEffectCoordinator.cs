using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace LyricFX.Core.Interfaces
{
    /// <summary>
    /// 行级效果协调器接口
    /// 负责协调整行歌词的效果播放，实现行级统一控制
    /// </summary>
    public interface ILineEffectCoordinator
    {
        /// <summary>
        /// 初始化协调器
        /// </summary>
        /// <param name="lineContainer">行容器对象</param>
        /// <param name="config">配置对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        UniTask Initialize(GameObject lineContainer, object config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 播放效果
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        UniTask Play(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 停止效果
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        UniTask Stop(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 重置效果
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        UniTask Reset(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 当前进度 (0.0 - 1.0)
        /// </summary>
        float Progress { get; }
        
        /// <summary>
        /// 是否已完成
        /// </summary>
        bool IsCompleted { get; }
    }
}