using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.Rendering;
using Cysharp.Threading.Tasks;


namespace GameLogic
{
    /// <summary>
    /// XR后处理效果管理器
    /// </summary>
    public class XRPostProcessManager
    {
        // 全局Volume引用
        private Volume _globalVolume;
        
        // 预加载的配置文件
        private Dictionary<string, VolumeProfile> _profiles = new Dictionary<string, VolumeProfile>();
        
        // 是否已初始化
        private bool _initialized = false;
        
        // 配置文件资源路径
        private const string PROFILES_PATH = "PostProcessing";
        
        /// <summary>
        /// 初始化后处理管理器
        /// </summary>
        public void Initialize(Camera xrCamera)
        {
            if (_initialized) return;
            
            if (xrCamera == null)
            {
                Log.Warning("无法初始化后处理管理器：XR相机为空");
                return;
            }
            
            // 查找Volume组件
            _globalVolume = xrCamera.GetComponentInChildren<Volume>();
            
            if (_globalVolume == null)
            {
                // 尝试在相机上创建Volume
                CreateVolumeForCamera(xrCamera);
            }
            
            // 加载可用的配置文件
            LoadProfiles();
            
            _initialized = true;
            Log.Info("XR后处理管理器初始化完成");
        }
        
        /// <summary>
        /// 为相机创建Volume组件
        /// </summary>
        private void CreateVolumeForCamera(Camera camera)
        {
            GameObject volumeObject = new GameObject("Global Volume");
            volumeObject.transform.SetParent(camera.transform);
            volumeObject.transform.localPosition = Vector3.zero;
            
            _globalVolume = volumeObject.AddComponent<Volume>();
            _globalVolume.isGlobal = true;
            
            // 创建默认配置文件
            _globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            
            Log.Info("已为XR相机创建Volume组件");
        }
        
        /// <summary>
        /// 加载所有可用的配置文件
        /// </summary>
        private void LoadProfiles()
        {
            _profiles.Clear();
            
            // 从Resources加载所有配置文件
            VolumeProfile[] profiles = Resources.LoadAll<VolumeProfile>(PROFILES_PATH);
            
            if (profiles != null && profiles.Length > 0)
            {
                foreach (var profile in profiles)
                {
                    _profiles[profile.name] = profile;
                    Log.Info($"已加载后处理配置文件: {profile.name}");
                }
            }
            else
            {
                Log.Warning($"无法从Resources/{PROFILES_PATH}加载配置文件");
            }
        }
        
        /// <summary>
        /// 切换到指定的配置文件
        /// </summary>
        public bool SwitchProfile(string profileName)
        {
            if (!_initialized || _globalVolume == null)
            {
                Log.Warning("后处理管理器未初始化");
                return false;
            }
            
            if (_profiles.TryGetValue(profileName, out VolumeProfile profile))
            {
                _globalVolume.profile = profile;
                Log.Info($"已切换后处理配置文件: {profileName}");
                return true;
            }
            
            Log.Warning($"未找到后处理配置文件: {profileName}");
            return false;
        }
        
        /// <summary>
        /// 启用或禁用全局后处理效果
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (_globalVolume != null)
            {
                _globalVolume.enabled = enabled;
                Log.Info($"后处理效果已{(enabled ? "启用" : "禁用")}");
            }
        }
        
        /// <summary>
        /// 获取当前配置文件名称
        /// </summary>
        public string GetCurrentProfileName()
        {
            if (_globalVolume != null && _globalVolume.profile != null)
            {
                return _globalVolume.profile.name;
            }
            return null;
        }
        
        /// <summary>
        /// 调整后处理效果的权重
        /// </summary>
        public void SetVolumeWeight(float weight)
        {
            if (_globalVolume != null)
            {
                _globalVolume.weight = Mathf.Clamp01(weight);
                Log.Info($"已设置后处理权重: {weight}");
            }
        }
        
        /// <summary>
        /// 平滑过渡到指定配置文件
        /// </summary>
        public async UniTaskVoid TransitionToProfile(string profileName, float duration)
        {
            if (!_initialized || _globalVolume == null || !_profiles.ContainsKey(profileName))
            {
                Log.Warning($"无法过渡到配置文件 {profileName}");
                return;
            }
            
            // 创建临时Volume用于过渡
            GameObject tempObject = new GameObject("Transition Volume");
            Volume transitionVolume = tempObject.AddComponent<Volume>();
            transitionVolume.isGlobal = true;
            transitionVolume.weight = 0;
            transitionVolume.profile = _profiles[profileName];
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transitionVolume.weight = t;
                _globalVolume.weight = 1 - t;
                
                await UniTask.Yield();
            }
            
            // 完成过渡
            _globalVolume.profile = _profiles[profileName];
            _globalVolume.weight = 1;
            
            GameObject.Destroy(tempObject);
            Log.Info($"已平滑过渡到配置文件: {profileName}");
        }
        
#if UNITY_EDITOR || ENABLE_XR
        /// <summary>
        /// 调整特定后处理效果参数
        /// </summary>
        /// <typeparam name="T">后处理效果类型</typeparam>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">参数值</param>
        public void AdjustEffectParameter<T>(string parameterName, float value) where T : VolumeComponent
        {
            if (!_initialized || _globalVolume == null || _globalVolume.profile == null)
                return;
                
            if (_globalVolume.profile.TryGet<T>(out var component))
            {
                var field = typeof(T).GetField(parameterName);
                if (field != null)
                {
                    var parameter = field.GetValue(component);
                    
                    // 处理不同类型的参数
                    if (parameter is ClampedFloatParameter floatParam)
                    {
                        floatParam.value = value;
                        Log.Info($"已设置 {typeof(T).Name}.{parameterName} = {value}");
                    }
                    else if (parameter is BoolParameter boolParam)
                    {
                        boolParam.value = value >= 0.5f;
                        Log.Info($"已设置 {typeof(T).Name}.{parameterName} = {(value >= 0.5f)}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 启用或禁用特定后处理效果
        /// </summary>
        public void ToggleEffect<T>(bool enabled) where T : VolumeComponent
        {
            if (!_initialized || _globalVolume == null || _globalVolume.profile == null)
                return;
                
            if (_globalVolume.profile.TryGet<T>(out var component))
            {
                component.active = enabled;
                Log.Info($"后处理效果 {typeof(T).Name} 已{(enabled ? "启用" : "禁用")}");
            }
            else
            {
                Log.Warning($"后处理配置文件中不存在 {typeof(T).Name} 效果");
            }
        }
#endif
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Release()
        {
            _profiles.Clear();
            _globalVolume = null;
            _initialized = false;
            
            Log.Info("XR后处理管理器已释放");
        }
    }
}
