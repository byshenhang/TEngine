using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// XR交互规则基类
    /// </summary>
    [Serializable]
    public abstract class XRInteractionRule
    {
        // 规则名称
        [SerializeField] protected string _ruleName = "Rule";
        
        // 匹配条件
        [SerializeField] protected List<string> _targetTags = new List<string>();
        [SerializeField] protected List<string> _targetInteractionTypes = new List<string>();
        
        /// <summary>
        /// 检查交互物体是否符合规则
        /// </summary>
        public virtual bool MatchesInteractable(IXRInteractable interactable)
        {
            if (interactable == null) return false;
            
            // 如果没有指定标签或交互类型，则认为匹配
            if (_targetTags.Count == 0 && _targetInteractionTypes.Count == 0)
                return true;
            
            // 检查交互类型
            bool typeMatch = _targetInteractionTypes.Count == 0;
            foreach (var type in interactable.InteractionTypes)
            {
                if (_targetInteractionTypes.Contains(type))
                {
                    typeMatch = true;
                    break;
                }
            }
            
            return typeMatch;
        }
        
        /// <summary>
        /// 应用规则到交互物体
        /// </summary>
        public abstract void ApplyTo(IXRInteractable interactable);
        
        /// <summary>
        /// 检查是否可以处理交互事件
        /// </summary>
        public abstract bool CanHandleEvent(XRInteractionEventBase evt);
        
        /// <summary>
        /// 处理交互事件
        /// </summary>
        public abstract void HandleEvent(XRInteractionEventBase evt);
    }
    
    /// <summary>
    /// 抓取音效规则 - 为可抓取物体添加音效和触觉反馈
    /// </summary>
    [Serializable]
    public class GrabSoundRule : XRInteractionRule
    {
        [SerializeField] private string _grabSoundName = "grab_sound";
        [SerializeField] private string _releaseSoundName = "release_sound";
        [SerializeField] private float _hapticIntensity = 0.5f;
        [SerializeField] private float _hapticDuration = 0.1f;
        
        public override void ApplyTo(IXRInteractable interactable)
        {
            // 将交互物体转换为MonoBehaviour以添加组件
            MonoBehaviour mono = interactable as MonoBehaviour;
            if (mono == null) return;
            
            // 添加处理组件或直接注册事件处理
            var handler = mono.gameObject.AddComponent<GrabSoundHandler>();
            handler.Initialize(_grabSoundName, _releaseSoundName, _hapticIntensity, _hapticDuration);
        }
        
        public override bool CanHandleEvent(XRInteractionEventBase evt)
        {
            // 这个规则只在ApplyTo中设置事件处理，不直接处理事件
            return false;
        }
        
        public override void HandleEvent(XRInteractionEventBase evt)
        {
            // 不需要实现
        }
    }
    
    /// <summary>
    /// 抓取音效处理器
    /// </summary>
    public class GrabSoundHandler : MonoBehaviour
    {
        private string _grabSoundName;
        private string _releaseSoundName;
        private float _hapticIntensity;
        private float _hapticDuration;
        
        /// <summary>
        /// 初始化处理器
        /// </summary>
        public void Initialize(string grabSound, string releaseSound, float hapticIntensity, float hapticDuration)
        {
            _grabSoundName = grabSound;
            _releaseSoundName = releaseSound;
            _hapticIntensity = hapticIntensity;
            _hapticDuration = hapticDuration;
            
            // 订阅事件
            XRInteractionEventBus.Instance.Subscribe<XRGrabEvent>(OnGrabEvent);
            XRInteractionEventBus.Instance.Subscribe<XRReleaseEvent>(OnReleaseEvent);
        }
        
        private void OnGrabEvent(XRGrabEvent evt)
        {
            if (evt.Target == gameObject)
            {
                // 播放抓取音效
                //GameModule.Audio.PlaySound(_grabSoundName);
                
                // 触发震动（这里假设你有一个触发震动的方法）
                if (GameModule.XRPlayer != null)
                {
                    // 使用您现有的方法触发震动
                    // 这里只是一个示例，具体实现需要根据您的框架调整
                }
            }
        }
        
        private void OnReleaseEvent(XRReleaseEvent evt)
        {
            if (evt.Target == gameObject)
            {
                // 播放释放音效
                //GameModule.Audio.PlaySound(_releaseSoundName);
            }
        }
        
        private void OnDestroy()
        {
            // 注销事件
            XRInteractionEventBus.Instance.Unsubscribe<XRGrabEvent>(OnGrabEvent);
            XRInteractionEventBus.Instance.Unsubscribe<XRReleaseEvent>(OnReleaseEvent);
        }
    }
}
