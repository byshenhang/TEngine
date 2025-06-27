using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TEngine;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TelePresent.AudioSyncPro;
using LyricFX.Managers;
using LyricFX.Utils;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 音频歌词协调器 - 统一管理音频同步和歌词播放的协调模块
    /// 采用协调器模式，提供最佳的可维护性和扩展性，同时保持各模块职责单一性
    /// 支持自动发现AudioReactor和多AudioReactor管理
    /// </summary>
    public class AudioLyricCoordinator : Singleton<AudioLyricCoordinator>, IUpdate
    {
        // 模块引用
        private AudioSyncModule _audioSync;
        private LyricFXModule _lyricFX;
        
        // 协调状态
        private bool _isInitialized = false;
        private bool _isPlaying = false;
        private bool _enableDebugger = false;
        
        // 当前播放信息
        private string _currentAudioName = "";
        private string _currentLyricContent = "";
        private float _syncOffset = 0f;
        
        // AudioReactor管理
        private Dictionary<string, AudioReactor> _discoveredAudioReactors = new Dictionary<string, AudioReactor>();
        private AudioReactor _currentAudioReactor;
        private string _currentAudioReactorId;
        
        // 取消令牌源
        private CancellationTokenSource _cts;
        
        // 事件
        public event Action OnPlaybackStarted;
        public event Action OnPlaybackStopped;
        public event Action<float, float[]> OnAudioDataReceived; // RMS值和频谱数据
        public event Action<string> OnLyricLineChanged; // 当前歌词行变化
        
        protected override void OnInit()
        {
            base.OnInit();
            _cts = new CancellationTokenSource();
            
            // 获取模块实例
            _audioSync = AudioSyncModule.Instance;
            _lyricFX = LyricFXModule.Instance;
            
            Log.Info("AudioLyricCoordinator initialized");
        }

        public async void SetLyric(Transform c, GameObject prefabInstance, Transform pool)
        {
           await _lyricFX.GetLyricManager().SetupAsync(pool, prefabInstance, pool);
        }

        public override void Release()
        {
            base.Release();
            
            // 停止所有播放
            StopAll();
            
            // 清理事件订阅
            UnsubscribeFromAudioEvents();
            
            // 清理资源
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            
            _audioSync = null;
            _lyricFX = null;
            
            Log.Info("AudioLyricCoordinator released");
        }
        
        public void OnUpdate()
        {
            // 协调器的每帧更新逻辑
            if (_isPlaying && _audioSync != null && _lyricFX != null)
            {
                // 可以在这里添加实时同步检查逻辑
            }
        }
        
        /// <summary>
        /// 自动发现场景中的AudioReactor组件
        /// </summary>
        /// <param name="searchInChildren">是否在子对象中搜索</param>
        /// <returns>发现的AudioReactor数量</returns>
        public int DiscoverAudioReactors(bool searchInChildren = true)
        {
            _discoveredAudioReactors.Clear();
            
            AudioReactor[] audioReactors;
            if (searchInChildren)
            {
                audioReactors = Object.FindObjectsOfType<AudioReactor>();
            }
            else
            {
                audioReactors = Object.FindObjectsOfType<AudioReactor>();
            }
            
            for (int i = 0; i < audioReactors.Length; i++)
            {
                var reactor = audioReactors[i];
                string id = $"{reactor.gameObject.name}_{reactor.GetInstanceID()}";
                _discoveredAudioReactors[id] = reactor;
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioLyricCoordinator: 发现AudioReactor - {id}");
                }
            }
            
            if (_enableDebugger)
            {
                Log.Info($"AudioLyricCoordinator: 总共发现 {_discoveredAudioReactors.Count} 个AudioReactor");
            }
            
            return _discoveredAudioReactors.Count;
        }
        
        /// <summary>
        /// 获取所有已发现的AudioReactor信息
        /// </summary>
        /// <returns>AudioReactor ID和名称的字典</returns>
        public Dictionary<string, string> GetDiscoveredAudioReactors()
        {
            var result = new Dictionary<string, string>();
            foreach (var kvp in _discoveredAudioReactors)
            {
                result[kvp.Key] = kvp.Value.gameObject.name;
            }
            return result;
        }
        
        /// <summary>
        /// 自动初始化协调器（使用第一个发现的AudioReactor）
        /// </summary>
        /// <param name="audioSourcePlus">音频源增强组件（可选）</param>
        /// <returns>是否初始化成功</returns>
        public async UniTask<bool> AutoInitialize(AudioSourcePlus audioSourcePlus = null)
        {
            // 先尝试发现AudioReactor
            int discoveredCount = DiscoverAudioReactors();
            if (discoveredCount == 0)
            {
                Log.Error("AudioLyricCoordinator: 未发现任何AudioReactor组件");
                return false;
            }
            
            // 使用第一个发现的AudioReactor
            var firstReactor = _discoveredAudioReactors.Values.First();
            _currentAudioReactor = firstReactor;
            _currentAudioReactorId = _discoveredAudioReactors.Keys.First();
            
            if (_enableDebugger)
            {
                Log.Info($"AudioLyricCoordinator: 自动选择AudioReactor - {_currentAudioReactorId}");
            }
            
            return await InternalInitialize(firstReactor, audioSourcePlus);
        }
        
        /// <summary>
        /// 切换到指定的AudioReactor
        /// </summary>
        /// <param name="audioReactorId">AudioReactor ID</param>
        /// <param name="audioSourcePlus">音频源增强组件（可选）</param>
        /// <returns>是否切换成功</returns>
        public async UniTask<bool> SwitchToAudioReactor(string audioReactorId, AudioSourcePlus audioSourcePlus = null)
        {
            if (!_discoveredAudioReactors.ContainsKey(audioReactorId))
            {
                Log.Error($"AudioLyricCoordinator: 未找到指定的AudioReactor - {audioReactorId}");
                return false;
            }
            
            // 停止当前播放
            if (_isPlaying)
            {
                await Stop();
            }
            
            // 释放当前资源
            if (_isInitialized)
            {
                UnsubscribeFromAudioEvents();
                _audioSync?.Release();
                _isInitialized = false;
            }
            
            // 切换到新的AudioReactor
            _currentAudioReactor = _discoveredAudioReactors[audioReactorId];
            _currentAudioReactorId = audioReactorId;
            
            if (_enableDebugger)
            {
                Log.Info($"AudioLyricCoordinator: 切换到AudioReactor - {audioReactorId}");
            }
            
            return await InternalInitialize(_currentAudioReactor, audioSourcePlus);
        }
        
        /// <summary>
        /// 注册所有发现的AudioReactor（批量管理）
        /// </summary>
        /// <returns>注册成功的数量</returns>
        public async UniTask<int> RegisterAllAudioReactors()
        {
            int successCount = 0;
            
            foreach (var kvp in _discoveredAudioReactors)
            {
                try
                {
                    // 这里可以为每个AudioReactor进行预初始化
                    // 例如检查其AudioSourcePlus组件等
                    var reactor = kvp.Value;
                    var audioSourcePlus = reactor.GetComponent<AudioSourcePlus>();
                    
                    if (_enableDebugger)
                    {
                        Log.Info($"AudioLyricCoordinator: 注册AudioReactor - {kvp.Key}, AudioSourcePlus: {(audioSourcePlus != null ? "存在" : "不存在")}");
                    }
                    
                    successCount++;
                }
                catch (Exception ex)
                {
                    Log.Error($"AudioLyricCoordinator: 注册AudioReactor失败 - {kvp.Key}: {ex}");
                }
            }
            
            return successCount;
        }
        
        /// <summary>
        /// 获取当前使用的AudioReactor信息
        /// </summary>
        /// <returns>当前AudioReactor的ID和名称</returns>
        public (string id, string name) GetCurrentAudioReactor()
        {
            if (_currentAudioReactor != null && !string.IsNullOrEmpty(_currentAudioReactorId))
            {
                return (_currentAudioReactorId, _currentAudioReactor.gameObject.name);
            }
            return (string.Empty, string.Empty);
        }
        
        /// <summary>
        /// 内部初始化方法
        /// </summary>
        /// <param name="audioReactor">音频反应器</param>
        /// <param name="audioSourcePlus">音频源增强组件</param>
        /// <returns>是否初始化成功</returns>
        private async UniTask<bool> InternalInitialize(AudioReactor audioReactor, AudioSourcePlus audioSourcePlus = null)
        {
            if (_audioSync == null || _lyricFX == null)
            {
                Log.Error("AudioLyricCoordinator: 模块实例未找到");
                return false;
            }
            
            try
            {
                if (_enableDebugger)
                {
                    Log.Info("AudioLyricCoordinator: 开始初始化协调器");
                }
                
                // 记录当前使用的AudioReactor
                _currentAudioReactor = audioReactor;
                _currentAudioReactorId = $"{audioReactor.gameObject.name}_{audioReactor.GetInstanceID()}";
                
                // 初始化音频同步模块
                bool audioInitSuccess = _audioSync.Initialize(audioReactor, audioSourcePlus);
                if (!audioInitSuccess)
                {
                    Log.Error("AudioLyricCoordinator: 音频同步模块初始化失败");
                    return false;
                }
                
                // 歌词模块已在其构造函数中初始化
                
                // 订阅音频事件
                SubscribeToAudioEvents();
                
                // 设置同步偏移
                if (_syncOffset != 0)
                {
                    _lyricFX.SetSyncOffset(_syncOffset);
                }
                
                _isInitialized = true;
                
                if (_enableDebugger)
                {
                    Log.Info("AudioLyricCoordinator: 协调器初始化成功");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioLyricCoordinator: 初始化失败: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 启用或禁用调试模式
        /// </summary>
        /// <param name="enable">是否启用调试</param>
        public void EnableDebugger(bool enable)
        {
            _enableDebugger = enable;
            
            // 同步到子模块
            _audioSync?.EnableDebugger(enable);
            _lyricFX?.EnableDebugger(enable);
            
            if (enable)
            {
                Log.Info("AudioLyricCoordinator: 调试模式已启用");
            }
            else
            {
                Log.Info("AudioLyricCoordinator: 调试模式已禁用");
            }
        }
        
        /// <summary>
        /// 设置同步偏移量
        /// </summary>
        /// <param name="offset">偏移量（秒）</param>
        public void SetSyncOffset(float offset)
        {
            _syncOffset = offset;
            
            if (_lyricFX != null)
            {
                _lyricFX.SetSyncOffset(_syncOffset);
            }
            
            Log.Info($"AudioLyricCoordinator: 设置同步偏移为 {_syncOffset}秒");
        }
        
        /// <summary>
        /// 同步播放音频和歌词
        /// </summary>
        /// <param name="audioClip">音频剪辑</param>
        /// <param name="lrcContent">LRC歌词内容</param>
        /// <param name="lyricPosition">歌词显示位置</param>
        /// <param name="effectId">歌词特效ID</param>
        /// <param name="layoutId">歌词布局ID</param>
        /// <param name="audioStartDelay">音频开始延迟（秒）</param>
        /// <returns>是否播放成功</returns>
        public async UniTask<bool> PlayWithSync(AudioClip audioClip, string lrcContent, Vector3 lyricPosition, 
            string effectId = null, string layoutId = null, float audioStartDelay = 0.1f)
        {
            if (!_isInitialized)
            {
                Log.Error("AudioLyricCoordinator: 协调器未初始化");
                return false;
            }
            
            if (audioClip == null || string.IsNullOrEmpty(lrcContent))
            {
                Log.Error("AudioLyricCoordinator: 音频剪辑或歌词内容为空");
                return false;
            }
            
            try
            {
                if (_enableDebugger)
                {
                    Log.Info($"AudioLyricCoordinator: 开始同步播放 - 音频: {audioClip.name}");
                }
                
                // 停止当前播放
                StopAll();
                
                // 记录当前播放信息
                _currentAudioName = audioClip.name;
                _currentLyricContent = lrcContent;
                
                // 启动音频播放任务
                var audioTask = PlayAudioAsync(audioClip, audioStartDelay);
                
                // 启动歌词播放任务
                var lyricTask = PlayLyricAsync(lrcContent, lyricPosition, effectId, layoutId);
                
                // 等待两个任务都完成
                var results = await UniTask.WhenAll(audioTask, lyricTask);
                
                bool audioSuccess = results.Item1;
                bool lyricSuccess = results.Item2;
                
                if (audioSuccess && lyricSuccess)
                {
                    _isPlaying = true;
                    OnPlaybackStarted?.Invoke();
                    
                    if (_enableDebugger)
                    {
                        Log.Info("AudioLyricCoordinator: 同步播放启动成功");
                    }
                    
                    return true;
                }
                else
                {
                    Log.Error($"AudioLyricCoordinator: 播放失败 - 音频: {audioSuccess}, 歌词: {lyricSuccess}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"AudioLyricCoordinator: 同步播放失败: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 播放音频（异步）
        /// </summary>
        /// <param name="audioClip">音频剪辑</param>
        /// <param name="startDelay">开始延迟</param>
        /// <returns>是否播放成功</returns>
        private async UniTask<bool> PlayAudioAsync(AudioClip audioClip, float startDelay)
        {
            try
            {
                // 延迟启动音频，为歌词系统预留初始化时间
                if (startDelay > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: _cts.Token);
                }
                
                return _audioSync.PlayAudio(audioClip);
            }
            catch (Exception ex)
            {
                Log.Error($"AudioLyricCoordinator: 音频播放失败: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 播放歌词（异步）
        /// </summary>
        /// <param name="lrcContent">LRC内容</param>
        /// <param name="position">显示位置</param>
        /// <param name="effectId">特效ID</param>
        /// <param name="layoutId">布局ID</param>
        /// <returns>是否播放成功</returns>
        private async UniTask<bool> PlayLyricAsync(string lrcContent, Vector3 position, string effectId, string layoutId)
        {
            try
            {
                return await _lyricFX.PlayLrcFile(lrcContent, position, null, 0f, effectId, layoutId);
            }
            catch (Exception ex)
            {
                Log.Error($"AudioLyricCoordinator: 歌词播放失败: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 异步停止播放
        /// </summary>
        /// <returns>停止任务</returns>
        public async UniTask Stop()
        {
            if (_isPlaying)
            {
                _audioSync?.StopAudio();
                _lyricFX?.StopAll();
                
                _isPlaying = false;
                OnPlaybackStopped?.Invoke();
                
                if (_enableDebugger)
                {
                    Log.Info("AudioLyricCoordinator: 已停止播放");
                }
            }
            
            await UniTask.CompletedTask;
        }
        
        /// <summary>
        /// 停止所有播放
        /// </summary>
        public void StopAll()
        {
            if (_isPlaying)
            {
                _audioSync?.StopAudio();
                _lyricFX?.StopAll();
                
                _isPlaying = false;
                OnPlaybackStopped?.Invoke();
                
                if (_enableDebugger)
                {
                    Log.Info("AudioLyricCoordinator: 已停止所有播放");
                }
            }
        }
        
        /// <summary>
        /// 准备音频和歌词资源
        /// </summary>
        /// <param name="audioClip">音频剪辑</param>
        /// <param name="lrcContent">LRC歌词内容</param>
        /// <returns>是否准备成功</returns>
        public async UniTask<bool> PrepareAudioAndLyrics(AudioClip audioClip, string lrcContent)
        {
            try
            {
                if (!_isInitialized)
                {
                    Log.Error("AudioLyricCoordinator: 未初始化，无法准备资源");
                    return false;
                }
                
                if (audioClip == null)
                {
                    Log.Error("AudioLyricCoordinator: 音频剪辑为空");
                    return false;
                }
                
                if (string.IsNullOrEmpty(lrcContent))
                {
                    Log.Error("AudioLyricCoordinator: 歌词内容为空");
                    return false;
                }
                
                // 保存当前播放信息
                _currentAudioName = audioClip.name;
                _currentLyricContent = lrcContent;
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioLyricCoordinator: 已准备音频 '{audioClip.name}' 和歌词资源");
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error($"AudioLyricCoordinator: 准备资源失败: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 开始同步播放音频和歌词
        /// </summary>
        /// <param name="lyricPosition">歌词显示位置</param>
        /// <param name="effectId">特效ID</param>
        /// <param name="layoutId">布局ID</param>
        /// <param name="startDelay">开始延迟（秒）</param>
        /// <returns>是否播放成功</returns>
        public async UniTask<bool> PlaySynchronized(Vector3? lyricPosition = null, string effectId = null, string layoutId = null, float startDelay = 0.1f)
        {
            try
            {
                if (!_isInitialized)
                {
                    Log.Error("AudioLyricCoordinator: 未初始化，无法开始播放");
                    return false;
                }
                
                if (string.IsNullOrEmpty(_currentAudioName) || string.IsNullOrEmpty(_currentLyricContent))
                {
                    Log.Error("AudioLyricCoordinator: 未准备音频或歌词资源");
                    return false;
                }
                
                // 查找音频剪辑
                var audioClip = GameModule.Resource.LoadAsset<AudioClip>(_currentAudioName);
                if (audioClip == null)
                {
                    Log.Error($"AudioLyricCoordinator: 无法加载音频剪辑 '{_currentAudioName}'");
                    return false;
                }
                
                // 使用默认位置如果未指定
                Vector3 position = lyricPosition ?? Vector3.zero;
                
                // 启动音频播放任务
                var audioTask = PlayAudioAsync(audioClip, startDelay);
                
                // 启动歌词播放任务
                var lyricTask = PlayLyricAsync(_currentLyricContent, position, effectId, layoutId);
                
                // 等待两个任务都完成
                var results = await UniTask.WhenAll(audioTask, lyricTask);
                
                bool audioSuccess = results.Item1;
                bool lyricSuccess = results.Item2;
                
                if (audioSuccess && lyricSuccess)
                {
                    _isPlaying = true;
                    OnPlaybackStarted?.Invoke();
                    
                    if (_enableDebugger)
                    {
                        Log.Info("AudioLyricCoordinator: 同步播放启动成功");
                    }
                    
                    return true;
                }
                else
                {
                    Log.Error($"AudioLyricCoordinator: 播放失败 - 音频: {audioSuccess}, 歌词: {lyricSuccess}");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"AudioLyricCoordinator: 同步播放失败: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            if (_isPlaying)
            {
                _audioSync?.PauseAudio();
                // 歌词系统暂停需要根据LyricFX的API来实现
                
                if (_enableDebugger)
                {
                    Log.Info("AudioLyricCoordinator: 播放已暂停");
                }
            }
        }
        
        /// <summary>
        /// 恢复播放
        /// </summary>
        public void Resume()
        {
            if (_isPlaying)
            {
                _audioSync?.ResumeAudio();
                // 歌词系统恢复需要根据LyricFX的API来实现
                
                if (_enableDebugger)
                {
                    Log.Info("AudioLyricCoordinator: 播放已恢复");
                }
            }
        }
        
        /// <summary>
        /// 设置播放位置
        /// </summary>
        /// <param name="time">时间（秒）</param>
        public void SetPlaybackTime(float time)
        {
            if (_isPlaying)
            {
                _audioSync?.SetAudioTime(time);
                // 歌词系统时间设置需要根据LyricFX的API来实现
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioLyricCoordinator: 设置播放位置为 {time:F2} 秒");
                }
            }
        }
        
        /// <summary>
        /// 订阅音频事件
        /// </summary>
        private void SubscribeToAudioEvents()
        {
            if (_audioSync != null)
            {
                _audioSync.OnAudioStarted += HandleAudioStarted;
                _audioSync.OnAudioStopped += HandleAudioStopped;
                _audioSync.OnAudioDataUpdated += HandleAudioDataUpdated;
            }
        }
        
        /// <summary>
        /// 取消订阅音频事件
        /// </summary>
        private void UnsubscribeFromAudioEvents()
        {
            if (_audioSync != null)
            {
                _audioSync.OnAudioStarted -= HandleAudioStarted;
                _audioSync.OnAudioStopped -= HandleAudioStopped;
                _audioSync.OnAudioDataUpdated -= HandleAudioDataUpdated;
            }
        }
        
        /// <summary>
        /// 处理音频开始事件
        /// </summary>
        private void HandleAudioStarted()
        {
            if (_enableDebugger)
            {
                Log.Info("AudioLyricCoordinator: 音频开始播放事件");
            }
            
            // 可以在这里添加音频开始时的协调逻辑
        }
        
        /// <summary>
        /// 处理音频停止事件
        /// </summary>
        private void HandleAudioStopped()
        {
            if (_enableDebugger)
            {
                Log.Info("AudioLyricCoordinator: 音频停止播放事件");
            }
            
            // 当音频停止时，也停止歌词
            if (_isPlaying)
            {
                _lyricFX?.StopAll();
                _isPlaying = false;
                OnPlaybackStopped?.Invoke();
            }
        }
        
        /// <summary>
        /// 处理音频数据更新事件
        /// </summary>
        /// <param name="rms">RMS值</param>
        /// <param name="spectrum">频谱数据</param>
        private void HandleAudioDataUpdated(float rms, float[] spectrum)
        {
            // 将音频数据传递给外部监听者
            OnAudioDataReceived?.Invoke(rms, spectrum);
            
            // 可以在这里根据音频数据动态调整歌词效果
            // 例如：根据音量大小调整歌词缩放、颜色等
            if (rms > 0.1f) // 音量阈值
            {
                // 实现音频响应的歌词效果
                // 这里可以调用歌词系统的动态效果API
            }
        }
        
        /// <summary>
        /// 获取当前播放状态
        /// </summary>
        /// <returns>是否正在播放</returns>
        public bool IsPlaying()
        {
            return _isPlaying;
        }
        
        /// <summary>
        /// 获取当前音频时间
        /// </summary>
        /// <returns>当前播放时间（秒）</returns>
        public float GetCurrentTime()
        {
            return _audioSync?.GetCurrentTime() ?? 0f;
        }
        
        /// <summary>
        /// 获取音频总长度
        /// </summary>
        /// <returns>音频总长度（秒）</returns>
        public float GetAudioLength()
        {
            return _audioSync?.GetAudioLength() ?? 0f;
        }
        
        /// <summary>
        /// 获取当前RMS值
        /// </summary>
        /// <returns>RMS值</returns>
        public float GetRMSValue()
        {
            return _audioSync?.GetRMSValue() ?? 0f;
        }
        
        /// <summary>
        /// 获取当前频谱数据
        /// </summary>
        /// <returns>频谱数据数组</returns>
        public float[] GetSpectrumData()
        {
            return _audioSync?.GetSpectrumData();
        }
        
        /// <summary>
        /// 获取音频同步模块实例
        /// </summary>
        /// <returns>AudioSyncModule实例</returns>
        public AudioSyncModule GetAudioSyncModule()
        {
            return _audioSync;
        }
        
        /// <summary>
        /// 获取歌词FX模块实例
        /// </summary>
        /// <returns>LyricFXModule实例</returns>
        public LyricFXModule GetLyricFXModule()
        {
            return _lyricFX;
        }
        
        /// <summary>
        /// 检查协调器是否已初始化
        /// </summary>
        /// <returns>是否已初始化</returns>
        public bool IsInitialized()
        {
            return _isInitialized;
        }
        
        /// <summary>
        /// 重置协调器状态 - 用于切换歌曲或场景时的清理
        /// 这个方法会停止当前播放，清理所有状态，但保持模块引用和发现的AudioReactor
        /// </summary>
        public void Reset()
        {
            if (_enableDebugger)
            {
                Log.Info("AudioLyricCoordinator: 开始重置状态");
            }
            
            // 停止当前播放
            if (_isPlaying)
            {
                _audioSync?.StopAudio();
                _lyricFX?.StopAll();
                _isPlaying = false;
                OnPlaybackStopped?.Invoke();
            }
            
            // 清理播放状态
            _currentAudioName = "";
            _currentLyricContent = "";
            _syncOffset = 0f;
            
            // 取消所有异步操作
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }
            
            // 清理音频同步模块的状态
            _audioSync?.ClearMarkers();
            
            // 清理歌词FX模块的状态
            _lyricFX?.StopAll();
            
            if (_enableDebugger)
            {
                Log.Info("AudioLyricCoordinator: 重置完成，可以开始新的播放");
            }
        }
        
        /// <summary>
        /// 完全重置协调器 - 用于场景切换时的完全清理
        /// 这个方法会重置所有状态，包括已发现的AudioReactor和初始化状态
        /// </summary>
        public void FullReset()
        {
            if (_enableDebugger)
            {
                Log.Info("AudioLyricCoordinator: 开始完全重置");
            }
            
            // 先执行基本重置
            Reset();
            
            // 释放初始化状态
            if (_isInitialized)
            {
                UnsubscribeFromAudioEvents();
                _audioSync?.Release();
                _isInitialized = false;
            }
            
            // 清理AudioReactor相关状态
            _discoveredAudioReactors.Clear();
            _currentAudioReactor = null;
            _currentAudioReactorId = null;
            
            if (_enableDebugger)
            {
                Log.Info("AudioLyricCoordinator: 完全重置完成，需要重新初始化");
            }
        }
        
        /// <summary>
        /// 快速切换歌曲 - 在同一场景内切换不同歌曲时使用
        /// </summary>
        /// <param name="audioClip">新的音频剪辑</param>
        /// <param name="lrcContent">新的歌词内容</param>
        /// <param name="lyricPosition">歌词显示位置</param>
        /// <param name="effectId">特效ID</param>
        /// <param name="layoutId">布局ID</param>
        /// <param name="startDelay">开始延迟</param>
        /// <returns>是否切换成功</returns>
        public async UniTask<bool> SwitchSong(AudioClip audioClip, string lrcContent, Vector3? lyricPosition = null, string effectId = null, string layoutId = null, float startDelay = 0.1f)
        {
            try
            {
                if (!_isInitialized)
                {
                    Log.Error("AudioLyricCoordinator: 未初始化，无法切换歌曲");
                    return false;
                }
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioLyricCoordinator: 切换到新歌曲 '{audioClip?.name}'");
                }
                
                // 重置当前状态（但保持初始化状态）
                Reset();
                
                // 准备新的音频和歌词
                bool prepareSuccess = await PrepareAudioAndLyrics(audioClip, lrcContent);
                if (!prepareSuccess)
                {
                    Log.Error("AudioLyricCoordinator: 准备新歌曲资源失败");
                    return false;
                }
                
                // 开始播放新歌曲
                bool playSuccess = await PlaySynchronized(lyricPosition, effectId, layoutId, startDelay);
                if (!playSuccess)
                {
                    Log.Error("AudioLyricCoordinator: 播放新歌曲失败");
                    return false;
                }
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioLyricCoordinator: 成功切换到新歌曲 '{audioClip.name}'");
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error($"AudioLyricCoordinator: 切换歌曲失败: {ex}");
                return false;
            }
        }


    }
}