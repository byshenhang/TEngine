using System;

namespace TEngine.PersistentData
{
    /// <summary>
    /// 持久化项目信息
    /// 用于统一表示文件和目录的基本信息
    /// </summary>
    [Serializable]
    public class PersistentItem
    {
        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 完整路径
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// 是否为目录
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// 是否为文件
        /// </summary>
        public bool IsFile => !IsDirectory;

        /// <summary>
        /// 大小（字节）
        /// 对于目录，此值可能为0或目录总大小
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        /// <summary>
        /// 文件扩展名（仅对文件有效）
        /// </summary>
        public string Extension
        {
            get
            {
                if (IsFile && !string.IsNullOrEmpty(Name))
                {
                    var lastDotIndex = Name.LastIndexOf('.');
                    return lastDotIndex >= 0 ? Name.Substring(lastDotIndex) : string.Empty;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取格式化的文件大小字符串
        /// </summary>
        /// <returns>格式化的大小字符串</returns>
        public string GetFormattedSize()
        {
            if (IsDirectory && Size == 0)
            {
                return "--";
            }

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = Size;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 获取项目类型描述
        /// </summary>
        /// <returns>类型描述字符串</returns>
        public string GetTypeDescription()
        {
            if (IsDirectory)
            {
                return "文件夹";
            }
            
            if (string.IsNullOrEmpty(Extension))
            {
                return "文件";
            }
            
            return $"{Extension.ToUpper().TrimStart('.')} 文件";
        }

        /// <summary>
        /// 转换为目录对象
        /// </summary>
        /// <returns>目录对象，如果不是目录则返回null</returns>
        public PersistentDirectory ToDirectory()
        {
            return IsDirectory ? new PersistentDirectory(FullPath) : null;
        }

        /// <summary>
        /// 转换为文件对象
        /// </summary>
        /// <returns>文件对象，如果不是文件则返回null</returns>
        public PersistentFile ToFile()
        {
            return IsFile ? new PersistentFile(FullPath) : null;
        }

        public override string ToString()
        {
            var type = IsDirectory ? "Directory" : "File";
            return $"{type}: {Name} ({GetFormattedSize()})";
        }

        public override bool Equals(object obj)
        {
            if (obj is PersistentItem other)
            {
                return string.Equals(FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return FullPath?.ToLowerInvariant().GetHashCode() ?? 0;
        }
    }
}