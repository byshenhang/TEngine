using System;

namespace GameLogic
{
    /// <summary>
    /// 场景3D UI特性，用于标记UI类为场景UI
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SceneUIAttribute : Attribute
    {
        /// <summary>
        /// 资源路径
        /// </summary>
        public readonly string AssetPath;
        
        /// <summary>
        /// 默认是否可抓取
        /// </summary>
        public readonly bool Grabbable;
        
        /// <summary>
        /// 默认交互模式
        /// </summary>
        public readonly UI3DInteractionMode InteractionMode;
        
        /// <summary>
        /// 默认定位模式
        /// </summary>
        public readonly UI3DPositionMode PositionMode;
        
        /// <summary>
        /// 创建场景3D UI特性
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="grabbable">是否可抓取</param>
        /// <param name="interactionMode">交互模式</param>
        /// <param name="positionMode">定位模式</param>
        public SceneUIAttribute(string assetPath, bool grabbable = true, 
            UI3DInteractionMode interactionMode = UI3DInteractionMode.RayBased,
            UI3DPositionMode positionMode = UI3DPositionMode.WorldFixed)
        {
            AssetPath = assetPath;
            Grabbable = grabbable;
            InteractionMode = interactionMode;
            PositionMode = positionMode;
        }
    }
}
