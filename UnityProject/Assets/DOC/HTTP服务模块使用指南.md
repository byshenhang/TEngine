# HTTP服务模块使用指南

## 概述

HTTP服务模块是基于TEngine框架开发的网络服务组件，它封装了`RuntimeHTTPServer`的功能，提供了统一的HTTP服务管理接口。该模块支持远程文件上传、下载、删除等功能，并可通过UI界面进行可视化控制。

## 主要特性

### 🚀 核心功能
- **HTTP服务器管理**：启动、停止、重启HTTP服务器
- **文件传输**：支持文件上传、下载、删除操作
- **状态监控**：实时监控服务器运行状态
- **事件系统**：提供丰富的事件回调
- **配置管理**：支持动态配置服务器参数

### 🎯 技术特点
- **模块化设计**：基于TEngine模块系统，易于集成和扩展
- **单例模式**：全局唯一实例，便于访问和管理
- **事件驱动**：通过事件系统实现松耦合的组件通信
- **UI集成**：提供完整的UI控制界面
- **跨平台**：支持Windows、Mac、Linux等平台

## 模块架构

```
HTTPServerModule (核心模块)
├── RuntimeHTTPServer (底层HTTP服务器)
├── HTTPServerConfig (配置数据)
├── HTTPServerStatus (状态数据)
├── HTTPServerControlUI (控制界面)
└── HTTPServerExample (使用示例)
```

## 快速开始

### 1. 模块注册

模块已自动注册到`GameModule`中，可通过以下方式访问：

```csharp
// 获取HTTP服务模块实例
var httpModule = GameModule.HTTPServer;
```

### 2. 基本使用

```csharp
using TEngine;
using UnityEngine;

public class HTTPServerDemo : MonoBehaviour
{
    private void Start()
    {
        // 获取模块实例
        var httpModule = GameModule.HTTPServer;
        
        // 订阅事件
        httpModule.OnServerStatusChanged += OnStatusChanged;
        httpModule.OnFileUploaded += OnFileUploaded;
        httpModule.OnError += OnError;
        
        // 启动服务器
        httpModule.StartServer();
    }
    
    private void OnStatusChanged(bool isRunning)
    {
        Debug.Log($"服务器状态: {(isRunning ? "运行中" : "已停止")}");
    }
    
    private void OnFileUploaded(string fileName, int fileCount)
    {
        Debug.Log($"文件上传: {fileName}");
    }
    
    private void OnError(string error)
    {
        Debug.LogError($"服务器错误: {error}");
    }
}
```

### 3. 配置服务器

```csharp
// 创建配置
var config = new HTTPServerConfig
{
    port = 8080,
    autoStart = true,
    enableCORS = true
};

// 更新配置
httpModule.UpdateConfig(config);
```

## API参考

### HTTPServerModule 主要方法

| 方法 | 描述 | 返回值 |
|------|------|--------|
| `StartServer()` | 启动HTTP服务器 | `bool` |
| `StopServer()` | 停止HTTP服务器 | `void` |
| `RestartServer()` | 重启HTTP服务器 | `bool` |
| `UpdateConfig(config)` | 更新服务器配置 | `bool` |
| `GetServerStatus()` | 获取服务器状态 | `HTTPServerStatus` |

### HTTPServerModule 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `IsServerRunning` | `bool` | 服务器是否运行中 |
| `ServerPort` | `int` | 服务器端口号 |
| `ServerURL` | `string` | 服务器本地URL |
| `UploadPath` | `string` | 文件上传路径 |

### 事件系统

| 事件 | 参数 | 描述 |
|------|------|------|
| `OnServerStatusChanged` | `bool isRunning` | 服务器状态改变 |
| `OnFileUploaded` | `string fileName, int fileCount` | 文件上传完成 |
| `OnError` | `string errorMessage` | 服务器错误 |

### 配置结构

```csharp
public class HTTPServerConfig
{
    public int port = 8080;           // 服务器端口
    public bool autoStart = false;    // 是否自动启动
    public bool enableCORS = true;    // 是否启用CORS
}
```

### 状态结构

```csharp
public class HTTPServerStatus
{
    public bool isRunning;        // 是否运行中
    public int port;              // 端口号
    public string localURL;       // 本地URL
    public string lanURL;         // 局域网URL
    public string uploadPath;     // 上传路径
    public float uptime;          // 运行时间(秒)
}
```

## UI控制界面

### HTTPServerControlUI 功能

- **服务器控制**：启动/停止/重启按钮
- **状态显示**：实时显示服务器运行状态
- **配置管理**：端口设置、自动启动等选项
- **操作日志**：显示服务器操作历史
- **文件管理**：查看上传文件列表

