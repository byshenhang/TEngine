# LRC歌词多效果播放指南

## 概述

本指南介绍如何使用字幕模块的LRC歌词解析功能，结合多种视觉效果来创建丰富的歌词显示体验。

## 功能特性

### 默认效果配置

当启用效果时，LRC歌词会自动应用以下默认效果：

**进场效果：**
- **淡入效果**：从透明度0渐变到1，持续0.5秒
- **缩放效果**：从0.8倍缩放到1倍，持续0.5秒

**离场效果：**
- **淡出效果**：从透明度1渐变到0，持续0.5秒

### 显示模式

- **显示模式**：WordByWord（逐词显示）
- **词间隔**：0.1秒
- **字体大小**：28
- **性别颜色**：
  - 男声（M）：蓝色
  - 女声（F）：红色
  - 合唱（D）：洋红色
  - 默认：白色

## 使用方法

### 1. 基础播放（启用默认效果）

```csharp
// 播放LRC文件（默认启用效果）
await subtitleModule.PlayLRCFileAsync("path/to/song.lrc");

// 播放LRC内容（默认启用效果）
string lrcContent = "[00:10.00]第一行歌词\n[00:15.00]第二行歌词";
await subtitleModule.PlayLRCContentAsync(lrcContent);

// 从Resources播放（默认启用效果）
await subtitleModule.PlayLRCFromResourcesAsync("Songs/MySong");
```

### 2. 禁用默认效果

```csharp
// 播放LRC文件（禁用效果）
await subtitleModule.PlayLRCFileAsync("path/to/song.lrc", 3f, false);

// 播放LRC内容（禁用效果）
await subtitleModule.PlayLRCContentAsync(lrcContent, 3f, false);
```

### 3. 使用时序控制器（支持暂停/恢复）

```csharp
// 解析LRC内容
var parseResult = LRCParser.ParseLRC(lrcContent);

// 添加到时序控制器（启用效果）
subtitleModule.AddLRCToTimingController(parseResult, 3f, "my_song", true);

// 控制播放
subtitleModule.PlayTimedSubtitles();   // 开始播放
subtitleModule.PauseTimedSubtitles();  // 暂停
subtitleModule.ResumeTimedSubtitles(); // 恢复
subtitleModule.StopTimedSubtitles();   // 停止
```

### 4. 自定义效果配置

```csharp
// 解析LRC并获取基础配置（不启用默认效果）
var parseResult = LRCParser.ParseLRC(lrcContent);
var configs = LRCParser.ConvertToSubtitleConfigs(parseResult, 3f, false);

// 为每个配置添加自定义效果
foreach (var config in configs)
{
    // 进场效果：打字机效果
    config.Effects.Add(new TypewriterEffectConfig
    {
        Phase = SubtitleEffectPhase.Enter,
        Duration = 1f,
        Delay = 0f,
        CharacterInterval = 0.05f
    });
    
    // 进场效果：模糊到清晰
    config.Effects.Add(new BlurEffectConfig
    {
        Phase = SubtitleEffectPhase.Enter,
        Duration = 0.8f,
        Delay = 0.2f,
        BlurStart = 5f,
        BlurEnd = 0f
    });
    
    // 离场效果：缩放 + 淡出
    config.Effects.Add(new ScaleEffectConfig
    {
        Phase = SubtitleEffectPhase.Exit,
        Duration = 0.6f,
        Delay = 0f,
        ScaleStart = Vector3.one,
        ScaleEnd = new Vector3(1.2f, 1.2f, 1f)
    });
    
    config.Effects.Add(new FadeEffectConfig
    {
        Phase = SubtitleEffectPhase.Exit,
        Duration = 0.6f,
        Delay = 0f,
        AlphaStart = 1f,
        AlphaEnd = 0f
    });
}

// 手动播放自定义配置的歌词
for (int i = 0; i < parseResult.entries.Count; i++)
{
    var entry = parseResult.entries[i];
    var config = configs[i];
    
    // 等待时间逻辑...
    
    // 播放当前歌词
    await subtitleModule.PlaySubtitleSequenceAsync($"line_{i}", config);
}
```

## 效果类型说明

### 1. 淡入淡出效果 (FadeEffectConfig)

```csharp
new FadeEffectConfig
{
    Phase = SubtitleEffectPhase.Enter,  // 或 Exit
    Duration = 0.5f,                    // 效果持续时间
    Delay = 0f,                         // 延迟时间
    AlphaStart = 0f,                    // 起始透明度
    AlphaEnd = 1f                       // 结束透明度
}
```

### 2. 缩放效果 (ScaleEffectConfig)

```csharp
new ScaleEffectConfig
{
    Phase = SubtitleEffectPhase.Enter,
    Duration = 0.5f,
    Delay = 0f,
    ScaleStart = new Vector3(0.8f, 0.8f, 1f),  // 起始缩放
    ScaleEnd = Vector3.one                      // 结束缩放
}
```

### 3. 打字机效果 (TypewriterEffectConfig)

```csharp
new TypewriterEffectConfig
{
    Phase = SubtitleEffectPhase.Enter,
    Duration = 1f,
    Delay = 0f,
    CharacterInterval = 0.05f           // 字符间隔时间
}
```

### 4. 模糊效果 (BlurEffectConfig)

```csharp
new BlurEffectConfig
{
    Phase = SubtitleEffectPhase.Enter,
    Duration = 0.8f,
    Delay = 0f,
    BlurStart = 5f,                     // 起始模糊度
    BlurEnd = 0f                        // 结束模糊度
}
```

## 效果阶段 (SubtitleEffectPhase)

- **Enter**：进场阶段，字幕出现时播放
- **Stay**：停留阶段，字幕显示期间播放
- **Exit**：离场阶段，字幕消失时播放

## LRC格式支持

### 基本格式

```
[ti:歌曲标题]
[ar:艺术家]
[al:专辑]
[offset:1000]
[00:12.00]第一行歌词
[00:17.20]第二行歌词
[00:21.10]第三行歌词
```

### 性别标签支持

```
[00:12.00]M: 男声部分
[00:17.20]F: 女声部分
[00:21.10]D: 合唱部分
```

## 示例代码

参考 `LRCMultiEffectExample.cs` 文件，其中包含了完整的使用示例：

- 基础播放功能
- TimingController模式演示
- 自定义效果配置演示

## 注意事项

1. **性能考虑**：大量效果同时播放可能影响性能，建议根据设备性能调整效果复杂度
2. **时间同步**：使用 `PlayLRCContentAsync` 等方法时，播放是基于 `UniTask.Delay` 的顺序播放
3. **暂停恢复**：如需完整的暂停/恢复功能，建议使用 `AddLRCToTimingController` 方法
4. **效果冲突**：同一阶段的相同类型效果可能会产生冲突，建议合理规划效果配置

## 扩展开发

如需添加新的效果类型，请参考现有的效果实现：

- `FadeSubtitleEffect.cs`
- `ScaleSubtitleEffect.cs`
- `TypewriterSubtitleEffect.cs`
- `BlurSubtitleEffect.cs`

并在 `SubtitleModule` 中注册对应的效果工厂。