# LyricFX 框架设计文档 - 第3部分：效果系统设计

## 4. 效果系统设计

效果系统是LyricFX最核心的部分，负责实现各种视觉效果并将它们应用到字符上。

### 4.1 核心效果组件

#### 4.1.1 BaseEffect

`BaseEffect`是所有效果的基类，定义了效果的基本接口和生命周期：

```csharp
public abstract class BaseEffect
{
    // 效果参数
    protected EffectParameters Parameters { get; private set; }
    
    // 效果事件
    public event Action<float> OnProgressChanged;
    public event Action OnEffectCompleted;
    
    public BaseEffect(EffectParameters parameters)
    {
        Parameters = parameters;
    }
    
    // 执行效果
    public abstract UniTask ExecuteAsync(TextMeshProUGUI target, 
        CharacterContext context, 
        CancellationToken cancellationToken);
    
    // 创建相反效果(用于Exit<->Enter)
    public abstract BaseEffect CreateReversed();
    
    // 获取当前效果值
    public abstract float GetCurrentValue();
    
    // 报告进度
    protected void ReportProgress(float progress)
    {
        OnProgressChanged?.Invoke(progress);
        
        if (Mathf.Approximately(progress, 1.0f))
            OnEffectCompleted?.Invoke();
    }
}
```

#### 4.1.2 EffectParameters

参数基类和示例实现：

```csharp
// 参数基类
public abstract class EffectParameters
{
    public float Duration { get; set; } = 1.0f;
    public AnimationCurve Curve { get; set; } = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool AutoReverse { get; set; } = false;
}

// 淡入淡出参数
public class FadeParameters : EffectParameters
{
    public float StartAlpha { get; set; } = 0.0f;
    public float EndAlpha { get; set; } = 1.0f;
}

// 缩放参数
public class ScaleParameters : EffectParameters
{
    public Vector3 StartScale { get; set; } = Vector3.zero;
    public Vector3 EndScale { get; set; } = Vector3.one;
}

// 模糊参数
public class BlurParameters : EffectParameters
{
    public float StartBlur { get; set; } = 30.0f;
    public float EndBlur { get; set; } = 0.0f;
}
```

#### 4.1.3 EffectChain

效果链用于组合多个效果：

```csharp
public class EffectChain
{
    private List<BaseEffect> _effects;
    
    public EffectChain(List<BaseEffect> effects)
    {
        _effects = effects;
    }
    
    // 并行执行所有效果
    public async UniTask ExecuteAsync(CharacterContext context, CancellationToken token = default)
    {
        if (_effects == null || _effects.Count == 0)
            return;
            
        try
        {
            // 创建任务列表
            var tasks = _effects.Select(effect => 
                effect.ExecuteAsync(context.TextComponent, context, token)).ToArray();
                
            // 并行执行所有效果
            await UniTask.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // 预期的取消，忽略
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error executing effect chain: {ex.Message}");
        }
    }
    
    // 串行执行效果
    public async UniTask ExecuteSerialAsync(CharacterContext context, CancellationToken token = default)
    {
        if (_effects == null || _effects.Count == 0)
            return;
            
        try
        {
            foreach (var effect in _effects)
            {
                await effect.ExecuteAsync(context.TextComponent, context, token);
                
                if (token.IsCancellationRequested)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // 预期的取消，忽略
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error executing effect chain serially: {ex.Message}");
        }
    }
}
```

### 4.2 效果适配器

效果适配器是连接字符、状态和效果的核心组件：

