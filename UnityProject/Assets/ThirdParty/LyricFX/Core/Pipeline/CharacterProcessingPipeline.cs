using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace LyricFX.Core.Pipeline
{
    /// <summary>
    /// 字符处理管道 - 协调多个处理器按顺序处理字符
    /// </summary>
    public class CharacterProcessingPipeline
    {
        private readonly List<ICharacterProcessor> processors = new List<ICharacterProcessor>();
        
        /// <summary>
        /// 注册一个处理器到管道
        /// </summary>
        public void RegisterProcessor(ICharacterProcessor processor)
        {
            processors.Add(processor);
            // 按优先级排序
            processors.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            Debug.Log($"[字符管道] 注册处理器: {processor.ProcessorId}, 优先级: {processor.Priority}");
        }
        
        /// <summary>
        /// 注销一个处理器
        /// </summary>
        public void UnregisterProcessor(string processorId)
        {
            var processor = processors.FirstOrDefault(p => p.ProcessorId == processorId);
            if (processor != null)
            {
                processors.Remove(processor);
                Debug.Log($"[字符管道] 注销处理器: {processorId}");
            }
        }
        
        /// <summary>
        /// 处理一个字符
        /// </summary>
        public async UniTask<ProcessingContext> ProcessCharacter(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            Debug.Log($"[字符管道] 开始处理字符 '{context.Character}' (索引: {context.CharacterIndex}, 行: {context.LineId})");
            
            foreach (var processor in processors)
            {
                if (context.IsCompleted || cancellationToken.IsCancellationRequested)
                    break;
                    
                Debug.Log($"[字符管道] 运行处理器: {processor.ProcessorId}");
                context = await processor.Process(context, cancellationToken);
            }
            
            Debug.Log($"[字符管道] 字符处理完成: '{context.Character}' (索引: {context.CharacterIndex}, 行: {context.LineId})");
            return context;
        }
        
        /// <summary>
        /// 批量处理多个字符
        /// </summary>
        public async UniTask<List<ProcessingContext>> ProcessCharacters(
            List<ProcessingContext> contexts, 
            bool parallel = false, 
            CancellationToken cancellationToken = default)
        {
            if (parallel)
            {
                // 并行处理
                var tasks = contexts.Select(context => ProcessCharacter(context, cancellationToken)).ToArray();
                await UniTask.WhenAll(tasks);
                return tasks.Select(t => t.GetAwaiter().GetResult()).ToList();
            }
            else
            {
                // 顺序处理
                var results = new List<ProcessingContext>();
                foreach (var context in contexts)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    var result = await ProcessCharacter(context, cancellationToken);
                    results.Add(result);
                }
                return results;
            }
        }
        
        /// <summary>
        /// 清空所有处理器
        /// </summary>
        public void Clear()
        {
            processors.Clear();
            Debug.Log("[字符管道] 已清空所有处理器");
        }
    }
}
