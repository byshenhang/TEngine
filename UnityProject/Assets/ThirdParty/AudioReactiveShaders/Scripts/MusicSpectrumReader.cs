using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AudioReactiveShader
{
    /// <summary>
    /// 音频频谱读取器
    /// 继承自MusicReader抽象基类，实现具体的音频数据获取和处理功能
    /// 支持多种音频输入源：AudioSource、AudioListener、MixerGroup等
    /// 包含WebGL平台的特殊处理逻辑
    /// 集中管理所有音频数据解释器的更新
    /// </summary>
    public class MusicSpectrumReader : MusicReader
    {
        public AUDIO_INPUT audio_input;  // 当前选择的音频输入类型
        
        // 音频数据解释器集中管理
        private List<BaseAudioDataInterpreter> audioDataInterpreters = new List<BaseAudioDataInterpreter>();

        #region editor
#if UNITY_EDITOR
        /// <summary>
        /// 音频频谱读取器的自定义编辑器
        /// 提供用户友好的Inspector界面，支持音频输入类型选择和参数配置
        /// </summary>
        [CustomEditor(typeof(MusicSpectrumReader))]
        public class MusicReaderEditor : Editor
        {
            bool showHiddenVars;  // 是否显示隐藏变量

            public override void OnInspectorGUI()
            {
                MusicSpectrumReader MSR = (MusicSpectrumReader)target;
                Undo.RecordObject(MSR, "MusicSpectrumReader changes");
                
                // 根据showHiddenVars决定是否显示默认Inspector或自定义界面
                if (showHiddenVars) 
                    base.OnInspectorGUI();
                else
                {
                    // 绘制自定义头部
                    Color color = new Color(.1f, .1f, .2f);
                    Rect headerArea = new Rect(0, 0, EditorGUIUtility.currentViewWidth, 35);
                    GUILayout.BeginArea(headerArea);
                    EditorGUILayout.Space(5);
                    EditorGUI.DrawRect(headerArea, color);
                    GUI.skin.label.fontSize = 15;
                    GUI.skin.label.fontStyle = FontStyle.BoldAndItalic;
                    GUILayout.Label("AUDIO REACTIVE SHADERS | music spectrum reader");
                    GUILayout.EndArea();
                    EditorGUILayout.Space(40);
                }

                // 音频输入类型选择
                MSR.audio_input = (AUDIO_INPUT)EditorGUILayout.EnumPopup("Input selection", MSR.audio_input);
                
                // 如果选择了MixerGroup类型，显示混音器组选择界面
                if (MSR.audio_input == AUDIO_INPUT.MixerGroup || MSR.audio_input == AUDIO_INPUT.MixerGroupWebGL)
                {
                    EditorGUILayout.LabelField("Choose your mixer group", EditorStyles.boldLabel);
                    MSR.targetMixerGroup = (AudioMixerGroup)EditorGUILayout.ObjectField("Target Mixer Group", MSR.targetMixerGroup, typeof(AudioMixerGroup), false);
                }
                
                EditorGUILayout.Space(5);
                
                // 非WebGL平台显示声道选择
                if (MSR.audio_input != AUDIO_INPUT.AudioSourceWebGL && MSR.audio_input != AUDIO_INPUT.MixerGroupWebGL)
                {
                    EditorGUILayout.LabelField("0 to use the left channel or 1 to use the right channel", EditorStyles.boldLabel);
                    MSR.channelSelection = (int)EditorGUILayout.Slider(MSR.channelSelection, 0, 1);
                }

                EditorGUILayout.Space(5);
                showHiddenVars = EditorGUILayout.Toggle("show hidden vars", showHiddenVars);

                // 标记对象为已修改
                if (GUI.changed)
                    EditorUtility.SetDirty(MSR);
            }
        }
#endif
        #endregion
        /// <summary>
        /// 初始化原始频谱数据数组
        /// 设置为128个元素，这是Unity音频系统的标准频谱数据大小
        /// </summary>
        private void Awake()
        {
            rawSpectrumData = new float[128];
        }
        /// <summary>
        /// 组件启用时的初始化
        /// 根据音频输入类型进行相应的初始化设置
        /// 自动检索并注册场景中的所有BaseAudioDataInterpreter
        /// </summary>
        void OnEnable()
        {
            // AudioSource类型：获取当前GameObject上的AudioSource组件
            if (audio_input == AUDIO_INPUT.AudioSource || audio_input == AUDIO_INPUT.AudioSourceWebGL)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    Debug.LogWarning("未找到AudioSource组件。");
                }
            }
            // MixerGroup类型：刷新混音器组中的音频源列表
            else if(audio_input == AUDIO_INPUT.MixerGroup || audio_input == AUDIO_INPUT.MixerGroupWebGL)
            {
                refreshAudioSourcesOnMixerGroup();
            }

            // 确保 numBands 不为零，避免除零异常
            if (numBands <= 0)
            {
                numBands = 8; // 设置一个默认值
                Debug.Log("numBands 被设置为默认值 8，因为原值为 " + _numBands);
            }

            // 初始化频段相关数组
            groupedBands = new float[numBands];           // 分组后的频段数据
            bandGroupsDistribution = new int[numBands];   // 频段分组分布

            // 计算动态频段分布
            DinamicBandsDistribution();
            
            // 自动检索并注册场景中的所有BaseAudioDataInterpreter
            AutoDiscoverAndRegisterInterpreters();
        }
       

        /// <summary>
        /// 每帧更新音频数据
        /// 根据不同的音频输入类型采用不同的数据获取方式
        /// 然后统一更新所有注册的音频数据解释器
        /// </summary>
        void Update()
        {
            // 首先更新音频频谱数据
            UpdateAudioSpectrumData();
            
            // 然后统一更新所有音频数据解释器
            UpdateAllAudioDataInterpreters();
        }
        
        /// <summary>
        /// 更新音频频谱数据
        /// 根据不同的音频输入类型采用不同的数据获取方式
        /// </summary>
        private void UpdateAudioSpectrumData()
        {
            // AudioSource模式：仅在音频播放时获取数据
            if (audio_input == AUDIO_INPUT.AudioSource)
            {
                if (audioSource.isPlaying)
                {
                    getAudiosourceData();
                }
            }
            // AudioListener模式：获取全局音频监听器的频谱数据
            else if (audio_input == AUDIO_INPUT.AudioListener)
            {
                AudioListener.GetSpectrumData(rawSpectrumData, channelSelection, FFTWindow.Rectangular);
                GroupSpectrumData();
            }
            // MixerGroup模式：优先使用当前音频源，否则搜索混音器组中正在播放的音频源
            else if (audio_input == AUDIO_INPUT.MixerGroup)
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    getAudiosourceData();
                }
                else
                {
                    searchForPlayingAudiosources();
                }
            }
            // WebGL平台的AudioSource模式：使用自定义FFT处理
            else if (audio_input == AUDIO_INPUT.AudioSourceWebGL)
            {
                if (audioSource.isPlaying)
                {
                    GetAudioClipSpectrumData();
                }
            }
            // WebGL平台的MixerGroup模式：结合MixerGroup和WebGL处理
            else if (audio_input == AUDIO_INPUT.MixerGroupWebGL)
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    GetAudioClipSpectrumData();
                }
                else
                {
                    searchForPlayingAudiosources();
                }
            }
        }
        
        /// <summary>
        /// 统一更新所有注册的音频数据解释器
        /// 自动清理已销毁的解释器
        /// </summary>
        private void UpdateAllAudioDataInterpreters()
        {
            // 反向遍历以安全移除已销毁的解释器
            for (int i = audioDataInterpreters.Count - 1; i >= 0; i--)
            {
                if (audioDataInterpreters[i] == null)
                {
                    // 移除已销毁的解释器
                    audioDataInterpreters.RemoveAt(i);
                }
                else
                {
                    audioDataInterpreters[i].OnAudioDataUpdated();
                }
            }
        }
        
        /// <summary>
        /// 注册音频数据解释器
        /// 如果解释器尚未初始化，则先进行初始化
        /// </summary>
        /// <param name="interpreter">要注册的音频数据解释器</param>
        public void RegisterAudioDataInterpreter(BaseAudioDataInterpreter interpreter)
        {
            if (interpreter != null && !audioDataInterpreters.Contains(interpreter))
            {
                // 如果解释器尚未初始化，则先初始化
                if (!interpreter.IsInitialized())
                {
                    interpreter.Initialize(this);
                }
                
                audioDataInterpreters.Add(interpreter);
            }
        }
        
        /// <summary>
        /// 注销音频数据解释器
        /// </summary>
        /// <param name="interpreter">要注销的音频数据解释器</param>
        public void UnregisterAudioDataInterpreter(BaseAudioDataInterpreter interpreter)
        {
            if (interpreter != null)
            {
                audioDataInterpreters.Remove(interpreter);
            }
        }
        
        /// <summary>
        /// 自动发现并注册场景中的所有BaseAudioDataInterpreter
        /// 查找场景中的所有解释器并初始化它们
        /// </summary>
        private void AutoDiscoverAndRegisterInterpreters()
        {
            // 查找场景中所有的BaseAudioDataInterpreter组件
            BaseAudioDataInterpreter[] allInterpreters = FindObjectsOfType<BaseAudioDataInterpreter>();
            
            int registeredCount = 0;
            foreach (BaseAudioDataInterpreter interpreter in allInterpreters)
            {
                if (interpreter != null)
                {
                    // 初始化解释器并注册
                    interpreter.Initialize(this);
                    RegisterAudioDataInterpreter(interpreter);
                    registeredCount++;
                }
            }
            
            Debug.Log($"MusicSpectrumReader自动发现、初始化并注册了 {registeredCount} 个音频数据解释器");
        }
        
        /// <summary>
        /// 手动刷新场景中的音频数据解释器注册
        /// 重新扫描场景并更新注册列表，同时初始化所有解释器
        /// </summary>
        [ContextMenu("刷新音频数据解释器注册")]
        public void RefreshInterpreterRegistration()
        {
            // 清空当前注册列表
            audioDataInterpreters.Clear();
            
            // 重新自动发现、初始化并注册
            AutoDiscoverAndRegisterInterpreters();
            
            Debug.Log("已刷新音频数据解释器注册并重新初始化");
        }
        
        /// <summary>
        /// 获取当前注册的解释器数量
        /// </summary>
        /// <returns>注册的解释器数量</returns>
        public int GetRegisteredInterpreterCount()
        {
            return audioDataInterpreters.Count;
        }
        
        /// <summary>
        /// 获取所有已注册的解释器列表（只读）
        /// </summary>
        /// <returns>已注册解释器的只读列表</returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<BaseAudioDataInterpreter> GetRegisteredInterpreters()
        {
            return audioDataInterpreters.AsReadOnly();
        }

        /// <summary>
        /// 从AudioSource获取频谱数据
        /// 使用Unity内置的GetSpectrumData方法
        /// </summary>
        void getAudiosourceData()
        {
            if (audioSource != null) 
                audioSource.GetSpectrumData(rawSpectrumData, channelSelection, FFTWindow.Rectangular);
            GroupSpectrumData();
        }

        /// <summary>
        /// 从AudioClip获取音频采样数据
        /// 用于WebGL平台的音频处理
        /// </summary>
        /// <param name="samples">存储采样数据的数组</param>
        private void GetAudioClipSamples(float[] samples)
        {
            audioSource.clip.GetData(samples, audioSource.timeSamples);
        }
        
        /// <summary>
        /// WebGL平台专用的频谱数据获取方法
        /// 使用自定义FFT算法处理音频剪辑数据
        /// </summary>
        private void GetAudioClipSpectrumData()
        {
            // 获取音频剪辑的采样数据
            GetAudioClipSamples(clipSamples);
            
            // 应用快速傅里叶变换
            FFT(clipSamples);

            // 将FFT结果转换为频谱数据
            // 计算复数的模长（幅度）
            for (int i = 0; i < rawSpectrumData.Length / 2; i++)
            {
                rawSpectrumData[i] = Mathf.Sqrt(clipSamples[2 * i] * clipSamples[2 * i] + clipSamples[2 * i + 1] * clipSamples[2 * i + 1]);
            }
            
            // 对频谱数据进行分组处理
            GroupSpectrumData();
        }

        /// <summary>
        /// 将原始频谱数据分组处理
        /// 根据频段分布将128个频谱数据点分组为指定数量的频段
        /// 每个频段内的数据取平均值作为该频段的强度
        /// </summary>
        void GroupSpectrumData()
        {
            // 安全检查：确保 numBands 大于 0 且数组已正确初始化
            if (numBands <= 0)
            {
                Debug.LogWarning("GroupSpectrumData: numBands 为 0 或负数，已自动设置为默认值 8");
                numBands = 8;
            }
            
            // 确保 rawSpectrumData 数组已初始化
            if (rawSpectrumData == null)
            {
                Debug.LogError("GroupSpectrumData: rawSpectrumData 为 null，无法处理频谱数据");
                return;
            }
            
            // 确保 groupedBands 和 bandGroupsDistribution 数组已正确初始化
            if (groupedBands == null || groupedBands.Length != numBands ||
                bandGroupsDistribution == null || bandGroupsDistribution.Length != numBands)
            {
                Debug.LogWarning("GroupSpectrumData: 数组未正确初始化，重新调用 DinamicBandsDistribution");
                DinamicBandsDistribution();
                
                // 再次检查初始化是否成功
                if (groupedBands == null || groupedBands.Length != numBands ||
                    bandGroupsDistribution == null || bandGroupsDistribution.Length != numBands)
                {
                    Debug.LogError("GroupSpectrumData: 重新初始化后数组仍然无效，终止处理");
                    return;
                }
            }
            
            // 重置分组后的频段数据
            for (int i = 0; i < numBands && i < groupedBands.Length; i++)
            {
                groupedBands[i] = 0;
            }

            int startIndex = 0;  // 当前处理的频谱数据起始索引
            
            // 遍历每个频段
            for (int i = 0; i < numBands && i < bandGroupsDistribution.Length; i++)
            {
                int size = bandGroupsDistribution[i];  // 当前频段包含的采样点数量
                
                // 安全检查：确保不会超出rawSpectrumData数组边界
                if (size <= 0)
                {
                    continue; // 跳过无效的频段大小
                }

                // 累加当前频段内所有采样点的幅度值
                for (int j = 0; j < size; j++)
                {
                    int dataIndex = startIndex + j;
                    
                    // 确保不会超出rawSpectrumData数组边界
                    if (dataIndex >= rawSpectrumData.Length)
                    {
                        Debug.LogWarning($"GroupSpectrumData: 数据索引 {dataIndex} 超出 rawSpectrumData 数组边界 {rawSpectrumData.Length}，跳过剩余数据");
                        break;
                    }
                    
                    if (size <= 1) 
                    {
                        // 单个采样点直接赋值
                        groupedBands[i] += rawSpectrumData[dataIndex];
                    }
                    else
                    {
                        // WebGL平台需要特殊的缩放处理
                        if(audio_input == AUDIO_INPUT.AudioSourceWebGL || audio_input == AUDIO_INPUT.MixerGroupWebGL)
                        {
                            groupedBands[i] += .01f * rawSpectrumData[dataIndex] / size;
                        }
                        else
                        {
                            // 标准平台计算平均值
                            groupedBands[i] += rawSpectrumData[dataIndex] / size;
                        }
                    }
                }

                // 移动到下一个频段的起始位置
                startIndex += size;
                
                // 安全检查：防止startIndex超出合理范围
                if (startIndex >= rawSpectrumData.Length)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 动态计算频段分布
        /// 使用渐进式算法将频谱数据分配到不同的频段中
        /// 确保所有64个频谱采样点都被合理分配到各个频段
        /// </summary>
        public void DinamicBandsDistribution()
        {
            // 安全检查：确保 numBands 大于 0
            if (numBands <= 0)
            {
                numBands = 8; // 设置一个默认值
                Debug.LogWarning("dinamicBandsDistribution: numBands 为 0，已设置为默认值 8");
            }
            
            // 确保数组已正确初始化且长度匹配
            if (groupedBands == null || groupedBands.Length != numBands)
            {
                groupedBands = new float[numBands];
            }
            if (bandGroupsDistribution == null || bandGroupsDistribution.Length != numBands)
            {
                bandGroupsDistribution = new int[numBands];
            }
            
            // 再次检查numBands，防止在执行过程中被修改
            if (numBands <= 0)
            {
                Debug.LogError("dinamicBandsDistribution: numBands 在执行过程中变为无效值，终止执行");
                return;
            }
            
            int totalAdded = 0;  // 已分配的采样点总数
            int progressionAmp = totalSpectrum / (numBands * 4);  // 渐进幅度
            int progressionStart = progressionAmp * ((numBands + 2) / -2);  // 渐进起始值

            // 为每个频段分配采样点数量
            for (int i = 0; i <= numBands - 1; i++)
            {
                progressionStart += 1 * progressionAmp;  // 递增渐进值

                totalAdded += (totalSpectrum / numBands) + progressionStart;
                bandGroupsDistribution[i] = (64 / numBands) + progressionStart;

                // 确保每个频段至少有1个采样点
                if ((totalSpectrum / numBands) + progressionStart < 1)
                {
                    bandGroupsDistribution[i] = 1;
                    totalAdded += -((totalSpectrum / numBands) + progressionStart) + 1;
                }
            }
            
            // 调整最后一个频段以确保总数为64（添加安全检查）
            if (numBands > 0 && bandGroupsDistribution != null && bandGroupsDistribution.Length >= numBands)
            {
                if (totalAdded < 64)
                {
                    bandGroupsDistribution[numBands - 1] += (totalSpectrum - totalAdded);
                }
                if (totalAdded > 64)
                {
                    bandGroupsDistribution[numBands - 1] -= (totalAdded - totalSpectrum);
                }
            }
        }
        /// <summary>
        /// 刷新混音器组中的音频源列表
        /// 重新搜索并更新当前目标混音器组中的所有音频源
        /// </summary>
        public void refreshAudioSourcesOnMixerGroup()
        {
            audioSourcesInGroup = new List<AudioSource>();
            FindAudioSourcesOnMixerGroup(targetMixerGroup);
            searchForPlayingAudiosources();
        }
        
        /// <summary>
        /// 查找指定混音器组中的所有音频源
        /// 遍历场景中的所有AudioSource组件，筛选出输出到指定混音器组的音频源
        /// </summary>
        /// <param name="mixerGroup">目标混音器组</param>
        void FindAudioSourcesOnMixerGroup(AudioMixerGroup mixerGroup)
        {
            AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

            // 遍历场景中的所有音频源
            foreach (AudioSource audioSource in allAudioSources)
            {
                // 检查音频源的输出混音器组是否匹配
                if (audioSource.outputAudioMixerGroup == mixerGroup)
                {
                    audioSourcesInGroup.Add(audioSource);
                }
            }
        }
        
        /// <summary>
        /// 搜索正在播放的音频源
        /// 在混音器组的音频源列表中查找第一个正在播放的音频源
        /// 并将其设置为当前的音频数据源
        /// </summary>
        void searchForPlayingAudiosources()
        {
            foreach (AudioSource AS in audioSourcesInGroup)
            {
                if (AS.isPlaying)
                {
                    audioSource = AS;  // 设置为当前音频源
                    return;  // 找到第一个播放中的音频源后退出
                }
            }
        }
    }
}
