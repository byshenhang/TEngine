using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 音频关键点检测器 - 实时轻量级音频算法识别检测
/// 支持多种检测模式：RMS突变、节拍同步、多频段能量变化
/// 适用于移动端实时音频处理，性能优化
/// </summary>
public class AudioKeyPointDetector : MonoBehaviour
{
    [Header("音频源配置")]
    public AudioSource audioSource;
    
    [Header("检测模式选择")]
    public bool enableRMSDetection = true;
    public bool enableOnsetDetection = true;
    public bool enableMultiBandDetection = true;
    
    [Header("RMS突变检测参数")]
    [Range(0.01f, 1.0f)]
    public float rmsThreshold = 0.1f;
    [Range(0.5f, 5.0f)]
    public float rmsCooldown = 2.0f;
    
    [Header("节拍检测参数")]
    [Range(4, 32)]
    public int onsetWindowSize = 8;
    [Range(1.2f, 3.0f)]
    public float onsetRisingFactor = 1.5f;
    
    [Header("多频段检测参数")]
    [Range(1.2f, 3.0f)]
    public float multiBandThreshold = 1.5f;
    [Range(1.0f, 4.0f)]
    public float multiBandCooldown = 2.0f;
    
    [Header("性能优化参数")]
    [Range(256, 2048)]
    public int sampleSize = 1024;
    [Range(128, 1024)]
    public int spectrumSize = 512;
    [Range(30, 120)]
    public int targetFPS = 60;
    
    // 事件委托
    public System.Action OnRMSKeyPoint;
    public System.Action OnOnsetKeyPoint;
    public System.Action OnMultiBandKeyPoint;
    public System.Action<float> OnEnergyUpdate;
    
    // 私有变量
    private float[] audioSamples;
    private float[] spectrumData;
    private Queue<float> energyQueue;
    private Queue<float> lowBandQueue;
    private Queue<float> midBandQueue;
    private Queue<float> highBandQueue;
    
    // RMS检测变量
    private float lastRMS = 0f;
    private float lastRMSTrigger = 0f;
    
    // 多频段检测变量
    private float lastLowBand = 0f;
    private float lastMidBand = 0f;
    private float lastHighBand = 0f;
    private float lastMultiBandTrigger = 0f;
    
    // 动态阈值变量
    private float dynamicRMSThreshold;
    private float rmsHistory = 0f;
    private int frameCount = 0;
    
    // 性能控制
    private float lastUpdateTime = 0f;
    private float updateInterval;
    
    /// <summary>
    /// 初始化音频检测器
    /// </summary>
    void Start()
    {
        InitializeDetector();
    }
    
