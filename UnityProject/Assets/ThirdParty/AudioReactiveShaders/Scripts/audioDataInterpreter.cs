using UnityEngine;
using UnityEngine.UI;

namespace AudioReactiveShader
{
    /// <summary>
    /// 音频数据解释器
    /// 将音频频谱数据转换为5个频段（低频、中低频、中频、中高频、高频）
    /// 并应用平滑处理和响应调整，最终传递给着色器材质
    /// </summary>
    public class AudioDataInterpreter : MonoBehaviour
    {
        [SerializeField] MusicReader MusicSpectrum;  // 音频频谱读取器引用
        [Tooltip("使用小于等于0的值来禁用平滑处理")] [SerializeField] float smoothSpeed;  // 平滑速度
        [SerializeField] AnimationCurve ResponseAdjustment;  // 响应调整曲线
        
        // 五个频段的强度值（0-5范围）
        [Range(0, 5)] public float Low;      // 低频强度
        [Range(0, 5)] public float MidLow;   // 中低频强度
        [Range(0, 5)] public float Mid;      // 中频强度
        [Range(0, 5)] public float MidHigh;  // 中高频强度
        [Range(0, 5)] public float High;     // 高频强度

        // 组件引用
        Renderer rend;           // 渲染器组件
        Image img;               // UI图像组件
        ParticleSystem particles; // 粒子系统组件
        Material mat;            // 目标材质
        [SerializeField] MusicSpectrumReader.MATERIAL_OUTPUT MaterialOutput;  // 材质输出类型

        // 粒子系统相关
        public bool soundAffectsParticlesEmmisionRate;  // 是否让声音影响粒子发射率
        float startingEmmisionRate;                     // 初始粒子发射率

        // 频段位置索引
        private int MidLowPosition;   // 中低频在频段数组中的位置
        private int MidPosition;      // 中频在频段数组中的位置
        private int MidHighPosition;  // 中高频在频段数组中的位置
        private int HighPosition;     // 高频在频段数组中的位置

        /// <summary>
        /// 确保频段数量满足最低要求
        /// 由于需要5个频段（低、中低、中、中高、高），所以最少需要5个频段
        /// </summary>
        private void Awake()
        {
            if (MusicSpectrum.numBands < 5) MusicSpectrum.numBands = 5;
        }
        /// <summary>
        /// 初始化频段位置和组件引用
        /// 根据总频段数计算各频段在数组中的位置索引
        /// </summary>
        void Start()
        {
            // 计算各频段在频段数组中的位置
            // 将频段均匀分布到5个区间：低频、中低频、中频、中高频、高频
            MidLowPosition = (int)Mathf.Floor(MusicSpectrum.numBands / 4 - 1);      // 1/4位置
            MidPosition = (int)Mathf.Floor(MusicSpectrum.numBands / 2 - 1);         // 1/2位置
            MidHighPosition = (int)Mathf.Floor(MusicSpectrum.numBands * .75f - 1);  // 3/4位置
            HighPosition = (int)Mathf.Floor(MusicSpectrum.numBands - 1);            // 最后位置

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
                if (soundAffectsParticlesEmmisionRate)
                {
                    var partsEmmision = particles.emission;
                    startingEmmisionRate = partsEmmision.rateOverTime.constant;
                }
            }
            else Debug.LogWarning("未找到粒子系统组件");
        }

        /// <summary>
        /// 每帧更新音频数据
        /// 从音频频谱读取器获取数据，应用响应调整曲线和平滑处理
        /// 如果启用，还会影响粒子系统的发射率
        /// </summary>
        void Update()
        {
            // 根据是否启用平滑处理来更新频段值
            if (smoothSpeed > 0)
            {
                // 使用线性插值进行平滑处理，并应用响应调整曲线
                Low = Mathf.Lerp(Low, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[0]), smoothSpeed * Time.deltaTime);
                MidLow = Mathf.Lerp(MidLow, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[MidLowPosition]), smoothSpeed * Time.deltaTime);
                Mid = Mathf.Lerp(Mid, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[MidPosition]), smoothSpeed * Time.deltaTime);
                MidHigh = Mathf.Lerp(MidHigh, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[MidHighPosition]), smoothSpeed * Time.deltaTime);
                High = Mathf.Lerp(High, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[HighPosition]), smoothSpeed * Time.deltaTime);
            }
            else
            {
                // 直接使用原始频段数据，不进行平滑处理
                Low = MusicSpectrum.groupedBands[0];
                MidLow = MusicSpectrum.groupedBands[MidLowPosition];
                Mid = MusicSpectrum.groupedBands[MidPosition];
                MidHigh = MusicSpectrum.groupedBands[MidHighPosition];
                High = MusicSpectrum.groupedBands[HighPosition];
            }
            
            // 如果启用了声音影响粒子发射率，根据所有频段的总和调整发射率
            if (particles != null && soundAffectsParticlesEmmisionRate)
            {
                var partsEmmision = particles.emission;
                partsEmmision.rateOverTime = startingEmmisionRate * (Low + MidLow + Mid + MidHigh + High);
            }
        }
        /// <summary>
        /// 将最终的频段值传递给着色器
        /// 使用FixedUpdate确保数据传输的稳定性
        /// </summary>
        private void FixedUpdate()
        {
            // 将5个频段的值设置到材质的着色器属性中
            mat.SetFloat("_Low", Low);        // 低频
            mat.SetFloat("_MidLow", MidLow);  // 中低频
            mat.SetFloat("_Mid", Mid);        // 中频
            mat.SetFloat("_MidHigh", MidHigh); // 中高频
            mat.SetFloat("_High", High);      // 高频
        }
    }
}

