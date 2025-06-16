# LyricFX 框架设计文档 - 第1部分：概述与目录结构

## 1. 概述

LyricFX是一个基于Unity和UniTask的高性能、可扩展的歌词效果系统。它旨在为游戏和应用程序提供专业级的歌词动画效果，支持逐字符控制，多阶段过渡效果，以及丰富的自定义配置。

### 1.1 设计目标

- 提供精确到单个字符的动画控制
- 支持Enter/Stay/Exit三阶段效果
- 基于UniTask的高效异步操作
- 通过ScriptableObject实现直观的效果配置
- 良好的可扩展性，易于添加新效果
- 完善的事件系统和状态管理

### 1.2 主要特性

- 基于TextMeshPro的高质量文本渲染
- 多种内置效果（淡入淡出、缩放、模糊等）
- 可自定义的效果组合与序列
- 基于状态机的字符生命周期管理
- 优化的对象池和资源管理
- 完整的编辑器支持和预览工具

## 2. 框架目录结构

```
Assets/
└── LyricFX/
    ├── Runtime/
    │   ├── Core/
    │   │   ├── LyricManager.cs                # 整体歌词管理器
    │   │   ├── LyricData/
    │   │   │   ├── LyricLine.cs               # 歌词行数据
    │   │   │   ├── LyricCharacter.cs          # 歌词字符数据
    │   │   │   └── LyricSequence.cs           # 歌词序列
    │   │   │
    │   │   ├── Context/
    │   │   │   ├── LyricContext.cs            # 全局上下文
    │   │   │   └── CharacterContext.cs        # 字符上下文
    │   │   │
    │   │   └── Events/
    │   │       ├── LyricEventSystem.cs        # 事件系统
    │   │       └── LyricEvents.cs             # 事件定义
    │   │
    │   ├── Parser/
    │   │   ├── ILyricParser.cs                # 解析器接口
    │   │   ├── BaseLyricParser.cs             # 基础解析器
    │   │   ├── LRCParser.cs                   # LRC格式解析器
    │   │   ├── ExtendedLRCParser.cs           # 扩展LRC(支持效果标签)
    │   │   └── LyricParserFactory.cs          # 解析器工厂
    │   │
    │   ├── Render/
    │   │   ├── Character/
    │   │   │   ├── CharacterRendererBase.cs   # 字符渲染基类
    │   │   │   ├── TMProCharacterRenderer.cs  # TextMeshPro实现
    │   │   │   └── CharacterRendererFactory.cs # 渲染器工厂
    │   │   │
    │   │   ├── Line/
    │   │   │   ├── LineRendererBase.cs        # 行渲染基类
    │   │   │   └── TMProLineRenderer.cs       # TextMeshPro行渲染
    │   │   │
    │   │   └── PoolSystem/
    │   │       ├── ObjectPoolBase.cs          # 对象池基类
    │   │       └── TMProPool.cs               # TextMeshPro对象池
    │   │
    │   ├── States/
    │   │   ├── CharacterState.cs              # 字符状态定义
    │   │   ├── StateManager.cs                # 状态管理器
    │   │   ├── StateTransition.cs             # 状态转换
    │   │   └── StateTrigger/
    │   │       ├── IStateTrigger.cs           # 状态触发器接口
    │   │       ├── TimedTrigger.cs            # 基于时间的触发
    │   │       └── ProgressTrigger.cs         # 基于进度的触发
    │   │
    │   ├── Effect/
    │   │   ├── Core/
    │   │   │   ├── BaseEffect.cs              # 效果基类
    │   │   │   ├── EffectAdapter.cs           # 效果适配器
    │   │   │   ├── EffectChain.cs             # 效果链
    │   │   │   └── GroupEffectController.cs   # 组效果控制器
    │   │   │
    │   │   ├── Parameters/
    │   │   │   ├── EffectParameters.cs        # 参数基类
    │   │   │   ├── FadeParameters.cs
    │   │   │   ├── ScaleParameters.cs
    │   │   │   ├── BlurParameters.cs
    │   │   │   └── ...
    │   │   │
    │   │   ├── Implementations/
    │   │   │   ├── VisualEffects/
    │   │   │   │   ├── FadeEffect.cs
    │   │   │   │   ├── ScaleEffect.cs
    │   │   │   │   ├── BlurEffect.cs
    │   │   │   │   ├── ColorEffect.cs
    │   │   │   │   ├── ShakeEffect.cs
    │   │   │   │   └── ...
    │   │   │   │
    │   │   │   └── CompoundEffects/
    │   │   │       ├── TypewriterEffect.cs
    │   │   │       ├── WaveEffect.cs
    │   │   │       └── ...
    │   │   │
    │   │   └── Factory/
    │   │       └── EffectFactory.cs           # 效果工厂
    │   │
    │   ├── Config/
    │   │   ├── ScriptableObjects/
    │   │   │   ├── BaseEffectSO.cs            # 效果配置基类
    │   │   │   ├── EffectDefinitions/
    │   │   │   │   ├── FadeEffectSO.cs
    │   │   │   │   ├── ScaleEffectSO.cs
    │   │   │   │   ├── BlurEffectSO.cs
    │   │   │   │   └── ...
    │   │   │   │
    │   │   │   ├── Phases/
    │   │   │   │   ├── PhaseEffectCollectionSO.cs  # 阶段效果集
    │   │   │   │   └── PhaseTransitionSO.cs        # 阶段转换配置
    │   │   │   │
    │   │   │   └── LyricEffectPresetSO.cs     # 完整效果预设
    │   │   │
    │   │   └── Runtime/
    │   │       ├── LyricConfig.cs             # 运行时配置
    │   │       └── ConfigProvider.cs          # 配置提供者
    │   │
    │   ├── Async/
    │   │   ├── LyricTaskScheduler.cs          # UniTask调度器
    │   │   ├── EffectExecutor.cs              # 效果异步执行
    │   │   ├── AsyncExtensions.cs             # 异步扩展方法
    │   │   └── ProgressTracker.cs             # 进度追踪
    │   │
    │   └── Utils/
    │       ├── TMProExtensions.cs             # TextMeshPro扩展
    │       ├── AnimationHelper.cs             # 动画辅助
    │       ├── MathUtils.cs                   # 数学工具
    │       └── StringUtils.cs                 # 字符串工具
    │
    ├── Editor/
    │   ├── Windows/
    │   │   ├── EffectPreviewWindow.cs         # 效果预览窗口
    │   │   └── LyricEditorWindow.cs           # 歌词编辑器
    │   │
    │   ├── Inspectors/
    │   │   ├── LyricEffectPresetEditor.cs     # 预设检视器
    │   │   └── EffectSOEditors.cs             # 效果配置检视器
    │   │
    │   ├── PropertyDrawers/
    │   │   └── EffectParameterDrawer.cs       # 参数绘制器
    │   │
    │   └── Utilities/
    │       ├── PresetImporter.cs              # 预设导入工具
    │       └── TextureGenerator.cs            # 纹理生成工具
    │
    ├── Resources/
    │   ├── DefaultPresets/                    # 默认预设
    │   └── Icons/                             # 编辑器图标
    │
    ├── Samples~/
    │   ├── BasicLyrics/                       # 基础歌词示例
    │   ├── KaraokeSystem/                     # 卡拉OK系统
    │   └── CustomEffects/                     # 自定义效果
    │
    └── Documentation~/
        ├── GettingStarted.md                  # 快速入门
        ├── API.md                             # API文档
        └── ExtendingEffects.md                # 扩展教程
```

### 2.1 主要模块说明

- **Core**: 系统的核心组件，包含管理器和基础数据结构
- **Parser**: 解析歌词文件的组件
- **Render**: 负责渲染和显示字符的组件
- **States**: 状态管理系统，控制字符的生命周期
- **Effect**: 效果系统，提供各种视觉效果
- **Config**: 配置系统，通过ScriptableObject提供配置
- **Async**: 异步处理相关组件，基于UniTask
- **Utils**: 工具类和扩展方法
