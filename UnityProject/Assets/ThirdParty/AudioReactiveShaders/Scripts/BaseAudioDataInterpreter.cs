using UnityEngine;
using UnityEngine.UI;

namespace AudioReactiveShader
{
    /// <summary>
    /// 音频数据解释器基类
    /// 提供音频频谱数据处理的通用功能，包括平滑处理、响应调整和材质输出
    /// 不依赖于Unity生命周期，由MusicSpectrumReader进行初始化
    /// </summary>
    public abstract class BaseAudioDataInterpreter : MonoBehaviour
    {
        protected MusicReader MusicSpectrum;  // 音频频谱读取器引用，由MusicSpectrumReader初始化
        [Tooltip("使用小于等于0的值来禁用平滑处理")] [SerializeField] protected float smoothSpeed;  // 平滑速度
        [SerializeField] protected AnimationCurve ResponseAdjustment;  // 响应调整曲线
        
        // 组件引用
        protected Renderer rend;           // 渲染器组件
        protected Image img;               // UI图像组件
        protected ParticleSystem particles; // 粒子系统组件
        protected Material mat;            // 目标材质
        [SerializeField] protected MusicSpectrumReader.MATERIAL_OUTPUT MaterialOutput;  // 材质输出类型
        
        // 粒子系统相关
        public bool soundAffectsParticlesEmmisionRate;  // 是否让声音影响粒子发射率
        protected float startingEmmisionRate;           // 初始粒子发射率
        
        // 初始化状态标志
        protected bool isInitialized = false;  // 标记是否已初始化

        /// <summary>
        /// 验证频段数量是否满足要求
        /// 子类需要重写此方法来实现具体的验证逻辑
        /// </summary>
        protected abstract void ValidateFrequencyBands();
        
        /// <summary>
        /// 初始化解释器
        /// 由MusicSpectrumReader调用，设置MusicSpectrum引用并初始化组件
        /// </summary>
        /// <param name="musicReader">音频频谱读取器引用</param>
        public virtual void Initialize(MusicReader musicReader)
        {
            if (isInitialized) return;
            
            if (musicReader == null)
            {
                Debug.LogError("BaseAudioDataInterpreter: 无法初始化，musicReader 为 null");
                return;
            }
            
            MusicSpectrum = musicReader;
            
            try
            {
                ValidateFrequencyBands();
                InitializeComponents();
                InitializeAudioData();
                
                isInitialized = true;
                Debug.Log($"BaseAudioDataInterpreter: {gameObject.name} 初始化成功");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"BaseAudioDataInterpreter: {gameObject.name} 初始化失败 - {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                isInitialized = false;
            }
        }

        /// <summary>
        /// 初始化组件引用
        /// 根据材质输出类型获取相应的组件和材质
        /// </summary>
        protected virtual void InitializeComponents()
        {
            // 根据材质输出类型获取相应的组件和材质
            if (MaterialOutput == MusicSpectrumReader.MATERIAL_OUTPUT.RENDERER)
            {
                rend = GetComponent<Renderer>();
                mat = rend.material;
            }
            else if (MaterialOutput == MusicSpectrumReader.MATERIAL_OUTPUT.PARTICLES)
            {
                particles = GetComponent<ParticleSystem>();
                SetParticleSystem();
            }
            else  // CANVAS_IMG
            {
                img = GetComponent<Image>();
                mat = img.material;
            }
        }

        /// <summary>
        /// 初始化音频数据相关设置
        /// 子类可以重写此方法来实现特定的初始化逻辑
        /// </summary>
        protected virtual void InitializeAudioData()
        {
            // 基类默认实现为空，子类可以重写
        }

        /// <summary>
        /// 设置粒子系统相关参数
        /// 获取粒子系统的材质，并记录初始发射率（如果需要音频影响发射率）
        /// </summary>
        protected virtual void SetParticleSystem()
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
            else 
            {
                Debug.LogWarning("未找到粒子系统组件");
            }
        }

        /// <summary>
        /// 当音频数据更新时被调用
        /// 由MusicSpectrumReader统一管理调用
        /// </summary>
        public virtual void OnAudioDataUpdated()
        {
            if (!isInitialized) return;
            
            ProcessAudioData();
            UpdateParticleEmission();
        }
        
        /// <summary>
        /// 检查解释器是否已初始化
        /// </summary>
        /// <returns>如果已初始化返回true，否则返回false</returns>
        public bool IsInitialized()
        {
            return isInitialized;
        }

        /// <summary>
        /// 处理音频数据
        /// 子类需要重写此方法来实现具体的音频数据处理逻辑
        /// </summary>
        protected abstract void ProcessAudioData();

        /// <summary>
        /// 更新粒子发射率
        /// 子类可以重写此方法来实现特定的粒子发射率更新逻辑
        /// </summary>
        protected virtual void UpdateParticleEmission()
        {
            // 基类默认实现为空，子类可以重写
        }

        /// <summary>
        /// 应用响应调整曲线到指定值
        /// </summary>
        /// <param name="value">原始值</param>
        /// <returns>调整后的值</returns>
        protected float ApplyResponseAdjustment(float value)
        {
            return ResponseAdjustment != null ? ResponseAdjustment.Evaluate(value) : value;
        }

        /// <summary>
        /// 应用平滑处理
        /// </summary>
        /// <param name="currentValue">当前值</param>
        /// <param name="targetValue">目标值</param>
        /// <returns>平滑处理后的值</returns>
        protected float ApplySmoothing(float currentValue, float targetValue)
        {
            if (smoothSpeed > 0)
            {
                return Mathf.Lerp(currentValue, targetValue, smoothSpeed * Time.deltaTime);
            }
            return targetValue;
        }

        /// <summary>
        /// 检查材质是否有效
        /// </summary>
        /// <returns>如果材质有效返回true，否则返回false</returns>
        protected bool IsMaterialValid()
        {
            return mat != null;
        }
    }
}