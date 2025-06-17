using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LyricFX.Parser
{
    /// <summary>
    /// LRC解析器 - 解析LRC格式的歌词文件
    /// </summary>
    public class LrcParser
    {
        // 时间戳正则表达式
        private static readonly Regex TimeTagRegex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\]", RegexOptions.Compiled);
        
        // 元数据标签正则表达式
        private static readonly Regex MetaTagRegex = new Regex(@"\[([\w\d]+):(.+?)\]", RegexOptions.Compiled);
        
        /// <summary>
        /// 解析LRC文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>解析后的歌词列表</returns>
        public async UniTask<List<LrcLine>> ParseLrcFile(string filePath)
        {
            string content;
            
            try
            {
                // 判断是Resources路径还是普通文件路径
                if (filePath.StartsWith("Assets/Resources/") || filePath.StartsWith("Resources/"))
                {
                    // 从Resources加载
                    string resourcePath = filePath.Replace("Assets/Resources/", "").Replace("Resources/", "");
                    resourcePath = Path.ChangeExtension(resourcePath, null); // 去除扩展名
                    
                    var textAsset = Resources.Load<TextAsset>(resourcePath);
                    if (textAsset == null)
                    {
                        Debug.LogError($"[LRC解析器] 无法从Resources加载LRC文件: {resourcePath}");
                        return new List<LrcLine>();
                    }
                    
                    content = textAsset.text;
                }
                else
                {
                    // 读取本地文件
                    content = await File.ReadAllTextAsync(filePath);
                }
                
                return ParseLrcContent(content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LRC解析器] 解析LRC文件失败: {ex.Message}");
                return new List<LrcLine>();
            }
        }
        
        /// <summary>
        /// 解析LRC内容
        /// </summary>
        /// <param name="content">LRC文件内容</param>
        /// <returns>解析后的歌词列表</returns>
        public List<LrcLine> ParseLrcContent(string content)
        {
            var result = new List<LrcLine>();
            var metadata = new LrcMetadata();
            
            // 按行分割
            string[] lines = content.Replace("\r", "").Split('\n');
            
            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;
                
                // 检查是否是元数据行
                var metaMatch = MetaTagRegex.Match(trimmedLine);
                if (metaMatch.Success && !TimeTagRegex.IsMatch(trimmedLine))
                {
                    // 处理元数据
                    string key = metaMatch.Groups[1].Value.ToLower();
                    string value = metaMatch.Groups[2].Value;
                    
                    switch (key)
                    {
                        case "ti": metadata.Title = value; break;
                        case "ar": metadata.Artist = value; break;
                        case "al": metadata.Album = value; break;
                        case "by": metadata.Creator = value; break;
                        case "offset": 
                            if (float.TryParse(value, out float offset))
                                metadata.Offset = offset;
                            break;
                    }
                    
                    continue;
                }
                
                // 处理带时间戳的行
                var timeMatches = TimeTagRegex.Matches(trimmedLine);
                if (timeMatches.Count > 0)
                {
                    // 提取歌词内容（去除所有时间标签）
                    string lyricContent = TimeTagRegex.Replace(trimmedLine, "").Trim();
                    
                    // 处理每个时间标签
                    foreach (Match match in timeMatches)
                    {
                        int minutes = int.Parse(match.Groups[1].Value);
                        int seconds = int.Parse(match.Groups[2].Value);
                        int milliseconds;
                        
                        // 处理毫秒，可能是2位或3位
                        string msStr = match.Groups[3].Value;
                        if (msStr.Length == 2)
                            milliseconds = int.Parse(msStr) * 10;
                        else
                            milliseconds = int.Parse(msStr);
                        
                        // 计算总时间（秒）
                        double timeStamp = minutes * 60 + seconds + milliseconds / 1000.0;
                        
                        // 应用偏移（毫秒转换为秒）
                        timeStamp += metadata.Offset / 1000.0;
                        
                        // 添加到结果
                        result.Add(new LrcLine
                        {
                            TimeStamp = timeStamp,
                            Text = lyricContent
                        });
                    }
                }
            }
            
            // 按时间戳排序
            result.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));
            
            Debug.Log($"[LRC解析器] 解析完成, 共 {result.Count} 行歌词");
            return result;
        }
    }
    
    /// <summary>
    /// LRC歌词行
    /// </summary>
    [Serializable]
    public class LrcLine
    {
        /// <summary>
        /// 时间戳（秒）
        /// </summary>
        public double TimeStamp;
        
        /// <summary>
        /// 歌词文本
        /// </summary>
        public string Text;
    }
    
    /// <summary>
    /// LRC元数据
    /// </summary>
    [Serializable]
    public class LrcMetadata
    {
        /// <summary>
        /// 歌曲标题
        /// </summary>
        public string Title;
        
        /// <summary>
        /// 艺术家
        /// </summary>
        public string Artist;
        
        /// <summary>
        /// 专辑
        /// </summary>
        public string Album;
        
        /// <summary>
        /// 创建者
        /// </summary>
        public string Creator;
        
        /// <summary>
        /// 时间偏移（毫秒）
        /// </summary>
        public float Offset = 0;
    }
}
