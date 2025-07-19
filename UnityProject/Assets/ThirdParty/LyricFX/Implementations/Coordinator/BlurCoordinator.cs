//using LyricFX.Core;
//using LyricFX.Core.Interfaces;
//using LyricFX.Implementations.Effect;
//using Cysharp.Threading.Tasks;
//using System;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;
//using GameLogic;
//using LyricFX.Core.Attributes;
//using System.Linq;
//using Random = UnityEngine.Random;
//using ChocDino.UIFX;
//using TMPro;
//using LyricFX.Managers;

//namespace LyricFX.Implementations.Coordinator
//{
//    /// <summary>
//    /// 随机模糊效果协调器
//    /// 实现字符随机模糊效果，从模糊到清晰，再到更模糊的过渡
//    /// </summary>
//    [EffectConfig(typeof(BlurConfig))]
//    public class BlurCoordinator : LineEffectCoordinator
//    {
//        // 配置参数
//        private float initialBlurValue = 10f;      // 初始模糊值
//        private float targetBlurValue = 0f;        // 目标模糊值（清晰）
//        private float finalBlurValue = 40f;        // 最终模糊值
//        private float fadeInDuration = 0.8f;       // 从模糊到清晰的过渡时间
//        private float holdDuration = 1.5f;         // 保持清晰的时间
//        private float fadeOutDuration = 1.0f;      // 从清晰到更模糊的过渡时间
//        private AnimationCurve blurCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
//        // 运行时数据
//        private List<DefaultFadeEffect> characterEffects = new List<DefaultFadeEffect>();
//        private List<BlurFilter> blurFilters = new List<BlurFilter>();
//        private List<int> characterOrder = new List<int>();
        
//        /// <summary>
//        /// 创建字符效果实例
//        /// </summary>
//        protected override async UniTask CreateCharacterEffects(ICoordinatorConfig config, CancellationToken cancellationToken)
//        {
//            // 应用配置
//            if (config is BlurConfig blurConfig)
//            {
//                initialBlurValue = blurConfig.InitialBlurValue;
//                targetBlurValue = blurConfig.TargetBlurValue;
//                finalBlurValue = blurConfig.FinalBlurValue;
//                fadeInDuration = blurConfig.FadeInDuration;
//                holdDuration = blurConfig.HoldDuration;
//                fadeOutDuration = blurConfig.FadeOutDuration;
                
//                if (blurConfig.BlurCurve != null)
//                    blurCurve = blurConfig.BlurCurve;
//            }
            
//            // 清空之前的数据
//            characterEffects.Clear();
//            blurFilters.Clear();
//            characterOrder.Clear();
            
//            // 为每个字符创建效果实例
//            for (int i = 0; i < characterObjects.Count; i++)
//            {
//                var charEffect = new DefaultFadeEffect();
//                await charEffect.Initialize(characterObjects[i], null, cancellationToken);
//                characterEffects.Add(charEffect);
                
//                // 获取或添加BlurFilter组件
//                var blurFilter = characterObjects[i].GetComponent<BlurFilter>();
//                if (blurFilter == null)
//                {
//                    blurFilter = characterObjects[i].AddComponent<BlurFilter>();
//                }
                
//                // 设置初始模糊值
//                blurFilter.Blur = initialBlurValue;
//                blurFilters.Add(blurFilter);
                
//                // 确保文字是可见的（Alpha值为1）
//                var textComp = characterObjects[i].GetComponent<TextMeshProUGUI>();
//                if (textComp != null)
//                {
//                    var color = textComp.color;
//                    color.a = 1f;
//                    textComp.color = color;
//                }
                
//                // 添加到字符顺序列表
//                characterOrder.Add(i);
//            }
            
//            // 随机打乱字符顺序
//            ShuffleCharacterOrder();
//        }
        
//        /// <summary>
//        /// 协调效果播放
//        /// </summary>
//        protected override async UniTask CoordinateEffects(CancellationToken cancellationToken)
//        {
//            // 第一阶段：随机顺序从模糊到清晰
//            await BlurToClean(cancellationToken);
            
//            // 第二阶段：保持清晰
//            await Hold(cancellationToken);
            
//            // 第三阶段：从清晰到更模糊
//            await CleanToBlur(cancellationToken);
//        }
        
//        /// <summary>
//        /// 随机打乱字符顺序
//        /// </summary>
//        private void ShuffleCharacterOrder()
//        {
//            // Fisher-Yates 洗牌算法
//            for (int i = characterOrder.Count - 1; i > 0; i--)
//            {
//                int j = Random.Range(0, i + 1);
//                int temp = characterOrder[i];
//                characterOrder[i] = characterOrder[j];
//                characterOrder[j] = temp;
//            }
//        }
        
//        /// <summary>
//        /// 从模糊到清晰的过渡
//        /// </summary>
//        private async UniTask BlurToClean(CancellationToken cancellationToken)
//        {
//            // 创建所有字符的过渡任务
//            var blurTasks = new List<UniTask>();
            
//            // 所有字符同时从模糊变清晰
//            for (int i = 0; i < blurFilters.Count; i++)
//            {
//                // 添加过渡任务，无延迟
//                blurTasks.Add(TransitionBlurValue(i, initialBlurValue, targetBlurValue, fadeInDuration, 0f, cancellationToken));
//            }
            
