using System;
using System.Collections;
using TEngine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic.Example
{
    /// <summary>
    /// 自定义交互预设示例 - 展示如何创建和应用自定义XR交互预设
    /// </summary>
    public class CustomPresetsExample : MonoBehaviour
    {
        [SerializeField] private XRISceneInteractionLogic _interactionLogic;
        [SerializeField] private XRGrabInteractable _radioDial;
        [SerializeField] private XRGrabInteractable _temperatureDial;
        
        [SerializeField] private AudioSource _clickSound;
        [SerializeField] private TextMesh _radioChannelDisplay;
        [SerializeField] private TextMesh _temperatureDisplay;
        
        private void Start()
        {
            // 确保有交互逻辑组件
            if (_interactionLogic == null)
            {
                _interactionLogic = FindObjectOfType<XRISceneInteractionLogic>();
                if (_interactionLogic == null)
                {
                    Log.Error("找不到XRISceneInteractionLogic组件，请确保场景中存在此组件");
                    return;
                }
            }
            
            // 注册自定义预设
            XRInteractionPresetManager.RegisterPreset(new RotaryDialPreset());
            
            // 应用预设到拨盘
            var dialPreset = XRInteractionPresetManager.GetPreset("旋转拨盘") as RotaryDialPreset;
            if (dialPreset != null)
            {
                // 应用到无线电频道选择拨盘
                if (_radioDial != null)
                {
                    dialPreset.DialAxis = Vector3.forward;
                    dialPreset.Steps = 10;
                    dialPreset.HasDetents = true;
                    dialPreset.DetentStrength = 0.5f;
                    dialPreset.ApplyToInteractable(_radioDial, _interactionLogic);
                    
                    // 添加事件监听
                    var dialEvents = _radioDial.gameObject.AddComponent<DialEvents>();
                    dialEvents.OnDialChanged.AddListener((step, total) => {
                        int channel = step + 1;
                        Log.Info($"无线电频道切换到: {channel}");
                        
                        // 播放点击音效
                        if (_clickSound != null)
                        {
                            _clickSound.pitch = 0.8f + (channel / 10f);
                            _clickSound.Play();
                        }
                        
                        // 更新显示
                        if (_radioChannelDisplay != null)
                        {
                            _radioChannelDisplay.text = $"CH {channel}";
                        }
                    });
                    
                    Log.Info("无线电频道拨盘设置完成");
                }
                
                // 应用到温度控制拨盘，但使用不同参数
                if (_temperatureDial != null)
                {
                    var tempDialPreset = new RotaryDialPreset()
                    {
                        DialAxis = Vector3.up,
                        Steps = 20,
                        HasDetents = false
                    };
                    tempDialPreset.ApplyToInteractable(_temperatureDial, _interactionLogic);
                    
                    // 添加事件监听
                    var dialEvents = _temperatureDial.gameObject.AddComponent<DialEvents>();
                    dialEvents.OnDialChanged.AddListener((step, total) => {
                        float temperature = Mathf.Lerp(15f, 35f, (float)step / total);
                        Log.Info($"温度设置为: {temperature:F1}°C");
                        
                        // 更新显示
                        if (_temperatureDisplay != null)
                        {
                            _temperatureDisplay.text = $"{temperature:F1}°C";
                        }
                    });
                    
                    Log.Info("温度控制拨盘设置完成");
                }
            }
        }
    }
    
    /// <summary>
    /// 旋转拨盘预设 - 带有刻度和阻尼感的旋转拨盘控制器
    /// </summary>
    [Serializable]
    public class RotaryDialPreset : XRInteractionPresetBase
    {
        public override string PresetName => "旋转拨盘";
        public override string Description => "带有刻度和阻尼感的旋转拨盘控制器";
        
        [Tooltip("旋转轴")]
        public Vector3 DialAxis = Vector3.forward;
        
        [Tooltip("刻度数量")]
        public int Steps = 10;
        
        [Tooltip("是否有阻尼感")]
        public bool HasDetents = true;
        
        [Tooltip("阻尼强度")]
        public float DetentStrength = 0.5f;
        
        public override void ApplyToInteractable(XRBaseInteractable interactable, XRISceneInteractionLogic logic)
        {
            if (interactable == null || logic == null) return;
            
            // 确保是XRGrabInteractable
            var grabInteractable = interactable as XRGrabInteractable;
            if (grabInteractable == null)
            {
                Log.Warning($"旋转拨盘预设应该应用于XRGrabInteractable");
                return;
            }
            
            // 配置抓取物体属性
            grabInteractable.throwOnDetach = false;
            grabInteractable.trackRotation = true;
            
            // 添加旋转约束
            var constraint = AddConstraint<RotationAxisConstraint>(interactable);
            constraint.Axis = DialAxis;
            constraint.MinAngle = 0f;
            constraint.MaxAngle = 360f;
            
            // 添加旋钮值提取器
            var extractor = new KnobValueExtractor() {
                RotationAxis = DialAxis,
                MinAngle = 0f,
                MaxAngle = 360f
            };
            
            // 注册旋转事件和阻尼效果
            if (HasDetents)
            {
                // 持续监控并应用阻尼效果
                var monitorComponent = interactable.gameObject.AddComponent<DialDetentMonitor>();
                monitorComponent.Initialize(extractor, Steps, DetentStrength);
            }
            
            // 注册值变化事件
            logic.RegisterInteractable(
                interactable,
                selectHandler: (args) => { /* 开始旋转 */ },
                deselectHandler: (args) => {
                    // 获取当前值和对应刻度
                    float value = extractor.ExtractValue(interactable);
                    int step = Mathf.RoundToInt(value * Steps);
                    if (step >= Steps) step = 0; // 处理边界情况
                    
                    Log.Info($"拨盘选择了刻度: {step}/{Steps}");
                    
                    // 可以通过事件通知其他系统
                    if (interactable.TryGetComponent<DialEvents>(out var events))
                    {
                        events.OnDialChanged.Invoke(step, Steps);
                    }
                }
            );
            
            Log.Info($"已将旋转拨盘预设应用到 {interactable.name}");
        }
        
        public override bool IsApplicableTo(XRBaseInteractable interactable)
        {
            return interactable is XRGrabInteractable;
        }
    }
    
    /// <summary>
    /// 辅助类：监控并应用阻尼效果
    /// </summary>
    public class DialDetentMonitor : MonoBehaviour
    {
        private KnobValueExtractor _extractor;
        private int _steps;
        private float _detentStrength;
        private XRBaseInteractable _interactable;
        private XRGrabInteractable _grabInteractable;
        
        public void Initialize(KnobValueExtractor extractor, int steps, float detentStrength)
        {
            _extractor = extractor;
            _steps = steps;
            _detentStrength = detentStrength;
            _interactable = GetComponent<XRBaseInteractable>();
            _grabInteractable = _interactable as XRGrabInteractable;
        }
        
        private void Update()
        {
            if (_grabInteractable != null && _grabInteractable.isSelected)
            {
                ApplyDetents();
            }
        }
        
        /// <summary>
        /// 应用刻度阻尼效果
        /// </summary>
        private void ApplyDetents()
        {
            // 获取当前值
            float value = _extractor.ExtractValue(_interactable);
            
            // 计算最近的刻度
            float stepSize = 1.0f / _steps;
            float targetValue = Mathf.Round(value / stepSize) * stepSize;
            
            // 应用轻微的向刻度吸引力
            float currentRawValue = _extractor.ExtractRawValue(_interactable);
            float targetRawValue = targetValue * 360f; // 假设是0-360度范围
            
            // 应用柔和的旋转力
            if (_grabInteractable != null && Mathf.Abs(currentRawValue - targetRawValue) > 0.1f)
            {
                float lerpFactor = Time.deltaTime * _detentStrength * 5f;
                Quaternion currentRotation = transform.localRotation;
                Quaternion targetRotation = Quaternion.Euler(
                    currentRotation.eulerAngles.x, 
                    currentRotation.eulerAngles.y,
                    targetRawValue
                );
                
                transform.localRotation = Quaternion.Slerp(currentRotation, targetRotation, lerpFactor);
                
                // 如果有XR控制器，还可以添加触觉反馈
                var interactor = _grabInteractable.selectingInteractor as XRBaseControllerInteractor;
                if (interactor != null && interactor.xrController != null)
                {
                    float hapticIntensity = _detentStrength * 0.3f * Mathf.Clamp01(1f - Mathf.Abs(currentRawValue - targetRawValue) / 5f);
                    if (hapticIntensity > 0.05f)
                    {
                        interactor.xrController.SendHapticImpulse(hapticIntensity, 0.05f);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 事件通知辅助类
    /// </summary>
    public class DialEvents : MonoBehaviour
    {
        [Serializable]
        public class DialChangedEvent : UnityEvent<int, int> { } // 当前刻度, 总刻度数
        
        public DialChangedEvent OnDialChanged = new DialChangedEvent();
    }
}
