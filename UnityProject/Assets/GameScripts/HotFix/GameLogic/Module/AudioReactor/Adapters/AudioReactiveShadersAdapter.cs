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

            // 确保 numBands 已正确初始化
            if (musicReader.numBands <= 0)
            {
                Log.Warning($"AudioReactiveShadersAdapter: MusicReader 的 numBands 为 {musicReader.numBands}，已设置为默认值 8");
                musicReader.numBands = 8;
            }

            // 确保频段相关数组已正确初始化
            if (musicReader.groupedBands == null || musicReader.groupedBands.Length != musicReader.numBands ||
                musicReader.bandGroupsDistribution == null || musicReader.bandGroupsDistribution.Length != musicReader.numBands)
            {
                Log.Warning("AudioReactiveShadersAdapter: MusicReader 的数组未正确初始化，尝试重新初始化");

                var musicSpectrumReader = musicReader as MusicSpectrumReader;
                if (musicSpectrumReader != null)
                {
                    try
                    {
                        musicSpectrumReader.DinamicBandsDistribution();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"AudioReactiveShadersAdapter: 重新初始化频段分布失败 - {ex.Message}");
                    }
                }
            }

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
                        // 确保 numBands 已正确初始化
                        if (musicSpectrumReader.numBands <= 0)
                        {
                            Log.Warning($"SetAudioSourceAsync: MusicReader 的 numBands 为 {musicSpectrumReader.numBands}，已设置为默认值 8");
                            musicSpectrumReader.numBands = 8;
                        }

                        // 确保频段相关数组已正确初始化
                        if (musicSpectrumReader.groupedBands == null || musicSpectrumReader.groupedBands.Length != musicSpectrumReader.numBands ||
                            musicSpectrumReader.bandGroupsDistribution == null || musicSpectrumReader.bandGroupsDistribution.Length != musicSpectrumReader.numBands)
                        {
                            Log.Warning("SetAudioSourceAsync: MusicReader 的数组未正确初始化，尝试重新初始化");

                            try
                            {
                                // 使用反射调用私有方法
                                musicSpectrumReader.DinamicBandsDistribution();
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"SetAudioSourceAsync: 重新初始化频段分布失败 - {ex.Message}");
                            }
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