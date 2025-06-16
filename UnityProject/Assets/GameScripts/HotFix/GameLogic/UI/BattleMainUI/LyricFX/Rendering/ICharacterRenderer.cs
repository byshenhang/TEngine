using TMPro;
using UnityEngine;

namespace LyricFX.Rendering
{
    /// <summary>
    /// 字符渲染器接口，定义字符渲染的基本功能
    /// </summary>
    public interface ICharacterRenderer
    {
        /// <summary>
        /// 获取TextMeshPro组件
        /// </summary>
        TextMeshProUGUI TextComponent { get; }
        
        /// <summary>
        /// 获取GameObject
        /// </summary>
        GameObject GameObject { get; }
        
        /// <summary>
        /// 设置激活状态
        /// </summary>
        /// <param name="active">是否激活</param>
        void SetActive(bool active);
        
        /// <summary>
        /// 设置文本内容
        /// </summary>
        /// <param name="text">文本内容</param>
        void SetText(string text);
        
        /// <summary>
        /// 设置位置
        /// </summary>
        /// <param name="position">位置</param>
        void SetPosition(Vector3 position);
        
        /// <summary>
        /// 获取或创建组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例</returns>
        T GetOrCreateComponent<T>() where T : Component;
        
        /// <summary>
        /// 设置透明度
        /// </summary>
        /// <param name="alpha">透明度值(0-1)</param>
        void SetAlpha(float alpha);
        
        /// <summary>
        /// 释放资源
        /// </summary>
        void Release();
        
        /// <summary>
        /// 获取激活状态
        /// </summary>
        /// <returns>是否激活</returns>
        bool IsActive();
    }
}
