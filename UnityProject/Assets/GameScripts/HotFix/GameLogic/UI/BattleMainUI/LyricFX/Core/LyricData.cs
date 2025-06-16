using System.Collections.Generic;

namespace LyricFX.Core
{
    /// <summary>
    /// 歌词行数据
    /// </summary>
    public class LyricLine
    {
        public int Index { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public string Text { get; set; }
        public List<LyricCharacter> Characters { get; } = new List<LyricCharacter>();
        public Dictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 歌词字符数据
    /// </summary>
    public class LyricCharacter
    {
        public char Character { get; set; }
        public int Index { get; set; }
        public int LineIndex { get; set; }
        public Dictionary<string, object> UserData { get; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 歌词序列数据
    /// </summary>
    public class LyricSequence
    {
        public List<LyricLine> Lines { get; } = new List<LyricLine>();
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public float TotalDuration { get; set; }
    }
}
