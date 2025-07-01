using System;
using System.Collections.Generic;
using System.Threading;
using TEngine;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TelePresent.AudioSyncPro;

namespace GameLogic
{
    /// <summary>
    /// Audio Sync Pro封装模块 - 提供对音频反应系统的统一访问接口
    /// </summary>
    public class AudioSyncModule : Singleton<AudioSyncModule>, IUpdate
    {
        // 当前AudioReactor实例
        private TelePresent.AudioSyncPro.AudioReactor _audioReactor;

        // 当前AudioSourcePlus实例
        private AudioSourcePlus _audioSourcePlus;

        // 活动的反应组件列表
        private List<ASP_IAudioReaction> _activeReactions = new List<ASP_IAudioReaction>();

        // 标记系统
        private List<ASP_Marker> _markers = new List<ASP_Marker>();

        // 调试设置
        private bool _enableDebugger = false;
        private bool _isInitialized = false;

        // 取消令牌源
        private CancellationTokenSource _cts;

        // 音频状态
        private bool _isPlaying = false;
        private float _currentTime = 0f;

        // 事件
        public event Action OnAudioStarted;
        public event Action OnAudioStopped;
        public event Action<ASP_Marker> OnMarkerTriggered;
        public event Action<float, float[]> OnAudioDataUpdated; // RMS值和频谱数据

        protected override void OnInit()
        {
            base.OnInit();
            _cts = new CancellationTokenSource();
            Log.Info("AudioSyncModule initialized");
        }

        public override void Release()
        {
            base.Release();

            // 停止所有音频活动
            StopAudio();

            // 清理事件订阅
            if (_audioSourcePlus != null)
            {
                _audioSourcePlus.OnAudioStarted -= HandleAudioStarted;
                _audioSourcePlus.OnAudioStopped -= HandleAudioStopped;
            }

            // 清理资源
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _audioReactor = null;
            _audioSourcePlus = null;
            _activeReactions.Clear();
            _markers.Clear();

            Log.Info("AudioSyncModule released");
        }

        public void OnUpdate()
        {
            if (!_isInitialized || _audioSourcePlus == null) return;

            // 更新当前播放时间
            if (_isPlaying && _audioSourcePlus.audioSource != null)
            {
                _currentTime = _audioSourcePlus.audioSource.time;

                // 检查标记触发
                CheckMarkerTriggers();

                // 触发音频数据更新事件
                if (_audioSourcePlus.rmsValue > 0 || _audioSourcePlus.spectrumData != null)
                {
                    OnAudioDataUpdated?.Invoke(_audioSourcePlus.rmsValue, _audioSourcePlus.spectrumData);
                }
            }
        }

