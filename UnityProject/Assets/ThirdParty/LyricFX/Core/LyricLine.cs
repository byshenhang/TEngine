using LyricFX.Core.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LyricFX.Managers.LyricManager;

namespace LyricFX.Managers
{
    // �ڲ��� - ��ʾһ�и��
    public class LyricLine
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string LayoutId { get; set; }
        public string EffectId { get; set; }
        public GameObject GameObject { get; set; }
        public GameObject Container => GameObject;
        public List<GameObject> Characters { get; set; }

        // �������м�Э����֧��
        public ILineEffectCoordinator EffectCoordinator { get; set; }

        // �������ַ���Ч���б�
        public List<ILyricEffect> CharacterEffects { get; set; }

        // �������м�״̬����
        public LineState State { get; set; }

        // ��������������
        public float Progress => EffectCoordinator?.Progress ?? 0f;

        public LyricLine()
        {
            Characters = new List<GameObject>();
            CharacterEffects = new List<ILyricEffect>();
            State = LineState.Created;
        }
    }

}
