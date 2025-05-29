using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// XR交互预设接口 - 用于定义可复用的交互逻辑
    /// </summary>
    public interface IXRInteractionPreset
    {
        /// <summary>
        /// 预设的名称
        /// </summary>
        string PresetName { get; }
        
        /// <summary>
        /// 预设的描述
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 将预设应用到交互物体
        /// </summary>
        void ApplyToInteractable(XRBaseInteractable interactable, XRISceneInteractionLogic logic);
        
        /// <summary>
        /// 检查预设是否适用于指定的交互物体
        /// </summary>
        bool IsApplicableTo(XRBaseInteractable interactable);
    }
    
    /// <summary>
    /// XR交互预设基类 - 预设的基础实现
    /// </summary>
    [Serializable]
    public abstract class XRInteractionPresetBase : IXRInteractionPreset
    {
        public abstract string PresetName { get; }
        public abstract string Description { get; }
        
        public abstract void ApplyToInteractable(XRBaseInteractable interactable, XRISceneInteractionLogic logic);
        
        public virtual bool IsApplicableTo(XRBaseInteractable interactable)
        {
            return interactable != null;
        }
        
        /// <summary>
        /// 向交互物体添加值提取器
        /// </summary>
        protected T AddValueExtractor<T>(XRBaseInteractable interactable) where T : MonoBehaviour, IXRValueExtractor
        {
            var extractor = interactable.GetComponent<T>();
            if (extractor == null)
            {
                extractor = interactable.gameObject.AddComponent<T>();
            }
            return extractor;
        }
        
        /// <summary>
        /// 向交互物体添加约束
        /// </summary>
        protected T AddConstraint<T>(XRBaseInteractable interactable) where T : XRInteractionConstraint
        {
            var constraint = interactable.GetComponent<T>();
            if (constraint == null)
            {
                constraint = interactable.gameObject.AddComponent<T>();
            }
            return constraint;
        }
    }
    
    /// <summary>
    /// 按钮交互预设 - 提供标准按钮行为
    /// </summary>
    [Serializable]
    public class ButtonPreset : XRInteractionPresetBase
    {        
        public override string PresetName => "标准按钮";
        public override string Description => "提供标准按钮行为，包括按下、释放事件和可选的视觉/音频反馈";
        
        [Tooltip("按钮移动轴")]
        public Vector3 ButtonAxis = Vector3.up;
        
        [Tooltip("按钮按下的最大距离")]
        public float PressDistance = 0.01f;
        
        [Tooltip("按钮是否有弹力返回")]
        public bool HasSpring = true;
        
        [Tooltip("是否添加触觉反馈")]
        public bool EnableHaptics = true;
        
        [Tooltip("触觉反馈强度")]
        public float HapticIntensity = 0.5f;
        
        [Tooltip("触觉反馈时长(秒)")]
        public float HapticDuration = 0.1f;
        
        public override void ApplyToInteractable(XRBaseInteractable interactable, XRISceneInteractionLogic logic)
        {
            if (interactable == null || logic == null) return;
            
            // 确保是XRSimpleInteractable
            if (!(interactable is XRSimpleInteractable) && !(interactable is XRGrabInteractable))
            {
                Log.Warning($"按钮预设最好应用于XRSimpleInteractable，而不是{interactable.GetType().Name}");
            }
            
            // 添加单轴约束
            var constraint = AddConstraint<SingleAxisConstraint>(interactable);
            constraint.Axis = ButtonAxis;
            constraint.MinLimit = -PressDistance;
            constraint.MaxLimit = 0;
            
            // 添加按钮值提取器 (这里是假设有ButtonValueExtractor类)
            // var extractor = AddValueExtractor<ButtonValueExtractor>(interactable);
            
            // 注册按钮事件
            logic.RegisterInteractable(
                interactable,
                selectHandler: (args) => {
                    // 按钮被选中/按下
                    Log.Info($"按钮 {interactable.name} 被按下");
                    
                    // 添加触觉反馈
                    if (EnableHaptics && args.interactorObject is XRBaseControllerInteractor controllerInteractor)
                    {
                        controllerInteractor.xrController?.SendHapticImpulse(HapticIntensity, HapticDuration);
                    }
                },
                deselectHandler: (args) => {
                    // 按钮被释放
                    Log.Info($"按钮 {interactable.name} 被释放");
                }
            );
            
            Log.Info($"已将按钮预设应用到 {interactable.name}");
        }
        
        public override bool IsApplicableTo(XRBaseInteractable interactable)
        {
            // 按钮预设适用于简单交互物体或可抓取物体
            return interactable is XRSimpleInteractable || interactable is XRGrabInteractable;
        }
    }
    
    /// <summary>
    /// 拉杆交互预设 - 提供标准拉杆行为
    /// </summary>
    [Serializable]
    public class LeverPreset : XRInteractionPresetBase
    {
        public override string PresetName => "标准拉杆";
        public override string Description => "提供标准拉杆行为，包括拉动、释放事件和位置检测";
        
        [Tooltip("拉杆移动轴")]
        public Vector3 LeverAxis = Vector3.up;
        
        [Tooltip("拉杆最小位置")]
        public float MinPosition = -0.1f;
        
        [Tooltip("拉杆最大位置")]
        public float MaxPosition = 0.1f;
        
        [Tooltip("激活阈值(0-1范围内)")]
        public float ActivationThreshold = 0.8f;
        
        [Tooltip("是否添加触觉反馈")]
        public bool EnableHaptics = true;
        
        public override void ApplyToInteractable(XRBaseInteractable interactable, XRISceneInteractionLogic logic)
        {
            if (interactable == null || logic == null) return;
            
            // 确保是XRGrabInteractable
            if (!(interactable is XRGrabInteractable))
            {
                Log.Warning($"拉杆预设应该应用于XRGrabInteractable，而不是{interactable.GetType().Name}");
                return;
            }
            
            var grabInteractable = interactable as XRGrabInteractable;
            
            // 配置抓取物体属性
            grabInteractable.throwOnDetach = false;
            grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
            
            // 添加单轴约束
            var constraint = AddConstraint<SingleAxisConstraint>(interactable);
            constraint.Axis = LeverAxis;
            constraint.MinLimit = MinPosition;
            constraint.MaxLimit = MaxPosition;
            
            // 添加拉杆值提取器
            // var extractor = AddValueExtractor<LeverValueExtractor>(interactable);
            
            // 注册拉杆事件
            logic.RegisterInteractable(
                interactable,
                selectHandler: (args) => {
                    // 拉杆被抓取
                    Log.Info($"拉杆 {interactable.name} 被抓取");
                },
                deselectHandler: (args) => {
                    // 拉杆被释放，检查位置
                    // 在实际应用中应该使用值提取器获取精确值
                    Vector3 localPos = interactable.transform.localPosition;
                    float leverValue = Mathf.InverseLerp(MinPosition, MaxPosition, Vector3.Dot(localPos, LeverAxis.normalized));
                    
                    Log.Info($"拉杆 {interactable.name} 被释放，当前值: {leverValue:F2}");
                    
                    // 检查是否超过激活阈值
                    if (leverValue >= ActivationThreshold)
                    {
                        Log.Info($"拉杆 {interactable.name} 激活 (超过阈值 {ActivationThreshold:F2})");
                        
                        // 添加触觉反馈
                        if (EnableHaptics && args.interactorObject is XRBaseControllerInteractor controllerInteractor)
                        {
                            controllerInteractor.xrController?.SendHapticImpulse(0.7f, 0.2f);
                        }
                    }
                }
            );
            
            Log.Info($"已将拉杆预设应用到 {interactable.name}");
        }
        
        public override bool IsApplicableTo(XRBaseInteractable interactable)
        {
            // 拉杆预设仅适用于可抓取物体
            return interactable is XRGrabInteractable;
        }
    }
    
    /// <summary>
    /// 旋钮交互预设 - 提供标准旋钮行为
    /// </summary>
    [Serializable]
    public class KnobPreset : XRInteractionPresetBase
    {
        public override string PresetName => "旋转旋钮";
        public override string Description => "提供标准旋钮行为，包括旋转和值检测";
        
        [Tooltip("旋钮旋转轴")]
        public Vector3 RotationAxis = Vector3.forward;
        
        [Tooltip("最小旋转角度")]
        public float MinAngle = 0f;
        
        [Tooltip("最大旋转角度")]
        public float MaxAngle = 270f;
        
        [Tooltip("是否添加触觉反馈")]
        public bool EnableHaptics = true;
        
        public override void ApplyToInteractable(XRBaseInteractable interactable, XRISceneInteractionLogic logic)
        {
            if (interactable == null || logic == null) return;
            
            // 确保是XRGrabInteractable
            if (!(interactable is XRGrabInteractable))
            {
                Log.Warning($"旋钮预设应该应用于XRGrabInteractable，而不是{interactable.GetType().Name}");
                return;
            }
            
            var grabInteractable = interactable as XRGrabInteractable;
            
            // 配置抓取物体属性
            grabInteractable.throwOnDetach = false;
            grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
            grabInteractable.trackPosition = false; // 只跟踪旋转
            grabInteractable.trackRotation = true;
            
            // 添加旋转约束
            var constraint = AddConstraint<RotationAxisConstraint>(interactable);
            constraint.Axis = RotationAxis;
            constraint.MinAngle = MinAngle;
            constraint.MaxAngle = MaxAngle;
            
            // 添加旋钮值提取器
            // var extractor = AddValueExtractor<KnobValueExtractor>(interactable);
            
            // 注册旋钮事件
            logic.RegisterInteractable(
                interactable,
                selectHandler: (args) => {
                    // 旋钮被抓取
                    Log.Info($"旋钮 {interactable.name} 被抓取");
                },
                deselectHandler: (args) => {
                    // 旋钮被释放，检查旋转值
                    // 这里是简化版，实际应用中应该使用值提取器
                    float currentAngle = 0f;
                    Vector3 euler = interactable.transform.localEulerAngles;
                    
                    if (RotationAxis == Vector3.right || RotationAxis == Vector3.left)
                        currentAngle = euler.x;
                    else if (RotationAxis == Vector3.up || RotationAxis == Vector3.down)
                        currentAngle = euler.y;
                    else 
                        currentAngle = euler.z;
                    
                    // 标准化角度到0-360范围
                    currentAngle = (currentAngle + 360) % 360;
                    float normalizedValue = Mathf.InverseLerp(MinAngle, MaxAngle, currentAngle);
                    
                    Log.Info($"旋钮 {interactable.name} 被释放，当前角度: {currentAngle:F1}°, 值: {normalizedValue:F2}");
                    
                    // 添加触觉反馈
                    if (EnableHaptics && args.interactorObject is XRBaseControllerInteractor controllerInteractor)
                    {
                        controllerInteractor.xrController?.SendHapticImpulse(0.3f, 0.1f);
                    }
                }
            );
            
            Log.Info($"已将旋钮预设应用到 {interactable.name}");
        }
        
        public override bool IsApplicableTo(XRBaseInteractable interactable)
        {
            // 旋钮预设仅适用于可抓取物体
            return interactable is XRGrabInteractable;
        }
    }
    
    /// <summary>
    /// 交互预设管理器 - 管理和提供预设
    /// </summary>
    public static class XRInteractionPresetManager
    {
        private static Dictionary<string, IXRInteractionPreset> _presets = new Dictionary<string, IXRInteractionPreset>();
        
        static XRInteractionPresetManager()
        {
            // 注册默认预设
            RegisterPreset(new ButtonPreset());
            RegisterPreset(new LeverPreset());
            RegisterPreset(new KnobPreset());
        }
        
        /// <summary>
        /// 注册交互预设
        /// </summary>
        public static void RegisterPreset(IXRInteractionPreset preset)
        {
            if (preset != null && !string.IsNullOrEmpty(preset.PresetName))
            {
                _presets[preset.PresetName] = preset;
            }
        }
        
        /// <summary>
        /// 获取所有可用预设
        /// </summary>
        public static IEnumerable<IXRInteractionPreset> GetAllPresets()
        {
            return _presets.Values;
        }
        
        /// <summary>
        /// 根据名称获取预设
        /// </summary>
        public static IXRInteractionPreset GetPreset(string presetName)
        {
            if (_presets.TryGetValue(presetName, out var preset))
            {
                return preset;
            }
            return null;
        }
        
        /// <summary>
        /// 获取适用于指定交互物体的预设列表
        /// </summary>
        public static IEnumerable<IXRInteractionPreset> GetApplicablePresets(XRBaseInteractable interactable)
        {
            if (interactable == null) yield break;
            
            foreach (var preset in _presets.Values)
            {
                if (preset.IsApplicableTo(interactable))
                {
                    yield return preset;
                }
            }
        }
    }
}
