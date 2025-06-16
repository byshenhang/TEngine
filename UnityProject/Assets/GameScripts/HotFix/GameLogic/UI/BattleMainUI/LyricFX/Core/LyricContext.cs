using System.Collections.Generic;
using TMPro;
using UnityEngine;
using LyricFX.Rendering;
using LyricFX.States;

namespace LyricFX.Core
{
    /// <summary>
    /// 全局歌词上下文，保存整体歌词的信息和共享数据
    /// </summary>
    public class LyricContext
    {
        public LyricSequence Sequence { get; set; }
        public float CurrentTime { get; set; }
        public int CurrentLineIndex { get; set; }
        public Transform Container { get; set; }
        public Dictionary<string, object> SharedData { get; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 字符级上下文，包含单个字符的渲染和状态信息
    /// </summary>
    public class CharacterContext
    {
        public LyricCharacter Character { get; }
        public ICharacterRenderer Renderer { get; }
        public TextMeshProUGUI TextComponent => Renderer?.TextComponent;
        public CharacterState CurrentState { get; internal set; }
        public int GlobalIndex { get; }
        public int LineIndex { get; }
        public float NormalizedProgress { get; set; }
        public Dictionary<string, object> SharedData { get; } = new Dictionary<string, object>();
        
        public CharacterContext(LyricCharacter character, ICharacterRenderer renderer)
        {
            Character = character;
            Renderer = renderer;
            GlobalIndex = character.Index;
            LineIndex = character.LineIndex;
        }
        
        /// <summary>
        /// 获取或创建组件
        /// </summary>
        public T GetOrCreateComponent<T>() where T : Component
        {
            return Renderer.GetOrCreateComponent<T>();
        }
    }
}
