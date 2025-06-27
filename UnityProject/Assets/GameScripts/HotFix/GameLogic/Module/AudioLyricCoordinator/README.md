# AudioLyricCoordinator 音频歌词协调器

## 概述

`AudioLyricCoordinator` 是一个采用协调器模式设计的模块，用于统一管理音频同步和歌词播放功能。它提供了最佳的可维护性和扩展性，同时保持了各模块的职责单一性。

## 架构设计

### 协调器模式优势

1. **职责清晰**: 每个模块专注于自己的核心功能
   - `AudioSyncModule`: 专注音频播放和音频反应
   - `LyricFXModule`: 专注歌词显示和特效
   - `AudioLyricCoordinator`: 专注两者之间的协调和同步

2. **松耦合**: 各模块之间通过协调器进行通信，降低直接依赖

3. **易扩展**: 可以轻松添加新的音频或歌词相关功能

4. **易测试**: 每个模块可以独立测试

5. **统一接口**: 提供一致的API供外部调用

### 模块关系图

```
┌─────────────────────────────────────┐
│        AudioLyricCoordinator        │
│         (协调器模块)                 │
├─────────────────────────────────────┤
│  • 统一管理音频和歌词播放            │
│  • 处理同步逻辑                     │
│  • 提供统一的外部接口               │
│  • 事件协调和转发                   │
└─────────────┬───────────────────────┘
              │
    ┌─────────┴─────────┐
    │                   │
    ▼                   ▼
┌─────────────┐    ┌─────────────┐
│AudioSync    │    │ LyricFX     │
│Module       │    │ Module      │
├─────────────┤    ├─────────────┤
│• 音频播放   │    │• 歌词显示   │
│• 音频反应   │    │• 歌词特效   │
│• 频谱分析   │    │• 布局管理   │
│• 标记系统   │    │• 时间同步   │
└─────────────┘    └─────────────┘
```

## 核心功能

### 1. 统一初始化
```csharp
// 获取协调器实例
var coordinator = GameModule.AUDIO_LYRIC;

// 初始化协调器
bool success = await coordinator.Initialize(audioReactor, audioSourcePlus);
```

### 2. 准备和播放资源
```csharp
// 方式一：分步准备和播放（推荐）
// 1. 准备音频和歌词资源
bool prepareSuccess = await coordinator.PrepareAudioAndLyrics(audioClip, lrcContent);
if (prepareSuccess)
{
    // 2. 开始同步播放
    bool playSuccess = await coordinator.PlaySynchronized(
        lyricPosition,   // 歌词显示位置（可选）
        effectId,        // 歌词特效ID（可选）
        layoutId,        // 歌词布局ID（可选）
        0.1f            // 音频开始延迟（可选）
    );
}

// 方式二：一步式播放（兼容旧版本）
bool playSuccess = await coordinator.PlayWithSync(
    audioClip,           // 音频剪辑
    lrcContent,          // LRC歌词内容
    lyricPosition,       // 歌词显示位置
    effectId,            // 歌词特效ID
    layoutId,            // 歌词布局ID
    audioStartDelay      // 音频开始延迟
);
```

### 3. 播放控制
```csharp
// 异步停止播放（推荐）
await coordinator.Stop();

// 同步停止所有播放
coordinator.StopAll();

// 暂停播放
coordinator.Pause();

// 恢复播放
coordinator.Resume();

// 设置播放位置
coordinator.SetPlaybackTime(30.0f); // 跳转到30秒
```

### 4. 状态查询
```csharp
// 检查是否正在播放
bool isPlaying = coordinator.IsPlaying();

// 获取当前播放时间
float currentTime = coordinator.GetCurrentTime();

// 获取音频总长度
float totalLength = coordinator.GetAudioLength();

// 获取实时音频数据
float rms = coordinator.GetRMSValue();
float[] spectrum = coordinator.GetSpectrumData();
```

