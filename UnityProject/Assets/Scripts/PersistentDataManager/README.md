# Unity 持久化目录管理插件

## 概述

这是一个为Unity开发的持久化目录管理插件，提供了对`Application.persistentDataPath`的完整管理功能。插件采用面向对象的设计，提供了直观易用的API接口，支持文件和目录的各种操作。

## 特性

- 🗂️ **面向对象设计** - 提供目录对象和文件对象，支持链式调用
- 📁 **完整的目录操作** - 创建、删除、复制、移动、重命名目录
- 📄 **丰富的文件操作** - 读写文件、复制、移动、重命名、获取文件信息
- 🔍 **文件搜索功能** - 支持模式匹配和递归搜索
- 🛠️ **实用工具类** - 文件名验证、MIME类型检测、磁盘使用情况等
- 📊 **详细的文件信息** - 大小、创建时间、修改时间、MD5哈希等
- 🔒 **安全性** - 路径验证、错误处理、日志记录

## 快速开始

### 1. 基础使用

```csharp
using TEngine.PersistentData;

// 获取持久化根目录
string rootPath = PersistentDataManager.RootPath;

// 创建目录
var myDir = PersistentDataManager.CreateDirectory("MyData");

// 创建文件
var myFile = PersistentDataManager.GetFile("MyData/config.txt");
myFile.WriteAllText("Hello World!");

// 读取文件
string content = myFile.ReadAllText();
```

### 2. 面向对象API

```csharp
// 获取目录对象
var directory = PersistentDataManager.GetDirectory("GameData");
directory.Create();

// 列出目录内容
var items = directory.List();
foreach (var item in items)
{
    Debug.Log($"{item.Name} - {item.GetFormattedSize()}");
}

// 获取子目录
var subDir = directory.GetSubDirectory("Saves");
subDir.Create();

// 获取文件
var saveFile = subDir.GetFile("save001.dat");
saveFile.WriteAllBytes(gameData);
```

## API 参考

### PersistentDataManager (静态管理器)

#### 基础方法
- `GetRootDirectory()` - 获取根目录对象
- `GetDirectory(string relativePath)` - 获取目录对象
- `GetFile(string relativePath)` - 获取文件对象
- `CreateDirectory(string relativePath)` - 创建目录

#### 检查方法
- `DirectoryExists(string relativePath)` - 检查目录是否存在
- `FileExists(string relativePath)` - 检查文件是否存在

#### 删除方法
- `DeleteDirectory(string relativePath, bool recursive = true)` - 删除目录
- `DeleteFile(string relativePath)` - 删除文件

#### 信息方法
- `GetTotalSize()` - 获取持久化目录总大小
- `ClearAll()` - 清空持久化目录

### PersistentDirectory (目录对象)

#### 属性
- `FullPath` - 完整路径
- `Name` - 目录名称
- `RelativePath` - 相对路径
- `Exists` - 是否存在
- `Parent` - 父目录
- `CreationTime` - 创建时间
- `LastWriteTime` - 最后修改时间

#### 操作方法
- `Create()` - 创建目录
- `Delete(bool recursive = true)` - 删除目录
- `CopyTo(string destinationPath, bool recursive = true)` - 复制目录
- `MoveTo(string destinationPath)` - 移动目录
- `Rename(string newName)` - 重命名目录
- `Clear()` - 清空目录内容

#### 查询方法
- `List()` - 列出所有内容
- `GetFiles(string searchPattern = "*", bool recursive = false)` - 获取文件列表
- `GetDirectories(string searchPattern = "*", bool recursive = false)` - 获取子目录列表
- `GetSubDirectory(string name)` - 获取子目录对象
- `GetFile(string fileName)` - 获取文件对象
- `GetSize()` - 获取目录大小

### PersistentFile (文件对象)

#### 属性
- `FullPath` - 完整路径
- `Name` - 文件名（含扩展名）
- `NameWithoutExtension` - 文件名（不含扩展名）
- `Extension` - 扩展名
- `RelativePath` - 相对路径
- `Exists` - 是否存在
- `Size` - 文件大小
- `Directory` - 所在目录
- `CreationTime` - 创建时间
- `LastWriteTime` - 最后修改时间
- `LastAccessTime` - 最后访问时间
- `IsReadOnly` - 是否只读

#### 操作方法
- `Create()` - 创建空文件
- `Delete()` - 删除文件
- `CopyTo(string destinationPath, bool overwrite = true)` - 复制文件
- `MoveTo(string destinationPath, bool overwrite = true)` - 移动文件
- `Rename(string newName)` - 重命名文件

#### 读取方法
- `ReadAllText(Encoding encoding = null)` - 读取所有文本
- `ReadAllBytes()` - 读取所有字节
- `ReadAllLines(Encoding encoding = null)` - 读取所有行

