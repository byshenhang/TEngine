# UI3D框架设计文档

## 1. 框架概述

UI3D框架是一个专为VR/AR环境设计的3D用户界面系统，它提供了一套完整的解决方案，用于在3D空间中创建、管理和交互UI元素。该框架独立于TEngine的2D UI系统，专注于解决空间UI的特殊需求和挑战。

### 1.1 设计目标

- 提供直观的API，简化3D UI的创建和管理
- 支持多种交互模式，包括射线交互和直接触摸
- 无缝集成Unity XR交互工具包
- 支持基于场景设计的UI布局
- 高性能，针对VR环境优化

### 1.2 核心特性

- **模块化设计**：清晰的组件划分，便于扩展和维护
- **多种定位模式**：支持世界固定、用户相对、锚点绑定等多种定位方式
- **完整生命周期管理**：从创建到销毁的全流程控制
- **事件系统集成**：与Unity事件系统和XR交互系统集成
- **资源管理**：与TEngine资源模块集成，支持异步加载
- **锚点系统**：通过场景锚点快速定位UI元素

## 2. 框架架构

### 2.1 架构概览

UI3D框架采用模块化设计，主要包含以下组件：

```
UI3DModule (核心管理模块)
  ├── UI3DBase (基础类)
  │     └── UI3DWindow (窗口类)
  ├── UI3DAnchorPoint (锚点组件)
  └── SceneUIAttribute (场景UI特性)
```

### 2.2 核心流程

1. **初始化**：UI3DModule在游戏启动时初始化，创建UI根节点，设置XR事件系统
2. **锚点扫描**：扫描场景中的UI3DAnchorPoint组件并注册
3. **窗口创建**：通过API创建UI窗口，可以指定位置或绑定到锚点
4. **交互处理**：窗口可以响应XR控制器的射线交互或直接触摸
5. **生命周期管理**：管理窗口的显示、隐藏和销毁

## 3. 核心组件

### 3.1 UI3DModule

UI3D系统的核心管理模块，负责初始化、窗口管理、锚点管理等。

主要职责：
- 管理3D UI的生命周期
- 处理锚点的注册和查找
- 创建和销毁UI窗口
- 配置XR事件系统
- 维护Canvas优化器

### 3.2 UI3DBase

所有3D UI元素的基类，定义了基本属性和生命周期方法。

关键方法：
- `OnCreate`：创建UI元素时调用
- `OnShow`：显示UI元素时调用
- `OnHide`：隐藏UI元素时调用
- `OnDestroy`：销毁UI元素时调用
- `OnUpdate`：每帧更新时调用

### 3.3 UI3DWindow

继承自UI3DBase，代表一个完整的3D UI窗口，具有交互能力。

主要特性：
- 支持多种定位模式（世界固定、用户相对、锚点绑定）
- 支持多种交互模式（射线交互、直接触摸）
- 可配置为可抓取，允许用户直接移动窗口
- 自动配置Canvas和事件系统
- 支持UI元素查找和事件绑定

### 3.4 UI3DAnchorPoint

场景组件，用于在设计时定义UI放置位置。

主要属性：
- `AnchorId`：锚点唯一标识
- `DefaultWindowType`：默认窗口类型
- `AutoCreate`：是否自动创建窗口
- `Priority`：优先级
- `AnchorGroup`：锚点分组

### 3.5 SceneUIAttribute

特性标记，用于配置UI窗口类。

```csharp
[SceneUI("UI3D/LoginWindow", grabbable: true, 
       interactionMode: UI3DInteractionMode.RayBased,
       positionMode: UI3DPositionMode.WorldFixed)]
public class LoginWindow : UI3DWindow
{
    // 窗口实现
}
```

## 4. 使用流程

### 4.1 创建UI窗口

#### 4.1.1 定义窗口类

```csharp
// 定义一个登录窗口
[SceneUI("UI3D/LoginWindow")]
public class LoginWindow : UI3DWindow
{
    private Button _loginButton;
    private TMP_InputField _usernameField;
    private TMP_InputField _passwordField;
    
    // 初始化UI元素
    protected override void FindUIElements()
    {
        _loginButton = transform.Find("LoginButton").GetComponent<Button>();
        _usernameField = transform.Find("UsernameField").GetComponent<TMP_InputField>();
        _passwordField = transform.Find("PasswordField").GetComponent<TMP_InputField>();
    }
    
    // 设置事件
    protected override void SetupEvents()
    {
        _loginButton.onClick.AddListener(OnLoginButtonClicked);
    }
    
    // 处理登录
    private void OnLoginButtonClicked()
    {
        string username = _usernameField.text;
        string password = _passwordField.text;
        
        // 处理登录逻辑
        Log.Info($"尝试登录: {username}");
        
        // 登录成功后关闭窗口
        Close();
    }
}
```

