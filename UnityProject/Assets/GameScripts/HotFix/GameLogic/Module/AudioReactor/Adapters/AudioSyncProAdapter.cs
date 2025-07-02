using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;
using System.Reflection;
using Object = UnityEngine.Object;

namespace GameLogic.AudioReactor.Adapters
{
    /// <summary>
    /// Audio Sync Pro 插件适配器
    /// 将 Audio Sync Pro 插件包装为统一的 IAudioReactor 接口
    /// 支持自动发现场景中的相关组件并提供统一的控制接口
    /// </summary>
    public class AudioSyncProAdapter : IAudioReactor
    {
        // 适配器标识
        private readonly string _reactorId;
        private readonly string _displayName;
        private const string REACTOR_TYPE = "AudioSyncPro";
        
        // Audio Sync Pro 相关组件
        private AudioSyncModule _audioSyncModule;
        private AudioSource _currentAudioSource;
        
        // 适配器状态
        private AudioReactorState _currentState = AudioReactorState.Uninitialized;
        private bool _isInitialized = false;
        private bool _isEnabled = false;
        
        private float _lastDataUpdateTime;
        private const float DATA_UPDATE_INTERVAL = 0.016f; // ~60fps
        
        // 参数存储
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        
        // 事件
        public event Action<IAudioReactor, AudioReactorState> OnStateChanged;
        public event Action<IAudioReactor, string> OnError;
        
        // IAudioReactor 属性实现
        public string ReactorId => _reactorId;
        public string DisplayName => _displayName;
        public string ReactorType => REACTOR_TYPE;
        public bool IsEnabled => _isEnabled;
        public AudioReactorState CurrentState => _currentState;
        public bool IsInitialized => _isInitialized;
        public AudioSource CurrentAudioSource => _currentAudioSource;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="audioSyncModule">Audio Sync Pro 模块实例</param>
        /// <param name="customName">自定义显示名称</param>
        public AudioSyncProAdapter(AudioSyncModule audioSyncModule, string customName = null)
        {
            _audioSyncModule = audioSyncModule ?? throw new ArgumentNullException(nameof(audioSyncModule));
            
            // 生成唯一标识
            _reactorId = $"AudioSyncPro_{audioSyncModule.GetHashCode()}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            _displayName = customName ?? $"Audio Sync Pro ({audioSyncModule.GetType().Name})";
            
            Log.Info($"AudioSyncProAdapter: 适配器已创建 - {_displayName}");
        }
        
        /// <summary>
        /// 在场景中自动发现 Audio Sync Pro 组件
        /// </summary>
        /// <returns>发现的适配器列表</returns>
        public static List<AudioSyncProAdapter> DiscoverInScene()
        {
            var adapters = new List<AudioSyncProAdapter>();
            
            try
            {
                // 获取AudioSyncModule单例实例
                var audioSyncModule = AudioSyncModule.Instance;
                
                if (audioSyncModule != null)
                {
                    // 尝试在场景中查找AudioReactor和AudioSourcePlus组件
                    var audioReactor = Object.FindObjectOfType<TelePresent.AudioSyncPro.AudioReactor>();
                    var audioSourcePlus = Object.FindObjectOfType<TelePresent.AudioSyncPro.AudioSourcePlus>();
                    
                    // 如果找到了必要的组件，初始化AudioSyncModule
                    if (audioReactor != null)
                    {
                        bool initSuccess = audioSyncModule.Initialize(audioReactor, audioSourcePlus);
                        if (initSuccess)
                        {
                            var adapter = new AudioSyncProAdapter(audioSyncModule);
                            adapters.Add(adapter);
                            Log.Info($"AudioSyncProAdapter: 成功初始化AudioSyncModule并创建适配器");
                        }
                        else
                        {
                            Log.Warning($"AudioSyncProAdapter: AudioSyncModule初始化失败");
                        }
                    }
                    else
                    {
                        Log.Warning($"AudioSyncProAdapter: 场景中未找到AudioReactor组件");
                    }
                }
                
                Log.Info($"AudioSyncProAdapter: 在场景中发现 {adapters.Count} 个 Audio Sync Pro 组件");
            }
            catch (Exception ex)
            {
                Log.Error($"AudioSyncProAdapter: 自动发现失败: {ex.Message}");
            }
            
            return adapters;
        }
        
        /// <summary>
        /// 异步初始化适配器
        /// </summary>
        /// <returns>初始化任务</returns>
        public async UniTask<bool> InitializeAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    Log.Warning($"AudioSyncProAdapter: 适配器已初始化 - {_displayName}");
                    return true;
                }
                
                ChangeState(AudioReactorState.Initializing);
                
                // 验证 Audio Sync Pro 模块
                if (_audioSyncModule == null)
                {
                    throw new InvalidOperationException("AudioSyncModule 引用为空");
                }
                
                // 如果当前有音频源，设置音频源
                if (_currentAudioSource != null)
                {
                    await SetAudioSourceAsync(_currentAudioSource);
                }
                
                // 初始化参数
                InitializeDefaultParameters();
                
