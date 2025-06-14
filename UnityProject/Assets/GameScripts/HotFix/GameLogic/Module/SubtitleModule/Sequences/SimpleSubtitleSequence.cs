using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 简单字幕序列 - 基础的字幕显示
    /// </summary>
    public class SimpleSubtitleSequence : SubtitleSequenceBase
    {
        public SimpleSubtitleSequence(SubtitleSequenceConfig config, SubtitleModule subtitleModule) 
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
                Log.Warning("[SimpleSubtitleSequence] 文本为空");
                return;
            }
            
            switch (_config.DisplayMode)
            {
                case SubtitleDisplayMode.CharacterByCharacter:
                    await PlayCharacterByCharacter();
                    break;
                    
                case SubtitleDisplayMode.WordByWord:
                    await PlayWordByWord();
                    break;
                    
                case SubtitleDisplayMode.LineByLine:
                    await PlayLineByLine();
                    break;
                    
                case SubtitleDisplayMode.All:
                    await PlayAll();
                    break;
            }
        }
        
        /// <summary>
        /// 逐字符播放
        /// </summary>
        private async UniTask PlayCharacterByCharacter()
        {
            // 创建所有字符对象
            for (int i = 0; i < _config.Text.Length; i++)
            {
                char character = _config.Text[i];
                if (char.IsWhiteSpace(character)) continue; // 跳过空白字符
                
                GameObject charObj = CreateCharacterObject(character, i);
                _characterObjects.Add(charObj);
            }
            
            // 逐个显示字符
            for (int i = 0; i < _characterObjects.Count; i++)
            {
                if (_isCompleted) break;
                
                var charObj = _characterObjects[i];
                if (charObj != null)
                {
                    // 激活字符对象
                    charObj.SetActive(true);
                    
                    // 应用效果
                    ApplyEffectsToCharacter(charObj);
                    
                    // 等待字符间隔
                    if (_config.CharacterInterval > 0)
                    {
                        await UniTask.Delay((int)(_config.CharacterInterval * 1000));
                    }
                }
            }
        }
        
        /// <summary>
        /// 逐词播放
        /// </summary>
        private async UniTask PlayWordByWord()
        {
            string[] words = _config.Text.Split(' ');
            int charIndex = 0;
            
            for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
            {
                if (_isCompleted) break;
                
                string word = words[wordIndex];
                
                // 为当前单词的每个字符创建对象
                for (int i = 0; i < word.Length; i++)
                {
                    char character = word[i];
                    GameObject charObj = CreateCharacterObject(character, charIndex);
                    _characterObjects.Add(charObj);
                    
                    // 立即激活并应用效果
                    charObj.SetActive(true);
                    ApplyEffectsToCharacter(charObj);
                    
                    charIndex++;
                }
                
                // 等待单词间隔
                if (_config.WordInterval > 0 && wordIndex < words.Length - 1)
                {
                    await UniTask.Delay((int)(_config.WordInterval * 1000));
                }
                
                // 添加空格的位置（如果不是最后一个单词）
                if (wordIndex < words.Length - 1)
                {
                    charIndex++; // 为空格预留位置
                }
            }
        }
        
        /// <summary>
        /// 逐行播放
        /// </summary>
        private async UniTask PlayLineByLine()
        {
            string[] lines = _config.Text.Split('\n');
            int charIndex = 0;
            
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (_isCompleted) break;
                
                string line = lines[lineIndex];
                
                // 为当前行的每个字符创建对象
                for (int i = 0; i < line.Length; i++)
                {
                    char character = line[i];
                    if (char.IsWhiteSpace(character)) continue;
                    
                    GameObject charObj = CreateCharacterObject(character, charIndex);
                    _characterObjects.Add(charObj);
                    
                    // 立即激活并应用效果
                    charObj.SetActive(true);
                    ApplyEffectsToCharacter(charObj);
                    
                    charIndex++;
                }
                
                // 等待行间隔
                if (_config.LineInterval > 0 && lineIndex < lines.Length - 1)
                {
                    await UniTask.Delay((int)(_config.LineInterval * 1000));
                }
            }
        }
        
        /// <summary>
        /// 整体播放
        /// </summary>
        private async UniTask PlayAll()
        {
            // 创建所有字符对象
            for (int i = 0; i < _config.Text.Length; i++)
            {
                char character = _config.Text[i];
                if (char.IsWhiteSpace(character)) continue;
                
                GameObject charObj = CreateCharacterObject(character, i);
                _characterObjects.Add(charObj);
                
                // 立即激活
                charObj.SetActive(true);
            }
            
            // 同时为所有字符应用效果
            foreach (var charObj in _characterObjects)
            {
                if (charObj != null)
                {
                    ApplyEffectsToCharacter(charObj);
                }
            }
            
            await UniTask.Yield();
        }
    }
}