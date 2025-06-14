using System.Collections;
using System.Collections.Generic;
using System.IO;
using TelePresent.AudioSyncPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using TextAsset = UnityEngine.TextAsset;

public class LrcSubtitleManager : MonoBehaviour
{
    public Text subtitleText;  // ��Ļ��ʾ�� Text ���
    public TextAsset lrcFileText;  // ��Ļ��ʾ�� Text ���
    //public AudioSource audioSource;  // ��Ƶ�� AudioSource
    public AudioClip audioClip;  // ��Ƶ�ļ�
    public Font subtitleFont;  // �����ļ�
    public AudioSourcePlus sourcePlus;
    private List<LrcLine> lrcLines = new List<LrcLine>();  // �洢������� LRC ��Ļ����
    private int currentSubtitleIndex = 0;  // ��ǰ��Ļ������

    void Start()
    {
        // ������Ļ������
        //subtitleText.font = subtitleFont;

        // ���ز����� LRC �ļ�
        LoadLrcFile();

        // ������Ƶ
        //audioSource.clip = audioClip;
        //audioSource.Play();

        // ��ʼ������Ļ
        StartCoroutine(PlaySubtitles());
    }

    void LoadLrcFile()
    {

        string[] lines = lrcFileText.text.Split("\n");
        foreach (var line in lines)
        {
            if (line.StartsWith("[") && line.Contains("]"))
            {
                // ��ȡʱ�������Ļ
                string timeString = line.Substring(1, line.IndexOf(']') - 1);
                string text = line.Substring(line.IndexOf(']') + 1).Trim();

                float timeInSeconds = ConvertLrcTimeToSeconds(timeString);
                lrcLines.Add(new LrcLine { Time = timeInSeconds, Text = text });
            }
        }
    }

    float ConvertLrcTimeToSeconds(string lrcTime)
    {
        string[] timeParts = lrcTime.Split(':');
        string[] secondParts = timeParts[1].Split('.');
        float minutes = float.Parse(timeParts[0]);
        float seconds = float.Parse(secondParts[0]);
        float milliseconds = float.Parse(secondParts[1]) / 1000f;

        return minutes * 60f + seconds + milliseconds;
    }

    IEnumerator PlaySubtitles()
    {
        while (sourcePlus.isPlaying)
        {
            if (currentSubtitleIndex < lrcLines.Count && sourcePlus.audioSource.time >= lrcLines[currentSubtitleIndex].Time)
            {
                subtitleText.text = lrcLines[currentSubtitleIndex].Text;
                currentSubtitleIndex++;
            }
            yield return null;
        }

        // ������Ϻ������Ļ
        subtitleText.text = "";
    }

    [System.Serializable]
    public class LrcLine
    {
        public float Time;
        public string Text;
    }
}