    /// <summary>
    /// 初始化检测器参数和数据结构
    /// </summary>
    private void InitializeDetector()
    {
        // 验证音频源
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("AudioKeyPointDetector: 未找到AudioSource组件！");
                enabled = false;
                return;
            }
        }
        
        // 初始化数组
        audioSamples = new float[sampleSize];
        spectrumData = new float[spectrumSize];
        
        // 初始化队列
        energyQueue = new Queue<float>();
        lowBandQueue = new Queue<float>();
        midBandQueue = new Queue<float>();
        highBandQueue = new Queue<float>();
        
        // 计算更新间隔
        updateInterval = 1.0f / targetFPS;
        
        // 初始化动态阈值
        dynamicRMSThreshold = rmsThreshold;
        
        Debug.Log("AudioKeyPointDetector: 初始化完成");
    }
    
    /// <summary>
    /// 主更新循环 - 控制检测频率以优化性能
    /// </summary>
    void Update()
    {
        // 性能控制：限制更新频率
        if (Time.time - lastUpdateTime < updateInterval)
            return;
            
        lastUpdateTime = Time.time;
        
        // 检查音频源状态
        if (!audioSource.isPlaying)
            return;
            
        // 执行音频分析
        PerformAudioAnalysis();
    }
    
    /// <summary>
    /// 执行音频分析的主要逻辑
    /// </summary>
    private void PerformAudioAnalysis()
    {
        // 获取音频数据
        audioSource.GetOutputData(audioSamples, 0);
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.Rectangular);
        
        // RMS突变检测
        if (enableRMSDetection)
        {
            DetectRMSKeyPoints();
        }
        
        // 节拍同步检测
        if (enableOnsetDetection)
        {
            DetectOnsetKeyPoints();
        }
        
        // 多频段检测
        if (enableMultiBandDetection)
        {
            DetectMultiBandKeyPoints();
        }
        
        // 更新动态阈值
        UpdateDynamicThreshold();
    }
    
    /// <summary>
    /// RMS突变检测算法
    /// 计算短时能量变化，检测音频突变点
    /// </summary>
    private void DetectRMSKeyPoints()
    {
        // 计算当前帧RMS
        float sum = 0f;
        for (int i = 0; i < audioSamples.Length; i++)
        {
            sum += audioSamples[i] * audioSamples[i];
        }
        float currentRMS = Mathf.Sqrt(sum / audioSamples.Length);
        
        // 计算RMS变化率
        float delta = currentRMS - lastRMS;
        float changeRate = lastRMS > 0 ? delta / lastRMS : 0;
        
        // 检测突变（使用动态阈值）
        bool isKeyPoint = changeRate > dynamicRMSThreshold && 
                         Time.time - lastRMSTrigger > rmsCooldown;
        
        if (isKeyPoint)
        {
            lastRMSTrigger = Time.time;
            OnRMSKeyPoint?.Invoke();
            Debug.Log($"RMS关键点检测: RMS={currentRMS:F4}, 变化率={changeRate:F4}");
        }
        
        // 更新能量信息
        OnEnergyUpdate?.Invoke(currentRMS);
        lastRMS = currentRMS;
    }
    
    /// <summary>
    /// 节拍同步峰值密度检测
    /// 基于能量变化趋势检测音乐结构变化点
    /// </summary>
    private void DetectOnsetKeyPoints()
    {
        // 计算频谱总能量
        float totalEnergy = 0f;
        for (int i = 0; i < spectrumData.Length; i++)
        {
            totalEnergy += spectrumData[i];
        }
        
        // 维护能量队列
        energyQueue.Enqueue(totalEnergy);
        if (energyQueue.Count > onsetWindowSize)
        {
            energyQueue.Dequeue();
        }
        
        // 检测上升趋势
        if (IsEnergyRising(energyQueue, onsetRisingFactor))
        {
            OnOnsetKeyPoint?.Invoke();
            Debug.Log($"节拍关键点检测: 能量上升趋势, 当前能量={totalEnergy:F4}");
        }
    }
    
    /// <summary>
    /// 多频段能量变化检测
    /// 分析不同频段的能量变化，检测频谱爆发点
    /// </summary>
    private void DetectMultiBandKeyPoints()
    {
        // 分频段计算能量
        float lowBand = CalculateBandEnergy(0, spectrumSize / 5);           // 低频
        float midBand = CalculateBandEnergy(spectrumSize / 5, spectrumSize * 3 / 5);  // 中频
        float highBand = CalculateBandEnergy(spectrumSize * 3 / 5, spectrumSize);     // 高频
        
        // 检测多频段同时爆发
        bool isMultiBandKeyPoint = 
            (midBand > lastMidBand * multiBandThreshold && 
             highBand > lastHighBand * multiBandThreshold) &&
            Time.time - lastMultiBandTrigger > multiBandCooldown;
        
        if (isMultiBandKeyPoint)
        {
            lastMultiBandTrigger = Time.time;
            OnMultiBandKeyPoint?.Invoke();
            Debug.Log($"多频段关键点检测: 低={lowBand:F4}, 中={midBand:F4}, 高={highBand:F4}");
        }
        
        // 更新历史数据
        lastLowBand = lowBand;
        lastMidBand = midBand;
        lastHighBand = highBand;
    }
    
    /// <summary>
    /// 计算指定频段的能量
    /// </summary>
    /// <param name="startIndex">起始索引</param>
    /// <param name="endIndex">结束索引</param>
    /// <returns>频段能量值</returns>
    private float CalculateBandEnergy(int startIndex, int endIndex)
    {
        float energy = 0f;
        for (int i = startIndex; i < endIndex && i < spectrumData.Length; i++)
        {
            energy += spectrumData[i];
        }
        return energy / (endIndex - startIndex);
    }
    
    /// <summary>
    /// 检测能量队列中的上升趋势
    /// </summary>
    /// <param name="queue">能量队列</param>
    /// <param name="risingFactor">上升因子</param>
    /// <returns>是否存在上升趋势</returns>
    private bool IsEnergyRising(Queue<float> queue, float risingFactor)
    {
        if (queue.Count < 4) return false;
        
        float[] values = queue.ToArray();
        int risingCount = 0;
        
        // 检查连续上升的点数
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > values[i - 1] * risingFactor)
            {
                risingCount++;
            }
        }
        
        // 如果超过一半的点都在上升，认为是趋势
        return risingCount >= values.Length / 2;
    }
    
    /// <summary>
    /// 更新动态阈值
    /// 根据历史RMS数据自适应调整检测阈值
    /// </summary>
    private void UpdateDynamicThreshold()
    {
        frameCount++;
        rmsHistory = (rmsHistory * (frameCount - 1) + lastRMS) / frameCount;
        
        // 每100帧更新一次动态阈值
        if (frameCount % 100 == 0)
        {
            dynamicRMSThreshold = Mathf.Max(rmsThreshold, rmsHistory * 0.5f);
        }
    }
    
    /// <summary>
    /// 手动触发关键点检测（用于测试）
    /// </summary>
    [ContextMenu("手动触发RMS检测")]
    public void ManualTriggerRMS()
    {
        OnRMSKeyPoint?.Invoke();
    }
    
    /// <summary>
    /// 重置检测器状态
    /// </summary>
    [ContextMenu("重置检测器")]
    public void ResetDetector()
    {
        lastRMS = 0f;
        lastRMSTrigger = 0f;
        lastMultiBandTrigger = 0f;
        
        energyQueue?.Clear();
        lowBandQueue?.Clear();
        midBandQueue?.Clear();
        highBandQueue?.Clear();
        
        frameCount = 0;
        rmsHistory = 0f;
        
        Debug.Log("AudioKeyPointDetector: 检测器已重置");
    }
    
    /// <summary>
    /// 获取当前检测状态信息
    /// </summary>
    /// <returns>状态信息字符串</returns>
    public string GetDetectorStatus()
    {
        return $"RMS: {lastRMS:F4} | 动态阈值: {dynamicRMSThreshold:F4} | 帧数: {frameCount}";
    }
    
    /// <summary>
    /// 在Inspector中显示实时状态
    /// </summary>
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label("音频关键点检测器状态", GUI.skin.box);
        GUILayout.Label($"当前RMS: {lastRMS:F4}");
        GUILayout.Label($"动态阈值: {dynamicRMSThreshold:F4}");
        GUILayout.Label($"检测帧数: {frameCount}");
        GUILayout.Label($"RMS检测: {(enableRMSDetection ? "启用" : "禁用")}");
        GUILayout.Label($"节拍检测: {(enableOnsetDetection ? "启用" : "禁用")}");
        GUILayout.Label($"多频段检测: {(enableMultiBandDetection ? "启用" : "禁用")}");
        GUILayout.EndArea();
    }
}