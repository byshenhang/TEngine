using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// VRu767bu5f55u529fu80fdu793au4f8b - u5c55u793au5982u4f55u4f7fu7528VRLoginWindow
    /// </summary>
    public class VRLoginExample : MonoBehaviour
    {
        /// <summary>
        /// u6d4bu8bd5u6309u94ae
        /// </summary>
        [SerializeField]
        private GameObject _loginButtonObject;
        
        /// <summary>
        /// u767bu5f55u72b6u6001u663eu793a
        /// </summary>
        [SerializeField]
        private TMPro.TextMeshProUGUI _statusText;
        
        private void Start()
        {
            // u6dfbu52a0u70b9u51fbu4e8bu4ef6
            if (_loginButtonObject != null)
            {
                var button = _loginButtonObject.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.onClick.AddListener(OnLoginButtonClicked);
                }
#if UNITY_EDITOR || ENABLE_XR
                // u5982u679cu6709XRu4ea4u4e92u7ec4u4ef6uff0cu6dfbu52a0XRu4ea4u4e92u4e8bu4ef6
                var interactable = _loginButtonObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>();
                if (interactable != null)
                {
                    interactable.selectEntered.AddListener((args) => OnLoginButtonClicked());
                }
#endif
            }
            
            if (_statusText != null)
            {
                _statusText.text = "u70b9u51fbu6309u94aeu767bu5f55";
            }
        }
        
        /// <summary>
        /// u767bu5f55u6309u94aeu70b9u51fb
        /// </summary>
        private async void OnLoginButtonClicked()
        {
            // u663eu793a3Du767bu5f55u7a97u53e3
            await ShowLoginWindow();
        }
        
        /// <summary>
        /// u663eu793au767bu5f55u7a97u53e3
        /// </summary>
        private async UniTask ShowLoginWindow()
        {
            // u65b9u5f0f1uff1au5728u7528u6237u9762u524du663eu793au767bu5f55u7a97u53e3
            VRLoginWindow loginWindow = await GameModule.UI3D.ShowUI3DInFrontOfUser<VRLoginWindow>(
                distance: 1.5f, 
                userDatas: new object[] { new Action<bool, string>(OnLoginComplete) }
            );
            
            // u65b9u5f0f2uff1au5982u679cu60f3u5728u6307u5b9au4f4du7f6eu663eu793au7a97u53e3
            // Vector3 position = new Vector3(0, 1.6f, 2f);
            // Quaternion rotation = Quaternion.identity;
            // VRLoginWindow loginWindow = await GameModule.UI3D.ShowUI3D<VRLoginWindow>(
            //     position, 
            //     rotation, 
            //     userDatas: new object[] { new Action<bool, string>(OnLoginComplete) }
            // );
            
            // u65b9u5f0f3uff1au5982u679cu60f3u5728u9884u5148u8bbeu7f6eu7684u951au70b9u663eu793au7a97u53e3
            // VRLoginWindow loginWindow = await GameModule.UI3D.ShowUI3DAtAnchor<VRLoginWindow>(
            //     "LoginAnchor", 
            //     userDatas: new object[] { new Action<bool, string>(OnLoginComplete) }
            // );
            
            // u53efu4ee5u6839u636eu9700u8981u8c03u6574u7a97u53e3u7684u4ea4u4e92u6a21u5f0f
            if (loginWindow != null)
            {
                // u8bbeu7f6eu662fu5426u53efu6293u53d6
                loginWindow.SetGrabbable(true);
                
                // u8bbeu7f6eu4ea4u4e92u6a21u5f0f
                loginWindow.SetInteractionMode(UI3DInteractionMode.RayBased);
            }
        }
        
        /// <summary>
        /// u767bu5f55u5b8cu6210u56deu8c03
        /// </summary>
        private void OnLoginComplete(bool success, string message)
        {
            if (_statusText != null)
            {
                if (success)
                {
                    _statusText.text = $"u767bu5f55u6210u529fuff01u6b22u8fce {message}";
                    _statusText.color = Color.green;
                }
                else
                {
                    _statusText.text = $"u767bu5f55u5931u8d25: {message}";
                    _statusText.color = Color.red;
                }
            }
            
            // u767bu5f55u6210u529fu540eu7684u903bu8f91
            if (success)
            {
                // u8fd9u91ccu6dfbu52a0u767bu5f55u6210u529fu540eu7684u903bu8f91
                Debug.Log($"User logged in: {message}");
            }
        }
    }
}
