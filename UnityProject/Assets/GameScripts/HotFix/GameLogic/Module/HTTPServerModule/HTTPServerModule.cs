using System;
using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// HTTP服务器模块 - 提供HTTP服务的统一管理接口
    /// 支持远程控制服务器的启动、停止，以及状态监控
    /// </summary>
    public sealed class HTTPServerModule : Singleton<HTTPServerModule>, IUpdate
    {
        #region 事件定义
        
        /// <summary>
        /// 服务器状态改变事件
        /// </summary>
        public event Action<bool> OnServerStatusChanged;
        
        /// <summary>
        /// 文件上传事件
        /// </summary>
        public event Action<string, int> OnFileUploaded;
        
        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string> OnError;
        
        #endregion
        
        #region 私有字段
        
        private RuntimeHTTPServer _httpServer;
        private HTTPServerConfig _config;
        private bool _isInitialized = false;
        private float _statusCheckTimer = 0f;
        private const float STATUS_CHECK_INTERVAL = 1f; // 每秒检查一次状态
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsServerRunning => _httpServer != null && _httpServer.IsRunning;
        
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort => _config?.port ?? 8080;
        
        /// <summary>
        /// 服务器URL
        /// </summary>
        public string ServerURL => $"http://localhost:{ServerPort}/";
        
        /// <summary>
        /// 局域网URL
        /// </summary>
        public string LANURL => _httpServer?.GetLANURL() ?? "";
        
        /// <summary>
        /// 上传目录路径
        /// </summary>
        public string UploadPath => _httpServer?.GetUploadPath() ?? "";
        
        #endregion
        
        #region 生命周期
        
        /// <summary>
        /// 模块初始化
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();
            
            // 加载配置
            LoadConfig();
            
            // 创建HTTP服务器组件
            CreateHTTPServer();
            
            _isInitialized = true;
            Log.Info("HTTPServerModule initialized");
        }
        
        /// <summary>
        /// 模块更新
        /// </summary>
        public void Update()
        {
            if (!_isInitialized) return;
            
            // 定期检查服务器状态
            _statusCheckTimer += Time.deltaTime;
            if (_statusCheckTimer >= STATUS_CHECK_INTERVAL)
            {
                _statusCheckTimer = 0f;
                CheckServerStatus();
            }
        }
        
        /// <summary>
        /// 模块释放
        /// </summary>
        public override void Release()
        {
            StopServer();
            
            if (_httpServer != null)
            {
                UnityEngine.Object.DestroyImmediate(_httpServer.gameObject);
                _httpServer = null;
            }
            
            _isInitialized = false;
            base.Release();
            Log.Info("HTTPServerModule released");
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 启动HTTP服务器
        /// </summary>
        /// <returns>是否启动成功</returns>
        public bool StartServer()
        {
            if (!_isInitialized)
            {
                Log.Error("HTTPServerModule not initialized");
                return false;
            }
            
            if (IsServerRunning)
            {
                Log.Warning("HTTP Server is already running");
                return true;
            }
            
            try
            {
                _httpServer.StartServer();
                OnServerStatusChanged?.Invoke(true);
                Log.Info($"HTTP Server started on port {ServerPort}");
                return true;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Failed to start HTTP server: {ex.Message}";
                Log.Error(errorMsg);
                OnError?.Invoke(errorMsg);
                return false;
            }
        }
        
        /// <summary>
        /// 停止HTTP服务器
        /// </summary>
        public void StopServer()
        {
            if (!IsServerRunning) return;
            
            try
            {
                _httpServer.StopServer();
                OnServerStatusChanged?.Invoke(false);
                Log.Info("HTTP Server stopped");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error stopping HTTP server: {ex.Message}";
                Log.Error(errorMsg);
                OnError?.Invoke(errorMsg);
            }
        }
        
        /// <summary>
        /// 重启HTTP服务器
        /// </summary>
        /// <returns>是否重启成功</returns>
        public bool RestartServer()
        {
            StopServer();
            return StartServer();
        }
        
        /// <summary>
        /// 更新服务器配置
        /// </summary>
        /// <param name="config">新的配置</param>
        /// <returns>是否更新成功</returns>
        public bool UpdateConfig(HTTPServerConfig config)
        {
            if (config == null)
            {
                Log.Error("HTTPServerConfig is null");
                return false;
            }
            
            bool wasRunning = IsServerRunning;
            
            if (wasRunning)
            {
                StopServer();
            }
            
            _config = config;
            ApplyConfig();
            
            if (wasRunning)
            {
                return StartServer();
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取服务器状态信息
        /// </summary>
        /// <returns>服务器状态信息</returns>
        public HTTPServerStatus GetServerStatus()
        {
            return new HTTPServerStatus
            {
                isRunning = IsServerRunning,
                port = ServerPort,
                localURL = ServerURL,
                lanURL = LANURL,
                uploadPath = UploadPath,
                uptime = IsServerRunning ? Time.time : 0f
            };
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfig()
        {
            // 这里可以从配置文件或Resources加载配置
            // 暂时使用默认配置
            _config = new HTTPServerConfig
            {
                port = 8080,
                autoStart = false,
                enableCORS = true,
                maxUploadSize = 100 * 1024 * 1024, // 100MB
                allowedExtensions = new[] { ".mp3" }
            };
        }
        
        /// <summary>
        /// 创建HTTP服务器组件
        /// </summary>
        private void CreateHTTPServer()
        {
            // 创建一个专门的GameObject来承载RuntimeHTTPServer组件
            GameObject serverGO = new GameObject("HTTPServer");
            UnityEngine.Object.DontDestroyOnLoad(serverGO);
            
            _httpServer = serverGO.AddComponent<RuntimeHTTPServer>();
            ApplyConfig();
        }
        
        /// <summary>
        /// 应用配置到HTTP服务器
        /// </summary>
        private void ApplyConfig()
        {
            if (_httpServer == null || _config == null) return;
            
            _httpServer.port = _config.port;
            _httpServer.autoStart = _config.autoStart;
        }
        
        /// <summary>
        /// 检查服务器状态
        /// </summary>
        private void CheckServerStatus()
        {
            // 这里可以添加更多的状态检查逻辑
            // 比如检查端口是否被占用、网络连接状态等
        }

        public void OnUpdate()
        {
        }

        #endregion
    }
    
    #region 数据结构
    
    /// <summary>
    /// HTTP服务器配置
    /// </summary>
    [System.Serializable]
    public class HTTPServerConfig
    {
        [Header("基础设置")]
        public int port = 8080;
        public bool autoStart = false;
        public bool enableCORS = true;
        
        [Header("上传设置")]
        public long maxUploadSize = 100 * 1024 * 1024; // 100MB
        public string[] allowedExtensions = { ".mp3", ".wav", ".ogg" };
        
        [Header("安全设置")]
        public bool requireAuth = false;
        public string authToken = "";
    }
    
    /// <summary>
    /// HTTP服务器状态信息
    /// </summary>
    [System.Serializable]
    public class HTTPServerStatus
    {
        public bool isRunning;
        public int port;
        public string localURL;
        public string lanURL;
        public string uploadPath;
        public float uptime;
    }
    
    #endregion
}