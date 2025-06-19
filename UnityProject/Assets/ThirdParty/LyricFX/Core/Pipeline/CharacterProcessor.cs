using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace LyricFX.Core.Pipeline
{
    /// <summary>
    /// 字符处理器接口 - 管道中的处理单元
    /// </summary>
    public interface ICharacterProcessor
    {
        /// <summary>
        /// 处理优先级 - 数值越低越先执行
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 处理器唯一ID
        /// </summary>
        string ProcessorId { get; }

        /// <summary>
        /// 处理字符上下文
        /// </summary>
        /// <param name="context">处理上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        UniTask<ProcessingContext> Process(ProcessingContext context, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 基础字符处理器实现
    /// </summary>
    public abstract class CharacterProcessor : ICharacterProcessor
    {
        public abstract int Priority { get; }
        public abstract string ProcessorId { get; }

        public virtual async UniTask<ProcessingContext> Process(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            if (context.IsCompleted || cancellationToken.IsCancellationRequested)
                return context;

            try
            {
                return await OnProcess(context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 正常取消，直接返回
                return context;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[{ProcessorId}] 处理异常: {ex}");
                return context;
            }
        }

        protected abstract UniTask<ProcessingContext> OnProcess(ProcessingContext context, CancellationToken cancellationToken);
    }
}
