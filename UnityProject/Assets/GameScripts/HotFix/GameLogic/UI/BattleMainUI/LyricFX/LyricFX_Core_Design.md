# LyricFX 框架核心设计文档

## 1. 框架概述

LyricFX 是一个基于 UniTask 的高度解耦、灵活的歌词/字幕效果框架，通过严格的关注点分离实现模块化设计，支持布局、视觉效果和时间控制的完全分离。框架支持字符级效果和行级协调器两种模式，提供强大的扩展性和向后兼容性。

## 2. 核心组件

### 2.1 核心接口

- **ILyricEffect**: 所有视觉效果的基础接口，定义效果的生命周期方法
- **ILayoutProvider**: 布局提供器接口，负责计算和应用字符位置
- **ICharacterProcessor**: 字符处理器接口，定义处理字符的方法
- **ILineEffectCoordinator**: 行级效果协调器接口，用于协调复杂的多字符效果

### 2.2 管理和协调

- **LyricManager**: 核心协调器，管理歌词行创建、播放、停止
- **CharacterFactory**: 负责字符对象的创建和回收
- **CharacterProcessingPipeline**: 管理字符处理流程

### 2.3 注册表和元数据

- **LayoutRegistry**: 管理和提供布局实现
- **EffectRegistry**: 管理和提供效果实现，支持效果元数据和协调器注册
- **EffectMetadata**: 效果元数据系统，包含效果作用域、类型信息和协调器类型
- **EffectScope**: 效果作用域枚举（Character/Line），用于区分字符级和行级效果

## 3. 执行流程

### 3.1 歌词行创建流程

1. LyricManager 分配唯一行ID
2. 获取布局提供器计算字符位置
3. 创建字符对象和处理上下文
4. 通过处理管道处理字符上下文
5. 应用布局到字符对象
6. 初始化视觉效果

```csharp
// 创建歌词行的核心代码
public async UniTask<int> CreateLyricLine(string text, string layoutId, string effectId, Vector3 position, object config = null)
{
    int lineId = GetNextLineId();
    ILayoutProvider layoutProvider = await layoutRegistry.GetLayout(layoutId);
    GameObject lineContainer = CreateLineContainer(lineId, position);
    Vector3[] positions = await layoutProvider.CalculateLayout(text, lineContainer.transform, config);
    
    List<ProcessingContext> contexts = new List<ProcessingContext>();
    for (int i = 0; i < text.Length; i++)
    {
        contexts.Add(new ProcessingContext(lineId, i, text[i], positions[i]));
    }
    
    var processedContexts = await pipeline.Process(contexts);
    ILyricEffect effect = await effectRegistry.GetEffect(effectId);
    await effect.Initialize(lineContainer, config);
    
    // 保存行信息并返回ID
    lineInfos[lineId] = new LineInfo(lineContainer, processedContexts, effect);
    return lineId;
}
```

### 3.2 歌词播放流程

1. 查找行信息
2. 创建取消令牌
3. 播放效果
4. 等待完成或取消

```csharp
// 播放歌词行的核心代码
public async UniTask PlayLyricLine(int lineId)
{
    if (!lineInfos.TryGetValue(lineId, out LineInfo info))
        return;
    
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token);
    
    info.CancellationTokenSource = cts;
    await info.Effect.Play(cts.Token);
}
```

## 4. 扩展实现

### 4.1 布局示例

**DefaultLinearLayout**: 基本的线性水平布局
```csharp
public async UniTask<Vector3[]> CalculateLayout(string text, Transform container, object config)
{
    float spacing = config is LinearLayoutConfig cfg ? cfg.CharacterSpacing : 1.0f;
    Vector3[] positions = new Vector3[text.Length];
    
    for (int i = 0; i < text.Length; i++)
    {
        positions[i] = new Vector3(i * spacing, 0, 0);
    }
    
    return positions;
}
```

### 4.2 效果示例

#### 字符级效果

**RandomColorFadeEffect**: 随机颜色渐变效果
```csharp
public async UniTask Play(CancellationToken cancellationToken)
{
    var randomColor = GetRandomColor();
    await FadeToColor(randomColor, fadeInDuration, cancellationToken);
    await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: cancellationToken);
    await FadeToTransparent(fadeOutDuration, cancellationToken);
}
```

#### 行级协调器效果

**LeftToRightFadeCoordinator**: 从左到右渐变协调器
```csharp
protected override async UniTask CoordinateEffects(CancellationToken cancellationToken)
{
    for (int i = 0; i < characterEffects.Count; i++)
    {
        var effect = characterEffects[i];
        _ = effect.Play(cancellationToken); // 异步启动
        
        float progress = (float)(i + 1) / characterEffects.Count;
        UpdateProgress(progress);
        
        await UniTask.Delay(TimeSpan.FromSeconds(characterDelay), cancellationToken: cancellationToken);
    }
    
    IsCompleted = true;
}
```

