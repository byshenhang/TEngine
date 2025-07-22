# TEngine模块扩展指南

## 概述

TEngine框架提供了灵活的模块扩展机制，支持两种主要的模块扩展方式：
1. **TEngine框架模块**：继承自`Module`基类，通过`ModuleSystem`管理
2. **游戏逻辑模块**：继承自`Singleton<T>`基类，通过`SingletonSystem`管理

## 1. TEngine框架模块扩展

### 1.1 基础结构

TEngine框架模块需要继承`Module`抽象类并实现相应接口：

```csharp
using TEngine;

namespace TEngine
{
    /// <summary>
    /// 自定义框架模块接口
    /// </summary>
    public interface ICustomModule
    {
        void DoSomething();
    }
    
    /// <summary>
    /// 自定义框架模块实现
    /// </summary>
    internal sealed class CustomModule : Module, ICustomModule, IUpdateModule
    {
        public override int Priority => 10; // 模块优先级
        
        public override void OnInit()
        {
            // 模块初始化逻辑
            Log.Info("CustomModule initialized");
        }
        
        public override void Shutdown()
        {
            // 模块关闭逻辑
            Log.Info("CustomModule shutdown");
        }
        
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            // 每帧更新逻辑（可选）
        }
        
        public void DoSomething()
        {
            // 自定义功能实现
        }
    }
}
```

### 1.2 模块注册

框架模块通过`ModuleSystem.GetModule<T>()`自动创建和注册：

```csharp
// 在GameModule.cs中添加模块访问器
public static ICustomModule Custom => _custom ??= Get<ICustomModule>();
private static ICustomModule _custom;
```

### 1.3 框架模块特点

- **自动生命周期管理**：由`ModuleSystem`统一管理初始化、更新和关闭
- **优先级支持**：通过`Priority`属性控制初始化和更新顺序
- **接口驱动**：通过接口访问，支持依赖注入
- **更新支持**：实现`IUpdateModule`接口可获得每帧更新

## 2. 游戏逻辑模块扩展

### 2.1 基础结构

游戏逻辑模块继承`Singleton<T>`基类：

```csharp
using GameLogic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 自定义游戏逻辑模块
    /// </summary>
    public sealed class CustomGameModule : Singleton<CustomGameModule>, IUpdate
    {
        private bool _isInitialized = false;
        
        protected override void OnInit()
        {
            base.OnInit();
            // 模块初始化逻辑
            _isInitialized = true;
            Log.Info("CustomGameModule initialized");
        }
        
        protected override void OnRelease()
        {
            // 模块释放逻辑
            _isInitialized = false;
            Log.Info("CustomGameModule released");
            base.OnRelease();
        }
        
        public void OnUpdate()
        {
            // 每帧更新逻辑（可选）
            if (!_isInitialized) return;
            
            // 更新逻辑
        }
        
        public void DoSomething()
        {
            if (!_isInitialized)
            {
                Log.Warning("CustomGameModule not initialized");
                return;
            }
            
            // 自定义功能实现
        }
    }
}
```

### 2.2 模块注册

在`GameModule.cs`中添加访问器：

```csharp
/// <summary>
/// 获取自定义游戏模块。
/// </summary>
public static CustomGameModule CustomGame => _customGame ??= CustomGameModule.Instance;
private static CustomGameModule _customGame;
```

在`Shutdown()`方法中添加清理：

```csharp
public static void Shutdown()
{
    // ... 其他清理代码
    _customGame = null;
}
```

### 2.3 游戏逻辑模块特点

- **单例模式**：全局唯一实例，延迟初始化
- **生命周期接口**：支持`IUpdate`、`IFixedUpdate`、`ILateUpdate`等
- **自动注册**：通过`SingletonSystem`自动管理生命周期
- **灵活性**：适合游戏逻辑相关的模块

## 3. 模块扩展最佳实践

### 3.1 选择合适的模块类型

| 模块类型 | 适用场景 | 特点 |
|---------|---------|------|
| TEngine框架模块 | 底层系统功能、跨项目复用 | 接口驱动、优先级管理、框架集成 |
| 游戏逻辑模块 | 游戏特定功能、业务逻辑 | 单例模式、简单易用、灵活扩展 |

