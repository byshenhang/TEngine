using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;
using GameLogic.AudioReactor.Adapters;

namespace GameLogic.AudioReactor
{
    /// <summary>
    /// 音频反应器管理器
    /// 统一管理多个音频反应插件，提供插件注册、发现、控制和音频源分发功能
    /// 采用单例模式，确保全局唯一的音频反应器管理入口
    /// </summary>
    public class AudioReactorManager : Singleton<AudioReactorManager>, IUpdate
    {
        // 注册的音频反应器字典
        private readonly Dictionary<string, IAudioReactor> _registeredReactors = new Dictionary<string, IAudioReactor>();
        
        // 当前全局音频源
        private AudioSource _globalAudioSource;
        
        // 管理器状态
        private bool _isInitialized = false;
        private bool _globalEnabled = true;
        private bool _enableDebugger = false;
        
        // 自动发现配置
        private bool _autoDiscoveryEnabled = true;
        private float _discoveryInterval = 5f; // 自动发现间隔（秒）
        private float _lastDiscoveryTime;
        
        // 事件
        public event Action<IAudioReactor> OnReactorRegistered;
        public event Action<IAudioReactor> OnReactorUnregistered;
        public event Action<IAudioReactor, AudioReactorState> OnReactorStateChanged;
        public event Action<bool> OnGlobalStateChanged;
        public event Action<AudioSource> OnGlobalAudioSourceChanged;
        
        protected override void OnInit()
        {
            base.OnInit();
            
            // 订阅反应器事件
            SubscribeToReactorEvents();
            
            _isInitialized = true;
            
            Log.Info("AudioReactorManager: 管理器已初始化");
        }
        
        public override void Release()
        {
            base.Release();
            
            // 释放所有注册的反应器
            ReleaseAllReactorsAsync().Forget();
            
            // 清理事件订阅
            UnsubscribeFromReactorEvents();
            
            _registeredReactors.Clear();
            _globalAudioSource = null;
            _isInitialized = false;
            
            Log.Info("AudioReactorManager: 管理器已释放");
        }
        
        public void OnUpdate()
        {
            // 自动发现新的音频反应器
            if (_autoDiscoveryEnabled && Time.time - _lastDiscoveryTime >= _discoveryInterval)
            {
                AutoDiscoverReactorsAsync().Forget();
                _lastDiscoveryTime = Time.time;
            }
        }
        
        /// <summary>
        /// 启用或禁用调试模式
        /// </summary>
        /// <param name="enable">是否启用调试</param>
        public void EnableDebugger(bool enable)
        {
            _enableDebugger = enable;
            
            if (enable)
            {
                Log.Info("AudioReactorManager: 调试模式已启用");
            }
            else
            {
                Log.Info("AudioReactorManager: 调试模式已禁用");
            }
        }
        
        /// <summary>
        /// 设置自动发现配置
        /// </summary>
        /// <param name="enabled">是否启用自动发现</param>
        /// <param name="interval">发现间隔（秒）</param>
        public void SetAutoDiscovery(bool enabled, float interval = 5f)
        {
            _autoDiscoveryEnabled = enabled;
            _discoveryInterval = interval;
            
            if (_enableDebugger)
            {
                Log.Info($"AudioReactorManager: 自动发现设置 - 启用: {enabled}, 间隔: {interval}秒");
            }
        }
        
