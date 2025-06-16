# 歌词播放管理模块 (LyricModule)

## 概述

歌词播放管理模块是一个功能强大的Unity歌词显示系统，支持字符级特效管理、LRC文件解析、动态字符创建和多种视觉效果。该模块基于TEngine框架设计，采用模块化架构和异步编程模式。

## 主要特性

- **字符级特效管理**: 每个字符都是独立的单位，支持精细的特效控制
- **LRC文件支持**: 完整的LRC歌词文件解析和播放功能
- **动态创建**: 运行时动态创建歌词字符和行，支持对象复用
- **丰富的视觉效果**: 模糊、缩放、移动、淡入淡出、旋转等多种效果
- **复合效果**: 支持多种效果的组合使用
- **配置化**: 通过配置文件灵活控制各种效果参数
- **异步处理**: 基于UniTask的异步编程，性能优化
- **扩展性**: 易于添加新的效果类型和功能

## 核心组件

### 1. LyricModule (主模块)
- 歌词播放的核心管理器
- 继承自Singleton模式，全局唯一实例
- 负责LRC文件解析、歌词播放控制、对象池管理

### 2. LyricData (数据结构)
- `LyricData`: 完整的歌词数据
- `LyricLineData`: 单行歌词数据
- `LyricConfig`: 歌词配置信息
- `LyricEffectConfig`: 效果配置参数

### 3. LyricLine (行管理)
- 管理单行歌词的显示和效果
- 负责字符的创建、布局和生命周期

### 4. LyricCharacter (字符管理)
- 管理单个歌词字符
- 包含字符的状态、特效和属性

### 5. LyricEffects (效果实现)
- 各种视觉效果的具体实现
- 支持基础效果和复合效果

### 6. LyricExtensions (扩展方法)
- 便捷的使用接口和预设配置
- 常用效果的快速创建方法

## 支持的效果类型

### 基础效果
- **Fade**: 淡入淡出效果
- **Scale**: 缩放效果
- **Move**: 移动效果
- **Blur**: 模糊效果（需要BlurFilter组件）
- **Rotate**: 旋转效果

### 复合效果
- **BlurFade**: 模糊+淡入效果
- **ScaleFade**: 缩放+淡入效果
- **MoveFade**: 移动+淡入效果

### 预设配置
- **经典模糊效果**: 基于原始RandomBlurFont的效果
- **弹性缩放效果**: 带有弹性动画的缩放效果
- **飞入效果**: 从上方飞入的效果
- **打字机效果**: 逐字显示的打字机效果

## 快速开始

### 1. 基础设置

```csharp
// 获取歌词模块实例
var lyricModule = LyricModule.Instance;

// 设置歌词父对象
lyricModule.SetLyricParent(lyricParentTransform);

// 设置字符预制体
lyricModule.SetCharacterPrefab(characterPrefab);
```

### 2. 播放简单文本

```csharp
// 使用默认配置播放文本
await lyricModule.PlaySimpleText("Hello World");

// 使用自定义配置播放文本
var config = LyricExtensions.GetClassicBlurConfig();
await lyricModule.PlaySimpleText("Hello World", 0f, config);
```

### 3. 播放LRC文件

```csharp
// 加载并播放LRC文件
var config = LyricExtensions.GetClassicBlurConfig();
await lyricModule.LoadAndPlayLyric("path/to/lyric.lrc", config);
```

### 4. 播放多行歌词

```csharp
var lines = new List<(float time, string text)>
{
    (0f, "第一行歌词"),
    (3f, "第二行歌词"),
    (6f, "第三行歌词")
};

var config = LyricExtensions.GetBouncyScaleConfig();
await lyricModule.PlayMultipleLines(lines, config);
```

## 配置说明

### LyricConfig (歌词配置)

```csharp
public class LyricConfig
{
    public float FontSize = 48f;                    // 字体大小
    public Color DefaultColor = Color.white;         // 默认颜色
    public Color HighlightColor = Color.yellow;      // 高亮颜色
    public Font Font;                                // 字体
    public float CharacterSpacing = 5f;              // 字符间距
    public float LineSpacing = 60f;                  // 行间距
    public float CharacterDelay = 0.1f;              // 字符间延迟
    public bool AutoDestroy = true;                  // 自动销毁
    public float AutoDestroyDelay = 2f;              // 自动销毁延迟
    
    public LyricEffectConfig EnterEffect;           // 进入效果
    public LyricEffectConfig ExitEffect;            // 离开效果
    public LyricEffectConfig CharacterEffect;       // 字符效果
}
```

