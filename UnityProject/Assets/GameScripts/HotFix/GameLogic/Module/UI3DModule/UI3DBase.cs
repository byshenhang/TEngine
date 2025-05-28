using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 3D UI基类。
    /// </summary>
    public class UI3DBase
    {
        /// <summary>
        /// 3D UI类型。
        /// </summary>
        public enum UI3DType
        {
            /// <summary>
            /// 类型无。
            /// </summary>
            None,

            /// <summary>
            /// 类型Windows。
            /// </summary>
            Window,

            /// <summary>
            /// 类型Widget。
            /// </summary>
            Widget,
        }

        /// <summary>
        /// 所属3D UI父节点。
        /// </summary>
        protected UI3DBase _parent = null;

        /// <summary>
        /// 3D UI父节点。
        /// </summary>
        public UI3DBase Parent => _parent;

        /// <summary>
        /// 自定义数据集。
        /// </summary>
        protected System.Object[] _userDatas;
        
        /// <summary>
        /// 自定义数据。
        /// </summary>
        public System.Object UserData
        {
            get
            {
                if (_userDatas != null && _userDatas.Length >= 1)
                {
                    return _userDatas[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 自定义数据集。
        /// </summary>
        public System.Object[] UserDatas => _userDatas;

        /// <summary>
        /// UI的实例资源对象。
        /// </summary>
        public virtual GameObject gameObject { protected set; get; }

        /// <summary>
        /// UI位置组件。
        /// </summary>
        public virtual Transform transform { protected set; get; }

        /// <summary>
        /// UI类型。
        /// </summary>
        public virtual UI3DType Type => UI3DType.None;

        /// <summary>
        /// 资源是否准备完毕。
        /// </summary>
        public bool IsPrepare { protected set; get; }

        /// <summary>
        /// 生命周期函数，创建。
        /// </summary>
        public virtual void OnCreate(Transform parent, System.Object[] userDatas)
        {
        }

        /// <summary>
        /// 生命周期函数，销毁。
        /// </summary>
        public virtual void OnDestroy()
        {
        }

        /// <summary>
        /// 生命周期函数，刷新。
        /// </summary>
        public virtual void OnRefresh()
        {
        }

        /// <summary>
        /// 生命周期函数，显示。
        /// </summary>
        public virtual void OnShow()
        {
        }

        /// <summary>
        /// 生命周期函数，隐藏。
        /// </summary>
        public virtual void OnHide()
        {
        }

        /// <summary>
        /// 生命周期函数，更新。
        /// </summary>
        public virtual void OnUpdate()
        {
        }
        
        /// <summary>
        /// 设置相对于用户的位置（跟随模式）
        /// </summary>
        /// <param name="relativePosition">相对位置</param>
        /// <param name="relativeRotation">相对旋转</param>
        public virtual void SetRelativeToUser(Vector3 relativePosition, Quaternion relativeRotation)
        {
        }
        
        /// <summary>
        /// 设置世界空间位置
        /// </summary>
        /// <param name="position">世界位置</param>
        /// <param name="rotation">世界旋转</param>
        public virtual void SetWorldPosition(Vector3 position, Quaternion rotation)
        {
        }
        
        /// <summary>
        /// 设置交互模式
        /// </summary>
        /// <param name="mode">交互模式</param>
        public virtual void SetInteractionMode(UI3DInteractionMode mode)
        {
        }
    }
    
    /// <summary>
    /// 3D UI交互模式
    /// </summary>
    public enum UI3DInteractionMode
    {
        /// <summary>
        /// 直接手部交互
        /// </summary>
        Direct,
        
        /// <summary>
        /// 射线交互
        /// </summary>
        RayBased,
        
        /// <summary>
        /// 凝视交互
        /// </summary>
        GazeBased,
        
        /// <summary>
        /// 混合模式
        /// </summary>
        Hybrid
    }
    
    /// <summary>
    /// 3D UI定位模式
    /// </summary>
    public enum UI3DPositionMode
    {
        /// <summary>
        /// 固定在世界坐标
        /// </summary>
        WorldFixed,
        
        /// <summary>
        /// 相对于用户
        /// </summary>
        UserRelative,
        
        /// <summary>
        /// 基于锚点
        /// </summary>
        AnchorBased,
        
        /// <summary>
        /// 附着在手上
        /// </summary>
        HandAttached
    }
}
