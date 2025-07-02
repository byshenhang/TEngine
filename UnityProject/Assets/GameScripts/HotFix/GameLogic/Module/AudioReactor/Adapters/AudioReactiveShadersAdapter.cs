using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;
using AudioReactiveShader;

namespace GameLogic.AudioReactor.Adapters
{
    /// <summary>
    /// AudioReactiveShaders 插件适配器
    /// 将 AudioReactiveShaders 插件包装为统一的 IAudioReactor 接口
    /// 支持自动发现场景中的相关组件并提供统一的控制接口
    /// </summary>
    public class AudioReactiveShadersAdapter : IAudioReactor
    {
        #region 私有字段
        
        private MusicSpectrumReader _musicReader;
        private AudioSource _audioSource;
        private AudioReactorState _currentState = AudioReactorState.Uninitialized;
        private string _reactorId;
        private bool _isEnabled = false;
        private bool _isInitialized = false;
        
        #endregion
        
        #region IAudioReactor 属性实现
        
        public string ReactorId => _reactorId;
        public string DisplayName => $"AudioReactiveShaders_{_musicReader?.gameObject.name ?? "Unknown"}"; 
        public string ReactorType => "AudioReactiveShaders";
        public bool IsEnabled => _isEnabled;
        public bool IsInitialized => _isInitialized;
        public AudioReactorState CurrentState => _currentState;
        public AudioSource CurrentAudioSource => _audioSource;
        
        #endregion
        
        #region IAudioReactor 事件实现
        
        public event Action<IAudioReactor, AudioReactorState> OnStateChanged;
        public event Action<IAudioReactor, string> OnError;
        
        #endregion
        
        #region 构造函数
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="musicReader">MusicReader 组件</param>
        public AudioReactiveShadersAdapter(MusicSpectrumReader musicReader)
        {
            _musicReader = musicReader ?? throw new ArgumentNullException(nameof(musicReader));
            _reactorId = $"AudioReactiveShaders_{musicReader.GetHashCode()}";
            
            // 尝试获取关联的 AudioSource
            _audioSource = musicReader.audioSource;
            
            SetState(AudioReactorState.Uninitialized);
        }
        
        #endregion
        
        #region 静态发现方法
        
        /// <summary>
        /// 在场景中发现所有 AudioReactiveShaders 相关组件
        /// </summary>
        /// <returns>发现的适配器列表</returns>
        public static List<AudioReactiveShadersAdapter> DiscoverInScene()
        {
            var adapters = new List<AudioReactiveShadersAdapter>();
            
            try
            {
                // 查找所有 MusicReader 组件
                var musicReaders = GameObject.FindObjectsOfType<MusicSpectrumReader>();
                
                foreach (var musicReader in musicReaders)
                {
                    if (musicReader != null && musicReader.gameObject.activeInHierarchy)
                    {
                        var adapter = new AudioReactiveShadersAdapter(musicReader);
                        adapters.Add(adapter);
                    }
                }
                
                Log.Info($"AudioReactiveShadersAdapter: 发现 {adapters.Count} 个 AudioReactiveShaders 组件");
            }
            catch (Exception ex)
            {
                Log.Error($"AudioReactiveShadersAdapter: 场景发现失败: {ex.Message}");
            }
            
            return adapters;
        }
        
        #endregion
        
        #region IAudioReactor 核心方法实现
        
        /// <summary>
        /// 异步初始化反应器
        /// </summary>
        /// <returns>初始化是否成功</returns>
        public async UniTask<bool> InitializeAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    return true;
                }
                
                SetState(AudioReactorState.Initializing);
                
                if (_musicReader == null)
                {
                    throw new InvalidOperationException("MusicReader 组件为空");
                }
                
                // 验证 MusicReader 组件状态
                // MusicReader 是抽象类，不需要启用/禁用操作
                // 只需要确保音频源可用
                if (_audioSource == null)
                {
                    Log.Warning("AudioReactiveShadersAdapter: 未找到关联的 AudioSource");
                }
                
                // 等待一帧确保初始化完成
                await UniTask.NextFrame();
                
                _isInitialized = true;
                SetState(AudioReactorState.Initialized);
                
