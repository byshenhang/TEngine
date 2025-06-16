using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using ChocDino.UIFX;

namespace GameLogic
{
    /// <summary>
    /// 歌词行管理类 - 负责管理单行歌词的字符创建和特效播放
    /// </summary>
    public class LyricLine : IDisposable
    {
        #region 字段定义
        
        private GameObject _lineGameObject;                     // 行游戏对象
        private LyricLineData _data;                           // 行数据
        private LyricConfig _config;                           // 配置
        private LyricModule _module;                           // 模块引用
        
        private readonly List<LyricCharacter> _characters = new List<LyricCharacter>(); // 字符列表
        private bool _isInitialized = false;                   // 是否已初始化
        private bool _isEnterEffectPlaying = false;           // 是否正在播放进入效果
        private bool _isExitEffectPlaying = false;            // 是否正在播放离开效果
        
        private CancellationTokenSource _enterEffectCts;      // 进入效果取消令牌
        private CancellationTokenSource _exitEffectCts;       // 离开效果取消令牌
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 行数据
        /// </summary>
        public LyricLineData Data => _data;
        
        /// <summary>
        /// 行游戏对象
        /// </summary>
        public GameObject GameObject => _lineGameObject;
        
        /// <summary>
        /// 字符列表
        /// </summary>
        public IReadOnlyList<LyricCharacter> Characters => _characters;
        
        /// <summary>
        /// 是否正在播放进入效果
        /// </summary>
        public bool IsEnterEffectPlaying => _isEnterEffectPlaying;
        
        /// <summary>
        /// 是否正在播放离开效果
        /// </summary>
        public bool IsExitEffectPlaying => _isExitEffectPlaying;
        
        #endregion
        
        #region 构造函数
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="lineGameObject">行游戏对象</param>
        /// <param name="data">行数据</param>
        /// <param name="config">配置</param>
        public LyricLine(GameObject lineGameObject, LyricLineData data, LyricConfig config)
        {
            _lineGameObject = lineGameObject;
            _data = data;
            _config = config;
        }
        
        #endregion
        
        #region 初始化和释放
        
