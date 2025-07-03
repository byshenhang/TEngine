using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TEngine;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameLogic.AudioReactor;

namespace GameLogic
{
    /// <summary>
    /// 音频歌词协调器 - 统一管理音频同步和歌词播放的协调模块
    /// 采用协调器模式，提供最佳的可维护性和扩展性，同时保持各模块职责单一性
    /// 重构版本：集成新的AudioReactorManager统一音频反应器管理系统
    /// </summary>
    public class AudioLyricCoordinator : Singleton<AudioLyricCoordinator>, IUpdate
    {
        // 模块引用
        private LyricFXModule _lyricFX;
        private AudioReactorManager _audioReactorManager;
        
        // 协调状态
        private bool _isInitialized = false;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _enableDebugger = false;
        
        // 当前播放信息
        private string _currentAudioName = "";
        private string _currentLyricContent = "";
        private float _syncOffset = 0f;
        private float _pausedTime = 0f;
        
        // 音频源管理
        private AudioSource _globalAudioSource;
        
        // 取消令牌源
        private CancellationTokenSource _cts;
        
        // 事件
        public event Action OnPlaybackStarted;
        public event Action OnPlaybackStopped;
        public event Action<string> OnLyricLineChanged; // 当前歌词行变化
        
        protected override void OnInit()
        {
            base.OnInit();
            _cts = new CancellationTokenSource();
            
            // 获取模块实例
            _lyricFX = LyricFXModule.Instance;
            _audioReactorManager = AudioReactorManager.Instance;
            
            Log.Info("AudioLyricCoordinator initialized with AudioReactorManager");
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
            
            // 清理资源
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            
            _lyricFX = null;
            _audioReactorManager = null;
            _globalAudioSource = null;
            
            Log.Info("AudioLyricCoordinator released");
        }
        
        public void OnUpdate()
        {
            // 监控音频播放状态
            MonitorAudioPlayback();
        }
        
        /// <summary>
        /// 监控音频播放状态
        /// </summary>
        private void MonitorAudioPlayback()
        {
            if (_isPlaying && !_isPaused && _globalAudioSource != null)
            {
                // 检查音频是否已经播放完成或异常停止
                if (!_globalAudioSource.isPlaying)
                {
                    // 检查是否是正常播放完成（播放时间接近音频长度）
                    float currentTime = _globalAudioSource.time;
                    float totalTime = _globalAudioSource.clip?.length ?? 0f;
                    
                    if (totalTime > 0 && currentTime >= totalTime - 0.1f)
                    {
                        // 正常播放完成
                        HandleAudioCompleted();
                    }
                    else
                    {
                        // 异常停止
                        HandleAudioStopped();
                    }
                }
            }
        }
        
        /// <summary>
        /// 自动发现并注册音频反应器
        /// </summary>
        /// <returns>发现并注册的音频反应器数量</returns>
        public async UniTask<int> DiscoverAudioReactorsAsync()
        {
            if (_audioReactorManager == null)
            {
                Log.Error("AudioLyricCoordinator: AudioReactorManager未初始化");
                return 0;
            }
            
            int discoveredCount = await _audioReactorManager.AutoDiscoverReactorsAsync();
            
            if (_enableDebugger)
            {
                Log.Info($"AudioLyricCoordinator: 通过AudioReactorManager发现并注册了 {discoveredCount} 个音频反应器");
            }
            
            return discoveredCount;
        }
        
        /// <summary>
        /// 获取所有已注册的音频反应器信息
        /// </summary>
        /// <returns>音频反应器信息字典</returns>
        public Dictionary<string, (string displayName, string type, bool isEnabled)> GetRegisteredAudioReactors()
        {
            if (_audioReactorManager == null)
            {
                Log.Warning("AudioLyricCoordinator: AudioReactorManager未初始化");
                return new Dictionary<string, (string, string, bool)>();
            }
            
            return _audioReactorManager.GetRegisteredReactors();
        }
        
        /// <summary>
        /// 获取当前使用的音频反应器
        /// </summary>
        /// <returns>当前音频反应器信息</returns>
        public (string name, string id) GetCurrentAudioReactor()
        {
            if (_audioReactorManager == null)
            {
                Log.Warning("AudioLyricCoordinator: AudioReactorManager未初始化");
                return ("None", "none");
            }
            
            var registeredReactors = _audioReactorManager.GetRegisteredReactors();
            var enabledReactor = registeredReactors.FirstOrDefault(r => r.Value.isEnabled);
            
            if (enabledReactor.Key != null)
            {
                return (enabledReactor.Value.displayName, enabledReactor.Key);
            }
            
            return ("None", "none");
        }
        
