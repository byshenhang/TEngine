using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 字幕资源管理器 - 负责管理字幕相关的资源加载和缓存
    /// </summary>
    public class SubtitleResourceManager : IDisposable
    {
        private readonly Dictionary<string, GameObject> _prefabCache;
        private readonly Dictionary<string, Font> _fontCache;
        private readonly Dictionary<string, Material> _materialCache;
        private readonly Dictionary<string, Texture2D> _textureCache;
        private readonly Dictionary<string, AudioClip> _audioCache;
        private readonly Dictionary<string, SubtitleData> _subtitleDataCache;
        private bool _isDisposed;
        
        /// <summary>
        /// 默认字体
        /// </summary>
        public Font DefaultFont { get; set; }
        
        /// <summary>
        /// 默认材质
        /// </summary>
        public Material DefaultMaterial { get; set; }
        
        /// <summary>
        /// 字幕预制体路径
        /// </summary>
        public string SubtitlePrefabPath { get; set; } = "UI/Subtitle/SubtitleCharacter";
        
        /// <summary>
        /// 字体资源路径
        /// </summary>
        public string FontResourcePath { get; set; } = "Fonts/";
        
        /// <summary>
        /// 材质资源路径
        /// </summary>
        public string MaterialResourcePath { get; set; } = "Materials/Subtitle/";
        
        /// <summary>
        /// 纹理资源路径
        /// </summary>
        public string TextureResourcePath { get; set; } = "Textures/Subtitle/";
        
        /// <summary>
        /// 音频资源路径
        /// </summary>
        public string AudioResourcePath { get; set; } = "Audio/Subtitle/";
        
        /// <summary>
        /// 字幕数据路径
        /// </summary>
        public string SubtitleDataPath { get; set; } = "Data/Subtitle/";
        
        public SubtitleResourceManager()
        {
            _prefabCache = new Dictionary<string, GameObject>();
            _fontCache = new Dictionary<string, Font>();
            _materialCache = new Dictionary<string, Material>();
            _textureCache = new Dictionary<string, Texture2D>();
            _audioCache = new Dictionary<string, AudioClip>();
            _subtitleDataCache = new Dictionary<string, SubtitleData>();
            
            InitializeDefaultResources();
        }
        
        /// <summary>
        /// 初始化默认资源
        /// </summary>
        private void InitializeDefaultResources()
        {
            // 设置默认字体
            DefaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            // 创建默认材质
            var shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader != null)
            {
                DefaultMaterial = new Material(shader);
                DefaultMaterial.name = "DefaultSubtitleMaterial";
            }
        }
        
        /// <summary>
        /// 异步加载字幕预制体
        /// </summary>
        public async UniTask<GameObject> LoadSubtitlePrefabAsync(string prefabName = null)
        {
            string path = string.IsNullOrEmpty(prefabName) ? SubtitlePrefabPath : $"{SubtitlePrefabPath}/{prefabName}";
            
            if (_prefabCache.TryGetValue(path, out var cachedPrefab))
            {
                return cachedPrefab;
            }
            
            try
            {
                // 使用TEngine的资源管理系统加载
                var prefab = await LoadResourceAsync<GameObject>(path);
                if (prefab != null)
                {
                    _prefabCache[path] = prefab;
                    Log.Debug($"[SubtitleResourceManager] 加载字幕预制体: {path}");
                }
                return prefab;
            }
            catch (Exception ex)
            {
                Log.Error($"[SubtitleResourceManager] 加载字幕预制体失败: {path}, 错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 异步加载字体
        /// </summary>
        public async UniTask<Font> LoadFontAsync(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
            {
                return DefaultFont;
            }
            
            if (_fontCache.TryGetValue(fontName, out var cachedFont))
            {
                return cachedFont;
            }
            
            string path = $"{FontResourcePath}{fontName}";
            
            try
            {
                var font = await LoadResourceAsync<Font>(path);
                if (font != null)
                {
                    _fontCache[fontName] = font;
                    Log.Debug($"[SubtitleResourceManager] 加载字体: {path}");
                }
                return font ?? DefaultFont;
            }
            catch (Exception ex)
            {
                Log.Error($"[SubtitleResourceManager] 加载字体失败: {path}, 错误: {ex.Message}");
                return DefaultFont;
            }
        }
        
        /// <summary>
        /// 异步加载材质
        /// </summary>
        public async UniTask<Material> LoadMaterialAsync(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
            {
                return DefaultMaterial;
            }
            
            if (_materialCache.TryGetValue(materialName, out var cachedMaterial))
            {
                return cachedMaterial;
            }
            
            string path = $"{MaterialResourcePath}{materialName}";
            
            try
            {
                var material = await LoadResourceAsync<Material>(path);
                if (material != null)
                {
                    _materialCache[materialName] = material;
                    Log.Debug($"[SubtitleResourceManager] 加载材质: {path}");
                }
                return material ?? DefaultMaterial;
            }
            catch (Exception ex)
            {
                Log.Error($"[SubtitleResourceManager] 加载材质失败: {path}, 错误: {ex.Message}");
                return DefaultMaterial;
            }
        }
        
        /// <summary>
        /// 异步加载纹理
        /// </summary>
        public async UniTask<Texture2D> LoadTextureAsync(string textureName)
        {
            if (string.IsNullOrEmpty(textureName))
            {
                return null;
            }
            
            if (_textureCache.TryGetValue(textureName, out var cachedTexture))
            {
                return cachedTexture;
            }
            
            string path = $"{TextureResourcePath}{textureName}";
            
            try
            {
                var texture = await LoadResourceAsync<Texture2D>(path);
                if (texture != null)
                {
                    _textureCache[textureName] = texture;
                    Log.Debug($"[SubtitleResourceManager] 加载纹理: {path}");
                }
                return texture;
            }
            catch (Exception ex)
            {
                Log.Error($"[SubtitleResourceManager] 加载纹理失败: {path}, 错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 异步加载音频
        /// </summary>
        public async UniTask<AudioClip> LoadAudioAsync(string audioName)
        {
            if (string.IsNullOrEmpty(audioName))
            {
                return null;
            }
            
            if (_audioCache.TryGetValue(audioName, out var cachedAudio))
            {
                return cachedAudio;
            }
            
            string path = $"{AudioResourcePath}{audioName}";
            
            try
            {
                var audio = await LoadResourceAsync<AudioClip>(path);
                if (audio != null)
                {
                    _audioCache[audioName] = audio;
                    Log.Debug($"[SubtitleResourceManager] 加载音频: {path}");
                }
                return audio;
            }
            catch (Exception ex)
            {
                Log.Error($"[SubtitleResourceManager] 加载音频失败: {path}, 错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 异步加载字幕数据
        /// </summary>
        public async UniTask<SubtitleData> LoadSubtitleDataAsync(string dataName)
        {
            if (string.IsNullOrEmpty(dataName))
            {
                return null;
            }
            
            if (_subtitleDataCache.TryGetValue(dataName, out var cachedData))
            {
                return cachedData;
            }
            
            string path = $"{SubtitleDataPath}{dataName}";
            
            try
            {
                var textAsset = await LoadResourceAsync<TextAsset>(path);
                if (textAsset != null)
                {
                    var subtitleData = JsonUtility.FromJson<SubtitleData>(textAsset.text);
                    if (subtitleData != null)
                    {
                        _subtitleDataCache[dataName] = subtitleData;
                        Log.Debug($"[SubtitleResourceManager] 加载字幕数据: {path}");
                        return subtitleData;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SubtitleResourceManager] 加载字幕数据失败: {path}, 错误: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 通用资源加载方法
        /// </summary>
        private async UniTask<T> LoadResourceAsync<T>(string path) where T : UnityEngine.Object
        {
            // 优先使用TEngine的资源管理系统
            if (GameModule.Resource != null)
            {
                try
                {
                    // 这里需要根据TEngine的具体API进行调整
                    // 假设TEngine有异步加载资源的方法
                    return await LoadResourceViaTEngine<T>(path);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[SubtitleResourceManager] TEngine加载失败，尝试Resources加载: {path}, 错误: {ex.Message}");
                }
            }
            
            // 回退到Resources.Load
            return await UniTask.Run(() => Resources.Load<T>(path));
        }
        
        /// <summary>
        /// 通过TEngine加载资源（需要根据实际API调整）
        /// </summary>
        private async UniTask<T> LoadResourceViaTEngine<T>(string path) where T : UnityEngine.Object
        {
            // 这里需要根据TEngine的实际资源管理API进行实现
            // 示例代码，需要根据实际情况调整
            await UniTask.Yield();
            return Resources.Load<T>(path);
        }
        
        /// <summary>
        /// 预加载资源
        /// </summary>
        public async UniTask PreloadResourcesAsync(string[] resourcePaths)
        {
            if (resourcePaths == null || resourcePaths.Length == 0)
            {
                return;
            }
            
            var tasks = new List<UniTask>();
            
            foreach (var path in resourcePaths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                
                // 根据路径类型预加载不同类型的资源
                if (path.Contains("Font"))
                {
                    tasks.Add(LoadFontAsync(System.IO.Path.GetFileNameWithoutExtension(path)));
                }
                else if (path.Contains("Material"))
                {
                    tasks.Add(LoadMaterialAsync(System.IO.Path.GetFileNameWithoutExtension(path)));
                }
                else if (path.Contains("Texture"))
                {
                    tasks.Add(LoadTextureAsync(System.IO.Path.GetFileNameWithoutExtension(path)));
                }
                else if (path.Contains("Audio"))
                {
                    tasks.Add(LoadAudioAsync(System.IO.Path.GetFileNameWithoutExtension(path)));
                }
                else if (path.Contains("Data"))
                {
                    tasks.Add(LoadSubtitleDataAsync(System.IO.Path.GetFileNameWithoutExtension(path)));
                }
            }
            
            await UniTask.WhenAll(tasks);
            Log.Info($"[SubtitleResourceManager] 预加载完成，共 {resourcePaths.Length} 个资源");
        }
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            _prefabCache.Clear();
            _fontCache.Clear();
            _materialCache.Clear();
            _textureCache.Clear();
            _audioCache.Clear();
            _subtitleDataCache.Clear();
            
            Log.Info("[SubtitleResourceManager] 清理缓存完成");
        }
        
        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public string GetCacheStats()
        {
            return $"预制体: {_prefabCache.Count}, 字体: {_fontCache.Count}, 材质: {_materialCache.Count}, " +
                   $"纹理: {_textureCache.Count}, 音频: {_audioCache.Count}, 数据: {_subtitleDataCache.Count}";
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            
            ClearCache();
            
            // 释放默认材质
            if (DefaultMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(DefaultMaterial);
                DefaultMaterial = null;
            }
            
            _isDisposed = true;
            Log.Info("[SubtitleResourceManager] 已释放");
        }
    }
    
    /// <summary>
    /// 字幕数据结构
    /// </summary>
    [Serializable]
    public class SubtitleData
    {
        public string id;
        public string text;
        public float startTime;
        public float duration;
        public SubtitleSequenceConfig config;
        public string audioClipName;
        public string fontName;
        public string materialName;
        
        public SubtitleData()
        {
            config = new SubtitleSequenceConfig();
        }
    }
}