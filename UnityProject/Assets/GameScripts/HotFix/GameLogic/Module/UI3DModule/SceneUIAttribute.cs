using System;

namespace GameLogic
{
    /// <summary>
    /// u7528u4e8eu6807u8bb0u573au666f3D UIu7a97u53e3u7684u7279u6027u7c7bu3002
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SceneUIAttribute : Attribute
    {
        /// <summary>
        /// UIu7684u6807u8bc6u7b26uff0cu7528u4e8eu5339u914du573au666fu4e2du7684UIu951au70b9u3002
        /// </summary>
        public string Identifier { get; }
        
        /// <summary>
        /// u521bu5efau4e00u4e2au65b0u7684SceneUIAttributeu5b9eu4f8bu3002
        /// </summary>
        /// <param name="identifier">u4e0eUI3DAnchorPointu5bf9u5e94u7684u6807u8bc6u7b26</param>
        public SceneUIAttribute(string identifier)
        {
            Identifier = identifier;
        }
    }
}
