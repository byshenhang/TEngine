using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace MikeNspired.VRConsoleLog
{
    /// <summary>
    /// 日志消息对象池管理类，负责高效管理和复用日志消息UI对象
    /// Log message object pool manager, responsible for efficiently managing and reusing log message UI objects
    /// 
    /// 主要功能 / Main Features:
    /// - 对象池管理：避免频繁创建和销毁UI对象 / Object pool management: Avoids frequent creation and destruction of UI objects
    /// - UI重建循环保护：使用协程延迟UI操作，避免Canvas重建冲突 / UI rebuild loop protection: Uses coroutines to defer UI operations, avoiding Canvas rebuild conflicts
    /// - 动态大小调整：根据需要自动调整池大小 / Dynamic size adjustment: Automatically adjusts pool size as needed
    /// - 内存优化：通过复用减少GC压力 / Memory optimization: Reduces GC pressure through reuse
    /// </summary>
    public class LogPool
    {
        private VRConsoleLogger vrConsoleLogger;
        internal Queue<LogMessage> _logMessagePool = new();
        private bool _poolInitialized = false;
        public int _activeMessageCount = 0;

        public LogPool(VRConsoleLogger vrConsoleLogger) => this.vrConsoleLogger = vrConsoleLogger;

        /// <summary>
        /// 初始化日志消息对象池
        /// Initializes the log message pool
        /// </summary>
        /// <param name="rebuildUIAction">UI重建回调动作 / UI rebuild callback action</param>
        internal void InitializeLogMessagePool(Action rebuildUIAction)
        {
            if (_poolInitialized)
                return;

            UnityAction unityAction = new UnityAction(rebuildUIAction);

            // First, add any existing log messages already in the parent to the pool.
            foreach (Transform child in vrConsoleLogger.LogMessageParent)
            {
                LogMessage existingMessage = child.GetComponent<LogMessage>();
                if (existingMessage != null)
                {
                    // Use coroutine to safely deactivate existing messages
                    vrConsoleLogger.StartCoroutine(SafeDeactivateMessage(existingMessage, unityAction));
                }
            }

            // Calculate how many new messages need to be instantiated.
            int additionalNeeded = vrConsoleLogger.MaxMessageCount - _logMessagePool.Count;

            for (int i = 0; i < additionalNeeded; i++)
            {
                var prefab = Object.Instantiate(vrConsoleLogger.LogMessagePrefab.gameObject, vrConsoleLogger.LogMessageParent);
                LogMessage messageInstance = prefab.GetComponent<LogMessage>();
                // New instances start inactive, so no need for coroutine here
                messageInstance.gameObject.SetActive(false);
                messageInstance.Button.onClick.AddListener(unityAction); // Subscribe here
                _logMessagePool.Enqueue(messageInstance);
            }
            _poolInitialized = true;
        }
        
        /// <summary>
        /// 安全地停用消息对象的协程
        /// Coroutine to safely deactivate message objects
        /// </summary>
        /// <param name="message">要停用的消息 / Message to deactivate</param>
        /// <param name="unityAction">要添加的Unity动作 / Unity action to add</param>
        /// <returns>协程枚举器 / Coroutine enumerator</returns>
        private System.Collections.IEnumerator SafeDeactivateMessage(LogMessage message, UnityAction unityAction)
        {
            yield return new WaitForEndOfFrame();
            
            if (message != null)
            {
                message.gameObject.SetActive(false);
                message.Button.onClick.AddListener(unityAction);
                _logMessagePool.Enqueue(message);
            }
        }

        /// <summary>
        /// 从对象池中获取日志消息对象
        /// Gets a log message from the pool
        /// </summary>
        /// <returns>可用的日志消息对象 / Available log message object</returns>
        internal LogMessage GetLogMessageFromPool()
        {
            LogMessage message;
            if (_logMessagePool.Count == 0)
            {
                // Recycle the oldest active message if the pool is empty
                message = vrConsoleLogger.LogMessageParent.GetChild(0).GetComponent<LogMessage>();
                // Use the coroutine method to avoid UI rebuild conflicts
                vrConsoleLogger.StartCoroutine(ReturnLogMessageToPoolCoroutine(message));
                _logMessagePool.Enqueue(message); // Then enqueue it
            }

            // Dequeue the next available message from the pool
            message = _logMessagePool.Dequeue();
            
            // Defer the SetActive(true) call to avoid UI rebuild loop issues
            vrConsoleLogger.StartCoroutine(ActivateLogMessageCoroutine(message));
            
            return message;
        }
        
        /// <summary>
        /// 延迟激活日志消息的协程，避免UI重建循环冲突
        /// Coroutine to defer log message activation to avoid UI rebuild conflicts
        /// </summary>
        /// <param name="message">要激活的日志消息 / Log message to activate</param>
        /// <returns>协程枚举器 / Coroutine enumerator</returns>
        private System.Collections.IEnumerator ActivateLogMessageCoroutine(LogMessage message)
        {
            // Wait until the end of frame to avoid Canvas rebuild conflicts
            yield return new WaitForEndOfFrame();
            
            if (message != null)
            {
                message.gameObject.SetActive(true);
                message.isActive = true;
                message.transform.SetAsLastSibling();
                _activeMessageCount++; // Increment active count as this message is now active
            }
        }

        /// <summary>
        /// 将日志消息返回到对象池中
        /// Returns a log message to the pool
        /// </summary>
        /// <param name="message">要返回的日志消息 / Log message to return</param>
        internal void ReturnLogMessageToPool(LogMessage message)
        {
            if (message == null) return;
            
            // Defer the SetActive(false) call to avoid UI rebuild loop issues
            vrConsoleLogger.StartCoroutine(ReturnLogMessageToPoolCoroutine(message));
        }
        
        /// <summary>
        /// 延迟将日志消息返回到池中的协程，避免UI重建循环冲突
        /// Coroutine to defer returning log message to pool to avoid UI rebuild conflicts
        /// </summary>
        /// <param name="message">要返回的日志消息 / Log message to return</param>
        /// <returns>协程枚举器 / Coroutine enumerator</returns>
        private System.Collections.IEnumerator ReturnLogMessageToPoolCoroutine(LogMessage message)
        {
            // Wait until the end of frame to avoid Canvas rebuild conflicts
            yield return new WaitForEndOfFrame();
            
            if (message != null)
            {
                message.gameObject.SetActive(false);
                message.isActive = false;
                _logMessagePool.Enqueue(message);
                _activeMessageCount--;
            }
        }

        /// <summary>
        /// 调整对象池大小以匹配最大消息数量
        /// Adjusts the pool size to match the maximum message count
        /// </summary>
        internal void AdjustPoolSize()
        {
            int currentActiveAndPooled = _logMessagePool.Count + _activeMessageCount;
        
            // If we have more messages than needed
            if (currentActiveAndPooled > vrConsoleLogger.MaxMessageCount)
            {
                int excess = currentActiveAndPooled - vrConsoleLogger.MaxMessageCount;
                RemoveExcessMessages(excess);
            }
            else if (currentActiveAndPooled < vrConsoleLogger.MaxMessageCount)
            {
                // Add messages if we have fewer than needed
                AddMessagesToPool(vrConsoleLogger.MaxMessageCount - currentActiveAndPooled);
            }
        }

        /// <summary>
        /// 移除多余的消息对象
        /// Removes excess message objects
        /// </summary>
        /// <param name="excess">要移除的多余数量 / Number of excess messages to remove</param>
        private void RemoveExcessMessages(int excess)
        {
            // Start by removing inactive messages from the pool
            while (excess > 0 && _logMessagePool.Count > 0)
            {
                var messageToRemove = _logMessagePool.Dequeue();
                Object.Destroy(messageToRemove.gameObject);
                excess--;
            }

            // Remove active messages if still necessary
            while (excess > 0 && _activeMessageCount > 0)
            {
                LogMessage messageToRemove = FindLastActiveMessage();
                if (messageToRemove != null)
                {
                    // Use coroutine to safely deactivate and destroy the message
                    vrConsoleLogger.StartCoroutine(SafeRemoveActiveMessage(messageToRemove));
                    _activeMessageCount--;
                    excess--;
                }
            }
        }
        
        /// <summary>
        /// 安全地移除活跃消息的协程
        /// Coroutine to safely remove active messages
        /// </summary>
        /// <param name="message">要移除的消息 / Message to remove</param>
        /// <returns>协程枚举器 / Coroutine enumerator</returns>
        private System.Collections.IEnumerator SafeRemoveActiveMessage(LogMessage message)
        {
            yield return new WaitForEndOfFrame();
            
            if (message != null)
            {
                message.gameObject.SetActive(false);
                message.Button.onClick.RemoveAllListeners();
                Object.Destroy(message.gameObject);
            }
        }

        /// <summary>
        /// 查找最后一个活跃的消息对象
        /// Finds the last active message object
        /// </summary>
        /// <returns>最后一个活跃的消息对象，如果没有则返回null / Last active message object, or null if none found</returns>
        private LogMessage FindLastActiveMessage()
        {
            for (int i = vrConsoleLogger.LogMessageParent.childCount - 1; i >= 0; i--)
            {
                LogMessage potentialMessage = vrConsoleLogger.LogMessageParent.GetChild(i).GetComponent<LogMessage>();
                if (potentialMessage != null && potentialMessage.gameObject.activeSelf)
                {
                    return potentialMessage;
                }
            }
            return null;
        }

        /// <summary>
        /// 向对象池添加指定数量的消息对象
        /// Adds a specified number of message objects to the pool
        /// </summary>
        /// <param name="count">要添加的消息数量 / Number of messages to add</param>
        private void AddMessagesToPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = (GameObject)GameObject.Instantiate((Object)vrConsoleLogger.LogMessagePrefab, vrConsoleLogger.LogMessageParent);
                LogMessage newMessage = prefab.GetComponent<LogMessage>();
                newMessage.gameObject.SetActive(false);
                _logMessagePool.Enqueue(newMessage);
            }
        }
    }
}