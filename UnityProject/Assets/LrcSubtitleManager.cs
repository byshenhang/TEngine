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
    public Text subtitleText;  // 字幕显示的 Text 组件
    public TextAsset lrcFileText;  // 字幕显示的 Text 组件
    //public AudioSource audioSource;  // 音频的 AudioSource
    public AudioClip audioClip;  // 音频文件
    public Font subtitleFont;  // 字体文件
    public AudioSourcePlus sourcePlus;
    private List<LrcLine> lrcLines = new List<LrcLine>();  // 存储解析后的 LRC 字幕内容
    private int currentSubtitleIndex = 0;  // 当前字幕的索引

    void Start()
    {
        // 设置字幕的字体
        //subtitleText.font = subtitleFont;

        // 加载并解析 LRC 文件
        LoadLrcFile();

        // 播放音频
        //audioSource.clip = audioClip;
        //audioSource.Play();

        // 开始播放字幕
        StartCoroutine(PlaySubtitles());
    }

    void LoadLrcFile()
    {

        string[] lines = lrcFileText.text.Split("\n");
        foreach (var line in lines)
        {
            if (line.StartsWith("[") && line.Contains("]"))
            {
                // 提取时间戳和字幕
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

        // 播放完毕后清空字幕
        subtitleText.text = "";
    }

    [System.Serializable]
    public class LrcLine
    {
        public float Time;
        public string Text;
    }
}
