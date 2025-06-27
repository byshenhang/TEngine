using Cysharp.Threading.Tasks;
using GameLogic;
using LyricFX.Core.Interfaces;
using System.Threading;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace LyricFX.Implementations.Layout
{
    /// <summary>
    /// 默认线性布局 - 将字符按水平方向排列，支持动态间距计算
    /// </summary>
    public class DefaultLinearLayout : ILayoutProvider
    {
        private Vector3 startOffset = Vector3.zero;  // 起始偏移
        public string LayoutId => "default_linear";
        
        /// <summary>
        /// 构造函数，可以传入配置参数
        /// </summary>
        /// <param name="spacing">默认字符间距</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="enableDynamic">是否启用动态间距计算</param>
        /// <param name="minSpacing">最小间距</param>
        /// <param name="maxSpacing">最大间距</param>
        public DefaultLinearLayout(Vector3 offset = default)
        {
            startOffset = offset;
        }
        
        /// <summary>
        /// 计算线性布局
        /// </summary>
        public async UniTask<Vector3[]> CalculateLayout(
            string text, 
            Transform container,
            ILayoutConfig config, 
            GameObject prefab,
            CancellationToken cancellationToken = default)
        {
            var tmpro = prefab.GetComponent<TextMeshProUGUI>();

            if (cancellationToken.IsCancellationRequested)
                return new Vector3[0];
            
            if (string.IsNullOrEmpty(text))
                return new Vector3[0];
            
            // 使用局部变量避免修改实例字段
            float spacing = tmpro.rectTransform.sizeDelta.x;
            Vector3 offset = startOffset;
            
            // 如果配置不为空，可以覆盖默认设置
            if (config is LinearLayoutConfig linearConfig)
            {
                offset = linearConfig.StartOffset;
            }
            
            // 计算字符位置
            var positions = new Vector3[text.Length];
            
            // 获取起始位置（容器左侧）
            Vector3 currentPos = offset;
            
            for (int i = 0; i < text.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                positions[i] = currentPos;
                
                // 移动到下一个字符位置
                currentPos += new Vector3(spacing, 0, 0);
                
                // 每处理几个字符，让出一帧以避免卡顿
                if (i % 10 == 0 && i > 0)
                    await UniTask.Yield();
            }
            
            
            return positions;
        }
        
        /// <summary>
        /// 应用布局到字符对象
        /// </summary>
        public async UniTask ApplyLayout(GameObject[] characters, Vector3[] positions, CancellationToken cancellationToken = default)
        {
            if (characters == null || positions == null || characters.Length != positions.Length)
                return;
            
            for (int i = 0; i < characters.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                if (characters[i] != null)
                {
                    characters[i].transform.localPosition = positions[i];
                }
                
                // 每处理几个字符，让出一帧以避免卡顿
                if (i % 10 == 0 && i > 0)
                    await UniTask.Yield();
            }
        }
    }
    
    /// <summary>
    /// 线性布局配置
    /// </summary>
    [System.Serializable]
    public class LinearLayoutConfig : ILayoutConfig
    {
        [Header("基础设置")]
        public Vector3 StartOffset = Vector3.zero;
    }
}