```csharp
public class EffectAdapter
{
    private LyricCharacter _character;
    private ICharacterRenderer _renderer;
    private StateManager _stateManager;
    private CancellationTokenSource _effectsCts;
    
    // 状态->效果映射
    private Dictionary<CharacterState, List<BaseEffect>> _stateEffects;
    
    // 当前活跃效果
    private List<BaseEffect> _activeEffects = new List<BaseEffect>();
    
    public CharacterState CurrentState => _stateManager.CurrentState;
    
    public EffectAdapter(LyricCharacter character, ICharacterRenderer renderer)
    {
        _character = character;
        _renderer = renderer;
        _stateManager = new StateManager();
        _stateEffects = new Dictionary<CharacterState, List<BaseEffect>>();
        
        // 设置默认转换路径
        SetupDefaultTransitions();
        
        // 订阅状态变化事件
        _stateManager.OnStateChanged += OnStateChanged;
    }
    
    // 设置默认转换路径
    private void SetupDefaultTransitions()
    {
        // 默认流程: Waiting -> Enter -> Stay -> Exit -> Complete
        _stateManager.AddTransition(CharacterState.Waiting, CharacterState.Enter);
        _stateManager.AddTransition(CharacterState.Enter, CharacterState.Stay);
        _stateManager.AddTransition(CharacterState.Stay, CharacterState.Exit);
        _stateManager.AddTransition(CharacterState.Exit, CharacterState.Complete);
    }
    
    // 状态变化时停止当前效果并启动新效果
    private void OnStateChanged(CharacterState oldState, CharacterState newState)
    {
        // 取消当前效果
        CancelActiveEffects();
        
        // 准备并运行新效果
        if (_stateEffects.TryGetValue(newState, out var effects) && effects.Count > 0)
        {
            _effectsCts = new CancellationTokenSource();
            ExecuteEffectChain(effects, _effectsCts.Token);
        }
        
        // 激活/禁用字符对象
        if (newState == CharacterState.Waiting || newState == CharacterState.Complete)
        {
            _renderer.SetActive(false);
        }
        else
        {
            _renderer.SetActive(true);
        }
    }
    
    // 执行效果链
    private async void ExecuteEffectChain(List<BaseEffect> effects, CancellationToken token)
    {
        _activeEffects.Clear();
        _activeEffects.AddRange(effects);
        
        // 构建效果上下文
        var context = CreateCharacterContext();
        
        // 执行效果链
        var effectChain = new EffectChain(effects);
        
        try
        {
            await effectChain.ExecuteAsync(context, token);
        }
        catch (OperationCanceledException)
        {
            // 预期中的取消
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error executing effects: {ex.Message}");
        }
    }
    
    // 构建字符上下文
    private CharacterContext CreateCharacterContext()
    {
        return new CharacterContext(_character, _renderer)
        {
            CurrentState = CurrentState
        };
    }
    
    // 转到指定状态
    public async UniTask TransitionTo(CharacterState state, CancellationToken token = default)
    {
        await _stateManager.TransitionTo(state, token);
    }
    
    // 配置特定状态的效果
    public void ConfigureEffects(CharacterState state, List<BaseEffect> effects)
    {
        _stateEffects[state] = effects;
    }
    
    // 获取当前特定类型的活跃效果
    public T GetActiveEffect<T>() where T : BaseEffect
    {
        return _activeEffects.OfType<T>().FirstOrDefault();
    }
    
    // 检查效果是否满足条件
    public bool MeetsCondition(string conditionKey)
    {
        switch (conditionKey)
        {
            case "blur_below_threshold":
                var blurEffect = GetActiveEffect<BlurEffect>();
                return blurEffect != null && blurEffect.GetCurrentValue() < 10f;
                
            case "effect_complete":
                return _activeEffects.Count > 0 && 
                       _activeEffects.All(e => Mathf.Approximately(e.GetCurrentValue(), 1.0f));
                       
            default:
                return true;
        }
    }
    
    // 清理资源
    public void Dispose()
    {
        CancelActiveEffects();
        _stateManager.OnStateChanged -= OnStateChanged;
    }
    
    private void CancelActiveEffects()
    {
        _effectsCts?.Cancel();
        _effectsCts?.Dispose();
        _effectsCts = null;
    }
}
```

### 4.3 具体效果实现

#### 4.3.1 FadeEffect (淡入淡出)

```csharp
public class FadeEffect : BaseEffect
{
    private FadeParameters _params;
    private float _currentAlpha;
    
    public FadeEffect(FadeParameters parameters) : base(parameters)
    {
        _params = parameters;
    }
    
    public override async UniTask ExecuteAsync(
        TextMeshProUGUI target, 
        CharacterContext context,
        CancellationToken token)
    {
        float startTime = Time.time;
        float elapsedTime = 0;
        
        _currentAlpha = _params.StartAlpha;
        
        // 设置初始透明度
        Color color = target.color;
        color.a = _currentAlpha;
        target.color = color;
        
        while (elapsedTime < _params.Duration)
        {
            if (token.IsCancellationRequested) break;
            
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / _params.Duration);
            float curveValue = _params.Curve.Evaluate(progress);
            
            // 计算当前透明度
            _currentAlpha = Mathf.Lerp(_params.StartAlpha, _params.EndAlpha, curveValue);
            
            // 应用到文本
            color = target.color;
            color.a = _currentAlpha;
            target.color = color;
            
            // 报告进度
            ReportProgress(progress);
            context.NormalizedProgress = progress;
            
            await UniTask.Yield();
        }
        
        // 设置最终状态
        color = target.color;
        color.a = _params.EndAlpha;
        target.color = color;
        
        _currentAlpha = _params.EndAlpha;
        ReportProgress(1.0f);
        context.NormalizedProgress = 1.0f;
    }
    
    public override BaseEffect CreateReversed()
    {
        var reversedParams = new FadeParameters
        {
            Duration = _params.Duration,
            Curve = _params.Curve,
            StartAlpha = _params.EndAlpha,
            EndAlpha = _params.StartAlpha
        };
        
        return new FadeEffect(reversedParams);
    }
    
    public override float GetCurrentValue()
    {
        return _currentAlpha;
    }
}
```

