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
    public class LayoutApplicationProcessor : MonoBehaviour, ICharacterProcessor
    {
        [SerializeField] private LayoutRegistry layoutRegistry;
        
        public int Priority => 20; // 布局应用优先级，在创建之后执行
        public string ProcessorId => "layout_application";
        
        private void Start()
        {
            // 尝试找到布局注册表
            if (layoutRegistry == null)
            {
                layoutRegistry = GetComponentInParent<LayoutRegistry>();
                if (layoutRegistry == null)
                {
                    Debug.LogWarning("[布局应用处理器] 未找到布局注册表，将使用默认位置逻辑");
                }
            }
        }
        
        public async UniTask<ProcessingContext> Process(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            if (context.CharacterObject == null)
            {
                Debug.LogError("[布局应用处理器] 字符对象为空，无法应用布局");
                return context;
            }
            
            // 从上下文获取布局ID
            string layoutId = context.GetMetadata<string>("layoutId", "default_linear");
            
            if (layoutRegistry != null)
            {
                // 使用注册表中的布局提供器
                var layoutProvider = layoutRegistry.GetLayoutProvider(layoutId);
                if (layoutProvider != null)
                {
                    // 应用单个字符位置
                    context.CharacterObject.transform.localPosition = context.Position;
                    
                    // 也可以通过布局提供器的ApplyLayout方法应用自定义逻辑
                    // 但这里我们只应用简单的位置设置，因为位置计算已经完成
                    
                    Debug.Log($"[布局应用处理器] 应用布局 '{layoutId}' 到字符: '{context.Character}' (索引: {context.CharacterIndex})");
                }
                else
                {
                    Debug.LogError($"[布局应用处理器] 未找到布局提供器: {layoutId}");
                    // 应用默认位置
                    context.CharacterObject.transform.localPosition = context.Position;
                }
            }
            else
            {
                // 直接应用位置
                context.CharacterObject.transform.localPosition = context.Position;
                Debug.Log($"[布局应用处理器] 应用默认位置到字符: '{context.Character}' (索引: {context.CharacterIndex})");
            }
            
            // 可以添加一些额外的定位逻辑，如旋转或缩放
            
            await UniTask.CompletedTask;
            return context;
        }
    }
}
