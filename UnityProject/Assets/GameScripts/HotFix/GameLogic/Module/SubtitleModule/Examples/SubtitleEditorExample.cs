//#if UNITY_EDITOR
//using UnityEngine;
//using UnityEditor;
//using System.Collections.Generic;

//namespace GameLogic.Examples
//{
//    /// <summary>
//    /// 字幕编辑器使用示例
//    /// </summary>
//    public class SubtitleEditorExample
//    {
//        /// <summary>
//        /// 创建示例字幕序列资源
//        /// </summary>
//        [MenuItem("GameLogic/Subtitle/Create Example Assets")]
//        public static void CreateExampleAssets()
//        {
//            CreateSimpleExampleAsset();
//            CreateComplexExampleAsset();
//            CreateLyricExampleAsset();
            
//            AssetDatabase.SaveAssets();
//            AssetDatabase.Refresh();
            
//            Debug.Log("示例字幕序列资源已创建完成！");
//        }
        
//        /// <summary>
//        /// 创建简单示例
//        /// </summary>
//        private static void CreateSimpleExampleAsset()
//        {
//            var asset = ScriptableObject.CreateInstance<SubtitleSequenceAsset>();
            
//            asset.SequenceName = "简单淡入效果";
//            asset.Description = "展示基础的淡入淡出效果";
//            asset.Version = "1.0";
            
//            asset.Config = new SubtitleSequenceConfig
//            {
//                SequenceType = SubtitleSequenceType.Simple,
//                DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
//                Text = "Hello World!",
//                Position = Vector3.zero,
//                FontSize = 24,
//                TextColor = Color.white,
                
//                StartDelay = 0.5f,
//                CharacterInterval = 0.1f,
//                HoldDuration = 2f,
//                FadeOutDuration = 1f,
                
//                Effects = new List<SubtitleEffectConfig>
//                {
//                    new FadeEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 0.8f,
//                        Delay = 0f,
//                        AlphaStart = 0f,
//                        AlphaEnd = 1f,
//                        FadeIn = true,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    }
//                }
//            };
            
//            string path = "Assets/GameScripts/HotFix/GameLogic/Module/SubtitleModule/Examples/SimpleExample.asset";
//            AssetDatabase.CreateAsset(asset, path);
//        }
        
//        /// <summary>
//        /// 创建复杂示例
//        /// </summary>
//        private static void CreateComplexExampleAsset()
//        {
//            var asset = ScriptableObject.CreateInstance<SubtitleSequenceAsset>();
            
//            asset.SequenceName = "复杂多效果组合";
//            asset.Description = "展示多种效果的组合使用";
//            asset.Version = "1.0";
            
//            asset.Config = new SubtitleSequenceConfig
//            {
//                SequenceType = SubtitleSequenceType.Complex,
//                DisplayMode = SubtitleDisplayMode.CharacterByCharacter,
//                Text = "Amazing Effects!",
//                Position = new Vector3(0, 2, 0),
//                FontSize = 32,
//                TextColor = Color.cyan,
                
//                StartDelay = 0.2f,
//                CharacterInterval = 0.08f,
//                HoldDuration = 3f,
//                FadeOutDuration = 1.5f,
                
//                Effects = new List<SubtitleEffectConfig>
//                {
//                    // 进场效果组合
//                    new FadeEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 0.6f,
//                        Delay = 0f,
//                        AlphaStart = 0f,
//                        AlphaEnd = 1f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 0.8f,
//                        Delay = 0.1f,
//                        ScaleStart = Vector3.zero,
//                        ScaleEnd = Vector3.one,
//                        Bounce = true,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
//                    new BlurEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 1f,
//                        Delay = 0f,
//                        BlurStart = 10f,
//                        BlurEnd = 0f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
                    
//                    // 停留效果
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Stay,
//                        Duration = 2f,
//                        Delay = 0.5f,
//                        ScaleStart = Vector3.one,
//                        ScaleEnd = Vector3.one * 1.1f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
                    
//                    // 离场效果
//                    new FadeEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1f,
//                        Delay = 0f,
//                        AlphaStart = 1f,
//                        AlphaEnd = 0f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1.2f,
//                        Delay = 0.2f,
//                        ScaleStart = Vector3.one,
//                        ScaleEnd = Vector3.zero,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    }
//                }
//            };
            
