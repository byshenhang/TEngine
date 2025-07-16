using UnityEngine;
using System;

namespace LyricFX.Config
{
    /// <summary>
    /// 歌词系统性能配置
    /// </summary>
    [CreateAssetMenu(fileName = "LyricPerformanceConfig", menuName = "LyricFX/Performance Config")]
    [Serializable]
    public class LyricPerformanceConfig : ScriptableObject
    {
        [Header("对象池配置")]
        [Tooltip("初始对象池大小")]
        [Range(10, 100)]
        public int initialPoolSize = 20;
        
        [Tooltip("最大对象池大小")]
        [Range(50, 500)]
        public int maxPoolSize = 100;
        
        [Tooltip("对象池预热倍数")]
        [Range(1.0f, 3.0f)]
        public float poolWarmupMultiplier = 1.5f;
        
        [Header("异步处理配置")]
        [Tooltip("启用异步字符回收")]
        public bool enableAsyncRecycling = true;
        
        [Tooltip("每帧最大处理对象数")]
        [Range(1, 10)]
        public int maxProcessPerFrame = 3;
        
        [Tooltip("启用异步歌词行清理")]
        public bool enableAsyncLineCleanup = true;
        
        [Tooltip("批处理大小")]
        [Range(1, 5)]
        public int batchSize = 2;
        
        [Header("延迟清理配置")]
        [Tooltip("启用延迟清理")]
        public bool enableDelayedCleanup = true;
        
        [Tooltip("延迟清理时间（秒）")]
        [Range(0.1f, 2.0f)]
        public float delayedCleanupTime = 0.5f;
        
        [Header("性能监控配置")]
        [Tooltip("启用性能监控")]
        public bool enablePerformanceMonitoring = true;
        
        [Tooltip("性能报告间隔（秒）")]
        [Range(1f, 30f)]
        public float performanceReportInterval = 5f;
        
        [Tooltip("最大性能记录数量")]
        [Range(50, 500)]
        public int maxPerformanceRecords = 100;
        
        [Header("调试配置")]
        [Tooltip("启用详细日志")]
        public bool enableVerboseLogging = false;
        
        [Tooltip("显示对象池状态")]
        public bool showPoolStatus = true;
        
        /// <summary>
        /// 应用配置到歌词系统
        /// </summary>
       
        
        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefaults()
        {
            initialPoolSize = 20;
            maxPoolSize = 100;
            poolWarmupMultiplier = 1.5f;
            enableAsyncRecycling = true;
            maxProcessPerFrame = 3;
            enableAsyncLineCleanup = true;
            batchSize = 2;
            enableDelayedCleanup = true;
            delayedCleanupTime = 0.5f;
            enablePerformanceMonitoring = true;
            performanceReportInterval = 5f;
            maxPerformanceRecords = 100;
            enableVerboseLogging = false;
            showPoolStatus = true;
            
            Debug.Log("[性能配置] 已重置为默认配置");
        }
        
        /// <summary>
        /// 获取配置摘要
        /// </summary>
        /// <returns>配置摘要字符串</returns>
        public string GetConfigSummary()
        {
            return $"对象池: {initialPoolSize}-{maxPoolSize}, " +
                   $"异步回收: {enableAsyncRecycling}, " +
                   $"异步清理: {enableAsyncLineCleanup}, " +
                   $"延迟清理: {enableDelayedCleanup}, " +
                   $"性能监控: {enablePerformanceMonitoring}";
        }
    }
}