using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using LyricFX.Core;
using LyricFX.States;
using LyricFX.Rendering;

namespace LyricFX.Effects
{
    /// <summary>
    /// 效果适配器，连接字符、状态和效果
    /// </summary>
    public class EffectAdapter : IDisposable
    {
        private LyricCharacter _character;
        private ICharacterRenderer _renderer;
        private StateManager _stateManager;
        private CancellationTokenSource _effectsCts;
        
        // 状态->效果映射
        private Dictionary<CharacterState, List<BaseEffect>> _stateEffects;
        
        // 当前活跃效果
        private List<BaseEffect> _activeEffects = new List<BaseEffect>();
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public CharacterState CurrentState => _stateManager.CurrentState;
        
        /// <summary>
        /// 状态变化事件
        /// </summary>
        public event Action<CharacterState, CharacterState> OnStateChanged;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public EffectAdapter(LyricCharacter character, ICharacterRenderer renderer)
        {
            _character = character;
            _renderer = renderer;
            _stateManager = new StateManager();
            _stateEffects = new Dictionary<CharacterState, List<BaseEffect>>();
            
            // 设置默认转换路径
            SetupDefaultTransitions();
            
            // 订阅状态变化事件
            _stateManager.OnStateChanged += HandleStateChanged;
        }
        
        /// <summary>
        /// 设置默认转换路径
        /// </summary>
        private void SetupDefaultTransitions()
        {
            // 默认流程: Waiting -> Enter -> Stay -> Exit -> Complete
            _stateManager.AddTransition(CharacterState.Waiting, CharacterState.Enter);
            _stateManager.AddTransition(CharacterState.Enter, CharacterState.Stay);
            _stateManager.AddTransition(CharacterState.Stay, CharacterState.Exit);
            _stateManager.AddTransition(CharacterState.Exit, CharacterState.Complete);
        }
        
