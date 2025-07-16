using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace LyricFX.Utils
{
    /// <summary>
    /// 歌词系统性能监控器
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        private static PerformanceMonitor _instance;
        public static PerformanceMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[LyricFX] PerformanceMonitor");
                    _instance = go.AddComponent<PerformanceMonitor>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("性能监控配置")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float reportInterval = 5f; // 报告间隔（秒）
        [SerializeField] private int maxRecordCount = 100; // 最大记录数量

        // 性能数据记录
        private Queue<PerformanceRecord> performanceRecords = new Queue<PerformanceRecord>();
        private float lastReportTime;
        
        // 当前帧性能数据
        private int currentFrameLineCreations = 0;
        private int currentFrameLineCleanups = 0;
        private int currentFrameCharacterCreations = 0;
        private int currentFrameCharacterRecycles = 0;
        private float currentFrameStartTime;

        private void Start()
        {
            currentFrameStartTime = Time.time;
            lastReportTime = Time.time;
        }

        private void Update()
        {
            if (!enableMonitoring) return;

            // 每帧结束时记录性能数据
            RecordFramePerformance();

            // 定期生成性能报告
            if (Time.time - lastReportTime >= reportInterval)
            {
                GeneratePerformanceReport();
                lastReportTime = Time.time;
            }
        }

        /// <summary>
        /// 记录歌词行创建
        /// </summary>
        public void RecordLineCreation()
        {
            if (!enableMonitoring) return;
            currentFrameLineCreations++;
        }

        /// <summary>
        /// 记录歌词行清理
        /// </summary>
        public void RecordLineCleanup()
        {
            if (!enableMonitoring) return;
            currentFrameLineCleanups++;
        }

        /// <summary>
        /// 记录字符对象创建
        /// </summary>
        public void RecordCharacterCreation()
        {
            if (!enableMonitoring) return;
            currentFrameCharacterCreations++;
        }

        /// <summary>
        /// 记录字符对象回收
        /// </summary>
        public void RecordCharacterRecycle()
        {
            if (!enableMonitoring) return;
            currentFrameCharacterRecycles++;
        }

        /// <summary>
        /// 记录效果执行
        /// </summary>
        public void RecordEffectExecution()
        {
            if (!enableMonitoring) return;
            // 可以在这里添加效果执行的统计
        }

        /// <summary>
        /// 记录对象池预热
        /// </summary>
        public void RecordPoolWarmup()
        {
            if (!enableMonitoring) return;
            // 可以在这里添加对象池预热的统计
        }

        /// <summary>
        /// 记录当前帧性能数据
        /// </summary>
        private void RecordFramePerformance()
        {
            if (currentFrameLineCreations > 0 || currentFrameLineCleanups > 0 || 
                currentFrameCharacterCreations > 0 || currentFrameCharacterRecycles > 0)
            {
                var record = new PerformanceRecord
                {
                    timestamp = Time.time,
                    frameTime = Time.deltaTime,
                    lineCreations = currentFrameLineCreations,
                    lineCleanups = currentFrameLineCleanups,
                    characterCreations = currentFrameCharacterCreations,
                    characterRecycles = currentFrameCharacterRecycles,
                    memoryUsage = GC.GetTotalMemory(false) / 1024f / 1024f // MB
                };

                performanceRecords.Enqueue(record);

                // 限制记录数量
                while (performanceRecords.Count > maxRecordCount)
                {
                    performanceRecords.Dequeue();
                }
            }

            // 重置当前帧计数器
            currentFrameLineCreations = 0;
            currentFrameLineCleanups = 0;
            currentFrameCharacterCreations = 0;
            currentFrameCharacterRecycles = 0;
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        private void GeneratePerformanceReport()
        {
            if (performanceRecords.Count == 0) return;

            float totalFrameTime = 0f;
            int totalLineCreations = 0;
            int totalLineCleanups = 0;
            int totalCharacterCreations = 0;
            int totalCharacterRecycles = 0;
            float maxFrameTime = 0f;
            float currentMemory = 0f;

            foreach (var record in performanceRecords)
            {
                totalFrameTime += record.frameTime;
                totalLineCreations += record.lineCreations;
                totalLineCleanups += record.lineCleanups;
                totalCharacterCreations += record.characterCreations;
                totalCharacterRecycles += record.characterRecycles;
                maxFrameTime = Mathf.Max(maxFrameTime, record.frameTime);
                currentMemory = record.memoryUsage; // 使用最新的内存使用量
            }

            float avgFrameTime = totalFrameTime / performanceRecords.Count;
            float avgFPS = 1f / avgFrameTime;

            Debug.Log($"[性能监控] 过去{reportInterval}秒性能报告:\n" +
                     $"平均帧时间: {avgFrameTime * 1000f:F2}ms\n" +
                     $"平均FPS: {avgFPS:F1}\n" +
                     $"最大帧时间: {maxFrameTime * 1000f:F2}ms\n" +
                     $"歌词行创建: {totalLineCreations}\n" +
                     $"歌词行清理: {totalLineCleanups}\n" +
                     $"字符创建: {totalCharacterCreations}\n" +
                     $"字符回收: {totalCharacterRecycles}\n" +
                     $"当前内存使用: {currentMemory:F2}MB");
        }

        /// <summary>
        /// 获取当前性能统计
        /// </summary>
        /// <returns>性能统计字符串</returns>
        public string GetCurrentStats()
        {
            if (performanceRecords.Count == 0)
                return "暂无性能数据";

            var latestRecord = performanceRecords.ToArray()[performanceRecords.Count - 1];
            return $"帧时间: {latestRecord.frameTime * 1000f:F2}ms, " +
                   $"FPS: {1f / latestRecord.frameTime:F1}, " +
                   $"内存: {latestRecord.memoryUsage:F2}MB";
        }

        /// <summary>
        /// 启用/禁用性能监控
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void SetMonitoringEnabled(bool enable)
        {
            enableMonitoring = enable;
            Debug.Log($"[性能监控] 监控状态: {(enable ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 清除性能记录
        /// </summary>
        public void ClearRecords()
        {
            performanceRecords.Clear();
            Debug.Log("[性能监控] 性能记录已清除");
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }

    /// <summary>
    /// 性能记录数据结构
    /// </summary>
    [Serializable]
    public struct PerformanceRecord
    {
        public float timestamp;
        public float frameTime;
        public int lineCreations;
        public int lineCleanups;
        public int characterCreations;
        public int characterRecycles;
        public float memoryUsage;
    }
}