        /// <summary>
        /// 初始化歌词行
        /// </summary>
        /// <param name="module">歌词模块引用</param>
        public async UniTask Initialize(LyricModule module)
        {
            _module = module;
            
            await CreateCharacters();
            SetupLayout();
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// 重新初始化歌词行（用于单行复用模式）
        /// </summary>
        /// <param name="newData">新的行数据</param>
        /// <param name="newConfig">新的配置</param>
        /// <param name="characterPool">字符对象池</param>
        public async UniTask Initialize(LyricLineData newData, LyricConfig newConfig, Queue<LyricCharacter> characterPool)
        {
            if (!_isInitialized)
                return;
                
            // 停止所有效果
            StopAllEffects();
            
            // 归还当前字符到对象池
            foreach (var character in _characters)
            {
                character.SetVisible(false);
                characterPool.Enqueue(character);
            }
            _characters.Clear();
            
            // 更新数据和配置
            _data = newData;
            _config = newConfig;
            
            // 重新创建字符
            await CreateCharacters();
            SetupLayout();
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StopAllEffects();
            
            // 归还字符到对象池
            foreach (var character in _characters)
            {
                _module?.ReturnCharacterToPool(character);
            }
            _characters.Clear();
            
            if (_lineGameObject != null)
            {
                UnityEngine.Object.Destroy(_lineGameObject);
                _lineGameObject = null;
            }
            
            _isInitialized = false;
        }
        
        #endregion
        
        #region 字符创建和布局
        
        /// <summary>
        /// 创建字符
        /// </summary>
        private async UniTask CreateCharacters()
        {
            if (string.IsNullOrEmpty(_data.Text))
                return;
            
            for (int i = 0; i < _data.Text.Length; i++)
            {
                var character = _module.GetCharacterFromPool();
                character.Initialize(_data.Text[i], i, _config);
                character.gameObject.transform.SetParent(_lineGameObject.transform, false);
                
                _characters.Add(character);
                
                // 初始状态设为不可见
                character.SetVisible(false);
            }
            
            await UniTask.Yield();
        }
        
        /// <summary>
        /// 设置布局
        /// </summary>
        private void SetupLayout()
        {
            float totalWidth = _characters.Count * (_config.FontSize + _config.CharacterSpacing);
            float startX = -totalWidth * 0.5f;
            
            for (int i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                var rectTransform = character.gameObject.GetComponent<RectTransform>();
                
                float x = startX + i * (_config.FontSize + _config.CharacterSpacing);
                rectTransform.anchoredPosition = new Vector2(x, 0);
            }
        }
        
        #endregion
        
        #region 效果播放控制
        
        /// <summary>
        /// 播放进入效果
        /// </summary>
        public void PlayEnterEffect()
        {
            if (!_isInitialized || _isEnterEffectPlaying)
                return;
            
            StopExitEffect();
            
            var effectConfig = _data.EffectConfig?.EnterEffect ?? _config.EnterEffect;
            if (effectConfig != null && effectConfig.EffectType != LyricEffectType.None)
            {
                _enterEffectCts = new CancellationTokenSource();
                PlayEnterEffectAsync(effectConfig, _enterEffectCts.Token).Forget();
            }
            else
            {
                // 没有效果配置，直接显示所有字符
                ShowAllCharacters();
            }
        }
        
        /// <summary>
        /// 播放离开效果
        /// </summary>
        public void PlayExitEffect()
        {
            if (!_isInitialized || _isExitEffectPlaying)
                return;
            
            StopEnterEffect();
            
            var effectConfig = _data.EffectConfig?.ExitEffect ?? _config.ExitEffect;
            if (effectConfig != null && effectConfig.EffectType != LyricEffectType.None)
            {
                _exitEffectCts = new CancellationTokenSource();
                PlayExitEffectAsync(effectConfig, _exitEffectCts.Token).Forget();
            }
            else
            {
                // 没有效果配置，直接隐藏所有字符
                HideAllCharacters();
            }
        }
        
        /// <summary>
        /// 更新字符效果
        /// </summary>
        /// <param name="characterTime">字符时间</param>
        public void UpdateCharacterEffects(float characterTime)
        {
            if (!_isInitialized || _isEnterEffectPlaying || _isExitEffectPlaying)
                return;
            
            var effectConfig = _data.EffectConfig?.CharacterEffect ?? _config.CharacterEffect;
            if (effectConfig == null || effectConfig.EffectType == LyricEffectType.None)
                return;
            
            // 计算每个字符的显示时间
            float characterInterval = effectConfig.Duration / _characters.Count;
            
            for (int i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                float charStartTime = i * characterInterval;
                float charEndTime = charStartTime + effectConfig.Duration;
                
                if (characterTime >= charStartTime && characterTime <= charEndTime)
                {
                    float t = (characterTime - charStartTime) / effectConfig.Duration;
                    character.UpdateEffect(effectConfig, t);
                }
                else if (characterTime > charEndTime)
                {
                    // 效果已完成，设置为最终状态
                    character.SetEffectComplete(effectConfig);
                }
            }
        }
        
        /// <summary>
        /// 停止所有效果
        /// </summary>
        public void StopAllEffects()
        {
            StopEnterEffect();
            StopExitEffect();
        }
        
        /// <summary>
        /// 停止进入效果
        /// </summary>
        private void StopEnterEffect()
        {
            if (_enterEffectCts != null)
            {
                _enterEffectCts.Cancel();
                _enterEffectCts.Dispose();
                _enterEffectCts = null;
            }
            _isEnterEffectPlaying = false;
        }
        
        /// <summary>
        /// 停止离开效果
        /// </summary>
        private void StopExitEffect()
        {
            if (_exitEffectCts != null)
            {
                _exitEffectCts.Cancel();
                _exitEffectCts.Dispose();
                _exitEffectCts = null;
            }
            _isExitEffectPlaying = false;
        }
        
        #endregion
        
        #region 效果异步方法
        
        /// <summary>
        /// 进入效果异步方法
        /// </summary>
        /// <param name="effectConfig">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async UniTask PlayEnterEffectAsync(LyricEffectConfig effectConfig, CancellationToken cancellationToken)
        {
            _isEnterEffectPlaying = true;
            
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(effectConfig.Delay), cancellationToken: cancellationToken);
                
                switch (effectConfig.EffectType)
                {
                    case LyricEffectType.BlurFade:
                        await PlayBlurFadeEffectAsync(effectConfig, true, cancellationToken);
                        break;
                        
                    case LyricEffectType.Fade:
                        await PlayFadeEffectAsync(effectConfig, true, cancellationToken);
                        break;
                        
                    case LyricEffectType.Scale:
                        await PlayScaleEffectAsync(effectConfig, true, cancellationToken);
                        break;
                        
                    case LyricEffectType.Move:
                        await PlayMoveEffectAsync(effectConfig, true, cancellationToken);
                        break;
                        
                    default:
                        ShowAllCharacters();
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消，正常情况
            }
            finally
            {
                _isEnterEffectPlaying = false;
            }
        }
        
