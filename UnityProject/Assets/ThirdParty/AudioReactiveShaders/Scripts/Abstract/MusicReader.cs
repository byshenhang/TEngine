using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


namespace AudioReactiveShader
{
    /// <summary>
    /// 音频读取器抽象基类
    /// 提供音频频谱数据读取和处理的基础功能
    /// 支持多种音频输入源和材质输出类型
    /// </summary>
    public abstract class MusicReader : MonoBehaviour
    {
        /// <summary>
        /// 音频输入类型枚举
        /// 定义了不同的音频数据获取方式
        /// </summary>
        public enum AUDIO_INPUT
        {
            AudioSource,        // 从AudioSource组件获取音频数据
            AudioListener,      // 从AudioListener获取全局音频数据
            MixerGroup,         // 从AudioMixerGroup获取混音器组音频数据
            AudioSourceWebGL,   // WebGL平台下的AudioSource模式
            MixerGroupWebGL,    // WebGL平台下的MixerGroup模式
        }
        
        /// <summary>
        /// 材质输出类型枚举
        /// 定义了音频数据应用到的目标组件类型
        /// </summary>
        public enum MATERIAL_OUTPUT
        {
            RENDERER,    // 应用到Renderer组件的材质
            PARTICLES,   // 应用到ParticleSystem组件的材质
            CANVAS_IMG   // 应用到Canvas Image组件的材质
        }

        [SerializeField] int _channelSelection;                    // 声道选择（0=左声道，1=右声道）
        [HideInInspector] public int totalSpectrum = 64;          // 频谱总数，默认64个频段
        AudioSource _audioSource;                                 // 当前使用的音频源组件
        [HideInInspector] public int _numBands;                   // 频段数量
        [HideInInspector] public AudioMixerGroup targetMixerGroup; // 目标混音器组
        /*[HideInInspector]*/ public List<AudioSource> audioSourcesInGroup; // 混音器组中的所有音频源列表

        // 音频数据数组
        public float[] rawSpectrumData;           // 原始频谱数据数组
        [HideInInspector] public int[] bandGroupsDistribution; // 频段分组分布数组
        public float[] groupedBands;              // 分组后的频段数据数组
        public float[] clipSamples = new float[256]; // 音频剪辑采样数据数组（用于WebGL平台）


        // 属性访问器
        /// <summary>当前音频源</summary>
        public AudioSource audioSource { get { return _audioSource; } set { _audioSource = value; } }
        /// <summary>频段数量</summary>
        public int numBands { get { return _numBands; } set { Debug.Log("Set _numBands " + _numBands); _numBands = value; } }
        /// <summary>声道选择</summary>
        public int channelSelection { get { return _channelSelection; } set { _channelSelection = value; } }

        /// <summary>
        /// 快速傅里叶变换（FFT）静态方法
        /// 将时域信号转换为频域信号，用于音频频谱分析
        /// </summary>
        /// <param name="data">输入的音频数据数组，长度必须是2的幂次方的两倍（实部和虚部交替存储）</param>
        public static void FFT(float[] data)
        {
            int n = data.Length / 2;  // 复数个数（实部虚部成对）
            int m = (int)Mathf.Log(n, 2);  // 计算log2(n)

            // 位反转重排序
            // 这是FFT算法的第一步，将数据按位反转的顺序重新排列
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; j >= bit; bit >>= 1)
                {
                    j -= bit;
                }
                j += bit;

                // 如果需要交换，则交换复数对
                if (i < j)
                {
                    int realIndex1 = 2 * i;      // 第i个复数的实部索引
                    int imagIndex1 = realIndex1 + 1; // 第i个复数的虚部索引
                    int realIndex2 = 2 * j;      // 第j个复数的实部索引
                    int imagIndex2 = realIndex2 + 1; // 第j个复数的虚部索引

                    // 交换复数对
                    float tempReal = data[realIndex1];
                    float tempImag = data[imagIndex1];
                    data[realIndex1] = data[realIndex2];
                    data[imagIndex1] = data[imagIndex2];
                    data[realIndex2] = tempReal;
                    data[imagIndex2] = tempImag;
                }
            }

            // 蝶形运算 - FFT的核心计算部分
            // 通过多层迭代完成频域变换
            for (int length = 2; length <= n; length <<= 1)
            {
                float angle = 2 * Mathf.PI / length;  // 当前层的角度步长
                float wlenX = Mathf.Cos(angle);        // 旋转因子的实部
                float wlenY = Mathf.Sin(angle);        // 旋转因子的虚部
                
                // 对每个子序列进行蝶形运算
                for (int i = 0; i < n; i += length)
                {
                    float wX = 1;  // 当前旋转因子的实部
                    float wY = 0;  // 当前旋转因子的虚部
                    
                    // 执行蝶形运算
                    for (int j = 0; j < length / 2; j++)
                    {
                        int evenIndex = 2 * (i + j);        // 偶数位复数索引
                        int oddIndex = evenIndex + length;   // 奇数位复数索引

                        // 获取偶数位和奇数位的复数值
                        float evenReal = data[evenIndex];
                        float evenImag = data[evenIndex + 1];
                        float oddReal = data[oddIndex];
                        float oddImag = data[oddIndex + 1];

                        // 计算旋转后的奇数位复数
                        float tempReal = oddReal * wX - oddImag * wY;
                        float tempImag = oddReal * wY + oddImag * wX;

                        // 蝶形运算：计算输出
                        data[evenIndex] = evenReal + tempReal;      // 上半部分
                        data[evenIndex + 1] = evenImag + tempImag;
                        data[oddIndex] = evenReal - tempReal;       // 下半部分
                        data[oddIndex + 1] = evenImag - tempImag;

                        // 更新旋转因子
                        float tempWX = wX * wlenX - wY * wlenY;
                        wY = wX * wlenY + wY * wlenX;
                        wX = tempWX;
                    }
                }
            }
        }
    }

}