//            // 等待所有过渡完成
//            await UniTask.WhenAll(blurTasks);
            
//            // 更新进度（第一阶段占总进度的40%）
//            UpdateProgress(0.4f);
//        }
        
//        /// <summary>
//        /// 保持清晰状态
//        /// </summary>
//        private async UniTask Hold(CancellationToken cancellationToken)
//        {
//            await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: cancellationToken);
            
//            // 更新进度（保持阶段占总进度的20%）
//            UpdateProgress(0.6f);
//        }
        
//        /// <summary>
//        /// 从清晰到更模糊的过渡
//        /// </summary>
//        private async UniTask CleanToBlur(CancellationToken cancellationToken)
//        {
//            // 创建所有字符的过渡任务
//            var blurTasks = new List<UniTask>();
            
//            // 所有字符同时从清晰变模糊
//            for (int i = 0; i < blurFilters.Count; i++)
//            {
//                // 添加过渡任务，无延迟
//                blurTasks.Add(TransitionBlurValue(i, targetBlurValue, finalBlurValue, fadeOutDuration, 0f, cancellationToken));
//            }
            
//            // 等待所有过渡完成
//            await UniTask.WhenAll(blurTasks);
            
//            // 更新进度到100%
//            UpdateProgress(1.0f);
//        }
        
//        /// <summary>
//        /// 过渡模糊值
//        /// </summary>
//        private async UniTask TransitionBlurValue(int index, float fromValue, float toValue, float duration, float delay, CancellationToken cancellationToken)
//        {
//            if (index >= blurFilters.Count || blurFilters[index] == null)
//                return;
                
//            // 等待延迟时间
//            if (delay > 0)
//            {
//                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
//            }
            
//            var blurFilter = blurFilters[index];
//            float elapsedTime = 0f;
            
//            while (elapsedTime < duration)
//            {
//                cancellationToken.ThrowIfCancellationRequested();
                
//                float t = elapsedTime / duration;
//                float curveValue = blurCurve.Evaluate(t);
                
//                // 计算当前模糊值
//                float currentBlur = Mathf.Lerp(fromValue, toValue, curveValue);
//                blurFilter.Blur = currentBlur;
                
//                elapsedTime += Time.deltaTime;
//                await UniTask.Yield(cancellationToken);
//            }
            
//            // 确保最终值正确
//            blurFilter.Blur = toValue;
//        }
//    }
    
//    /// <summary>
//    /// 随机模糊效果配置
//    /// </summary>
//    public class BlurConfig : ICoordinatorConfig, IAdjustConfig
//    {
//        public float InitialBlurValue { get; set; } = 10f;
//        public float TargetBlurValue { get; set; } = 0f;
//        public float FinalBlurValue { get; set; } = 20f;
//        public float FadeInDuration { get; set; } = 0.8f;
//        public float HoldDuration { get; set; } = 1.5f;
//        public float FadeOutDuration { get; set; } = 1.0f;
//        public AnimationCurve BlurCurve { get; set; }
        
//        /// <summary>
//        /// 获取总持续时间
//        /// </summary>
//        /// <param name="characterCount">字符数量</param>
//        /// <returns>总持续时间</returns>
//        public float GetTotalDuration(int characterCount = 10)
//        {
//            // 估算字符延迟总时间（每个字符0.05秒延迟）
//            float totalDelay = 0.05f * (characterCount - 1);
            
//            // 总时间 = 最大延迟时间 + 淡入时间 + 保持时间 + 淡出时间
//            return totalDelay + FadeInDuration + HoldDuration + FadeOutDuration;
//        }
        
//        /// <summary>
//        /// 根据可用时间调整持续时间，保持原有比例
//        /// </summary>
//        /// <param name="availableDuration">可用的总时间</param>
//        /// <param name="characterCount">字符数量</param>
//        public void AdjustDuration(float availableDuration, int characterCount = 10)
//        {
//            //if (availableDuration <= 0)
//            //    return;
                
//            //// 计算当前总时长
//            //float totalDuration = GetTotalDuration(characterCount);
            
//            //// 如果可用时间小于总时长，按比例缩放
//            //if (availableDuration < totalDuration)
//            //{
//            //    float ratio = availableDuration / totalDuration;
                
//            //    // 保持最小时间
//            //    float minDuration = 0.1f;
                
//            //    // 计算新的时间，确保各阶段至少有最小时间
//            //    FadeInDuration = Mathf.Max(FadeInDuration * ratio, minDuration);
//            //    FadeOutDuration = Mathf.Max(FadeOutDuration * ratio, minDuration);
                
//            //    // 剩余时间分配给保持阶段
//            //    float totalDelay = 0.05f * (characterCount - 1);
//            //    float remainingTime = availableDuration - totalDelay - FadeInDuration - FadeOutDuration;
//            //    HoldDuration = Mathf.Max(remainingTime, 0);
//            //}
//            //else
//            //{
//            //    // 如果可用时间充足，增加保持时间
//            //    HoldDuration += (availableDuration - totalDuration);
//            //}
//        }
//    }
//}