        /// <summary>
        /// 初始化Audio Sync模块
        /// </summary>
        /// <param name="audioReactor">AudioReactor实例</param>
        /// <param name="audioSourcePlus">AudioSourcePlus实例，可选</param>
        /// <returns>是否初始化成功</returns>
        public bool Initialize(TelePresent.AudioSyncPro.AudioReactor audioReactor, AudioSourcePlus audioSourcePlus = null)
        {
            if (audioReactor == null)
            {
                Log.Error("AudioSyncModule: AudioReactor不能为空");
                return false;
            }

            try
            {
                if (_enableDebugger)
                {
                    Log.Info("AudioSyncModule: 开始初始化音频反应系统");
                }

                _audioReactor = audioReactor;

                // 如果没有提供AudioSourcePlus，尝试从AudioReactor获取
                if (audioSourcePlus != null)
                {
                    _audioSourcePlus = audioSourcePlus;
                }
                else if (_audioReactor.audioSourcePlus != null)
                {
                    _audioSourcePlus = _audioReactor.audioSourcePlus;
                }
                else
                {
                    Log.Warning("AudioSyncModule: 未找到AudioSourcePlus实例");
                    return false;
                }

                // 订阅音频事件
                _audioSourcePlus.OnAudioStarted += HandleAudioStarted;
                _audioSourcePlus.OnAudioStopped += HandleAudioStopped;

                // 获取现有的反应组件
                RefreshReactionComponents();

                _isInitialized = true;

                if (_enableDebugger)
                {
                    Log.Info($"AudioSyncModule: 初始化成功，找到 {_activeReactions.Count} 个反应组件");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioSyncModule: 初始化失败: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 刷新反应组件列表
        /// </summary>
        public void RefreshReactionComponents()
        {
            if (_audioReactor == null) return;

            _activeReactions.Clear();

            // 从AudioReactor获取反应组件
            if (_audioReactor.reactionComponents != null)
            {
                foreach (var component in _audioReactor.reactionComponents)
                {
                    if (component is ASP_IAudioReaction reaction)
                    {
                        _activeReactions.Add(reaction);
                    }
                }
            }

            if (_enableDebugger)
            {
                Log.Info($"AudioSyncModule: 刷新反应组件，当前有 {_activeReactions.Count} 个活动组件");
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
                Log.Info("AudioSyncModule: 调试模式已启用");
            }
            else
            {
                Log.Info("AudioSyncModule: 调试模式已禁用");
            }
        }

        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="audioClip">音频剪辑，可选</param>
        /// <returns>是否播放成功</returns>
        public bool PlayAudio(AudioClip audioClip = null)
        {
            if (!_isInitialized || _audioSourcePlus == null)
            {
                Log.Error("AudioSyncModule: 模块未初始化或AudioSourcePlus为空");
                return false;
            }

            try
            {
                if (audioClip != null)
                {
                    _audioSourcePlus.audioSource.clip = audioClip;
                }

                if (_audioSourcePlus.audioSource.clip == null)
                {
                    Log.Warning("AudioSyncModule: 没有设置音频剪辑");
                    return false;
                }

                _audioSourcePlus.audioSource.Play();

                if (_enableDebugger)
                {
                    Log.Info($"AudioSyncModule: 开始播放音频: {_audioSourcePlus.audioSource.clip.name}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"AudioSyncModule: 播放音频失败: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 停止音频播放
        /// </summary>
        public void StopAudio()
        {
            if (_audioSourcePlus?.audioSource != null)
            {
                _audioSourcePlus.audioSource.Stop();

                if (_enableDebugger)
                {
                    Log.Info("AudioSyncModule: 音频播放已停止");
                }
            }
        }

        /// <summary>
        /// 暂停音频播放
        /// </summary>
        public void PauseAudio()
        {
            if (_audioSourcePlus?.audioSource != null)
            {
                _audioSourcePlus.audioSource.Pause();

                if (_enableDebugger)
                {
                    Log.Info("AudioSyncModule: 音频播放已暂停");
                }
            }
        }

        /// <summary>
        /// 恢复音频播放
        /// </summary>
        public void ResumeAudio()
        {
            if (_audioSourcePlus?.audioSource != null)
            {
                _audioSourcePlus.audioSource.UnPause();

                if (_enableDebugger)
                {
                    Log.Info("AudioSyncModule: 音频播放已恢复");
                }
            }
        }

        /// <summary>
        /// 设置音频播放位置
        /// </summary>
        /// <param name="time">时间（秒）</param>
        public void SetAudioTime(float time)
        {
            if (_audioSourcePlus?.audioSource != null)
            {
                _audioSourcePlus.audioSource.time = Mathf.Clamp(time, 0f, _audioSourcePlus.audioSource.clip.length);
                _currentTime = _audioSourcePlus.audioSource.time;

                if (_enableDebugger)
                {
                    Log.Info($"AudioSyncModule: 设置音频播放位置为 {time:F2} 秒");
                }
            }
        }

        /// <summary>
        /// 添加标记
        /// </summary>
        /// <param name="time">标记时间</param>
        /// <param name="name">标记名称</param>
        /// <param name="duration">效果持续时间</param>
        /// <returns>创建的标记</returns>
        public ASP_Marker AddMarker(float time, string name = "New Marker", float duration = 1f)
        {
            if (_audioSourcePlus == null)
            {
                Log.Warning("AudioSyncModule: AudioSourcePlus为空，无法添加标记");
                return null;
            }

            var marker = new ASP_Marker(_audioSourcePlus)
            {
                Time = time,
                MarkerName = name,
                EffectDuration = duration
            };

            _markers.Add(marker);

            if (_enableDebugger)
            {
                Log.Info($"AudioSyncModule: 添加标记 '{name}' 在时间 {time:F2} 秒");
            }

            return marker;
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        /// <param name="marker">要移除的标记</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveMarker(ASP_Marker marker)
        {
            if (marker == null) return false;

            bool removed = _markers.Remove(marker);

            if (removed && _enableDebugger)
            {
                Log.Info($"AudioSyncModule: 移除标记 '{marker.MarkerName}'");
            }

            return removed;
        }

        /// <summary>
        /// 清除所有标记
        /// </summary>
        public void ClearMarkers()
        {
            int count = _markers.Count;
            _markers.Clear();

            if (_enableDebugger)
            {
                Log.Info($"AudioSyncModule: 清除了 {count} 个标记");
            }
        }

        /// <summary>
        /// 获取当前音频时间
        /// </summary>
        /// <returns>当前播放时间（秒）</returns>
        public float GetCurrentTime()
        {
            return _currentTime;
        }

        /// <summary>
        /// 获取音频总长度
        /// </summary>
        /// <returns>音频总长度（秒），如果没有音频则返回0</returns>
        public float GetAudioLength()
        {
            if (_audioSourcePlus?.audioSource?.clip != null)
            {
                return _audioSourcePlus.audioSource.clip.length;
            }
            return 0f;
        }

        /// <summary>
        /// 获取当前RMS值
        /// </summary>
        /// <returns>RMS值</returns>
        public float GetRMSValue()
        {
            return _audioSourcePlus?.rmsValue ?? 0f;
        }

        /// <summary>
        /// 获取当前频谱数据
        /// </summary>
        /// <returns>频谱数据数组</returns>
        public float[] GetSpectrumData()
        {
            return _audioSourcePlus?.spectrumData;
        }

        /// <summary>
        /// 设置反应组件的激活状态
        /// </summary>
        /// <param name="reactionType">反应组件类型</param>
        /// <param name="active">是否激活</param>
        public void SetReactionActive<T>(bool active) where T : ASP_IAudioReaction
        {
            foreach (var reaction in _activeReactions)
            {
                if (reaction is T)
                {
                    reaction.IsActive = active;

                    if (_enableDebugger)
                    {
                        Log.Info($"AudioSyncModule: 设置反应组件 {typeof(T).Name} 激活状态为 {active}");
                    }
                }
            }
        }

        /// <summary>
        /// 设置所有反应组件的激活状态
        /// </summary>
        /// <param name="active">是否激活</param>
        public void SetAllReactionsActive(bool active)
        {
            foreach (var reaction in _activeReactions)
            {
                reaction.IsActive = active;
            }

            if (_enableDebugger)
            {
                Log.Info($"AudioSyncModule: 设置所有反应组件激活状态为 {active}");
            }
        }

        /// <summary>
        /// 获取指定类型的反应组件
        /// </summary>
        /// <typeparam name="T">反应组件类型</typeparam>
        /// <returns>反应组件列表</returns>
        public List<T> GetReactions<T>() where T : ASP_IAudioReaction
        {
            var results = new List<T>();
            foreach (var reaction in _activeReactions)
            {
                if (reaction is T typedReaction)
                {
                    results.Add(typedReaction);
                }
            }
            return results;
        }

        /// <summary>
        /// 处理音频开始事件
        /// </summary>
        private void HandleAudioStarted()
        {
            _isPlaying = true;

            if (_enableDebugger)
            {
                Log.Info("AudioSyncModule: 音频开始播放");
            }

            OnAudioStarted?.Invoke();
        }

        /// <summary>
        /// 处理音频停止事件
        /// </summary>
        private void HandleAudioStopped()
        {
            _isPlaying = false;

            if (_enableDebugger)
            {
                Log.Info("AudioSyncModule: 音频停止播放");
            }

            OnAudioStopped?.Invoke();
        }

        /// <summary>
        /// 检查标记触发
        /// </summary>
        private void CheckMarkerTriggers()
        {
            foreach (var marker in _markers)
            {
                if (!marker.IsTriggered && _currentTime >= marker.Time)
                {
                    marker.Trigger();
                    OnMarkerTriggered?.Invoke(marker);

                    if (_enableDebugger)
                    {
                        Log.Info($"AudioSyncModule: 触发标记 '{marker.MarkerName}' 在时间 {marker.Time:F2} 秒");
                    }
                }
            }
        }

        /// <summary>
        /// 重置所有标记状态
        /// </summary>
        public void ResetMarkers()
        {
            foreach (var marker in _markers)
            {
                marker.IsTriggered = false;
            }

            if (_enableDebugger)
            {
                Log.Info("AudioSyncModule: 重置所有标记状态");
            }
        }

        /// <summary>
        /// 获取所有标记
        /// </summary>
        /// <returns>标记列表</returns>
        public List<ASP_Marker> GetMarkers()
        {
            return new List<ASP_Marker>(_markers);
        }

        /// <summary>
        /// 获取AudioReactor实例
        /// </summary>
        /// <returns>AudioReactor实例</returns>
        public TelePresent.AudioSyncPro.AudioReactor GetAudioReactor()
        {
            return _audioReactor;
        }

        /// <summary>
        /// 获取AudioSourcePlus实例
        /// </summary>
        /// <returns>AudioSourcePlus实例</returns>
        public AudioSourcePlus GetAudioSourcePlus()
        {
            return _audioSourcePlus;
        }

        /// <summary>
        /// 检查模块是否已初始化
        /// </summary>
        /// <returns>是否已初始化</returns>
        public bool IsInitialized()
        {
            return _isInitialized;
        }

        /// <summary>
        /// 检查音频是否正在播放
        /// </summary>
        /// <returns>是否正在播放</returns>
        public bool IsPlaying()
        {
            return _isPlaying;
        }
    }
}