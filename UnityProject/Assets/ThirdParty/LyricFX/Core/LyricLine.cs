using LyricFX.Core.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LyricFX.Managers.LyricManager;

namespace LyricFX.Managers
{
    // 内部类 - 表示一行歌词
    public class LyricLine
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string LayoutId { get; set; }
        public string EffectId { get; set; }
        public GameObject GameObject { get; set; }
        public GameObject Container => GameObject;
        public List<GameObject> Characters { get; set; }

        // 新增：行级协调器支持
        public ILineEffectCoordinator EffectCoordinator { get; set; }

        // 新增：字符级效果列表
        public List<ILyricEffect> CharacterEffects { get; set; }

        // 新增：行级状态管理
        public LineState State { get; set; }

        // 新增：进度属性
        public float Progress => EffectCoordinator?.Progress ?? 0f;

        public LyricLine()
        {
            Characters = new List<GameObject>();
            CharacterEffects = new List<ILyricEffect>();
            State = LineState.Created;
        }
    }

}
