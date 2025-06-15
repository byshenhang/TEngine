using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// LRC歌词文件解析器
    /// 支持解析标准LRC格式的歌词文件
    /// </summary>
    public class LRCParser
    {
        /// <summary>
        /// LRC歌词条目
        /// </summary>
        [System.Serializable]
        public class LRCEntry
        {
            public float timeInSeconds;
            public string lyricText;
            public string gender; // M: Male, F: Female, D: Duet
            
            public LRCEntry(float time, string text, string genderTag = "")
            {
                timeInSeconds = time;
                lyricText = text;
                gender = genderTag;
            }
            
            public override string ToString()
            {
                return $"[{timeInSeconds:F2}] {lyricText}";
            }
        }
        
        /// <summary>
        /// LRC文件元数据
        /// </summary>
        [System.Serializable]
        public class LRCMetadata
        {
            public string title = "";
            public string artist = "";
            public string album = "";
            public string author = "";
            public string lyricist = "";
            public string length = "";
            public string createdBy = "";
            public float offset = 0f;
            public string version = "";
            
            public override string ToString()
            {
                return $"Title: {title}, Artist: {artist}, Album: {album}";
            }
        }
        
        /// <summary>
        /// 解析结果
        /// </summary>
        [System.Serializable]
        public class LRCParseResult
        {
            public LRCMetadata metadata = new LRCMetadata();
            public List<LRCEntry> entries = new List<LRCEntry>();
            public bool isValid = false;
            public string errorMessage = "";
            
            /// <summary>
            /// 获取总时长（秒）
            /// </summary>
            public float GetTotalDuration()
            {
                if (entries.Count == 0) return 0f;
                
                // 如果元数据中有长度信息，优先使用
                if (!string.IsNullOrEmpty(metadata.length))
                {
                    if (TryParseTimeString(metadata.length, out float duration))
                    {
                        return duration;
                    }
                }
                
                // 否则使用最后一个条目的时间 + 默认显示时长
                return entries[entries.Count - 1].timeInSeconds + 3f;
            }
        }
        
        // 时间标签正则表达式 [mm:ss.xx] 或 [mm:ss:xx]
        private static readonly Regex TimeTagRegex = new Regex(@"\[(\d{1,2}):(\d{2})[.:]?(\d{0,3})\]");
        
        // ID标签正则表达式 [tag:value]
        private static readonly Regex IdTagRegex = new Regex(@"\[([a-zA-Z]+):(.*)\]");
        
        // 性别标签正则表达式 M:, F:, D:
        private static readonly Regex GenderTagRegex = new Regex(@"^([MFD]):\s*(.*)$");
        
        /// <summary>
        /// 解析LRC文件内容
        /// </summary>
        /// <param name="lrcContent">LRC文件内容</param>
        /// <returns>解析结果</returns>
        public static LRCParseResult ParseLRC(string lrcContent)
        {
            var result = new LRCParseResult();
            
            if (string.IsNullOrEmpty(lrcContent))
            {
                result.errorMessage = "LRC内容为空";
                return result;
            }
            
            try
            {
                var lines = lrcContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine)) continue;
                    
                    // 尝试解析ID标签
                    if (TryParseIdTag(trimmedLine, result.metadata))
                    {
                        continue;
                    }
                    
                    // 尝试解析时间标签
                    if (TryParseTimeTags(trimmedLine, result.entries))
                    {
                        continue;
                    }
                }
                
                // 应用偏移量
                ApplyOffset(result.entries, result.metadata.offset);
                
                // 按时间排序
                result.entries.Sort((a, b) => a.timeInSeconds.CompareTo(b.timeInSeconds));
                
                result.isValid = result.entries.Count > 0;
                if (!result.isValid)
                {
                    result.errorMessage = "未找到有效的歌词条目";
                }
                
                Log.Info($"[LRCParser] 解析完成，共{result.entries.Count}个歌词条目");
            }
            catch (Exception ex)
            {
                result.errorMessage = $"解析LRC文件时发生错误: {ex.Message}";
                Log.Error($"[LRCParser] {result.errorMessage}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 尝试解析ID标签
        /// </summary>
        private static bool TryParseIdTag(string line, LRCMetadata metadata)
        {
            var match = IdTagRegex.Match(line);
            if (!match.Success) return false;
            
            var tag = match.Groups[1].Value.ToLower();
            var value = match.Groups[2].Value.Trim();
            
            switch (tag)
            {
                case "ti":
                    metadata.title = value;
                    break;
                case "ar":
                    metadata.artist = value;
                    break;
                case "al":
                    metadata.album = value;
                    break;
                case "au":
                    metadata.author = value;
                    break;
                case "lr":
                    metadata.lyricist = value;
                    break;
                case "length":
                    metadata.length = value;
                    break;
                case "by":
                    metadata.createdBy = value;
                    break;
                case "offset":
                    if (float.TryParse(value, out float offset))
                    {
                        metadata.offset = offset / 1000f; // 转换为秒
                    }
                    break;
                case "ve":
                    metadata.version = value;
                    break;
            }
            
            return true;
        }
        
        /// <summary>
        /// 尝试解析时间标签
        /// </summary>
        private static bool TryParseTimeTags(string line, List<LRCEntry> entries)
        {
            var matches = TimeTagRegex.Matches(line);
            if (matches.Count == 0) return false;
            
            // 提取歌词文本（移除所有时间标签后的内容）
            var lyricText = TimeTagRegex.Replace(line, "").Trim();
            
            // 检查性别标签
            string genderTag = "";
            var genderMatch = GenderTagRegex.Match(lyricText);
            if (genderMatch.Success)
            {
                genderTag = genderMatch.Groups[1].Value;
                lyricText = genderMatch.Groups[2].Value.Trim();
            }
            
            // 为每个时间标签创建条目
            foreach (Match match in matches)
            {
                if (TryParseTimeTag(match, out float timeInSeconds))
                {
                    entries.Add(new LRCEntry(timeInSeconds, lyricText, genderTag));
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 解析单个时间标签
        /// </summary>
        private static bool TryParseTimeTag(Match match, out float timeInSeconds)
        {
            timeInSeconds = 0f;
            
            if (!int.TryParse(match.Groups[1].Value, out int minutes)) return false;
            if (!int.TryParse(match.Groups[2].Value, out int seconds)) return false;
            
            int hundredths = 0;
            if (match.Groups[3].Success && !string.IsNullOrEmpty(match.Groups[3].Value))
            {
                var hundredthsStr = match.Groups[3].Value.PadRight(2, '0').Substring(0, 2);
                int.TryParse(hundredthsStr, out hundredths);
            }
            
            timeInSeconds = minutes * 60f + seconds + hundredths / 100f;
            return true;
        }
        
        /// <summary>
        /// 解析时间字符串 (mm:ss 格式)
        /// </summary>
        private static bool TryParseTimeString(string timeStr, out float timeInSeconds)
        {
            timeInSeconds = 0f;
            
            var parts = timeStr.Split(':');
            if (parts.Length != 2) return false;
            
            if (!int.TryParse(parts[0], out int minutes)) return false;
            if (!int.TryParse(parts[1], out int seconds)) return false;
            
            timeInSeconds = minutes * 60f + seconds;
            return true;
        }
        
        /// <summary>
        /// 应用时间偏移量
        /// </summary>
        private static void ApplyOffset(List<LRCEntry> entries, float offsetSeconds)
        {
            if (Math.Abs(offsetSeconds) < 0.001f) return;
            
            foreach (var entry in entries)
            {
                entry.timeInSeconds += offsetSeconds;
                // 确保时间不为负数
                if (entry.timeInSeconds < 0f)
                {
                    entry.timeInSeconds = 0f;
                }
            }
        }
        
        /// <summary>
        /// 将LRC解析结果转换为字幕序列配置
        /// </summary>
        /// <param name="parseResult">LRC解析结果</param>
        /// <param name="displayDuration">每行歌词的显示时长（秒）</param>
        /// <param name="enableEffects">是否启用默认效果</param>
        /// <returns>字幕序列配置列表</returns>
        public static List<SubtitleSequenceConfig> ConvertToSubtitleConfigs(LRCParseResult parseResult, float displayDuration = 3f, bool enableEffects = true)
        {
            var configs = new List<SubtitleSequenceConfig>();
            
            if (!parseResult.isValid || parseResult.entries.Count == 0)
            {
                Log.Warning("[LRCParser] LRC解析结果无效，无法转换为字幕配置");
                return configs;
            }
            
            for (int i = 0; i < parseResult.entries.Count; i++)
            {
                var entry = parseResult.entries[i];
                
                // 计算显示时长
                float duration = displayDuration;
                if (i < parseResult.entries.Count - 1)
                {
                    // 如果不是最后一行，使用到下一行的时间间隔
                    float nextTime = parseResult.entries[i + 1].timeInSeconds;
                    duration = Math.Min(displayDuration, nextTime - entry.timeInSeconds);
                }
                
                var config = new SubtitleSequenceConfig
                {
                    Text = entry.lyricText,
                    SequenceType = SubtitleSequenceType.Lyric,
                    DisplayMode = SubtitleDisplayMode.WordByWord,
                    WordInterval = 0.1f,
                    Duration = duration,
                    Position = Vector3.zero,
                    FontSize = (int)28f,
                    TextColor = GetGenderColor(entry.gender),
                    HoldDuration = Math.Max(0.5f, duration - 1f) // 保持时间为总时长减去进出场效果时间
                };
                
                // 添加默认效果
                if (enableEffects)
                {
                    AddDefaultLyricEffects(config);
                }
                
                configs.Add(config);
            }
            
            Log.Info($"[LRCParser] 转换完成，生成{configs.Count}个字幕配置，效果启用: {enableEffects}");
            return configs;
        }
        
        /// <summary>
        /// 为歌词配置添加默认效果
        /// </summary>
        /// <param name="config">字幕序列配置</param>
        private static void AddDefaultLyricEffects(SubtitleSequenceConfig config)
        {
            // 进场效果：淡入 + 缩放
            config.Effects.Add(new FadeEffectConfig
            {
                Phase = SubtitleEffectPhase.Enter,
                Duration = 0.5f,
                Delay = 0f,
                AlphaStart = 0f,
                AlphaEnd = 1f
            });
            
            config.Effects.Add(new ScaleEffectConfig
            {
                Phase = SubtitleEffectPhase.Enter,
                Duration = 0.5f,
                Delay = 0f,
                ScaleStart = new Vector3(0.8f, 0.8f, 1f),
                ScaleEnd = Vector3.one
            });
            
            // 离场效果：淡出
            config.Effects.Add(new FadeEffectConfig
            {
                Phase = SubtitleEffectPhase.Exit,
                Duration = 0.5f,
                Delay = 0f,
                AlphaStart = 1f,
                AlphaEnd = 0f
            });
        }
        
        /// <summary>
        /// 根据性别标签获取颜色
        /// </summary>
        private static Color GetGenderColor(string gender)
        {
            return gender switch
            {
                "M" => Color.blue,    // 男声 - 蓝色
                "F" => Color.red,     // 女声 - 红色
                "D" => Color.magenta, // 合唱 - 洋红色
                _ => Color.white       // 默认 - 白色
            };
        }
        
        /// <summary>
        /// 将LRC解析结果转换为定时字幕条目
        /// </summary>
        /// <param name="parseResult">LRC解析结果</param>
        /// <param name="subtitleModule">字幕模块实例</param>
        /// <param name="displayDuration">每行歌词的显示时长（秒）</param>
        /// <param name="enableEffects">是否启用默认效果</param>
        /// <returns>定时字幕条目列表</returns>
        public static List<TimedSubtitleEntry> ConvertToTimedSubtitles(LRCParseResult parseResult, SubtitleModule subtitleModule, float displayDuration = 3f, bool enableEffects = true)
        {
            var timedSubtitles = new List<TimedSubtitleEntry>();
            
            if (!parseResult.isValid || parseResult.entries.Count == 0)
            {
                Log.Warning("[LRCParser] LRC解析结果无效，无法转换为定时字幕");
                return timedSubtitles;
            }
            
            var configs = ConvertToSubtitleConfigs(parseResult, displayDuration, enableEffects);
            
            for (int i = 0; i < parseResult.entries.Count; i++)
            {
                var entry = parseResult.entries[i];
                var config = configs[i];
                
                // 使用SubtitleModule的公共方法添加定时字幕
                subtitleModule.AddTimedSubtitle($"lrc_line_{i}", entry.timeInSeconds, config.Duration, config);
            }
            
            Log.Info($"[LRCParser] 转换完成，生成{parseResult.entries.Count}个定时字幕条目，效果启用: {enableEffects}");
            return new List<TimedSubtitleEntry>(); // 返回空列表，因为字幕已直接添加到模块中
        }
    }
}