                Log.Info($"AudioReactiveShadersAdapter: 初始化成功 - {DisplayName}");
                return true;
            }
            catch (Exception ex)
            {
                SetState(AudioReactorState.Error);
                OnError?.Invoke(this, $"初始化失败: {ex.Message}");
                Log.Error($"AudioReactiveShadersAdapter: 初始化失败 - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 异步启用反应器
        /// </summary>
        /// <returns>启用是否成功</returns>
        public async UniTask<bool> EnableAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    bool initSuccess = await InitializeAsync();
                    if (!initSuccess)
                    {
                        return false;
                    }
                }
                
                if (_isEnabled)
                {
                    return true;
                }
                
                SetState(AudioReactorState.Enabling);
                
                // MusicReader 不需要启用操作
                // 确保音频源可用
                if (_audioSource == null)
                {
                    Log.Warning("AudioReactiveShadersAdapter: 音频源未设置");
                }
                
                _isEnabled = true;
                SetState(AudioReactorState.Enabled);
                
                Log.Info($"AudioReactiveShadersAdapter: 启用成功 - {DisplayName}");
                return true;
            }
            catch (Exception ex)
            {
                SetState(AudioReactorState.Error);
                OnError?.Invoke(this, $"启用失败: {ex.Message}");
                Log.Error($"AudioReactiveShadersAdapter: 启用失败 - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 异步禁用反应器
        /// </summary>
        /// <returns>禁用是否成功</returns>
        public async UniTask<bool> DisableAsync()
        {
            try
            {
                if (!_isEnabled)
                {
                    return true;
                }
                
                SetState(AudioReactorState.Disabling);
                
                // MusicReader 不需要禁用操作
                // 这里可以进行其他清理工作
                
                _isEnabled = false;
                SetState(AudioReactorState.Disabled);
                
                Log.Info($"AudioReactiveShadersAdapter: 禁用成功 - {DisplayName}");
                return true;
            }
            catch (Exception ex)
            {
                SetState(AudioReactorState.Error);
                OnError?.Invoke(this, $"禁用失败: {ex.Message}");
                Log.Error($"AudioReactiveShadersAdapter: 禁用失败 - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 设置音频源
        /// </summary>
        /// <param name="audioSource">音频源</param>
        /// <returns>设置是否成功</returns>
        public async UniTask<bool> SetAudioSourceAsync(AudioSource audioSource)
        {
            try
            {
                _audioSource = audioSource;
                
                // 如果 MusicReader 支持设置音频源，在这里设置
                if (_musicReader != null && audioSource != null)
                {
                    // 直接设置 MusicSpectrumReader 的 audioSource 属性
                    _musicReader.audioSource = audioSource;
                    
                    // 如果是 MusicSpectrumReader 类型，可能需要刷新音频源
                    var musicSpectrumReader = _musicReader as MusicSpectrumReader;
                    if (musicSpectrumReader != null)
                    {
                        // 如果使用的是混音器组模式，可能需要刷新音频源列表
                        if (musicSpectrumReader.audio_input == AudioReactiveShader.MusicReader.AUDIO_INPUT.MixerGroup ||
                            musicSpectrumReader.audio_input == AudioReactiveShader.MusicReader.AUDIO_INPUT.MixerGroupWebGL)
                        {
                            musicSpectrumReader.refreshAudioSourcesOnMixerGroup();
                        }
                    }
                    
                    Log.Info($"AudioReactiveShadersAdapter: 音频源已设置并同步到 MusicReader - {audioSource.name}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"设置音频源失败: {ex.Message}");
                Log.Error($"AudioReactiveShadersAdapter: 设置音频源失败 - {ex.Message}");
                return false;
            }
        }
      
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <returns>释放任务</returns>
        public async UniTask ReleaseAsync()
        {
            try
            {
                SetState(AudioReactorState.Releasing);
                
                // MusicReader 不需要禁用操作
                // 进行资源清理
                
                // 清理引用
                _musicReader = null;
                _audioSource = null;
                _isEnabled = false;
                _isInitialized = false;
                
                SetState(AudioReactorState.Released);
                
                Log.Info($"AudioReactiveShadersAdapter: 资源已释放 - {DisplayName}");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"释放资源失败: {ex.Message}");
                Log.Error($"AudioReactiveShadersAdapter: 释放资源失败 - {ex.Message}");
            }
        }
        
        #endregion
        
        #region IAudioReactor 参数配置实现
        
        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns>设置是否成功</returns>
        public async UniTask<bool> SetParameterAsync(string parameterName, object value)
        {
            try
            {
                if (_musicReader == null)
                {
                    return false;
                }
                
                // 根据参数名设置对应的属性
                switch (parameterName.ToLower())
                {
                    case "enabled":
                        // MusicReader 没有 enabled 属性
                        Log.Warning("AudioReactiveShadersAdapter: MusicReader 不支持 enabled 参数");
                        break;
                        
                    // 可以根据 MusicReader 的实际属性添加更多参数
                    default:
                        Log.Warning($"AudioReactiveShadersAdapter: 未知参数 - {parameterName}");
                        break;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"设置参数失败: {ex.Message}");
                Log.Error($"AudioReactiveShadersAdapter: 设置参数失败 - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取参数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        /// <returns>参数值</returns>
        public async UniTask<T> GetParameterAsync<T>(string parameterName)
        {
            try
            {
                if (_musicReader == null)
                {
                    return default(T);
                }
                
                // 根据参数名获取对应的属性值
                switch (parameterName.ToLower())
                {
                    case "enabled":
                        // MusicReader 没有 enabled 属性
                        Log.Warning("AudioReactiveShadersAdapter: MusicReader 不支持 enabled 参数");
                        break;
                        
                    // 可以根据 MusicReader 的实际属性添加更多参数
                    default:
                        Log.Warning($"AudioReactiveShadersAdapter: 未知参数 - {parameterName}");
                        break;
                }
                
                return default(T);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"获取参数失败: {ex.Message}");
                Log.Error($"AudioReactiveShadersAdapter: 获取参数失败 - {ex.Message}");
                return default(T);
            }
        }
        
        /// <summary>
        /// 获取所有可用参数名称
        /// </summary>
        /// <returns>参数名称列表</returns>
        public async UniTask<List<string>> GetAvailableParametersAsync()
        {
            return new List<string>
            {
                // MusicReader 的实际可配置参数
                // 根据实际 API 添加参数
            };
        }
        
        #endregion
        
        #region IAudioReactor 信息查询实现
        
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
                ["GameObjectName"] = _musicReader?.gameObject.name ?? "None"
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
                case "dynamic_bands":
                    return true;
                    
                case "rms_analysis":
                case "beat_detection":
                    return false; // AudioReactiveShaders 可能不直接支持这些功能
                    
                default:
                    return false;
            }
        }
        
        #endregion
        
        #region 私有辅助方法
        
        /// <summary>
        /// 设置状态并触发事件
        /// </summary>
        /// <param name="newState">新状态</param>
        private void SetState(AudioReactorState newState)
        {
            if (_currentState != newState)
            {
                var oldState = _currentState;
                _currentState = newState;
                OnStateChanged?.Invoke(this, newState);
                
                Log.Info($"AudioReactiveShadersAdapter: 状态变化 {oldState} -> {newState} - {DisplayName}");
            }
        }
        
        #endregion
    }
}