### 使用UI界面

```csharp
// 打开HTTP服务器控制界面
GameModule.UI.OpenWindow<HTTPServerControlUI>();

// 关闭界面
GameModule.UI.CloseWindow<HTTPServerControlUI>();
```

## 文件操作

### 支持的文件类型

当前版本主要支持以下文件类型：
- **音频文件**：`.mp3`, `.wav`, `.ogg`
- **歌词文件**：`.lrc`, `.txt`
- **其他文件**：可通过配置扩展

### 文件上传流程

1. 客户端通过Web界面选择文件
2. 文件上传到服务器指定目录
3. 服务器验证文件类型和大小
4. 触发`OnFileUploaded`事件
5. 应用程序处理上传的文件

### 访问上传界面

在浏览器中访问：`http://localhost:8080/` 或 `http://[局域网IP]:8080/`

## 最佳实践

### 1. 错误处理

```csharp
private void HandleServerError(string error)
{
    // 记录错误日志
    Log.Error($"HTTP服务器错误: {error}");
    
    // 尝试重启服务器
    if (error.Contains("端口被占用"))
    {
        // 更换端口重试
        var config = new HTTPServerConfig { port = 8081 };
        httpModule.UpdateConfig(config);
        httpModule.StartServer();
    }
}
```

### 2. 生命周期管理

```csharp
public class GameManager : MonoBehaviour
{
    private void Start()
    {
        // 游戏启动时初始化HTTP服务
        var httpModule = GameModule.HTTPServer;
        httpModule.Initialize();
        
        if (httpModule.Config.autoStart)
        {
            httpModule.StartServer();
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // 应用暂停时停止服务器
        if (pauseStatus)
        {
            GameModule.HTTPServer.StopServer();
        }
    }
    
    private void OnApplicationQuit()
    {
        // 应用退出时清理资源
        GameModule.HTTPServer.Release();
    }
}
```

### 3. 安全考虑

```csharp
// 限制文件上传大小
var config = new HTTPServerConfig
{
    port = 8080,
    maxFileSize = 50 * 1024 * 1024, // 50MB
    allowedExtensions = new[] { ".mp3", ".wav", ".lrc" }
};

// 启用CORS但限制来源
config.enableCORS = true;
config.allowedOrigins = new[] { "localhost", "127.0.0.1" };
```

## 故障排除

### 常见问题

**Q: 服务器启动失败**
- 检查端口是否被占用
- 确认防火墙设置
- 验证权限配置

**Q: 无法访问Web界面**
- 确认服务器已启动
- 检查IP地址和端口
- 验证网络连接

**Q: 文件上传失败**
- 检查文件大小限制
- 验证文件类型支持
- 确认磁盘空间充足

### 调试技巧

```csharp
// 启用详细日志
Log.SetLogLevel(LogLevel.Debug);

// 监控服务器状态
InvokeRepeating(nameof(CheckServerStatus), 1f, 5f);

private void CheckServerStatus()
{
    var status = GameModule.HTTPServer.GetServerStatus();
    Debug.Log($"服务器状态: {status.isRunning}, 运行时间: {status.uptime}s");
}
```

## 扩展开发

### 自定义文件处理器

```csharp
public class CustomFileHandler
{
    public void HandleUploadedFile(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLower();
        
        switch (extension)
        {
            case ".json":
                ProcessJsonFile(fileName);
                break;
            case ".xml":
                ProcessXmlFile(fileName);
                break;
            default:
                Log.Warning($"未支持的文件类型: {extension}");
                break;
        }
    }
}
```

### 扩展UI界面

```csharp
public class ExtendedHTTPServerUI : HTTPServerControlUI
{
    [Header("扩展功能")]
    public Button advancedSettingsButton;
    public Toggle securityModeToggle;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        
        // 添加自定义功能
        advancedSettingsButton.onClick.AddListener(OpenAdvancedSettings);
        securityModeToggle.onValueChanged.AddListener(OnSecurityModeChanged);
    }
}
```

## 版本历史

- **v1.0.0** - 初始版本，基础HTTP服务功能
- **v1.1.0** - 添加UI控制界面
- **v1.2.0** - 增强错误处理和日志系统
- **v1.3.0** - 支持配置管理和事件系统

## 技术支持

如果在使用过程中遇到问题，请：

1. 查看控制台日志输出
2. 检查网络和防火墙设置
3. 参考示例代码和最佳实践
4. 联系技术支持团队

---

*本文档基于TEngine框架HTTP服务模块v1.3.0编写*