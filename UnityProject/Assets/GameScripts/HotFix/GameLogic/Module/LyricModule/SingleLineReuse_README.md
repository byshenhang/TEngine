# 歌词单行复用功能说明

## 概述

歌词模块现在支持两种显示模式：
- **多行模式 (MultiLine)**: 传统模式，为每行歌词创建独立的GameObject
- **单行复用模式 (SingleLineReuse)**: 新增模式，使用单个GameObject显示所有歌词行，下一行显示时清除上一行内容

## 功能特点

### 单行复用模式优势
1. **内存优化**: 只创建一个歌词行GameObject，大幅减少内存占用
2. **性能提升**: 减少GameObject数量，提高渲染性能
3. **适合长歌词**: 特别适用于歌词行数较多的场景
4. **平滑切换**: 支持退出和进入效果的平滑过渡

### 多行模式优势
1. **视觉丰富**: 可以同时显示多行歌词，提供更丰富的视觉效果
2. **效果多样**: 支持复杂的多行联动效果
3. **适合短歌词**: 适用于歌词行数较少的场景

## 使用方法

### 1. 配置显示模式

```csharp
// 创建歌词配置
var config = LyricConfig.Default;

// 设置为单行复用模式
config.DisplayMode = LyricDisplayMode.SingleLineReuse;

// 或设置为多行模式（默认）
config.DisplayMode = LyricDisplayMode.MultiLine;
```

### 2. 播放歌词

```csharp
// 获取歌词模块实例
var lyricModule = LyricModule.Instance;

// 播放歌词
await lyricModule.PlayLyric(lyricData, config);
```

### 3. 完整示例

```csharp
public async UniTask PlayLyricWithSingleLineReuse()
{
    // 创建歌词数据
    var lyricData = new LyricData();
    lyricData.Lines.Add(new LyricLineData { Text = "第一行歌词", Time = 0f });
    lyricData.Lines.Add(new LyricLineData { Text = "第二行歌词", Time = 3f });
    lyricData.Lines.Add(new LyricLineData { Text = "第三行歌词", Time = 6f });
    
    // 创建配置并设置为单行复用模式
    var config = LyricConfig.Default;
    config.DisplayMode = LyricDisplayMode.SingleLineReuse;
    config.FontSize = 36;
    config.DefaultColor = Color.white;
    
    // 播放歌词
    var lyricModule = LyricModule.Instance;
    await lyricModule.PlayLyric(lyricData, config);
}
```

## 技术实现

### 核心机制
1. **复用行对象**: 创建单个 `LyricLine` 对象用于显示所有歌词
2. **内容更新**: 当切换到新歌词行时，更新复用行的文本内容和字符
3. **效果过渡**: 播放退出效果 → 更新内容 → 播放进入效果
4. **字符池管理**: 复用字符对象，减少创建和销毁开销

### 关键方法
- `UpdateLyricDisplaySingleLineReuse()`: 单行复用模式的显示更新逻辑
- `UpdateReusableLineContent()`: 更新复用行的内容
- `LyricLine.Initialize(LyricLineData, LyricConfig, Queue<LyricCharacter>)`: 重新初始化歌词行

## 性能对比

| 特性 | 多行模式 | 单行复用模式 |
|------|----------|-------------|
| GameObject数量 | N行 | 1行 |
| 内存占用 | 高 | 低 |
| 渲染性能 | 一般 | 优秀 |
| 视觉效果 | 丰富 | 简洁 |
| 适用场景 | 短歌词 | 长歌词 |

## 注意事项

1. **效果时长**: 单行复用模式下，建议调整退出效果时长以确保平滑过渡
2. **字符池**: 确保字符对象池有足够的字符对象供复用
3. **配置一致性**: 切换模式时建议重新播放歌词以确保配置生效
4. **内存管理**: 单行复用模式会自动管理字符对象的回收和复用

## 示例场景

查看 `LyricSingleLineReuseExample.cs` 文件获取完整的使用示例，包括：
- 模式切换
- 播放控制
- 配置自定义
- 实时状态显示

## 扩展建议

1. **配置预设**: 为不同场景创建预设配置
2. **动态切换**: 支持运行时动态切换显示模式
3. **效果优化**: 针对单行复用模式优化特定的过渡效果
4. **性能监控**: 添加性能监控以对比两种模式的实际表现