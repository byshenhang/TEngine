using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 3D UIu8d44u6e90u52a0u8f7du5668u63a5u53e3
    /// </summary>
    public interface IUI3DResourceLoader
    {
        /// <summary>
        /// u5f02u6b65u52a0u8f7du9884u5236u4f53
        /// </summary>
        /// <param name="assetName">u8d44u6e90u8defu5f84</param>
        /// <returns>u9884u5236u4f53u5b9eu4f8b</returns>
        UniTask<GameObject> LoadPrefabAsync(string assetName);
        
        /// <summary>
        /// u540cu6b65u52a0u8f7du9884u5236u4f53
        /// </summary>
        /// <param name="assetName">u8d44u6e90u8defu5f84</param>
        /// <returns>u9884u5236u4f53u5b9eu4f8b</returns>
        GameObject LoadPrefab(string assetName);
    }
    
    /// <summary>
    /// 3D UIu8d44u6e90u52a0u8f7du5668u5b9eu73b0uff0cu5229u7528TEngineu7684u8d44u6e90u7cfbu7edf
    /// </summary>
    public class UI3DResourceLoader : IUI3DResourceLoader
    {
        // u7f13u5b58u5df2u52a0u8f7du7684u9884u5236u4f53
        private readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        
        /// <summary>
        /// u5f02u6b65u52a0u8f7du9884u5236u4f53
        /// </summary>
        public async UniTask<GameObject> LoadPrefabAsync(string assetName)
        {
            // u5c1du8bd5u4eceu7f13u5b58u4e2du83b7u53d6
            if (_prefabCache.TryGetValue(assetName, out GameObject prefab))
            {
                return prefab;
            }
            
            try
            {
                // u4f7fu7528TEngineu7684u8d44u6e90u7cfbu7edfu52a0u8f7d
                GameObject loadedPrefab = await GameModule.Resource.LoadPrefabAsync(assetName);
                if (loadedPrefab != null)
                {
                    // u7f13u5b58u9884u5236u4f53
                    _prefabCache[assetName] = loadedPrefab;
                }
                return loadedPrefab;
            }
            catch (Exception e)
            {
                Log.Error($"Load UI3D prefab async failed: {assetName}, {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// u540cu6b65u52a0u8f7du9884u5236u4f53
        /// </summary>
        public GameObject LoadPrefab(string assetName)
        {
            // u5c1du8bd5u4eceu7f13u5b58u4e2du83b7u53d6
            if (_prefabCache.TryGetValue(assetName, out GameObject prefab))
            {
                return prefab;
            }
            
            try
            {
                // u4f7fu7528TEngineu7684u8d44u6e90u7cfbu7edfu52a0u8f7d
                GameObject loadedPrefab = GameModule.Resource.LoadPrefab(assetName);
                if (loadedPrefab != null)
                {
                    // u7f13u5b58u9884u5236u4f53
                    _prefabCache[assetName] = loadedPrefab;
                }
                return loadedPrefab;
            }
            catch (Exception e)
            {
                Log.Error($"Load UI3D prefab failed: {assetName}, {e.Message}");
                return null;
            }
        }
    }
}
