using System;
using System.Collections;
using TEngine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic.Example
{
    /// <summary>
    /// 控制面板示例 - 展示如何创建交互式VR控制面板
    /// </summary>
    public class ControlPanelExample : MonoBehaviour
    {
        [SerializeField] private XRISceneInteractionLogic _interactionLogic;
        [SerializeField] private XRBaseInteractable _powerButton;
        [SerializeField] private XRBaseInteractable _volumeKnob;
        [SerializeField] private XRBaseInteractable _temperatureSlider;

        [SerializeField] private GameObject _statusLight;
        [SerializeField] private TextMesh _volumeText;
        [SerializeField] private TextMesh _temperatureText;

        private float _volume = 0f;
        private float _temperature = 20f;
        private bool _isPoweredOn = false;

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
            UpdateUI();
        }

        /// <summary>
        /// 设置控制面板上的交互控件
        /// </summary>
        private void SetupControls()
        {
            // 1. 使用预设应用按钮行为
            var buttonPreset = new ButtonPreset()
            {
                ButtonAxis = Vector3.up,
                PressDistance = 0.02f,
                HasSpring = true,
                EnableHaptics = true
            };

            if (_powerButton != null)
            {
                buttonPreset.ApplyToInteractable(_powerButton, _interactionLogic);

                // 注册电源按钮事件
                _interactionLogic.RegisterInteractable(
                    _powerButton,
                    selectHandler: (args) =>
                    {
                        _isPoweredOn = !_isPoweredOn;
                        OnPowerStateChanged();
                    }
                );

                Log.Info("电源按钮设置完成");
            }

            // 2. 使用预设应用旋钮行为
            var knobPreset = new KnobPreset()
            {
                RotationAxis = Vector3.forward,
                MinAngle = 0f,
                MaxAngle = 270f,
            };

            if (_volumeKnob != null)
            {
                knobPreset.ApplyToInteractable(_volumeKnob, _interactionLogic);
                Log.Info("音量旋钮设置完成");
            }

            // 3. 手动设置滑块组件
            if (_temperatureSlider != null)
            {
                // 添加滑块约束
                var sliderConstraint = _temperatureSlider.gameObject.AddComponent<SingleAxisConstraint>();
                sliderConstraint.Axis = Vector3.right;
                sliderConstraint.MinLimit = -0.1f;
                sliderConstraint.MaxLimit = 0.1f;

                Log.Info("温度滑块设置完成");
            }

            // 持续监听音量和温度变化
            StartCoroutine(MonitorControlValues());
        }

        /// <summary>
        /// 监控控制值的变化
        /// </summary>
        private IEnumerator MonitorControlValues()
        {
            var knobExtractor = new KnobValueExtractor()
            {
                RotationAxis = Vector3.forward,
                MinAngle = 0f,
                MaxAngle = 270f
            };

            var sliderExtractor = new SliderValueExtractor()
            {
                SlideAxis = Vector3.right,
                MinPosition = -0.1f,
                MaxPosition = 0.1f
            };

            while (true)
            {
                // 使用值提取器获取当前旋钮值
                if (_volumeKnob != null)
                {
                    float newVolume = knobExtractor.ExtractValue(_volumeKnob);
                    if (Math.Abs(_volume - newVolume) > 0.01f)
                    {
                        _volume = newVolume;
                        UpdateVolume(_volume);
                    }
                }

                // 使用值提取器获取当前滑块值
                if (_temperatureSlider != null)
                {
                    float normalizedTemp = sliderExtractor.ExtractValue(_temperatureSlider);
                    float newTemp = Mathf.Lerp(15f, 30f, normalizedTemp); // 映射到15-30度
                    if (Math.Abs(_temperature - newTemp) > 0.1f)
                    {
                        _temperature = newTemp;
                        UpdateTemperature(_temperature);
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// 更新音量显示和实际音量
        /// </summary>
        private void UpdateVolume(float volume)
        {
            Log.Info($"音量调整为: {volume * 100:F0}%");

            // 更新UI显示
            if (_volumeText != null)
            {
                _volumeText.text = $"{volume * 100:F0}%";
            }

            // 实际应用音量变化 (例如调整AudioSource音量)
            AudioListener.volume = _isPoweredOn ? volume : 0;
        }

        /// <summary>
        /// 更新温度显示和相关效果
        /// </summary>
        private void UpdateTemperature(float temperature)
        {
            Log.Info($"温度设置为: {temperature:F1}°C");

            // 更新UI显示
            if (_temperatureText != null)
            {
                _temperatureText.text = $"{temperature:F1}°C";
            }

            // 可以在这里添加温度变化的实际效果
            // 例如改变场景中某些材质颜色，或启用粒子效果等
        }

        /// <summary>
        /// 处理电源状态变化
        /// </summary>
        private void OnPowerStateChanged()
        {
            Log.Info($"电源状态: {(_isPoweredOn ? "开启" : "关闭")}");

            // 更新UI和功能状态
            UpdateUI();

            // 如果设备关闭，将音量设为0
            if (!_isPoweredOn)
            {
                AudioListener.volume = 0;
            }
            else
            {
                // 恢复之前的音量设置
                AudioListener.volume = _volume;
            }
        }

        /// <summary>
        /// 更新UI显示
        /// </summary>
        private void UpdateUI()
        {
            // 更新状态指示灯
            if (_statusLight != null)
            {
                var renderer = _statusLight.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // 开启时显示绿色，关闭时显示红色
                    renderer.material.color = _isPoweredOn ? Color.green : Color.red;
                }
            }

            // 更新文本显示
            if (_volumeText != null)
            {
                _volumeText.text = _isPoweredOn ? $"{_volume * 100:F0}%" : "OFF";
            }

            if (_temperatureText != null)
            {
                _temperatureText.text = _isPoweredOn ? $"{_temperature:F1}°C" : "--.-°C";
            }
        }
    }
}