### 5. 事件系统
```csharp
// 订阅播放事件
coordinator.OnPlaybackStarted += () => Debug.Log("播放开始");
coordinator.OnPlaybackStopped += () => Debug.Log("播放停止");

// 订阅音频数据事件
coordinator.OnAudioDataReceived += (rms, spectrum) => {
    // 处理实时音频数据
    UpdateVisualization(rms, spectrum);
};

// 订阅歌词变化事件
coordinator.OnLyricLineChanged += (lyricLine) => {
    // 处理歌词行变化
    UpdateLyricDisplay(lyricLine);
};
```

### 6. 调试和配置
```csharp
// 启用调试模式
coordinator.EnableDebugger(true);

// 设置同步偏移
coordinator.SetSyncOffset(0.2f); // 歌词提前0.2秒显示
```

## 使用示例

### 基本使用流程

```csharp
public class MusicPlayerController : MonoBehaviour
{
    [Header("音频设置")]
    public AudioClip musicClip;
    public AudioReactor audioReactor;
    
    [Header("歌词设置")]
    public string lrcContent;
    public Vector3 lyricPosition;
    
    private AudioLyricCoordinator coordinator;
    
    private async void Start()
    {
        // 1. 获取协调器实例
        coordinator = GameModule.AUDIO_LYRIC;
        
        // 2. 启用调试模式
        coordinator.EnableDebugger(true);
        
        // 3. 初始化协调器
        bool initSuccess = await coordinator.Initialize(audioReactor);
        
        if (initSuccess)
        {
            // 4. 订阅事件
            SubscribeToEvents();
            
            // 5. 开始播放
            await PlayMusic();
        }
    }
    
    private async UniTask PlayMusic()
    {
        // 方式一：分步准备和播放（推荐）
        bool prepareSuccess = await coordinator.PrepareAudioAndLyrics(musicClip, lrcContent);
        if (prepareSuccess)
        {
            bool playSuccess = await coordinator.PlaySynchronized(
                lyricPosition,   // 歌词显示位置
                "fade",         // 淡入淡出效果
                "center",       // 居中布局
                0.1f            // 音频延迟0.1秒启动
            );
            
            if (playSuccess)
            {
                Debug.Log("音乐播放成功启动");
            }
        }
        
        // 方式二：一步式播放（兼容旧版本）
        /*
        bool playSuccess = await coordinator.PlayWithSync(
            musicClip,
            lrcContent,
            lyricPosition,
            "fade",      // 淡入淡出效果
            "center",    // 居中布局
            0.1f         // 音频延迟0.1秒启动
        );
        
        if (playSuccess)
        {
            Debug.Log("音乐播放成功启动");
        }
        */
    }
    
    private void SubscribeToEvents()
    {
        coordinator.OnPlaybackStarted += OnMusicStarted;
        coordinator.OnPlaybackStopped += OnMusicStopped;
        coordinator.OnAudioDataReceived += OnAudioDataUpdate;
    }
    
    private void OnMusicStarted()
    {
        // 播放开始时的处理逻辑
        Debug.Log("音乐开始播放");
    }
    
    private void OnMusicStopped()
    {
        // 播放停止时的处理逻辑
        Debug.Log("音乐停止播放");
    }
    
    private void OnAudioDataUpdate(float rms, float[] spectrum)
    {
        // 根据音频数据更新可视化效果
        UpdateMusicVisualization(rms, spectrum);
    }
}
```

### 高级使用场景

