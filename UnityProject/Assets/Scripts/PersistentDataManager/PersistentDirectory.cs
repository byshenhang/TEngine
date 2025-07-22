using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TEngine.PersistentData
{
    /// <summary>
    /// 持久化目录对象
    /// 提供面向对象的目录操作接口
    /// </summary>
    public class PersistentDirectory
    {
        private readonly string _fullPath;
        private DirectoryInfo _directoryInfo;

        /// <summary>
        /// 目录的完整路径
        /// </summary>
        public string FullPath => _fullPath;

        /// <summary>
        /// 目录名称
        /// </summary>
        public string Name => Path.GetFileName(_fullPath);

        /// <summary>
        /// 相对于持久化根目录的路径
        /// </summary>
        public string RelativePath => Path.GetRelativePath(Application.persistentDataPath, _fullPath);

        /// <summary>
        /// 目录是否存在
        /// </summary>
        public bool Exists => Directory.Exists(_fullPath);

        /// <summary>
        /// 父目录
        /// </summary>
        public PersistentDirectory Parent
        {
            get
            {
                var parentPath = Path.GetDirectoryName(_fullPath);
                return parentPath != null ? new PersistentDirectory(parentPath) : null;
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime
        {
            get
            {
                RefreshDirectoryInfo();
                return _directoryInfo?.CreationTime ?? DateTime.MinValue;
            }
        }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastWriteTime
        {
            get
            {
                RefreshDirectoryInfo();
                return _directoryInfo?.LastWriteTime ?? DateTime.MinValue;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fullPath">目录的完整路径</param>
        public PersistentDirectory(string fullPath)
        {
            _fullPath = Path.GetFullPath(fullPath);
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <returns>当前目录对象</returns>
        public PersistentDirectory Create()
        {
            try
            {
                if (!Exists)
                {
                    Directory.CreateDirectory(_fullPath);
                    Debug.Log($"目录创建成功: {_fullPath}");
                }
                return this;
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建目录失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="recursive">是否递归删除子目录和文件</param>
        public void Delete(bool recursive = true)
        {
            try
            {
                if (Exists)
                {
                    Directory.Delete(_fullPath, recursive);
                    Debug.Log($"目录删除成功: {_fullPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除目录失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取目录中的所有文件
        /// </summary>
        /// <param name="searchPattern">搜索模式，默认为所有文件</param>
        /// <param name="recursive">是否递归搜索子目录</param>
        /// <returns>文件对象列表</returns>
        public List<PersistentFile> GetFiles(string searchPattern = "*", bool recursive = false)
        {
            var files = new List<PersistentFile>();
            
            if (!Exists) return files;

            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var filePaths = Directory.GetFiles(_fullPath, searchPattern, searchOption);
                
                files.AddRange(filePaths.Select(path => new PersistentFile(path)));
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取文件列表失败: {_fullPath}, 错误: {ex.Message}");
            }

            return files;
        }

        /// <summary>
        /// 获取目录中的所有子目录
        /// </summary>
        /// <param name="searchPattern">搜索模式，默认为所有目录</param>
        /// <param name="recursive">是否递归搜索</param>
        /// <returns>目录对象列表</returns>
        public List<PersistentDirectory> GetDirectories(string searchPattern = "*", bool recursive = false)
        {
            var directories = new List<PersistentDirectory>();
            
            if (!Exists) return directories;

            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var directoryPaths = Directory.GetDirectories(_fullPath, searchPattern, searchOption);
                
                directories.AddRange(directoryPaths.Select(path => new PersistentDirectory(path)));
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取目录列表失败: {_fullPath}, 错误: {ex.Message}");
            }

            return directories;
        }

        /// <summary>
        /// 获取目录中的所有项目（文件和子目录）
        /// </summary>
        /// <returns>包含文件和目录信息的列表</returns>
        public List<PersistentItem> List()
        {
            var items = new List<PersistentItem>();
            
            if (!Exists) return items;

            try
            {
                // 添加子目录
                var directories = GetDirectories();
                items.AddRange(directories.Select(dir => new PersistentItem
                {
                    Name = dir.Name,
                    FullPath = dir.FullPath,
                    IsDirectory = true,
                    Size = 0,
                    CreationTime = dir.CreationTime,
                    LastWriteTime = dir.LastWriteTime
                }));

                // 添加文件
                var files = GetFiles();
                items.AddRange(files.Select(file => new PersistentItem
                {
                    Name = file.Name,
                    FullPath = file.FullPath,
                    IsDirectory = false,
                    Size = file.Size,
                    CreationTime = file.CreationTime,
                    LastWriteTime = file.LastWriteTime
                }));
            }
            catch (Exception ex)
            {
                Debug.LogError($"列出目录内容失败: {_fullPath}, 错误: {ex.Message}");
            }

            return items;
        }

        /// <summary>
        /// 获取子目录
        /// </summary>
        /// <param name="name">子目录名称</param>
        /// <returns>子目录对象</returns>
        public PersistentDirectory GetSubDirectory(string name)
        {
            return new PersistentDirectory(Path.Combine(_fullPath, name));
        }

        /// <summary>
        /// 获取文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件对象</returns>
        public PersistentFile GetFile(string fileName)
        {
            return new PersistentFile(Path.Combine(_fullPath, fileName));
        }

        /// <summary>
        /// 复制目录到指定位置
        /// </summary>
        /// <param name="destinationPath">目标路径</param>
        /// <param name="recursive">是否递归复制</param>
        /// <returns>目标目录对象</returns>
        public PersistentDirectory CopyTo(string destinationPath, bool recursive = true)
        {
            try
            {
                var destination = new PersistentDirectory(destinationPath);
                destination.Create();

                if (recursive)
                {
                    CopyDirectoryRecursive(_fullPath, destinationPath);
                }
                else
                {
                    // 只复制当前目录的文件
                    var files = GetFiles();
                    foreach (var file in files)
                    {
                        var destFilePath = Path.Combine(destinationPath, file.Name);
                        file.CopyTo(destFilePath);
                    }
                }

                Debug.Log($"目录复制成功: {_fullPath} -> {destinationPath}");
                return destination;
            }
            catch (Exception ex)
            {
                Debug.LogError($"复制目录失败: {_fullPath} -> {destinationPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 移动目录到指定位置
        /// </summary>
        /// <param name="destinationPath">目标路径</param>
        /// <returns>目标目录对象</returns>
        public PersistentDirectory MoveTo(string destinationPath)
        {
            try
            {
                Directory.Move(_fullPath, destinationPath);
                Debug.Log($"目录移动成功: {_fullPath} -> {destinationPath}");
                return new PersistentDirectory(destinationPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"移动目录失败: {_fullPath} -> {destinationPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 重命名目录
        /// </summary>
        /// <param name="newName">新名称</param>
        /// <returns>重命名后的目录对象</returns>
        public PersistentDirectory Rename(string newName)
        {
            var parentPath = Path.GetDirectoryName(_fullPath);
            var newPath = Path.Combine(parentPath, newName);
            return MoveTo(newPath);
        }

        /// <summary>
        /// 获取目录大小（包含所有子文件和子目录）
        /// </summary>
        /// <returns>目录大小（字节）</returns>
        public long GetSize()
        {
            if (!Exists) return 0;

            try
            {
                var files = GetFiles("*", true);
                return files.Sum(file => file.Size);
            }
            catch (Exception ex)
            {
                Debug.LogError($"计算目录大小失败: {_fullPath}, 错误: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 清空目录（删除所有内容但保留目录本身）
        /// </summary>
        public void Clear()
        {
            if (!Exists) return;

            try
            {
                // 删除所有文件
                var files = GetFiles();
                foreach (var file in files)
                {
                    file.Delete();
                }

                // 删除所有子目录
                var directories = GetDirectories();
                foreach (var directory in directories)
                {
                    directory.Delete(true);
                }

                Debug.Log($"目录清空成功: {_fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"清空目录失败: {_fullPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 刷新目录信息
        /// </summary>
        private void RefreshDirectoryInfo()
        {
            if (Exists)
            {
                _directoryInfo = new DirectoryInfo(_fullPath);
            }
        }

        /// <summary>
        /// 递归复制目录
        /// </summary>
        /// <param name="sourcePath">源路径</param>
        /// <param name="destinationPath">目标路径</param>
        private void CopyDirectoryRecursive(string sourcePath, string destinationPath)
        {
            var sourceDir = new DirectoryInfo(sourcePath);
            var destDir = new DirectoryInfo(destinationPath);

            if (!destDir.Exists)
            {
                destDir.Create();
            }

            // 复制文件
            foreach (var file in sourceDir.GetFiles())
            {
                var destFilePath = Path.Combine(destinationPath, file.Name);
                file.CopyTo(destFilePath, true);
            }

            // 递归复制子目录
            foreach (var subDir in sourceDir.GetDirectories())
            {
                var destSubDirPath = Path.Combine(destinationPath, subDir.Name);
                CopyDirectoryRecursive(subDir.FullName, destSubDirPath);
            }
        }

        public override string ToString()
        {
            return $"Directory: {_fullPath} (Exists: {Exists})"; 
        }
    }
}