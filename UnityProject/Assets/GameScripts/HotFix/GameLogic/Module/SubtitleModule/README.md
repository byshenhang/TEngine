# 字幕效果模块 (Subtitle Module)

一个高度可扩展的Unity字幕效果系统，支持复杂的歌词显示和多种动画效果。

## 功能特性

### 🎯 核心功能
- **模块化设计**: 基于TEngine框架的模块化架构
- **高度可扩展**: 支持自定义效果和序列类型
- **多种显示模式**: 逐字符、逐词、逐行、整体显示
- **效果阶段控制**: 支持进场、停留、离场三个阶段的独立效果配置
- **时序控制**: 精确的时间控制和同步
- **资源管理**: 完整的资源加载和缓存系统

### 🎨 内置效果
- **模糊效果**: 支持ChocDino.UIFX插件的BlurFilter
- **淡入淡出**: 平滑的透明度变化
- **打字机效果**: 逐字符显示动画
- **缩放效果**: 支持弹跳和自定义曲线
- **自定义效果**: 易于扩展的效果系统

### ⏱️ 时序功能
- **定时播放**: 支持复杂的时间轴控制
- **暂停/恢复**: 完整的播放控制
- **跳转**: 任意时间点跳转
- **同步**: 多字幕序列同步播放

## 快速开始

### 1. 基本使用

```csharp
// 获取字幕模块
var subtitleModule = GameModule.GetModule<SubtitleModule>();

// 创建简单字幕配置
var config = new SubtitleSequenceConfig
{
    Text = "Hello World!",
    DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
    CharacterInterval = 0.1f,
    Parent = uiParent
};

// 添加淡入效果
config.Effects.Add(new FadeEffectConfig
{
    EffectType = "Fade",
    Duration = 0.5f,
    FadeIn = true
});

// 创建并播放字幕
var sequence = await subtitleModule.CreateSequenceAsync(SubtitleSequenceType.Simple, config);
await sequence.PlayAsync();
```

### 2. 复杂模糊效果（类似原始代码）

```csharp
var config = new SubtitleSequenceConfig
{
    Text = "I saw you",
    DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
    CharacterInterval = 0.2f,
    Parent = subtitleParent
};

// 添加模糊效果
config.Effects.Add(new BlurEffectConfig
{
    EffectType = "Blur",
    Phase = SubtitleEffectPhase.Enter, // 进场阶段
    Duration = 1.0f,
    BlurStart = 30f,
    BlurEnd = 0f,
    BlurThreshold = 10f,
    AnimationCurve = blurCurve
});

// 添加最终淡出
config.Effects.Add(new FadeEffectConfig
{
    EffectType = "Fade",
    Phase = SubtitleEffectPhase.Exit, // 离场阶段
    Duration = 0.5f,
    Delay = 1.5f,
    FadeIn = false
});

var sequence = await subtitleModule.CreateSequenceAsync(SubtitleSequenceType.Complex, config);
await sequence.PlayAsync();
```

### 3. 效果阶段控制

```csharp
var config = new SubtitleSequenceConfig
{
    Text = "完整阶段效果演示",
    Effects = new List<SubtitleEffectConfig>
    {
        // 进场阶段：缩放 + 模糊
        new ScaleEffectConfig
        {
            Phase = SubtitleEffectPhase.Enter,
            Duration = 0.8f,
            StartScale = Vector3.zero,
            EndScale = Vector3.one,
            EnableBounce = true
        },
        new BlurEffectConfig
        {
            Phase = SubtitleEffectPhase.Enter,
            Duration = 1f,
            BlurStart = 20f,
            BlurEnd = 0f
        },
        
        // 停留阶段：呼吸效果
        new ScaleEffectConfig
        {
            Phase = SubtitleEffectPhase.Stay,
            Duration = 2f,
            StartScale = Vector3.one,
            EndScale = Vector3.one * 1.1f
        },
        
        // 离场阶段：淡出 + 缩小
        new FadeEffectConfig
        {
            Phase = SubtitleEffectPhase.Exit,
            Duration = 1f,
            FadeIn = false
        }
    }
};
```

### 4. 定时字幕序列

```csharp
// 添加多个定时字幕
subtitleModule.AddTimedSubtitle("subtitle1", 0f, 2f, sequence1);
subtitleModule.AddTimedSubtitle("subtitle2", 2.5f, 2f, sequence2);
subtitleModule.AddTimedSubtitle("subtitle3", 5f, 2f, sequence3);

// 开始播放定时序列
subtitleModule.PlayTimedSequence();

// 控制播放
subtitleModule.Pause();
subtitleModule.Resume();
subtitleModule.SeekTo(3.0f);
```

## 架构设计

### 核心组件

```
SubtitleModule (主模块)
├── SubtitleEffectManager (效果管理器)
├── SubtitleTimingController (时序控制器)
├── SubtitleResourceManager (资源管理器)
└── Sequences (字幕序列)
    ├── SimpleSubtitleSequence (简单序列)
    ├── ComplexSubtitleSequence (复杂序列)
    └── SubtitleSequenceBase (基类)
```

### 效果系统

```
ISubtitleEffect (效果接口)
├── BlurSubtitleEffect (模糊效果)
├── FadeSubtitleEffect (淡入淡出)
├── TypewriterSubtitleEffect (打字机效果)
├── ScaleSubtitleEffect (缩放效果)
└── CustomSubtitleEffect (自定义效果)
```

## 配置说明

### SubtitleSequenceConfig

| 属性 | 类型 | 说明 |
|------|------|------|
| Text | string | 字幕文本 |
| DisplayMode | SubtitleDisplayMode | 显示模式 |
| CharacterInterval | float | 字符间隔时间 |
| WordInterval | float | 单词间隔时间 |
| LineInterval | float | 行间隔时间 |
| Parent | Transform | 父对象 |
| Effects | List<SubtitleEffectConfig> | 效果列表 |