        /// <summary>
        /// 获取所有已发现的音频反应器
        /// </summary>
        /// <returns>已发现的音频反应器字典</returns>
        public Dictionary<string, string> GetDiscoveredAudioReactors()
        {
            if (_audioReactorManager == null)
            {
                Log.Warning("AudioLyricCoordinator: AudioReactorManager未初始化");
                return new Dictionary<string, string>();
            }
            
            var registeredReactors = _audioReactorManager.GetRegisteredReactors();
            var result = new Dictionary<string, string>();
            
            foreach (var reactor in registeredReactors)
            {
                result[reactor.Key] = reactor.Value.displayName;
            }
            
            return result;
        }
        
        
        /// <summary>
        /// 自动初始化协调器（自动发现并启用音频反应器）
        /// </summary>
        /// <param name="audioSource">全局音频源（可选）</param>
        /// <returns>是否初始化成功</returns>
        public async UniTask<bool> AutoInitializeAsync()
        {
            try
            {
                _globalAudioSource = GameObject.Find("SyncAudioSource").GetComponent<AudioSource>();
                if (_audioReactorManager == null)
                {
                    Log.Error("AudioLyricCoordinator: AudioReactorManager未初始化");
                    return false;
                }
                
                // 自动发现并注册音频反应器
                int discoveredCount = await DiscoverAudioReactorsAsync();
                if (discoveredCount == 0)
                {
                    Log.Error("AudioLyricCoordinator: 未发现任何音频反应器组件");
                    return false;
                }
                
                // 设置全局音频源
                await SetGlobalAudioSourceAsync(_globalAudioSource);
                
                // 启用所有音频反应器
                bool enableSuccess = await _audioReactorManager.EnableAllReactorsAsync();
                if (!enableSuccess)
                {
                    Log.Warning("AudioLyricCoordinator: 部分音频反应器启用失败");
                }
                
                return await InternalInitializeAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"AudioLyricCoordinator: 自动初始化失败: {ex.Message}");
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
                
                if (_audioReactorManager != null)
                {
                    bool success = await _audioReactorManager.SetGlobalAudioSourceAsync(audioSource);
                    
                    if (_enableDebugger)
                    {
                        Log.Info($"AudioLyricCoordinator: 全局音频源已设置 - {(success ? "成功" : "失败")}");
                    }
                    
                    return success;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioLyricCoordinator: 设置全局音频源失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 启用所有音频反应器
        /// </summary>
        /// <returns>启用任务</returns>
        public async UniTask<bool> EnableAllAudioReactorsAsync()
        {
            if (_audioReactorManager == null)
            {
                Log.Error("AudioLyricCoordinator: AudioReactorManager未初始化");
                return false;
            }
            
            bool success = await _audioReactorManager.EnableAllReactorsAsync();
            
            if (_enableDebugger)
            {
                Log.Info($"AudioLyricCoordinator: 启用所有音频反应器 - {(success ? "成功" : "失败")}");
            }
            
            return success;
        }
        
        /// <summary>
        /// 禁用所有音频反应器
        /// </summary>
        /// <returns>禁用任务</returns>
        public async UniTask<bool> DisableAllAudioReactorsAsync()
        {
            if (_audioReactorManager == null)
            {
                Log.Error("AudioLyricCoordinator: AudioReactorManager未初始化");
                return false;
            }
            
            bool success = await _audioReactorManager.DisableAllReactorsAsync();
            
            if (_enableDebugger)
            {
                Log.Info($"AudioLyricCoordinator: 禁用所有音频反应器 - {(success ? "成功" : "失败")}");
            }
            
            return success;
        }
        
        /// <summary>
        /// 启用指定的音频反应器
        /// </summary>
        /// <param name="reactorId">反应器ID</param>
        /// <returns>启用任务</returns>
        public async UniTask<bool> EnableAudioReactorAsync(string reactorId)
        {
            if (_audioReactorManager == null)
            {
                Log.Error("AudioLyricCoordinator: AudioReactorManager未初始化");
                return false;
            }
            
            bool success = await _audioReactorManager.EnableReactorAsync(reactorId);
            
            if (_enableDebugger)
            {
                Log.Info($"AudioLyricCoordinator: 启用音频反应器 {reactorId} - {(success ? "成功" : "失败")}");
            }
            
            return success;
        }
        
        /// <summary>
        /// 禁用指定的音频反应器
        /// </summary>
        /// <param name="reactorId">反应器ID</param>
        /// <returns>禁用任务</returns>
        public async UniTask<bool> DisableAudioReactorAsync(string reactorId)
        {
            if (_audioReactorManager == null)
            {
                Log.Error("AudioLyricCoordinator: AudioReactorManager未初始化");
                return false;
            }
            
            bool success = await _audioReactorManager.DisableReactorAsync(reactorId);
            
            if (_enableDebugger)
            {
                Log.Info($"AudioLyricCoordinator: 禁用音频反应器 {reactorId} - {(success ? "成功" : "失败")}");
            }
            
            return success;
        }
        
        /// <summary>
        /// 内部初始化方法
        /// </summary>
        /// <returns>是否初始化成功</returns>
        private async UniTask<bool> InternalInitializeAsync()
        {
            
            try
            {
                if (_enableDebugger)
                {
                    Log.Info("AudioLyricCoordinator: 开始初始化协调器");
                }
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
                _globalAudioSource.clip = audioClip;
                _globalAudioSource.Play();
                return true;
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
                _globalAudioSource?.Stop();
                _lyricFX?.StopAll();
                
                _isPlaying = false;
                _isPaused = false;
                _pausedTime = 0f;
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
                _globalAudioSource?.Stop();
                _lyricFX?.StopAll();
                
                _isPlaying = false;
                _isPaused = false;
                _pausedTime = 0f;
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
            if (_isPlaying && !_isPaused)
            {
                _pausedTime = _globalAudioSource?.time ?? 0f;
                _globalAudioSource?.Pause();
                _lyricFX?.Pause(); // 需要LyricFX支持暂停功能
                _isPaused = true;
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioLyricCoordinator: 播放已暂停，位置: {_pausedTime:F2}秒");
                }
            }
        }
        
        /// <summary>
        /// 恢复播放
        /// </summary>
        public void Resume()
        {
            if (_isPlaying && _isPaused)
            {
                _globalAudioSource?.UnPause();
                _lyricFX?.Resume(); // 需要LyricFX支持恢复功能
                _isPaused = false;
                
                if (_enableDebugger)
                {
                    Log.Info($"AudioLyricCoordinator: 播放已恢复，位置: {_pausedTime:F2}秒");
                }
            }
        }
        
        ///// <summary>
        ///// 设置播放位置
        ///// </summary>
        ///// <param name="time">时间（秒）</param>
        //public void SetPlaybackTime(float time)
        //{
        //    if (_isPlaying)
        //    {
        //        _globalAudioSource?.cur;
        //        // 歌词系统时间设置需要根据LyricFX的API来实现
                
        //        if (_enableDebugger)
        //        {
        //            Log.Info($"AudioLyricCoordinator: 设置播放位置为 {time:F2} 秒");
        //        }
        //    }
        //}
        
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
                Log.Info("AudioLyricCoordinator: 音频异常停止事件");
            }
            
            // 当音频停止时，也停止歌词
            if (_isPlaying)
            {
                _lyricFX?.StopAll();
                _isPlaying = false;
                _isPaused = false;
                _pausedTime = 0f;
                OnPlaybackStopped?.Invoke();
            }
        }
        
