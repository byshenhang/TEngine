using System;
using Cysharp.Threading.Tasks;
using Launcher;
using TEngine;
using UnityEngine.SceneManagement;

namespace Procedure
{
    public class ProcedureStartGame : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        protected override void OnEnter(IFsm<IProcedureModule> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            StartGame().Forget();
        }

        private async UniTaskVoid StartGame()
        {
            await UniTask.Yield();
            var scene = await GameModule.Scene.LoadSceneAsync(
                 "main",                           // 场景定位地址
                 LoadSceneMode.Single,             // 单场景模式（替换当前场景）
                 false,                            // 不挂起加载
                 100,                              // 优先级
                 true                     // 加载后回收垃圾
             );

            Log.Info($"场景切换完成: {scene.name}");
            LauncherMgr.HideAll();
            
            // 场景加载完成后显示UI3D
            GameApp.ShowMainSceneUI();
        }
    }
}