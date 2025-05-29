using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// 交互值提取器接口 - 用于从不同类型的交互物体中提取当前值
    /// </summary>
    public interface IXRValueExtractor
    {
        /// <summary>
        /// 从交互物体中提取值 (0-1范围)
        /// </summary>
        float ExtractValue(XRBaseInteractable interactable);
        
        /// <summary>
        /// 提取交互物体的原始值 (无需映射到0-1范围)
        /// </summary>
        float ExtractRawValue(XRBaseInteractable interactable);
    }
    
    /// <summary>
    /// 拉杆值提取器 - 基于位置提取拉杆值
    /// </summary>
    public class LeverValueExtractor : IXRValueExtractor
    {
        public Vector3 Axis = Vector3.up;
        public float MinPosition = -0.1f;
        public float MaxPosition = 0.1f;
        public Transform ReferenceTransform;
        
        public float ExtractValue(XRBaseInteractable interactable)
        {
            float rawValue = ExtractRawValue(interactable);
            return Mathf.Clamp01((rawValue - MinPosition) / (MaxPosition - MinPosition));
        }
        
        public float ExtractRawValue(XRBaseInteractable interactable)
        {
            if (interactable == null) return 0f;
            
            Vector3 position = interactable.transform.position;
            if (ReferenceTransform != null)
            {
                // 转换为参考坐标系
                position = ReferenceTransform.InverseTransformPoint(position);
            }
            
            // 根据指定轴获取位置值
            float value = Vector3.Dot(position, Axis.normalized);
            return value;
        }
    }
    
    /// <summary>
    /// 旋钮值提取器 - 基于旋转提取旋钮值
    /// </summary>
    public class KnobValueExtractor : IXRValueExtractor
    {
        public Vector3 RotationAxis = Vector3.forward;
        public float MinAngle = 0f;
        public float MaxAngle = 360f;
        public Transform ReferenceTransform;
        
        public float ExtractValue(XRBaseInteractable interactable)
        {
            float rawValue = ExtractRawValue(interactable);
            float range = MaxAngle - MinAngle;
            float normalizedValue = ((rawValue - MinAngle) % range + range) % range / range;
            return normalizedValue;
        }
        
        public float ExtractRawValue(XRBaseInteractable interactable)
        {
            if (interactable == null) return 0f;
            
            Quaternion rotation = interactable.transform.rotation;
            if (ReferenceTransform != null)
            {
                // 转换为参考坐标系
                rotation = Quaternion.Inverse(ReferenceTransform.rotation) * rotation;
            }
            
            // 提取绕指定轴的旋转角度
            Vector3 rotationEuler = rotation.eulerAngles;
            float angle = 0f;
            
            if (RotationAxis == Vector3.right || RotationAxis == Vector3.left)
                angle = rotationEuler.x;
            else if (RotationAxis == Vector3.up || RotationAxis == Vector3.down)
                angle = rotationEuler.y;
            else if (RotationAxis == Vector3.forward || RotationAxis == Vector3.back)
                angle = rotationEuler.z;
            
            return angle;
        }
    }
    
    /// <summary>
    /// 滑块值提取器 - 基于线性移动提取滑块值
    /// </summary>
    public class SliderValueExtractor : IXRValueExtractor
    {
        public Vector3 SlideAxis = Vector3.right;
        public float MinPosition = -0.1f;
        public float MaxPosition = 0.1f;
        public Transform ReferenceTransform;
        
        public float ExtractValue(XRBaseInteractable interactable)
        {
            float rawValue = ExtractRawValue(interactable);
            return Mathf.Clamp01((rawValue - MinPosition) / (MaxPosition - MinPosition));
        }
        
        public float ExtractRawValue(XRBaseInteractable interactable)
        {
            if (interactable == null) return 0f;
            
            Vector3 position = interactable.transform.localPosition;
            if (ReferenceTransform != null)
            {
                // 转换为参考坐标系
                position = ReferenceTransform.InverseTransformPoint(interactable.transform.position);
            }
            
            // 沿滑动轴的位置
            float value = Vector3.Dot(position, SlideAxis.normalized);
            return value;
        }
    }
    
    /// <summary>
    /// 按钮值提取器 - 基于按压深度提取按钮值
    /// </summary>
    public class ButtonValueExtractor : IXRValueExtractor
    {
        public Vector3 PressAxis = Vector3.down;
        public float RestPosition = 0f;
        public float PressedPosition = -0.02f;  // 按下2cm
        public Transform ReferenceTransform;
        
        public float ExtractValue(XRBaseInteractable interactable)
        {
            float rawValue = ExtractRawValue(interactable);
            float pressDepth = RestPosition - rawValue; // 正值表示按下
            return Mathf.Clamp01(pressDepth / (RestPosition - PressedPosition));
        }
        
        public float ExtractRawValue(XRBaseInteractable interactable)
        {
            if (interactable == null) return RestPosition;
            
            Vector3 position = interactable.transform.localPosition;
            if (ReferenceTransform != null)
            {
                // 转换为参考坐标系
                position = ReferenceTransform.InverseTransformPoint(interactable.transform.position);
            }
            
            // 沿按压轴的位置
            float value = Vector3.Dot(position, PressAxis.normalized);
            return value;
        }
    }
    
    /// <summary>
    /// 提供常用值提取器的工厂类
    /// </summary>
    public static class XRValueExtractorFactory
    {
        public static IXRValueExtractor CreateLeverExtractor(Vector3 axis, float min = -0.1f, float max = 0.1f, Transform reference = null)
        {
            return new LeverValueExtractor
            {
                Axis = axis,
                MinPosition = min,
                MaxPosition = max,
                ReferenceTransform = reference
            };
        }
        
        public static IXRValueExtractor CreateKnobExtractor(Vector3 axis, float min = 0f, float max = 360f, Transform reference = null)
        {
            return new KnobValueExtractor
            {
                RotationAxis = axis,
                MinAngle = min,
                MaxAngle = max,
                ReferenceTransform = reference
            };
        }
        
        public static IXRValueExtractor CreateSliderExtractor(Vector3 axis, float min = -0.1f, float max = 0.1f, Transform reference = null)
        {
            return new SliderValueExtractor
            {
                SlideAxis = axis,
                MinPosition = min,
                MaxPosition = max,
                ReferenceTransform = reference
            };
        }
        
        public static IXRValueExtractor CreateButtonExtractor(Vector3 axis = default, float restPos = 0f, float pressedPos = -0.02f, Transform reference = null)
        {
            if (axis == default) axis = Vector3.down;
            
            return new ButtonValueExtractor
            {
                PressAxis = axis,
                RestPosition = restPos,
                PressedPosition = pressedPos,
                ReferenceTransform = reference
            };
        }
    }
}
