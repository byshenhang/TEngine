using System.Collections.Generic;
using UnityEngine;

namespace TelePresent.AudioSyncPro
{
    // 将该组件添加到 Unity 编辑器的 "GameObject" 菜单下
    [AddComponentMenu("GameObject/")]
    // 自定义分类标签为“Particles”，用于在系统中分类
    [ASP_ReactorCategory("Particles")]
    public class ASPR_EmitParticlesOnVolume : MonoBehaviour, ASP_IAudioReaction
    {
        // 显示在编辑器中的名称与信息提示
        public new string name = "Emit Particles On Volume!";
        public string info = "This Component controls the particle emission rate based on Audio Volume.";

        // 目标粒子系统列表，响应音频控制
        public List<ParticleSystem> targetParticleSystems;

        // 音量放大倍数，影响粒子发射强度
        [SerializeField] private float volumeMultiplier = 5.0f;

        // 平滑因子（插值速率），防止音量突变导致粒子剧烈变化
        [ASP_FloatSlider(0.0f, 1f)]
        [SerializeField] private float smoothness = .25f;

        // 音量灵敏度（数值越高，对微小音量越敏感）
        [ASP_FloatSlider(0.0f, 15f)]
        [SerializeField] public float sensitivity = 1.0f;

        // 音量范围：[当前值（动态）, 最小阈值, 最大阈值]
        [ASP_MinMaxSlider(0f, 1f)]
        [SerializeField] private Vector3 volumeRange = new Vector3(0f, 0.3f, 0.7f);

        // 标志是否已初始化
        private bool isInitialized = false;

        // 存储每个粒子系统的发射模块
        private Dictionary<ParticleSystem, ParticleSystem.EmissionModule> emissionModules = new Dictionary<ParticleSystem, ParticleSystem.EmissionModule>();

        // 存储每个粒子系统的初始发射速率
        private Dictionary<ParticleSystem, float> initialEmissionRates = new Dictionary<ParticleSystem, float>();

        // 控制该反应器是否启用
        [HideInInspector]
        [SerializeField] private bool isActive = true;

        // 公共属性，供外部控制是否激活该组件
        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        /// <summary>
        /// 初始化函数：记录粒子系统初始状态
        /// </summary>
        public void Initialize(Vector3 _initialPosition, Vector3 initialScale, Quaternion initialRotation)
        {
            if (!IsActive) return;

            // 清空旧数据
            emissionModules.Clear();
            initialEmissionRates.Clear();

            if (targetParticleSystems != null && targetParticleSystems.Count > 0)
            {
                foreach (var ps in targetParticleSystems)
                {
                    if (ps != null)
                    {
                        var emissionModule = ps.emission;
                        // 存储发射模块和其初始速率
                        emissionModules[ps] = emissionModule;
                        initialEmissionRates[ps] = emissionModule.rateOverTime.constant;
                    }
                }
            }

            isInitialized = true;
        }

        /// <summary>
        /// 音频响应函数，根据音量调节粒子发射速率
        /// </summary>
        public void React(AudioSourcePlus audioSourcePlus, Transform targetTransform, float rmsValue, float[] spectrumData)
        {
            if (!isInitialized || !IsActive || targetParticleSystems == null || targetParticleSystems.Count == 0) return;

            // 计算当前音量（RMS乘以灵敏度）
            float volume = rmsValue * sensitivity;

            // 平滑处理当前音量值，防止突变
            volumeRange.x = Mathf.Lerp(volumeRange.x, volume, Time.deltaTime * (1.0f / Mathf.Clamp(smoothness, 0.01f, 10.0f)));

            // 如果当前音量低于最小阈值，则不触发发射调整
            if (volumeRange.x < volumeRange.y)
            {
                return;
            }

            // 计算当前音量在最小和最大阈值之间的相对位置 [0, 1]
            float relativeMultiplier = Mathf.InverseLerp(volumeRange.y, volumeRange.z, volumeRange.x) * volumeMultiplier;
            float emissionRate = relativeMultiplier;

            // 将计算出的发射速率应用到每个目标粒子系统
            foreach (var ps in targetParticleSystems)
            {
                if (ps != null && emissionModules.ContainsKey(ps))
                {
                    var emissionModule = emissionModules[ps];
                    emissionModule.rateOverTime = emissionRate;
                }
            }
        }

        /// <summary>
        /// 重置粒子系统到最初始状态（初始发射速率）
        /// </summary>
        public void ResetToOriginalState(Transform targetTransform)
        {
            if (!isInitialized || targetParticleSystems == null || targetParticleSystems.Count == 0) return;

            foreach (var ps in targetParticleSystems)
            {
                if (ps != null && emissionModules.ContainsKey(ps))
                {
                    var emissionModule = emissionModules[ps];
                    if (initialEmissionRates.ContainsKey(ps))
                    {
                        // 恢复初始发射速率
                        emissionModule.rateOverTime = initialEmissionRates[ps];
                    }
                }
            }

            // 重置音量
            volumeRange.x = 0f;
        }
    }
}
