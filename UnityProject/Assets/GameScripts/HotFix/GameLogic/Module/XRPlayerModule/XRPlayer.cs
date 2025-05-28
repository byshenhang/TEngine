using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 传送模式
    /// </summary>
    public enum TeleportMode
    {
        /// <summary>
        /// 禁用传送
        /// </summary>
        Disabled,

        /// <summary>
        /// 只使用左手传送
        /// </summary>
        LeftHand,

        /// <summary>
        /// 只使用右手传送
        /// </summary>
        RightHand,

        /// <summary>
        /// 双手都可以传送
        /// </summary>
        Both
    }

    /// <summary>
    /// 移动模式
    /// </summary>
    public enum LocomotionMode
    {
        /// <summary>
        /// 只使用传送移动
        /// </summary>
        TeleportOnly,

        /// <summary>
        /// 平滑移动（使用摇杆）
        /// </summary>
        SmoothMovement,

        /// <summary>
        /// 平滑移动和快速转向
        /// </summary>
        SmoothWithSnapTurn,

        /// <summary>
        /// 传送移动和快速转向
        /// </summary>
        TeleportWithSnapTurn
    }

    /// <summary>
    /// 控制器手部
    /// </summary>
    public enum ControllerHand
    {
        /// <summary>
        /// 左手
        /// </summary>
        Left,

        /// <summary>
        /// 右手
        /// </summary>
        Right
    }

    /// <summary>
    /// XR交互事件类型
    /// </summary>
    public enum XRInteractionEventType
    {
        /// <summary>
        /// 选择开始（抓取开始）
        /// </summary>
        SelectEnter,

        /// <summary>
        /// 选择结束（抓取结束）
        /// </summary>
        SelectExit,

        /// <summary>
        /// 悬停开始
        /// </summary>
        HoverEnter,

        /// <summary>
        /// 悬停结束
        /// </summary>
        HoverExit,
        
        /// <summary>
        /// 扃机按下
        /// </summary>
        TriggerPressed,
        
        /// <summary>
        /// 手柄按下(拿取按钮)
        /// </summary>
        GripPressed
    }
}