                _isInitialized = true;
                ChangeState(AudioReactorState.Disabled);
                
                Log.Info($"AudioSyncProAdapter: 适配器初始化成功 - {_displayName}");
                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"初始化失败: {ex.Message}";
                Log.Error($"AudioSyncProAdapter: {errorMsg}");
                OnError?.Invoke(this, errorMsg);
                ChangeState(AudioReactorState.Error);
                return false;
            }
        }
        
        /// <summary>
        /// 异步启用适配器
        /// </summary>
        /// <returns>启用任务</returns>
        public async UniTask<bool> EnableAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    Log.Warning($"AudioSyncProAdapter: 适配器未初始化，无法启用 - {_displayName}");
                    return false;
                }
                
                if (_isEnabled)
                {
                    Log.Info($"AudioSyncProAdapter: 适配器已启用 - {_displayName}");
                    return true;
                }
                
                ChangeState(AudioReactorState.Enabling);
                
                // 启用 Audio Sync Pro 模块
                if (_audioSyncModule != null)
                {
                    // 如果有音频源，确保模块使用正确的音频源
                    if (_currentAudioSource != null)
                    {
                        // 使用反射安全地设置音频源
                        try
                        {
                            var setAudioSourceMethod = _audioSyncModule.GetType().GetMethod("SetAudioSource");
                            setAudioSourceMethod?.Invoke(_audioSyncModule, new object[] { _currentAudioSource });
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"AudioSyncProAdapter: 设置音频源失败: {ex.Message}");
                        }
                    }
                }
                
                _isEnabled = true;
                ChangeState(AudioReactorState.Enabled);
                
                Log.Info($"AudioSyncProAdapter: 适配器已启用 - {_displayName}");
                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"启用失败: {ex.Message}";
                Log.Error($"AudioSyncProAdapter: {errorMsg}");
                OnError?.Invoke(this, errorMsg);
                ChangeState(AudioReactorState.Error);
                return false;
            }
        }
        
        /// <summary>
        /// 异步禁用适配器
        /// </summary>
        /// <returns>禁用任务</returns>
        public async UniTask<bool> DisableAsync()
        {
            try
            {
                if (!_isEnabled)
                {
                    Log.Info($"AudioSyncProAdapter: 适配器已禁用 - {_displayName}");
                    return true;
                }
                
                ChangeState(AudioReactorState.Disabling);
                
                // 禁用 Audio Sync Pro 模块
                if (_audioSyncModule != null)
                {
                    // 使用反射安全地调用停止方法
                    try
                    {
                        var stopMethod = _audioSyncModule.GetType().GetMethod("Stop");
                        stopMethod?.Invoke(_audioSyncModule, null);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"AudioSyncProAdapter: 停止模块失败: {ex.Message}");
                    }
                }
                
                _isEnabled = false;
                ChangeState(AudioReactorState.Disabled);
                
                Log.Info($"AudioSyncProAdapter: 适配器已禁用 - {_displayName}");
                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"禁用失败: {ex.Message}";
                Log.Error($"AudioSyncProAdapter: {errorMsg}");
                OnError?.Invoke(this, errorMsg);
                ChangeState(AudioReactorState.Error);
                return false;
            }
        }
        
        /// <summary>
        /// 异步设置音频源
        /// </summary>
        /// <param name="audioSource">音频源</param>
        /// <returns>设置任务</returns>
        public async UniTask<bool> SetAudioSourceAsync(AudioSource audioSource)
        {
            try
            {
                _currentAudioSource = audioSource;
                
                // 如果适配器已启用，立即应用音频源设置
                if (_isEnabled && _audioSyncModule != null)
                {
                    // 使用反射安全地设置音频源
                    try
                    {
                        var setAudioSourceMethod = _audioSyncModule.GetType().GetMethod("SetAudioSource");
                        setAudioSourceMethod?.Invoke(_audioSyncModule, new object[] { audioSource });
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"AudioSyncProAdapter: 设置音频源失败: {ex.Message}");
                    }
                }
                
                Log.Info($"AudioSyncProAdapter: 音频源已设置 - {_displayName}");
                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"设置音频源失败: {ex.Message}";
                Log.Error($"AudioSyncProAdapter: {errorMsg}");
                OnError?.Invoke(this, errorMsg);
                return false;
            }
        }
        
        
        /// </summary>
        /// <param name="key">参数键</param>
        /// <param name="value">参数值</param>
        /// <returns>设置任务</returns>
        public async UniTask<bool> SetParameterAsync(string key, object value)
        {
            try
            {
                _parameters[key] = value;
                // 根据参数键应用到 AudioSyncModule
                await ApplyParameterToModule(key, value);
                
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioSyncProAdapter: 设置参数失败 - {key}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 异步获取参数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        /// <returns>参数值</returns>
        public async UniTask<T> GetParameterAsync<T>(string parameterName)
        {
            if (_parameters.TryGetValue(parameterName, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            
            return default(T);
        }
        
        /// <summary>
        /// 获取所有可用参数名称
        /// </summary>
        /// <returns>参数名称列表</returns>
        public async UniTask<List<string>> GetAvailableParametersAsync()
        {
            return new List<string>(_parameters.Keys);
        }
        
        /// <summary>
        /// 获取反应器详细信息
        /// </summary>
        /// <returns>详细信息字典</returns>
        public async UniTask<Dictionary<string, object>> GetInfoAsync()
        {
            var info = new Dictionary<string, object>
            {
                ["ReactorId"] = ReactorId,
                ["DisplayName"] = DisplayName,
                ["ReactorType"] = ReactorType,
                ["IsEnabled"] = IsEnabled,
                ["IsInitialized"] = IsInitialized,
                ["CurrentState"] = CurrentState.ToString(),
                ["HasAudioSource"] = CurrentAudioSource != null,
                ["AudioSourceName"] = CurrentAudioSource?.name ?? "None",
                ["ModuleType"] = _audioSyncModule?.GetType().Name ?? "None",
                ["ParameterCount"] = _parameters.Count
            };
            
            return info;
        }
        
        /// <summary>
        /// 检查是否支持特定功能
        /// </summary>
        /// <param name="featureName">功能名称</param>
        /// <returns>是否支持</returns>
        public bool SupportsFeature(string featureName)
        {
            switch (featureName.ToLower())
            {
                case "spectrum_analysis":
                case "frequency_bands":
                case "rms_analysis":
                case "dynamic_bands":
                    return true;
                    
                case "beat_detection":
                    return false; // 根据 AudioSyncModule 的实际功能调整
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 获取参数
        /// </summary>
        /// <param name="key">参数键</param>
        /// <returns>参数值</returns>
        public T GetParameter<T>(string key)
        {
            if (_parameters.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            
            return default(T);
        }
        
        /// <summary>
        /// 异步释放资源
        /// </summary>
        /// <returns>释放任务</returns>
        public async UniTask ReleaseAsync()
        {
            try
            {
                // 先禁用适配器
                if (_isEnabled)
                {
                    await DisableAsync();
                }
                
                // 清理资源
                _currentAudioSource = null;
                _audioSyncModule = null;
                _parameters.Clear();
                
                _isInitialized = false;
                ChangeState(AudioReactorState.Released);
                
                Log.Info($"AudioSyncProAdapter: 适配器资源已释放 - {_displayName}");
            }
            catch (Exception ex)
            {
                Log.Error($"AudioSyncProAdapter: 释放资源失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 改变适配器状态
        /// </summary>
        /// <param name="newState">新状态</param>
        private void ChangeState(AudioReactorState newState)
        {
            if (_currentState != newState)
            {
                _currentState = newState;
                OnStateChanged?.Invoke(this, newState);
            }
        }
        
        /// <summary>
        /// 初始化默认参数
        /// </summary>
        private void InitializeDefaultParameters()
        {
            _parameters["sensitivity"] = 1.0f;
            _parameters["smoothing"] = 0.1f;
            _parameters["gain"] = 1.0f;
            _parameters["threshold"] = 0.01f;
        }
        
        /// <summary>
        /// 应用参数到模块
        /// </summary>
        /// <param name="key">参数键</param>
        /// <param name="value">参数值</param>
        /// <returns>应用任务</returns>
        private async UniTask ApplyParameterToModule(string key, object value)
        {
            if (_audioSyncModule == null) return;
            
            try
            {
                // 使用反射安全地设置参数到 AudioSyncModule
                switch (key.ToLower())
                {
                    case "sensitivity":
                        SetModuleProperty("Sensitivity", value);
                        break;
                    
                    case "smoothing":
                        SetModuleProperty("Smoothing", value);
                        break;
                    
                    case "gain":
                        SetModuleProperty("Gain", value);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"AudioSyncProAdapter: 应用参数失败 - {key}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取RMS值
        /// </summary>
        /// <returns>RMS值</returns>
        private float GetRMSValue()
        {
            return GetModuleProperty<float>("RMS");
        }
      
        /// <summary>
        /// 安全地获取模块属性值
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="propertyName">属性名称</param>
        /// <returns>属性值</returns>
        private T GetModuleProperty<T>(string propertyName)
        {
            try
            {
                if (_audioSyncModule == null) return default(T);
                
                var property = _audioSyncModule.GetType().GetProperty(propertyName);
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(_audioSyncModule);
                    if (value is T typedValue)
                    {
                        return typedValue;
                    }
                }
                
                return default(T);
            }
            catch (Exception ex)
            {
                Log.Warning($"AudioSyncProAdapter: 获取属性 {propertyName} 失败: {ex.Message}");
                return default(T);
            }
        }
        
        /// <summary>
        /// 安全地设置模块属性值
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="value">属性值</param>
        private void SetModuleProperty(string propertyName, object value)
        {
            try
            {
                if (_audioSyncModule == null) return;
                
                var property = _audioSyncModule.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(_audioSyncModule, value);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"AudioSyncProAdapter: 设置属性 {propertyName} 失败: {ex.Message}");
            }
        }
    }
}