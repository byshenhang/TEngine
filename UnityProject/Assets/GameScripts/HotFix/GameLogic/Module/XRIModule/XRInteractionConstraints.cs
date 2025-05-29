using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// XR交互约束基类 - 用于限制交互物体的移动或旋转
    /// </summary>
    public abstract class XRInteractionConstraint : MonoBehaviour
    {
        [Tooltip("是否启用该约束")]
        public bool enabled = true;

        [Tooltip("约束优先级，越大越优先应用")]
        public int priority = 0;

        protected XRGrabInteractable _grabInteractable;
        protected Vector3 _initialLocalPosition;
        protected Quaternion _initialLocalRotation;

        protected virtual void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>() ?? GetComponentInParent<XRGrabInteractable>();

            if (_grabInteractable != null)
            {
                // 记录初始位置和旋转
                _initialLocalPosition = transform.localPosition;
                _initialLocalRotation = transform.localRotation;

                // 注册交互事件
                RegisterEvents();
            }
            else
            {
                TEngine.Log.Warning($"无法找到XRGrabInteractable组件，{GetType().Name}约束将不会生效");
                enabled = false;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_grabInteractable != null)
            {
                // 取消注册交互事件
                UnregisterEvents();
            }
        }

        /// <summary>
        /// 注册交互事件
        /// </summary>
        protected virtual void RegisterEvents()
        {
            _grabInteractable.selectEntered.AddListener(OnSelectEntered);
            _grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        /// <summary>
        /// 取消注册交互事件
        /// </summary>
        protected virtual void UnregisterEvents()
        {
            _grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            _grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }

        /// <summary>
        /// 当交互物体被选中时调用
        /// </summary>
        protected virtual void OnSelectEntered(SelectEnterEventArgs args) { if (!enabled) return; }

        /// <summary>
        /// 当交互物体被取消选中时调用
        /// </summary>
        protected virtual void OnSelectExited(SelectExitEventArgs args) { if (!enabled) return; }

        /// <summary>
        /// 应用约束，子类必须实现该方法
        /// </summary>
        public abstract void ApplyConstraint();
    }

    /// <summary>
    /// 单轴约束 - 限制交互物体沿单一轴移动
    /// </summary>
    public class SingleAxisConstraint : XRInteractionConstraint
    {
        [Tooltip("允许移动的轴")]
        public Vector3 Axis = Vector3.up;

        [Tooltip("最小移动限制")]
        public float MinLimit = -0.5f;

        [Tooltip("最大移动限制")]
        public float MaxLimit = 0.5f;

        private Vector3 _grabLocalPosition;
        private Transform _referenceTransform;

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            // 记录初始抓取位置
            _grabLocalPosition = transform.localPosition;

            // 使用父轴作为参考
            _referenceTransform = transform.parent;
        }

        public override void ApplyConstraint()
        {
            if (!enabled || _grabInteractable == null || !_grabInteractable.isSelected) return;

            Vector3 currentPosition = transform.localPosition;
            Vector3 normalizedAxis = Axis.normalized;
            float axisValue = Vector3.Dot(currentPosition, normalizedAxis);

            float constrainedValue = Mathf.Clamp(axisValue, MinLimit, MaxLimit);
            Vector3 constrainedPosition = _initialLocalPosition + normalizedAxis * (constrainedValue - Vector3.Dot(_initialLocalPosition, normalizedAxis));

            Vector3 nonAxisPosition = currentPosition - Vector3.Project(currentPosition, normalizedAxis);
            Vector3 initialNonAxisPosition = _initialLocalPosition - Vector3.Project(_initialLocalPosition, normalizedAxis);

            transform.localPosition = constrainedPosition + initialNonAxisPosition;
        }

        private void Update()
        {
            ApplyConstraint();
        }
    }

    /// <summary>
    /// 旋转约束 - 限制交互物体围绕单一轴旋转
    /// </summary>
    public class RotationAxisConstraint : XRInteractionConstraint
    {
        [Tooltip("允许旋转的轴")]
        public Vector3 Axis = Vector3.up;

        [Tooltip("最小旋转角度")]
        public float MinAngle = -180f;

        [Tooltip("最大旋转角度")]
        public float MaxAngle = 180f;

        private Quaternion _grabLocalRotation;
        private Vector3 _initialAngle;

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            _grabLocalRotation = transform.localRotation;
            _initialAngle = _grabLocalRotation.eulerAngles;
        }

        public override void ApplyConstraint()
        {
            if (!enabled || _grabInteractable == null || !_grabInteractable.isSelected) return;

            Quaternion currentRotation = transform.localRotation;
            Vector3 currentEuler = currentRotation.eulerAngles;
            Vector3 constrainedEuler = currentEuler;

            if (Axis == Vector3.right || Axis == Vector3.left)
            {
                float xAngle = currentEuler.x > 180 ? currentEuler.x - 360 : currentEuler.x;
                constrainedEuler.x = Mathf.Clamp(xAngle, MinAngle, MaxAngle);
                constrainedEuler.y = _initialAngle.y;
                constrainedEuler.z = _initialAngle.z;
            }
            else if (Axis == Vector3.up || Axis == Vector3.down)
            {
                float yAngle = currentEuler.y > 180 ? currentEuler.y - 360 : currentEuler.y;
                constrainedEuler.y = Mathf.Clamp(yAngle, MinAngle, MaxAngle);
                constrainedEuler.x = _initialAngle.x;
                constrainedEuler.z = _initialAngle.z;
            }
            else if (Axis == Vector3.forward || Axis == Vector3.back)
            {
                float zAngle = currentEuler.z > 180 ? currentEuler.z - 360 : currentEuler.z;
                constrainedEuler.z = Mathf.Clamp(zAngle, MinAngle, MaxAngle);
                constrainedEuler.x = _initialAngle.x;
                constrainedEuler.y = _initialAngle.y;
            }

            transform.localRotation = Quaternion.Euler(constrainedEuler);
        }

        private void Update()
        {
            ApplyConstraint();
        }
    }

    /// <summary>
    /// 限制区域约束 - 限制交互物体在特定区域内移动
    /// </summary>
    public class BoundsConstraint : XRInteractionConstraint
    {
        [Tooltip("区域中心")]
        public Vector3 Center = Vector3.zero;

        [Tooltip("区域大小")]
        public Vector3 Size = new Vector3(1f, 1f, 1f);

        [Tooltip("是否使用世界坐标")]
        public bool UseWorldSpace = false;

        private Bounds _bounds;
        private Transform _referenceTransform;

        protected override void Awake()
        {
            base.Awake();
            _bounds = new Bounds(Center, Size);
            _referenceTransform = UseWorldSpace ? null : transform.parent;
        }

        public override void ApplyConstraint()
        {
            if (!enabled || _grabInteractable == null || !_grabInteractable.isSelected) return;

            if (UseWorldSpace)
            {
                Vector3 constrainedPosition = _bounds.ClosestPoint(transform.position);
                transform.position = constrainedPosition;
            }
            else
            {
                Vector3 constrainedPosition = _bounds.ClosestPoint(transform.localPosition);
                transform.localPosition = constrainedPosition;
            }
        }

        private void Update()
        {
            ApplyConstraint();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            if (UseWorldSpace)
            {
                Gizmos.DrawWireCube(Center, Size);
            }
            else if (transform.parent != null)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.parent.localToWorldMatrix;
                Gizmos.DrawWireCube(Center, Size);
                Gizmos.matrix = oldMatrix;
            }
        }
    }

    /// <summary>
    /// 距离约束 - 限制交互物体与指定点的距离
    /// </summary>
    public class DistanceConstraint : XRInteractionConstraint
    {
        [Tooltip("锚点")]
        public Transform AnchorPoint;

        [Tooltip("最大距离")]
        public float MaxDistance = 1f;

        [Tooltip("是否使用弹性约束（如弹簧）")]
        public bool UseSpring = false;

        [Tooltip("弹簧强度（当UseSpring为true时有效）")]
        public float SpringStrength = 10f;

        private Vector3 _initialAnchorOffset;

        protected override void Awake()
        {
            base.Awake();

            if (AnchorPoint == null)
            {
                AnchorPoint = transform.parent;
            }

            if (AnchorPoint != null)
            {
                _initialAnchorOffset = transform.position - AnchorPoint.position;
            }
        }

        public override void ApplyConstraint()
        {
            if (!enabled || _grabInteractable == null || AnchorPoint == null) return;

            Vector3 currentPosition = transform.position;
            Vector3 anchorPosition = AnchorPoint.position;
            Vector3 direction = currentPosition - anchorPosition;
            float distance = direction.magnitude;

            if (distance > MaxDistance)
            {
                if (UseSpring)
                {
                    float springForce = (distance - MaxDistance) * SpringStrength * Time.deltaTime;
                    Vector3 pullDirection = direction.normalized * springForce;
                    transform.position = currentPosition - pullDirection;
                }
                else
                {
                    transform.position = anchorPosition + direction.normalized * MaxDistance;
                }
            }
        }

        private void Update()
        {
            ApplyConstraint();
        }

        private void OnDrawGizmosSelected()
        {
            if (AnchorPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(AnchorPoint.position, MaxDistance);
                Gizmos.DrawLine(transform.position, AnchorPoint.position);
            }
        }
    }
}
