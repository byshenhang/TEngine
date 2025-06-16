namespace LyricFX.States
{
    /// <summary>
    /// 定义字符的生命周期状态
    /// </summary>
    public enum CharacterState
    {
        Waiting,    // 等待显示
        Enter,      // 入场阶段
        Stay,       // 停留阶段
        Exit,       // 退场阶段
        Complete    // 完成显示
    }
}
