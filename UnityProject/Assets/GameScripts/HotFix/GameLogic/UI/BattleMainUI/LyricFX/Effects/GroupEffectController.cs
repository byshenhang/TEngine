using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LyricFX.Core;
using LyricFX.States;

namespace LyricFX.Effects
{
    /// <summary>
    /// 组效果控制器，用于协调多个字符的效果执行
    /// </summary>
    public class GroupEffectController
    {
        private List<EffectAdapter> _adapters = new List<EffectAdapter>();
        
        // 组级别状态参数
        private float _groupProgress;
        private CancellationTokenSource _groupCts;
        
        // 组级事件
        public event Action<float> OnGroupProgressChanged;
        public event Action OnGroupCompleted;
        
        /// <summary>
        /// 添加字符适配器
        /// </summary>
        public void AddAdapter(EffectAdapter adapter)
        {
            _adapters.Add(adapter);
        }

        /// <summary>
        /// 添加多个适配器
        /// </summary>
        public void AddAdapters(IEnumerable<EffectAdapter> adapters)
        {
            _adapters.AddRange(adapters);
        }
        
        /// <summary>
        /// 按顺序激活效果，在指定时间范围内均匀显示
        /// </summary>
        public async UniTask ActivateInSequence(
            CharacterState state,
            SequenceOptions options,
            CancellationToken token)
        {
            _groupCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            
            try
            {
                int count = _adapters.Count;
                _groupProgress = 0f;
                
                // 计算应该使用的时间范围内的字符显示
                float totalDuration = options.TotalDuration > 0 ? options.TotalDuration : 
                    (count * (options.Delay > 0 ? options.Delay : 0.1f));
                
                LyricLogger.Log($"开始按顺序激活: 状态={state}, 起始索引={options.StartIndex}, 步长={options.Step}, 总字符数={count}, 总时长={totalDuration}秒");
                
                // 计算每个字符分配的时间
                int itemsToProcess = (count - options.StartIndex + options.Step - 1) / options.Step;
                float timePerItem = totalDuration / itemsToProcess;
                float startTime = Time.time;
                
                for (int i = options.StartIndex; i < count; i += options.Step)
                {
                    if (i >= count || i < 0) continue;
                    
                    // 计算当前索引应该在什么时间点显示
                    int itemIndex = (i - options.StartIndex) / options.Step;
                    float targetTime = startTime + (itemIndex * timePerItem);
                    float currentTime = Time.time;
                    
                    // 如果当前时间小于目标时间，等待至目标时间
                    if (currentTime < targetTime)
                    {
                        float waitTime = targetTime - currentTime;
                        //await UniTask.WaitForEndOfFrame();
                        await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: _groupCts.Token);
                    }
                    
                    // 触发状态转换
                    if (i >= 0 && i < _adapters.Count)
                    {
                        // 日志记录当前字符激活信息
                        LyricLogger.Log($"激活字符 - 索引:{i}, 总数:{count}, 时间点:{Time.time - startTime:F2}/{totalDuration:F2}秒");
                        
                        // 调用状态转换
                        await _adapters[i].TransitionTo(state, _groupCts.Token);
                        
                        // 添加适当的延迟确保效果执行完成
                        // 但不要影响整体时间安排
                        float effectDelay = 0;
                        if (state == CharacterState.Enter)
                        {
                            effectDelay = Mathf.Min(0.3f, timePerItem * 0.5f); // 使用更灵活的延迟时间
                        }
                        else if (state == CharacterState.Stay)
                        {
                            effectDelay = Mathf.Min(0.1f, timePerItem * 0.2f);
                        }
                        
                        if (effectDelay > 0)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(effectDelay), cancellationToken: _groupCts.Token);
                        }
                    }
                    
                    // 更新组进度
                    int itemsProcessed = itemIndex + 1;
                    _groupProgress = (float)itemsProcessed / itemsToProcess;
                    OnGroupProgressChanged?.Invoke(_groupProgress);
                }
                