//            string path = "Assets/GameScripts/HotFix/GameLogic/Module/SubtitleModule/Examples/ComplexExample.asset";
//            AssetDatabase.CreateAsset(asset, path);
//        }
        
//        /// <summary>
//        /// 创建歌词示例
//        /// </summary>
//        private static void CreateLyricExampleAsset()
//        {
//            var asset = ScriptableObject.CreateInstance<SubtitleSequenceAsset>();
            
//            asset.SequenceName = "歌词效果";
//            asset.Description = "专门用于歌词显示的效果配置";
//            asset.Version = "1.0";
            
//            asset.Config = new SubtitleSequenceConfig
//            {
//                SequenceType = SubtitleSequenceType.Lyric,
//                DisplayMode = SubtitleDisplayMode.WordByWord,
//                Text = "♪ Beautiful music flows ♪",
//                Position = new Vector3(0, -2, 0),
//                FontSize = 28,
//                TextColor = Color.magenta,
                
//                StartDelay = 0.3f,
//                WordInterval = 0.4f,
//                HoldDuration = 4f,
//                FadeOutDuration = 2f,
                
//                Effects = new List<SubtitleEffectConfig>
//                {
//                    // 音乐节拍感的进场
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 0.3f,
//                        Delay = 0f,
//                        ScaleStart = Vector3.one * 0.8f,
//                        ScaleEnd = Vector3.one,
//                        Bounce = true,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
//                    new FadeEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Enter,
//                        Duration = 0.4f,
//                        Delay = 0f,
//                        AlphaStart = 0f,
//                        AlphaEnd = 1f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
                    
//                    // 停留时的轻微律动
//                    new ScaleEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Stay,
//                        Duration = 3f,
//                        Delay = 0f,
//                        ScaleStart = Vector3.one,
//                        ScaleEnd = Vector3.one * 1.05f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
                    
//                    // 优雅的离场
//                    new FadeEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1.5f,
//                        Delay = 0f,
//                        AlphaStart = 1f,
//                        AlphaEnd = 0f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    },
//                    new BlurEffectConfig
//                    {
//                        Phase = SubtitleEffectPhase.Exit,
//                        Duration = 1.8f,
//                        Delay = 0.2f,
//                        BlurStart = 0f,
//                        BlurEnd = 15f,
//                        AnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
//                    }
//                }
//            };
            
//            string path = "Assets/GameScripts/HotFix/GameLogic/Module/SubtitleModule/Examples/LyricExample.asset";
//            AssetDatabase.CreateAsset(asset, path);
//        }
        
//        /// <summary>
//        /// 打开字幕序列编辑器
//        /// </summary>
//        [MenuItem("GameLogic/Subtitle/Open Sequence Editor")]
//        public static void OpenSequenceEditor()
//        {
//            SubtitleSequenceEditorWindow.ShowWindow();
//        }
        
//        /// <summary>
//        /// 打开字幕预览工具
//        /// </summary>
//        [MenuItem("GameLogic/Subtitle/Open Preview Tool")]
//        public static void OpenPreviewTool()
//        {
//            SubtitlePreviewTool.ShowWindow();
//        }
        
//        /// <summary>
//        /// 创建测试场景
//        /// </summary>
//        [MenuItem("GameLogic/Subtitle/Create Test Scene")]
//        public static void CreateTestScene()
//        {
//            // 创建测试相机
//            var cameraObj = new GameObject("Test Camera");
//            var camera = cameraObj.AddComponent<Camera>();
//            camera.transform.position = new Vector3(0, 0, -10);
            
//            // 创建字幕父对象
//            var subtitleParent = new GameObject("Subtitle Parent");
            
//            // 创建测试脚本对象
//            var testObj = new GameObject("Subtitle Test");
//            var testScript = testObj.AddComponent<SubtitlePhaseTest>();
//            testScript.parentTransform = subtitleParent.transform;
            
//            // 选中测试对象
//            Selection.activeGameObject = testObj;
            
//            Debug.Log("测试场景已创建！可以使用SubtitlePhaseTest组件进行测试。");
//        }
//    }
//}
//#endif