#### 写入方法
- `WriteAllText(string content, Encoding encoding = null, bool append = false)` - 写入文本
- `WriteAllBytes(byte[] bytes)` - 写入字节
- `WriteAllLines(string[] lines, Encoding encoding = null)` - 写入行
- `AppendText(string content, Encoding encoding = null)` - 追加文本

#### 高级方法
- `GetStream(FileMode mode, FileAccess access, FileShare share)` - 获取文件流
- `GetFormattedSize()` - 获取格式化大小字符串
- `GetMD5Hash()` - 计算MD5哈希值

### PersistentFileUtils (工具类)

#### 验证方法
- `IsValidFileName(string fileName)` - 验证文件名
- `IsValidPath(string path)` - 验证路径
- `SanitizeFileName(string fileName, char replacement = '_')` - 清理文件名

#### 文件类型检测
- `IsTextFile(string fileName)` - 是否为文本文件
- `IsImageFile(string fileName)` - 是否为图片文件
- `IsAudioFile(string fileName)` - 是否为音频文件
- `IsVideoFile(string fileName)` - 是否为视频文件
- `GetMimeType(string fileName)` - 获取MIME类型

#### 高级功能
- `GenerateUniqueFileName(string directory, string fileName)` - 生成唯一文件名
- `AreFilesEqual(string file1Path, string file2Path)` - 比较文件是否相同
- `SearchFiles(string directory, string searchPattern, bool recursive, bool includeDirectories)` - 搜索文件
- `BatchDeleteFiles(IEnumerable<string> filePaths)` - 批量删除文件
- `GetDiskUsage(string directoryPath)` - 获取磁盘使用情况

## 使用示例

### 游戏存档管理

```csharp
// 创建存档目录
var saveDir = PersistentDataManager.CreateDirectory("GameSaves");

// 保存游戏数据
var saveData = JsonUtility.ToJson(playerData);
var saveFile = saveDir.GetFile($"save_{DateTime.Now:yyyyMMdd_HHmmss}.json");
saveFile.WriteAllText(saveData);

// 列出所有存档
var saveFiles = saveDir.GetFiles("*.json");
foreach (var file in saveFiles)
{
    Debug.Log($"存档: {file.NameWithoutExtension} - {file.LastWriteTime}");
}

// 加载最新存档
var latestSave = saveFiles.OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
if (latestSave != null)
{
    var loadedData = latestSave.ReadAllText();
    playerData = JsonUtility.FromJson<PlayerData>(loadedData);
}
```

### 配置文件管理

```csharp
// 创建配置目录
var configDir = PersistentDataManager.CreateDirectory("Config");

// 保存设置
var settingsFile = configDir.GetFile("settings.json");
var settings = new GameSettings { volume = 0.8f, language = "zh-CN" };
settingsFile.WriteAllText(JsonUtility.ToJson(settings, true));

// 读取设置
if (settingsFile.Exists)
{
    var settingsJson = settingsFile.ReadAllText();
    settings = JsonUtility.FromJson<GameSettings>(settingsJson);
}
```

### 日志文件管理

```csharp
// 创建日志目录
var logDir = PersistentDataManager.CreateDirectory("Logs");

// 创建日志文件
var logFile = logDir.GetFile($"game_{DateTime.Now:yyyyMMdd}.log");

// 写入日志
var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}\n";
logFile.AppendText(logEntry);

// 清理旧日志（保留最近7天）
var oldLogs = logDir.GetFiles("*.log")
    .Where(f => f.CreationTime < DateTime.Now.AddDays(-7))
    .ToList();

foreach (var oldLog in oldLogs)
{
    oldLog.Delete();
}
```

### 缓存管理

```csharp
// 创建缓存目录
var cacheDir = PersistentDataManager.CreateDirectory("Cache");

// 缓存图片
var imageCache = cacheDir.GetSubDirectory("Images");
imageCache.Create();

// 保存缓存文件
var cacheFile = imageCache.GetFile($"{imageId}.png");
cacheFile.WriteAllBytes(imageData);

// 检查缓存大小，清理超出限制的文件
var cacheSize = cacheDir.GetSize();
if (cacheSize > maxCacheSize)
{
    var files = cacheDir.GetFiles("*", true)
        .OrderBy(f => f.LastAccessTime)
        .ToList();
    
    foreach (var file in files)
    {
        file.Delete();
        cacheSize -= file.Size;
        if (cacheSize <= targetCacheSize) break;
    }
}
```

## 注意事项

1. **路径安全性**: 所有路径操作都会进行验证，确保不会访问持久化目录之外的文件
2. **异常处理**: 所有文件操作都包含异常处理，失败时会记录错误日志
3. **性能考虑**: 大文件操作建议使用流式API，避免一次性加载到内存
4. **线程安全**: 当前版本不是线程安全的，多线程环境下需要额外的同步机制
5. **平台兼容性**: 支持所有Unity支持的平台，路径分隔符会自动处理

## 许可证

本插件遵循MIT许可证，可自由使用和修改。