#### 4.3.2 BlurEffect (模糊效果)

```csharp
public class BlurEffect : BaseEffect
{
    private BlurParameters _params;
    private float _currentBlur;
    private BlurFilter _blurFilter;
    
    public BlurEffect(BlurParameters parameters) : base(parameters)
    {
        _params = parameters;
    }
    
    public override async UniTask ExecuteAsync(
        TextMeshProUGUI target,
        CharacterContext context,
        CancellationToken token)
    {
        // 获取或创建模糊滤镜组件
        _blurFilter = context.GetOrCreateComponent<BlurFilter>();
        
        if (_blurFilter == null)
        {
            Debug.LogError("BlurFilter component not found and could not be created");
            return;
        }
        
        float startTime = Time.time;
        float elapsedTime = 0;
        
        _currentBlur = _params.StartBlur;
        _blurFilter.Blur = _currentBlur;
        
        while (elapsedTime < _params.Duration)
        {
            if (token.IsCancellationRequested) break;
            
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / _params.Duration);
            float curveValue = _params.Curve.Evaluate(progress);
            
            // 计算当前模糊度
            _currentBlur = Mathf.Lerp(_params.StartBlur, _params.EndBlur, curveValue);
            
            // 应用到滤镜
            _blurFilter.Blur = _currentBlur;
            
            // 报告进度
            ReportProgress(progress);
            context.NormalizedProgress = progress;
            
            await UniTask.Yield();
        }
        
        // 设置最终状态
        _blurFilter.Blur = _params.EndBlur;
        _currentBlur = _params.EndBlur;
        
        ReportProgress(1.0f);
        context.NormalizedProgress = 1.0f;
    }
    
    public override BaseEffect CreateReversed()
    {
        var reversedParams = new BlurParameters
        {
            Duration = _params.Duration,
            Curve = _params.Curve,
            StartBlur = _params.EndBlur,
            EndBlur = _params.StartBlur
        };
        
        return new BlurEffect(reversedParams);
    }
    
    public override float GetCurrentValue()
    {
        return _currentBlur;
    }
}
```

### 4.4 组效果控制器

组效果控制器用于协调多个字符的效果执行：

```csharp
public class GroupEffectController
{
    private List<EffectAdapter> _adapters = new List<EffectAdapter>();
    
    // 组级别状态参数
    private float _groupProgress;
    private CancellationTokenSource _groupCts;
    
    // 组级事件
    public event Action<float> OnGroupProgressChanged;
    public event Action OnGroupCompleted;
    
    // 添加字符适配器
    public void AddAdapter(EffectAdapter adapter)
    {
        _adapters.Add(adapter);
    }
    
    // 按顺序激活效果
    public async UniTask ActivateInSequence(
        CharacterState state,
        SequenceOptions options,
        CancellationToken token)
    {
        _groupCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        
        try
        {
            int count = _adapters.Count;
            _groupProgress = 0f;
            
            for (int i = options.StartIndex; i < count; i += options.Step)
            {
                if (i >= count) break;
                
                // 如果需要等待上一个完成才继续
                if (options.WaitForCompletion && i > options.StartIndex)
                {
                    var prevAdapter = _adapters[i - options.Step];
                    await WaitForCondition(
                        () => prevAdapter.MeetsCondition(options.CompletionCondition),
                        _groupCts.Token);
                }
                
                // 如果设置了间隔等待
                if (options.Delay > 0 && i > options.StartIndex)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(options.Delay), 
                        cancellationToken: _groupCts.Token);
                }
                
                // 触发状态转换
                await _adapters[i].TransitionTo(state);
                
                // 更新组进度
                _groupProgress = (float)(i - options.StartIndex + 1) / 
                    ((count - options.StartIndex) / options.Step);
                OnGroupProgressChanged?.Invoke(_groupProgress);
            }
            
            _groupProgress = 1f;
            OnGroupCompleted?.Invoke();
        }
        finally
        {
            _groupCts?.Dispose();
            _groupCts = null;
        }
    }
    
    // 同时应用效果到所有字符
    public async UniTask ActivateAll(
        CharacterState state, 
        CancellationToken token)
    {
        var tasks = _adapters.Select(adapter => adapter.TransitionTo(state));
        await UniTask.WhenAll(tasks);
    }
    
    // 等待条件满足
    private async UniTask WaitForCondition(
        Func<bool> condition, 
        CancellationToken token)
    {
        await UniTask.WaitUntil(condition, PlayerLoopTiming.Update, token);
    }
    
    // 用于配置序列选项的结构
    public class SequenceOptions
    {
        public int StartIndex { get; set; } = 0;
        public int Step { get; set; } = 1;
        public float Delay { get; set; } = 0.1f;
        public bool WaitForCompletion { get; set; } = false;
        public string CompletionCondition { get; set; } = string.Empty;
    }
}
```