### 效果阶段说明

字幕效果分为三个阶段：

#### SubtitleEffectPhase.Enter（进场阶段）
- **触发时机**: 字幕开始显示时
- **适用效果**: 模糊清晰化、缩放出现、淡入等
- **典型用法**: 让字幕以华丽的方式出现

#### SubtitleEffectPhase.Stay（停留阶段）
- **触发时机**: 字幕完全显示后，在保持时间内
- **适用效果**: 呼吸缩放、闪烁、轻微动画等
- **典型用法**: 保持字幕的视觉吸引力

#### SubtitleEffectPhase.Exit（离场阶段）
- **触发时机**: 字幕准备消失时
- **适用效果**: 淡出、模糊消失、缩放消失等
- **典型用法**: 让字幕优雅地退场

**注意**: 如果配置了自定义的离场效果，系统将不会执行默认的淡出效果。

### 显示模式

- **CharacterByCharacter**: 逐字符显示
- **WordByWord**: 逐词显示
- **LineByLine**: 逐行显示
- **All**: 整体显示

### 效果配置

### SubtitleEffectConfig

效果配置参数：

- `EffectType`: 效果类型名称
- `Phase`: 效果阶段（Enter/Stay/Exit）
- `Duration`: 效果持续时间
- `Delay`: 效果延迟时间
- `AnimationCurve`: 动画曲线
- `Parameters`: 效果特定参数字典

#### BlurEffectConfig
```csharp
public class BlurEffectConfig : SubtitleEffectConfig
{
    public float BlurStart { get; set; } = 30f;
    public float BlurEnd { get; set; } = 0f;
    public float BlurThreshold { get; set; } = 10f;
    public AnimationCurve AnimationCurve { get; set; }
}
```

#### FadeEffectConfig
```csharp
public class FadeEffectConfig : SubtitleEffectConfig
{
    public bool FadeIn { get; set; } = true;
    public AnimationCurve AnimationCurve { get; set; }
}
```

#### ScaleEffectConfig
```csharp
public class ScaleEffectConfig : SubtitleEffectConfig
{
    public Vector3 StartScale { get; set; } = Vector3.zero;
    public Vector3 EndScale { get; set; } = Vector3.one;
    public bool EnableBounce { get; set; } = false;
    public float BounceIntensity { get; set; } = 0.1f;
}
```

## 扩展开发

### 创建自定义效果

1. **实现效果接口**
```csharp
public class MyCustomEffect : ISubtitleEffect
{
    public bool IsCompleted { get; private set; }
    
    public async UniTask ExecuteAsync(GameObject target)
    {
        // 实现自定义效果逻辑
        await UniTask.Delay(1000);
        IsCompleted = true;
    }
    
    public void Stop()
    {
        IsCompleted = true;
    }
}
```

2. **创建效果工厂**
```csharp
public class MyCustomEffectFactory : ISubtitleEffectFactory
{
    public ISubtitleEffect CreateEffect(ISubtitleEffectConfig config)
    {
        return new MyCustomEffect(config);
    }
}
```

3. **注册效果**
```csharp
subtitleModule.RegisterEffectFactory("MyCustom", new MyCustomEffectFactory());
```

### 创建自定义序列

```csharp
public class MyCustomSequence : SubtitleSequenceBase
{
    public MyCustomSequence(SubtitleSequenceConfig config, SubtitleModule subtitleModule) 
        : base(config, subtitleModule)
    {
    }
    
    protected override async UniTask ExecutePlayLogic()
    {
        // 实现自定义播放逻辑
    }
}
```

## 性能优化

### 资源管理
- 自动缓存常用资源
- 支持预加载
- 及时清理未使用资源

### 效果优化
- 对象池复用
- 批量处理
- 异步执行

### 内存管理
```csharp
// 清理缓存
subtitleModule.ClearCache();

// 停止所有效果
subtitleModule.StopAll();

// 释放资源
subtitleModule.Dispose();
```

## 依赖要求

### 必需依赖
- Unity 2021.3+
- TEngine框架
- UniTask
- TextMeshPro

### 可选依赖
- ChocDino.UIFX (用于高级模糊效果)
- DOTween (可用于更丰富的动画)

## 示例场景

查看 `Examples/SubtitleModuleExample.cs` 了解完整的使用示例，包括：
- 简单字幕播放
- 复杂模糊效果
- 多效果组合
- 定时序列播放
- 自定义效果开发

## 故障排除

### 常见问题

1. **模糊效果不工作**
   - 检查是否安装了ChocDino.UIFX插件
   - 确认BlurFilter组件正确添加

2. **字幕不显示**
   - 检查Parent对象是否正确设置
   - 确认Canvas和Camera配置

3. **性能问题**
   - 减少同时播放的字幕数量
   - 使用对象池优化
   - 及时清理完成的效果

### 调试信息

```csharp
// 获取模块状态
var stats = subtitleModule.GetStats();
Log.Info($"活动字幕: {stats.ActiveSubtitles}, 缓存资源: {stats.CachedResources}");

// 获取效果统计
var effectStats = subtitleModule.GetEffectStats();
Log.Info($"活动效果: {effectStats.ActiveEffects}");
```

## 更新日志

### v1.0.0
- 初始版本发布
- 支持基本字幕效果
- 模块化架构设计
- 完整的示例和文档

## 许可证

本模块遵循项目的整体许可证协议。

## 贡献

欢迎提交Issue和Pull Request来改进这个模块。

---

**注意**: 这是一个高度可扩展的字幕系统，可以根据具体需求进行定制和扩展。如有问题或建议，请及时反馈。