using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GameConfig.item;
using GameLogic;
using TEngine;
using UnityEngine;
#pragma warning disable CS0436


/// <summary>
/// 游戏App。
/// </summary>
public partial class GameApp
{
    private static List<Assembly> _hotfixAssembly;

    /// <summary>
    /// 热更域App主入口。
    /// </summary>
    /// <param name="objects"></param>
    public static void Entrance(object[] objects)
    {
        GameEventHelper.Init();
        _hotfixAssembly = (List<Assembly>)objects[0];
        Log.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
        Log.Warning("======= Entrance GameApp =======");
        
        // 初始化战斗模块
        CombatModule.Instance.Initialize();
        // 模块已继承并实现IUpdate接口以进行自动更新
        
        Utility.Unity.AddDestroyListener(Release);
        StartGameLogic();
    }
    
    private static void StartGameLogic()
    {
        GameEvent.Get<ILoginUI>().ShowLoginUI();

        //GameModule.UI.ShowUIAsync<BattleMainUI>();
        //GameModule.UI3D.ShowUI3D<BattleMainUI>(Vector3.zero, Quaternion.identity, null);
        // UI3D调用移到场景加载完成后，避免锚点未注册的问题
        // GameModule.UI3D.ShowUI3DAtAnchor<BattleMainUI>("MainUI", null);
    }
    
    /// <summary>
    /// 在主场景加载完成后显示UI3D
    /// 此方法应该在场景加载完成后调用
    /// </summary>
    public static void ShowMainSceneUI()
    {
        // 延迟一帧确保锚点已注册
        UniTask.DelayFrame(1).ContinueWith(() =>
        {
            GameModule.UI3D.ShowUI3DAtAnchor<BattleMainUI>("MainUI", null);
        }).Forget();
    }
    
    private static void Release()
    {
        // 关闭战斗模块
        if (CombatModule.Instance != null)
        {
            CombatModule.Instance.Shutdown();
        }
        
        SingletonSystem.Release();
        Log.Warning("======= Release GameApp =======");
    }
}