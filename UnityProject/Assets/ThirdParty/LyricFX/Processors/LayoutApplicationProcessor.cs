using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using LyricFX.Core.Pipeline;
using LyricFX.Registry;
using System.Threading;
using UnityEngine;

namespace LyricFX.Processors
{
    /// <summary>
    /// 布局应用处理器 - 将字符对象放置到计算好的位置
    /// </summary>
    public class LayoutApplicationProcessor : ICharacterProcessor
    {
        public int Priority => 20; // 布局应用优先级，在创建之后执行
        public string ProcessorId => "layout_application";
        
        private void Initialize()
        {
            
        }
        
        /// <summary>
        /// 处理字符对象的父级设置和基础属性配置
        /// 注意：位置设置由 ILayoutProvider.ApplyLayout 统一处理
        /// </summary>
        /// <param name="context">处理上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理后的上下文</returns>
        public async UniTask<ProcessingContext> Process(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            if (context.CharacterObject == null)
            {
                Debug.LogError("[布局应用处理器] 字符对象为空，无法应用布局");
                return context;
            }
            
            // 从上下文获取布局ID和行ID
            string layoutId = context.GetMetadata<string>("layoutId", "default_linear");
            int lineId = context.LineId;
            
            // 找到对应的歌词行容器
            var lineContainer = GameObject.Find($"LyricLine_{lineId}");
            if (lineContainer != null)
            {
                // 设置字符对象的父对象为歌词行容器
                context.CharacterObject.transform.SetParent(lineContainer.transform);
                
                Debug.Log($"[布局应用处理器] 设置字符父对象: '{context.Character}' (索引: {context.CharacterIndex}), 父对象: {lineContainer.name}");
            }
            else
            {
                Debug.LogError($"[布局应用处理器] 未找到歌词行容器: LyricLine_{lineId}");
            }
            
            // 可以添加一些额外的属性设置，如旋转或缩放
            // 注意：位置设置由 ILayoutProvider.ApplyLayout 统一处理，避免重复设置
            
            await UniTask.CompletedTask;
            return context;
        }
    }
}
