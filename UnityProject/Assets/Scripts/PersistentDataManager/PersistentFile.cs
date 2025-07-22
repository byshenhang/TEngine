using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TEngine.PersistentData
{
    /// <summary>
    /// 持久化文件对象
    /// 提供面向对象的文件操作接口
    /// </summary>
    public class PersistentFile
    {
        private readonly string _fullPath;
        private FileInfo _fileInfo;

        /// <summary>
        /// 文件的完整路径
        /// </summary>
        public string FullPath => _fullPath;

        /// <summary>
        /// 文件名（包含扩展名）
        /// </summary>
        public string Name => Path.GetFileName(_fullPath);

        /// <summary>
        /// 文件名（不包含扩展名）
        /// </summary>
        public string NameWithoutExtension => Path.GetFileNameWithoutExtension(_fullPath);

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string Extension => Path.GetExtension(_fullPath);

        /// <summary>
        /// 相对于持久化根目录的路径
        /// </summary>
        public string RelativePath => Path.GetRelativePath(Application.persistentDataPath, _fullPath);

        /// <summary>
        /// 文件是否存在
        /// </summary>
        public bool Exists => File.Exists(_fullPath);

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long Size
        {
            get
            {
                RefreshFileInfo();
                return _fileInfo?.Length ?? 0;
            }
        }

        /// <summary>
        /// 文件所在目录
        /// </summary>
        public PersistentDirectory Directory
        {
            get
            {
                var directoryPath = Path.GetDirectoryName(_fullPath);
                return directoryPath != null ? new PersistentDirectory(directoryPath) : null;
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime
        {
            get
            {
                RefreshFileInfo();
                return _fileInfo?.CreationTime ?? DateTime.MinValue;
            }
        }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastWriteTime
        {
            get
            {
                RefreshFileInfo();
                return _fileInfo?.LastWriteTime ?? DateTime.MinValue;
            }
        }

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime LastAccessTime
        {
            get
            {
                RefreshFileInfo();
                return _fileInfo?.LastAccessTime ?? DateTime.MinValue;
            }
        }

        /// <summary>
        /// 文件是否为只读
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                RefreshFileInfo();
                return _fileInfo?.IsReadOnly ?? false;
            }
            set
            {
                RefreshFileInfo();
                if (_fileInfo != null)
                {
                    _fileInfo.IsReadOnly = value;
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fullPath">文件的完整路径</param>
        public PersistentFile(string fullPath)
        {
            _fullPath = Path.GetFullPath(fullPath);
        }

        /// <summary>
        /// 创建空文件
        /// </summary>
        /// <returns>当前文件对象</returns>
        public PersistentFile Create()
        {
            try
            {
                // 确保目录存在
                Directory?.Create();
                
                if (!Exists)
                {
                    File.Create(_fullPath).Dispose();
                    Debug.Log($"文件创建成功: {_fullPath}");
                }
                return this;
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建文件失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public void Delete()
        {
            try
            {
                if (Exists)
                {
                    File.Delete(_fullPath);
                    Debug.Log($"文件删除成功: {_fullPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除文件失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 复制文件到指定位置
        /// </summary>
        /// <param name="destinationPath">目标路径</param>
        /// <param name="overwrite">是否覆盖已存在的文件</param>
        /// <returns>目标文件对象</returns>
        public PersistentFile CopyTo(string destinationPath, bool overwrite = true)
        {
            try
            {
                // 确保目标目录存在
                var destDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destDir))
                {
                    new PersistentDirectory(destDir).Create();
                }

                File.Copy(_fullPath, destinationPath, overwrite);
                Debug.Log($"文件复制成功: {_fullPath} -> {destinationPath}");
                return new PersistentFile(destinationPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"复制文件失败: {_fullPath} -> {destinationPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 移动文件到指定位置
        /// </summary>
        /// <param name="destinationPath">目标路径</param>
        /// <param name="overwrite">是否覆盖已存在的文件</param>
        /// <returns>目标文件对象</returns>
        public PersistentFile MoveTo(string destinationPath, bool overwrite = true)
        {
            try
            {
                // 确保目标目录存在
                var destDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destDir))
                {
                    new PersistentDirectory(destDir).Create();
                }

                if (overwrite && File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                File.Move(_fullPath, destinationPath);
                Debug.Log($"文件移动成功: {_fullPath} -> {destinationPath}");
                return new PersistentFile(destinationPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"移动文件失败: {_fullPath} -> {destinationPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 重命名文件
        /// </summary>
        /// <param name="newName">新文件名（包含扩展名）</param>
        /// <returns>重命名后的文件对象</returns>
        public PersistentFile Rename(string newName)
        {
            var directoryPath = Path.GetDirectoryName(_fullPath);
            var newPath = Path.Combine(directoryPath, newName);
            return MoveTo(newPath);
        }

        /// <summary>
        /// 读取文件的所有文本内容
        /// </summary>
        /// <param name="encoding">文本编码，默认为UTF-8</param>
        /// <returns>文件内容</returns>
        public string ReadAllText(Encoding encoding = null)
        {
            try
            {
                encoding = encoding ?? Encoding.UTF8;
                return File.ReadAllText(_fullPath, encoding);
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取文件文本失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 读取文件的所有字节
        /// </summary>
        /// <returns>文件字节数组</returns>
        public byte[] ReadAllBytes()
        {
            try
            {
                return File.ReadAllBytes(_fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取文件字节失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 读取文件的所有行
        /// </summary>
        /// <param name="encoding">文本编码，默认为UTF-8</param>
        /// <returns>文件行数组</returns>
        public string[] ReadAllLines(Encoding encoding = null)
        {
            try
            {
                encoding = encoding ?? Encoding.UTF8;
                return File.ReadAllLines(_fullPath, encoding);
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取文件行失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 写入文本内容到文件
        /// </summary>
        /// <param name="content">要写入的内容</param>
        /// <param name="encoding">文本编码，默认为UTF-8</param>
        /// <param name="append">是否追加到文件末尾</param>
        public void WriteAllText(string content, Encoding encoding = null, bool append = false)
        {
            try
            {
                // 确保目录存在
                Directory?.Create();
                
                encoding = encoding ?? Encoding.UTF8;
                
                if (append)
                {
                    File.AppendAllText(_fullPath, content, encoding);
                }
                else
                {
                    File.WriteAllText(_fullPath, content, encoding);
                }
                
                Debug.Log($"文件写入成功: {_fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入文件文本失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 写入字节数组到文件
        /// </summary>
        /// <param name="bytes">要写入的字节数组</param>
        public void WriteAllBytes(byte[] bytes)
        {
            try
            {
                // 确保目录存在
                Directory?.Create();
                
                File.WriteAllBytes(_fullPath, bytes);
                Debug.Log($"文件字节写入成功: {_fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入文件字节失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 写入行数组到文件
        /// </summary>
        /// <param name="lines">要写入的行数组</param>
        /// <param name="encoding">文本编码，默认为UTF-8</param>
        public void WriteAllLines(string[] lines, Encoding encoding = null)
        {
            try
            {
                // 确保目录存在
                Directory?.Create();
                
                encoding = encoding ?? Encoding.UTF8;
                File.WriteAllLines(_fullPath, lines, encoding);
                Debug.Log($"文件行写入成功: {_fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入文件行失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 追加文本到文件末尾
        /// </summary>
        /// <param name="content">要追加的内容</param>
        /// <param name="encoding">文本编码，默认为UTF-8</param>
        public void AppendText(string content, Encoding encoding = null)
        {
            WriteAllText(content, encoding, true);
        }

        /// <summary>
        /// 获取文件流（用于高级操作）
        /// </summary>
        /// <param name="mode">文件模式</param>
        /// <param name="access">访问权限</param>
        /// <param name="share">共享模式</param>
        /// <returns>文件流</returns>
        public FileStream GetStream(FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read)
        {
            try
            {
                // 确保目录存在
                Directory?.Create();
                
                return new FileStream(_fullPath, mode, access, share);
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取文件流失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取格式化的文件大小字符串
        /// </summary>
        /// <returns>格式化的大小字符串</returns>
        public string GetFormattedSize()
        {
            var size = Size;
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 计算文件的MD5哈希值
        /// </summary>
        /// <returns>MD5哈希字符串</returns>
        public string GetMD5Hash()
        {
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                using (var stream = File.OpenRead(_fullPath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"计算文件MD5失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 刷新文件信息
        /// </summary>
        private void RefreshFileInfo()
        {
            if (Exists)
            {
                _fileInfo = new FileInfo(_fullPath);
            }
        }

        public override string ToString()
        {
            return $"File: {_fullPath} (Exists: {Exists}, Size: {GetFormattedSize()})"; 
        }
    }
}