### LyricEffectConfig (效果配置)

```csharp
public class LyricEffectConfig
{
    public LyricEffectType EffectType;               // 效果类型
    public float Duration = 1f;                      // 持续时间
    public AnimationCurve Curve;                     // 动画曲线
    
    // 各种效果参数
    public FadeEffectParams FadeParams;
    public ScaleEffectParams ScaleParams;
    public MoveEffectParams MoveParams;
    public BlurEffectParams BlurParams;
    public RotateEffectParams RotateParams;
}
```

## 自定义效果

### 创建自定义效果配置

```csharp
public LyricConfig CreateCustomConfig()
{
    var config = LyricConfig.Default;
    
    // 自定义进入效果
    config.EnterEffect = new LyricEffectConfig
    {
        EffectType = LyricEffectType.ScaleFade,
        Duration = 1.5f,
        ScaleParams = new ScaleEffectParams
        {
            StartScale = Vector3.zero,
            EndScale = Vector3.one
        },
        FadeParams = new FadeEffectParams
        {
            StartAlpha = 0f,
            EndAlpha = 1f
        },
        Curve = AnimationCurve.EaseInOut(0, 0, 1, 1)
    };
    
    return config;
}
```

### 扩展新的效果类型

1. 在`LyricEffectType`枚举中添加新类型
2. 在`LyricEffects`类中实现效果逻辑
3. 在效果调度器中添加新类型的处理

```csharp
// 1. 添加枚举
public enum LyricEffectType
{
    // ... 现有类型
    CustomEffect  // 新效果类型
}

// 2. 实现效果
public static async UniTask PlayCustomEffect(LyricCharacter character, LyricEffectConfig config, CancellationToken cancellationToken = default)
{
    // 自定义效果实现
}

// 3. 在调度器中添加处理
case LyricEffectType.CustomEffect:
    await PlayCustomEffect(character, config, cancellationToken);
    break;
```

## LRC文件格式

支持标准LRC格式：

```
[ti:歌曲标题]
[ar:艺术家]
[al:专辑]
[by:制作者]
[offset:时间偏移]

[00:12.34]第一行歌词
[00:15.67]第二行歌词
[00:18.90]第三行歌词
```

## 性能优化

### 对象池
- 字符对象自动复用，减少GC压力
- 行对象池化管理
- 智能的对象生命周期管理

### 异步处理
- 基于UniTask的异步编程
- 非阻塞的效果播放
- 可取消的操作支持

### 内存管理
- 自动清理不活跃的歌词对象
- 可配置的自动销毁机制
- 智能的资源释放

## 调试功能

### 调试信息
- 实时显示播放状态
- 活跃对象数量监控
- 性能指标统计

### 可视化调试
- 字符边界框显示
- 效果参数实时调整
- 播放进度可视化

## 示例场景

`LyricModuleExample`类提供了完整的使用示例：

- 各种预设效果的演示
- 自定义效果的创建
- LRC文件播放示例
- 调试界面的使用

## 依赖项

- **TEngine框架**: 基础架构支持
- **UniTask**: 异步编程支持
- **TextMeshPro**: 文本渲染
- **BlurFilter** (可选): 模糊效果支持

## 注意事项

1. **BlurFilter组件**: 如果项目中没有相关的模糊效果组件，模糊效果将被跳过
2. **字体资源**: 确保设置了正确的字体资源
3. **性能考虑**: 大量字符同时播放时注意性能影响
4. **内存管理**: 及时清理不需要的歌词对象

## 扩展建议

1. **音频同步**: 可以集成音频播放器实现音画同步
2. **更多效果**: 添加粒子效果、光效等更丰富的视觉效果
3. **编辑器工具**: 创建可视化的歌词编辑器
4. **本地化支持**: 支持多语言歌词显示
5. **主题系统**: 支持不同的视觉主题切换

## 版本历史

- **v1.0.0**: 初始版本，基础功能实现
  - LRC文件解析
  - 基础视觉效果
  - 字符级管理
  - 对象池优化

---

更多详细信息请参考源代码注释和示例场景。