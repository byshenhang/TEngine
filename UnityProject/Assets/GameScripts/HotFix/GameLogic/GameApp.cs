using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameConfig.item;
using GameLogic;
using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        ShowMainSceneUIAsync();
    }
    
    /// <summary>
    /// 在主场景加载完成后显示UI3D
    /// 此方法应该在场景加载完成后调用
    /// </summary>
    public static async Task ShowMainSceneUIAsync()
    {
        // 延迟一帧确保锚点已注册
        var scene = await GameModule.Scene.LoadSceneAsync(
               "Demo01",                           // 场景定位地址
               LoadSceneMode.Single,             // 单场景模式（替换当前场景）
               false,                            // 不挂起加载
               100,                              // 优先级
               true                     // 加载后回收垃圾
               ,OnLoadSuccessAsync
           );


        Log.Info($"准备进入打开ManiUI: {scene.name}");
        await UniTask.Delay(1000).ContinueWith(() =>
        {
            Log.Info($"打开BattleMainUI: {scene.name}");
            GameModule.UI3D.ShowUI3D<BattleMainUI>(new Vector3(88f, 2f, 93f), Quaternion.identity).Forget();
            Log.Info($"结束打开BattleMainUI: {scene.name}");
        });

        Log.Info($"场景切换完成: {scene.name}");
    }

    private static  void OnLoadSuccessAsync(float obj)
    {
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