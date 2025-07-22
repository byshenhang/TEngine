using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TEngine.PersistentData
{
    /// <summary>
    /// 持久化文件工具类
    /// 提供常用的文件操作静态方法
    /// </summary>
    public static class PersistentFileUtils
    {
        /// <summary>
        /// 安全的文件名字符
        /// </summary>
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        
        /// <summary>
        /// 安全的路径字符
        /// </summary>
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

        /// <summary>
        /// 验证文件名是否有效
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否有效</returns>
        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
                
            return !fileName.Any(c => InvalidFileNameChars.Contains(c));
        }

        /// <summary>
        /// 验证路径是否有效
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>是否有效</returns>
        public static bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
                
            try
            {
                Path.GetFullPath(path);
                return !path.Any(c => InvalidPathChars.Contains(c));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 清理文件名，移除无效字符
        /// </summary>
        /// <param name="fileName">原文件名</param>
        /// <param name="replacement">替换字符，默认为下划线</param>
        /// <returns>清理后的文件名</returns>
        public static string SanitizeFileName(string fileName, char replacement = '_')
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "unnamed";

            var sanitized = fileName;
            foreach (var invalidChar in InvalidFileNameChars)
            {
                sanitized = sanitized.Replace(invalidChar, replacement);
            }

            // 移除开头和结尾的空格和点
            sanitized = sanitized.Trim(' ', '.');
            
            return string.IsNullOrEmpty(sanitized) ? "unnamed" : sanitized;
        }

        /// <summary>
        /// 生成唯一的文件名（如果文件已存在）
        /// </summary>
        /// <param name="directory">目录路径</param>
        /// <param name="fileName">原文件名</param>
        /// <returns>唯一的文件名</returns>
        public static string GenerateUniqueFileName(string directory, string fileName)
        {
            var fullPath = Path.Combine(directory, fileName);
            if (!File.Exists(fullPath))
                return fileName;

            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var counter = 1;

            do
            {
                var newFileName = $"{nameWithoutExt} ({counter}){extension}";
                fullPath = Path.Combine(directory, newFileName);
                counter++;
            }
            while (File.Exists(fullPath));

            return Path.GetFileName(fullPath);
        }

        /// <summary>
        /// 获取文件的MIME类型
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>MIME类型</returns>
        public static string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            
            return extension switch
            {
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".mp4" => "video/mp4",
                ".avi" => "video/avi",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// 检查文件是否为文本文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为文本文件</returns>
        public static bool IsTextFile(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            
            var textExtensions = new HashSet<string>
            {
                ".txt", ".json", ".xml", ".html", ".css", ".js", ".cs", ".py", ".java",
                ".cpp", ".c", ".h", ".md", ".log", ".ini", ".cfg", ".conf", ".yaml", ".yml"
            };
            
            return textExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件是否为图片文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为图片文件</returns>
        public static bool IsImageFile(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            
            var imageExtensions = new HashSet<string>
            {
                ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".svg", ".tiff", ".webp", ".ico"
            };
            
            return imageExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件是否为音频文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为音频文件</returns>
        public static bool IsAudioFile(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            
            var audioExtensions = new HashSet<string>
            {
                ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a", ".wma"
            };
            
            return audioExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件是否为视频文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为视频文件</returns>
        public static bool IsVideoFile(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            
            var videoExtensions = new HashSet<string>
            {
                ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v"
            };
            
            return videoExtensions.Contains(extension);
        }

        /// <summary>
        /// 比较两个文件是否相同（通过内容比较）
        /// </summary>
        /// <param name="file1Path">文件1路径</param>
        /// <param name="file2Path">文件2路径</param>
        /// <returns>是否相同</returns>
        public static bool AreFilesEqual(string file1Path, string file2Path)
        {
            try
            {
                if (!File.Exists(file1Path) || !File.Exists(file2Path))
                    return false;

                var file1Info = new FileInfo(file1Path);
                var file2Info = new FileInfo(file2Path);

                // 首先比较文件大小
                if (file1Info.Length != file2Info.Length)
                    return false;

                // 如果大小相同，比较内容
                using var file1Stream = File.OpenRead(file1Path);
                using var file2Stream = File.OpenRead(file2Path);
                
                const int bufferSize = 4096;
                var buffer1 = new byte[bufferSize];
                var buffer2 = new byte[bufferSize];

                int bytesRead1, bytesRead2;
                do
                {
                    bytesRead1 = file1Stream.Read(buffer1, 0, bufferSize);
                    bytesRead2 = file2Stream.Read(buffer2, 0, bufferSize);

                    if (bytesRead1 != bytesRead2)
                        return false;

                    for (int i = 0; i < bytesRead1; i++)
                    {
                        if (buffer1[i] != buffer2[i])
                            return false;
                    }
                }
                while (bytesRead1 > 0);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"比较文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 搜索文件
        /// </summary>
        /// <param name="directory">搜索目录</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <param name="recursive">是否递归搜索</param>
        /// <param name="includeDirectories">是否包含目录</param>
        /// <returns>搜索结果</returns>
        public static List<PersistentItem> SearchFiles(string directory, string searchPattern = "*", bool recursive = true, bool includeDirectories = false)
        {
            var results = new List<PersistentItem>();
            
            if (!Directory.Exists(directory))
                return results;

            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                // 搜索文件
                var files = Directory.GetFiles(directory, searchPattern, searchOption);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    results.Add(new PersistentItem
                    {
                        Name = fileInfo.Name,
                        FullPath = fileInfo.FullName,
                        IsDirectory = false,
                        Size = fileInfo.Length,
                        CreationTime = fileInfo.CreationTime,
                        LastWriteTime = fileInfo.LastWriteTime
                    });
                }

                // 搜索目录（如果需要）
                if (includeDirectories)
                {
                    var directories = Directory.GetDirectories(directory, searchPattern, searchOption);
                    foreach (var dir in directories)
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        results.Add(new PersistentItem
                        {
                            Name = dirInfo.Name,
                            FullPath = dirInfo.FullName,
                            IsDirectory = true,
                            Size = 0,
                            CreationTime = dirInfo.CreationTime,
                            LastWriteTime = dirInfo.LastWriteTime
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"搜索文件失败: {directory}, 错误: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// 批量删除文件
        /// </summary>
        /// <param name="filePaths">文件路径列表</param>
        /// <returns>删除成功的文件数量</returns>
        public static int BatchDeleteFiles(IEnumerable<string> filePaths)
        {
            int successCount = 0;
            
            foreach (var filePath in filePaths)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除文件失败: {filePath}, 错误: {ex.Message}");
                }
            }

            Debug.Log($"批量删除完成，成功删除 {successCount} 个文件");
            return successCount;
        }

        /// <summary>
        /// 获取目录的磁盘使用情况
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>磁盘使用信息</returns>
        public static DiskUsageInfo GetDiskUsage(string directoryPath)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(directoryPath));
                return new DiskUsageInfo
                {
                    TotalSize = driveInfo.TotalSize,
                    FreeSpace = driveInfo.AvailableFreeSpace,
                    UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取磁盘使用情况失败: {directoryPath}, 错误: {ex.Message}");
                return new DiskUsageInfo();
            }
        }
    }

    /// <summary>
    /// 磁盘使用信息
    /// </summary>
    [Serializable]
    public class DiskUsageInfo
    {
        /// <summary>
        /// 总大小（字节）
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// 可用空间（字节）
        /// </summary>
        public long FreeSpace { get; set; }

        /// <summary>
        /// 已使用空间（字节）
        /// </summary>
        public long UsedSpace { get; set; }

        /// <summary>
        /// 使用率（百分比）
        /// </summary>
        public double UsagePercentage => TotalSize > 0 ? (double)UsedSpace / TotalSize * 100 : 0;

        /// <summary>
        /// 获取格式化的总大小
        /// </summary>
        public string FormattedTotalSize => FormatBytes(TotalSize);

        /// <summary>
        /// 获取格式化的可用空间
        /// </summary>
        public string FormattedFreeSpace => FormatBytes(FreeSpace);

        /// <summary>
        /// 获取格式化的已使用空间
        /// </summary>
        public string FormattedUsedSpace => FormatBytes(UsedSpace);

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        public override string ToString()
        {
            return $"总空间: {FormattedTotalSize}, 已使用: {FormattedUsedSpace} ({UsagePercentage:0.1}%), 可用: {FormattedFreeSpace}";
        }
    }
}