using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// XR交互事件总线 - 集中管理所有VR交互事件的分发
    /// </summary>
    public sealed class XRInteractionEventBus : Singleton<XRInteractionEventBus>
    {
        // 使用类型安全的事件系统
        private readonly Dictionary<string, Dictionary<Type, List<Delegate>>> _eventHandlers = new Dictionary<string, Dictionary<Type, List<Delegate>>>();
        
        // 场景隔离的事件频道
        private readonly HashSet<string> _activeChannels = new HashSet<string>();
        
        /// <summary>
        /// 注册事件处理方法
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理委托</param>
        /// <param name="channel">事件频道，默认为全局</param>
        public void Subscribe<T>(Action<T> handler, string channel = "global")
        {
            if (handler == null) return;
            
            if (!_eventHandlers.TryGetValue(channel, out var channelHandlers))
            {
                channelHandlers = new Dictionary<Type, List<Delegate>>();
                _eventHandlers[channel] = channelHandlers;
            }
            
            var eventType = typeof(T);
            if (!channelHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                channelHandlers[eventType] = handlers;
            }
            
            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
                Log.Info($"已注册事件处理器 {eventType.Name} 到频道 {channel}");
            }
        }
        
        /// <summary>
        /// 注销事件处理方法
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理委托</param>
        /// <param name="channel">事件频道，默认为全局</param>
        public void Unsubscribe<T>(Action<T> handler, string channel = "global")
        {
            if (handler == null) return;
            
            if (_eventHandlers.TryGetValue(channel, out var channelHandlers))
            {
                var eventType = typeof(T);
                if (channelHandlers.TryGetValue(eventType, out var handlers))
                {
                    if (handlers.Remove(handler))
                    {
                        Log.Info($"已注销事件处理器 {eventType.Name} 从频道 {channel}");
                    }
                    
                    // 如果没有处理器了，清理
                    if (handlers.Count == 0)
                    {
                        channelHandlers.Remove(eventType);
                    }
                }
                
                // 如果频道没有处理器了，清理
                if (channelHandlers.Count == 0)
                {
                    _eventHandlers.Remove(channel);
                }
            }
        }
        
        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <param name="channel">事件频道，默认为全局</param>
        public void Publish<T>(T eventData, string channel = "global")
        {
            // 首先处理特定频道
            PublishToChannel(eventData, channel);
            
            // 然后处理全局频道(如果不是已经在处理全局频道)
            if (channel != "global")
            {
                PublishToChannel(eventData, "global");
            }
        }
        
        // 向特定频道发布事件
        private void PublishToChannel<T>(T eventData, string channel)
        {
            if (_eventHandlers.TryGetValue(channel, out var channelHandlers))
            {
                var eventType = typeof(T);
                if (channelHandlers.TryGetValue(eventType, out var handlers))
                {
                    // 创建一个副本，防止在调用过程中修改集合
                    var handlersCopy = new List<Delegate>(handlers);
                    foreach (var handler in handlersCopy)
                    {
                        try
                        {
                            ((Action<T>)handler)(eventData);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"处理事件 {eventType.Name} 时出错: {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 注册场景频道
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void RegisterSceneChannel(string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName) && !_activeChannels.Contains(sceneName))
            {
                _activeChannels.Add(sceneName);
                Log.Info($"已注册场景频道: {sceneName}");
            }
        }
        
        /// <summary>
        /// 注销场景频道
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void UnregisterSceneChannel(string sceneName)
        {
            if (!string.IsNullOrEmpty(sceneName) && _activeChannels.Contains(sceneName))
            {
                _activeChannels.Remove(sceneName);
                Log.Info($"已注销场景频道: {sceneName}");
            }
        }
        
        /// <summary>
        /// 清理所有事件
        /// </summary>
        public void Clear()
        {
            _eventHandlers.Clear();
            _activeChannels.Clear();
            Log.Info("已清理所有交互事件订阅");
        }
    }
}
