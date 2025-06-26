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
        private float characterSpacing = 60f;  // 字符间距（像素）
        private Vector3 startOffset = Vector3.zero;  // 起始偏移
        private bool enableDynamicSpacing = true;  // 启用动态间距计算
        private float minSpacing = 10f;  // 最小间距
        private float maxSpacing = 100f;  // 最大间距
        
        public string LayoutId => "default_linear";
        
        /// <summary>
        /// 构造函数，可以传入配置参数
        /// </summary>
        /// <param name="spacing">默认字符间距</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="enableDynamic">是否启用动态间距计算</param>
        /// <param name="minSpacing">最小间距</param>
        /// <param name="maxSpacing">最大间距</param>
        public DefaultLinearLayout(float spacing = 60f, Vector3 offset = default, bool enableDynamic = true, float minSpacing = 5f, float maxSpacing = 100f)
        {
            characterSpacing = spacing;
            startOffset = offset;
            enableDynamicSpacing = enableDynamic;
            this.minSpacing = minSpacing;
            this.maxSpacing = maxSpacing;
        }
        
        /// <summary>
        /// 计算线性布局
        /// </summary>
        public async UniTask<Vector3[]> CalculateLayout(
            string text, 
            Transform container,
            ILayoutConfig config, 
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return new Vector3[0];
            
            if (string.IsNullOrEmpty(text))
                return new Vector3[0];
            
            // 使用局部变量避免修改实例字段
            float spacing = characterSpacing;
            Vector3 offset = startOffset;
            bool useDynamicSpacing = enableDynamicSpacing;
            
            // 如果配置不为空，可以覆盖默认设置
            if (config is LinearLayoutConfig linearConfig)
            {
                spacing = linearConfig.CharacterSpacing;
                offset = linearConfig.StartOffset;
                useDynamicSpacing = linearConfig.EnableDynamicSpacing;
            }
            
            // 如果启用动态间距，计算合适的间距
            if (useDynamicSpacing)
            {
                spacing = await CalculateDynamicSpacing(text, container, spacing, cancellationToken);
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
            
            // 如果启用动态间距，验证位置是否在Canvas边界内
            if (useDynamicSpacing)
            {
                var canvasInfo = GetCanvasInfo(container);
                if (canvasInfo != null)
                {
                    float estimatedCharWidth = await EstimateCharacterWidth(container, cancellationToken);
                    if (estimatedCharWidth <= 0) estimatedCharWidth = 30f;
                    
                    // 验证位置是否超出边界
                    if (!ValidatePositionsWithinBounds(positions, canvasInfo, estimatedCharWidth))
                    {
                        Debug.LogWarning("[DefaultLinearLayout] 字符位置超出Canvas边界，尝试重新计算间距");
                        
                        // 重新计算更紧凑的间距
                        var canvasBounds = GetCanvasBounds(canvasInfo);
                        if (canvasBounds.HasValue)
                        {
                            float availableWidth = canvasBounds.Value.size.x * 0.8f; // 更保守的80%
                            float totalCharWidth = estimatedCharWidth * text.Length;
                            float totalSpacingWidth = availableWidth - totalCharWidth;
                            
                            if (totalSpacingWidth > 0 && text.Length > 1)
                            {
                                float newSpacing = totalSpacingWidth / (text.Length - 1);
                                newSpacing = Mathf.Clamp(newSpacing, minSpacing, spacing * 0.5f); // 不超过原间距的一半
                                
                                Debug.Log($"[DefaultLinearLayout] 重新计算间距: {spacing:F1} -> {newSpacing:F1}");
                                
                                // 重新计算位置
                                currentPos = offset;
                                for (int i = 0; i < text.Length; i++)
                                {
                                    positions[i] = currentPos;
                                    currentPos += new Vector3(newSpacing, 0, 0);
                                }
                            }
                        }
                    }
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// 计算动态间距 - 根据Canvas模式和字符宽度自动调整
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="container">容器Transform</param>
        /// <param name="defaultSpacing">默认间距</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>计算后的间距</returns>
        private async UniTask<float> CalculateDynamicSpacing(string text, Transform container, float defaultSpacing, CancellationToken cancellationToken)
        {
            try
            {
                // 获取Canvas信息
                var canvasInfo = GetCanvasInfo(container);
                if (canvasInfo == null)
                {
                    Debug.LogWarning("[DefaultLinearLayout] 无法获取Canvas信息，使用默认间距");
                    return defaultSpacing;
                }
                
                // 估算字符宽度
                float estimatedCharWidth = await EstimateCharacterWidth(container, cancellationToken);
                if (estimatedCharWidth <= 0)
                {
                    estimatedCharWidth = 30f; // 默认字符宽度
                }
                
                // 根据Canvas模式调整间距
                float adjustedSpacing = CalculateSpacingByCanvasMode(canvasInfo, estimatedCharWidth, text.Length, defaultSpacing);
                
                // 限制间距范围
                adjustedSpacing = Mathf.Clamp(adjustedSpacing, minSpacing, maxSpacing);
                
                Debug.Log($"[DefaultLinearLayout] 动态间距计算: Canvas模式={canvasInfo.renderMode}, 字符宽度={estimatedCharWidth:F1}, 调整后间距={adjustedSpacing:F1}");
                
                return adjustedSpacing;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DefaultLinearLayout] 动态间距计算失败: {ex.Message}");
                return defaultSpacing;
            }
        }
        
        /// <summary>
        /// 获取Canvas信息
        /// </summary>
        /// <param name="container">容器Transform</param>
        /// <returns>Canvas信息</returns>
        private CanvasInfo GetCanvasInfo(Transform container)
        {
            Canvas canvas = container.GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;
                
            var canvasScaler = canvas.GetComponent<CanvasScaler>();
            
            return new CanvasInfo
            {
                canvas = canvas,
                renderMode = canvas.renderMode,
                scaleFactor = canvas.scaleFactor,
                canvasScaler = canvasScaler,
                referenceResolution = canvasScaler != null ? canvasScaler.referenceResolution : Vector2.zero
            };
        }
        
        /// <summary>
        /// 估算字符宽度
        /// </summary>
        /// <param name="container">容器Transform</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>估算的字符宽度</returns>
        private async UniTask<float> EstimateCharacterWidth(Transform container, CancellationToken cancellationToken)
        {
            try
            {
                // 尝试从容器的子对象中获取TextMeshPro组件
                TextMeshProUGUI sampleText = container.GetComponentInChildren<TextMeshProUGUI>();
                
                if (sampleText != null)
                {
                    // 使用现有的TextMeshPro组件测量字符宽度
                    string originalText = sampleText.text;
                    sampleText.text = "测";
                    
                    await UniTask.Yield(); // 等待一帧让UI更新
                    
                    float width = sampleText.preferredWidth;
                    sampleText.text = originalText; // 恢复原始文本
                    
                    return width;
                }
                
                // 如果没有找到现有组件，创建临时测量对象
                return await CreateTemporaryMeasureObject(container, cancellationToken);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DefaultLinearLayout] 字符宽度估算失败: {ex.Message}");
                return 30f; // 返回默认宽度
            }
        }
        
        /// <summary>
        /// 创建临时测量对象来获取字符宽度
        /// </summary>
        /// <param name="container">容器Transform</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>字符宽度</returns>
        private async UniTask<float> CreateTemporaryMeasureObject(Transform container, CancellationToken cancellationToken)
        {
            GameObject tempObj = null;
            try
            {
                tempObj = new GameObject("TempMeasure");
                tempObj.transform.SetParent(container, false);
                
                var rectTransform = tempObj.AddComponent<RectTransform>();
                var textComponent = tempObj.AddComponent<TextMeshProUGUI>();
                
                textComponent.text = "测";
                textComponent.fontSize = 36; // 使用默认字体大小
                textComponent.alignment = TextAlignmentOptions.Center;
                
                await UniTask.Yield(); // 等待一帧让UI更新
                
                float width = textComponent.preferredWidth;
                return width;
            }
            finally
            {
                if (tempObj != null)
                {
                    GameObject.DestroyImmediate(tempObj);
                }
            }
        }
        
        /// <summary>
        /// 根据Canvas模式计算合适的间距，确保字符不超出Canvas边界
        /// </summary>
        /// <param name="canvasInfo">Canvas信息</param>
        /// <param name="charWidth">字符宽度</param>
        /// <param name="textLength">文本长度</param>
        /// <param name="defaultSpacing">默认间距</param>
        /// <returns>调整后的间距</returns>
        private float CalculateSpacingByCanvasMode(CanvasInfo canvasInfo, float charWidth, int textLength, float defaultSpacing)
        {
            // 获取Canvas的边界信息
            var canvasBounds = GetCanvasBounds(canvasInfo);
            if (canvasBounds.HasValue)
            {
                // 根据Canvas边界计算最大可用宽度
                float availableWidth = canvasBounds.Value.size.x;
                float totalCharWidth = charWidth * textLength;
                float totalSpacingWidth = availableWidth - totalCharWidth;
                
                // 确保有足够的空间，预留10%的边距
                totalSpacingWidth *= 0.9f;
                
                if (totalSpacingWidth > 0 && textLength > 1)
                {
                    float calculatedSpacing = totalSpacingWidth / (textLength - 1);
                    calculatedSpacing = Mathf.Clamp(calculatedSpacing, minSpacing, maxSpacing);
                    
                    Debug.Log($"[DefaultLinearLayout] 基于Canvas边界计算间距: 可用宽度={availableWidth:F1}, 字符总宽度={totalCharWidth:F1}, 计算间距={calculatedSpacing:F1}");
                    return calculatedSpacing;
                }
            }
            
            // 如果无法获取边界信息，使用原有的模式计算
            switch (canvasInfo.renderMode)
            {
                case RenderMode.WorldSpace:
                    // WorldSpace模式：间距应该相对较小，基于字符宽度
                    return Mathf.Max(charWidth * 0.1f, minSpacing);
                    
                case RenderMode.ScreenSpaceOverlay:
                    // ScreenSpaceOverlay模式：考虑屏幕分辨率
                    float screenBasedSpacing = Screen.width / (textLength * 20f); // 基于屏幕宽度的动态计算
                    return Mathf.Clamp(screenBasedSpacing, charWidth * 0.2f, charWidth * 2f);
                    
                case RenderMode.ScreenSpaceCamera:
                    // ScreenSpaceCamera模式：考虑Canvas缩放和相机距离
                    float cameraBasedSpacing = defaultSpacing / canvasInfo.scaleFactor;
                    
                    // 如果有CanvasScaler，进一步调整
                    if (canvasInfo.canvasScaler != null && canvasInfo.referenceResolution != Vector2.zero)
                    {
                        float scaleRatio = Screen.width / canvasInfo.referenceResolution.x;
                        cameraBasedSpacing *= scaleRatio;
                    }
                    
                    return Mathf.Clamp(cameraBasedSpacing, charWidth * 0.3f, charWidth * 1.5f);
                    
                default:
                    return defaultSpacing;
            }
        }
        
        /// <summary>
        /// 获取Canvas的边界信息（世界坐标系下的4个角点）
        /// </summary>
        /// <param name="canvasInfo">Canvas信息</param>
        /// <returns>Canvas边界，如果无法获取则返回null</returns>
        private Bounds? GetCanvasBounds(CanvasInfo canvasInfo)
        {
            try
            {
                RectTransform canvasRect = canvasInfo.canvas.GetComponent<RectTransform>();
                if (canvasRect == null)
                    return null;
                
                Vector3[] worldCorners = new Vector3[4];
                canvasRect.GetWorldCorners(worldCorners);
                
                // worldCorners数组包含4个角点：
                // [0] = 左下角 (bottom-left)
                // [1] = 左上角 (top-left) 
                // [2] = 右上角 (top-right)
                // [3] = 右下角 (bottom-right)
                
                Vector3 min, max, center, size;
                
                // 根据Canvas模式进行不同的处理
                switch (canvasInfo.renderMode)
                {
                    case RenderMode.WorldSpace:
                        // WorldSpace模式：直接使用世界坐标
                        min = new Vector3(
                            Mathf.Min(worldCorners[0].x, worldCorners[1].x, worldCorners[2].x, worldCorners[3].x),
                            Mathf.Min(worldCorners[0].y, worldCorners[1].y, worldCorners[2].y, worldCorners[3].y),
                            Mathf.Min(worldCorners[0].z, worldCorners[1].z, worldCorners[2].z, worldCorners[3].z)
                        );
                        max = new Vector3(
                            Mathf.Max(worldCorners[0].x, worldCorners[1].x, worldCorners[2].x, worldCorners[3].x),
                            Mathf.Max(worldCorners[0].y, worldCorners[1].y, worldCorners[2].y, worldCorners[3].y),
                            Mathf.Max(worldCorners[0].z, worldCorners[1].z, worldCorners[2].z, worldCorners[3].z)
                        );
                        break;
                        
                    case RenderMode.ScreenSpaceOverlay:
                        // ScreenSpaceOverlay模式：转换为本地坐标
                        min = canvasRect.InverseTransformPoint(worldCorners[0]);
                        max = canvasRect.InverseTransformPoint(worldCorners[2]);
                        break;
                        
                    case RenderMode.ScreenSpaceCamera:
                        // ScreenSpaceCamera模式：考虑相机和缩放
                        Camera renderCamera = canvasInfo.canvas.worldCamera;
                        if (renderCamera != null)
                        {
                            // 将屏幕坐标转换为世界坐标，然后转换为Canvas本地坐标
                            Vector3 localMin = canvasRect.InverseTransformPoint(worldCorners[0]);
                            Vector3 localMax = canvasRect.InverseTransformPoint(worldCorners[2]);
                            min = localMin;
                            max = localMax;
                        }
                        else
                        {
                            min = worldCorners[0];
                            max = worldCorners[2];
                        }
                        break;
                        
                    default:
                        min = worldCorners[0];
                        max = worldCorners[2];
                        break;
                }
                
                center = (min + max) * 0.5f;
                size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));
                
                Debug.Log($"[DefaultLinearLayout] Canvas边界: 模式={canvasInfo.renderMode}, 中心={center}, 尺寸={size}, 4个角点: [{worldCorners[0]}, {worldCorners[1]}, {worldCorners[2]}, {worldCorners[3]}]");
                
                return new Bounds(center, size);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DefaultLinearLayout] 获取Canvas边界失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 验证字符位置是否在Canvas边界内
        /// </summary>
        /// <param name="positions">字符位置数组</param>
        /// <param name="canvasInfo">Canvas信息</param>
        /// <param name="charWidth">字符宽度</param>
        /// <returns>是否所有字符都在边界内</returns>
        private bool ValidatePositionsWithinBounds(Vector3[] positions, CanvasInfo canvasInfo, float charWidth)
        {
            var canvasBounds = GetCanvasBounds(canvasInfo);
            if (!canvasBounds.HasValue || positions == null || positions.Length == 0)
                return true; // 无法验证时假设有效
            
            Bounds bounds = canvasBounds.Value;
            
            foreach (var position in positions)
            {
                // 检查字符的左边界和右边界是否都在Canvas内
                float leftEdge = position.x - charWidth * 0.5f;
                float rightEdge = position.x + charWidth * 0.5f;
                
                if (leftEdge < bounds.min.x || rightEdge > bounds.max.x)
                {
                    Debug.LogWarning($"[DefaultLinearLayout] 字符位置超出边界: 位置={position.x:F1}, 字符边界=[{leftEdge:F1}, {rightEdge:F1}], Canvas边界=[{bounds.min.x:F1}, {bounds.max.x:F1}]");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Canvas信息结构体
        /// </summary>
        private class CanvasInfo
        {
            public Canvas canvas;
            public RenderMode renderMode;
            public float scaleFactor;
            public CanvasScaler canvasScaler;
            public Vector2 referenceResolution;
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
        public float CharacterSpacing = 0.5f;
        public Vector3 StartOffset = Vector3.zero;
        public bool CenterLayout = false;
        
        [Header("动态间距设置")]
        [Tooltip("启用动态间距计算，根据Canvas模式和字符宽度自动调整间距")]
        public bool EnableDynamicSpacing = true;
        
        [Tooltip("最小字符间距")]
        public float MinSpacing = 10f;
        
        [Tooltip("最大字符间距")]
        public float MaxSpacing = 100f;
    }
}
