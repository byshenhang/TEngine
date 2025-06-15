using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// LRC文件加载器
    /// 负责从各种来源加载LRC文件并解析
    /// </summary>
    public class LRCLoader
    {
        /// <summary>
        /// 从文件路径加载LRC文件
        /// </summary>
        /// <param name="filePath">LRC文件路径</param>
        /// <param name="encoding">文件编码，默认为UTF-8</param>
        /// <returns>LRC解析结果</returns>
        public static async UniTask<LRCParser.LRCParseResult> LoadFromFileAsync(string filePath, Encoding encoding = null)
        {
            var result = new LRCParser.LRCParseResult();
            
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    result.errorMessage = "文件路径为空";
                    return result;
                }
                
                if (!File.Exists(filePath))
                {
                    result.errorMessage = $"文件不存在: {filePath}";
                    return result;
                }
                
                // 默认使用UTF-8编码
                if (encoding == null)
                {
                    encoding = Encoding.UTF8;
                }
                
                Log.Info($"[LRCLoader] 开始加载LRC文件: {filePath}");
                
                // 异步读取文件内容
                string content;
                using (var reader = new StreamReader(filePath, encoding))
                {
                    content = await reader.ReadToEndAsync();
                }
                
                if (string.IsNullOrEmpty(content))
                {
                    result.errorMessage = "文件内容为空";
                    return result;
                }
                
                // 解析LRC内容
                result = LRCParser.ParseLRC(content);
                
                if (result.isValid)
                {
                    Log.Info($"[LRCLoader] LRC文件加载成功: {filePath}, 共{result.entries.Count}个条目");
                }
                else
                {
                    Log.Warning($"[LRCLoader] LRC文件解析失败: {result.errorMessage}");
                }
            }
            catch (Exception ex)
            {
                result.errorMessage = $"加载LRC文件时发生错误: {ex.Message}";
                Log.Error($"[LRCLoader] {result.errorMessage}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 从Resources文件夹加载LRC文件
        /// </summary>
        /// <param name="resourcePath">Resources中的路径（不包含扩展名）</param>
        /// <returns>LRC解析结果</returns>
        public static async UniTask<LRCParser.LRCParseResult> LoadFromResourcesAsync(string resourcePath)
        {
            var result = new LRCParser.LRCParseResult();
            
            try
            {
                if (string.IsNullOrEmpty(resourcePath))
                {
                    result.errorMessage = "资源路径为空";
                    return result;
                }
                
                Log.Info($"[LRCLoader] 开始从Resources加载LRC文件: {resourcePath}");
                
                // 从Resources加载TextAsset
                var textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset == null)
                {
                    result.errorMessage = $"无法在Resources中找到文件: {resourcePath}";
                    return result;
                }
                
                // 解析LRC内容
                result = LRCParser.ParseLRC(textAsset.text);
                
                if (result.isValid)
                {
                    Log.Info($"[LRCLoader] LRC文件从Resources加载成功: {resourcePath}, 共{result.entries.Count}个条目");
                }
                else
                {
                    Log.Warning($"[LRCLoader] LRC文件解析失败: {result.errorMessage}");
                }
                
                // 释放资源
                Resources.UnloadAsset(textAsset);
            }
            catch (Exception ex)
            {
                result.errorMessage = $"从Resources加载LRC文件时发生错误: {ex.Message}";
                Log.Error($"[LRCLoader] {result.errorMessage}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 从StreamingAssets文件夹加载LRC文件
        /// </summary>
        /// <param name="fileName">文件名（包含扩展名）</param>
        /// <returns>LRC解析结果</returns>
        public static async UniTask<LRCParser.LRCParseResult> LoadFromStreamingAssetsAsync(string fileName)
        {
            var result = new LRCParser.LRCParseResult();
            
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    result.errorMessage = "文件名为空";
                    return result;
                }
                
                string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
                Log.Info($"[LRCLoader] 开始从StreamingAssets加载LRC文件: {filePath}");
                
                // 在不同平台上处理StreamingAssets的访问
#if UNITY_ANDROID && !UNITY_EDITOR
                // Android平台需要使用UnityWebRequest
                using (var request = UnityEngine.Networking.UnityWebRequest.Get(filePath))
                {
                    await request.SendWebRequest();
                    
                    if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        result.errorMessage = $"无法加载文件: {request.error}";
                        return result;
                    }
                    
                    result = LRCParser.ParseLRC(request.downloadHandler.text);
                }
#else
                // 其他平台直接使用文件系统
                if (!File.Exists(filePath))
                {
                    result.errorMessage = $"文件不存在: {filePath}";
                    return result;
                }
                
                string content;
                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    content = await reader.ReadToEndAsync();
                }
                
                result = LRCParser.ParseLRC(content);
#endif
                
                if (result.isValid)
                {
                    Log.Info($"[LRCLoader] LRC文件从StreamingAssets加载成功: {fileName}, 共{result.entries.Count}个条目");
                }
                else
                {
                    Log.Warning($"[LRCLoader] LRC文件解析失败: {result.errorMessage}");
                }
            }
            catch (Exception ex)
            {
                result.errorMessage = $"从StreamingAssets加载LRC文件时发生错误: {ex.Message}";
                Log.Error($"[LRCLoader] {result.errorMessage}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 从URL加载LRC文件
        /// </summary>
        /// <param name="url">LRC文件的URL</param>
        /// <returns>LRC解析结果</returns>
        public static async UniTask<LRCParser.LRCParseResult> LoadFromUrlAsync(string url)
        {
            var result = new LRCParser.LRCParseResult();
            
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    result.errorMessage = "URL为空";
                    return result;
                }
                
                Log.Info($"[LRCLoader] 开始从URL加载LRC文件: {url}");
                
                using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
                {
                    await request.SendWebRequest();
                    
                    if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        result.errorMessage = $"无法从URL加载文件: {request.error}";
                        return result;
                    }
                    
                    result = LRCParser.ParseLRC(request.downloadHandler.text);
                }
                
                if (result.isValid)
                {
                    Log.Info($"[LRCLoader] LRC文件从URL加载成功: {url}, 共{result.entries.Count}个条目");
                }
                else
                {
                    Log.Warning($"[LRCLoader] LRC文件解析失败: {result.errorMessage}");
                }
            }
            catch (Exception ex)
            {
                result.errorMessage = $"从URL加载LRC文件时发生错误: {ex.Message}";
                Log.Error($"[LRCLoader] {result.errorMessage}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 直接从字符串解析LRC内容
        /// </summary>
        /// <param name="lrcContent">LRC文件内容</param>
        /// <returns>LRC解析结果</returns>
        public static LRCParser.LRCParseResult LoadFromString(string lrcContent)
        {
            Log.Info($"[LRCLoader] 开始解析LRC字符串内容");
            
            var result = LRCParser.ParseLRC(lrcContent);
            
            if (result.isValid)
            {
                Log.Info($"[LRCLoader] LRC字符串解析成功，共{result.entries.Count}个条目");
            }
            else
            {
                Log.Warning($"[LRCLoader] LRC字符串解析失败: {result.errorMessage}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 自动检测文件编码并加载LRC文件
        /// </summary>
        /// <param name="filePath">LRC文件路径</param>
        /// <returns>LRC解析结果</returns>
        public static async UniTask<LRCParser.LRCParseResult> LoadWithEncodingDetectionAsync(string filePath)
        {
            var result = new LRCParser.LRCParseResult();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    result.errorMessage = $"文件不存在: {filePath}";
                    return result;
                }
                
                Log.Info($"[LRCLoader] 开始自动检测编码并加载LRC文件: {filePath}");
                
                // 读取文件的前几个字节来检测编码
                byte[] buffer = new byte[4];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await fs.ReadAsync(buffer, 0, 4);
                }
                
                Encoding encoding = DetectEncoding(buffer);
                Log.Info($"[LRCLoader] 检测到编码: {encoding.EncodingName}");
                
                // 使用检测到的编码加载文件
                result = await LoadFromFileAsync(filePath, encoding);
            }
            catch (Exception ex)
            {
                result.errorMessage = $"自动检测编码加载LRC文件时发生错误: {ex.Message}";
                Log.Error($"[LRCLoader] {result.errorMessage}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 检测文件编码
        /// </summary>
        /// <param name="buffer">文件头字节</param>
        /// <returns>检测到的编码</returns>
        private static Encoding DetectEncoding(byte[] buffer)
        {
            // 检测BOM
            if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            {
                return Encoding.UTF8;
            }
            
            if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
            {
                return Encoding.Unicode; // UTF-16 LE
            }
            
            if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode; // UTF-16 BE
            }
            
            if (buffer.Length >= 4 && buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
            {
                return Encoding.UTF32; // UTF-32 LE
            }
            
            if (buffer.Length >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF)
            {
                return new UTF32Encoding(true, true); // UTF-32 BE
            }
            
            // 默认返回UTF-8
            return Encoding.UTF8;
        }
        
        /// <summary>
        /// 批量加载多个LRC文件
        /// </summary>
        /// <param name="filePaths">LRC文件路径列表</param>
        /// <returns>LRC解析结果列表</returns>
        public static async UniTask<List<LRCParser.LRCParseResult>> LoadMultipleFilesAsync(List<string> filePaths)
        {
            var results = new List<LRCParser.LRCParseResult>();
            
            if (filePaths == null || filePaths.Count == 0)
            {
                Log.Warning("[LRCLoader] 文件路径列表为空");
                return results;
            }
            
            Log.Info($"[LRCLoader] 开始批量加载{filePaths.Count}个LRC文件");
            
            var tasks = new List<UniTask<LRCParser.LRCParseResult>>();
            
            foreach (var filePath in filePaths)
            {
                tasks.Add(LoadFromFileAsync(filePath));
            }
            
            var loadResults = await UniTask.WhenAll(tasks);
            results.AddRange(loadResults);
            
            int successCount = 0;
            foreach (var result in results)
            {
                if (result.isValid) successCount++;
            }
            
            Log.Info($"[LRCLoader] 批量加载完成，成功{successCount}/{filePaths.Count}个文件");
            
            return results;
        }
    }
}