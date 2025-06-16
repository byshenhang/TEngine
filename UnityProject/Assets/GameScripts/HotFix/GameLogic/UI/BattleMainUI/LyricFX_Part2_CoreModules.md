# LyricFX 框架设计文档 - 第2部分：核心模块设计

## 3. 核心模块设计

### 3.1 Core 模块

Core模块是整个框架的核心，负责管理歌词数据和协调各组件工作。

#### 3.1.1 LyricManager

`LyricManager`是整个系统的入口点和控制中心，负责初始化系统、加载歌词、协调字符创建和动画控制。

```csharp
public class LyricManager : MonoBehaviour
{
    [SerializeField] private LyricEffectPresetSO defaultPreset;
    [SerializeField] private TextAsset lyricFile;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject characterPrefab;
    
    private LyricContext _context;
    private ILyricParser _parser;
    private List<LyricLine> _lines = new List<LyricLine>();
    private CancellationTokenSource _cts;
    private GroupEffectController _groupController;
    
    // 初始化并开始播放
    public async UniTask InitializeAndPlay(TextAsset lyrics = null, LyricEffectPresetSO preset = null)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        // 创建全局上下文
        _context = new LyricContext();
        
        // 使用指定或默认资源
        var lyricAsset = lyrics ?? lyricFile;
        var effectPreset = preset ?? defaultPreset;
        
        // 解析歌词
        _parser = LyricParserFactory.CreateParser(lyricAsset.name);
        _lines = await _parser.ParseAsync(lyricAsset.text, _cts.Token);
        
        // 创建渲染器并应用效果
        await SetupLinesAndCharacters(effectPreset, _cts.Token);
        
        // 开始播放
        await PlayLyricSequence(_cts.Token);
    }
    
    // 其他方法...
}
```

#### 3.1.2 LyricData 结构

歌词数据结构定义了歌词的基本组织单位：

```csharp
// 歌词行
public class LyricLine
{
    public int Index { get; set; }
    public float StartTime { get; set; }
    public float EndTime { get; set; }
    public string Text { get; set; }
    public List<LyricCharacter> Characters { get; } = new List<LyricCharacter>();
    public Dictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
}

// 歌词字符
public class LyricCharacter
{
    public char Character { get; set; }
    public int Index { get; set; }
    public int LineIndex { get; set; }
    public Dictionary<string, object> UserData { get; } = new Dictionary<string, object>();
}

// 歌词序列
public class LyricSequence
{
    public List<LyricLine> Lines { get; } = new List<LyricLine>();
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }
    public float TotalDuration { get; set; }
}
```

#### 3.1.3 上下文系统

上下文系统提供了效果执行所需的完整信息环境：

```csharp
// 全局歌词上下文
public class LyricContext
{
    public LyricSequence Sequence { get; set; }
    public float CurrentTime { get; set; }
    public int CurrentLineIndex { get; set; }
    public LyricEffectPresetSO EffectPreset { get; set; }
    public TMProPool CharacterPool { get; set; }
    public Transform Container { get; set; }
    public Dictionary<string, object> SharedData { get; } = new Dictionary<string, object>();
}

// 字符级上下文
public class CharacterContext
{
    public LyricCharacter Character { get; }
    public ICharacterRenderer Renderer { get; }
    public TextMeshProUGUI TextComponent => Renderer.TextComponent;
    public CharacterState CurrentState { get; internal set; }
    public int GlobalIndex { get; }
    public int LineIndex { get; }
    public float NormalizedProgress { get; set; }
    public Dictionary<string, object> SharedData { get; } = new Dictionary<string, object>();
    
    public CharacterContext(LyricCharacter character, ICharacterRenderer renderer)
    {
        Character = character;
        Renderer = renderer;
        GlobalIndex = character.Index;
        LineIndex = character.LineIndex;
    }
    
    // 便捷访问方法
    public T GetOrCreateComponent<T>() where T : Component
    {
        return Renderer.GetOrCreateComponent<T>();
    }
}
```

#### 3.1.4 事件系统

```csharp
public static class LyricEventSystem
{
    // 事件委托类型
    public delegate void LyricEventHandler<T>(T eventData) where T : LyricEventBase;
    
    // 事件字典
    private static readonly Dictionary<Type, Delegate> _eventHandlers = 
        new Dictionary<Type, Delegate>();
    
    // 订阅事件
    public static void Subscribe<T>(LyricEventHandler<T> handler) where T : LyricEventBase
    {
        var type = typeof(T);
        
        if (_eventHandlers.ContainsKey(type))
        {
            _eventHandlers[type] = Delegate.Combine(_eventHandlers[type], handler);
        }
        else
        {
            _eventHandlers[type] = handler;
        }
    }
    
    // 取消订阅
    public static void Unsubscribe<T>(LyricEventHandler<T> handler) where T : LyricEventBase
    {
        var type = typeof(T);
        
        if (_eventHandlers.ContainsKey(type))
        {
            _eventHandlers[type] = Delegate.Remove(_eventHandlers[type], handler);
            
            if (_eventHandlers[type] == null)
            {
                _eventHandlers.Remove(type);
            }
        }
    }
    
    // 触发事件
    public static void RaiseEvent<T>(T eventData) where T : LyricEventBase
    {
        var type = typeof(T);
        
        if (_eventHandlers.ContainsKey(type))
        {
            ((LyricEventHandler<T>)_eventHandlers[type])?.Invoke(eventData);
        }
    }
}

// 基础事件类
public abstract class LyricEventBase { }

// 状态变化事件
public class CharacterStateChangedEvent : LyricEventBase
{
    public LyricCharacter Character { get; }
    public CharacterState OldState { get; }
    public CharacterState NewState { get; }
    
    public CharacterStateChangedEvent(LyricCharacter character, 
        CharacterState oldState, CharacterState newState)
    {
        Character = character;
        OldState = oldState;
        NewState = newState;
    }
}
```

