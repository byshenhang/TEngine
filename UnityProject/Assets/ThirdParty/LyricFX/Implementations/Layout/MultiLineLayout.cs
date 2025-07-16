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
        /// 计算网格布局
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
            
            // 预处理文本：美化歌词显示
            string processedText = PreprocessText(text);
            if (string.IsNullOrEmpty(processedText))
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
            
            // 创建字符映射表，记录原始字符索引到处理后字符索引的映射
            var charMapping = CreateCharacterMapping(text, processedText);
            var positions = new Vector3[text.Length];
            
            // 计算总行数（基于处理后的文本）
            int totalLines = Mathf.CeilToInt((float)processedText.Length / maxCharsPerLine);
            
            // 计算每行的字符数量，用于居中对齐
            var lineCharCounts = new int[totalLines];
            for (int i = 0; i < processedText.Length; i++)
            {
                int lineIndex = i / maxCharsPerLine;
                lineCharCounts[lineIndex]++;
            }
            
            // 计算网格的总高度（用于垂直居中）
            float totalHeight = (totalLines - 1) * (cellHeight + currentLineSpacing);
            
            // 网格起始Y位置（垂直居中对齐）
            float gridStartY = offset.y + totalHeight * 0.5f;
            
            // 为每个原始字符计算位置
            for (int originalIndex = 0; originalIndex < text.Length; originalIndex++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                // 检查字符是否应该显示
                if (charMapping.TryGetValue(originalIndex, out int processedIndex))
                {
                    // 计算网格坐标
                    int row = processedIndex / maxCharsPerLine;
                    int col = processedIndex % maxCharsPerLine;
                    
                    // 计算当前行的字符数量和行宽
                    int charsInCurrentLine = lineCharCounts[row];
                    float lineWidth = (charsInCurrentLine - 1) * cellWidth;
                    
                    // 计算当前行的起始X位置（水平居中）
                    float lineStartX = offset.x - lineWidth * 0.5f;
                    
                    // 计算实际位置
                    float x = lineStartX + col * cellWidth;
                    float y = gridStartY - row * (cellHeight + currentLineSpacing);
                    
                    positions[originalIndex] = new Vector3(x, y, offset.z);
                }
                else
                {
                    // 不显示的字符（如行首行尾空格）设置到屏幕外
                    positions[originalIndex] = new Vector3(float.MinValue, float.MinValue, 0);
                }
                
                // 每处理几个字符，让出一帧以避免卡顿
                if (originalIndex % 20 == 0 && originalIndex > 0)
                    await UniTask.Yield();
            }
            
            return positions;
        }
        
        /// <summary>
        /// 预处理文本：美化歌词显示
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>处理后的文本</returns>
        private string PreprocessText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            
            // 将文本按行分割处理
            var lines = text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            var processedLines = new List<string>();
            
            foreach (var line in lines)
            {
                // 去除行首行尾空格
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    processedLines.Add(trimmedLine);
                }
            }
            
            return string.Join("", processedLines);
        }
        
        /// <summary>
        /// 创建字符映射表
        /// </summary>
        /// <param name="originalText">原始文本</param>
        /// <param name="processedText">处理后文本</param>
        /// <returns>原始索引到处理后索引的映射</returns>
        private Dictionary<int, int> CreateCharacterMapping(string originalText, string processedText)
        {
            var mapping = new Dictionary<int, int>();
            int processedIndex = 0;
            
            for (int originalIndex = 0; originalIndex < originalText.Length; originalIndex++)
            {
                char originalChar = originalText[originalIndex];
                
                // 跳过换行符和回车符
                if (originalChar == '\n' || originalChar == '\r')
                    continue;
                
                // 检查是否为行首行尾空格（需要更复杂的逻辑来判断）
                if (ShouldDisplayCharacter(originalText, originalIndex))
                {
                    if (processedIndex < processedText.Length)
                    {
                        mapping[originalIndex] = processedIndex;
                        processedIndex++;
                    }
                }
            }
            
            return mapping;
        }
        
        /// <summary>
        /// 判断字符是否应该显示
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <param name="index">字符索引</param>
        /// <returns>是否应该显示</returns>
        private bool ShouldDisplayCharacter(string text, int index)
        {
            char currentChar = text[index];
            
            // 非空格字符总是显示
            if (currentChar != ' ')
                return true;
            
            // 对于空格，检查是否为行首或行尾
            int lineStart = index;
            int lineEnd = index;
            
            // 找到行的开始
            while (lineStart > 0 && text[lineStart - 1] != '\n' && text[lineStart - 1] != '\r')
                lineStart--;
            
            // 找到行的结束
            while (lineEnd < text.Length - 1 && text[lineEnd + 1] != '\n' && text[lineEnd + 1] != '\r')
                lineEnd++;
            
            // 检查是否为行首空格
            bool isLineStartSpace = true;
            for (int i = lineStart; i < index; i++)
            {
                if (text[i] != ' ')
                {
                    isLineStartSpace = false;
                    break;
                }
            }
            
            // 检查是否为行尾空格
            bool isLineEndSpace = true;
            for (int i = index + 1; i <= lineEnd; i++)
            {
                if (text[i] != ' ')
                {
                    isLineEndSpace = false;
                    break;
                }
            }
            
            // 行首或行尾的空格不显示
            return !(isLineStartSpace || isLineEndSpace);
        }
        
        /// <summary>
        /// 应用布局到字符对象
        /// </summary>
        public async UniTask ApplyLayout(GameObject[] characters, Vector3[] positions, CancellationToken cancellationToken = default)
        {
            if (characters == null || positions == null || characters.Length != positions.Length)
                return;
            
            for (int i = 0; i < characters.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                if (characters[i] != null)
                {
                    characters[i].transform.localPosition = positions[i];
                }
                
                // 每处理几个字符，让出一帧以避免卡顿
                if (i % 10 == 0 && i > 0)
                    await UniTask.Yield();
            }
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