using UnityEngine;
using TMPro;
using ChocDino.UIFX;

namespace GameLogic
{
    /// <summary>
    /// 歌词字符类 - 负责单个字符的特效管理和状态控制
    /// </summary>
    public class LyricCharacter
    {
        #region 字段定义
        
        private GameObject _gameObject;                         // 字符游戏对象
        private TextMeshProUGUI _textMesh;                     // 文本组件
        private BlurFilter _blurFilter;                        // 模糊滤镜组件
        private RectTransform _rectTransform;                  // RectTransform组件
        
        private char _character;                               // 字符内容
        private int _index;                                    // 字符索引
        private LyricConfig _config;                           // 配置引用
        
        private Color _originalColor;                          // 原始颜色
        private Vector3 _originalScale;                        // 原始缩放
        private Vector3 _originalPosition;                     // 原始位置
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 字符游戏对象
        /// </summary>
        public GameObject gameObject => _gameObject;
        
        /// <summary>
        /// 字符内容
        /// </summary>
        public char Character => _character;
        
        /// <summary>
        /// 字符索引
        /// </summary>
        public int Index => _index;
        
        /// <summary>
        /// 文本组件
        /// </summary>
        public TextMeshProUGUI TextMesh => _textMesh;
        
        /// <summary>
        /// 文本组件（兼容性属性）
        /// </summary>
        public TextMeshProUGUI TextComponent => _textMesh;
        
        /// <summary>
        /// Transform组件
        /// </summary>
        public Transform Transform => _rectTransform;
        
        /// <summary>
        /// GameObject（兼容性属性）
        /// </summary>
        public GameObject GameObject => _gameObject;
        
        #endregion
        
        #region 构造函数
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="gameObject">字符游戏对象</param>
        public LyricCharacter(GameObject gameObject)
        {
            _gameObject = gameObject;
            _textMesh = gameObject.GetComponent<TextMeshProUGUI>();
            _blurFilter = gameObject.GetComponent<BlurFilter>();
            _rectTransform = gameObject.GetComponent<RectTransform>();
            
            // 如果没有BlurFilter组件，添加一个
            if (_blurFilter == null)
            {
                _blurFilter = gameObject.AddComponent<BlurFilter>();
            }
            
            // 保存原始状态
            if (_textMesh != null)
            {
                _originalColor = _textMesh.color;
            }
            _originalScale = _rectTransform.localScale;
            _originalPosition = _rectTransform.localPosition;
        }
        
        #endregion
        
        #region 初始化和重置
        
        /// <summary>
        /// 初始化字符
        /// </summary>
        /// <param name="character">字符内容</param>
        /// <param name="index">字符索引</param>
        /// <param name="config">配置</param>
        public void Initialize(char character, int index, LyricConfig config)
        {
            _character = character;
            _index = index;
            _config = config;
            
            // 设置字符内容
            if (_textMesh != null)
            {
                _textMesh.text = character.ToString();
                _textMesh.fontSize = config.FontSize;
                _textMesh.color = config.DefaultColor;
                _originalColor = config.DefaultColor;
            }
            
            // 重置特效状态
            Reset();
        }
        
        /// <summary>
        /// 重置字符状态
        /// </summary>
        public void Reset()
        {
            if (_textMesh != null)
            {
                _textMesh.color = _originalColor;
            }
            
            if (_blurFilter != null)
            {
                _blurFilter.Blur = 0f;
            }
            
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _originalScale;
                _rectTransform.localPosition = _originalPosition;
            }
            
            SetVisible(false);
        }
        
        #endregion
        
        #region 可见性控制
        
        /// <summary>
        /// 设置可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetVisible(bool visible)
        {
            if (_gameObject != null)
            {
                _gameObject.SetActive(visible);
            }
        }
        
        /// <summary>
        /// 获取可见性
        /// </summary>
        /// <returns>是否可见</returns>
        public bool IsVisible()
        {
            return _gameObject != null && _gameObject.activeInHierarchy;
        }
        
        #endregion
        
        #region 属性设置
        
        /// <summary>
        /// 设置透明度
        /// </summary>
        /// <param name="alpha">透明度值 (0-1)</param>
        public void SetAlpha(float alpha)
        {
            if (_textMesh != null)
            {
                var color = _textMesh.color;
                color.a = Mathf.Clamp01(alpha);
                _textMesh.color = color;
            }
        }
        
