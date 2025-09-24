using TagLib;
using UnityEngine;

/// <summary>
/// 音频文件元数据信息类
/// 用于存储从音频文件中读取的标签信息和技术参数
/// </summary>
public class AudioMeta
{
    /// <summary>
    /// 歌曲标题/曲名
    /// 对应音频文件的Title标签，如果为空则可使用文件名作为备用标题
    /// </summary>
    public string Title;
    
    /// <summary>
    /// 艺术家/演唱者列表
    /// 可能包含多个艺术家，如主唱、合唱、特邀嘉宾等
    /// 对应音频文件的Performers标签
    /// </summary>
    public string[] Artists;
    
    /// <summary>
    /// 专辑名称
    /// 歌曲所属的专辑、EP或单曲集名称
    /// 对应音频文件的Album标签
    /// </summary>
    public string Album;
    
    /// <summary>
    /// 发行年份
    /// 歌曲或专辑的发布年份，使用uint类型避免负数
    /// 对应音频文件的Year标签
    /// </summary>
    public uint Year;
    
    /// <summary>
    /// 音乐风格/流派列表
    /// 如：流行、摇滚、古典、电子等音乐类型标签
    /// 一首歌可能属于多个流派，对应音频文件的Genres标签
    /// </summary>
    public string[] Genres;
    
    /// <summary>
    /// 音频时长
    /// 歌曲的播放时长，使用TimeSpan精确表示时分秒
    /// 从音频文件的技术属性中读取，不依赖标签信息
    /// </summary>
    public System.TimeSpan Duration;
    
    /// <summary>
    /// 音频比特率（kbps）
    /// 表示音频质量，常见值：128kbps、192kbps、320kbps等
    /// 数值越高音质越好，文件也越大
    /// </summary>
    public int BitrateKbps;
    
    /// <summary>
    /// 音频采样率（Hz）
    /// 表示每秒采样次数，常见值：44100Hz、48000Hz、96000Hz等
    /// 影响音频的频率响应范围和质量
    /// </summary>
    public int SampleRate;
    
    /// <summary>
    /// 音频声道数
    /// 1=单声道(Mono)，2=立体声(Stereo)，6=5.1环绕声等
    /// 决定音频的空间感和播放设备兼容性
    /// </summary>
    public int Channels;
    
    /// <summary>
    /// 专辑封面图片数据
    /// 存储嵌入在音频文件中的封面图片的原始字节数据
    /// 通常为JPEG或PNG格式，可转换为Unity的Texture2D使用
    /// </summary>
    public byte[] CoverBytes;
    
    /// <summary>
    /// 备注/评论信息
    /// 音频文件中的自由文本字段，可包含任意说明信息
    /// 如制作人员、录制信息、版权声明等
    /// </summary>
    public string Comment;
    
    /// <summary>
    /// 歌词文本
    /// 嵌入在音频文件中的歌词内容
    /// 注意：这通常是纯文本歌词，不包含时间轴信息
    /// 带时间轴的歌词通常存储在单独的LRC文件中
    /// </summary>
    public string Lyrics;
}

/// <summary>
/// 音频标签读取工具类
/// 提供从音频文件中提取元数据信息的静态方法
/// </summary>
public static class AudioTagUtil
{
    /// <summary>
    /// 从音频文件中读取元数据信息
    /// 使用TagLib库解析音频文件的标签和技术属性
    /// </summary>
    /// <param name="path">音频文件的完整路径</param>
    /// <param name="fallbackTitle">备用标题，当音频文件没有Title标签时使用</param>
    /// <returns>包含音频元数据的AudioMeta对象</returns>
    /// <exception cref="System.IO.FileNotFoundException">当指定路径的文件不存在时抛出</exception>
    /// <exception cref="TagLib.UnsupportedFormatException">当音频文件格式不受支持时抛出</exception>
    public static AudioMeta ReadMetaFromFile(string path, string fallbackTitle = null)
    {
        using (var file = TagLib.File.Create(path))
        {
            var t = file.Tag;        // 获取标签信息（艺术家、专辑等）
            var p = file.Properties; // 获取技术属性（时长、比特率等）

            var meta = new AudioMeta
            {
                // 优先使用文件中的标题，如果为空则使用备用标题
                Title = string.IsNullOrEmpty(t.Title) ? fallbackTitle : t.Title,
                Artists = t.Performers,           // 演唱者列表
                Album = t.Album,                  // 专辑名称
                Year = t.Year,                    // 发行年份
                Genres = t.Genres,                // 音乐风格列表
                Duration = p.Duration,            // 音频时长
                BitrateKbps = p.AudioBitrate,     // 音频比特率
                SampleRate = p.AudioSampleRate,   // 采样率
                Channels = p.AudioChannels,       // 声道数
                Comment = t.Comment,              // 备注信息
                Lyrics = t.Lyrics                 // 歌词文本
            };

            // 提取第一张封面图片（如果存在）
            if (t.Pictures != null && t.Pictures.Length > 0)
                meta.CoverBytes = t.Pictures[0].Data?.Data;

            return meta;
        }
    }

    public static Texture2D CoverToTexture(byte[] coverBytes)
    {
        if (coverBytes == null || coverBytes.Length == 0) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(coverBytes); // ���� JPG/PNG
        return tex;
    }
}
