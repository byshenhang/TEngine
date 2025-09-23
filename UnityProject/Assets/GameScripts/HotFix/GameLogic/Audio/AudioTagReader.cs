using TagLib;
using UnityEngine;

public class AudioMeta
{
    public string Title;
    public string[] Artists;
    public string Album;
    public uint Year;
    public string[] Genres;
    public System.TimeSpan Duration;
    public int BitrateKbps;
    public int SampleRate;
    public int Channels;
    public byte[] CoverBytes;
    public string Comment;
    public string Lyrics;
}

public static class AudioTagUtil
{
    public static AudioMeta ReadMetaFromFile(string path, string fallbackTitle = null)
    {
        using (var file = TagLib.File.Create(path))
        {
            var t = file.Tag;
            var p = file.Properties;

            var meta = new AudioMeta
            {
                Title = string.IsNullOrEmpty(t.Title) ? fallbackTitle : t.Title,
                Artists = t.Performers,
                Album = t.Album,
                Year = t.Year,
                Genres = t.Genres,
                Duration = p.Duration,
                BitrateKbps = p.AudioBitrate,
                SampleRate = p.AudioSampleRate,
                Channels = p.AudioChannels,
                Comment = t.Comment,
                Lyrics = t.Lyrics
            };

            if (t.Pictures != null && t.Pictures.Length > 0)
                meta.CoverBytes = t.Pictures[0].Data?.Data;

            return meta;
        }
    }

    public static Texture2D CoverToTexture(byte[] coverBytes)
    {
        if (coverBytes == null || coverBytes.Length == 0) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(coverBytes); // ºÊ»› JPG/PNG
        return tex;
    }
}
