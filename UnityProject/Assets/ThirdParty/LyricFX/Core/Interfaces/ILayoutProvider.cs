using Cysharp.Threading.Tasks;
using GameLogic;
using System.Threading;
using UnityEngine;

namespace LyricFX.Core.Interfaces
{
    /// <summary>
    /// 布局提供器接口 - 负责计算字符位置
    /// </summary>
    public interface ILayoutProvider
    {
        /// <summary>
        /// 布局唯一标识符
        /// </summary>
        string LayoutId { get; }
        
        /// <summary>
        /// 计算一行字符的布局位置
        /// </summary>
        /// <param name="text">要布局的文本</param>
        /// <param name="container">容器Transform</param>
        /// <param name="config">布局配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>字符位置数组</returns>
        UniTask<Vector3[]> CalculateLayout(string text, Transform container, ILayoutConfig config, GameObject characterPrefab, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 应用布局到字符对象
        /// </summary>
        /// <param name="characters">字符对象数组</param>
        /// <param name="positions">位置数组</param>
        /// <param name="cancellationToken">取消令牌</param>
        UniTask ApplyLayout(GameObject[] characters, Vector3[] positions, CancellationToken cancellationToken = default);
    }
}