#### 4.1.2 创建窗口实例

##### 方法1：在用户前方创建

```csharp
// 在用户前方1.5米处创建登录窗口
var loginWindow = await GameModule.UI3D.CreateWindowInFrontOfUser<LoginWindow>(distance: 1.5f);
```

##### 方法2：在指定位置创建

```csharp
// 在指定世界坐标创建登录窗口
Vector3 position = new Vector3(0, 1.5f, 2);
Quaternion rotation = Quaternion.Euler(0, 180, 0);
var loginWindow = await GameModule.UI3D.CreateWindow<LoginWindow>(position, rotation);
```

##### 方法3：在锚点处创建

```csharp
// 在ID为"MainMenuAnchor"的锚点处创建登录窗口
var loginWindow = await GameModule.UI3D.CreateWindowAtAnchor<LoginWindow>("MainMenuAnchor");
```

### 4.2 在场景中放置锚点

1. 在Unity编辑器中创建空游戏对象
2. 添加`UI3DAnchorPoint`组件
3. 设置锚点ID、默认窗口类型和其他属性
4. 根据需要调整位置和旋转

如果设置了`AutoCreate`为true并指定了`DefaultWindowType`，该锚点会在启用时自动创建指定类型的窗口。

### 4.3 窗口管理

```csharp
// 获取已打开的窗口实例
var loginWindow = GameModule.UI3D.GetWindow<LoginWindow>();

// 关闭指定类型的窗口
GameModule.UI3D.CloseWindow<LoginWindow>();

// 关闭所有窗口
GameModule.UI3D.CloseAllWindows();
```

### 4.4 窗口定位模式

```csharp
// 设置为世界固定模式
window.SetPositionMode(UI3DPositionMode.WorldFixed);

// 设置为相对用户位置
window.SetRelativeToUser(new Vector3(0, 0, 1.5f), Quaternion.identity);

// 设置为锚点绑定模式
window.SetPositionMode(UI3DPositionMode.AnchorBased, anchorTransform);
```

## 5. 示例

### 5.1 VR登录示例

完整的VR登录界面实现：

```csharp
[SceneUI("UI3D/VRLoginWindow", grabbable: true)]
public class VRLoginWindow : UI3DWindow
{
    // UI元素
    private TMP_InputField _usernameInput;
    private TMP_InputField _passwordInput;
    private Toggle _rememberToggle;
    private Button _loginButton;
    private Button _closeButton;
    
    // 数据
    private string _username = "";
    private string _password = "";
    private bool _rememberMe = false;
    
    protected override void FindUIElements()
    {
        // 查找UI元素
        _usernameInput = transform.Find("Panel/UsernameInput").GetComponent<TMP_InputField>();
        _passwordInput = transform.Find("Panel/PasswordInput").GetComponent<TMP_InputField>();
        _rememberToggle = transform.Find("Panel/RememberToggle").GetComponent<Toggle>();
        _loginButton = transform.Find("Panel/LoginButton").GetComponent<Button>();
        _closeButton = transform.Find("TitleBar/CloseButton").GetComponent<Button>();
        
        // 加载保存的用户名
        _username = PlayerPrefs.GetString("RememberedUsername", "");
        _rememberMe = !string.IsNullOrEmpty(_username);
        
        // 设置初始值
        _usernameInput.text = _username;
        _rememberToggle.isOn = _rememberMe;
    }
    
    protected override void SetupEvents()
    {
        _usernameInput.onValueChanged.AddListener(OnUsernameChanged);
        _passwordInput.onValueChanged.AddListener(OnPasswordChanged);
        _rememberToggle.onValueChanged.AddListener(OnRememberMeChanged);
        _loginButton.onClick.AddListener(OnLoginClicked);
        _closeButton.onClick.AddListener(OnCloseClicked);
    }
    
    private void OnUsernameChanged(string value)
    {
        _username = value;
    }
    
    private void OnPasswordChanged(string value)
    {
        _password = value;
    }
    
    private void OnRememberMeChanged(bool value)
    {
        _rememberMe = value;
    }
    
    private void OnLoginClicked()
    {
        // 验证输入
        if (string.IsNullOrEmpty(_username))
        {
            ShowMessage("请输入用户名");
            return;
        }
        
        if (string.IsNullOrEmpty(_password))
        {
            ShowMessage("请输入密码");
            return;
        }
        
        // 模拟登录过程
        Log.Info($"尝试登录: {_username}");
        
        // 保存记住的用户名
        if (_rememberMe)
        {
            PlayerPrefs.SetString("RememberedUsername", _username);
        }
        else
        {
            PlayerPrefs.DeleteKey("RememberedUsername");
        }
        
        // 关闭窗口
        Close();
        
        // 这里可以添加登录成功后的逻辑
    }
    
    private void OnCloseClicked()
    {
        Close();
    }
    
    private void ShowMessage(string message)
    {
        // 显示消息，可以实现为一个简单的文本动画或弹窗
        Log.Info($"[LoginWindow] {message}");
    }
}
```

