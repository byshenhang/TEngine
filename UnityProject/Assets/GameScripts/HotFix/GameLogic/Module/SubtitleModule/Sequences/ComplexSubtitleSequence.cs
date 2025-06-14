using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 复杂字幕序列 - 支持多种效果组合和高级动画
    /// </summary>
    public class ComplexSubtitleSequence : SubtitleSequenceBase
    {
        private readonly List<EffectGroup> _effectGroups = new List<EffectGroup>();
        
        public ComplexSubtitleSequence(SubtitleSequenceConfig config, SubtitleModule subtitleModule) 
            : base(config, subtitleModule)
        {
        }
        
        /// <summary>
        /// 执行播放逻辑
        /// </summary>
        protected override async UniTask ExecutePlayLogic()
        {
            if (string.IsNullOrEmpty(_config.Text))
            {
                Log.Warning("[ComplexSubtitleSequence] 文本为空");
                return;
            }
            
            // 创建所有字符对象
            await CreateAllCharacterObjects();
            
            // 根据配置执行不同的播放模式
            switch (_config.DisplayMode)
            {
                case SubtitleDisplayMode.CharacterByCharacter:
                    await PlayComplexCharacterByCharacter();
                    break;
                    
                case SubtitleDisplayMode.WordByWord:
                    await PlayComplexWordByWord();
                    break;
                    
                default:
                    await PlayComplexAll();
                    break;
            }
        }
        
        /// <summary>
        /// 创建所有字符对象
        /// </summary>
        private async UniTask CreateAllCharacterObjects()
        {
            for (int i = 0; i < _config.Text.Length; i++)
            {
                char character = _config.Text[i];
                if (char.IsWhiteSpace(character)) continue;
                
                GameObject charObj = CreateCharacterObject(character, i);
                _characterObjects.Add(charObj);
                
                // 初始状态设为不可见
                charObj.SetActive(false);
            }
            
            await UniTask.Yield();
        }
        
        /// <summary>
        /// 复杂的逐字符播放（支持分组效果）
        /// </summary>
        private async UniTask PlayComplexCharacterByCharacter()
        {
            // 分组播放：偶数索引字符先播放，奇数索引字符后播放
            await PlayCharacterGroup(0, 2); // 0, 2, 4, 6...
            await UniTask.Delay(200); // 短暂间隔
            await PlayCharacterGroup(1, 2); // 1, 3, 5, 7...
        }
        
        /// <summary>
        /// 复杂的逐词播放
        /// </summary>
        private async UniTask PlayComplexWordByWord()
        {
            string[] words = _config.Text.Split(' ');
            int charIndex = 0;
            
            for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
            {
                if (_isCompleted) break;
                
                string word = words[wordIndex];
                List<GameObject> wordCharacters = new List<GameObject>();
                
                // 收集当前单词的所有字符对象
                for (int i = 0; i < word.Length; i++)
                {
                    if (charIndex < _characterObjects.Count)
                    {
                        wordCharacters.Add(_characterObjects[charIndex]);
                        charIndex++;
                    }
                }
                
                // 为整个单词应用波浪式出现效果
                await PlayWordWithWaveEffect(wordCharacters);
                
                // 等待单词间隔
                if (_config.WordInterval > 0 && wordIndex < words.Length - 1)
                {
                    await UniTask.Delay((int)(_config.WordInterval * 1000));
                }
            }
        }
        
        /// <summary>
        /// 复杂的整体播放
        /// </summary>
        private async UniTask PlayComplexAll()
        {
            // 随机顺序激活字符
            var randomIndices = GenerateRandomIndices(_characterObjects.Count);
            
            foreach (int index in randomIndices)
            {
                if (_isCompleted) break;
                
                var charObj = _characterObjects[index];
                if (charObj != null)
                {
                    charObj.SetActive(true);
                    ApplyEffectsToCharacter(charObj);
                    
                    // 短暂间隔
                    await UniTask.Delay((int)(_config.CharacterInterval * 500));
                }
            }
        }
        
        /// <summary>
        /// 播放字符组
        /// </summary>
        private async UniTask PlayCharacterGroup(int startIndex, int step)
        {
            for (int i = startIndex; i < _characterObjects.Count; i += step)
            {
                if (_isCompleted) break;
                
                var charObj = _characterObjects[i];
                if (charObj != null)
                {
                    charObj.SetActive(true);
                    ApplyEffectsToCharacter(charObj);
                    
                    // 等待效果达到阈值（模拟原始代码的模糊阈值等待）
                    await WaitForEffectThreshold(charObj);
                }
            }
        }
        
        /// <summary>
        /// 单词波浪效果播放
        /// </summary>
        private async UniTask PlayWordWithWaveEffect(List<GameObject> wordCharacters)
        {
            for (int i = 0; i < wordCharacters.Count; i++)
            {
                if (_isCompleted) break;
                
                var charObj = wordCharacters[i];
                if (charObj != null)
                {
                    charObj.SetActive(true);
                    
                    // 为每个字符添加延迟，创造波浪效果
                    float delay = i * 0.1f;
                    ApplyEffectsToCharacter(charObj, delay);
                }
            }
            
            // 等待波浪效果完成
            float waveCompletionTime = wordCharacters.Count * 0.1f + 0.5f;
            await UniTask.Delay((int)(waveCompletionTime * 1000));
        }
        
        /// <summary>
        /// 等待效果达到阈值
        /// </summary>
        private async UniTask WaitForEffectThreshold(GameObject charObj)
        {
            // 模拟等待模糊效果降到阈值以下
            float waitTime = 0.3f; // 默认等待时间
            
            // 如果有模糊效果配置，使用配置的阈值时间
            foreach (var effectConfig in _config.Effects)
            {
                if (effectConfig.EffectType == "Blur")
                {
                    float blurThreshold = effectConfig.GetParameter("BlurThreshold", 10f);
                    float blurStart = effectConfig.GetParameter("BlurStart", 30f);
                    float duration = effectConfig.Duration;
                    
                    // 计算达到阈值的时间
                    float thresholdRatio = (blurStart - blurThreshold) / blurStart;
                    waitTime = duration * thresholdRatio;
                    break;
                }
            }
            
            await UniTask.Delay((int)(waitTime * 1000));
        }
        
        /// <summary>
        /// 生成随机索引序列
        /// </summary>
        private List<int> GenerateRandomIndices(int count)
        {
            var indices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                indices.Add(i);
            }
            
            // Fisher-Yates 洗牌算法
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (indices[i], indices[randomIndex]) = (indices[randomIndex], indices[i]);
            }
            
            return indices;
        }
        
        /// <summary>
        /// 重写字符对象创建，支持更复杂的布局
        /// </summary>
        protected override GameObject CreateCharacterObject(char character, int index)
        {
            GameObject charObj = base.CreateCharacterObject(character, index);
            
            // 添加更复杂的定位逻辑
            var textComponent = charObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                // 计算更精确的字符位置
                float charWidth = textComponent.preferredWidth;
                float charHeight = textComponent.preferredHeight;
                
                // 支持多行布局
                int charactersPerLine = 20; // 每行字符数
                int lineIndex = index / charactersPerLine;
                int charInLine = index % charactersPerLine;
                
                Vector3 position = new Vector3(
                    charInLine * charWidth,
                    -lineIndex * charHeight,
                    0
                );
                
                charObj.transform.localPosition = position;
            }
            
            return charObj;
        }
    }
    
    /// <summary>
    /// 效果组 - 用于管理一组相关的效果
    /// </summary>
    public class EffectGroup
    {
        public List<GameObject> Characters { get; set; } = new List<GameObject>();
        public List<ISubtitleEffect> Effects { get; set; } = new List<ISubtitleEffect>();
        public float StartDelay { get; set; }
        public bool IsCompleted { get; set; }
    }
}