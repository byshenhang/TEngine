using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// XR可交互物体接口
    /// </summary>
    public interface IXRInteractable
    {
        /// <summary>
        /// 交互物体唯一标识符
        /// </summary>
        string InteractableID { get; }
        
        /// <summary>
        /// 交互类型标识（用于分组和筛选）
        /// </summary>
        HashSet<string> InteractionTypes { get; }
        
        /// <summary>
        /// 交互优先级
        /// </summary>
        int InteractionPriority { get; }
        
        /// <summary>
        /// 响应交互事件
        /// </summary>
        /// <param name="interactionEvent">交互事件</param>
        void ProcessInteraction(XRInteractionEventBase interactionEvent);
    }
    
    /// <summary>
    /// 交互优先级定义
    /// </summary>
    public enum XRInteractionPriority
    {
        /// <summary>
        /// 关键交互（如安全相关交互）
        /// </summary>
        Critical = 100,
        
        /// <summary>
        /// UI交互优先
        /// </summary>
        UI = 80,
        
        /// <summary>
        /// 游戏玩法交互
        /// </summary>
        Gameplay = 60,
        
        /// <summary>
        /// 环境交互
        /// </summary>
        Environmental = 40,
        
        /// <summary>
        /// 装饰性交互
        /// </summary>
        Decorative = 20,
        
        /// <summary>
        /// 默认优先级
        /// </summary>
        Default = 0
    }
}
