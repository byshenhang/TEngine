using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 字幕资源加载器
    /// </summary>
    public class SubtitleResourceLoader
    {
        private readonly Dictionary<string, GameObject> _cachedPrefabs = new();
        private readonly Dictionary<string, Font> _cachedFonts = new();
        
        /// <summary>
        /// 加载字幕预制体
        /// </summary>
        /// <param name="prefabPath">预制体路径</param>
        /// <returns>预制体GameObject</returns>
        public async UniTask<GameObject> LoadSubtitlePrefabAsync(string prefabPath)
        {
            if (_cachedPrefabs.TryGetValue(prefabPath, out var cachedPrefab))
            {
                return cachedPrefab;
            }
            
            // 使用Resources.Load加载预制体
            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
            {
                _cachedPrefabs[prefabPath] = prefab;
                return prefab;
            }
            
            Log.Warning($"[SubtitleResourceLoader] 无法加载预制体: {prefabPath}");
            return null;
        }
        
        /// <summary>
        /// 加载字体资源
        /// </summary>
        /// <param name="fontPath">字体路径</param>
        /// <returns>字体资源</returns>
        public async UniTask<Font> LoadFontAsync(string fontPath)
        {
            if (_cachedFonts.TryGetValue(fontPath, out var cachedFont))
            {
                return cachedFont;
            }
            
            // 使用Resources.Load加载字体
            var font = Resources.Load<Font>(fontPath);
            if (font != null)
            {
                _cachedFonts[fontPath] = font;
                return font;
            }
            
            Log.Warning($"[SubtitleResourceLoader] 无法加载字体: {fontPath}");
            return null;
        }
        
        /// <summary>
        /// 创建字幕文本对象
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="text">文本内容</param>
        /// <returns>字幕文本GameObject</returns>
        public GameObject CreateSubtitleText(Transform parent, string text = "")
        {
            var textObj = new GameObject("SubtitleText");
            textObj.transform.SetParent(parent);
            
            // 添加TextMeshProUGUI组件
            var textComponent = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 24;
            textComponent.color = Color.white;
            textComponent.alignment = TMPro.TextAlignmentOptions.Center;
            
            // 添加RectTransform
            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            return textObj;
        }
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            _cachedPrefabs.Clear();
            _cachedFonts.Clear();
        }
    }
}