### 5.2 在场景中使用示例

```csharp
public class VRLoginExample : MonoBehaviour
{
    private void Start()
    {
        // 确保UI3DModule已初始化
        if (GameModule.UI3D == null)
        {
            Log.Error("UI3DModule not initialized!");
            return;
        }
        
        // 延迟2秒后显示登录窗口
        StartCoroutine(ShowLoginWindow());
    }
    
    private IEnumerator ShowLoginWindow()
    {
        yield return new WaitForSeconds(2f);
        
        // 在用户前方1.5米处创建登录窗口
        GameModule.UI3D.CreateWindowInFrontOfUser<VRLoginWindow>(distance: 1.5f).Forget();
        
        Log.Info("VR登录窗口已创建");
    }
}
```

## 6. 最佳实践与注意事项

### 6.1 性能优化

- **Canvas优化**：框架自动注册Canvas到Canvas优化器，提高远距离UI的性能
- **事件系统**：使用TrackedDeviceGraphicRaycaster而不是普通GraphicRaycaster以提高性能
- **碰撞体设计**：为可交互区域设计尽可能简单的碰撞体

### 6.2 交互设计

- **抓取区域**：将窗口的抓取区域限制在标题栏或边框，避免与内部UI元素冲突
- **输入字段**：使用TMP_InputField而不是传统InputField获得更好的VR兼容性
- **按钮大小**：在VR中设计更大的按钮，便于射线交互

### 6.3 资源管理

- **预制体路径**：使用SceneUIAttribute指定预制体路径，遵循"UI3D/窗口名称"的命名规则
- **异步加载**：所有窗口创建操作都是异步的，使用await或.Forget()处理返回值

### 6.4 锚点系统

- **锚点分组**：使用锚点分组功能组织不同类型的UI位置
- **自动创建**：对于常用UI，可以设置锚点自动创建窗口
- **优先级**：为相似位置的锚点设置不同优先级，控制窗口吸附行为

### 6.5 平台注意事项

- **Quest平台**：针对Oculus Quest等移动VR平台，已优化输入字段和软键盘处理
- **UWP平台**：在Mixed Reality环境中可能需要特殊处理输入

## 7. 扩展和定制

### 7.1 创建自定义交互模式

可以通过扩展UI3DWindow类实现自定义交互模式：

```csharp
public class CustomInteractionWindow : UI3DWindow
{
    protected override void SetupInteraction()
    {
        base.SetupInteraction();
        
        // 添加自定义交互组件和行为
    }
}
```

### 7.2 创建自定义Canvas优化器

对于性能要求高的应用，可以实现自定义Canvas优化器：

```csharp
public class CustomCanvasOptimizer : MonoBehaviour
{
    private List<Canvas> _managedCanvases = new List<Canvas>();
    
    public void RegisterCanvas(Canvas canvas)
    {
        if (!_managedCanvases.Contains(canvas))
        {
            _managedCanvases.Add(canvas);
        }
    }
    
    private void Update()
    {
        // 实现自定义优化逻辑
    }
}
```

## 8. 故障排除

### 8.1 常见问题

1. **窗口不显示**
   - 检查资源路径是否正确
   - 确认UI3DModule已正确初始化
   - 检查XR事件系统是否配置正确

2. **交互无响应**
   - 确认TrackedDeviceGraphicRaycaster组件已添加
   - 检查XRRig和交互器是否正确初始化
   - 验证事件系统配置

3. **输入字段问题**
   - 使用TMP_InputField而不是传统InputField
   - 检查软键盘处理代码是否适合目标平台

### 8.2 调试工具

```csharp
// 调试模式
UI3DModule.EnableDebugMode = true;

// 查看活动窗口
var windows = UI3DModule.Instance.GetAllWindows();
foreach (var window in windows)
{
    Log.Info($"Active window: {window.WindowName}");
}

// 查看锚点
var anchors = UI3DModule.Instance.GetAllAnchors();
foreach (var anchor in anchors)
{
    Log.Info($"Anchor: {anchor.AnchorId}, Group: {anchor.AnchorGroup}");
}
```
