#if UNITY_EDITOR || UNITY_STANDALONE
#define ENABLE_FILE_LOGGING
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LyricFX.Utils
{
    /// <summary>
    /// 歌词特效调试工具 - 记录各阶段的时间消耗用于性能分析
    /// </summary>
    public class LyricFXDebugger
    {
        private static LyricFXDebugger _instance;
        
        /// <summary>
        /// 调试器实例（单例模式）
        /// </summary>
        public static LyricFXDebugger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LyricFXDebugger();
                }
                return _instance;
            }
        }
        
        // 是否启用调试输出
        public bool EnableDebug { get; set; } = false;
        
        // 日志文件路径
        private string logFilePath;
        
        // 会话开始时间
        private float sessionStartTime;
        
        // 记录的时间点信息
        private Dictionary<string, float> timePoints = new Dictionary<string, float>();
        
        // 标记开始播放的行
        private Dictionary<int, float> lineStartTimes = new Dictionary<int, float>();
        
        // 当前正在记录的歌词文本
        private string currentLyric = "";
        
        // 构造函数
        private LyricFXDebugger()
        {
            InitLogFile();
        }
        
        /// <summary>
        /// 初始化日志文件
        /// </summary>
        private void InitLogFile()
        {
#if ENABLE_FILE_LOGGING
            try
            {
                // 检查平台是否支持文件操作
                if (!IsFileOperationSupported())
                {
                    Debug.LogWarning("[LyricFXDebugger] 当前平台不支持文件日志记录，仅使用控制台输出");
                    return;
                }
                
                // 使用持久化数据路径
                string directory = Path.Combine(Application.persistentDataPath, "LyricFXLogs");
                
                // 确保目录存在
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 创建带时间戳的日志文件名
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                logFilePath = Path.Combine(directory, $"LyricFX_Log_{timestamp}.txt");
                
                // 创建日志文件头
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine($"LyricFX 调试日志 - 会话开始: {DateTime.Now}");
                sb.AppendLine($"平台: {Application.platform}");
                sb.AppendLine($"设备型号: {SystemInfo.deviceModel}");
                sb.AppendLine("==================================================");
                
                // 写入文件
                File.WriteAllText(logFilePath, sb.ToString());
                
                Debug.Log($"[LyricFXDebugger] 调试日志初始化完成，文件路径: {logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LyricFXDebugger] 无法初始化日志文件: {ex.Message}");
                logFilePath = null; // 确保后续不会尝试写入文件
            }
#else
            Debug.Log("[LyricFXDebugger] 移动平台模式，仅使用控制台日志输出");
#endif
        }
        
        /// <summary>
        /// 开始新的调试会话
        /// </summary>
        /// <param name="sessionName">会话名称</param>
        public void StartSession(string sessionName = "未命名会话")
        {
            if (!EnableDebug) return;
            
            sessionStartTime = Time.realtimeSinceStartup;
            timePoints.Clear();
            lineStartTimes.Clear();
            
            // 记录日志
            string msg = $"\n\n[会话开始] {sessionName} - 时间: {DateTime.Now}";
            AppendToLog(msg);
            
            // 记录开始时间点
            RecordTimePoint("SESSION_START");
        }
        
        /// <summary>
        /// 设置当前歌词文本
        /// </summary>
        /// <param name="lyric">歌词文本</param>
        public void SetCurrentLyric(string lyric)
        {
            if (!EnableDebug) return;
            currentLyric = lyric;
            AppendToLog($"\n[歌词] {lyric}");
        }
        
        /// <summary>
        /// 记录时间点
        /// </summary>
        /// <param name="pointName">时间点名称</param>
        public void RecordTimePoint(string pointName)
        {
            if (!EnableDebug) return;
            
            float currentTime = Time.realtimeSinceStartup;
            float elapsedFromStart = currentTime - sessionStartTime;
            
            timePoints[pointName] = currentTime;
            
            string msg = $"[时间点] {pointName}: {elapsedFromStart:F4}s";
            AppendToLog(msg);
        }
        
        /// <summary>
        /// 记录歌词行开始播放时间
        /// </summary>
        /// <param name="lineId">行ID</param>
        /// <param name="timestamp">LRC时间戳</param>
        /// <param name="actualTime">实际开始时间</param>
        public void RecordLineStart(int lineId, float timestamp, float actualTime)
        {
            if (!EnableDebug) return;
            
            lineStartTimes[lineId] = actualTime;
            float timeSinceSessionStart = actualTime - sessionStartTime;
            
            string msg = $"[行开始] ID: {lineId}, LRC时间戳: {timestamp:F3}s, 实际时间: {timeSinceSessionStart:F3}s, 差异: {(timeSinceSessionStart - timestamp):F3}s";
            AppendToLog(msg);
        }
        
        /// <summary>
        /// 记录阶段时间消耗
        /// </summary>
        /// <param name="fromPoint">起始点名称</param>
        /// <param name="toPoint">结束点名称</param>
        /// <param name="stageName">阶段名称</param>
        public void RecordStageDuration(string fromPoint, string toPoint, string stageName)
        {
            if (!EnableDebug) return;
            
            if (timePoints.ContainsKey(fromPoint) && timePoints.ContainsKey(toPoint))
            {
                float duration = timePoints[toPoint] - timePoints[fromPoint];
                string msg = $"[阶段耗时] {stageName}: {duration:F4}s ({fromPoint} -> {toPoint})";
                AppendToLog(msg);
            }
            else
            {
                AppendToLog($"[错误] 无法计算阶段耗时 {stageName}，缺少时间点: {fromPoint} 或 {toPoint}");
            }
        }
        
        /// <summary>
        /// 记录效果配置信息
        /// </summary>
        /// <param name="effectId">效果ID</param>
        /// <param name="availableDuration">可用时间</param>
        /// <param name="configInfo">配置信息</param>
        public void RecordEffectConfig(string effectId, float availableDuration, string configInfo)
        {
            if (!EnableDebug) return;
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[效果配置] ID: {effectId}, 可用时间: {availableDuration:F3}s");
            sb.AppendLine(configInfo);
            
            AppendToLog(sb.ToString());
        }
        
        /// <summary>
        /// 记录错误或异常
        /// </summary>
        /// <param name="errorMsg">错误信息</param>
        /// <param name="exception">异常对象（可选）</param>
        public void RecordError(string errorMsg, Exception exception = null)
        {
            if (!EnableDebug) return;
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[错误] {errorMsg}");
            
            if (exception != null)
            {
                sb.AppendLine($"异常: {exception.Message}");
                sb.AppendLine($"堆栈: {exception.StackTrace}");
            }
            
            AppendToLog(sb.ToString());
        }
        
        /// <summary>
        /// 结束调试会话
        /// </summary>
        /// <param name="summary">会话总结（可选）</param>
        public void EndSession(string summary = null)
        {
            if (!EnableDebug) return;
            
            // 记录结束时间点
            RecordTimePoint("SESSION_END");
            
            // 计算总耗时
            float totalDuration = timePoints["SESSION_END"] - timePoints["SESSION_START"];
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n[会话结束]");
            sb.AppendLine($"总耗时: {totalDuration:F4}s");
            
            if (!string.IsNullOrEmpty(summary))
            {
                sb.AppendLine($"总结: {summary}");
            }
            
            AppendToLog(sb.ToString());
        }
        
        /// <summary>
        /// 检查当前平台是否支持文件操作
        /// </summary>
        /// <returns>是否支持文件操作</returns>
        private bool IsFileOperationSupported()
        {
            // 在某些移动平台或受限环境中，文件操作可能受限
            try
            {
                // 尝试访问持久化数据路径
                string testPath = Application.persistentDataPath;
                return !string.IsNullOrEmpty(testPath) && Directory.Exists(Path.GetDirectoryName(testPath));
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 追加内容到日志文件
        /// </summary>
        /// <param name="content">日志内容</param>
        private void AppendToLog(string content)
        {
            // 始终输出到控制台，便于移动端调试
            Debug.Log($"[LyricFXDebugger] {content}");
            
#if ENABLE_FILE_LOGGING
            // 仅在支持的平台上写入文件
            try
            {
                if (!string.IsNullOrEmpty(logFilePath) && File.Exists(Path.GetDirectoryName(logFilePath)))
                {
                    using (StreamWriter writer = File.AppendText(logFilePath))
                    {
                        writer.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {content}");
                        writer.Flush(); // 确保立即写入，防止应用崩溃时丢失日志
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LyricFXDebugger] 写入日志文件失败: {ex.Message}");
                // 文件写入失败时，清空路径避免后续重复尝试
                logFilePath = null;
            }
#endif
        }
    }
}
