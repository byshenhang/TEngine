using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LyricFX.Core
{
    /// <summary>
    /// 核心事件系统 - 使用UniTask.Subject实现的事件总线
    /// 降低系统组件间的直接依赖
    /// </summary>
    public static class LyricEvents
    {
        // 字符生命周期事件
        public static readonly UniTaskCompletionSource<bool> InitializationCompleted = new UniTaskCompletionSource<bool>();
        
        // 字符事件
        public static event Action<CharacterEventArgs> OnCharacterCreated;
        public static event Action<CharacterEventArgs> OnCharacterReady;
        public static event Action<CharacterEventArgs> OnCharacterEffectApplied;
        public static event Action<CharacterEventArgs> OnCharacterEffectCompleted;
        public static event Action<CharacterEventArgs> OnCharacterDestroyed;

        // 行级事件
        public static event Action<LineEventArgs> OnLineCreated;
        public static event Action<LineEventArgs> OnLineStarted;
        public static event Action<LineEventArgs> OnLineCompleted;
        
        // 布局事件
        public static event Action<LayoutEventArgs> OnLayoutCalculated;
        
        // 进度事件
        public static event Action<ProgressEventArgs> OnProgressUpdated;
        
        // 触发事件的安全方法
        public static void TriggerCharacterCreated(CharacterEventArgs args) => OnCharacterCreated?.Invoke(args);
        public static void TriggerCharacterReady(CharacterEventArgs args) => OnCharacterReady?.Invoke(args);
        public static void TriggerCharacterEffectApplied(CharacterEventArgs args) => OnCharacterEffectApplied?.Invoke(args);
        public static void TriggerCharacterEffectCompleted(CharacterEventArgs args) => OnCharacterEffectCompleted?.Invoke(args);
        public static void TriggerCharacterDestroyed(CharacterEventArgs args) => OnCharacterDestroyed?.Invoke(args);
        
        public static void TriggerLineCreated(LineEventArgs args) => OnLineCreated?.Invoke(args);
        public static void TriggerLineStarted(LineEventArgs args) => OnLineStarted?.Invoke(args);
        public static void TriggerLineCompleted(LineEventArgs args) => OnLineCompleted?.Invoke(args);
        
        public static void TriggerLayoutCalculated(LayoutEventArgs args) => OnLayoutCalculated?.Invoke(args);
        
        public static void TriggerProgressUpdated(ProgressEventArgs args) => OnProgressUpdated?.Invoke(args);
    }

    // 事件参数类型
    #region Event Args

    public class CharacterEventArgs
    {
        public GameObject CharacterObject { get; set; }
        public int CharacterIndex { get; set; }
        public char Character { get; set; }
        public int LineId { get; set; }
    }

    public class LineEventArgs
    {
        public int LineId { get; set; }
        public string Content { get; set; }
        public string EffectId { get; set; }
        public string LayoutId { get; set; }
        public double TimeStamp { get; set; }
    }

    public class LayoutEventArgs
    {
        public int LineId { get; set; }
        public Vector3[] Positions { get; set; }
    }

    public class ProgressEventArgs
    {
        public int LineId { get; set; }
        public float Progress { get; set; }
    }

    #endregion
}
