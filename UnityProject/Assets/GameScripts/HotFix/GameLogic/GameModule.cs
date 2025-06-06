using GameLogic;
using TEngine;
using Object = UnityEngine.Object;

public class GameModule
{
    #region 框架模块

    /// <summary>
    /// 获取游戏基础模块。
    /// </summary>
    public static RootModule Base
    {
        get => _base ??= Object.FindObjectOfType<RootModule>();
        private set => _base = value;
    }

    private static RootModule _base;

    /// <summary>
    /// 获取调试模块。
    /// </summary>
    public static IDebuggerModule Debugger
    {
        get => _debugger ??= Get<IDebuggerModule>();
        private set => _debugger = value;
    }


    private static IDebuggerModule _debugger;

    /// <summary>
    /// 获取有限状态机模块。
    /// </summary>
    public static IFsmModule Fsm => _fsm ??= Get<IFsmModule>();

    private static IFsmModule _fsm;

    /// <summary>
    /// 流程管理模块。
    /// </summary>
    public static IProcedureModule Procedure => _procedure ??= Get<IProcedureModule>();

    private static IProcedureModule _procedure;

    /// <summary>
    /// 获取资源模块。
    /// </summary>
    public static IResourceModule Resource => _resource ??= Get<IResourceModule>();

    private static IResourceModule _resource;

    /// <summary>
    /// 获取战斗模块。
    /// </summary>
    public static CombatModule Combat => _combat ??= CombatModule.Instance;

    private static CombatModule _combat;

    /// <summary>
    /// 获取音频模块。
    /// </summary>
    public static IAudioModule Audio => _audio ??= Get<IAudioModule>();

    private static IAudioModule _audio;

    /// <summary>
    /// 获取UI模块。
    /// </summary>
    public static UIModule UI => _ui ??= UIModule.Instance;

    private static UIModule _ui;

    /// <summary>
    /// 获取场景模块。
    /// </summary>
    public static ISceneModule Scene => _scene ??= Get<ISceneModule>();

    private static ISceneModule _scene;

    /// <summary>
    /// 获取计时器模块。
    /// </summary>
    public static ITimerModule Timer => _timer ??= Get<ITimerModule>();

    private static ITimerModule _timer;

    /// <summary>
    /// 获取本地化模块。
    /// </summary>
    public static ILocalizationModule Localization => _localization ??= Get<ILocalizationModule>();
    
    private static ILocalizationModule _localization;
    
    /// <summary>
    /// 获取3D UI模块。
    /// </summary>
    public static UI3DModule UI3D => _ui3d ??= UI3DModule.Instance;

    private static UI3DModule _ui3d;
    
    /// <summary>
    /// 获取XR玩家模块。
    /// </summary>
    public static XRPlayerModule XRPlayer => _xrPlayer ??= XRPlayerModule.Instance;

    private static XRPlayerModule _xrPlayer;
    
    /// <summary>
    /// 获取XR交互模块。
    /// </summary>
    public static XRIModule XRI => _xri ??= XRIModule.Instance;

    private static XRIModule _xri;
    
    ///// <summary>
    ///// 获取XR交互模块。
    ///// </summary>
    //public static XRInteractionManager XRInteraction => _xrInteraction ??= XRInteractionManager.Instance;

    //private static XRInteractionManager _xrInteraction;
    #endregion
    
    /// <summary>
    /// 获取游戏框架模块类。
    /// </summary>
    /// <typeparam name="T">游戏框架模块类。</typeparam>
    /// <returns>游戏框架模块实例。</returns>
    private static T Get<T>() where T : class
    {
        T module = ModuleSystem.GetModule<T>();

        Log.Assert(condition: module != null, $"{typeof(T)} is null");

        return module;
    }
    
    public static void Shutdown()
    {
        Log.Info("GameModule Shutdown");
            
        _base = null;
        _debugger = null;
        _fsm = null;
        _procedure = null;
        _resource = null;
        _audio = null;
        _ui = null;
        _scene = null;
        _timer = null;
        _localization = null;
        _ui3d = null;
        _xrPlayer = null;
        _xri = null;
        
        // 战斗模块作为Singleton由GameApp.Release处理
        _combat = null;
        //_xrInteraction = null;
    }
}