        /// <summary>
        /// 获取透明度
        /// </summary>
        /// <returns>透明度值</returns>
        public float GetAlpha()
        {
            return _textMesh?.color.a ?? 0f;
        }
        
        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="color">颜色</param>
        public void SetColor(Color color)
        {
            if (_textMesh != null)
            {
                _textMesh.color = color;
            }
        }
        
        /// <summary>
        /// 获取颜色
        /// </summary>
        /// <returns>颜色</returns>
        public Color GetColor()
        {
            return _textMesh?.color ?? Color.white;
        }
        
        /// <summary>
        /// 设置缩放
        /// </summary>
        /// <param name="scale">缩放值</param>
        public void SetScale(Vector3 scale)
        {
            if (_rectTransform != null)
            {
                _rectTransform.localScale = scale;
            }
        }
        
        /// <summary>
        /// 获取缩放
        /// </summary>
        /// <returns>缩放值</returns>
        public Vector3 GetScale()
        {
            return _rectTransform?.localScale ?? Vector3.one;
        }
        
        /// <summary>
        /// 设置位置
        /// </summary>
        /// <param name="position">位置</param>
        public void SetPosition(Vector3 position)
        {
            if (_rectTransform != null)
            {
                _rectTransform.localPosition = position;
            }
        }
        
        /// <summary>
        /// 获取位置
        /// </summary>
        /// <returns>位置</returns>
        public Vector3 GetPosition()
        {
            return _rectTransform?.localPosition ?? Vector3.zero;
        }
        
        /// <summary>
        /// 设置模糊值
        /// </summary>
        /// <param name="blur">模糊值</param>
        public void SetBlur(float blur)
        {
            if (_blurFilter != null)
            {
                _blurFilter.Blur = Mathf.Max(0f, blur);
            }
        }
        
        /// <summary>
        /// 获取模糊值
        /// </summary>
        /// <returns>模糊值</returns>
        public float GetBlur()
        {
            return _blurFilter?.Blur ?? 0f;
        }
        
        /// <summary>
        /// 获取模糊滤镜组件
        /// </summary>
        /// <returns>模糊滤镜组件</returns>
        public BlurFilter GetBlurFilter()
        {
            return _blurFilter;
        }
        
        #endregion
        
        #region 特效更新
        
        /// <summary>
        /// 更新特效
        /// </summary>
        /// <param name="effectConfig">特效配置</param>
        /// <param name="t">时间进度 (0-1)</param>
        public void UpdateEffect(LyricEffectConfig effectConfig, float t)
        {
            if (effectConfig == null) return;
            
            float curveValue = effectConfig.Curve.Evaluate(t);
            
            switch (effectConfig.EffectType)
            {
                case LyricEffectType.Blur:
                    UpdateBlurEffect(effectConfig.BlurParams, curveValue);
                    break;
                    
                case LyricEffectType.Fade:
                    UpdateFadeEffect(effectConfig.FadeParams, curveValue);
                    break;
                    
                case LyricEffectType.Scale:
                    UpdateScaleEffect(effectConfig.ScaleParams, curveValue);
                    break;
                    
                case LyricEffectType.Move:
                    UpdateMoveEffect(effectConfig.MoveParams, curveValue);
                    break;
                    
                case LyricEffectType.BlurFade:
                    UpdateBlurEffect(effectConfig.BlurParams, curveValue);
                    UpdateFadeEffect(effectConfig.FadeParams, curveValue);
                    break;
                    
                case LyricEffectType.ScaleFade:
                    UpdateScaleEffect(effectConfig.ScaleParams, curveValue);
                    UpdateFadeEffect(effectConfig.FadeParams, curveValue);
                    break;
                    
                case LyricEffectType.MoveFade:
                    UpdateMoveEffect(effectConfig.MoveParams, curveValue);
                    UpdateFadeEffect(effectConfig.FadeParams, curveValue);
                    break;
            }
        }
        