#### 1. 多首歌曲播放列表
```csharp
public class PlaylistManager : MonoBehaviour
{
    [System.Serializable]
    public class MusicItem
    {
        public AudioClip audioClip;
        public string lrcContent;
        public string effectId;
        public float syncOffset;
    }
    
    public List<MusicItem> playlist;
    private int currentIndex = 0;
    private AudioLyricCoordinator coordinator;
    
    private async void Start()
    {
        coordinator = GameModule.AUDIO_LYRIC;
        await coordinator.Initialize(audioReactor);
        
        // 订阅播放完成事件，自动播放下一首
        coordinator.OnPlaybackStopped += PlayNext;
        
        // 开始播放第一首
        await PlayCurrent();
    }
    
    private async UniTask PlayCurrent()
    {
        if (currentIndex >= 0 && currentIndex < playlist.Count)
        {
            var item = playlist[currentIndex];
            coordinator.SetSyncOffset(item.syncOffset);
            
            // 使用新的分步API
            bool prepareSuccess = await coordinator.PrepareAudioAndLyrics(item.audioClip, item.lrcContent);
            if (prepareSuccess)
            {
                await coordinator.PlaySynchronized(
                    Vector3.zero,    // 歌词显示位置
                    item.effectId,   // 特效ID
                    null,           // 布局ID
                    0.1f            // 开始延迟
                );
            }
        }
    }
    
    private async void PlayNext()
    {
        currentIndex = (currentIndex + 1) % playlist.Count;
        await PlayCurrent();
    }
}
```

#### 2. 实时音频可视化
```csharp
public class AudioVisualizer : MonoBehaviour
{
    public LineRenderer spectrumRenderer;
    public Transform[] beatCubes;
    
    private AudioLyricCoordinator coordinator;
    
    private void Start()
    {
        coordinator = GameModule.AUDIO_LYRIC;
        coordinator.OnAudioDataReceived += UpdateVisualization;
    }
    
    private void UpdateVisualization(float rms, float[] spectrum)
    {
        // 更新频谱显示
        UpdateSpectrumDisplay(spectrum);
        
        // 更新节拍立方体
        UpdateBeatCubes(rms);
    }
    
    private void UpdateSpectrumDisplay(float[] spectrum)
    {
        if (spectrum == null || spectrumRenderer == null) return;
        
        spectrumRenderer.positionCount = spectrum.Length;
        for (int i = 0; i < spectrum.Length; i++)
        {
            Vector3 pos = new Vector3(i * 0.1f, spectrum[i] * 10f, 0);
            spectrumRenderer.SetPosition(i, pos);
        }
    }
    
    private void UpdateBeatCubes(float rms)
    {
        float scale = 1f + rms * 2f;
        foreach (var cube in beatCubes)
        {
            if (cube != null)
            {
                cube.localScale = Vector3.one * scale;
            }
        }
    }
}
```

## API 方法详解

### 新增方法（推荐使用）

#### PrepareAudioAndLyrics
```csharp
public async UniTask<bool> PrepareAudioAndLyrics(AudioClip audioClip, string lrcContent)
```
**功能**: 准备音频和歌词资源，进行预处理和验证

**参数**:
- `audioClip`: 音频剪辑对象
- `lrcContent`: LRC格式的歌词内容

**返回值**: 准备是否成功

**使用场景**:
- 在播放前预先验证资源
- 支持资源的预加载和缓存
- 提供更好的错误处理

#### PlaySynchronized
```csharp
public async UniTask<bool> PlaySynchronized(Vector3? lyricPosition = null, string effectId = null, string layoutId = null, float startDelay = 0.1f)
```
**功能**: 使用已准备的资源开始同步播放

**参数**:
- `lyricPosition`: 歌词显示位置（可选，默认Vector3.zero）
- `effectId`: 歌词特效ID（可选）
- `layoutId`: 歌词布局ID（可选）
- `startDelay`: 音频开始延迟秒数（可选，默认0.1秒）

**返回值**: 播放是否成功启动

**使用场景**:
- 需要在播放前准备资源的场景
- 支持多次播放同一资源
- 提供更精细的播放控制

#### Stop (异步版本)
```csharp
public async UniTask Stop()
```
**功能**: 异步停止当前播放

**使用场景**:
- 需要等待停止操作完成的场景
- 在异步流程中优雅地停止播放

#### Reset
```csharp
public void Reset()
```
**功能**: 重置协调器状态，用于切换歌曲时的清理

**使用场景**:
- 在同一场景内切换不同歌曲时使用
- 停止当前播放，清理播放状态，但保持模块引用和已发现的AudioReactor

