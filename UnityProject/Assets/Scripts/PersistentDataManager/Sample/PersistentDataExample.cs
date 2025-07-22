using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TEngine.PersistentData;

namespace TEngine.PersistentData
{
    /// <summary>
    /// 持久化数据管理器使用示例
    /// 展示如何使用各种API接口
    /// </summary>
    public class PersistentDataExample : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private string testDirectoryName = "TestData";
        [SerializeField] private string testFileName = "test.txt";
        [SerializeField] private string testContent = "Hello, Persistent Data Manager!";

        private void Start()
        {
            if (runTestOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        private IEnumerator RunAllTests()
        {
            Debug.Log("=== 开始持久化数据管理器测试 ===");
            
            // 基础信息测试
            TestBasicInfo();
            yield return new WaitForSeconds(0.5f);
            
            // 目录操作测试
            TestDirectoryOperations();
            yield return new WaitForSeconds(0.5f);
            
            // 文件操作测试
            TestFileOperations();
            yield return new WaitForSeconds(0.5f);
            
            // 面向对象API测试
            TestObjectOrientedAPI();
            yield return new WaitForSeconds(0.5f);
            
            // 工具类测试
            TestUtilities();
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("=== 持久化数据管理器测试完成 ===");
        }

        /// <summary>
        /// 测试基础信息
        /// </summary>
        private void TestBasicInfo()
        {
            Debug.Log("--- 基础信息测试 ---");
            
            // 获取根目录路径
            Debug.Log($"持久化根目录: {PersistentDataManager.RootPath}");
            
            // 获取磁盘使用情况
            var diskUsage = PersistentFileUtils.GetDiskUsage(PersistentDataManager.RootPath);
            Debug.Log($"磁盘使用情况: {diskUsage}");
            
            // 获取根目录大小
            var rootSize = PersistentDataManager.GetTotalSize();
            Debug.Log($"持久化目录总大小: {FormatBytes(rootSize)}");
        }

        /// <summary>
        /// 测试目录操作
        /// </summary>
        private void TestDirectoryOperations()
        {
            Debug.Log("--- 目录操作测试 ---");
            
            // 创建测试目录
            var testDir = PersistentDataManager.CreateDirectory(testDirectoryName);
            Debug.Log($"创建目录: {testDir.Name}, 路径: {testDir.RelativePath}");
            
            // 创建子目录
            var subDir = testDir.GetSubDirectory("SubFolder");
            subDir.Create();
            Debug.Log($"创建子目录: {subDir.Name}");
            
            // 检查目录是否存在
            Debug.Log($"目录存在检查: {testDirectoryName} = {PersistentDataManager.DirectoryExists(testDirectoryName)}");
            
            // 列出目录内容
            var items = testDir.List();
            Debug.Log($"目录内容数量: {items.Count}");
            foreach (var item in items)
            {
                Debug.Log($"  - {item.Name} ({item.GetTypeDescription()}) - {item.GetFormattedSize()}");
            }
        }

        /// <summary>
        /// 测试文件操作
        /// </summary>
        private void TestFileOperations()
        {
            Debug.Log("--- 文件操作测试 ---");
            
            // 创建测试文件
            var testFile = PersistentDataManager.GetFile($"{testDirectoryName}/{testFileName}");
            testFile.WriteAllText(testContent);
            Debug.Log($"创建文件: {testFile.Name}, 大小: {testFile.GetFormattedSize()}");
            
            // 读取文件内容
            var content = testFile.ReadAllText();
            Debug.Log($"文件内容: {content}");
            
            // 复制文件
            var copiedFile = testFile.CopyTo(testFile.FullPath.Replace(".txt", "_copy.txt"));
            Debug.Log($"复制文件: {copiedFile.Name}");
            
            // 获取文件信息
            Debug.Log($"文件创建时间: {testFile.CreationTime}");
            Debug.Log($"文件修改时间: {testFile.LastWriteTime}");
            Debug.Log($"文件MD5: {testFile.GetMD5Hash()}");
            
            // 检查文件类型
            Debug.Log($"是否为文本文件: {PersistentFileUtils.IsTextFile(testFile.Name)}");
            Debug.Log($"MIME类型: {PersistentFileUtils.GetMimeType(testFile.Name)}");
        }

        /// <summary>
        /// 测试面向对象API
        /// </summary>
        private void TestObjectOrientedAPI()
        {
            Debug.Log("--- 面向对象API测试 ---");
            
            // 获取根目录对象
            var rootDir = PersistentDataManager.GetRootDirectory();
            Debug.Log($"根目录: {rootDir.Name}");
            
            // 使用目录对象的方法
            var testDir = rootDir.GetSubDirectory(testDirectoryName);
            if (testDir.Exists)
            {
                // 列出所有文件
                var files = testDir.GetFiles();
                Debug.Log($"目录中的文件数量: {files.Count}");
                
                foreach (var file in files)
                {
                    Debug.Log($"  文件: {file.Name} - {file.GetFormattedSize()}");
                    
                    // 演示文件对象的方法
                    Debug.Log($"    扩展名: {file.Extension}");
                    Debug.Log($"    相对路径: {file.RelativePath}");
                    Debug.Log($"    所在目录: {file.Directory.Name}");
                }
                
                // 列出所有子目录
                var subDirs = testDir.GetDirectories();
                Debug.Log($"子目录数量: {subDirs.Count}");
                
                foreach (var dir in subDirs)
                {
                    Debug.Log($"  目录: {dir.Name}");
                    Debug.Log($"    创建时间: {dir.CreationTime}");
                    Debug.Log($"    大小: {dir.GetSize()} 字节");
                }
            }
        }

        /// <summary>
        /// 测试工具类功能
        /// </summary>
        private void TestUtilities()
        {
            Debug.Log("--- 工具类测试 ---");
            
            // 测试文件名验证和清理
            var invalidFileName = "test<>file?.txt";
            var sanitizedName = PersistentFileUtils.SanitizeFileName(invalidFileName);
            Debug.Log($"文件名清理: '{invalidFileName}' -> '{sanitizedName}'");
            
            // 测试唯一文件名生成
            var testDir = PersistentDataManager.GetDirectory(testDirectoryName);
            var uniqueName = PersistentFileUtils.GenerateUniqueFileName(testDir.FullPath, testFileName);
            Debug.Log($"唯一文件名: {uniqueName}");
            
            // 测试文件搜索
            var searchResults = PersistentFileUtils.SearchFiles(testDir.FullPath, "*.txt", false);
            Debug.Log($"搜索结果数量: {searchResults.Count}");
            
            foreach (var result in searchResults)
            {
                Debug.Log($"  搜索到: {result.Name} - {result.GetFormattedSize()}");
            }
            
            // 测试文件比较
            var files = testDir.GetFiles("*.txt");
            if (files.Count >= 2)
            {
                var areEqual = PersistentFileUtils.AreFilesEqual(files[0].FullPath, files[1].FullPath);
                Debug.Log($"文件比较: {files[0].Name} vs {files[1].Name} = {areEqual}");
            }
        }

        /// <summary>
        /// 格式化字节大小
        /// </summary>
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

        #region Unity Inspector 按钮方法
        
        /// <summary>
        /// 在Inspector中显示持久化目录信息
        /// </summary>
        [ContextMenu("显示持久化目录信息")]
        public void ShowPersistentDataInfo()
        {
            TestBasicInfo();
        }
        
        /// <summary>
        /// 创建示例数据
        /// </summary>
        [ContextMenu("创建示例数据")]
        public void CreateSampleData()
        {
            // 创建示例目录结构
            var sampleDir = PersistentDataManager.CreateDirectory("SampleData");
            
            // 创建配置目录
            var configDir = sampleDir.GetSubDirectory("Config");
            configDir.Create();
            
            // 创建配置文件
            var configFile = configDir.GetFile("settings.json");
            var configData = @"{
    ""version"": ""1.0.0"",
    ""language"": ""zh-CN"",
    ""volume"": 0.8
}";
            configFile.WriteAllText(configData);
            
            // 创建日志目录
            var logDir = sampleDir.GetSubDirectory("Logs");
            logDir.Create();
            
            // 创建日志文件
            var logFile = logDir.GetFile($"game_{System.DateTime.Now:yyyyMMdd}.log");
            var logContent = $"[{System.DateTime.Now}] 游戏启动\n[{System.DateTime.Now}] 示例数据创建完成\n";
            logFile.WriteAllText(logContent);
            
            // 创建用户数据目录
            var userDir = sampleDir.GetSubDirectory("UserData");
            userDir.Create();
            
            // 创建用户配置文件
            var userFile = userDir.GetFile("player.dat");
            var userData = System.Text.Encoding.UTF8.GetBytes("Player Level: 1\nScore: 1000\nCoins: 500");
            userFile.WriteAllBytes(userData);
            
            Debug.Log("示例数据创建完成！");
            Debug.Log($"示例目录: {sampleDir.RelativePath}");
            Debug.Log($"目录大小: {sampleDir.GetSize()} 字节");
        }
        
        /// <summary>
        /// 清理测试数据
        /// </summary>
        [ContextMenu("清理测试数据")]
        public void CleanupTestData()
        {
            // 删除测试目录
            if (PersistentDataManager.DirectoryExists(testDirectoryName))
            {
                PersistentDataManager.DeleteDirectory(testDirectoryName);
                Debug.Log($"已删除测试目录: {testDirectoryName}");
            }
            
            // 删除示例数据目录
            if (PersistentDataManager.DirectoryExists("SampleData"))
            {
                PersistentDataManager.DeleteDirectory("SampleData");
                Debug.Log("已删除示例数据目录: SampleData");
            }
            
            Debug.Log("测试数据清理完成！");
        }
        
        /// <summary>
        /// 列出所有持久化数据
        /// </summary>
        [ContextMenu("列出所有持久化数据")]
        public void ListAllPersistentData()
        {
            var rootDir = PersistentDataManager.GetRootDirectory();
            var allItems = rootDir.List();
            
            Debug.Log($"持久化目录总计: {allItems.Count} 个项目");
            
            foreach (var item in allItems)
            {
                var type = item.IsDirectory ? "[目录]" : "[文件]";
                Debug.Log($"{type} {item.Name} - {item.GetFormattedSize()} - {item.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            }
        }
        
        #endregion
    }
}