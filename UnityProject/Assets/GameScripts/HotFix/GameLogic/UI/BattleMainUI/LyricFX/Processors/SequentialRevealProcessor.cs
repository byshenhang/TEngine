using Cysharp.Threading.Tasks;
using LyricFX.Core.Pipeline;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LyricFX.Processors
{
    /// <summary>
    /// 序列显示处理器 - 为序列模糊效果提供字符索引和顺序信息
    /// 该处理器负责收集字符上下文并标记序列信息，以支持序列模糊效果的交替显示逻辑
    /// </summary>
    public class SequentialRevealProcessor : CharacterProcessor
    {
        public override int Priority => 40; // 在布局应用之后，效果应用之前执行
        public override string ProcessorId => "sequential_reveal_processor";
        
        // 用于分组保存处理过的字符信息，按行ID组织
        private Dictionary<int, List<ProcessingContext>> processedLineContexts = new Dictionary<int, List<ProcessingContext>>();
        
        // 字符处理序号状态
        private Dictionary<int, int> lineProcessedCount = new Dictionary<int, int>();
        
        /// <summary>
        /// 初始化处理器
        /// </summary>
        public void Initialize()
        {
            processedLineContexts.Clear();
            lineProcessedCount.Clear();
            Debug.Log("[序列显示处理器] 已初始化");
        }
        
        /// <summary>
        /// 处理单个字符上下文
        /// </summary>
        protected override async UniTask<ProcessingContext> OnProcess(ProcessingContext context, CancellationToken cancellationToken)
        {
            int lineId = context.LineId;
            
            // 确保相关字典已初始化
            if (!processedLineContexts.ContainsKey(lineId))
            {
                processedLineContexts[lineId] = new List<ProcessingContext>();
                lineProcessedCount[lineId] = 0;
            }
            
            // 添加到处理列表并更新处理计数
            processedLineContexts[lineId].Add(context);
            int currentIndex = lineProcessedCount[lineId]++;
            
            // 在上下文中保存序列信息
            // 1. 字符在整行中的序列索引
            context.SetMetadata("sequenceIndex", currentIndex);
            
            // 2. 是否为偶数位置字符（用于交替显示）
            bool isEven = currentIndex % 2 == 0;
            context.SetMetadata("isEvenPosition", isEven);
            
            // 3. 计算字符在其分组中的索引（偶数组或奇数组）
            int groupIndex = isEven ? currentIndex / 2 : (currentIndex - 1) / 2;
            context.SetMetadata("groupIndex", groupIndex);
            
            // 4. 计算该字符应该显示的序列顺序（偶数字符先显示，然后是奇数字符）
            // 偶数位置的显示顺序从0开始，奇数位置从总偶数位置数量开始
            int evenCount = (processedLineContexts[lineId].Count + 1) / 2; // 向上取整获得偶数位置字符总数
            int displayOrder = isEven ? groupIndex : evenCount + groupIndex;
            context.SetMetadata("displayOrder", displayOrder);
            
            // 记录调试信息
            Debug.Log($"[序列显示处理器] 处理字符 '{context.Character}' - 行ID: {lineId}, 序列索引: {currentIndex}, 显示顺序: {displayOrder}");
            
            return context;
        }
        
        /// <summary>
        /// 清理特定行的处理状态
        /// </summary>
        public void ClearLineData(int lineId)
        {
            if (processedLineContexts.ContainsKey(lineId))
            {
                processedLineContexts.Remove(lineId);
                lineProcessedCount.Remove(lineId);
                Debug.Log($"[序列显示处理器] 已清理行 {lineId} 的处理状态");
            }
        }
        
        /// <summary>
        /// 清理所有行的处理状态
        /// </summary>
        public void ClearAllData()
        {
            processedLineContexts.Clear();
            lineProcessedCount.Clear();
            Debug.Log("[序列显示处理器] 已清理所有处理状态");
        }
        
        /// <summary>
        /// 获取指定行的处理字符总数
        /// </summary>
        public int GetProcessedCharacterCount(int lineId)
        {
            return lineProcessedCount.TryGetValue(lineId, out int count) ? count : 0;
        }
        
        /// <summary>
        /// 获取指定行的处理上下文列表
        /// </summary>
        public List<ProcessingContext> GetLineContexts(int lineId)
        {
            return processedLineContexts.TryGetValue(lineId, out var contexts) ? contexts : new List<ProcessingContext>();
        }
    }
}
