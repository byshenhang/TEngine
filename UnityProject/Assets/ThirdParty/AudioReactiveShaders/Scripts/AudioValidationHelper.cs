using UnityEngine;

/// <summary>
/// 音频验证工具类，提供常用的验证方法
/// </summary>
public static class AudioValidationHelper
{
    /// <summary>
    /// 验证频段数量是否有效
    /// </summary>
    /// <param name="numBands">频段数量</param>
    /// <param name="minBands">最小频段数量</param>
    /// <param name="maxBands">最大频段数量</param>
    /// <returns>频段数量是否有效</returns>
    public static bool IsValidBandCount(int numBands, int minBands = 1, int maxBands = 64)
    {
        if (numBands < minBands)
        {
            Debug.LogWarning($"频段数量 {numBands} 小于最小值 {minBands}，将使用最小值");
            return false;
        }
        
        if (numBands > maxBands)
        {
            Debug.LogWarning($"频段数量 {numBands} 大于最大值 {maxBands}，将使用最大值");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取有效的频段数量
    /// </summary>
    /// <param name="numBands">频段数量</param>
    /// <param name="minBands">最小频段数量</param>
    /// <param name="maxBands">最大频段数量</param>
    /// <returns>有效的频段数量</returns>
    public static int GetValidBandCount(int numBands, int minBands = 1, int maxBands = 64)
    {
        return Mathf.Clamp(numBands, minBands, maxBands);
    }
    
    /// <summary>
    /// 验证数组是否已初始化并且长度正确
    /// </summary>
    /// <param name="array">要验证的数组</param>
    /// <param name="expectedLength">期望的数组长度</param>
    /// <param name="arrayName">数组名称（用于日志）</param>
    /// <returns>数组是否有效</returns>
    public static bool IsArrayValid<T>(T[] array, int expectedLength, string arrayName = "数组")
    {
        if (array == null)
        {
            Debug.LogWarning($"{arrayName} 未初始化");
            return false;
        }
        
        if (array.Length != expectedLength)
        {
            Debug.LogWarning($"{arrayName} 长度不正确，期望: {expectedLength}，实际: {array.Length}");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 验证索引是否在数组范围内
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="array">数组</param>
    /// <param name="arrayName">数组名称（用于日志）</param>
    /// <returns>索引是否有效</returns>
    public static bool IsIndexInRange<T>(int index, T[] array, string arrayName = "数组")
    {
        if (array == null)
        {
            Debug.LogError($"{arrayName} 为 null，无法验证索引");
            return false;
        }
        
        if (index < 0 || index >= array.Length)
        {
            Debug.LogWarning($"索引 {index} 超出 {arrayName} 范围 [0, {array.Length - 1}]");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取安全的数组索引
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="array">数组</param>
    /// <returns>安全的索引值</returns>
    public static int GetSafeIndex<T>(int index, T[] array)
    {
        if (array == null || array.Length == 0)
        {
            return -1;
        }
        
        return Mathf.Clamp(index, 0, array.Length - 1);
    }
}