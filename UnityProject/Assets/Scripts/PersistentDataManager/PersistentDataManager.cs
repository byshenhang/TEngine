using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TEngine.PersistentData
{
    /// <summary>
    /// Unity持久化目录管理器
    /// 提供对Application.persistentDataPath的完整管理功能
    /// </summary>
    public static class PersistentDataManager
    {
        /// <summary>
        /// 获取持久化数据根目录路径
        /// </summary>
        public static string RootPath => Application.persistentDataPath;

        /// <summary>
        /// 获取根目录对象
        /// </summary>
        /// <returns>根目录对象</returns>
        public static PersistentDirectory GetRootDirectory()
        {
            return new PersistentDirectory(RootPath);
        }

        /// <summary>
        /// 获取指定路径的目录对象
        /// </summary>
        /// <param name="relativePath">相对于持久化根目录的路径</param>
        /// <returns>目录对象</returns>
        public static PersistentDirectory GetDirectory(string relativePath)
        {
            string fullPath = Path.Combine(RootPath, relativePath);
            return new PersistentDirectory(fullPath);
        }

        /// <summary>
        /// 获取指定路径的文件对象
        /// </summary>
        /// <param name="relativePath">相对于持久化根目录的文件路径</param>
        /// <returns>文件对象</returns>
        public static PersistentFile GetFile(string relativePath)
        {
            string fullPath = Path.Combine(RootPath, relativePath);
            return new PersistentFile(fullPath);
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <returns>创建的目录对象</returns>
        public static PersistentDirectory CreateDirectory(string relativePath)
        {
            var directory = GetDirectory(relativePath);
            directory.Create();
            return directory;
        }

        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <returns>是否存在</returns>
        public static bool DirectoryExists(string relativePath)
        {
            return GetDirectory(relativePath).Exists;
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <returns>是否存在</returns>
        public static bool FileExists(string relativePath)
        {
            return GetFile(relativePath).Exists;
        }

        /// <summary>
        /// 删除目录及其所有内容
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <param name="recursive">是否递归删除</param>
        public static void DeleteDirectory(string relativePath, bool recursive = true)
        {
            GetDirectory(relativePath).Delete(recursive);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        public static void DeleteFile(string relativePath)
        {
            GetFile(relativePath).Delete();
        }

        /// <summary>
        /// 获取持久化目录的总大小（字节）
        /// </summary>
        /// <returns>目录大小</returns>
        public static long GetTotalSize()
        {
            return GetRootDirectory().GetSize();
        }

        /// <summary>
        /// 清空持久化目录
        /// </summary>
        public static void ClearAll()
        {
            GetRootDirectory().Clear();
        }
    }
}