#### FullReset
```csharp
public void FullReset()
```
**功能**: 完全重置协调器，用于场景切换时的完全清理

**使用场景**:
- 场景切换时使用，重置后需要重新初始化
- 重置所有状态，包括已发现的AudioReactor和初始化状态

#### SwitchSong
```csharp
public async UniTask<bool> SwitchSong(AudioClip audioClip, string lrcContent, Vector3? lyricPosition = null, string effectId = null, string layoutId = null, float startDelay = 0.1f)
```
**功能**: 快速切换歌曲，在同一场景内切换不同歌曲时使用

**参数**:
- `audioClip`: 新的音频剪辑
- `lrcContent`: 新的歌词内容
- `lyricPosition`: 歌词显示位置（可选）
- `effectId`: 特效ID（可选）
- `layoutId`: 布局ID（可选）
- `startDelay`: 开始延迟，默认0.1秒

**返回值**: 是否切换成功

**使用场景**:
- 一键式歌曲切换，自动处理重置和新歌曲播放

### 兼容方法

#### PlayWithSync (保持兼容)
```csharp
public async UniTask<bool> PlayWithSync(AudioClip audioClip, string lrcContent, Vector3 lyricPosition, string effectId = null, string layoutId = null, float audioStartDelay = 0.1f)
```
**功能**: 一步式播放音频和歌词（兼容旧版本）

**推荐**: 新项目建议使用 `PrepareAudioAndLyrics` + `PlaySynchronized` 的组合方式

## 配置选项

### 同步偏移设置
```csharp
// 歌词提前显示（负值表示延迟）
coordinator.SetSyncOffset(-0.5f); // 歌词延迟0.5秒
coordinator.SetSyncOffset(0.3f);  // 歌词提前0.3秒
```

### 调试模式
```csharp
// 启用详细日志输出
coordinator.EnableDebugger(true);
```

### 音频启动延迟
```csharp
// 在PlayWithSync中设置音频启动延迟
await coordinator.PlayWithSync(
    audioClip, lrcContent, position,
    effectId, layoutId,
    0.2f  // 音频延迟0.2秒启动，给歌词系统预留初始化时间
);
```

## 最佳实践

### API 使用建议

1. **优先使用新API**: 新项目建议使用 `PrepareAudioAndLyrics` + `PlaySynchronized` 组合
   ```csharp
   // 推荐方式
   bool prepared = await coordinator.PrepareAudioAndLyrics(audioClip, lrcContent);
   if (prepared)
   {
       await coordinator.PlaySynchronized(position, effectId, layoutId);
   }
   ```

2. **资源预处理**: 利用 `PrepareAudioAndLyrics` 进行资源验证和预处理
   ```csharp
   // 在播放前验证资源
   if (await coordinator.PrepareAudioAndLyrics(clip, lyrics))
   {
       // 资源有效，可以安全播放
       await coordinator.PlaySynchronized();
   }
   else
   {
       // 处理资源无效的情况
       Debug.LogError("音频或歌词资源无效");
   }
   ```

3. **异步停止**: 在需要等待停止完成的场景使用异步版本
   ```csharp
   // 在切换歌曲前等待当前播放完全停止
   await coordinator.Stop();
   await PlayNextSong();
   ```

4. **歌曲切换策略**: 根据不同场景选择合适的切换方式
   ```csharp
   // 方式1：使用SwitchSong一键切换（推荐）
   bool success = await coordinator.SwitchSong(newAudioClip, newLrcContent);
   
   // 方式2：手动重置后播放
   coordinator.Reset();
   bool prepared = await coordinator.PrepareAudioAndLyrics(newAudioClip, newLrcContent);
   if (prepared)
   {
       await coordinator.PlaySynchronized();
   }
   ```

5. **场景切换处理**: 正确处理场景切换时的重置
   ```csharp
   // 场景切换前完全重置
   coordinator.FullReset();
   
   // 新场景中重新初始化
   bool initSuccess = await coordinator.AutoInitialize();
   ```

