using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChocDino.UIFX;

namespace GameLogic
{
    /// <summary>
    /// 歌词播放管理模块 - 支持LRC文件解析、字符级特效管理和动态创建
    /// </summary>
    public sealed class LyricModule : Singleton<LyricModule>, IUpdate
    {
        #region 字段定义
        
        private Transform _lyricRoot;                           // 歌词根节点
        private Camera _lyricCamera;                            // 歌词专用摄像机
        private readonly List<LyricLine> _lyricLines = new List<LyricLine>();  // 歌词行列表
        private readonly Queue<LyricCharacter> _characterPool = new Queue<LyricCharacter>(); // 字符对象池
        private LyricConfig _currentConfig;                     // 当前歌词配置
        private bool _isPlaying = false;                       // 是否正在播放
        private float _currentTime = 0f;                       // 当前播放时间
        private int _currentLineIndex = -1;                    // 当前歌词行索引
        
        // 单行复用模式相关字段
        private LyricLine _reusableLine;                        // 复用的歌词行对象
        private LyricData _currentLyricData;                    // 当前歌词数据
        
        // 预制体和资源
        private GameObject _characterPrefab;                    // 字符预制体
        private GameObject _linePrefab;                         // 行预制体
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 歌词根节点访问属性
        /// </summary>
        public Transform LyricRoot => _lyricRoot;
        
        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying => _isPlaying;
        
        /// <summary>
        /// 当前播放时间
        /// </summary>
        public float CurrentTime => _currentTime;
        
        #endregion
        
        #region 模块生命周期
        
        /// <summary>
        /// 模块初始化
        /// </summary>
        protected override void OnInit()
        {
            InitializeLyricRoot();
            LoadPrefabs();
            Log.Info("[LyricModule] 初始化完成");
        }
        
        /// <summary>
        /// 模块更新
        /// </summary>
        public void OnUpdate()
        {
            if (!_isPlaying) return;
            
            _currentTime += Time.deltaTime;
            UpdateLyricDisplay();
        }
        
        /// <summary>
        /// 模块释放
        /// </summary>
        protected override void OnRelease()
        {
            StopLyric();
            ClearAllLines();
            ClearCharacterPool();
            
            if (_lyricRoot != null)
            {
                UnityEngine.Object.Destroy(_lyricRoot.gameObject);
                _lyricRoot = null;
            }
            
            Log.Info("[LyricModule] 模块已释放");
        }
        
        #endregion
        
        #region 初始化方法
        
        /// <summary>
        /// 初始化歌词根节点
        /// </summary>
        private void InitializeLyricRoot()
        {
            var lyricRootGO = GameObject.Find("LyricRoot");
            if (lyricRootGO == null)
            {
                lyricRootGO = new GameObject("LyricRoot");
                var canvas = lyricRootGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // 确保在UI之上
                
                lyricRootGO.AddComponent<CanvasScaler>();
                lyricRootGO.AddComponent<GraphicRaycaster>();
            }
            
            _lyricRoot = lyricRootGO.transform;
            _lyricCamera = lyricRootGO.GetComponentInChildren<Camera>();
            
            UnityEngine.Object.DontDestroyOnLoad(_lyricRoot.gameObject);
        }
        
        /// <summary>
        /// 加载预制体资源
        /// </summary>
        private void LoadPrefabs()
        {
            // 这里应该通过资源管理器加载预制体
            // 暂时创建基础预制体
            CreateBasicPrefabs();
        }
        