        /// <summary>
        /// 自动发现并注册音频反应器
        /// </summary>
        /// <returns>发现并注册的反应器数量</returns>
        public async UniTask<int> AutoDiscoverReactorsAsync()
        {
            try
            {
                int discoveredCount = 0;
                
                // 发现AudioReactiveShaders适配器
                var arsAdapters = AudioReactiveShadersAdapter.DiscoverInScene();
                foreach (var adapter in arsAdapters)
                {
                    if (!_registeredReactors.ContainsKey(adapter.ReactorId))
                    {
                        await RegisterReactorAsync(adapter);
                        discoveredCount++;
                    }
                }
                
                // 发现AudioSyncPro适配器
                var aspAdapters = AudioSyncProAdapter.DiscoverInScene();
                foreach (var adapter in aspAdapters)
                {
                    if (!_registeredReactors.ContainsKey(adapter.ReactorId))
                    {
                        await RegisterReactorAsync(adapter);
                        discoveredCount++;
                    }
                }
                
                if (_enableDebugger && discoveredCount > 0)
                {
                    Log.Info($"AudioReactorManager: 自动发现并注册了 {discoveredCount} 个音频反应器");
                }
                
                return discoveredCount;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 自动发现失败: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// 注册音频反应器
        /// </summary>
        /// <param name="reactor">音频反应器实例</param>
        /// <returns>注册任务</returns>
        public async UniTask<bool> RegisterReactorAsync(IAudioReactor reactor)
        {
            try
            {
                if (reactor == null)
                {
                    Log.Error("AudioReactorManager: 尝试注册空的音频反应器");
                    return false;
                }
                
                if (_registeredReactors.ContainsKey(reactor.ReactorId))
                {
                    if (_enableDebugger)
                    {
                        Log.Warning($"AudioReactorManager: 音频反应器已存在  - {reactor.ReactorId}");
                    }
                    return false;
                }
                
                // 设置音频源
                bool setAudioSourceSuccess = await reactor.SetAudioSourceAsync(_globalAudioSource);
                if (!setAudioSourceSuccess)
                {
                    Log.Error($"AudioReactorManager: 设置音频源失败 - {reactor.ReactorId}");
                    return false;
                }
                
                // 初始化反应器
                bool initSuccess = await reactor.InitializeAsync();
                if (!initSuccess)
                {
                    Log.Error($"AudioReactorManager: 音频反应器初始化失败 - {reactor.ReactorId}");
                    return false;
                }
                
                // 注册反应器
                _registeredReactors[reactor.ReactorId] = reactor;
                
                // 订阅反应器事件
                reactor.OnStateChanged += HandleReactorStateChanged;
                reactor.OnError += HandleReactorError;
                
                // 如果全局启用，则启用该反应器
                if (_globalEnabled)
                {
                    await reactor.EnableAsync();
                }
                
                OnReactorRegistered?.Invoke(reactor);
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioReactorManager: 成功注册音频反应器 - {reactor.DisplayName} ({reactor.ReactorId})");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 注册音频反应器失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 注销音频反应器
        /// </summary>
        /// <param name="reactorId">反应器ID</param>
        /// <returns>注销任务</returns>
        public async UniTask<bool> UnregisterReactorAsync(string reactorId)
        {
            try
            {
                if (!_registeredReactors.TryGetValue(reactorId, out var reactor))
                {
                    if (_enableDebugger)
                    {
                        Log.Warning($"AudioReactorManager: 未找到要注销的音频反应器 - {reactorId}");
                    }
                    return false;
                }
                
                // 取消事件订阅
                reactor.OnStateChanged -= HandleReactorStateChanged;
                reactor.OnError -= HandleReactorError;
                
                // 释放反应器资源
                await reactor.ReleaseAsync();
                
                // 从注册表中移除
                _registeredReactors.Remove(reactorId);
                
                OnReactorUnregistered?.Invoke(reactor);
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioReactorManager: 成功注销音频反应器 - {reactor.DisplayName} ({reactorId})");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 注销音频反应器失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 设置全局音频源
        /// </summary>
        /// <param name="audioSource">音频源</param>
        /// <returns>设置任务</returns>
        public async UniTask<bool> SetGlobalAudioSourceAsync(AudioSource audioSource)
        {
            try
            {
                _globalAudioSource = audioSource;
                
                // 为所有注册的反应器设置音频源
                var tasks = _registeredReactors.Values.Select(reactor => reactor.SetAudioSourceAsync(audioSource)).ToArray();
                var results = await UniTask.WhenAll(tasks);
                
                bool allSuccess = results.All(result => result);
                
                OnGlobalAudioSourceChanged?.Invoke(audioSource);
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioReactorManager: 全局音频源已设置，应用到 {_registeredReactors.Count} 个反应器，成功率: {results.Count(r => r)}/{results.Length}");
                }
                
                return allSuccess;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 设置全局音频源失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 全局启用所有音频反应器
        /// </summary>
        /// <returns>启用任务</returns>
        public async UniTask<bool> EnableAllReactorsAsync()
        {
            try
            {
                _globalEnabled = true;
                
                var tasks = _registeredReactors.Values.Select(reactor => reactor.EnableAsync()).ToArray();
                var results = await UniTask.WhenAll(tasks);
                
                bool allSuccess = results.All(result => result);
                
                OnGlobalStateChanged?.Invoke(true);
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioReactorManager: 全局启用完成，成功率: {results.Count(r => r)}/{results.Length}");
                }
                
                return allSuccess;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 全局启用失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 全局禁用所有音频反应器
        /// </summary>
        /// <returns>禁用任务</returns>
        public async UniTask<bool> DisableAllReactorsAsync()
        {
            try
            {
                _globalEnabled = false;
                
                var tasks = _registeredReactors.Values.Select(reactor => reactor.DisableAsync()).ToArray();
                var results = await UniTask.WhenAll(tasks);
                
                bool allSuccess = results.All(result => result);
                
                OnGlobalStateChanged?.Invoke(false);
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioReactorManager: 全局禁用完成，成功率: {results.Count(r => r)}/{results.Length}");
                }
                
                return allSuccess;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 全局禁用失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 启用指定的音频反应器
        /// </summary>
        /// <param name="reactorId">反应器ID</param>
        /// <returns>启用任务</returns>
        public async UniTask<bool> EnableReactorAsync(string reactorId)
        {
            try
            {
                if (_registeredReactors.TryGetValue(reactorId, out var reactor))
                {
                    bool success = await reactor.EnableAsync();
                    
                    if (_enableDebugger)
                    {
                        Log.Info($"AudioReactorManager: 反应器 {reactor.DisplayName} 启用{(success ? "成功" : "失败")}");
                    }
                    
                    return success;
                }
                
                if (_enableDebugger)
                {
                    Log.Warning($"AudioReactorManager: 未找到要启用的反应器 - {reactorId}");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 启用反应器失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 禁用指定的音频反应器
        /// </summary>
        /// <param name="reactorId">反应器ID</param>
        /// <returns>禁用任务</returns>
        public async UniTask<bool> DisableReactorAsync(string reactorId)
        {
            try
            {
                if (_registeredReactors.TryGetValue(reactorId, out var reactor))
                {
                    bool success = await reactor.DisableAsync();
                    
                    if (_enableDebugger)
                    {
                        Log.Info($"AudioReactorManager: 反应器 {reactor.DisplayName} 禁用{(success ? "成功" : "失败")}");
                    }
                    
                    return success;
                }
                
                if (_enableDebugger)
                {
                    Log.Warning($"AudioReactorManager: 未找到要禁用的反应器 - {reactorId}");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 禁用反应器失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取所有注册的音频反应器信息
        /// </summary>
        /// <returns>反应器信息字典</returns>
        public Dictionary<string, (string displayName, string type, bool isEnabled)> GetRegisteredReactors()
        {
            var result = new Dictionary<string, (string, string, bool)>();
            
            foreach (var kvp in _registeredReactors)
            {
                var reactor = kvp.Value;
                result[kvp.Key] = (reactor.DisplayName, reactor.ReactorType, reactor.IsEnabled);
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取指定的音频反应器
        /// </summary>
        /// <param name="reactorId">反应器ID</param>
        /// <returns>音频反应器实例</returns>
        public IAudioReactor GetReactor(string reactorId)
        {
            _registeredReactors.TryGetValue(reactorId, out var reactor);
            return reactor;
        }
        
        /// <summary>
        /// 获取指定类型的所有音频反应器
        /// </summary>
        /// <param name="reactorType">反应器类型</param>
        /// <returns>音频反应器列表</returns>
        public List<IAudioReactor> GetReactorsByType(string reactorType)
        {
            return _registeredReactors.Values
                .Where(reactor => reactor.ReactorType.Equals(reactorType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        /// <summary>
        /// 获取当前全局音频源
        /// </summary>
        /// <returns>全局音频源</returns>
        public AudioSource GetGlobalAudioSource()
        {
            return _globalAudioSource;
        }
        
        /// <summary>
        /// 获取全局启用状态
        /// </summary>
        /// <returns>是否全局启用</returns>
        public bool IsGlobalEnabled()
        {
            return _globalEnabled;
        }
        
        /// <summary>
        /// 获取注册的反应器数量
        /// </summary>
        /// <returns>反应器数量</returns>
        public int GetReactorCount()
        {
            return _registeredReactors.Count;
        }
        
        /// <summary>
        /// 清理所有已注册的音频反应器 - 用于场景切换时的批量清理
        /// 这个方法会注销并释放所有当前注册的音频反应器
        /// </summary>
        /// <returns>清理任务，返回是否全部清理成功</returns>
        public async UniTask<bool> CleanupAllReactorsAsync()
        {
            try
            {
                if (_registeredReactors.Count == 0)
                {
                    if (_enableDebugger)
                    {
                        Log.Info("AudioReactorManager: 没有需要清理的音频反应器");
                    }
                    return true;
                }
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioReactorManager: 开始清理 {_registeredReactors.Count} 个音频反应器");
                }
                
                // 获取所有反应器ID的副本，避免在迭代过程中修改字典
                var reactorIds = _registeredReactors.Keys.ToList();
                
                // 创建注销任务列表
                var unregisterTasks = reactorIds.Select(id => UnregisterReactorAsync(id)).ToArray();
                
                // 等待所有注销任务完成
                var results = await UniTask.WhenAll(unregisterTasks);
                
                // 统计成功数量
                int successCount = results.Count(r => r);
                bool allSuccess = successCount == reactorIds.Count;
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioReactorManager: 音频反应器清理完成，成功清理 {successCount}/{reactorIds.Count} 个反应器");
                }
                
                return allSuccess;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 批量清理音频反应器失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 释放所有反应器资源
        /// </summary>
        /// <returns>释放任务</returns>
        private async UniTask ReleaseAllReactorsAsync()
        {
            try
            {
                var tasks = _registeredReactors.Values.Select(reactor => reactor.ReleaseAsync()).ToArray();
                await UniTask.WhenAll(tasks);
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioReactorManager: 已释放 {_registeredReactors.Count} 个音频反应器");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactorManager: 释放反应器资源失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 订阅反应器事件
        /// </summary>
        private void SubscribeToReactorEvents()
        {
            // 这里可以添加全局事件订阅逻辑
        }
        
        /// <summary>
        /// 取消订阅反应器事件
        /// </summary>
        private void UnsubscribeFromReactorEvents()
        {
            // 这里可以添加全局事件取消订阅逻辑
        }
        
        /// <summary>
        /// 处理反应器状态变化事件
        /// </summary>
        /// <param name="reactor">反应器实例</param>
        /// <param name="state">新状态</param>
        private void HandleReactorStateChanged(IAudioReactor reactor, AudioReactorState state)
        {
            OnReactorStateChanged?.Invoke(reactor, state);
            
            if (_enableDebugger)
            {
                Log.Info($"AudioReactorManager: 反应器 {reactor.DisplayName} 状态变更为 {state}");
            }
        }
        
        /// <summary>
        /// 处理反应器错误事件
        /// </summary>
        /// <param name="reactor">反应器实例</param>
        /// <param name="error">错误信息</param>
        private void HandleReactorError(IAudioReactor reactor, string error)
        {
            Log.Error($"AudioReactorManager: 反应器 {reactor.DisplayName} 发生错误: {error}");
        }
    }
}