        /// <summary>
        /// 处理音频播放完成事件
        /// </summary>
        private void HandleAudioCompleted()
        {
            if (_enableDebugger)
            {
                Log.Info("AudioLyricCoordinator: 音频播放完成事件");
            }
            
            // 当音频播放完成时，停止歌词并触发完成事件
            if (_isPlaying)
            {
                _lyricFX?.StopAll();
                _isPlaying = false;
                _isPaused = false;
                _pausedTime = 0f;
                OnPlaybackStopped?.Invoke();
            }
        }
        
        /// <summary>
        /// 获取当前播放状态
        /// </summary>
        /// <returns>是否正在播放</returns>
        public bool IsPlaying()
        {
            return _isPlaying && !_isPaused;
        }
        
        /// <summary>
        /// 获取当前暂停状态
        /// </summary>
        /// <returns>是否已暂停</returns>
        public bool IsPaused()
        {
            return _isPaused;
        }
        
        /// <summary>
        /// 获取当前播放时间
        /// </summary>
        /// <returns>当前播放时间（秒）</returns>
        public float GetCurrentTime()
        {
            return _globalAudioSource?.time ?? 0f;
        }
        
        /// <summary>
        /// 获取音频总时长
        /// </summary>
        /// <returns>音频总时长（秒）</returns>
        public float GetTotalTime()
        {
            return _globalAudioSource?.clip?.length ?? 0f;
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
                _globalAudioSource?.Stop();
                _lyricFX?.StopAll();
                _isPlaying = false;
                _isPaused = false;
                OnPlaybackStopped?.Invoke();
            }
            
            // 清理播放状态
            _currentAudioName = "";
            _currentLyricContent = "";
            _syncOffset = 0f;
            _pausedTime = 0f;
            
            // 取消所有异步操作
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }
            
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
                _isInitialized = false;
            }
            

            
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