6. **错误处理**: 充分利用返回值进行错误处理
   ```csharp
   try
   {
       bool prepared = await coordinator.PrepareAudioAndLyrics(audioClip, lrcContent);
       if (!prepared)
       {
           Debug.LogError("资源准备失败");
           return;
       }
       
       bool started = await coordinator.PlaySynchronized();
       if (!started)
       {
           Debug.LogError("播放启动失败");
       }
   }
   catch (System.Exception ex)
   {
       Debug.LogError($"播放过程中发生异常: {ex.Message}");
   }
   ```

## 性能优化建议

1. **合理设置更新频率**: 避免过于频繁的音频数据更新
2. **使用对象池**: 对于频繁创建的歌词对象使用对象池
3. **异步操作**: 利用UniTask进行异步操作，避免阻塞主线程
4. **事件订阅管理**: 及时取消不需要的事件订阅，避免内存泄漏
5. **资源预加载**: 使用 `PrepareAudioAndLyrics` 提前准备资源，减少播放时的延迟

## 错误处理

协调器内置了完善的错误处理机制：

- 初始化失败时会返回false并记录错误日志
- 播放失败时会自动清理资源并通知外部
- 异常情况下会自动停止播放并释放资源

## 扩展性

协调器设计支持以下扩展：

1. **新增音频效果**: 通过AudioSyncModule添加新的音频反应组件
2. **新增歌词特效**: 通过LyricFXModule添加新的歌词显示效果
3. **自定义同步逻辑**: 在协调器中添加特定的同步算法
4. **多媒体支持**: 可扩展支持视频、动画等多媒体内容

## 注意事项

1. 确保在使用前正确初始化协调器
2. 及时清理事件订阅，避免内存泄漏
3. 在播放新内容前停止当前播放
4. 合理设置同步偏移，确保音频和歌词同步
5. 在生产环境中关闭调试模式以提高性能

## 故障排除

### 常见问题

1. **初始化失败**
   - 检查AudioReactor和AudioSourcePlus是否正确设置
   - 确认相关模块是否已正确注册到GameModule

2. **PrepareAudioAndLyrics 失败**
   - 确认协调器已正确初始化
   - 检查AudioClip是否为null
   - 验证LRC内容是否为空或格式错误
   - 查看控制台错误日志获取详细信息

3. **PlaySynchronized 失败**
   - 确认已先调用PrepareAudioAndLyrics
   - 检查音频资源是否能从Resources文件夹正确加载
   - 验证歌词内容是否已正确准备

4. **播放不同步**
   - 调整syncOffset参数
   - 检查startDelay设置（PlaySynchronized的参数）
   - 确认LRC文件格式正确

5. **Stop方法调用问题**
   - 使用异步版本：`await coordinator.Stop()`
   - 或使用同步版本：`coordinator.StopAll()`
   - 确保在正确的上下文中调用

6. **SwitchSong 切换失败**
   - 确认协调器已正确初始化
   - 检查新的音频剪辑和歌词内容是否有效
   - 验证当前播放状态是否正常

7. **Reset 后无法播放**
   - Reset 只清理播放状态，不影响初始化状态
   - 确认 AudioReactor 仍然有效
   - 检查是否需要重新准备资源

8. **FullReset 后初始化失败**
   - FullReset 会清理所有状态，需要重新初始化
   - 重新调用 AutoInitialize 或 Initialize
   - 确认场景中存在有效的 AudioReactor 组件

9. **事件不触发**
   - 确认事件订阅在初始化之后
   - 检查是否有异常导致事件处理中断

10. **性能问题**
    - 减少音频数据更新频率
    - 关闭不必要的调试输出
    - 优化歌词特效复杂度
    - 使用PrepareAudioAndLyrics预加载资源

通过采用协调器模式，我们实现了音频同步和歌词播放的统一管理，提供了良好的可维护性、扩展性和用户体验。