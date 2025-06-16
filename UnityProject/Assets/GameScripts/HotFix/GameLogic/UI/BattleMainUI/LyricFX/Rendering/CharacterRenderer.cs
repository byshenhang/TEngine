using TMPro;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace LyricFX.Rendering
{
    /// <summary>
    /// 字符渲染器，负责创建和管理单个字符的显示
    /// </summary>
    public class CharacterRenderer : ICharacterRenderer, IDisposable
    {
        private readonly GameObject _gameObject;
        private readonly TextMeshProUGUI _textComponent;
        private bool _isReleased = false;
        
        public TextMeshProUGUI TextComponent => _textComponent;
        public GameObject GameObject => _gameObject;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="prefab">字符预制体</param>
        /// <param name="parent">父级变换</param>
        public CharacterRenderer(GameObject prefab, Transform parent)
        {
            // 实例化游戏对象
            _gameObject = Object.Instantiate(prefab, parent);
            _gameObject.SetActive(false);
            
            // 获取文本组件
            _textComponent = _gameObject.GetComponent<TextMeshProUGUI>();
            if (_textComponent == null)
            {
                _textComponent = _gameObject.AddComponent<TextMeshProUGUI>();
            }
            
            // 初始设置
            _textComponent.enableWordWrapping = false;
            _textComponent.overflowMode = TextOverflowModes.Overflow;
            _textComponent.alignment = TextAlignmentOptions.Center;
            _textComponent.enableAutoSizing = false;
        }
        
        /// <summary>
        /// 设置激活状态
        /// </summary>
        public void SetActive(bool active)
        {
            if (_isReleased) return;
            _gameObject.SetActive(active);
        }
        
        /// <summary>
        /// 设置文本内容
        /// </summary>
        public void SetText(string text)
        {
            if (_isReleased) return;
            _textComponent.text = text;
        }
        
        /// <summary>
        /// 设置位置
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            if (_isReleased) return;
            _gameObject.transform.localPosition = position;
        }
        
        /// <summary>
        /// 设置透明度
        /// </summary>
        /// <param name="alpha">透明度值(0-1)</param>
        public void SetAlpha(float alpha)
        {
            if (_isReleased) return;
            
            Color color = _textComponent.color;
            color.a = Mathf.Clamp01(alpha);
            _textComponent.color = color;
        }
        
        /// <summary>
        /// 获取或创建组件
        /// </summary>
        public T GetOrCreateComponent<T>() where T : Component
        {
            if (_isReleased) return null;
            
            T component = _gameObject.GetComponent<T>();
            if (component == null)
            {
                component = _gameObject.AddComponent<T>();
            }
            return component;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release()
        {
            if (_isReleased) return;
            
            _isReleased = true;
            Object.Destroy(_gameObject);
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Release();
        }
        
        /// <summary>
        /// 获取激活状态
        /// </summary>
        /// <returns>是否激活</returns>
        public bool IsActive()
        {
            // 增加完善的安全检查
            if (_isReleased) return false;
            if (_gameObject == null) return false;
            
            // 检查对象是否已经被销毁
            try
            {
                return _gameObject.activeSelf;
            }
            catch (System.Exception)
            {
                return false; // 如果访问失败，则对象可能已被销毁
            }
        }
    }
}
