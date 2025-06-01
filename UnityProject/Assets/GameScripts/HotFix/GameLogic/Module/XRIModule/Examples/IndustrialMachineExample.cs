using System;
using System.Collections;
using TEngine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic.Example
{
    /// <summary>
    /// 工业机械模拟示例 - 展示如何创建工业设备控制面板
    /// </summary>
    public class IndustrialMachineExample : MonoBehaviour
    {
        [SerializeField] private XRISceneInteractionLogic _interactionLogic;

        [Header("控制器")]
        [SerializeField] private XRBaseInteractable _emergencyStopButton;
        [SerializeField] private XRBaseInteractable _modeLever;
        [SerializeField] private XRBaseInteractable _pressureValve;
        [SerializeField] private XRBaseInteractable _temperatureGauge;

        [Header("反馈元素")]
        [SerializeField] private GameObject _alarmLight;
        [SerializeField] private TextMesh _statusDisplay;
        [SerializeField] private AudioSource _alarmSound;

        private bool _isEmergencyStopped = false;
        private string _machineMode = "待机"; // 待机, 手动, 自动
        private float _pressure = 0f; // 0-10 bar

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

            SetupControls();
            UpdateStatusDisplay();
        }

        /// <summary>
        /// 设置控制器交互行为
        /// </summary>
        private void SetupControls()
        {
            // 1. 紧急停止按钮
            if (_emergencyStopButton != null)
            {
                var buttonConstraint = _emergencyStopButton.gameObject.AddComponent<SingleAxisConstraint>();
                buttonConstraint.Axis = Vector3.down;
                buttonConstraint.MinLimit = 0f;
                buttonConstraint.MaxLimit = 0.05f;

                _interactionLogic.RegisterInteractable(
                    _emergencyStopButton,
                    selectHandler: (args) =>
                    {
                        ToggleEmergencyStop();
                    }
                );

                Log.Info("紧急停止按钮设置完成");
            }

            // 2. 模式选择拉杆
            if (_modeLever != null)
            {
                var leverConstraint = _modeLever.gameObject.AddComponent<RotationAxisConstraint>();
                leverConstraint.Axis = Vector3.right;
                leverConstraint.MinAngle = -45f;
                leverConstraint.MaxAngle = 45f;

                var leverExtractor = new LeverValueExtractor()
                {
                
                };

                StartCoroutine(MonitorLeverPosition(_modeLever, leverExtractor));

                Log.Info("模式选择拉杆设置完成");
            }

            // 3. 压力阀门
            if (_pressureValve != null)
            {
                var valveConstraint = _pressureValve.gameObject.AddComponent<RotationAxisConstraint>();
                valveConstraint.Axis = Vector3.forward;
                valveConstraint.MinAngle = 0f;
                valveConstraint.MaxAngle = 360f;

                var knobExtractor = new KnobValueExtractor()
                {
                    RotationAxis = Vector3.forward,
                    MinAngle = 0f,
                    MaxAngle = 360f
                };

                StartCoroutine(MonitorPressureValve(_pressureValve, knobExtractor));

                Log.Info("压力阀门设置完成");
            }

            // 4. 温度计（只读显示）
            if (_temperatureGauge != null)
            {
                var boundsConstraint = _temperatureGauge.gameObject.AddComponent<BoundsConstraint>();
                Log.Info("温度计设置完成");
            }
        }

        /// <summary>
        /// 切换紧急停止状态
        /// </summary>
        private void ToggleEmergencyStop()
        {
            _isEmergencyStopped = !_isEmergencyStopped;

            Log.Warning($"紧急停止状态变化: {(_isEmergencyStopped ? "启动" : "解除")}");

            if (_alarmSound != null)
            {
                if (_isEmergencyStopped)
                    _alarmSound.Play();
                else
                    _alarmSound.Stop();
            }

            UpdateStatusDisplay();

            if (_isEmergencyStopped)
                _pressure = 0f;
        }

        /// <summary>
        /// 监控拉杆位置
        /// </summary>
        private IEnumerator MonitorLeverPosition(XRBaseInteractable lever, LeverValueExtractor extractor)
        {
            while (true)
            {
                if (!_isEmergencyStopped)
                {
                    float leverValue = extractor.ExtractValue(lever);

                    string newMode;
                    if (leverValue < 0.33f)
                        newMode = "待机";
                    else if (leverValue < 0.66f)
                        newMode = "手动";
                    else
                        newMode = "自动";

                    if (newMode != _machineMode)
                    {
                        _machineMode = newMode;
                        Log.Info($"机器模式切换为: {_machineMode}");
                        UpdateStatusDisplay();
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// 监控压力阀门
        /// </summary>
        private IEnumerator MonitorPressureValve(XRBaseInteractable valve, KnobValueExtractor extractor)
        {
            while (true)
            {
                if (!_isEmergencyStopped)
                {
                    float valveValue = extractor.ExtractValue(valve);
                    float newPressure = valveValue * 10f;

                    if (Math.Abs(_pressure - newPressure) > 0.1f)
                    {
                        _pressure = newPressure;
                        Log.Info($"压力调整为: {_pressure:F1} bar");
                        UpdateStatusDisplay();
                        UpdatePressureEffects();
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// 处理压力变化带来的其他效果
        /// </summary>
        private void UpdatePressureEffects()
        {
            if (!_isEmergencyStopped)
            {
                float temperature = 20f + _pressure * 5f;
                UpdateTemperatureGauge(temperature);
            }

            if (_pressure > 8.5f && !_isEmergencyStopped)
            {
                Log.Warning($"压力过高警告: {_pressure:F1} bar！");

                if (_alarmLight != null)
                    StartCoroutine(FlashWarningLight());
            }
        }

        /// <summary>
        /// 更新温度计显示
        /// </summary>
        private void UpdateTemperatureGauge(float temperature)
        {
            float rotationAmount = Mathf.Lerp(0f, 270f, (temperature - 20f) / 50f);
            _temperatureGauge.transform.localRotation = Quaternion.Euler(0, 0, -rotationAmount);
        }

        /// <summary>
        /// 闪烁警告灯
        /// </summary>
        private IEnumerator FlashWarningLight()
        {
            if (_alarmLight == null) yield break;

            var renderer = _alarmLight.GetComponent<Renderer>();
            if (renderer == null) yield break;

            Color originalColor = renderer.material.color;

            for (int i = 0; i < 5; i++)
            {
                renderer.material.color = Color.red;
                yield return new WaitForSeconds(0.2f);
                renderer.material.color = Color.yellow;
                yield return new WaitForSeconds(0.2f);
            }

            renderer.material.color = originalColor;
        }

        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void UpdateStatusDisplay()
        {
            if (_statusDisplay != null)
            {
                string statusText = _isEmergencyStopped
                    ? "紧急停止！\n请重启系统"
                    : $"模式: {_machineMode}\n压力: {_pressure:F1} bar";

                _statusDisplay.text = statusText;
                _statusDisplay.color = _isEmergencyStopped ? Color.red : Color.green;
            }
        }
    }
}