        /// <summary>
        /// 处理状态变化
        /// </summary>
        private async void HandleStateChanged(CharacterState oldState, CharacterState newState)
        {
            int lineIndex = _character?.LineIndex?? -1;
            int charIndex = _character?.Index ?? -1;
            string charName = _character?.Character.ToString() ?? "?";
            
            // 日志记录状态变化
            LyricLogger.Log($"字符状态变化 - 行{lineIndex}, 字符{charIndex}[{charName}]: {oldState} -> {newState}");
            
            // 取消当前生效的效果
            CancelActiveEffects();
            
            // 广播状态变化事件
            OnStateChanged?.Invoke(oldState, newState);
            
            // 根据状态设置激活状态 - 修改默认行为，让Complete状态下的字符也保持激活
            // 只有Waiting状态的字符才设置为非激活
            bool shouldBeActive = (newState != CharacterState.Waiting);
            _renderer?.SetActive(shouldBeActive);
            
            // 日志记录激活状态
            string activeStatus = _renderer != null ? (_renderer.IsActive() ? "True" : "False") : "Unknown";
            LyricLogger.Log($"字符激活状态 - 行{lineIndex}, 字符{charIndex}[{charName}]: Active={activeStatus}");
            
            // 如果状态是进入而字符没有激活，强制激活
            if (newState == CharacterState.Enter && activeStatus == "False")
            {
                LyricLogger.LogError($"错误:字符进入状态但未激活 - 行{lineIndex}, 字符{charIndex}[{charName}]");
                // 尝试强制激活
                _renderer?.SetActive(true);
            }
            
            // 准备并运行新效果
            if (_stateEffects.TryGetValue(newState, out var effects) && effects.Count > 0)
            {
                _effectsCts = new CancellationTokenSource();
                
                // 检查目标TextMeshPro字体当前状态
                var context = CreateCharacterContext();
                if (context.TextComponent != null)
                {
                    LyricLogger.Log($"状态变更前检查 - 字符:{context.TextComponent.text}, 当前透明度:{context.TextComponent.color.a:F2}");
                }
                
                try
                {
                    // 等待效果链执行完成
                    await ExecuteEffectChain(effects, _effectsCts.Token);
                    
                    // 效果执行后再次检查激活状态
                    if (_renderer != null)
                    {
                        activeStatus = _renderer.IsActive() ? "True" : "False";
                        LyricLogger.Log($"效果执行后字符激活状态 - 行{lineIndex}, 字符{charIndex}[{charName}]: Active={activeStatus}");
                        
                        // 再次确保字符激活(如果应该激活)
                        if (shouldBeActive && !_renderer.IsActive())
                        {
                            LyricLogger.Log($"效果执行后字符仍然未激活，再次强制激活 - 行{lineIndex}, 字符{charIndex}[{charName}]");
                            _renderer.SetActive(true);
                        }
                    }
                    
                    // 如果是第一个字符，再次检查透明度
                    if (lineIndex == 0 && charIndex == 0 && context.TextComponent != null)
                    {
                        float currentAlpha = context.TextComponent.color.a;
                        LyricLogger.Log($"第一个字符效果执行后 - 当前透明度:{currentAlpha:F2}");
                        
                        // 如果透明度仍然为0，强制设置为可见
                        if (currentAlpha < 0.1f && newState == CharacterState.Enter)
                        {
                            Color color = context.TextComponent.color;
                            color.a = 1.0f; // 强制设置完全可见
                            context.TextComponent.color = color;
                            LyricLogger.Log($"第一个字符强制设置透明度 - 新透明度:{context.TextComponent.color.a:F2}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LyricLogger.LogError($"执行效果链失败 - 行{lineIndex}, 字符{charIndex}[{charName}]: {ex.Message}");
                }
            }
            else
            {
                LyricLogger.Log($"警告: 行{lineIndex}, 字符{charIndex}[{charName}] 在状态 {newState} 没有关联效果");
            }
        }
        
        /// <summary>
        /// 执行效果链
        /// </summary>
        private async UniTask ExecuteEffectChain(List<BaseEffect> effects, CancellationToken token)
        {
            int lineIndex = _character?.LineIndex?? -1;
            int charIndex = _character?.Index ?? -1;
            string charName = _character?.Character.ToString() ?? "?";
            
            LyricLogger.Log($"开始执行效果链 - 行{lineIndex}, 字符{charIndex}[{charName}], 效果数量:{effects.Count}");
            
            _activeEffects.Clear();
            _activeEffects.AddRange(effects);
            
            // 构建效果上下文
            var context = CreateCharacterContext();
            LyricLogger.Log($"效果上下文创建 - 字符:{context.TextComponent?.text ?? "null"}, 当前状态:{_stateManager.CurrentState}");
            
            // 执行效果链
            var effectChain = new EffectChain(effects);
            
            try
            {
                // 确保字符渲染器处于激活状态
                if (_renderer != null && !_renderer.IsActive())
                {
                    LyricLogger.Log($"执行效果前确保渲染器激活 - 行{lineIndex}, 字符{charIndex}[{charName}]");
                    _renderer.SetActive(true);
                }
                
                // 记录执行前的透明度
                float alphaBeforeEffects = context.TextComponent != null ? context.TextComponent.color.a : -1;
                LyricLogger.Log($"效果执行前 - 字符:{context.TextComponent?.text ?? "null"}, 透明度:{alphaBeforeEffects:F2}");
                
                // 开始执行效果链
                await effectChain.ExecuteAsync(context, token);
                
                // 确保效果有足够时间应用
                await UniTask.Delay(50, cancellationToken: token);
                
                // 记录执行后的透明度
                float alphaAfterEffects = context.TextComponent != null ? context.TextComponent.color.a : -1;
                LyricLogger.Log($"效果执行完成 - 字符:{context.TextComponent?.text ?? "null"}, 透明度:{alphaAfterEffects:F2}");
            }
            catch (OperationCanceledException)
            {
                LyricLogger.Log($"效果执行被取消 - 行{lineIndex}, 字符{charIndex}[{charName}]");
                // 预期中的取消
            }
            catch (Exception ex)
            {
                LyricLogger.LogError($"效果执行异常 - 行{lineIndex}, 字符{charIndex}[{charName}]: {ex.Message}");
                Debug.LogError($"Error executing effects: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 构建字符上下文
        /// </summary>
        private CharacterContext CreateCharacterContext()
        {
            return new CharacterContext(_character, _renderer)
            {
                CurrentState = CurrentState
            };
        }
        
        /// <summary>
        /// 转到指定状态
        /// </summary>
        public async UniTask TransitionTo(CharacterState state, CancellationToken token = default)
        {
            await _stateManager.TransitionTo(state, token);
        }
        
        /// <summary>
        /// 配置特定状态的效果
        /// </summary>
        public void ConfigureEffects(CharacterState state, List<BaseEffect> effects)
        {
            _stateEffects[state] = effects;
        }
        
        /// <summary>
        /// 获取当前特定类型的活跃效果
        /// </summary>
        public T GetActiveEffect<T>() where T : BaseEffect
        {
            return _activeEffects.Find(e => e is T) as T;
        }
        
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            CancelActiveEffects();
            _stateManager.OnStateChanged -= HandleStateChanged;
        }
        
        /// <summary>
        /// 取消活跃效果
        /// </summary>
        private void CancelActiveEffects()
        {
            _effectsCts?.Cancel();
            _effectsCts?.Dispose();
            _effectsCts = null;
        }
    }
}
