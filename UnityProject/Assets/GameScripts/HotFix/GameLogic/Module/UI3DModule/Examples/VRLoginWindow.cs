using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR || ENABLE_XR
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace GameLogic
{
    /// <summary>
    /// VR登录界面示例
    /// </summary>
    [SceneUI("UI3D/VRLoginWindow", grabbable: true, interactionMode: UI3DInteractionMode.RayBased)]
    public class VRLoginWindow : UI3DWindow
    {
        // 界面元素
        private InputField _usernameInput;
        private InputField _passwordInput;
        private Button _loginButton;
        private Button _cancelButton;
        private Toggle _rememberToggle;
        private Text _statusText;
        
        // 登录状态
        private bool _isLoggingIn = false;
        
        // 登录完成回调
        private Action<bool, string> _loginCallback;
        
        /// <summary>
        /// 创建后初始化
        /// </summary>
        public override void OnCreate(Transform parent, object[] userDatas)
        {
            base.OnCreate(parent, userDatas);
            
            // 提取回调
            if (userDatas != null && userDatas.Length > 0 && userDatas[0] is Action<bool, string> callback)
            {
                _loginCallback = callback;
            }
            
            // 找到并缓存UI元素
            FindUIElements();
            
            // 设置事件
            SetupEvents();
            
            // 初始化状态
            ResetStatus();
            
            // 添加虚拟键盘弹出事件
            AddVirtualKeyboardSupport();
        }
        
        /// <summary>
        /// 查找UI元素
        /// </summary>
        private void FindUIElements()
        {
            // 在预制体中寻找这些元素 - 实际项目中请根据预制体结构调整路径
            _usernameInput = transform.Find("Panel/UsernameInput").GetComponent<InputField>();
            _passwordInput = transform.Find("Panel/PasswordInput").GetComponent<InputField>();
            _loginButton = transform.Find("Panel/LoginButton").GetComponent<Button>();
            _cancelButton = transform.Find("Panel/CancelButton").GetComponent<Button>();
            _rememberToggle = transform.Find("Panel/RememberToggle").GetComponent<Toggle>();
            _statusText = transform.Find("Panel/StatusText").GetComponent<Text>();
            
            // 设置密码模式
            _passwordInput.contentType = InputField.ContentType.Password;
        }
        
        /// <summary>
        /// 设置事件
        /// </summary>
        private void SetupEvents()
        {
            // 登录按钮事件
            _loginButton.onClick.AddListener(OnLoginButtonClicked);
            
            // 取消按钮事件
            _cancelButton.onClick.AddListener(OnCancelButtonClicked);
            
            // 键盘回车事件
            _usernameInput.onEndEdit.AddListener(OnInputEndEdit);
            _passwordInput.onEndEdit.AddListener(OnInputEndEdit);
        }
        
        /// <summary>
        /// 重置状态
        /// </summary>
        private void ResetStatus()
        {
            _isLoggingIn = false;
            _statusText.text = "请输入账号密码";
            _statusText.color = Color.white;
            _loginButton.interactable = true;
            
            // 加载保存的用户名
            if (PlayerPrefs.HasKey("VRUsername"))
            {
                _usernameInput.text = PlayerPrefs.GetString("VRUsername");
                _rememberToggle.isOn = true;
            }
        }
        
        /// <summary>
        /// 登录按钮点击
        /// </summary>
        private async void OnLoginButtonClicked()
        {
            if (_isLoggingIn)
                return;
                
            string username = _usernameInput.text.Trim();
            string password = _passwordInput.text.Trim();
            
            // 输入验证
            if (string.IsNullOrEmpty(username))
            {
                ShowStatus("请输入用户名", Color.yellow);
                return;
            }
            
            if (string.IsNullOrEmpty(password))
            {
                ShowStatus("请输入密码", Color.yellow);
                return;
            }
            
            // 记住用户名
            if (_rememberToggle.isOn)
            {
                PlayerPrefs.SetString("VRUsername", username);
                PlayerPrefs.Save();
            }
            else if (PlayerPrefs.HasKey("VRUsername"))
            {
                PlayerPrefs.DeleteKey("VRUsername");
                PlayerPrefs.Save();
            }
            
            // 显示登录中
            _isLoggingIn = true;
            _loginButton.interactable = false;
            ShowStatus("登录中...", Color.white);
            
            // 模拟网络延迟
            await UniTask.Delay(1500);
            
            // 这里是模拟的登录成功逻辑
            // 实际应用中应该调用服务器API进行认证
            bool success = false;
            string message = "登录失败，请检查账号密码";
            
            // 模拟登录逻辑 - admin/admin 成功，其他组合失败
            if (username == "admin" && password == "admin")
            {
                success = true;
                message = "登录成功！";
                ShowStatus(message, Color.green);
                
                // 模拟成功后的操作
                await UniTask.Delay(1000); 
                
                // 回调登录结果
                _loginCallback?.Invoke(true, username);
                
                // 登录成功后关闭窗口
                GameModule.UI3D.CloseWindow<VRLoginWindow>();
                return;
            }
            else
            {
                ShowStatus(message, Color.red);
            }
            
            // 重置登录状态
            _isLoggingIn = false;
            _loginButton.interactable = true;
            
            // 调用登录回调
            _loginCallback?.Invoke(success, message);
        }
        
        /// <summary>
        /// 取消按钮点击
        /// </summary>
        private void OnCancelButtonClicked()
        {
            // 回调取消结果
            _loginCallback?.Invoke(false, "用户取消登录");
            
            // 关闭窗口
            GameModule.UI3D.CloseWindow<VRLoginWindow>();
        }
        
        /// <summary>
        /// 输入框结束编辑
        /// </summary>
        private void OnInputEndEdit(string text)
        {
            // 如果按了回车键，模拟点击登录按钮
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnLoginButtonClicked();
            }
        }
        
        /// <summary>
        /// 显示状态信息
        /// </summary>
        private void ShowStatus(string message, Color color)
        {
            _statusText.text = message;
            _statusText.color = color;
        }
        
        /// <summary>
        /// 添加虚拟键盘支持
        /// </summary>
        private void AddVirtualKeyboardSupport()
        {
#if UNITY_EDITOR || ENABLE_XR
            // 如果需要虚拟键盘，这里添加相关代码
            // 在实际项目中，建议实现一个3D虚拟键盘
            var inputModules = GameObject.FindObjectsOfType<XRUIInputModule>();
            if (inputModules.Length > 0)
            {
                // 有XR UI输入模块，可以添加更多的支持代码
                Debug.Log("XR UI Input Module found. VR keyboard support ready.");
            }
#endif
        }
        
        /// <summary>
        /// 每帧更新
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();
            
            // 一些持续的更新逻辑，如键盘交互状态等
        }
    }
}
