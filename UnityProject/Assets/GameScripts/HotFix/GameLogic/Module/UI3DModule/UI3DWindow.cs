using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 3D UIu7a97u53e3u57fau7c7bu3002
    /// </summary>
    public abstract class UI3DWindow : UIBase
    {
        private GameObject _panel;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;
        
        // u4e16u754cu7a7au95f4u4f4du7f6eu548cu65cbu8f6c
        private Vector3 _worldPosition;
        private Quaternion _worldRotation;
        
        // u7a97u53e3u540du79f0u548cu8d44u6e90u8defu5f84
        public string WindowName { private set; get; }
        public string AssetName { private set; get; }
        
        // u662fu5426u52a0u8f7du5b8cu6210
        public bool IsLoadDone { private set; get; }
        
        public Canvas Canvas => _canvas;
        public GraphicRaycaster GraphicRaycaster => _raycaster;
        
        public override UIType Type => UIType.Window;
        
        /// <summary>
        /// u7a97u53e3u4f4du7f6eu7ec4u4ef6u3002
        /// </summary>
        public override Transform transform => _panel?.transform;
        
        /// <summary>
        /// u7a97u53e3u77e9u9635u4f4du7f6eu7ec4u4ef6u3002
        /// </summary>
        public override RectTransform rectTransform => _panel?.transform as RectTransform;

        /// <summary>
        /// u7a97u53e3u7684u5b9eu4f8bu8d44u6e90u5bf9u8c61u3002
        /// </summary>
        public override GameObject gameObject => _panel;
        
        /// <summary>
        /// u8bbeu7f6eu4e16u754cu7a7au95f4u4f4du7f6e
        /// </summary>
        public void SetWorldPosition(Vector3 position)
        {
            _worldPosition = position;
            if (_panel != null)
            {
                _panel.transform.position = position;
            }
        }
        
        /// <summary>
        /// u8bbeu7f6eu4e16u754cu7a7au95f4u65cbu8f6c
        /// </summary>
        public void SetWorldRotation(Quaternion rotation)
        {
            _worldRotation = rotation;
            if (_panel != null)
            {
                _panel.transform.rotation = rotation;
            }
        }
        
        /// <summary>
        /// u8bbeu7f6eGameObject
        /// </summary>
        public void SetGameObject(GameObject go)
        {
            _panel = go;
            _canvas = _panel.GetComponent<Canvas>();
            _raycaster = _panel.GetComponent<GraphicRaycaster>();
            
            // u5e94u7528u4f4du7f6eu548cu65cbu8f6c
            _panel.transform.position = _worldPosition;
            _panel.transform.rotation = _worldRotation;
        }
        
        /// <summary>
        /// u5185u90e8u52a0u8f7du65b9u6cd5
        /// </summary>
        internal async UniTask InternalLoad(string assetName, Action<UIWindow> prepareCallback, bool async, params object[] userDatas)
        {
            // u8bbeu7f6eu57fau672cu5c5eu6027
            WindowName = GetType().FullName;
            AssetName = assetName ?? GetType().Name;
            _userDatas = userDatas;
            
            if (string.IsNullOrEmpty(assetName))
            {
                Log.Error($"UI3DWindow {WindowName} assetName is null or empty");
                return;
            }
            
            // u52a0u8f7dUIu9884u5236u4f53
            GameObject prefab = null;
            try 
            {
                if (async)
                {
                    prefab = await UI3DModule.Resource.LoadPrefabAsync(assetName);
                }
                else
                {
                    prefab = UI3DModule.Resource.LoadPrefab(assetName);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Load UI3D prefab failed: {assetName}, {e.Message}");
                return;
            }
            
            if (prefab == null)
            {
                Log.Error($"UI3D prefab load failed: {assetName}");
                return;
            }
            
            // u5b9eu4f8bu5316UI
            _panel = GameObject.Instantiate(prefab, UI3DModule.UIRoot);
            _panel.name = WindowName;
            
            // u8bbeu7f6eu4f4du7f6eu548cu65cbu8f6c
            _panel.transform.position = _worldPosition;
            _panel.transform.rotation = _worldRotation;
            
            // u83b7u53d6Canvasu7ec4u4ef6
            _canvas = _panel.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = _panel.AddComponent<Canvas>();
            }
            
            // u83b7u53d6GraphicRaycasteru7ec4u4ef6
            _raycaster = _panel.GetComponent<GraphicRaycaster>();
            if (_raycaster == null)
            {
                _raycaster = _panel.AddComponent<GraphicRaycaster>();
            }
            
            // u8bbeu7f6eu4e3au4e16u754cu7a7au95f4u6e32u67d3u6a21u5f0f
            _canvas.renderMode = RenderMode.WorldSpace;
            
            // u521du59cbu5316UI
            ScriptGenerator();
            BindMemberProperty();
            RegisterEvent();
            OnCreate();
            
            // u6807u8bb0u52a0u8f7du5b8cu6210
            IsLoadDone = true;
            
            // u8c03u7528u51c6u5907u5b8cu6210u56deu8c03
            prepareCallback?.Invoke(this);
        }
        
        /// <summary>
        /// u5185u90e8u66f4u65b0u65b9u6cd5
        /// </summary>
        internal void InternalUpdate()
        {
            OnUpdate();
        }
        
        /// <summary>
        /// u9500u6bc1u65b9u6cd5
        /// </summary>
        internal void InternalDestroy()
        {
            OnDestroy();
            if (_panel != null)
            {
                GameObject.Destroy(_panel);
                _panel = null;
            }
        }
        
        /// <summary>
        /// XRu60acu505cu8fdbu5165u4ea4u4e92
        /// </summary>
        public virtual void OnXRHoverEnter()
        {
            // u5b50u7c7bu5b9eu73b0u60acu505cu6548u679c
        }
        
        /// <summary>
        /// XRu60acu505cu79bbu5f00u4ea4u4e92
        /// </summary>
        public virtual void OnXRHoverExit()
        {
            // u5b50u7c7bu5b9eu73b0u60acu505cu79bbu5f00u6548u679c
        }
        
        /// <summary>
        /// XRu9009u62e9u4ea4u4e92
        /// </summary>
        public virtual void OnXRSelect()
        {
            // u5b50u7c7bu5b9eu73b0u9009u62e9u6548u679c
        }
        
        /// <summary>
        /// XRu9009u62e9u79bbu5f00u4ea4u4e92
        /// </summary>
        public virtual void OnXRSelectExit()
        {
            // u5b50u7c7bu5b9eu73b0u9009u62e9u79bbu5f00u6548u679c
        }
    }
}
