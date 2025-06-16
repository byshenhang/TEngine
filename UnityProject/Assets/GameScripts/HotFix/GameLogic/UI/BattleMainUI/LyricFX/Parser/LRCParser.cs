using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using LyricFX.Core;
using UnityEngine;
using System.IO;

namespace LyricFX.Parser
{
    /// <summary>
    /// LRC格式歌词解析器
    /// </summary>
    public class LRCParser : ILyricParser
    {
        private static readonly Regex _timeTagRegex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\]");
        private static readonly Regex _metadataRegex = new Regex(@"\[([a-z]+):(.+?)\]");
        
        /// <summary>
        /// 解析LRC格式的歌词文本
        /// </summary>
        public async UniTask<LyricSequence> ParseAsync(string content, CancellationToken token = default)
        {
            var sequence = new LyricSequence();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            int lineIndex = 0;
            
            // 首先处理元数据
            foreach (var line in lines)
            {
                if (token.IsCancellationRequested) break;
                
                // 处理元数据
                var metaMatch = _metadataRegex.Match(line);
                if (metaMatch.Success && !_timeTagRegex.IsMatch(line))
                {
                    string key = metaMatch.Groups[1].Value.ToLower();
                    string value = metaMatch.Groups[2].Value;
                    
                    switch (key)
                    {
                        case "ti":
                            sequence.Title = value;
                            break;
                        case "ar":
                            sequence.Artist = value;
                            break;
                        case "al":
                            sequence.Album = value;
                            break;
                    }
                    
                    continue;
                }
            }
            
            // 然后处理歌词行
            foreach (var line in lines)
            {
                if (token.IsCancellationRequested) break;
                
                if (_timeTagRegex.IsMatch(line))
                {
                    var match = _timeTagRegex.Match(line);
                    
                    while (match.Success)
                    {
                        int minutes = int.Parse(match.Groups[1].Value);
                        int seconds = int.Parse(match.Groups[2].Value);
                        
                        // 处理毫秒，可能是2位或3位
                        string msStr = match.Groups[3].Value;
                        int ms = int.Parse(msStr);
                        if (msStr.Length == 2) ms *= 10; // 如果是2位，转换为毫秒
                        
                        float startTime = minutes * 60 + seconds + ms / 1000f;
                        
                        // 获取歌词内容，去掉所有时间标签
                        string text = _timeTagRegex.Replace(line, string.Empty).Trim();
                        
                        var lyricLine = new LyricLine
                        {
                            Index = lineIndex,
                            StartTime = startTime,
                            EndTime = startTime + 5.0f, // 默认5秒，后续处理会修正
                            Text = text
                        };
                        
                        // 创建字符数据
                        CreateCharactersForLine(lyricLine);
                        
                        // 添加到序列
                        sequence.Lines.Add(lyricLine);
                        lineIndex++;
                        
                        match = match.NextMatch();
                    }
                }
            }
            
            // 设置正确的结束时间
            for (int i = 0; i < sequence.Lines.Count - 1; i++)
            {
                sequence.Lines[i].EndTime = sequence.Lines[i + 1].StartTime;
            }
            
            // 设置总时长
            if (sequence.Lines.Count > 0)
            {
                var lastLine = sequence.Lines[sequence.Lines.Count - 1];
                sequence.TotalDuration = lastLine.EndTime;
            }
            
            // 在工作线程上等待一帧，避免阻塞主线程
            await UniTask.Yield();
            
            return sequence;
        }
        
        /// <summary>
        /// 为歌词行创建字符数据
        /// </summary>
        private void CreateCharactersForLine(LyricLine line)
        {
            for (int i = 0; i < line.Text.Length; i++)
            {
                var character = new LyricCharacter
                {
                    Character = line.Text[i],
                    Index = i,
                    LineIndex = line.Index
                };
                
                line.Characters.Add(character);
            }
        }
    }
    
    /// <summary>
    /// 歌词解析器接口
    /// </summary>
    public interface ILyricParser
    {
        UniTask<LyricSequence> ParseAsync(string content, CancellationToken token = default);
    }
    
    /// <summary>
    /// 歌词解析器工厂
    /// </summary>
    public static class LyricParserFactory
    {
        public static ILyricParser CreateParser(string filename)
        {
            string extension = Path.GetExtension(filename).ToLower();
            
            switch (extension)
            {
                case ".lrc":
                    return new LRCParser();
                default:
                    return new LRCParser(); // 默认使用LRC解析器
            }
        }
    }
}
