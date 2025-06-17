using System.Collections.Generic;
using UnityEngine;

namespace LyricFX.Core.Pipeline
{
    /// <summary>
    /// 处理上下文 - 在管道中传递的上下文数据
    /// </summary>
    public class ProcessingContext
    {
        /// <summary>
        /// 字符游戏对象
        /// </summary>
        public GameObject CharacterObject { get; set; }
        
        /// <summary>
        /// 字符索引
        /// </summary>
        public int CharacterIndex { get; set; }
        
        /// <summary>
        /// 字符内容
        /// </summary>
        public char Character { get; set; }
        
        /// <summary>
        /// 字符目标位置
        /// </summary>
        public Vector3 Position { get; set; }
        
        /// <summary>
        /// 所属行ID
        /// </summary>
        public int LineId { get; set; }
        
        /// <summary>
        /// 处理是否已完成
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// 元数据字典 - 可用于处理器间传递自定义数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();
        
        /// <summary>
        /// 获取元数据，如果不存在返回默认值
        /// </summary>
        public T GetMetadata<T>(string key, T defaultValue = default)
        {
            if (Metadata.TryGetValue(key, out object value) && value is T typedValue)
                return typedValue;
                
            return defaultValue;
        }
        
        /// <summary>
        /// 设置元数据
        /// </summary>
        public void SetMetadata(string key, object value)
        {
            Metadata[key] = value;
        }
        
        /// <summary>
        /// 创建字符处理上下文
        /// </summary>
        public static ProcessingContext Create(GameObject characterObject, int characterIndex, char character, int lineId, Vector3 position)
        {
            return new ProcessingContext
            {
                CharacterObject = characterObject,
                CharacterIndex = characterIndex,
                Character = character,
                LineId = lineId,
                Position = position,
                IsCompleted = false
            };
        }
    }
}
