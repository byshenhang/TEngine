using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GameLogic.AudioReactor
{
    /// <summary>
    /// 音频反应器状态枚举
    /// </summary>
    public enum AudioReactorState
    {
        /// <summary>未初始化</summary>
        Uninitialized,
        /// <summary>初始化中</summary>
        Initializing,
        /// <summary>已初始化但未启用</summary>
        Initialized,
        /// <summary>启用中</summary>
        Enabling,
        /// <summary>已启用</summary>
        Enabled,
        /// <summary>禁用中</summary>
        Disabling,
        /// <summary>已禁用</summary>
        Disabled,
        /// <summary>错误状态</summary>
        Error,
        /// <summary>释放中</summary>
        Releasing,
        /// <summary>已释放</summary>
        Released
    }
    
    /// <summary>
    /// 标准化音频数据结构
    /// 用于在不同音频反应器之间传递统一格式的音频数据
    /// </summary>
    [Serializable]
    public struct AudioReactorData
    {
        /// <summary>RMS值（音频响度）</summary>
        public float rms;
        
        /// <summary>原始频谱数据（通常512或1024个采样点）</summary>
        public float[] rawSpectrum;
        
        /// <summary>分组频段数据（通常8个频段）</summary>
        public float[] groupedBands;
        
        /// <summary>五频段数据（低音、低中音、中音、高中音、高音）</summary>
        public float[] fiveBands;
        
        /// <summary>动态频段数据（可配置数量）</summary>
        public float[] dynamicBands;
        
        /// <summary>音频时间戳</summary>
        public float timestamp;
        
        /// <summary>采样率</summary>
        public int sampleRate;
        
        /// <summary>数据是否有效</summary>
        public bool isValid;
        
        /// <summary>
        /// 创建空的音频数据
        /// </summary>
        /// <returns>空的音频数据结构</returns>
        public static AudioReactorData Empty => new AudioReactorData
        {
            rms = 0f,
            rawSpectrum = new float[0],
            groupedBands = new float[0],
            fiveBands = new float[0],
            dynamicBands = new float[0],
            timestamp = 0f,
            sampleRate = 0,
            isValid = false
        };
    }
    
    /// <summary>
    /// 统一的音频反应器接口
    /// 为不同的音频反应插件提供统一的控制接口
    /// 支持异步操作、事件通知和参数配置
    /// </summary>
    public interface IAudioReactor
    {
        #region 基本属性
        
        /// <summary>
        /// 反应器唯一标识符
        /// </summary>
        string ReactorId { get; }
        
        /// <summary>
        /// 反应器显示名称
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// 反应器类型（如：AudioReactiveShaders、AudioSyncPro等）
        /// </summary>
        string ReactorType { get; }
        
        /// <summary>
        /// 是否启用
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// 当前状态
        /// </summary>
        AudioReactorState CurrentState { get; }
        
        /// <summary>
        /// 当前使用的音频源
        /// </summary>
        AudioSource CurrentAudioSource { get; }
        
        /// <summary>
        /// 版本信息
        /// </summary>
        string Version { get; }
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 状态变化事件
        /// </summary>
        event Action<IAudioReactor, AudioReactorState> OnStateChanged;
        
        /// <summary>
        /// 音频数据更新事件
        /// </summary>
        event Action<IAudioReactor, AudioReactorData> OnAudioDataUpdated;
        
        /// <summary>
        /// 错误事件
        /// </summary>
        event Action<IAudioReactor, string> OnError;
        
        #endregion
        
        #region 核心方法
        
        /// <summary>
        /// 异步初始化反应器
        /// </summary>
        /// <returns>初始化是否成功</returns>
        UniTask<bool> InitializeAsync();
        
        /// <summary>
        /// 异步启用反应器
        /// </summary>
        /// <returns>启用是否成功</returns>
        UniTask<bool> EnableAsync();
        
        /// <summary>
        /// 异步禁用反应器
        /// </summary>
        /// <returns>禁用是否成功</returns>
        UniTask<bool> DisableAsync();
        
        /// <summary>
        /// 设置音频源
        /// </summary>
        /// <param name="audioSource">音频源</param>
        /// <returns>设置是否成功</returns>
        UniTask<bool> SetAudioSourceAsync(AudioSource audioSource);
        
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <returns>释放任务</returns>
        UniTask ReleaseAsync();
        
        #endregion
        
        #region 参数配置
        
        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns>设置是否成功</returns>
        UniTask<bool> SetParameterAsync(string parameterName, object value);
        
        /// <summary>
        /// 获取参数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        /// <returns>参数值</returns>
        UniTask<T> GetParameterAsync<T>(string parameterName);
        
        /// <summary>
        /// 获取所有可用参数名称
        /// </summary>
        /// <returns>参数名称列表</returns>
        UniTask<List<string>> GetAvailableParametersAsync();
        
        #endregion
        
        #region 信息查询
        
        /// <summary>
        /// 获取反应器详细信息
        /// </summary>
        /// <returns>详细信息字典</returns>
        UniTask<Dictionary<string, object>> GetInfoAsync();
        
        #endregion
    }
}