**RandomBatchFadeCoordinator**: 随机批量淡入淡出协调器
```csharp
protected override async UniTask CoordinateEffects(CancellationToken cancellationToken)
{
    // 第一阶段：随机分批淡入显示
    await RandomBatchFadeIn(cancellationToken);
    
    // 第二阶段：保持显示
    await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: cancellationToken);
    
    // 第三阶段：整体淡出
    await FadeOutAll(cancellationToken);
    
    UpdateProgress(1.0f);
    IsCompleted = true;
}
```

### 4.3 处理器示例

**SequentialRevealProcessor**: 为序列效果提供显示顺序信息
```csharp
protected override async UniTask<ProcessingContext> OnProcess(ProcessingContext context, CancellationToken cancellationToken)
{
    int lineId = context.LineId;
    int currentIndex = lineProcessedCount.GetValueOrDefault(lineId, 0)++;
    
    bool isEven = currentIndex % 2 == 0;
    int evenCount = (GetProcessedCharacterCount(lineId) + 1) / 2;
    int displayOrder = isEven ? currentIndex / 2 : evenCount + (currentIndex - 1) / 2;
    
    context.SetMetadata("displayOrder", displayOrder);
    return context;
}
```

## 5. 架构特性

### 5.1 双模式支持

框架支持两种效果模式：

1. **字符级效果模式**：每个字符独立应用效果，适用于简单的单字符动画
2. **行级协调器模式**：通过协调器统一管理整行字符的效果，适用于复杂的多字符协调动画

### 5.2 效果注册系统

```csharp
// 注册字符级效果
RegisterEffect<RandomColorFadeEffect>("random_color_fade", EffectScope.Character);

// 注册行级效果（需要协调器）
RegisterEffect<DefaultFadeEffect, LeftToRightFadeCoordinator>("left_to_right_fade", EffectScope.Line);
RegisterEffect<DefaultFadeEffect, RandomBatchFadeCoordinator>("random_batch_fade", EffectScope.Line);
```

### 5.3 智能效果选择

```csharp
// LyricManager 自动根据效果元数据选择播放模式
public async UniTask PlayLyricLine(int lineId)
{
    var line = GetLyricLine(lineId);
    
    if (EffectRegistry.RequiresCoordinator(line.EffectId))
    {
        // 使用行级协调器模式
        await PlayWithCoordinator(line, cancellationToken);
    }
    else
    {
        // 使用字符级效果模式
        await PlayWithCharacterEffects(line, cancellationToken);
    }
}
```

## 6. 使用示例

### 6.1 基本使用

```csharp
// 创建和播放一行歌词（字符级效果）
int lineId = await lyricManager.CreateLyricLine(
    "Hello World!", 
    "default_linear", 
    "random_color_fade", 
    new Vector3(0, 0, 0)
);

await lyricManager.PlayLyricLine(lineId);

// 创建和播放一行歌词（行级协调器效果）
int lineId2 = await lyricManager.CreateLyricLine(
    "随机批量显示效果", 
    "default_linear", 
    "random_batch_fade", 
    new Vector3(0, 0, 0),
    new RandomBatchFadeConfig {
        MaxBatchSize = 3,
        BatchInterval = 0.5f,
        FadeInDuration = 0.8f
    }
);

await lyricManager.PlayLyricLine(lineId2);

// 播放整个LRC文件
await lyricManager.PlayLrcFile(
    "Assets/Resources/Lyrics/MySong.lrc",
    "default_linear",
    "left_to_right_fade"
);
```

### 6.2 自定义效果配置

```csharp
// 自定义随机批量淡入淡出配置
var config = new RandomBatchFadeConfig
{
    MaxBatchSize = 5,           // 每批最多5个字符
    BatchInterval = 0.3f,       // 批次间隔0.3秒
    FadeInDuration = 0.5f,      // 淡入时间0.5秒
    HoldDuration = 2.0f,        // 保持显示2秒
    FadeOutDuration = 1.0f,     // 淡出时间1秒
    FadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
    FadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0)
};

int lineId = await lyricManager.CreateLyricLine(
    "自定义配置的随机批量效果",
    "default_linear",
    "random_batch_fade",
    Vector3.zero,
    config
);
```

## 7. 扩展开发

### 7.1 创建自定义行级协调器

```csharp
public class CustomCoordinator : LineEffectCoordinator
{
    protected override async UniTask CreateCharacterEffects(object config, CancellationToken cancellationToken)
    {
        // 为每个字符创建效果实例
        foreach (var charObj in characterObjects)
        {
            var effect = new DefaultFadeEffect();
            await effect.Initialize(charObj, config, cancellationToken);
            characterEffects.Add(effect);
        }
    }
    
    protected override async UniTask CoordinateEffects(CancellationToken cancellationToken)
    {
        // 实现自定义的协调逻辑
        // ...
    }
}
```

### 7.2 注册自定义效果

```csharp
// 在EffectRegistry中注册
RegisterEffect<DefaultFadeEffect, CustomCoordinator>("custom_effect", EffectScope.Line);
```
