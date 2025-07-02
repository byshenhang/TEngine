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
            // 确保 bands 值有效
            bands = Mathf.Clamp(bands, 1, 63);
            
            if (MusicSpectrum != null)
            {
                // 确保 MusicSpectrum 的 numBands 有效
                if (MusicSpectrum.numBands <= 0)
                {
                    Debug.LogWarning($"DinamicBandsAudioDataInterpreter: MusicSpectrum.numBands 无效值 {MusicSpectrum.numBands}，设置为默认值 {bands}");
                    MusicSpectrum.numBands = bands;
                }
                // 如果 MusicSpectrum 的 numBands 小于当前设置的 bands，则调整它
                else if (MusicSpectrum.numBands < bands)
                {
                    Debug.Log($"DinamicBandsAudioDataInterpreter: 调整 MusicSpectrum.numBands 从 {MusicSpectrum.numBands} 到 {bands}");
                    MusicSpectrum.numBands = bands;
                }
                
                // 确保 MusicSpectrum 的数组已正确初始化
                if (MusicSpectrum.groupedBands == null || MusicSpectrum.groupedBands.Length != MusicSpectrum.numBands ||
                    MusicSpectrum.bandGroupsDistribution == null || MusicSpectrum.bandGroupsDistribution.Length != MusicSpectrum.numBands)
                {
                    Debug.LogWarning("DinamicBandsAudioDataInterpreter: MusicSpectrum 的数组未正确初始化");
                    
                    // 尝试调用 MusicSpectrumReader 的 DinamicBandsDistribution 方法
                    var musicSpectrumReader = MusicSpectrum as MusicSpectrumReader;
                    if (musicSpectrumReader != null)
                    {
                        try
                        {
                            // 在调用前再次确保 numBands 有效
                            if (MusicSpectrum.numBands <= 0)
                            {
                                MusicSpectrum.numBands = bands;
                            }
                            
                            musicSpectrumReader.DinamicBandsDistribution();
                            
                            // 验证初始化是否成功
                            if (MusicSpectrum.groupedBands == null || MusicSpectrum.groupedBands.Length != MusicSpectrum.numBands)
                            {
                                Debug.LogError("DinamicBandsAudioDataInterpreter: 频段分布初始化后数组仍然无效");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"DinamicBandsAudioDataInterpreter: 重新初始化频段分布失败 - {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                            
                            // 作为最后的备用方案，手动初始化数组
                            try
                            {
                                MusicSpectrum.groupedBands = new float[MusicSpectrum.numBands];
                                MusicSpectrum.bandGroupsDistribution = new int[MusicSpectrum.numBands];
                                Debug.LogWarning("DinamicBandsAudioDataInterpreter: 使用备用方案手动初始化数组");
                            }
                            catch (System.Exception fallbackEx)
                            {
                                Debug.LogError($"DinamicBandsAudioDataInterpreter: 备用初始化也失败 - {fallbackEx.Message}");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("DinamicBandsAudioDataInterpreter: MusicSpectrum 为 null，无法验证频段");
            }
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
            if (MusicSpectrum == null || !IsMaterialValid()) 
            {
                return;
            }
            
            // 检查 groupedBands 是否为 null
            if (MusicSpectrum.groupedBands == null)
            {
                Debug.LogWarning("ProcessAudioData: MusicSpectrum.groupedBands 为 null，尝试重新初始化");
                
                // 尝试调用 ValidateFrequencyBands 重新初始化
                ValidateFrequencyBands();
                
                // 如果仍然为 null，则返回
                if (MusicSpectrum.groupedBands == null)
                {
                    return;
                }
            }
            
            try
            {
                // 确保 smoothedIntensisyValues 数组已正确初始化
                if (smoothedIntensisyValues == null || smoothedIntensisyValues.Length != MusicSpectrum.numBands)
                {
                    Debug.Log($"ProcessAudioData: 重新初始化 smoothedIntensisyValues 数组，大小为 {MusicSpectrum.numBands}");
                    smoothedIntensisyValues = new float[MusicSpectrum.numBands];
                }
                
                // 根据是否启用平滑处理来更新频段数据
                if (smoothSpeed > 0)
                {
                    // 对每个频段应用平滑处理和响应调整曲线
                    int maxIndex = Mathf.Min(MusicSpectrum.numBands, smoothedIntensisyValues.Length);
                    
                    // 确保 groupedBands 数组长度足够
                    if (MusicSpectrum.groupedBands.Length < maxIndex)
                    {
                        Debug.LogWarning($"ProcessAudioData: groupedBands 数组长度 ({MusicSpectrum.groupedBands.Length}) 小于预期 ({maxIndex})");
                        maxIndex = MusicSpectrum.groupedBands.Length;
                    }
                    
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
                Debug.LogWarning($"处理音频数据时出错: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 更新粒子发射率
        /// 根据低频强度和频段数量调整发射率
        /// </summary>
        protected override void UpdateParticleEmission()
        {
            // 确保所有必要的组件和数据都可用
            if (particles == null)
            {
                // Debug.LogWarning("UpdateParticleEmission: particles 为 null");
                return;
            }
            
            if (!soundAffectsEmmisionRate)
            {
                return;
            }
            
            if (smoothedIntensisyValues == null)
            {
                Debug.LogWarning("UpdateParticleEmission: smoothedIntensisyValues 为 null，尝试重新初始化");
                
                // 尝试重新初始化
                if (MusicSpectrum != null && MusicSpectrum.numBands > 0)
                {
                    smoothedIntensisyValues = new float[MusicSpectrum.numBands];
                }
                else
                {
                    smoothedIntensisyValues = new float[Mathf.Max(1, bands)];
                }
            }
            
            if (smoothedIntensisyValues.Length == 0)
            {
                Debug.LogWarning("UpdateParticleEmission: smoothedIntensisyValues 长度为 0");
                return;
            }
            
            try
            {
                var partsEmmision = particles.emission;
                
                // 确保 bands 大于 0，避免乘以 0 导致发射率为 0
                int safeBands = Mathf.Max(1, bands);
                
                // 使用安全的索引访问，避免数组越界
                float intensityValue = smoothedIntensisyValues.Length > 0 ? smoothedIntensisyValues[0] : 0.5f;
                
                // 设置粒子发射率，确保不为负数
                float newRate = Mathf.Max(0, startingEmmisionRate * intensityValue * safeBands);
                partsEmmision.rateOverTime = newRate;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"更新粒子发射率时出错: {e.Message}\n{e.StackTrace}");
            }
        }

    }
}
