using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// XR交互事件基类
    /// </summary>
    public abstract class XRInteractionEventBase
    {
        /// <summary>
        /// 交互发起者（通常是控制器）
        /// </summary>
        public GameObject Interactor { get; set; }
        
        /// <summary>
        /// 交互目标物体
        /// </summary>
        public GameObject Interactable { get; set; }
        
        /// <summary>
        /// 交互目标物体 (兼容性属性)
        /// </summary>
        public GameObject Target { 
            get { return Interactable; }
            set { Interactable = value; }
        }
        
        /// <summary>
        /// 事件发生时间戳
        /// </summary>
        public float Timestamp { get; set; }
        
        /// <summary>
        /// 唯一交互ID（用于跟踪交互序列）
        /// </summary>
        public string InteractionID { get; set; }
    }
    
    /// <summary>
    /// 抓取事件
    /// </summary>
    public class XRGrabEvent : XRInteractionEventBase
    {
        /// <summary>
        /// 抓取位置
        /// </summary>
        public Vector3 GrabPosition { get; set; }
        
        /// <summary>
        /// 抓取旋转
        /// </summary>
        public Quaternion GrabRotation { get; set; }
        
        /// <summary>
        /// 是否是双手抓取的第二只手
        /// </summary>
        public bool IsSecondaryGrab { get; set; }
    }
    
    /// <summary>
    /// 释放事件
    /// </summary>
    public class XRReleaseEvent : XRInteractionEventBase
    {
        /// <summary>
        /// 释放位置
        /// </summary>
        public Vector3 ReleasePosition { get; set; }
        
        /// <summary>
        /// 释放速度
        /// </summary>
        public Vector3 ReleaseVelocity { get; set; }
    }
    
    /// <summary>
    /// 按钮按下事件
    /// </summary>
    public class XRButtonPressEvent : XRInteractionEventBase
    {
        /// <summary>
        /// 按钮ID
        /// </summary>
        public string ButtonID { get; set; }
        
        /// <summary>
        /// 按下力度（0-1）
        /// </summary>
        public float PressAmount { get; set; }
    }
    
    /// <summary>
    /// 触摸事件
    /// </summary>
    public class XRTouchEvent : XRInteractionEventBase
    {
        /// <summary>
        /// 触摸位置
        /// </summary>
        public Vector3 TouchPosition { get; set; }
        
        /// <summary>
        /// 触摸法线
        /// </summary>
        public Vector3 TouchNormal { get; set; }
        
        /// <summary>
        /// 触摸压力 (0-1)
        /// </summary>
        public float Pressure { get; set; }
    }
    
    /// <summary>
    /// 悬停开始事件
    /// </summary>
    public class XRHoverEnterEvent : XRInteractionEventBase
    {
        /// <summary>
        /// 悬停位置
        /// </summary>
        public Vector3 HoverPosition { get; set; }
    }
    
    /// <summary>
    /// 悬停结束事件
    /// </summary>
    public class XRHoverExitEvent : XRInteractionEventBase
    {
        /// <summary>
        /// 悬停持续时间
        /// </summary>
        public float HoverDuration { get; set; }
    }
    
    /// <summary>
    /// 拉杆拉动事件
    /// </summary>
    public class XRLeverPullEvent : XRInteractionEventBase
    {
        /// <summary>
        /// 拉杆值（0-1）
        /// </summary>
        public float LeverValue { get; set; }
        
        /// <summary>
        /// 拉杆值（0-1）(兼容性属性)
        /// </summary>
        public float PullValue { 
            get { return LeverValue; }
            set { LeverValue = value; }
        }
        
        /// <summary>
        /// 是否触发了事件阈值
        /// </summary>
        public bool ReachedThreshold { get; set; }
    }
    
    #region Unity XR Interaction Toolkit兼容事件
    
    /// <summary>
    /// 交互器注册事件
    /// </summary>
    public class XRInteractorRegisteredEvent : XRInteractionEventBase
    {
        // 基类已有Interactor属性
    }
    
    /// <summary>
    /// 交互物体注册事件
    /// </summary>
    public class XRInteractableRegisteredEvent : XRInteractionEventBase
    {
        // Target属性在此作为Interactable使用
    }
    
    /// <summary>
    /// 选择进入事件
    /// </summary>
    public class XRSelectEnterEvent : XRInteractionEventBase
    {
        /// <summary>
        /// 选择类型
        /// </summary>
        public int SelectType { get; set; } // 使用int替代，兼容Unity不同版本
    }
    
    /// <summary>
    /// 选择退出事件
    /// </summary>
    public class XRSelectExitEvent : XRInteractionEventBase
    {
        /// <summary>
        /// 退出类型
        /// </summary>
        public int ExitType { get; set; } // 使用int替代，兼容Unity不同版本
    }
    
    /// <summary>
    /// 激活事件
    /// </summary>
    public class XRActivateEvent : XRInteractionEventBase
    {
        // 基类已包含必要属性
    }
    
    /// <summary>
    /// 停止激活事件
    /// </summary>
    public class XRDeactivateEvent : XRInteractionEventBase
    {
        // 基类已包含必要属性
    }
    
    #endregion
}
