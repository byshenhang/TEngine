using UnityEngine;
using UnityEngine.UI;
namespace AudioReactiveShader
{
    /// <summary>
    /// 动态频段音频数据解释器
    /// 支持可变数量的频段（1-63个），将音频频谱数据处理后传递给着色器
    /// 相比固定5频段的解释器，提供更灵活的频段控制
    /// </summary>
    public class DinamicBandsAudioDataInterpreter : MonoBehaviour
    {
        [SerializeField] MusicReader MusicSpectrum;  // 音频频谱读取器引用
        [Tooltip("使用小于等于0的值来禁用平滑处理")] [SerializeField] float smoothSpeed;  // 平滑速度
        [SerializeField] AnimationCurve ResponseAdjustment;  // 响应调整曲线
        [Range(1, 63)] [SerializeField] int bands = 10;      // 频段数量（1-63范围）
        float[] smoothedIntensisyValues;  // 平滑处理后的强度值数组

        // 组件引用
        Renderer rend;           // 渲染器组件
        Image img;               // UI图像组件
        ParticleSystem particles; // 粒子系统组件
        Material mat;            // 目标材质
        [SerializeField] MusicSpectrumReader.MATERIAL_OUTPUT MaterialOutput;  // 材质输出类型
        
        // 粒子系统相关
        public bool soundAffectsEmmisionRate;  // 是否让声音影响粒子发射率
        float startingEmmisionRate;            // 初始粒子发射率


        /// <summary>
        /// 确保音频频谱读取器的频段数量满足当前设置的要求
        /// 如果读取器的频段数少于设置值，则调整为设置值
        /// </summary>
        private void Awake()
        {
            if (MusicSpectrum.numBands < bands) MusicSpectrum.numBands = bands;
        }

        /// <summary>
        /// 初始化组件引用和数据数组
        /// 根据材质输出类型获取相应的组件，并设置着色器参数
        /// </summary>
        void Start()
        {
            // 初始化平滑强度值数组
            smoothedIntensisyValues = new float[MusicSpectrum.numBands];
            
            // 根据材质输出类型获取相应的组件和材质
            if (MaterialOutput == MusicSpectrumReader.MATERIAL_OUTPUT.RENDERER)
            {
                rend = GetComponent<Renderer>();
                mat = rend.material;
            }
            else if (MaterialOutput == MusicSpectrumReader.MATERIAL_OUTPUT.PARTICLES)
            {
                particles = GetComponent<ParticleSystem>();
                setParticleSystem();
            }
            else  // CANVAS_IMG
            {
                img = GetComponent<Image>();
                mat = img.material;
            }
            
            // 将频段数量传递给着色器
            mat.SetFloat("_Bands", bands);
        }

        /// <summary>
        /// 设置粒子系统相关参数
        /// 获取粒子系统的材质，并记录初始发射率（如果需要音频影响发射率）
        /// </summary>
        void setParticleSystem()
        {
            if (particles != null)
            {
                var partsRend = particles.GetComponent<ParticleSystemRenderer>().material;
                mat = partsRend;
                
                // 如果启用了声音影响粒子发射率，记录初始发射率
                if (soundAffectsEmmisionRate)
                {
                    var partsEmmision = particles.emission;
                    startingEmmisionRate = partsEmmision.rateOverTime.constant;
                }
            }
            else Debug.LogWarning("未找到粒子系统组件");
        }

        /// <summary>
        /// 每帧更新音频数据并传递给着色器
        /// 处理平滑、响应调整，并影响粒子系统发射率（如果启用）
        /// </summary>
        void Update()
        {
            // 根据是否启用平滑处理来更新频段数据
            if (smoothSpeed > 0)
            {
                // 对每个频段应用平滑处理和响应调整曲线
                for (int i = 0; i <= MusicSpectrum.numBands - 1; i++)
                {
                    smoothedIntensisyValues[i] = Mathf.Lerp(
                        smoothedIntensisyValues[i], 
                        ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[i]), 
                        smoothSpeed * Time.deltaTime
                    );
                }
                // 将平滑处理后的数据传递给着色器
                mat.SetFloatArray("_FreqLevels", smoothedIntensisyValues);
            }
            else
            {
                // 直接将原始频段数据传递给着色器，不进行平滑处理
                mat.SetFloatArray("_FreqLevels", MusicSpectrum.groupedBands);
            }

            // 如果启用了声音影响粒子发射率，根据低频强度和频段数量调整发射率
            if (particles != null && soundAffectsEmmisionRate)
            {
                var partsEmmision = particles.emission;
                partsEmmision.rateOverTime = startingEmmisionRate * smoothedIntensisyValues[0] * bands;
            }
        }

    }
}
