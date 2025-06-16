using System;
using System.IO;
using UnityEngine;

namespace LyricFX.Core
{
    /// <summary>
    /// 日志工具，用于将调试信息输出至文件
    /// </summary>
    public static class LyricLogger
    {
        private static readonly string LogFilePath = Path.Combine(Application.persistentDataPath, "LyricFX_Log.txt");
        private static bool _initialized = false;
        
        /// <summary>
        /// 初始化日志文件
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // 创建或清空日志文件
                File.WriteAllText(LogFilePath, $"=== LyricFX Log ===\n开始时间: {DateTime.Now}\n\n");
                _initialized = true;
                
                Debug.Log($"LyricFX日志文件初始化完成: {LogFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"无法创建日志文件: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 记录调试信息
        /// </summary>
        public static void Log(string message)
        {
            if (!_initialized)
            {
                Initialize();
            }
            
            try
            {
                string entry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                File.AppendAllText(LogFilePath, entry + "\n");
                
                // 同时输出到Unity控制台
                Debug.Log($"[LyricFX] {message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入日志文件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 记录错误信息
        /// </summary>
        public static void LogError(string message)
        {
            if (!_initialized)
            {
                Initialize();
            }
            
            try
            {
                string entry = $"[{DateTime.Now:HH:mm:ss.fff}] [错误] {message}";
                File.AppendAllText(LogFilePath, entry + "\n");
                
                // 同时输出到Unity控制台
                Debug.LogError($"[LyricFX] {message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入日志文件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 记录状态转换信息
        /// </summary>
        public static void LogStateTransition(string objectName, string oldState, string newState, bool isActive)
        {
            Log($"状态转换 - {objectName}: {oldState} -> {newState}, Active={isActive}");
        }
        
        /// <summary>
        /// 记录字符激活信息
        /// </summary>
        public static void LogCharacterActivation(int lineIndex, int charIndex, bool activated)
        {
            Log($"字符激活 - 行{lineIndex}, 字符{charIndex}: Activated={activated}");
        }
    }
}
