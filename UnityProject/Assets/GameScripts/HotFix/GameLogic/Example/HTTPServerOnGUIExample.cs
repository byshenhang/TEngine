using UnityEngine;
using TEngine;
using System;
using System.Collections.Generic;

namespace GameLogic.Example
{
    /// <summary>
    /// HTTP服务器OnGUI控制界面示例
    /// 使用Unity的OnGUI系统实现HTTP服务器的可视化控制
    /// </summary>
    public class HTTPServerOnGUIExample : MonoBehaviour
    {
        [Header("界面设置")]
        [SerializeField] private bool _showGUI = true;
        [SerializeField] private KeyCode _toggleGUIKey = KeyCode.F10;
        [SerializeField] private int _windowWidth = 400;
        [SerializeField] private int _windowHeight = 600;
        
        [Header("服务器设置")]
        [SerializeField] private int _serverPort = 8080;
        [SerializeField] private bool _autoStartOnAwake = false;
        
        private HTTPServerModule _httpServerModule;
        private Rect _windowRect;
        private Vector2 _scrollPosition;
        private List<string> _logMessages = new List<string>();
        private int _maxLogMessages = 50;
        
        // GUI样式
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _logStyle;
        
        // 配置参数
        private string _portInputField;
        private bool _autoStartToggle;
        private bool _corsToggle = true;
        
        // 状态信息
        private HTTPServerStatus _lastStatus;
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 1f;
        
        #region Unity生命周期
        
        /// <summary>
        /// 组件初始化
        /// </summary>
        private void Awake()
        {
            // 初始化窗口位置
            _windowRect = new Rect(50, 50, _windowWidth, _windowHeight);
            
            // 初始化输入字段
            _portInputField = _serverPort.ToString();
            _autoStartToggle = _autoStartOnAwake;
            
            // 获取HTTP服务器模块
            _httpServerModule = GameModule.HTTPServer;
            
            // 订阅事件
            SubscribeEvents();
            
            // 添加初始日志
            AddLogMessage("HTTP服务器OnGUI控制界面已初始化");
        }
        
        /// <summary>
        /// 开始时执行
        /// </summary>
        private void Start()
        {
            if (_autoStartOnAwake && _httpServerModule != null)
            {
                StartServer();
            }
        }
        