        /// <summary>
        /// 设置特效完成状态
        /// </summary>
        /// <param name="effectConfig">特效配置</param>
        public void SetEffectComplete(LyricEffectConfig effectConfig)
        {
            if (effectConfig == null) return;
            
            switch (effectConfig.EffectType)
            {
                case LyricEffectType.Blur:
                    SetBlur(effectConfig.BlurParams.EndBlur);
                    break;
                    
                case LyricEffectType.Fade:
                    SetAlpha(effectConfig.FadeParams.EndAlpha);
                    break;
                    
                case LyricEffectType.Scale:
                    SetScale(effectConfig.ScaleParams.EndScale);
                    break;
                    
                case LyricEffectType.Move:
                    if (effectConfig.MoveParams.UseRelativePosition)
                    {
                        SetPosition(_originalPosition + effectConfig.MoveParams.EndPosition);
                    }
                    else
                    {
                        SetPosition(effectConfig.MoveParams.EndPosition);
                    }
                    break;
                    
                case LyricEffectType.BlurFade:
                    SetBlur(effectConfig.BlurParams.EndBlur);
                    SetAlpha(effectConfig.FadeParams.EndAlpha);
                    break;
                    
                case LyricEffectType.ScaleFade:
                    SetScale(effectConfig.ScaleParams.EndScale);
                    SetAlpha(effectConfig.FadeParams.EndAlpha);
                    break;
                    
                case LyricEffectType.MoveFade:
                    if (effectConfig.MoveParams.UseRelativePosition)
                    {
                        SetPosition(_originalPosition + effectConfig.MoveParams.EndPosition);
                    }
                    else
                    {
                        SetPosition(effectConfig.MoveParams.EndPosition);
                    }
                    SetAlpha(effectConfig.FadeParams.EndAlpha);
                    break;
            }
            
            // 确保字符可见
            if (!IsVisible())
            {
                SetVisible(true);
            }
        }
        
        #endregion
        
        #region 私有特效更新方法
        
        /// <summary>
        /// 更新模糊特效
        /// </summary>
        /// <param name="blurParams">模糊参数</param>
        /// <param name="t">时间进度</param>
        private void UpdateBlurEffect(BlurEffectParams blurParams, float t)
        {
            float blur = Mathf.Lerp(blurParams.StartBlur, blurParams.EndBlur, t);
            SetBlur(blur);
        }
        
        /// <summary>
        /// 更新淡入淡出特效
        /// </summary>
        /// <param name="fadeParams">淡入淡出参数</param>
        /// <param name="t">时间进度</param>
        private void UpdateFadeEffect(FadeEffectParams fadeParams, float t)
        {
            float alpha = Mathf.Lerp(fadeParams.StartAlpha, fadeParams.EndAlpha, t);
            SetAlpha(alpha);
        }
        
        /// <summary>
        /// 更新缩放特效
        /// </summary>
        /// <param name="scaleParams">缩放参数</param>
        /// <param name="t">时间进度</param>
        private void UpdateScaleEffect(ScaleEffectParams scaleParams, float t)
        {
            Vector3 scale = Vector3.Lerp(scaleParams.StartScale, scaleParams.EndScale, t);
            SetScale(scale);
        }
        
        /// <summary>
        /// 更新移动特效
        /// </summary>
        /// <param name="moveParams">移动参数</param>
        /// <param name="t">时间进度</param>
        private void UpdateMoveEffect(MoveEffectParams moveParams, float t)
        {
            Vector3 offset = Vector3.Lerp(moveParams.StartPosition, moveParams.EndPosition, t);
            
            if (moveParams.UseRelativePosition)
            {
                SetPosition(_originalPosition + offset);
            }
            else
            {
                SetPosition(offset);
            }
        }
        
        #endregion
        
        #region 高亮控制
        
        /// <summary>
        /// 设置高亮状态
        /// </summary>
        /// <param name="highlight">是否高亮</param>
        public void SetHighlight(bool highlight)
        {
            if (_config != null && _textMesh != null)
            {
                Color targetColor = highlight ? _config.HighlightColor : _config.DefaultColor;
                SetColor(targetColor);
            }
        }
        
        /// <summary>
        /// 设置高亮颜色
        /// </summary>
        /// <param name="highlightColor">高亮颜色</param>
        public void SetHighlightColor(Color highlightColor)
        {
            SetColor(highlightColor);
        }
        
        #endregion
        
        #region 调试信息
        
        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息字符串</returns>
        public string GetDebugInfo()
        {
            return $"Character: '{_character}', Index: {_index}, Visible: {IsVisible()}, " +
                   $"Alpha: {GetAlpha():F2}, Blur: {GetBlur():F2}, Scale: {GetScale()}, Position: {GetPosition()}";
        }
        
        #endregion
    }
}