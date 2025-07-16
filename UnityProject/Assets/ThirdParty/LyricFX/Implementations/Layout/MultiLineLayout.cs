using Cysharp.Threading.Tasks;
using GameLogic;
using LyricFX.Core.Interfaces;
using System.Threading;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace LyricFX.Implementations.Layout
{
    /// <summary>
    /// 网格布局 - 支持自动换行的网格排列
    /// </summary>
    public class MultiLineLayout : ILayoutProvider
    {
        private Vector3 startOffset = Vector3.zero;  // 起始偏移
        private int maxCharactersPerLine = 25;  // 每行最大字符数
        private float lineSpacing = 1f;  // 行间距
        
        public string LayoutId => "multi_line";
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="offset">起始偏移</param>
        /// <param name="maxCharsPerLine">每行最大字符数</param>
        /// <param name="lineSpacing">行间距</param>
        public MultiLineLayout(Vector3 offset = default, int maxCharsPerLine = 25, float lineSpacing = 1f)
        {
            startOffset = offset;
            maxCharactersPerLine = maxCharsPerLine;
            this.lineSpacing = lineSpacing;
        }
        
        /// <summary>
        /// 计算网格布局 - 重写为更简单可靠的算法
        /// </summary>
        public async UniTask<Vector3[]> CalculateLayout(
            string text, 
            Transform container,
            ILayoutConfig config, 
            GameObject prefab,
            CancellationToken cancellationToken = default)
        {
            var tmpro = prefab.GetComponent<TextMeshProUGUI>();

            if (cancellationToken.IsCancellationRequested)
                return new Vector3[0];
            
            if (string.IsNullOrEmpty(text))
                return new Vector3[0];
            
            // 使用局部变量避免修改实例字段
            float cellWidth = tmpro.rectTransform.sizeDelta.x;
            float cellHeight = tmpro.rectTransform.sizeDelta.y;
            Vector3 offset = startOffset;
            int maxCharsPerLine = maxCharactersPerLine;
            float currentLineSpacing = lineSpacing;
            
            // 如果配置不为空，可以覆盖默认设置
            if (config is MultiLineLayoutConfig multiLineConfig)
            {
                offset = multiLineConfig.StartOffset;
                maxCharsPerLine = multiLineConfig.MaxCharactersPerLine;
                currentLineSpacing = multiLineConfig.LineSpacing;
            }
            
            var positions = new Vector3[text.Length];
            
            // 第一步：构建有效字符索引映射表
            var validCharIndices = new List<int>();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c != '\n' && c != '\r')
                {
                    validCharIndices.Add(i);
                }
            }
            
            if (validCharIndices.Count == 0)
            {
                // 所有字符都是换行符，设置为屏幕外
                for (int i = 0; i < text.Length; i++)
                {
                    positions[i] = new Vector3(float.MinValue, float.MinValue, 0);
                }
                return positions;
            }
            
            // 第二步：计算总行数和每行字符数
            int totalLines = Mathf.CeilToInt((float)validCharIndices.Count / maxCharsPerLine);
            var lineCharCounts = new int[totalLines];
            
            for (int i = 0; i < validCharIndices.Count; i++)
            {
                int lineIndex = i / maxCharsPerLine;
                lineCharCounts[lineIndex]++;
            }
            
            // 第三步：为每个有效字符计算位置
            for (int validIndex = 0; validIndex < validCharIndices.Count; validIndex++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                int originalIndex = validCharIndices[validIndex];
                
                // 计算网格坐标
                int row = validIndex / maxCharsPerLine;
                int col = validIndex % maxCharsPerLine;
                
                // 获取当前行的字符数量
                int charsInCurrentLine = lineCharCounts[row];
                
                // 计算当前行的总宽度
                float lineWidth = charsInCurrentLine > 1 ? (charsInCurrentLine - 1) * cellWidth : 0;
                
                // 计算当前行的起始X位置（水平居中）
                float lineStartX = offset.x - lineWidth * 0.5f;
                
                // 计算实际位置
                float x = lineStartX + col * cellWidth;
                float y = offset.y - row * (cellHeight + currentLineSpacing);
                
                positions[originalIndex] = new Vector3(x, y, offset.z);
                
                // 每处理几个字符，让出一帧以避免卡顿
                if (validIndex % 20 == 0 && validIndex > 0)
                    await UniTask.Yield();
            }
            
            // 第四步：为无效字符（换行符等）设置屏幕外位置
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n' || c == '\r')
                {
                    positions[i] = new Vector3(float.MinValue, float.MinValue, 0);
                }
            }
            
            // 验证计算结果
            if (!ValidateLayoutResult(text, positions))
            {
                Debug.LogError("[MultiLineLayout] Layout calculation failed validation");
            }
            
            Debug.Log($"[MultiLineLayout] Calculated positions for {text.Length} characters, {validCharIndices.Count} valid chars, {totalLines} lines");
            
            return positions;
        }
        
        // 移除了复杂的预处理逻辑，简化为直接处理原始文本
        
        /// <summary>
        /// 应用布局到字符对象
        /// </summary>
        public async UniTask ApplyLayout(GameObject[] characters, Vector3[] positions, CancellationToken cancellationToken = default)
        {
            if (characters == null || positions == null)
            {
                Debug.LogWarning("[MultiLineLayout] ApplyLayout: characters or positions is null");
                return;
            }
            
            if (characters.Length != positions.Length)
            {
                Debug.LogWarning($"[MultiLineLayout] ApplyLayout: Length mismatch - characters: {characters.Length}, positions: {positions.Length}");
                return;
            }
            
            int appliedCount = 0;
            for (int i = 0; i < characters.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                if (characters[i] != null)
                {
                    // 验证位置是否有效
                    Vector3 pos = positions[i];
                    if (!float.IsNaN(pos.x) && !float.IsNaN(pos.y) && !float.IsNaN(pos.z))
                    {
                        characters[i].transform.localPosition = pos;
                        appliedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"[MultiLineLayout] Invalid position at index {i}: {pos}");
                        // 设置默认位置
                        characters[i].transform.localPosition = Vector3.zero;
                    }
                }
                
                // 每处理几个字符，让出一帧以避免卡顿
                if (i % 10 == 0 && i > 0)
                    await UniTask.Yield();
            }
            
            Debug.Log($"[MultiLineLayout] Applied layout to {appliedCount}/{characters.Length} characters");
        }
        
        /// <summary>
        /// 验证布局计算结果
        /// </summary>
        private bool ValidateLayoutResult(string text, Vector3[] positions)
        {
            if (string.IsNullOrEmpty(text) || positions == null)
                return false;
                
            if (text.Length != positions.Length)
            {
                Debug.LogError($"[MultiLineLayout] Validation failed: text length {text.Length} != positions length {positions.Length}");
                return false;
            }
            
            int validPositions = 0;
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 pos = positions[i];
                if (!float.IsNaN(pos.x) && !float.IsNaN(pos.y) && !float.IsNaN(pos.z))
                {
                    validPositions++;
                }
            }
            
            Debug.Log($"[MultiLineLayout] Validation: {validPositions}/{positions.Length} valid positions");
            return validPositions > 0;
        }
    }
    
    /// <summary>
    /// 网格布局配置
    /// </summary>
    [System.Serializable]
    public class MultiLineLayoutConfig : ILayoutConfig
    {
        [Header("基础设置")]
        public Vector3 StartOffset = Vector3.zero;
        
        [Header("网格设置")]
        [Tooltip("每行最大字符数")]
        [Range(1, 50)]
        public int MaxCharactersPerLine = 25;
        
        [Tooltip("行间距")]
        [Range(0.5f, 5f)]
        public float LineSpacing = 1f;
    }
}