using Cysharp.Threading.Tasks;
using LyricFX.Core.Pipeline;
using LyricFX.Factory;
using System.Threading;
using TMPro;
using UnityEngine;

namespace LyricFX.Processors
{
    /// <summary>
    /// 字符创建处理器 - 负责从对象池创建字符实例
    /// </summary>
    public class CharacterCreationProcessor : ICharacterProcessor
    {
        private CharacterFactory characterFactory;
        
        public int Priority => 10; // 最先执行，创建字符
        public string ProcessorId => "character_creation";
        
        public void Initialize(CharacterFactory factory)
        {
            characterFactory = factory;
        }
        
        public async UniTask<ProcessingContext> Process(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            if (characterFactory == null)
            {
                Debug.LogError("[字符创建处理器] 字符工厂未初始化");
                return context;
            }
            
            // 创建字符实例
            var characterObj = characterFactory.GetCharacter();
            if (characterObj != null)
            {
                // 设置字符文本
                var textComponent = characterObj.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = context.Character.ToString();
                }
                
                // 更新上下文
                context.CharacterObject = characterObj;
                
                Debug.Log($"[字符创建处理器] 创建字符: '{context.Character}' (索引: {context.CharacterIndex})");
            }
            else
            {
                Debug.LogError($"[字符创建处理器] 创建字符失败: '{context.Character}' (索引: {context.CharacterIndex})");
            }
            
            await UniTask.CompletedTask;
            return context;
        }
    }
}
