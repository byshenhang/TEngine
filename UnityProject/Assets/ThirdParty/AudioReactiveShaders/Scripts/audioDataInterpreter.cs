using UnityEngine;
using UnityEngine.UI;

namespace AudioReactiveShader
{
    /// <summary>
    /// 音频数据解释器
    /// 将音频频谱数据转换为5个频段（低频、中低频、中频、中高频、高频）
    /// 并应用平滑处理和响应调整，最终传递给着色器材质
    /// </summary>
    public class AudioDataInterpreter : BaseAudioDataInterpreter
    {
        // 五个频段的强度值（0-5范围）
        [Range(0, 5)] public float Low;      // 低频强度
        [Range(0, 5)] public float MidLow;   // 中低频强度
        [Range(0, 5)] public float Mid;      // 中频强度
        [Range(0, 5)] public float MidHigh;  // 中高频强度
        [Range(0, 5)] public float High;     // 高频强度

        // 频段位置索引
        private int MidLowPosition;   // 中低频在频段数组中的位置
        private int MidPosition;      // 中频在频段数组中的位置
        private int MidHighPosition;  // 中高频在频段数组中的位置
        private int HighPosition;     // 高频在频段数组中的位置

        /// <summary>
        /// 确保频段数量满足最低要求
        /// 由于需要5个频段（低、中低、中、中高、高），所以最少需要5个频段
        /// </summary>
        protected override void ValidateFrequencyBands()
        {
            if (MusicSpectrum != null && MusicSpectrum.numBands < 5) 
                MusicSpectrum.numBands = 5;
        }
        /// <summary>
        /// 初始化频段位置
        /// 根据总频段数计算各频段在数组中的位置索引
        /// </summary>
        protected override void InitializeAudioData()
        {
            if (MusicSpectrum == null) return;
            
            // 计算各频段在频段数组中的位置
            // 将频段均匀分布到5个区间：低频、中低频、中频、中高频、高频
            MidLowPosition = (int)Mathf.Floor(MusicSpectrum.numBands / 4 - 1);      // 1/4位置
            MidPosition = (int)Mathf.Floor(MusicSpectrum.numBands / 2 - 1);         // 1/2位置
            MidHighPosition = (int)Mathf.Floor(MusicSpectrum.numBands * .75f - 1);  // 3/4位置
            HighPosition = (int)Mathf.Floor(MusicSpectrum.numBands - 1);            // 最后位置
        }



        /// <summary>
        /// 处理音频数据
        /// 从音频频谱读取器获取数据，应用响应调整曲线和平滑处理
        /// </summary>
        protected override void ProcessAudioData()
        {
            // 如果MusicSpectrum为null或groupedBands为null，则不处理
            if (MusicSpectrum == null || MusicSpectrum.groupedBands == null) return;
            
            // 确保索引在有效范围内
            if (MusicSpectrum.groupedBands.Length <= HighPosition) return;
            
            // 根据是否启用平滑处理来更新频段值
            if (smoothSpeed > 0)
            {
                // 使用平滑处理和响应调整曲线
                Low = ApplySmoothing(Low, ApplyResponseAdjustment(MusicSpectrum.groupedBands[0]));
                MidLow = ApplySmoothing(MidLow, ApplyResponseAdjustment(MusicSpectrum.groupedBands[MidLowPosition]));
                Mid = ApplySmoothing(Mid, ApplyResponseAdjustment(MusicSpectrum.groupedBands[MidPosition]));
                MidHigh = ApplySmoothing(MidHigh, ApplyResponseAdjustment(MusicSpectrum.groupedBands[MidHighPosition]));
                High = ApplySmoothing(High, ApplyResponseAdjustment(MusicSpectrum.groupedBands[HighPosition]));
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
            
            // 将频段值传递给着色器
            UpdateShaderProperties();
        }
        
        /// <summary>
        /// 更新着色器属性
        /// 将频段值传递给材质的着色器属性
        /// </summary>
        private void UpdateShaderProperties()
        {
            if (!IsMaterialValid()) return;

            try
            {
                // 将5个频段的值设置到材质的着色器属性中
                mat.SetFloat("_Low", Low);        // 低频
                mat.SetFloat("_MidLow", MidLow);  // 中低频
                mat.SetFloat("_Mid", Mid);        // 中频
                mat.SetFloat("_MidHigh", MidHigh); // 中高频
                mat.SetFloat("_High", High);      // 高频
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"更新着色器属性时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 更新粒子发射率
        /// 根据所有频段的总和调整发射率
        /// </summary>
        protected override void UpdateParticleEmission()
        {
            if (particles != null && soundAffectsParticlesEmmisionRate)
            {
                var partsEmmision = particles.emission;
                partsEmmision.rateOverTime = startingEmmisionRate * (Low + MidLow + Mid + MidHigh + High);
            }
        }

    }
}

