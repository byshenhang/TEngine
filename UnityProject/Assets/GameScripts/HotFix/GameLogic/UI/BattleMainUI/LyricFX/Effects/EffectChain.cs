using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LyricFX.Core;

namespace LyricFX.Effects
{
    /// <summary>
    /// 效果链，用于组合和执行多个效果
    /// </summary>
    public class EffectChain
    {
        private List<BaseEffect> _effects;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="effects">效果列表</param>
        public EffectChain(List<BaseEffect> effects)
        {
            _effects = effects ?? new List<BaseEffect>();
        }
        
        /// <summary>
        /// 并行执行所有效果
        /// </summary>
        public async UniTask ExecuteAsync(CharacterContext context, CancellationToken token = default)
        {
            if (_effects == null || _effects.Count == 0)
                return;
                
            try
            {
                // 创建任务列表
                var tasks = _effects.Select(effect => 
                    effect.ExecuteAsync(context.TextComponent, context, token)).ToArray();
                    
                // 并行执行所有效果
                await UniTask.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // 预期的取消，忽略
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing effect chain: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 串行执行效果
        /// </summary>
        public async UniTask ExecuteSerialAsync(CharacterContext context, CancellationToken token = default)
        {
            if (_effects == null || _effects.Count == 0)
                return;
                
            try
            {
                foreach (var effect in _effects)
                {
                    await effect.ExecuteAsync(context.TextComponent, context, token);
                    
                    if (token.IsCancellationRequested)
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // 预期的取消，忽略
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing effect chain serially: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加效果到链中
        /// </summary>
        public void AddEffect(BaseEffect effect)
        {
            _effects.Add(effect);
        }

        /// <summary>
        /// 清空所有效果
        /// </summary>
        public void Clear()
        {
            _effects.Clear();
        }
    }
}
