using Cysharp.Threading.Tasks;
using LyricFX.Core.Interfaces;
using LyricFX.Core.Pipeline;
using LyricFX.Registry;
using System.Threading;
using UnityEngine;

namespace LyricFX.Processors
{
    /// <summary>
    /// 效果应用处理器 - 初始化并应用视觉效果到字符对象
    /// </summary>
    public class EffectApplicationProcessor : ICharacterProcessor
    {
        public int Priority => 30; // 效果应用优先级，在创建和布局之后执行
        public string ProcessorId => "effect_application";


        public async UniTask<ProcessingContext> Process(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            if (context.CharacterObject == null)
            {
                Debug.LogError("[效果应用处理器] 字符对象为空，无法应用效果");
                return context;
            }

            // 从上下文获取效果ID
            string effectId = context.GetMetadata<string>("effectId", "default_fade");


            // 使用注册表中的效果提供器
            if (!EffectRegistry.RequiresCoordinator(effectId))
            {
                var effectProvider = EffectRegistry.GetEffectProvider(effectId);
                if (effectProvider != null)
                {
                    // 初始化效果，但不播放
                    // 播放将由LyricManager单独控制
                    await effectProvider.Initialize(context.CharacterObject, null, cancellationToken);

                    // 将效果ID存储在上下文中
                    context.SetMetadata("appliedEffectId", effectProvider.EffectId);

                    Debug.Log($"[效果应用处理器] 应用效果 '{effectId}' 到字符: '{context.Character}' (索引: {context.CharacterIndex})");
                }
                else
                {
                    Debug.LogError($"[效果应用处理器] 未找到效果提供器: {effectId}");
                }
            }
            
            return context;
        }
    }
}
