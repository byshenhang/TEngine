using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 字幕时序控制器 - 负责管理字幕的播放时机和同步
    /// </summary>
    public class SubtitleTimingController : IDisposable
    {
        private readonly List<TimedSubtitleEntry> _timedEntries;
        private readonly Dictionary<string, ISubtitleSequence> _activeSequences;
        private bool _isPlaying;
        private bool _isPaused;
        private float _currentTime;
        private float _timeScale = 1f;
        private bool _isDisposed;
        
        /// <summary>
        /// 当前播放时间
        /// </summary>
        public float CurrentTime => _currentTime;
        
        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying => _isPlaying;
        
        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused => _isPaused;
        
        /// <summary>
        /// 时间缩放
        /// </summary>
        public float TimeScale
        {
            get => _timeScale;
            set => _timeScale = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// 字幕开始事件
        /// </summary>
        public event Action<string, ISubtitleSequence> OnSubtitleStarted;
        
        /// <summary>
        /// 字幕结束事件
        /// </summary>
        public event Action<string, ISubtitleSequence> OnSubtitleCompleted;
        
        /// <summary>
        /// 时间更新事件
        /// </summary>
        public event Action<float> OnTimeUpdated;
        
        public SubtitleTimingController()
        {
            _timedEntries = new List<TimedSubtitleEntry>();
            _activeSequences = new Dictionary<string, ISubtitleSequence>();
        }
        
        /// <summary>
        /// 添加定时字幕条目
        /// </summary>
        public void AddTimedEntry(string id, float startTime, float duration, ISubtitleSequence sequence)
        {
            if (string.IsNullOrEmpty(id))
            {
                Log.Warning("[SubtitleTimingController] 字幕ID不能为空");
                return;
            }
            
            if (sequence == null)
            {
                Log.Warning($"[SubtitleTimingController] 字幕序列不能为空: {id}");
                return;
            }
            
            var entry = new TimedSubtitleEntry
            {
                Id = id,
                StartTime = startTime,
                Duration = duration,
                EndTime = startTime + duration,
                Sequence = sequence,
                IsTriggered = false
            };
            
            _timedEntries.Add(entry);
            
            // 按开始时间排序
            _timedEntries.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            
            Log.Debug($"[SubtitleTimingController] 添加定时字幕: {id}, 开始时间: {startTime}, 持续时间: {duration}");
        }
        
        /// <summary>
        /// 移除定时字幕条目
        /// </summary>
        public void RemoveTimedEntry(string id)
        {
            for (int i = _timedEntries.Count - 1; i >= 0; i--)
            {
                if (_timedEntries[i].Id == id)
                {
                    var entry = _timedEntries[i];
                    
                    // 如果正在播放，先停止
                    if (_activeSequences.ContainsKey(id))
                    {
                        StopSubtitle(id);
                    }
                    
                    _timedEntries.RemoveAt(i);
                    Log.Debug($"[SubtitleTimingController] 移除定时字幕: {id}");
                    break;
                }
            }
        }
        
        /// <summary>
        /// 清空所有定时条目
        /// </summary>
        public void ClearAllEntries()
        {
            StopAll();
            _timedEntries.Clear();
            Log.Info("[SubtitleTimingController] 清空所有定时字幕");
        }
        
        /// <summary>
        /// 开始播放
        /// </summary>
        public void Play()
        {
            _isPlaying = true;
            _isPaused = false;
            Log.Info("[SubtitleTimingController] 开始播放");
        }
        
        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
            
            // 暂停所有活动的字幕序列
            foreach (var sequence in _activeSequences.Values)
            {
                sequence.Pause();
            }
            
            Log.Info("[SubtitleTimingController] 暂停播放");
        }
        
        /// <summary>
        /// 恢复播放
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
            
            // 恢复所有活动的字幕序列
            foreach (var sequence in _activeSequences.Values)
            {
                sequence.Resume();
            }
            
            Log.Info("[SubtitleTimingController] 恢复播放");
        }
        
        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _isPaused = false;
            _currentTime = 0f;
            
            StopAll();
            
            // 重置所有条目的触发状态
            foreach (var entry in _timedEntries)
            {
                entry.IsTriggered = false;
            }
            
            Log.Info("[SubtitleTimingController] 停止播放");
        }
        
        /// <summary>
        /// 跳转到指定时间
        /// </summary>
        public void SeekTo(float time)
        {
            float previousTime = _currentTime;
            _currentTime = Mathf.Max(0f, time);
            
            // 停止所有当前活动的字幕
            StopAll();
            
            // 重置所有条目的触发状态
            foreach (var entry in _timedEntries)
            {
                entry.IsTriggered = false;
            }
            
            // 如果正在播放，立即处理当前时间点的字幕
            if (_isPlaying && !_isPaused)
            {
                ProcessTimedEntries();
            }
            
            OnTimeUpdated?.Invoke(_currentTime);
            Log.Debug($"[SubtitleTimingController] 跳转到时间: {time}");
        }
        
        /// <summary>
        /// 更新时序控制器
        /// </summary>
        public void Update()
        {
            if (_isDisposed || !_isPlaying || _isPaused) return;
            
            // 更新当前时间
            _currentTime += Time.deltaTime * _timeScale;
            
            // 处理定时条目
            ProcessTimedEntries();
            
            // 检查已完成的字幕
            CheckCompletedSubtitles();
            
            OnTimeUpdated?.Invoke(_currentTime);
        }
        
        /// <summary>
        /// 处理定时条目
        /// </summary>
        private void ProcessTimedEntries()
        {
            foreach (var entry in _timedEntries)
            {
                // 检查是否应该开始播放
                if (!entry.IsTriggered && _currentTime >= entry.StartTime)
                {
                    StartSubtitle(entry);
                }
                
                // 检查是否应该停止播放
                if (entry.IsTriggered && _currentTime >= entry.EndTime)
                {
                    if (_activeSequences.ContainsKey(entry.Id))
                    {
                        StopSubtitle(entry.Id);
                    }
                }
            }
        }
        
        /// <summary>
        /// 开始播放字幕
        /// </summary>
        private async void StartSubtitle(TimedSubtitleEntry entry)
        {
            if (entry.IsTriggered) return;
            
            entry.IsTriggered = true;
            _activeSequences[entry.Id] = entry.Sequence;
            
            try
            {
                OnSubtitleStarted?.Invoke(entry.Id, entry.Sequence);
                await entry.Sequence.PlayAsync();
                
                Log.Debug($"[SubtitleTimingController] 字幕播放完成: {entry.Id}");
            }
            catch (Exception ex)
            {
                Log.Error($"[SubtitleTimingController] 字幕播放失败: {entry.Id}, 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 停止字幕播放
        /// </summary>
        private void StopSubtitle(string id)
        {
            if (_activeSequences.TryGetValue(id, out var sequence))
            {
                sequence.Stop();
                _activeSequences.Remove(id);
                
                OnSubtitleCompleted?.Invoke(id, sequence);
                Log.Debug($"[SubtitleTimingController] 停止字幕: {id}");
            }
        }
        
        /// <summary>
        /// 停止所有字幕
        /// </summary>
        private void StopAll()
        {
            var activeIds = new List<string>(_activeSequences.Keys);
            foreach (var id in activeIds)
            {
                StopSubtitle(id);
            }
        }
        
        /// <summary>
        /// 检查已完成的字幕
        /// </summary>
        private void CheckCompletedSubtitles()
        {
            var completedIds = new List<string>();
            
            foreach (var kvp in _activeSequences)
            {
                if (kvp.Value.IsCompleted)
                {
                    completedIds.Add(kvp.Key);
                }
            }
            
            foreach (var id in completedIds)
            {
                StopSubtitle(id);
            }
        }
        
        /// <summary>
        /// 获取指定时间范围内的字幕条目
        /// </summary>
        public List<TimedSubtitleEntry> GetEntriesInTimeRange(float startTime, float endTime)
        {
            var result = new List<TimedSubtitleEntry>();
            
            foreach (var entry in _timedEntries)
            {
                if (entry.StartTime <= endTime && entry.EndTime >= startTime)
                {
                    result.Add(entry);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取活动字幕数量
        /// </summary>
        public int GetActiveSubtitleCount()
        {
            return _activeSequences.Count;
        }
        
        /// <summary>
        /// 获取总字幕数量
        /// </summary>
        public int GetTotalSubtitleCount()
        {
            return _timedEntries.Count;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            
            Stop();
            _timedEntries.Clear();
            
            OnSubtitleStarted = null;
            OnSubtitleCompleted = null;
            OnTimeUpdated = null;
            
            _isDisposed = true;
            Log.Info("[SubtitleTimingController] 已释放");
        }
    }
    
    /// <summary>
    /// 定时字幕条目
    /// </summary>
    public class TimedSubtitleEntry
    {
        public string Id { get; set; }
        public float StartTime { get; set; }
        public float Duration { get; set; }
        public float EndTime { get; set; }
        public ISubtitleSequence Sequence { get; set; }
        public bool IsTriggered { get; set; }
        
        public override string ToString()
        {
            return $"TimedSubtitleEntry[{Id}]: {StartTime}s - {EndTime}s ({Duration}s)";
        }
    }
}