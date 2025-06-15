using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 字幕效果接口
    /// </summary>
    public interface ISubtitleEffect
    {
        /// <summary>
        /// 效果是否已完成
        /// </summary>
        bool IsCompleted { get; }
        
        /// <summary>
        /// 效果是否正在运行
        /// </summary>
        bool IsRunning { get; }
        
        /// <summary>
        /// 初始化效果
        /// </summary>
        /// <param name="config">效果配置</param>
        void Initialize(ISubtitleEffectConfig config);
        
        /// <summary>
        /// 开始播放效果
        /// </summary>
        UniTask PlayAsync();
        
        /// <summary>
        /// 停止效果
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 暂停效果
        /// </summary>
        void Pause();
        
        /// <summary>
        /// 恢复效果
        /// </summary>
        void Resume();
        
        /// <summary>
        /// 更新效果
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(float deltaTime);
        
        /// <summary>
        /// 释放效果资源
        /// </summary>
        void Release();
    }
    
    /// <summary>
    /// 字幕效果配置接口
    /// </summary>
    public interface ISubtitleEffectConfig
    {
        /// <summary>
        /// 效果持续时间
        /// </summary>
        float Duration { get; }
        
        /// <summary>
        /// 效果延迟时间
        /// </summary>
        float Delay { get; }
        
        /// <summary>
        /// 目标GameObject
        /// </summary>
        GameObject Target { get; }
        
        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="parameterName">参数名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值</returns>
        T GetParameter<T>(string parameterName, T defaultValue = default(T));
    }
    
    /// <summary>
    /// 字幕序列接口
    /// </summary>
    public interface ISubtitleSequence
    {
        /// <summary>
        /// 序列是否已完成
        /// </summary>
        bool IsCompleted { get; }
        
        /// <summary>
        /// 序列是否正在运行
        /// </summary>
        bool IsRunning { get; }
        
        /// <summary>
        /// 播放序列
        /// </summary>
        UniTask PlayAsync();
        
        /// <summary>
        /// 停止序列
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 暂停序列
        /// </summary>
        void Pause();
        
        /// <summary>
        /// 恢复序列
        /// </summary>
        void Resume();
        
        /// <summary>
        /// 更新序列
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(float deltaTime);
        
        /// <summary>
        /// 释放序列资源
        /// </summary>
        void Release();
    }
}