using System;
using System.Collections.Generic;
using System.Threading;
using TEngine;
using UnityEngine;
using Cysharp.Threading.Tasks;
using LyricFX.Managers;
using LyricFX.Utils;

namespace GameLogic
{
    /// <summary>
    /// LyricFX封装模块 - 提供对歌词特效系统的统一访问接口
    /// </summary>
    public class LyricFXModule : Singleton<LyricFXModule>, IUpdate
    {
        // 当前LyricManager实例
        private LyricManager _lyricManager;
        
        // 活动的歌词行ID
        private int _activeLyricLineId = -1;
        
        // 默认效果和布局ID
        private string _defaultEffectId = "default_fade";
        private string _defaultLayoutId = "default_linear";
        
        // 调试设置
        private bool _enableDebugger = false;
        private float _syncOffset = 0.0f;
        
        // 取消令牌源
        private CancellationTokenSource _cts;
        
        protected override void OnInit()
        {
            base.OnInit();
            _cts = new CancellationTokenSource();
            Log.Info("LyricFXModule initialized");

            _lyricManager = new LyricManager();

            try
            {
                if (_lyricManager == null)
                {
                    Log.Error("LyricFXModule: 尚未设置LyricManager实例");
                    return ;
                }

                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.StartSession("初始化歌词管理器");
                    LyricFXDebugger.Instance.RecordTimePoint("开始初始化");
                }

                _lyricManager.Initialize().Forget();

                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("初始化完成");
                    LyricFXDebugger.Instance.EndSession("初始化成功");
                }

                Log.Info("LyricFXModule: 初始化成功");
                return ;
            }
            catch (Exception ex)
            {
                Log.Error($"LyricFXModule: 初始化失败: {ex}");

                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordError("初始化失败", ex);
                    LyricFXDebugger.Instance.EndSession("初始化失败");
                }

