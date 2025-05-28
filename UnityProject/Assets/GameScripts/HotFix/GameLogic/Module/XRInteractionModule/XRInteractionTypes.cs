using System;

namespace GameLogic
{
    /// <summary>
    /// u4ea4u4e92u7269u4f53u7c7bu578bu679au4e3e
    /// </summary>
    public enum InteractableType
    {
        /// <summary>
        /// u9ed8u8ba4u7c7bu578b
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// u53efu6293u53d6u7269u4f53
        /// </summary>
        Grabbable = 1,
        
        /// <summary>
        /// u6309u94aeu7c7bu578b
        /// </summary>
        Button = 2,
        
        /// <summary>
        /// u62c9u6746u7c7bu578b
        /// </summary>
        Lever = 3,
        
        /// <summary>
        /// u53efu89e6u6478u7269u4f53
        /// </summary>
        Touchable = 4,
        
        /// <summary>
        /// u53efu7f29u653eu7269u4f53
        /// </summary>
        Scalable = 5,
        
        /// <summary>
        /// UIu4ea4u4e92u5143u7d20
        /// </summary>
        UI = 6
    }
}
