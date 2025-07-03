using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;
using TelePresent.AudioSyncPro;
using Object = UnityEngine.Object;
using AudioReactiveShader;

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
        public AudioSourcePlus sourcePlus;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="audioSyncModule">Audio Sync Pro 模块实例</param>
        /// <param name="customName">自定义显示名称</param>
        public AudioSyncProAdapter(AudioSourcePlus audioSourcePlus)
        {
            this.sourcePlus = audioSourcePlus;
            // 生成唯一标识
            _reactorId = $"AudioSyncPro__{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            _displayName = $"Audio Sync Pro ";
            
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
                // 尝试在场景中查找AudioReactor和AudioSourcePlus组件
                var audioReactor = Object.FindObjectsOfType<TelePresent.AudioSyncPro.AudioReactor>();
                var audioSourcePlus = Object.FindObjectsOfType<AudioSourcePlus>();

                // 检查是否找到必要的组件
                if (audioReactor.Length == 0)
                {
                    Log.Warning("AudioSyncProAdapter: 场景中未找到AudioReactor组件");
                    return adapters;
                }
                
                if (audioSourcePlus.Length == 0)
                {
                    Log.Warning("AudioSyncProAdapter: 场景中未找到AudioSourcePlus组件");
                    return adapters;
                }

                if (audioSourcePlus.Length > 1)
                {
                    Log.Warning($"AudioSyncProAdapter: 在场景中找到多个AudioSourcePlus组件，将使用第一个");
                }

                // 如果找到了必要的组件，创建适配器
                var adapter = new AudioSyncProAdapter(audioSourcePlus.First());
                adapters.Add(adapter);

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
                // 参数验证
                if (audioSource == null)
                {
                    Log.Warning("AudioSyncProAdapter: 传入的音频源为空");
                    return false;
                }
                
                if (sourcePlus == null)
                {
                    Log.Error("AudioSyncProAdapter: AudioSourcePlus组件为空");
                    return false;
                }
                
                _currentAudioSource = audioSource;
                sourcePlus.SetAudioSource(audioSource);
                
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
                
                // 取消事件订阅，防止内存泄漏
                OnStateChanged = null;
                OnError = null;
                
                // 清理资源
                _currentAudioSource = null;
                _parameters.Clear();
                sourcePlus = null;
                
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
        
       
        
    }
}