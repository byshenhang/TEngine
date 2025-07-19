using LyricFX.Core.Interfaces;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using TMPro;
using System;
using GameLogic;
using LyricFX.Managers;

namespace LyricFX.Core
{
    /// <summary>
    /// 行级效果协调器基类
    /// 提供行级效果的通用协调逻辑
    /// </summary>
    public abstract class LineEffectCoordinator : ILineEffectCoordinator
    {
        protected GameObject lineContainer;
        protected List<GameObject> characterObjects = new List<GameObject>();
        protected List<TextMeshProUGUI> textComponents = new List<TextMeshProUGUI>();
        protected List<ILyricEffect> characterEffects = new List<ILyricEffect>();
        protected CancellationTokenSource effectCts;
        
        /// <summary>
        /// 当前进度
        /// </summary>
        public float Progress { get; protected set; }
        
        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted { get; protected set; }
        
        /// <summary>
        /// 初始化协调器
        /// </summary>
        public virtual async UniTask Initialize(GameObject lineContainer, ICoordinatorConfig config, CancellationToken cancellationToken = default)
        {
            this.lineContainer = lineContainer;
            
            // 清理之前的数据
            characterObjects.Clear();
            textComponents.Clear();
            characterEffects.Clear();
            
            // 收集字符对象
            CollectCharacterObjects();
            
            // 创建字符效果实例
            await CreateCharacterEffects(config, cancellationToken);
            
            // 重置状态
            Progress = 0f;
            IsCompleted = false;
        }
        
        /// <summary>
        /// 播放效果
        /// </summary>
        public virtual async UniTask Play(CancellationToken cancellationToken = default)
        {
            if (characterObjects.Count == 0)
                return;
                
            // 创建效果的取消令牌
            StopInternal();
            effectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            try
            {
                // 执行协调逻辑
                await CoordinateEffects(effectCts.Token);
                
                IsCompleted = true;
                Progress = 1f;
            }
            catch (OperationCanceledException)
            {
                // 取消操作，不设置完成状态
            }
        }
        
        /// <summary>
        /// 停止效果
        /// </summary>
        public virtual async UniTask Stop(CancellationToken cancellationToken = default)
        {
            StopInternal();
            
            // 停止所有字符效果
            foreach (var effect in characterEffects)
            {
                await effect.Stop(cancellationToken);
            }
        }
        
        /// <summary>
        /// 重置效果
        /// </summary>
        public virtual async UniTask Reset(CancellationToken cancellationToken = default)
        {
            StopInternal();
            
            // 重置所有字符效果
            foreach (var effect in characterEffects)
            {
                await effect.Reset(cancellationToken);
            }
            
            Progress = 0f;
            IsCompleted = false;
        }
        
        /// <summary>
        /// 协调效果播放的抽象方法
        /// 子类需要实现具体的协调逻辑
        /// </summary>
        protected abstract UniTask CoordinateEffects(CancellationToken cancellationToken);
        
        /// <summary>
        /// 创建字符效果实例的抽象方法
        /// 子类需要实现具体的效果创建逻辑
        /// </summary>
        protected abstract UniTask CreateCharacterEffects(ICoordinatorConfig config, CancellationToken cancellationToken);
        
        /// <summary>
        /// 收集字符对象
        /// </summary>
        protected virtual void CollectCharacterObjects()
        {
            if (lineContainer == null) return;
            
            for (int i = 0; i < lineContainer.transform.childCount; i++)
            {
                GameObject charObj = lineContainer.transform.GetChild(i).gameObject;
                TextMeshProUGUI textComp = charObj.GetComponent<TextMeshProUGUI>();
                
                if (textComp != null)
                {
                    characterObjects.Add(charObj);
                    textComponents.Add(textComp);
                }
            }
        }
        
        /// <summary>
        /// 内部停止方法
        /// </summary>
        protected virtual void StopInternal()
        {
            effectCts?.Cancel();
            effectCts?.Dispose();
            effectCts = null;
        }
        
        /// <summary>
        /// 更新进度
        /// </summary>
        protected void UpdateProgress(float progress)
        {
            Progress = Mathf.Clamp01(progress);
        }
    }
}