        /// <summary>
        /// 离开效果异步方法
        /// </summary>
        /// <param name="effectConfig">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async UniTask PlayExitEffectAsync(LyricEffectConfig effectConfig, CancellationToken cancellationToken)
        {
            _isExitEffectPlaying = true;
            
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(effectConfig.Delay), cancellationToken: cancellationToken);
                
                switch (effectConfig.EffectType)
                {
                    case LyricEffectType.Fade:
                        await PlayFadeEffectAsync(effectConfig, false, cancellationToken);
                        break;
                        
                    case LyricEffectType.Scale:
                        await PlayScaleEffectAsync(effectConfig, false, cancellationToken);
                        break;
                        
                    case LyricEffectType.Move:
                        await PlayMoveEffectAsync(effectConfig, false, cancellationToken);
                        break;
                        
                    default:
                        HideAllCharacters();
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消，正常情况
            }
            finally
            {
                _isExitEffectPlaying = false;
            }
        }
        
        /// <summary>
        /// 播放模糊淡入淡出效果（基于原始代码的实现）
        /// </summary>
        /// <param name="effectConfig">效果配置</param>
        /// <param name="isEnter">是否为进入效果</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async UniTask PlayBlurFadeEffectAsync(LyricEffectConfig effectConfig, bool isEnter, CancellationToken cancellationToken)
        {
            // 等待1秒（模拟原始代码）
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);
            
            // 第一轮：偶数索引字符 (0, 2, 4...)
            for (int i = 0; i < _characters.Count; i += 2)
            {
                ActivateAndFadeCharacterAsync(i, effectConfig, cancellationToken).Forget();
                await WaitForBlurBelowThresholdAsync(_characters[i], effectConfig.BlurParams.BlurThreshold, cancellationToken);
            }
            
