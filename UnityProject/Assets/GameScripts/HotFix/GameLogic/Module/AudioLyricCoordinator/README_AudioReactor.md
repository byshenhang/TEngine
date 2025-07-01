# 音频反应器架构重构说明

## 概述

本次重构将原有的音频反应器管理系统进行了统一化改造，引入了 `AudioReactorManager` 作为统一的音频反应器管理器，支持多种音频反应插件的统一管理和控制。

## 架构图

```
 ┌─────────────────────────────────────┐ 
 │     AudioLyricCoordinator           │ 
 │         (协调器)                    │ 
 ├─────────────────────────────────────┤ 
 │  • 统一音频源管理                   │ 
 │  • 歌词同步协调                     │ 
 └─────────────┬───────────────────────┘ 
               │ 
               ▼ 
 ┌─────────────────────────────────────┐ 
 │    AudioReactorManager              │ 
 │      (音频反应器管理器)              │ 
 ├─────────────────────────────────────┤ 
 │  • 插件注册与发现                   │ 
 │  • 统一开启/关闭控制                │ 
 │  • 音频源分发                       │ 
 └─────────────┬───────────────────────┘ 
               │ 
     ┌─────────┴─────────┐ 
     │                   │ 
     ▼                   ▼ 
 ┌─────────────┐    ┌─────────────┐ 
 │AudioSync    │    │AudioReactive│ 
 │Pro Plugin   │    │Shaders      │ 
 │             │    │Plugin       │ 
 └─────────────┘    └─────────────┘ 
```

## 核心组件

### 1. IAudioReactor 接口

统一的音频反应器接口，为不同的音频反应插件提供统一的控制接口。

**主要属性：**
- `ReactorId`: 反应器唯一标识
- `DisplayName`: 显示名称
- `ReactorType`: 反应器类型
- `IsEnabled`: 是否启用
- `IsInitialized`: 是否已初始化
- `CurrentAudioSource`: 当前音频源

**主要方法：**
- `InitializeAsync()`: 异步初始化
- `EnableAsync()`: 异步启用
- `DisableAsync()`: 异步禁用
- `SetAudioSourceAsync()`: 设置音频源
- `GetAudioDataAsync()`: 获取音频数据
- `SetParameterAsync()`: 设置参数
- `GetParameterAsync()`: 获取参数
- `ReleaseAsync()`: 释放资源

### 2. AudioReactorManager

音频反应器管理器，负责统一管理多个音频反应插件。

**主要功能：**
- 自动发现场景中的音频反应器
- 注册和注销音频反应器
- 统一控制音频反应器的启用/禁用
- 全局音频源分发
- 事件通知机制

**使用示例：**
```csharp
// 获取管理器实例
var manager = AudioReactorManager.Instance;

// 启用调试模式
manager.EnableDebugger(true);

// 启用自动发现
manager.SetAutoDiscovery(true);

// 设置全局音频源
await manager.SetGlobalAudioSourceAsync(audioSource);

// 启用所有反应器
await manager.EnableAllReactorsAsync();
```

### 3. AudioLyricCoordinator (重构后)

音频歌词协调器，集成了新的音频反应器管理系统。

**新增方法：**
- `DiscoverAudioReactorsAsync()`: 异步发现音频反应器
- `GetRegisteredAudioReactors()`: 获取已注册的音频反应器
- `AutoInitializeAsync()`: 自动初始化
- `SetGlobalAudioSourceAsync()`: 设置全局音频源
- `EnableAllAudioReactorsAsync()`: 启用所有音频反应器
- `DisableAllAudioReactorsAsync()`: 禁用所有音频反应器
- `EnableAudioReactorAsync()`: 启用指定音频反应器
- `DisableAudioReactorAsync()`: 禁用指定音频反应器