        /// <summary>
        /// 创建基础预制体
        /// </summary>
        private void CreateBasicPrefabs()
        {
            // 创建字符预制体
            _characterPrefab = new GameObject("LyricCharacter");
            var textMesh = _characterPrefab.AddComponent<TextMeshProUGUI>();
            textMesh.fontSize = 48;
            textMesh.color = Color.white;
            textMesh.alignment = TextAlignmentOptions.Center;
            
            var blurFilter = _characterPrefab.AddComponent<BlurFilter>();
            blurFilter.Blur = 0f;
            
            var rectTransform = _characterPrefab.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(50, 60);
            
            // 创建行预制体
            _linePrefab = new GameObject("LyricLine");
            var lineRect = _linePrefab.AddComponent<RectTransform>();
            lineRect.sizeDelta = new Vector2(1920, 100);
            
            // 设置为预制体（在实际项目中应该保存为预制体文件）
            _characterPrefab.SetActive(false);
            _linePrefab.SetActive(false);
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 加载并播放LRC歌词文件
        /// </summary>
        /// <param name="lrcFilePath">LRC文件路径</param>
        /// <param name="config">歌词配置</param>
        public async UniTask LoadAndPlayLyric(string lrcFilePath, LyricConfig config = null)
        {
            try
            {
                string lrcContent = await LoadLrcFile(lrcFilePath);
                var lyricData = ParseLrcContent(lrcContent);
                await PlayLyric(lyricData, config);
            }
            catch (Exception e)
            {
                Log.Error($"[LyricModule] 加载歌词文件失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 播放歌词数据
        /// </summary>
        /// <param name="lyricData">歌词数据</param>
        /// <param name="config">歌词配置</param>
        public async UniTask PlayLyric(LyricData lyricData, LyricConfig config = null)
        {
            StopLyric();
            
            _currentConfig = config ?? LyricConfig.Default;
            _currentLyricData = lyricData;
            
            if (_currentConfig.DisplayMode == LyricDisplayMode.SingleLineReuse)
            {
                await InitializeSingleLineReuse();
            }
            else
            {
                await CreateLyricLines(lyricData);
            }
            
            _currentTime = 0f;
            _currentLineIndex = -1;
            _isPlaying = true;
            
            Log.Info($"[LyricModule] 开始播放歌词，模式: {_currentConfig.DisplayMode}, 共 {lyricData.Lines.Count} 行");
        }
        
        /// <summary>
        /// 停止歌词播放
        /// </summary>
        public void StopLyric()
        {
            _isPlaying = false;
            _currentTime = 0f;
            _currentLineIndex = -1;
            
            // 停止所有正在播放的效果
            foreach (var line in _lyricLines)
            {
                line.StopAllEffects();
            }
            
            // 停止复用行的效果
            if (_reusableLine != null)
            {
                _reusableLine.StopAllEffects();
                _reusableLine.GameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 暂停歌词播放
        /// </summary>
        public void PauseLyric()
        {
            _isPlaying = false;
        }
        
        /// <summary>
        /// 恢复歌词播放
        /// </summary>
        public void ResumeLyric()
        {
            _isPlaying = true;
        }
        
        /// <summary>
        /// 设置播放时间
        /// </summary>
        /// <param name="time">时间（秒）</param>
        public void SetTime(float time)
        {
            _currentTime = time;
            UpdateLyricDisplay();
        }
        
        /// <summary>
        /// 清除所有歌词
        /// </summary>
        public void ClearLyrics()
        {
            StopLyric();
            ClearAllLines();
            
            // 清理复用行
            if (_reusableLine != null)
            {
                _reusableLine.Dispose();
                _reusableLine = null;
            }
            
            _currentLyricData = null;
        }
        
        #endregion
        
        #region LRC文件处理
        
        /// <summary>
        /// 加载LRC文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容</returns>
        private async UniTask<string> LoadLrcFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            else
            {
                // 尝试通过资源管理器加载
                var resourceModule = ModuleSystem.GetModule<IResourceModule>();
                if (resourceModule != null)
                {
                    var textAsset = await resourceModule.LoadAssetAsync<TextAsset>(filePath);
                    return textAsset?.text ?? string.Empty;
                }
                
                throw new FileNotFoundException($"LRC文件未找到: {filePath}");
            }
        }
        
        /// <summary>
        /// 解析LRC文件内容
        /// </summary>
        /// <param name="content">LRC文件内容</param>
        /// <returns>歌词数据</returns>
        private LyricData ParseLrcContent(string content)
        {
            var lyricData = new LyricData();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            // LRC时间标签正则表达式
            var timeRegex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2})\](.*)$");
            
            foreach (var line in lines)
            {
                var match = timeRegex.Match(line.Trim());
                if (match.Success)
                {
                    var minutes = int.Parse(match.Groups[1].Value);
                    var seconds = int.Parse(match.Groups[2].Value);
                    var centiseconds = int.Parse(match.Groups[3].Value);
                    var text = match.Groups[4].Value.Trim();
                    
                    var time = minutes * 60f + seconds + centiseconds * 0.01f;
                    
                    lyricData.Lines.Add(new LyricLineData
                    {
                        Time = time,
                        Text = text
                    });
                }
            }
            
            // 按时间排序
            lyricData.Lines.Sort((a, b) => a.Time.CompareTo(b.Time));
            
            Log.Info($"[LyricModule] 解析LRC文件完成，共 {lyricData.Lines.Count} 行歌词");
            return lyricData;
        }
        
        #endregion
        
        #region 歌词显示更新
        
        /// <summary>
        /// 更新歌词显示
        /// </summary>
        private void UpdateLyricDisplay()
        {
            if (_currentConfig.DisplayMode == LyricDisplayMode.SingleLineReuse)
            {
                UpdateLyricDisplaySingleLineReuse();
            }
            else
            {
                UpdateLyricDisplayMultiLine();
            }
        }
        
        /// <summary>
        /// 多行模式的歌词显示更新
        /// </summary>
        private void UpdateLyricDisplayMultiLine()
        {
            // 查找当前应该显示的歌词行
            int targetLineIndex = FindCurrentLineIndex();
            
            if (targetLineIndex != _currentLineIndex)
            {
                // 切换到新的歌词行
                if (_currentLineIndex >= 0 && _currentLineIndex < _lyricLines.Count)
                {
                    _lyricLines[_currentLineIndex].PlayExitEffect();
                }
                
                _currentLineIndex = targetLineIndex;
                
                if (_currentLineIndex >= 0 && _currentLineIndex < _lyricLines.Count)
                {
                    _lyricLines[_currentLineIndex].PlayEnterEffect();
                }
            }
            
            // 更新当前行的字符效果
            if (_currentLineIndex >= 0 && _currentLineIndex < _lyricLines.Count)
            {
                var currentLine = _lyricLines[_currentLineIndex];
                var lineStartTime = currentLine.Data.Time;
                var characterTime = _currentTime - lineStartTime;
                
                currentLine.UpdateCharacterEffects(characterTime);
            }
        }
        
        /// <summary>
        /// 单行复用模式的歌词显示更新
        /// </summary>
        private void UpdateLyricDisplaySingleLineReuse()
        {
            if (_currentLyricData == null || _reusableLine == null)
                return;
                
            // 查找当前应该显示的歌词行
            int targetLineIndex = FindCurrentLineIndexForReuse();
            
            if (targetLineIndex != _currentLineIndex && targetLineIndex >= 0)
            {
                _currentLineIndex = targetLineIndex;
                var targetLineData = _currentLyricData.Lines[targetLineIndex];
                
                // 更新复用行的内容
                UpdateReusableLineContent(targetLineData).Forget();
            }
            
            // 更新当前行的字符效果
            if (_currentLineIndex >= 0 && _currentLineIndex < _currentLyricData.Lines.Count)
            {
                var lineStartTime = _currentLyricData.Lines[_currentLineIndex].Time;
                var characterTime = _currentTime - lineStartTime;
                
                _reusableLine.UpdateCharacterEffects(characterTime);
            }
        }
        
        /// <summary>
        /// 查找当前时间对应的歌词行索引
        /// </summary>
        /// <returns>歌词行索引，-1表示没有对应的行</returns>
        private int FindCurrentLineIndex()
        {
            for (int i = _lyricLines.Count - 1; i >= 0; i--)
            {
                if (_currentTime >= _lyricLines[i].Data.Time)
                {
                    return i;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// 单行复用模式下查找当前时间对应的歌词行索引
        /// </summary>
        /// <returns>歌词行索引，-1表示没有对应的行</returns>
        private int FindCurrentLineIndexForReuse()
        {
            if (_currentLyricData == null || _currentLyricData.Lines.Count == 0)
                return -1;
                
            for (int i = _currentLyricData.Lines.Count - 1; i >= 0; i--)
            {
                if (_currentTime >= _currentLyricData.Lines[i].Time)
                {
                    return i;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// 更新复用行的内容
        /// </summary>
        /// <param name="lineData">新的歌词行数据</param>
        private async UniTask UpdateReusableLineContent(LyricLineData lineData)
        {
            if (_reusableLine == null)
                return;
                
            // 如果当前有内容，播放退出效果
            if (_reusableLine.GameObject.activeInHierarchy)
            {
                _reusableLine.PlayExitEffect();
                
                // 等待退出效果完成
                await UniTask.Delay(100); // 可以根据实际效果时长调整
            }
            
            // 更新行数据和重新初始化
            await _reusableLine.Initialize(lineData, _currentConfig, _characterPool);
            
            // 激活GameObject并播放进入效果
            _reusableLine.GameObject.SetActive(true);
            _reusableLine.PlayEnterEffect();
        }
        
        #endregion
        
        #region 歌词行创建和管理
        
        /// <summary>
        /// 创建歌词行
        /// </summary>
        /// <param name="lyricData">歌词数据</param>
        private async UniTask CreateLyricLines(LyricData lyricData)
        {
            ClearAllLines();
            
            for (int i = 0; i < lyricData.Lines.Count; i++)
            {
                var lineData = lyricData.Lines[i];
                var lyricLine = await CreateLyricLine(lineData, i);
                _lyricLines.Add(lyricLine);
            }
        }
        
        /// <summary>
        /// 初始化单行复用模式
        /// </summary>
        private async UniTask InitializeSingleLineReuse()
        {
            ClearAllLines();
            
            // 清理之前的复用行
            if (_reusableLine != null)
            {
                _reusableLine.Dispose();
                _reusableLine = null;
            }
            
            // 创建复用的歌词行
            if (_currentLyricData.Lines.Count > 0)
            {
                var firstLineData = _currentLyricData.Lines[0];
                _reusableLine = await CreateLyricLine(firstLineData, 0);
                
                // 设置复用行位置（居中显示）
                var rectTransform = _reusableLine.GameObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = Vector2.zero;
                
                // 初始状态隐藏
                _reusableLine.GameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 创建单个歌词行
        /// </summary>
        /// <param name="lineData">行数据</param>
        /// <param name="index">行索引</param>
        /// <returns>歌词行对象</returns>
        private async UniTask<LyricLine> CreateLyricLine(LyricLineData lineData, int index)
        {
            var lineGO = UnityEngine.Object.Instantiate(_linePrefab, _lyricRoot);
            lineGO.name = $"LyricLine_{index}";
            lineGO.SetActive(true);
            
            var lyricLine = new LyricLine(lineGO, lineData, _currentConfig);
            await lyricLine.Initialize(this);
            
            // 设置行位置
            var rectTransform = lineGO.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, -index * _currentConfig.LineSpacing);
            
            return lyricLine;
        }
        
        /// <summary>
        /// 清除所有歌词行
        /// </summary>
        private void ClearAllLines()
        {
            foreach (var line in _lyricLines)
            {
                line.Dispose();
            }
            _lyricLines.Clear();
        }
        
        #endregion
        
        #region 字符对象池管理
        
        /// <summary>
        /// 获取字符对象（从对象池）
        /// </summary>
        /// <returns>字符对象</returns>
        public LyricCharacter GetCharacterFromPool()
        {
            if (_characterPool.Count > 0)
            {
                var character = _characterPool.Dequeue();
                character.gameObject.SetActive(true);
                return character;
            }
            else
            {
                var characterGO = UnityEngine.Object.Instantiate(_characterPrefab);
                characterGO.SetActive(true);
                return new LyricCharacter(characterGO);
            }
        }
        
        /// <summary>
        /// 归还字符对象到对象池
        /// </summary>
        /// <param name="character">字符对象</param>
        public void ReturnCharacterToPool(LyricCharacter character)
        {
            if (character != null)
            {
                character.Reset();
                character.gameObject.SetActive(false);
                _characterPool.Enqueue(character);
            }
        }
        
        /// <summary>
        /// 清空字符对象池
        /// </summary>
        private void ClearCharacterPool()
        {
            while (_characterPool.Count > 0)
            {
                var character = _characterPool.Dequeue();
                if (character?.gameObject != null)
                {
                    UnityEngine.Object.Destroy(character.gameObject);
                }
            }
        }
        
        #endregion
        
        #region 公共配置方法
        
        /// <summary>
        /// 设置字符预制体
        /// </summary>
        /// <param name="prefab">字符预制体</param>
        public void SetCharacterPrefab(GameObject prefab)
        {
            _characterPrefab = prefab;
        }
        
        /// <summary>
        /// 停止所有歌词播放
        /// </summary>
        public void StopAllLyrics()
        {
            StopLyric();
        }
        
        /// <summary>
        /// 设置歌词父节点
        /// </summary>
        /// <param name="parent">父节点</param>
        public void SetLyricParent(Transform parent)
        {
            if (_lyricRoot != null && parent != null)
            {
                _lyricRoot.SetParent(parent);
            }
        }
        
        /// <summary>
        /// 获取当前活跃的歌词数量
        /// </summary>
        /// <returns>活跃歌词数量</returns>
        public int GetActiveLyricCount()
        {
            return _lyricLines.Count;
        }
        
        /// <summary>
        /// 清除所有歌词（别名方法）
        /// </summary>
        public void ClearAllLyrics()
        {
            ClearLyrics();
        }
        
        #endregion
    }
}