            // 第二轮：奇数索引字符 (1, 3, 5...)
            for (int i = 1; i < _characters.Count; i += 2)
            {
                ActivateAndFadeCharacterAsync(i, effectConfig, cancellationToken).Forget();
                await WaitForBlurBelowThresholdAsync(_characters[i], effectConfig.BlurParams.BlurThreshold, cancellationToken);
            }
        }
        
        /// <summary>
        /// 激活并淡入字符（基于原始代码）
        /// </summary>
        /// <param name="index">字符索引</param>
        /// <param name="effectConfig">效果配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async UniTask ActivateAndFadeCharacterAsync(int index, LyricEffectConfig effectConfig, CancellationToken cancellationToken)
        {
            if (index >= _characters.Count) return;
            
            var character = _characters[index];
            character.SetVisible(true);
            
            var blurFilter = character.GetBlurFilter();
            if (blurFilter != null)
            {
                blurFilter.Blur = effectConfig.BlurParams.StartBlur;
            }
            
            float elapsed = 0f;
            
            while (elapsed < effectConfig.Duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / effectConfig.Duration);
                float curveValue = effectConfig.Curve.Evaluate(t);
                
                if (blurFilter != null)
                {
                    blurFilter.Blur = Mathf.Lerp(effectConfig.BlurParams.StartBlur, effectConfig.BlurParams.EndBlur, curveValue);
                }
                
                await UniTask.Yield(cancellationToken);
            }
            
            if (blurFilter != null && !cancellationToken.IsCancellationRequested)
            {
                blurFilter.Blur = effectConfig.BlurParams.EndBlur;
            }
        }
        
        /// <summary>
        /// 等待模糊值低于阈值
        /// </summary>
        /// <param name="character">字符</param>
        /// <param name="threshold">阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async UniTask WaitForBlurBelowThresholdAsync(LyricCharacter character, float threshold, CancellationToken cancellationToken)
        {
            var blurFilter = character.GetBlurFilter();
            if (blurFilter == null) return;
            
            while (blurFilter.Blur > threshold && !cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(cancellationToken);
            }
        }
        
        /// <summary>
        /// 播放淡入淡出效果
        /// </summary>
        /// <param name="effectConfig">效果配置</param>
        /// <param name="isEnter">是否为进入效果</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async UniTask PlayFadeEffectAsync(LyricEffectConfig effectConfig, bool isEnter, CancellationToken cancellationToken)
        {
            float startAlpha = isEnter ? effectConfig.FadeParams.StartAlpha : effectConfig.FadeParams.EndAlpha;
            float endAlpha = isEnter ? effectConfig.FadeParams.EndAlpha : effectConfig.FadeParams.StartAlpha;
            
            // 显示所有字符
            if (isEnter)
            {
                ShowAllCharacters();
            }
            
            float elapsed = 0f;
            
            while (elapsed < effectConfig.Duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / effectConfig.Duration);
                float curveValue = effectConfig.Curve.Evaluate(t);
                float alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
                
                foreach (var character in _characters)
                {
                    character.SetAlpha(alpha);
                }
                
                await UniTask.Yield(cancellationToken);
            }
            
            // 设置最终透明度
            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (var character in _characters)
                {
                    character.SetAlpha(endAlpha);
                }
                
                if (!isEnter && endAlpha <= 0f)
                {
                    HideAllCharacters();
                }
            }
        }
        
        /// <summary>
        /// 播放缩放效果
        /// </summary>
        /// <param name="effectConfig">效果配置</param>
        /// <param name="isEnter">是否为进入效果</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async UniTask PlayScaleEffectAsync(LyricEffectConfig effectConfig, bool isEnter, CancellationToken cancellationToken)
        {
            Vector3 startScale = isEnter ? effectConfig.ScaleParams.StartScale : effectConfig.ScaleParams.EndScale;
            Vector3 endScale = isEnter ? effectConfig.ScaleParams.EndScale : effectConfig.ScaleParams.StartScale;
            
            // 显示所有字符
            if (isEnter)
            {
                ShowAllCharacters();
            }
            
            float elapsed = 0f;
            
            while (elapsed < effectConfig.Duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / effectConfig.Duration);
                float curveValue = effectConfig.Curve.Evaluate(t);
                Vector3 scale = Vector3.Lerp(startScale, endScale, curveValue);
                
                foreach (var character in _characters)
                {
                    character.SetScale(scale);
                }
                
                await UniTask.Yield(cancellationToken);
            }
            
            // 设置最终缩放
            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (var character in _characters)
                {
                    character.SetScale(endScale);
                }
                
                if (!isEnter && endScale == Vector3.zero)
                {
                    HideAllCharacters();
                }
            }
        }
        
        /// <summary>
        /// 播放移动效果
        /// </summary>
        /// <param name="effectConfig">效果配置</param>
        /// <param name="isEnter">是否为进入效果</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async UniTask PlayMoveEffectAsync(LyricEffectConfig effectConfig, bool isEnter, CancellationToken cancellationToken)
        {
            Vector3 startPos = isEnter ? effectConfig.MoveParams.StartPosition : effectConfig.MoveParams.EndPosition;
            Vector3 endPos = isEnter ? effectConfig.MoveParams.EndPosition : effectConfig.MoveParams.StartPosition;
            
            // 显示所有字符
            if (isEnter)
            {
                ShowAllCharacters();
            }
            
            // 保存原始位置
            var originalPositions = new Vector3[_characters.Count];
            for (int i = 0; i < _characters.Count; i++)
            {
                originalPositions[i] = _characters[i].gameObject.transform.localPosition;
            }
            
            float elapsed = 0f;
            
            while (elapsed < effectConfig.Duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / effectConfig.Duration);
                float curveValue = effectConfig.Curve.Evaluate(t);
                Vector3 offset = Vector3.Lerp(startPos, endPos, curveValue);
                
                for (int i = 0; i < _characters.Count; i++)
                {
                    var character = _characters[i];
                    Vector3 targetPos = effectConfig.MoveParams.UseRelativePosition 
                        ? originalPositions[i] + offset 
                        : offset;
                    character.SetPosition(targetPos);
                }
                
                await UniTask.Yield(cancellationToken);
            }
            
            // 设置最终位置
            if (!cancellationToken.IsCancellationRequested)
            {
                Vector3 finalOffset = isEnter ? endPos : startPos;
                for (int i = 0; i < _characters.Count; i++)
                {
                    var character = _characters[i];
                    Vector3 finalPos = effectConfig.MoveParams.UseRelativePosition 
                        ? originalPositions[i] + finalOffset 
                        : finalOffset;
                    character.SetPosition(finalPos);
                }
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 显示所有字符
        /// </summary>
        private void ShowAllCharacters()
        {
            foreach (var character in _characters)
            {
                character.SetVisible(true);
            }
        }
        
        /// <summary>
        /// 隐藏所有字符
        /// </summary>
        private void HideAllCharacters()
        {
            foreach (var character in _characters)
            {
                character.SetVisible(false);
            }
        }
        
        #endregion
    }
}