### 3.2 Parser 模块

Parser模块负责解析各种格式的歌词文件：

```csharp
// 解析器接口
public interface ILyricParser
{
    UniTask<List<LyricLine>> ParseAsync(string content, CancellationToken token = default);
}

// 基础解析器
public abstract class BaseLyricParser : ILyricParser
{
    public abstract UniTask<List<LyricLine>> ParseAsync(string content, CancellationToken token = default);
    
    // 共享辅助方法
    protected LyricCharacter[] CreateCharactersFromText(string text, int lineIndex)
    {
        var characters = new LyricCharacter[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            characters[i] = new LyricCharacter
            {
                Character = text[i],
                Index = i,
                LineIndex = lineIndex
            };
        }
        return characters;
    }
}

// LRC格式解析器
public class LRCParser : BaseLyricParser
{
    private static readonly Regex _timeTagRegex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2})\]");
    
    public override async UniTask<List<LyricLine>> ParseAsync(string content, CancellationToken token = default)
    {
        var lines = new List<LyricLine>();
        var contentLines = content.Split('\n');
        
        int lineIndex = 0;
        
        foreach (var line in contentLines)
        {
            if (token.IsCancellationRequested)
                break;
                
            if (string.IsNullOrWhiteSpace(line) || !_timeTagRegex.IsMatch(line))
                continue;
                
            var match = _timeTagRegex.Match(line);
            
            if (match.Success)
            {
                int minutes = int.Parse(match.Groups[1].Value);
                int seconds = int.Parse(match.Groups[2].Value);
                int hundredths = int.Parse(match.Groups[3].Value);
                
                float startTime = minutes * 60 + seconds + hundredths / 100f;
                
                string text = line.Substring(match.Index + match.Length).Trim();
                
                var lyricLine = new LyricLine
                {
                    Index = lineIndex,
                    StartTime = startTime,
                    EndTime = startTime + 5.0f, // 默认5秒，后续处理会修正
                    Text = text
                };
                
                // 创建字符
                var characters = CreateCharactersFromText(text, lineIndex);
                lyricLine.Characters.AddRange(characters);
                
                lines.Add(lyricLine);
                lineIndex++;
            }
        }
        
        // 设置正确的结束时间
        for (int i = 0; i < lines.Count - 1; i++)
        {
            lines[i].EndTime = lines[i + 1].StartTime;
        }
        
        // 在工作线程上等待一帧，避免阻塞主线程
        await UniTask.Yield();
        
        return lines;
    }
}

// 解析器工厂
public static class LyricParserFactory
{
    public static ILyricParser CreateParser(string filename)
    {
        string extension = Path.GetExtension(filename).ToLower();
        
        switch (extension)
        {
            case ".lrc":
                return new LRCParser();
            case ".elrc":
                return new ExtendedLRCParser();
            default:
                return new LRCParser(); // 默认使用LRC解析器
        }
    }
}
```

### 3.3 States 模块

States模块管理字符的状态和状态转换：

```csharp
// 字符状态
public enum CharacterState
{
    Waiting,    // 等待显示
    Enter,      // 入场阶段
    Stay,       // 停留阶段
    Exit,       // 退场阶段
    Complete    // 完成显示
}

// 状态管理器
public class StateManager
{
    private CharacterState _currentState = CharacterState.Waiting;
    private Dictionary<CharacterState, List<StateTransition>> _transitions = new();
    
    public CharacterState CurrentState => _currentState;
    
    // 状态变化事件
    public event Action<CharacterState, CharacterState> OnStateChanged;
    
    // 添加状态转换条件
    public void AddTransition(CharacterState fromState, CharacterState toState, 
        Func<bool> condition = null)
    {
        if (!_transitions.ContainsKey(fromState))
            _transitions[fromState] = new List<StateTransition>();
            
        _transitions[fromState].Add(new StateTransition(toState, condition ?? (() => true)));
    }
    
    // 转换到新状态
    public async UniTask<bool> TransitionTo(CharacterState newState, CancellationToken token = default)
    {
        if (newState == _currentState) return false;
        
        var oldState = _currentState;
        _currentState = newState;
        
        OnStateChanged?.Invoke(oldState, newState);
        
        // 检查是否有自动转换
        if (_transitions.TryGetValue(newState, out var possibleTransitions))
        {
            // 查找可以自动转换的状态
            foreach (var transition in possibleTransitions)
            {
                if (await transition.ShouldTransition(token))
                {
                    await TransitionTo(transition.TargetState, token);
                    break;
                }
            }
        }
        
        return true;
    }
}

// 状态转换
public class StateTransition
{
    private Func<bool> _condition;
    private UniTask<bool> _asyncCondition;
    private bool _isAsyncCondition;
    
    public CharacterState TargetState { get; }
    
    public StateTransition(CharacterState targetState, Func<bool> condition)
    {
        TargetState = targetState;
        _condition = condition;
        _isAsyncCondition = false;
    }
    
    public StateTransition(CharacterState targetState, Func<UniTask<bool>> asyncCondition)
    {
        TargetState = targetState;
        _asyncCondition = asyncCondition();
        _isAsyncCondition = true;
    }
    
    public async UniTask<bool> ShouldTransition(CancellationToken token = default)
    {
        if (_isAsyncCondition)
            return await _asyncCondition.AttachExternalCancellation(token);
            
        return _condition();
    }
}
```