        /// <summary>
        /// 更新检查
        /// </summary>
        private void Update()
        {
            // 快捷键控制
            if (Input.GetKeyDown(_toggleGUIKey))
            {
                _showGUI = !_showGUI;
                AddLogMessage($"GUI界面 {(_showGUI ? "显示" : "隐藏")}");
            }
            
            // 定期更新状态
            if (Time.time - _lastUpdateTime > UPDATE_INTERVAL)
            {
                UpdateServerStatus();
                _lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// 绘制GUI界面
        /// </summary>
        private void OnGUI()
        {
            if (!_showGUI) return;
            
            // 初始化GUI样式
            InitializeGUIStyles();
            
            // 绘制主窗口
            _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "HTTP服务器控制面板", _boxStyle);
        }
        
        /// <summary>
        /// 组件销毁
        /// </summary>
        private void OnDestroy()
        {
            // 取消订阅事件
            UnsubscribeEvents();
        }
        
        #endregion
        
        #region GUI绘制方法
        
        /// <summary>
        /// 初始化GUI样式
        /// </summary>
        private void InitializeGUIStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }
            
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    normal = { textColor = Color.white }
                };
            }
            
            if (_textFieldStyle == null)
            {
                _textFieldStyle = new GUIStyle(GUI.skin.textField)
                {
                    fontSize = 11
                };
            }
            
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.window)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (_logStyle == null)
            {
                _logStyle = new GUIStyle(GUI.skin.textArea)
                {
                    fontSize = 10,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }
        }
        
        /// <summary>
        /// 绘制主窗口内容
        /// </summary>
        /// <param name="windowID">窗口ID</param>
        private void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            
            // 绘制标题
            GUILayout.Label("HTTP服务器管理", _titleStyle);
            GUILayout.Space(10);
            
            // 绘制服务器状态区域
            DrawServerStatusSection();
            GUILayout.Space(10);
            
            // 绘制控制按钮区域
            DrawControlButtonsSection();
            GUILayout.Space(10);
            
            // 绘制配置区域
            DrawConfigurationSection();
            GUILayout.Space(10);
            
            // 绘制日志区域
            DrawLogSection();
            
            GUILayout.EndVertical();
            
            // 使窗口可拖拽
            GUI.DragWindow();
        }
        
        /// <summary>
        /// 绘制服务器状态区域
        /// </summary>
        private void DrawServerStatusSection()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("服务器状态", _titleStyle);
            
            if (_lastStatus != null)
            {
                // 运行状态
                Color statusColor = _lastStatus.isRunning ? Color.green : Color.red;
                GUI.color = statusColor;
                GUILayout.Label($"状态: {(_lastStatus.isRunning ? "运行中" : "已停止")}", _labelStyle);
                GUI.color = Color.white;
                
                // 端口信息
                GUILayout.Label($"端口: {_lastStatus.port}", _labelStyle);
                
                // URL信息
                if (!string.IsNullOrEmpty(_lastStatus.localURL))
                {
                    GUILayout.Label($"本地URL: {_lastStatus.localURL}", _labelStyle);
                    
                    // 添加复制按钮
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("复制本地URL", _buttonStyle, GUILayout.Width(100)))
                    {
                        GUIUtility.systemCopyBuffer = _lastStatus.localURL;
                        AddLogMessage("本地URL已复制到剪贴板");
                    }
                    GUILayout.EndHorizontal();
                }
                
                // 局域网URL
                if (!string.IsNullOrEmpty(_lastStatus.lanURL))
                {
                    GUILayout.Label($"局域网URL: {_lastStatus.lanURL}", _labelStyle);
                    
                    // 添加复制按钮
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("复制局域网URL", _buttonStyle, GUILayout.Width(120)))
                    {
                        GUIUtility.systemCopyBuffer = _lastStatus.lanURL;
                        AddLogMessage("局域网URL已复制到剪贴板");
                    }
                    GUILayout.EndHorizontal();
                }
                
                // 上传路径
                if (!string.IsNullOrEmpty(_lastStatus.uploadPath))
                {
                    GUILayout.Label($"上传路径: {_lastStatus.uploadPath}", _labelStyle);
                }
                
                // 运行时间
                if (_lastStatus.isRunning)
                {
                    GUILayout.Label($"运行时间: {FormatUptime(_lastStatus.uptime)}", _labelStyle);
                }
            }
            else
            {
                GUILayout.Label("获取状态中...", _labelStyle);
            }
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制控制按钮区域
        /// </summary>
        private void DrawControlButtonsSection()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("服务器控制", _titleStyle);
            
            GUILayout.BeginHorizontal();
            
            // 启动按钮
            bool isRunning = _lastStatus?.isRunning ?? false;
            GUI.enabled = !isRunning;
            if (GUILayout.Button("启动服务器", _buttonStyle))
            {
                StartServer();
            }
            
            // 停止按钮
            GUI.enabled = isRunning;
            if (GUILayout.Button("停止服务器", _buttonStyle))
            {
                StopServer();
            }
            
            // 重启按钮
            GUI.enabled = isRunning;
            if (GUILayout.Button("重启服务器", _buttonStyle))
            {
                RestartServer();
            }
            
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // 其他控制按钮
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("清空日志", _buttonStyle))
            {
                ClearLogs();
            }
            
            if (GUILayout.Button("刷新状态", _buttonStyle))
            {
                UpdateServerStatus();
                AddLogMessage("状态已刷新");
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制配置区域
        /// </summary>
        private void DrawConfigurationSection()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("服务器配置", _titleStyle);
            
            // 端口配置
            GUILayout.BeginHorizontal();
            GUILayout.Label("端口:", _labelStyle, GUILayout.Width(60));
            _portInputField = GUILayout.TextField(_portInputField, _textFieldStyle, GUILayout.Width(80));
            if (GUILayout.Button("应用", _buttonStyle, GUILayout.Width(60)))
            {
                ApplyPortConfig();
            }
            GUILayout.EndHorizontal();
            
            // 自动启动配置
            GUILayout.BeginHorizontal();
            GUILayout.Label("自动启动:", _labelStyle, GUILayout.Width(80));
            _autoStartToggle = GUILayout.Toggle(_autoStartToggle, "", GUILayout.Width(20));
            GUILayout.EndHorizontal();
            
            // CORS配置
            GUILayout.BeginHorizontal();
            GUILayout.Label("启用CORS:", _labelStyle, GUILayout.Width(80));
            _corsToggle = GUILayout.Toggle(_corsToggle, "", GUILayout.Width(20));
            GUILayout.EndHorizontal();
            
            // 应用配置按钮
            if (GUILayout.Button("应用所有配置", _buttonStyle))
            {
                ApplyAllConfigs();
            }
            
            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制日志区域
        /// </summary>
        private void DrawLogSection()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("操作日志", _titleStyle);
            
            // 日志滚动区域
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
            
            string logText = string.Join("\n", _logMessages);
            GUILayout.TextArea(logText, _logStyle, GUILayout.ExpandHeight(true));
            
            GUILayout.EndScrollView();
            
            GUILayout.EndVertical();
        }
        
        #endregion
        
        #region 服务器控制方法
        
        /// <summary>
        /// 启动HTTP服务器
        /// </summary>
        private void StartServer()
        {
            if (_httpServerModule != null)
            {
                bool success = _httpServerModule.StartServer();
                AddLogMessage(success ? "服务器启动成功" : "服务器启动失败");
            }
            else
            {
                AddLogMessage("HTTP服务器模块未初始化");
            }
        }
        
        /// <summary>
        /// 停止HTTP服务器
        /// </summary>
        private void StopServer()
        {
            if (_httpServerModule != null)
            {
                _httpServerModule.StopServer();
                AddLogMessage("服务器已停止");
            }
        }
        
        /// <summary>
        /// 重启HTTP服务器
        /// </summary>
        private void RestartServer()
        {
            if (_httpServerModule != null)
            {
                bool success = _httpServerModule.RestartServer();
                AddLogMessage(success ? "服务器重启成功" : "服务器重启失败");
            }
        }
        
        /// <summary>
        /// 应用端口配置
        /// </summary>
        private void ApplyPortConfig()
        {
            if (int.TryParse(_portInputField, out int port))
            {
                if (port > 0 && port <= 65535)
                {
                    var config = new HTTPServerConfig
                    {
                        port = port,
                        autoStart = _autoStartToggle,
                        enableCORS = _corsToggle
                    };
                    
                    bool success = _httpServerModule?.UpdateConfig(config) ?? false;
                    AddLogMessage(success ? $"端口配置已更新为: {port}" : "端口配置更新失败");
                }
                else
                {
                    AddLogMessage("端口号必须在1-65535范围内");
                }
            }
            else
            {
                AddLogMessage("无效的端口号格式");
            }
        }
        
        /// <summary>
        /// 应用所有配置
        /// </summary>
        private void ApplyAllConfigs()
        {
            if (int.TryParse(_portInputField, out int port) && port > 0 && port <= 65535)
            {
                var config = new HTTPServerConfig
                {
                    port = port,
                    autoStart = _autoStartToggle,
                    enableCORS = _corsToggle
                };
                
                bool success = _httpServerModule?.UpdateConfig(config) ?? false;
                AddLogMessage(success ? "所有配置已应用" : "配置应用失败");
            }
            else
            {
                AddLogMessage("请检查端口号配置");
            }
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 订阅HTTP服务器事件
        /// </summary>
        private void SubscribeEvents()
        {
            if (_httpServerModule != null)
            {
                _httpServerModule.OnServerStatusChanged += OnServerStatusChanged;
                _httpServerModule.OnFileUploaded += OnFileUploaded;
                _httpServerModule.OnError += OnServerError;
            }
        }
        
        /// <summary>
        /// 取消订阅HTTP服务器事件
        /// </summary>
        private void UnsubscribeEvents()
        {
            if (_httpServerModule != null)
            {
                _httpServerModule.OnServerStatusChanged -= OnServerStatusChanged;
                _httpServerModule.OnFileUploaded -= OnFileUploaded;
                _httpServerModule.OnError -= OnServerError;
            }
        }
        
        /// <summary>
        /// 服务器状态改变事件处理
        /// </summary>
        /// <param name="isRunning">服务器是否运行中</param>
        private void OnServerStatusChanged(bool isRunning)
        {
            AddLogMessage($"服务器状态改变: {(isRunning ? "启动" : "停止")}");
            UpdateServerStatus();
        }
        
        /// <summary>
        /// 文件上传事件处理
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="fileCount">文件数量</param>
        private void OnFileUploaded(string fileName, int fileCount)
        {
            AddLogMessage($"文件上传: {fileName} (共{fileCount}个文件)");
        }
        
        /// <summary>
        /// 服务器错误事件处理
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        private void OnServerError(string errorMessage)
        {
            AddLogMessage($"错误: {errorMessage}");
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 更新服务器状态
        /// </summary>
        private void UpdateServerStatus()
        {
            if (_httpServerModule != null)
            {
                _lastStatus = _httpServerModule.GetServerStatus();
            }
        }
        
        /// <summary>
        /// 添加日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        private void AddLogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            
            _logMessages.Add(logEntry);
            
            // 限制日志数量
            if (_logMessages.Count > _maxLogMessages)
            {
                _logMessages.RemoveAt(0);
            }
            
            // 自动滚动到底部
            _scrollPosition.y = float.MaxValue;
            
            // 输出到Unity控制台
            Debug.Log($"HTTPServerGUI: {message}");
        }
        
        /// <summary>
        /// 清空日志
        /// </summary>
        private void ClearLogs()
        {
            _logMessages.Clear();
            AddLogMessage("日志已清空");
        }
        
        /// <summary>
        /// 格式化运行时间
        /// </summary>
        /// <param name="uptime">运行时间(秒)</param>
        /// <returns>格式化的时间字符串</returns>
        private string FormatUptime(float uptime)
        {
            TimeSpan time = TimeSpan.FromSeconds(uptime);
            if (time.TotalDays >= 1)
            {
                return $"{(int)time.TotalDays}天 {time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
            else
            {
                return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 显示/隐藏GUI界面
        /// </summary>
        /// <param name="show">是否显示</param>
        public void ShowGUI(bool show)
        {
            _showGUI = show;
            AddLogMessage($"GUI界面 {(show ? "显示" : "隐藏")}");
        }
        
        /// <summary>
        /// 切换GUI界面显示状态
        /// </summary>
        public void ToggleGUI()
        {
            ShowGUI(!_showGUI);
        }
        
        /// <summary>
        /// 设置窗口位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        public void SetWindowPosition(float x, float y)
        {
            _windowRect.x = x;
            _windowRect.y = y;
        }
        
        /// <summary>
        /// 设置窗口大小
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public void SetWindowSize(float width, float height)
        {
            _windowRect.width = width;
            _windowRect.height = height;
        }
        
        #endregion
    }
}