                return ;
            }
        }

        public override void Release()
        {
            base.Release();
            
            // 停止所有活动
            if (_lyricManager != null)
            {
                _lyricManager.StopAll();
                _activeLyricLineId = -1;
            }
            
            // 清理资源
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            
            _lyricManager = null;
            
            Log.Info("LyricFXModule released");
        }

        public void OnUpdate()
        {
            // 在需要时可以在此添加每帧更新逻辑
        }
        
        /// <summary>
        /// 设置LyricManager实例
        /// </summary>
        /// <param name="manager">LyricManager实例</param>
        public void SetLyricManager(LyricManager manager)
        {
            if (manager == null) return;
            
            _lyricManager = manager;
            
            // 配置同步偏移
            if (_syncOffset != 0)
            {
                _lyricManager.SetSyncOffset(_syncOffset);
            }
            
            Log.Info("LyricFXModule: 歌词管理器已设置");
        }
        
        /// <summary>
        /// 获取当前LyricManager实例
        /// </summary>
        /// <returns>LyricManager实例，如果未设置则返回null</returns>
        public LyricManager GetLyricManager()
        {
            return _lyricManager;
        }
        
        /// <summary>
        /// 启用或禁用调试模式
        /// </summary>
        /// <param name="enable">是否启用调试</param>
        public void EnableDebugger(bool enable)
        {
            _enableDebugger = enable;
            LyricFXDebugger.Instance.EnableDebug = enable;
            
            if (enable)
            {
                Log.Info("LyricFXModule: 调试模式已启用，将记录详细执行时间");
            }
            else
            {
                Log.Info("LyricFXModule: 调试模式已禁用");
            }
        }
        
        /// <summary>
        /// 设置歌词同步偏移
        /// </summary>
        /// <param name="offset">偏移量（秒）</param>
        public void SetSyncOffset(float offset)
        {
            _syncOffset = offset;
            
            if (_lyricManager != null)
            {
                _lyricManager.SetSyncOffset(_syncOffset);
            }
            
            Log.Info($"LyricFXModule: 设置歌词同步偏移为 {_syncOffset}秒");
        }
        
        /// <summary>
        /// 设置默认效果ID
        /// </summary>
        /// <param name="effectId">效果ID</param>
        public void SetDefaultEffect(string effectId)
        {
            if (!string.IsNullOrEmpty(effectId))
            {
                _defaultEffectId = effectId;
            }
        }
        
        /// <summary>
        /// 设置默认布局ID
        /// </summary>
        /// <param name="layoutId">布局ID</param>
        public void SetDefaultLayout(string layoutId)
        {
            if (!string.IsNullOrEmpty(layoutId))
            {
                _defaultLayoutId = layoutId;
            }
        }
        
        /// <summary>
        /// 创建单行歌词
        /// </summary>
        /// <param name="lyric">歌词文本</param>
        /// <param name="position">位置</param>
        /// <param name="effectId">特效ID，为空则使用默认特效</param>
        /// <param name="layoutId">布局ID，为空则使用默认布局</param>
        /// <returns>歌词行ID，失败则返回-1</returns>
        public async UniTask<int> CreateLyricLine(string lyric, Vector3 position, string effectId = null, string layoutId = null)
        {
            if (_lyricManager == null)
            {
                Log.Error("LyricFXModule: 尚未设置LyricManager实例");
                return -1;
            }
            
            if (string.IsNullOrEmpty(lyric))
            {
                Log.Warning("LyricFXModule: 无法创建空歌词");
                return -1;
            }
            
            // 使用默认值（如果未提供）
            string finalEffectId = !string.IsNullOrEmpty(effectId) ? effectId : _defaultEffectId;
            string finalLayoutId = !string.IsNullOrEmpty(layoutId) ? layoutId : _defaultLayoutId;
            
            try
            {
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.StartSession("单行歌词创建");
                    LyricFXDebugger.Instance.SetCurrentLyric(lyric);
                    LyricFXDebugger.Instance.RecordTimePoint("开始创建歌词行");
                }
                
                int lineId = await _lyricManager.CreateLyricLine(lyric, finalLayoutId, finalEffectId, position);
                
                if (lineId >= 0)
                {
                    _activeLyricLineId = lineId;
                    
                    if (_enableDebugger)
                    {
                        LyricFXDebugger.Instance.RecordTimePoint("歌词行创建完成");
                        LyricFXDebugger.Instance.EndSession("歌词行创建成功");
                    }
                    
                    Log.Info($"LyricFXModule: 歌词行创建成功, ID: {lineId}");
                }
                else if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordError("歌词行创建失败");
                    LyricFXDebugger.Instance.EndSession("歌词行创建失败");
                }
                
                return lineId;
            }
            catch (Exception ex)
            {
                Log.Error($"LyricFXModule: 创建歌词行失败: {ex}");
                
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordError("创建歌词行失败", ex);
                    LyricFXDebugger.Instance.EndSession("创建歌词行失败");
                }
                
                return -1;
            }
        }
        
        /// <summary>
        /// 播放歌词行
        /// </summary>
        /// <param name="lineId">歌词行ID，为-1则使用当前活动的歌词行</param>
        /// <returns>是否播放成功</returns>
        public async UniTask<bool> PlayLyricLine(int lineId = -1)
        {
            if (_lyricManager == null)
            {
                Log.Error("LyricFXModule: 尚未设置LyricManager实例");
                return false;
            }
            
            // 如果未提供行ID，使用当前活动的歌词行
            int targetLineId = lineId >= 0 ? lineId : _activeLyricLineId;
            
            if (targetLineId < 0)
            {
                Log.Warning("LyricFXModule: 无效的歌词行ID");
                return false;
            }
            
            try
            {
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.StartSession("歌词行播放");
                    LyricFXDebugger.Instance.RecordTimePoint("开始播放歌词行");
                }
                
                await _lyricManager.PlayLyricLine(targetLineId);
                
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("歌词行播放完成");
                    LyricFXDebugger.Instance.EndSession("歌词行播放成功");
                }
                
                Log.Info($"LyricFXModule: 歌词行播放成功, ID: {targetLineId}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"LyricFXModule: 播放歌词行失败: {ex}");
                
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordError("播放歌词行失败", ex);
                    LyricFXDebugger.Instance.EndSession("播放歌词行失败");
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// 停止歌词行
        /// </summary>
        /// <param name="lineId">歌词行ID，为-1则使用当前活动的歌词行</param>
        /// <returns>是否停止成功</returns>
        public async UniTask<bool> StopLyricLine(int lineId = -1)
        {
            if (_lyricManager == null)
            {
                Log.Error("LyricFXModule: 尚未设置LyricManager实例");
                return false;
            }
            
            // 如果未提供行ID，使用当前活动的歌词行
            int targetLineId = lineId >= 0 ? lineId : _activeLyricLineId;
            
            if (targetLineId < 0)
            {
                Log.Warning("LyricFXModule: 无效的歌词行ID");
                return false;
            }
            
            try
            {
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.StartSession("停止歌词行");
                    LyricFXDebugger.Instance.RecordTimePoint("开始停止歌词行");
                }
                
                await _lyricManager.StopLyricLine(targetLineId);
                
                // 如果停止的是当前活动的歌词行，重置活动行ID
                if (targetLineId == _activeLyricLineId)
                {
                    _activeLyricLineId = -1;
                }
                
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("歌词行停止完成");
                    LyricFXDebugger.Instance.EndSession("歌词行停止成功");
                }
                
                Log.Info($"LyricFXModule: 歌词行停止成功, ID: {targetLineId}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"LyricFXModule: 停止歌词行失败: {ex}");
                
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordError("停止歌词行失败", ex);
                    LyricFXDebugger.Instance.EndSession("停止歌词行失败");
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// 停止所有歌词活动
        /// </summary>
        public void StopAll()
        {
            if (_lyricManager != null)
            {
                _lyricManager.StopAll();
                _activeLyricLineId = -1;
                Log.Info("LyricFXModule: 已停止所有歌词活动");
            }
        }
        
        /// <summary>
        /// 播放LRC文件
        /// </summary>
        /// <param name="lrcContent">LRC文件内容</param>
        /// <param name="audioSource">音频源，可选，如果提供了音频源，将自动开始播放</param>
        /// <param name="audioStartDelay">音频开始播放的延迟时间（秒），默认0.1秒</param>
        /// <param name="effectId">特效ID，为空则使用默认特效</param>
        /// <param name="layoutId">布局ID，为空则使用默认布局</param>
        /// <returns>是否播放成功</returns>
        public async UniTask<bool> PlayLrcFile(string lrcContent, UnityEngine.AudioSource audioSource = null, float audioStartDelay = 0.1f, string effectId = null, string layoutId = null)
        {
            if (_lyricManager == null)
            {
                Log.Error("LyricFXModule: 尚未设置LyricManager实例");
                return false;
            }
            
            if (string.IsNullOrEmpty(lrcContent))
            {
                Log.Warning("LyricFXModule: LRC内容为空");
                return false;
            }
            
            // 使用默认值（如果未提供）
            string finalEffectId = !string.IsNullOrEmpty(effectId) ? effectId : _defaultEffectId;
            string finalLayoutId = !string.IsNullOrEmpty(layoutId) ? layoutId : _defaultLayoutId;
            
            try
            {
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.StartSession("LRC文件播放");
                    LyricFXDebugger.Instance.RecordTimePoint("开始处理LRC");
                }
                
                // 开始处理LRC
                var processTask = _lyricManager.PlayLrcFile(lrcContent, finalLayoutId, finalEffectId);
                
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("LRC处理已开始");
                }
                
                // 如果提供了音频源，则延迟播放音频
                if (audioSource != null)
                {
                    // 延迟音频开始，以便为初始处理留出时间
                    await UniTask.Delay(TimeSpan.FromSeconds(audioStartDelay), cancellationToken: _cts.Token);
                    
                    if (_enableDebugger)
                    {
                        LyricFXDebugger.Instance.RecordTimePoint("音频开始播放");
                    }
                    
                    // 开始播放音频
                    audioSource.Play();
                }
                
                // 等待LRC处理完成
                await processTask;
                
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordTimePoint("LRC播放完成");
                    LyricFXDebugger.Instance.EndSession("LRC文件播放完成");
                }
                
                Log.Info("LyricFXModule: LRC文件播放成功");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"LyricFXModule: 播放LRC文件失败: {ex}");
                
                if (_enableDebugger)
                {
                    LyricFXDebugger.Instance.RecordError("播放LRC文件失败", ex);
                    LyricFXDebugger.Instance.EndSession("LRC播放出错");
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// 从文本资源加载LRC并播放
        /// </summary>
        /// <param name="lrcTextAsset">LRC文本资源</param>
        /// <param name="audioSource">音频源，可选</param>
        /// <param name="audioStartDelay">音频开始播放的延迟时间（秒）</param>
        /// <param name="effectId">特效ID</param>
        /// <param name="layoutId">布局ID</param>
        /// <returns>是否播放成功</returns>
        public async UniTask<bool> PlayLrcFromTextAsset(UnityEngine.TextAsset lrcTextAsset, UnityEngine.AudioSource audioSource = null, float audioStartDelay = 0.1f, string effectId = null, string layoutId = null)
        {
            if (lrcTextAsset == null)
            {
                Log.Error("LyricFXModule: LRC文本资源为空");
                return false;
            }
            
            return await PlayLrcFile(lrcTextAsset.text, audioSource, audioStartDelay, effectId, layoutId);
        }
    
    
    
    }
}
