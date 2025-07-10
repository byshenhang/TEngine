using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.VRConsoleLog
{
    public class LogManager : MonoBehaviour
    {
        public static LogManager Instance { get; private set; }

        public event UnityAction<LogData> OnLogAdded;
        public List<LogData> Logs { get; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable() => Application.logMessageReceived += HandleUnityLog;

        private void OnDisable() => Application.logMessageReceived -= HandleUnityLog;

        private void HandleUnityLog(string logString, string stackTrace, LogType type) => Log(logString, stackTrace, type);

        private void Log(string message, string stackTrace, LogType type)
        {
            LogData data = new LogData
            {
                Message = message,
                StackTrace = stackTrace,
                Type = type,
                TimeStamp = DateTime.Now,
                GameTime = Time.time.ToString("f3") // Stores game time in seconds since the game started
            };

            Logs.Add(data);
            OnLogAdded?.Invoke(data);
        }

        public void Clear() => Logs.Clear();
    }


    [Serializable]
    public class LogData
    {
        public string Message;
        public string StackTrace;
        public LogType Type;
        public DateTime TimeStamp; // Real-world time
        public string GameTime; // Game time
    }
}