                _groupProgress = 1f;
                OnGroupCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // 正常取消，忽略
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in ActivateInSequence: {ex.Message}");
            }
            finally
            {
                _groupCts?.Dispose();
                _groupCts = null;
            }
        }
        
        /// <summary>
        /// 交替激活（奇偶数索引）
        /// </summary>
        public async UniTask ActivateAlternating(
            CharacterState state,
            bool evenFirst, // 是否先激活偶数索引
            float delay,
            string completionCondition,
            CancellationToken token)
        {
            // 选择首先激活的索引
            int firstStartIndex = evenFirst ? 0 : 1;
            
            // 创建第一组序列选项
            var firstGroupOptions = new SequenceOptions
            {
                StartIndex = firstStartIndex,
                Step = 2,
                Delay = delay,
                WaitForCompletion = true,
                CompletionCondition = completionCondition
            };
            
            // 创建第二组序列选项
            var secondGroupOptions = new SequenceOptions
            {
                StartIndex = 1 - firstStartIndex, // 如果firstStartIndex是0，则这里是1；反之亦然
                Step = 2,
                Delay = delay,
                WaitForCompletion = true,
                CompletionCondition = completionCondition
            };
            
            // 先激活第一组
            await ActivateInSequence(state, firstGroupOptions, token);
            
            // 再激活第二组
            await ActivateInSequence(state, secondGroupOptions, token);
        }
        
        /// <summary>
        /// 同时应用效果到所有字符
        /// </summary>
        public async UniTask ActivateAll(CharacterState state, CancellationToken token)
        {
            var tasks = _adapters.Select(adapter => adapter.TransitionTo(state, token));
            await UniTask.WhenAll(tasks);
        }
        
        /// <summary>
        /// 等待条件满足
        /// </summary>
        private async UniTask WaitForCondition(Func<bool> condition, CancellationToken token)
        {
            await UniTask.WaitUntil(condition, PlayerLoopTiming.Update, token);
        }

        /// <summary>
        /// 清除所有适配器
        /// </summary>
        public void Clear()
        {
            _adapters.Clear();
        }
        
        /// <summary>
        /// 获取第一个字符的状态
        /// </summary>
        public CharacterState GetFirstCharacterState()
        {
            if (_adapters.Count > 0)
            {
                return _adapters[0].CurrentState;
            }
            return CharacterState.Waiting;
        }
        
        /// <summary>
        /// 检查第一个字符是否激活
        /// </summary>
        public bool IsFirstCharacterActive()
        {
            try
            {
                if (_adapters == null || _adapters.Count == 0 || _adapters[0] == null)
                    return false;
                    
                return IsAdapterCharacterActive(_adapters[0]);
            }
            catch (System.Exception ex)
            {
                LyricLogger.LogError($"检查第一个字符激活状态时发生错误: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 输出字符状态调试信息
        /// </summary>
        public void LogCharacterStatus(int lineIndex)
        {
            LyricLogger.Log($"===== 行 {lineIndex} 字符状态检查 =====");
            for (int i = 0; i < _adapters.Count; i++)
            {
                LyricLogger.Log($"字符 {i}: 状态={_adapters[i].CurrentState}, 激活={IsAdapterCharacterActive(_adapters[i])}");
            }
            LyricLogger.Log($"===== 行 {lineIndex} 检查结束 =====");
        }
        
        /// <summary>
        /// 检查适配器的字符是否激活
        /// </summary>
        private bool IsAdapterCharacterActive(EffectAdapter adapter)
        {
            try
            {
                if (adapter == null) return false;
                
                // 使用反射安全地获取渲染器
                var rendererField = adapter.GetType().GetField("_renderer",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                    
                if (rendererField == null) return false;
                
                var renderer = rendererField.GetValue(adapter) as LyricFX.Rendering.ICharacterRenderer;
                if (renderer == null) return false;
                
                // 安全调用IsActive方法
                return renderer.IsActive();
            }
            catch (System.Exception)
            {
                // 当对象已销毁或其他异常情况时默认为未激活
                return false;
            }
        }
        
        /// <summary>
        /// 用于配置序列选项的结构
        /// </summary>
        public class SequenceOptions
        {
            /// <summary>起始字符索引</summary>
            public int StartIndex { get; set; } = 0;
            
            /// <summary>每次增加的步长</summary>
            public int Step { get; set; } = 1;
            
            /// <summary>字符间的延迟时间(秒)</summary>
            public float Delay { get; set; } = 0.1f;
            
            /// <summary>是否等待上一个完成</summary>
            public bool WaitForCompletion { get; set; } = false;
            
            /// <summary>完成条件</summary>
            public string CompletionCondition { get; set; } = string.Empty;
            
            /// <summary>显示所有字符的总时长(秒)。如果设置为0，则使用基于Delay的默认时间</summary>
            public float TotalDuration { get; set; } = 0f;
        }
    }
}
