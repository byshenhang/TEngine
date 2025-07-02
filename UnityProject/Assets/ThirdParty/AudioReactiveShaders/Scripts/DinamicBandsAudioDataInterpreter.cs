using UnityEngine;
using UnityEngine.UI;
namespace AudioReactiveShader
{
    /// <summary>
    /// 动态频段音频数据解释器
    /// 支持可变数量的频段（1-63个），将音频频谱数据处理后传递给着色器
    /// 相比固定5频段的解释器，提供更灵活的频段控制
    /// </summary>
    public class DinamicBandsAudioDataInterpreter : BaseAudioDataInterpreter
    {
        [Range(1, 63)] [SerializeField] int bands = 10;      // 频段数量（1-63范围）
        float[] smoothedIntensisyValues;  // 平滑处理后的强度值数组
        
        // 粒子系统相关
        public bool soundAffectsEmmisionRate;  // 是否让声音影响粒子发射率


        /// <summary>
        /// 确保音频频谱读取器的频段数量满足当前设置的要求
        /// 如果读取器的频段数少于设置值，则调整为设置值
        /// </summary>
        protected override void ValidateFrequencyBands()
        {
            if (MusicSpectrum != null && MusicSpectrum.numBands < bands) 
                MusicSpectrum.numBands = bands;
        }

        /// <summary>
        /// 初始化数据数组和着色器参数
        /// </summary>
        protected override void InitializeAudioData()
        {
            // 初始化平滑强度值数组
            if (MusicSpectrum != null)
            {
                smoothedIntensisyValues = new float[MusicSpectrum.numBands];
            }
            else
            {
                smoothedIntensisyValues = new float[bands];
            }
            
            // 将频段数量传递给着色器
            if (IsMaterialValid())
            {
                mat.SetFloat("_Bands", bands);
            }
        }



        /// <summary>
        /// 处理音频数据并传递给着色器
        /// 处理平滑、响应调整
        /// </summary>
        protected override void ProcessAudioData()
        {
            // 如果MusicSpectrum为null或材质无效，则不处理
            if (MusicSpectrum == null || !IsMaterialValid() || MusicSpectrum.groupedBands == null) return;
            
            try
            {
                // 根据是否启用平滑处理来更新频段数据
                if (smoothSpeed > 0)
                {
                    // 对每个频段应用平滑处理和响应调整曲线
                    int maxIndex = Mathf.Min(MusicSpectrum.numBands, smoothedIntensisyValues.Length);
                    for (int i = 0; i < maxIndex; i++)
                    {
                        smoothedIntensisyValues[i] = ApplySmoothing(
                            smoothedIntensisyValues[i], 
                            ApplyResponseAdjustment(MusicSpectrum.groupedBands[i])
                        );
                    }
                    // 将平滑处理后的数据传递给着色器
                    if (smoothedIntensisyValues.Length > 0)
                    {
                        mat.SetFloatArray("_FreqLevels", smoothedIntensisyValues);
                    }
                }
                else
                {
                    // 直接将原始频段数据传递给着色器，不进行平滑处理
                    if (MusicSpectrum.groupedBands.Length > 0)
                    {
                        mat.SetFloatArray("_FreqLevels", MusicSpectrum.groupedBands);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"处理音频数据时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 更新粒子发射率
        /// 根据低频强度和频段数量调整发射率
        /// </summary>
        protected override void UpdateParticleEmission()
        {
            if (particles != null && soundAffectsEmmisionRate && smoothedIntensisyValues != null && smoothedIntensisyValues.Length > 0)
            {
                try
                {
                    var partsEmmision = particles.emission;
                    partsEmmision.rateOverTime = startingEmmisionRate * smoothedIntensisyValues[0] * bands;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"更新粒子发射率时出错: {e.Message}");
                }
            }
        }

    }
}