**使用示例：**
```csharp
// 获取协调器实例
var coordinator = AudioLyricCoordinator.Instance;

// 启用调试模式
coordinator.EnableDebugger(true);

// 自动初始化（发现并启用所有反应器）
bool success = await coordinator.AutoInitializeAsync(audioSource);

// 播放音频和歌词
if (success)
{
    await coordinator.PlayWithSync(audioClip, lrcContent, Vector3.zero);
}
```

### 4. 适配器类

#### AudioReactiveShadersAdapter
将 `AudioReactiveShaders` 插件包装成统一的 `IAudioReactor` 接口。

#### AudioSyncProAdapter
将 `AudioSync Pro` 插件包装成统一的 `IAudioReactor` 接口。

## 使用指南

### 基本使用流程

1. **初始化系统**
```csharp
// 获取协调器实例
var coordinator = AudioLyricCoordinator.Instance;
coordinator.EnableDebugger(true);

// 自动初始化（推荐）
bool initSuccess = await coordinator.AutoInitializeAsync(audioSource);
```

2. **手动控制反应器**
```csharp
// 发现音频反应器
await coordinator.DiscoverAudioReactorsAsync();

// 获取已注册的反应器
var reactors = coordinator.GetRegisteredAudioReactors();

// 设置全局音频源
await coordinator.SetGlobalAudioSourceAsync(audioSource);

// 启用所有反应器
await coordinator.EnableAllAudioReactorsAsync();
```

3. **播放音频和歌词**
```csharp
// 同步播放音频和歌词
bool playSuccess = await coordinator.PlayWithSync(
    audioClip, 
    lrcContent, 
    Vector3.zero,
    effectId: "default",
    layoutId: "center"
);
```

4. **切换歌曲**
```csharp
// 快速切换到新歌曲
bool switchSuccess = await coordinator.SwitchSong(
    newAudioClip,
    newLrcContent,
    Vector3.zero
);
```

### 高级用法

#### 单独控制特定反应器
```csharp
// 启用特定反应器
await coordinator.EnableAudioReactorAsync("AudioReactiveShaders_12345");

// 禁用特定反应器
await coordinator.DisableAudioReactorAsync("AudioSyncPro_67890");
```

#### 监听事件
```csharp
// 订阅播放事件
coordinator.OnPlaybackStarted += () => Debug.Log("播放开始");
coordinator.OnPlaybackStopped += () => Debug.Log("播放停止");

// 订阅音频数据事件
coordinator.OnAudioDataReceived += (rms, spectrum) => {
    // 处理音频数据
};
```

## 测试

使用 `AudioReactorTest` 脚本可以测试整个音频反应器架构：

1. 将 `AudioReactorTest` 脚本添加到场景中的GameObject
2. 配置测试参数（AudioSource、AudioClip等）
3. 运行场景或在Inspector中点击"运行测试"

## 注意事项

1. **初始化顺序**：确保在使用协调器之前，相关的音频反应插件已经正确安装和配置。

2. **异步操作**：大部分操作都是异步的，请使用 `await` 关键字等待操作完成。

3. **错误处理**：所有异步方法都会返回操作结果，请检查返回值以确保操作成功。

4. **资源管理**：在场景切换或应用退出时，调用 `FullReset()` 方法清理资源。

5. **调试模式**：在开发阶段建议启用调试模式，以便查看详细的日志信息。

## 兼容性

- 支持 AudioReactiveShaders 插件
- 支持 AudioSync Pro 插件
- 可扩展支持其他音频反应插件
- 向后兼容原有的 AudioLyricCoordinator API

## 扩展性

要添加新的音频反应插件支持：

1. 实现 `IAudioReactor` 接口
2. 创建对应的适配器类
3. 在 `AudioReactorManager` 中添加自动发现逻辑

示例：
```csharp
public class CustomAudioReactorAdapter : IAudioReactor
{
    // 实现IAudioReactor接口
    public string ReactorId { get; private set; }
    public string DisplayName { get; private set; }
    // ... 其他属性和方法
}
```