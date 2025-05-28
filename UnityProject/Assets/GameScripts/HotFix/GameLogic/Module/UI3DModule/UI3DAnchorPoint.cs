using System;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 3D UI锚点组件，用于在场景中定义UI放置位置
    /// </summary>
    [AddComponentMenu("UI3D/UI3D Anchor Point")]
    public class UI3DAnchorPoint : MonoBehaviour
    {
        [SerializeField, Tooltip("锚点唯一标识")]
        private string _anchorId;
        /// <summary>
        /// 锚点标识
        /// </summary>
        public string AnchorId => _anchorId;
        
        [SerializeField, Tooltip("默认窗口类型")]
        private string _defaultWindowType;
        /// <summary>
        /// 默认窗口类型
        /// </summary>
        public string DefaultWindowType => _defaultWindowType;
        
        [SerializeField, Tooltip("是否自动创建默认窗口")]
        private bool _autoCreate = false;
        /// <summary>
        /// 是否自动创建窗口
        /// </summary>
        public bool AutoCreate => _autoCreate;
        
        [SerializeField, Tooltip("锚点优先级（用于吸附时选择最近且优先级最高的锚点）")]
        private int _priority = 0;
        /// <summary>
        /// 锚点优先级
        /// </summary>
        public int Priority => _priority;

        [SerializeField, Tooltip("锚点分组（用于筛选）")]
        private string _anchorGroup = "Default";
        /// <summary>
        /// 锚点分组
        /// </summary>
        public string AnchorGroup => _anchorGroup;
        
        /// <summary>
        /// 当该锚点处于激活状态时
        /// </summary>
        private void OnEnable()
        {
            // 如果UI3DModule已初始化，则注册此锚点
            if (UI3DModule.Instance != null)
            {
                UI3DModule.Instance.RegisterAnchor(this);
                
                // 如果设置为自动创建，且指定了默认窗口类型
                if (_autoCreate && !string.IsNullOrEmpty(_defaultWindowType))
                {
                    UI3DModule.Instance.ShowUI3DAtAnchorByType(_anchorId, _defaultWindowType);
                }
            }
        }
        
        /// <summary>
        /// 当该锚点处于非激活状态时
        /// </summary>
        private void OnDisable()
        {
            // 如果UI3DModule存在，则取消注册
            if (UI3DModule.Instance != null)
            {
                UI3DModule.Instance.UnregisterAnchor(this);
            }
        }
        
        /// <summary>
        /// 在编辑器中绘制图标
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.2f);
            
#if UNITY_EDITOR
            // 显示锚点ID标签
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, _anchorId);
#endif
        }
        
        /// <summary>
        /// 在编辑器中绘制选中图标
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
