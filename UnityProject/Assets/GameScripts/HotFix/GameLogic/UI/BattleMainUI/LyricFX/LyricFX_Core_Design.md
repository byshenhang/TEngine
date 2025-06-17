# LyricFX 框架核心设计文档

## 1. 框架概述

LyricFX 是一个基于 UniTask 的高度解耦、灵活的歌词/字幕效果框架，通过严格的关注点分离实现模块化设计，支持布局、视觉效果和时间控制的完全分离。

## 2. 核心组件

### 2.1 核心接口

- **ILyricEffect**: 所有视觉效果的基础接口，定义效果的生命周期方法
- **ILayoutProvider**: 布局提供器接口，负责计算和应用字符位置
- **ICharacterProcessor**: 字符处理器接口，定义处理字符的方法

### 2.2 管理和协调

- **LyricManager**: 核心协调器，管理歌词行创建、播放、停止
- **CharacterFactory**: 负责字符对象的创建和回收
- **CharacterProcessingPipeline**: 管理字符处理流程

### 2.3 注册表

- **LayoutRegistry**: 管理和提供布局实现
- **EffectRegistry**: 管理和提供效果实现

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

**SequentialBlurEffect**: 序列模糊效果，实现字符交替出现、从模糊到清晰、最后整体淡出
```csharp
public async UniTask Play(CancellationToken cancellationToken)
{
    // 第一轮：先显示偶数位置字符
    for (int i = 0; i < characterObjects.Count; i += 2)
    {
        await ActivateAndFade(i, cancellationToken);
        await WaitForBlurBelowThreshold(i, cancellationToken);
    }
    
    // 第二轮：再显示奇数位置字符
    for (int i = 1; i < characterObjects.Count; i += 2)
    {
        await ActivateAndFade(i, cancellationToken);
        await WaitForBlurBelowThreshold(i, cancellationToken);
    }
    
    // 延迟后整体淡出
    await UniTask.Delay(500, cancellationToken: cancellationToken);
    await FadeOutAllText(cancellationToken);
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

## 5. 使用示例

```csharp
// 创建和播放一行歌词
int lineId = await lyricManager.CreateLyricLine(
    "Hello World!", 
    "default_linear", 
    "sequential_blur", 
    new Vector3(0, 0, 0),
    new SequentialBlurConfig { 
        BlurStart = 30.0f, 
        BlurFadeDuration = 1.0f 
    }
);

await lyricManager.PlayLyricLine(lineId);

// 播放整个LRC文件
await lyricManager.PlayLrcFile(
    lrcContent,
    "wave_layout",
    "blur_font_effect",
    new Vector3(0, 0, 0)
);
```