### 3.2 模块设计原则

1. **单一职责**：每个模块只负责一个特定的功能领域
2. **接口隔离**：定义清晰的接口，隐藏实现细节
3. **依赖倒置**：依赖抽象而非具体实现
4. **生命周期管理**：正确处理初始化和清理逻辑

### 3.3 模块间通信

#### 3.3.1 直接调用
```csharp
// 通过GameModule访问其他模块
GameModule.UI.OpenWindow("MainWindow");
GameModule.Audio.PlaySound("click");
```

#### 3.3.2 事件系统
```csharp
// 发送事件
EventManager.Instance.SendEvent("PlayerLevelUp", new PlayerLevelUpEventArgs());

// 监听事件
EventManager.Instance.AddListener<PlayerLevelUpEventArgs>("PlayerLevelUp", OnPlayerLevelUp);
```

### 3.4 模块配置

#### 3.4.1 配置文件
```csharp
[System.Serializable]
public class CustomModuleConfig
{
    public bool enableDebug = false;
    public float updateInterval = 0.1f;
    public string[] supportedFeatures;
}
```

#### 3.4.2 ScriptableObject配置
```csharp
[CreateAssetMenu(fileName = "CustomModuleSetting", menuName = "TEngine/Custom Module Setting")]
public class CustomModuleSetting : ScriptableObject
{
    [SerializeField] private CustomModuleConfig _config;
    public CustomModuleConfig Config => _config;
}
```

## 4. 实际案例分析

### 4.1 现有模块分析

项目中已有的模块实现可以作为参考：

- **UIModule**：游戏逻辑模块，管理UI窗口系统
- **CombatModule**：游戏逻辑模块，管理战斗系统
- **LyricFXModule**：游戏逻辑模块，管理歌词特效系统

### 4.2 模块扩展示例

以创建一个数据管理模块为例：

```csharp
namespace GameLogic
{
    /// <summary>
    /// 数据管理模块 - 负责游戏数据的加载、缓存和管理
    /// </summary>
    public sealed class DataModule : Singleton<DataModule>, IUpdate
    {
        private Dictionary<string, object> _dataCache;
        private bool _isLoading = false;
        
        protected override void OnInit()
        {
            base.OnInit();
            _dataCache = new Dictionary<string, object>();
            LoadGameData();
            Log.Info("DataModule initialized");
        }
        
        protected override void OnRelease()
        {
            _dataCache?.Clear();
            _dataCache = null;
            Log.Info("DataModule released");
            base.OnRelease();
        }
        
        public void OnUpdate()
        {
            // 处理异步数据加载等
        }
        
        private async void LoadGameData()
        {
            _isLoading = true;
            try
            {
                // 加载配置数据
                await LoadConfigData();
                // 加载用户数据
                await LoadUserData();
            }
            finally
            {
                _isLoading = false;
            }
        }
        
        public T GetData<T>(string key) where T : class
        {
            if (_dataCache.TryGetValue(key, out object data))
            {
                return data as T;
            }
            return null;
        }
        
        public void SetData<T>(string key, T data)
        {
            _dataCache[key] = data;
        }
    }
}
```

## 5. 注意事项

### 5.1 性能考虑

- **避免在Update中进行重复计算**
- **合理使用对象池减少GC压力**
- **异步操作使用UniTask**

### 5.2 内存管理

- **及时释放不需要的资源**
- **避免循环引用**
- **正确实现OnRelease方法**

### 5.3 线程安全

- **单例模块在主线程中访问**
- **异步操作注意线程切换**
- **使用线程安全的数据结构**

## 6. 总结

TEngine提供了完善的模块扩展机制，开发者可以根据需求选择合适的模块类型进行扩展。通过遵循框架的设计原则和最佳实践，可以创建出高质量、易维护的模块系统。

关键要点：
1. 选择合适的模块基类（Module vs Singleton）
2. 正确实现生命周期方法
3. 在GameModule中添加访问器
4. 遵循单一职责和接口隔离原则
5. 注意性能和内存管理

通过模块化设计，可以让项目结构更加清晰，代码更易维护和扩展。