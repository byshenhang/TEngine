# VR 3D UI框架设计文档

## 1. 概述

### 1.1 设计目标

在VR环境中，UI的展示和交互方式与传统2D UI有着本质区别。VR中的UI需要考虑空间位置、旋转信息以及更加直观的交互方式。本框架旨在解决以下问题：

- 提供独立于现有2D UI系统的3D UI管理框架
- 支持VR环境中的空间UI放置和交互
- 与Unity XR Interaction Toolkit无缝集成
- 提供灵活且易用的API和配置方式
- 兼顾开发者和设计师的使用体验

### 1.2 核心理念

- **职责分离**：将UI对象管理和交互逻辑分离，保持代码清晰和可维护
- **易于扩展**：框架各组件遵循单一职责原则，便于未来扩展
- **设计师友好**：提供基于场景的UI布局方式，降低技术门槛
- **开发者友好**：提供简洁的API和丰富的配置选项

## 2. 架构设计

### 2.1 总体架构

框架采用模块化设计，主要包含以下核心组件：

```
UI3DModule（核心模块）
    |- UI3DWindow（窗口基类）
    |- UI3DResourceLoader（资源加载器）
    |- SceneUIAttribute（场景UI特性标记）

XRUIController（交互控制器）
    |- UI3DAnchorPoint（场景UI锚点）
```

### 2.2 组件职责

#### UI3DModule

作为核心模块，UI3DModule主要负责3D UI的创建、资源加载和生命周期管理：

- 提供注册到GameModule的全局访问点
- 管理3D UI窗口的创建、显示和销毁
- 处理资源的加载和卸载
- 维护当前显示的UI窗口列表
- 提供统一的API接口给其他系统调用

#### XRUIController

作为场景级控制器，XRUIController专注于XR环境下的UI交互逻辑：

- 检测用户视线、距离和控制器指向
- 决定哪些UI应该显示或隐藏
- 处理XR特定的输入和交互
- 与Unity XR Interaction Toolkit集成
- 提供实时的UI可见性管理

#### UI3DWindow

所有3D UI窗口的基类，提供基础功能：

- 支持在3D空间中设置位置和旋转
- 处理资源加载和初始化
- 提供生命周期事件（创建、更新、销毁）
- 支持XR交互回调

#### UI3DAnchorPoint

场景中的UI锚点组件，用于标记UI在场景中的位置：

- 支持在Unity编辑器中直接放置和配置
- 提供丰富的交互配置选项
- 自动集成XR交互组件
- 支持基于距离、视线和控制器指向的多种交互模式

#### SceneUIAttribute

用于将UI类与场景锚点关联的特性标记：

- 简化UI类与场景锚点的映射
- 支持通过反射自动查找对应关系

#### UI3DResourceLoader

负责加载3D UI资源的组件：

- 封装资源加载细节
- 提供同步和异步加载接口
- 实现资源缓存提高性能

### 2.3 职责分离的优势

将UI3DModule和XRUIController分离带来以下优势：

1. **更好的集成性**：
   - XRUIController可以直接集成到场景中，作为GameObject存在
   - 可以通过Inspector配置交互参数，更易于设计师调整
   - 可以直接引用场景中的XR组件（如XRRayInteractor）

2. **与Unity XR Interaction Toolkit的自然集成**：
   - XR交互逻辑与Unity's XR Toolkit紧密相关
   - 将XR特定逻辑隔离在单独的控制器中更符合Unity的组件设计模式

3. **更灵活的架构**：
   - UI3DModule作为核心框架的一部分，相对稳定
   - XRUIController可以根据项目需求更灵活地调整或替换
   - 允许在不同场景中有不同的UI交互策略

4. **降低耦合度**：
   - UI3DModule不需要了解具体的XR交互细节
   - XRUIController不需要处理UI资源管理的复杂性

## 3. 工作流程

### 3.1 UI开发流程

1. **创建UI预制体**
   - 创建常规的Unity UI Canvas预制体
   - 设置Canvas为World Space渲染模式
   - 调整UI元素大小和布局适合VR环境

2. **创建UI窗口类**
   - 继承UI3DWindow基类
   - 实现必要的生命周期方法（OnCreate, OnUpdate等）
   - 添加特定交互逻辑

3. **关联UI与场景锚点**
   - 使用SceneUI特性标记UI类
   ```csharp
   [SceneUI("Inventory")]
   public class InventoryUI : UI3DWindow
   {
       // UI实现
   }
   ```

4. **在场景中放置UI锚点**
   - 创建空游戏对象
   - 添加UI3DAnchorPoint组件
   - 设置uiIdentifier匹配UI类标记
   - 配置交互参数（距离、视线检测等）

### 3.2 运行时流程

```
用户接近/看向/指向UI锚点 → XRUIController检测交互 → 调用UI3DModule显示UI → UI3DModule加载并实例化UI → 用户与UI交互
```

详细流程：

1. **初始化阶段**
   - UI3DModule随游戏启动初始化
   - XRUIController在场景加载时创建
   - XRUIController查找场景中的所有UI锚点

2. **交互检测阶段**
   - XRUIController持续检测用户位置、视线和控制器指向
   - 当满足显示条件时，触发显示相应UI

3. **UI显示阶段**
   - XRUIController调用UI3DModule.ShowSceneUI方法
   - UI3DModule查找对应的UI类型
   - UI3DModule创建UI实例并设置位置/旋转
   - UI完成加载并显示在锚点位置

4. **交互阶段**
   - 用户可以通过XR控制器与UI交互
   - UI3DWindow接收交互事件并响应

5. **隐藏阶段**
   - 当用户离开交互范围或触发隐藏条件时
   - XRUIController调用UI3DModule.CloseSceneUI方法
   - UI3DModule销毁UI实例并清理资源

## 4. 使用示例

### 4.1 基于场景的UI（设计师友好）

```csharp
// 1. 创建UI类并添加标记
[SceneUI("InfoPanel")]
public class InfoPanelUI : UI3DWindow
{
    protected override void OnCreate()
    {
        base.OnCreate();
        // 初始化UI
    }
    
    // 重写交互方法
    public override void OnXRSelect()
    {
        // 处理选择事件
    }
}

// 2. 在场景中设置UI锚点
// - 创建空对象并添加UI3DAnchorPoint组件
// - 设置uiIdentifier = "InfoPanel"
// - 配置交互参数

// 3. 游戏运行时，当玩家接近或看向锚点时，UI会自动显示
```

### 4.2 代码控制的UI（开发者友好）

```csharp
// 直接通过代码显示3D UI
GameModule.UI3D.ShowUI3D<InventoryUI>(
    position: new Vector3(0, 1.5f, 2),
    rotation: Quaternion.Euler(0, 180, 0),
    userDatas: new object[] { playerData }
);

// 显示场景UI
GameModule.UI3D.ShowSceneUI("WeaponInfo");

// 通过XRUIController控制UI显示
XRUIController.Instance.ShowUI(anchorPoint);
```

## 5. 扩展性与未来方向

### 5.1 可扩展点

- **自定义UI锚点行为**：继承UI3DAnchorPoint类实现特殊交互
- **自定义UI窗口类型**：继承UI3DWindow创建特定类型的UI
- **扩展XRUIController**：添加新的交互检测方法
- **自定义资源加载**：实现IUI3DResourceLoader接口提供不同的资源加载策略

### 5.2 未来改进方向

- **UI转换系统**：支持2D UI和3D UI之间的自动转换
- **布局优化**：自动调整UI位置避免遮挡
- **性能优化**：基于距离的LOD系统
- **手势交互**：集成手部追踪交互
- **空间音频反馈**：为UI交互添加空间音频

## 6. 总结

本框架通过将UI对象管理(UI3DModule)和交互控制(XRUIController)分离，结合场景锚点(UI3DAnchorPoint)的设计，为VR环境提供了一个灵活、易用且可扩展的3D UI解决方案。该框架既满足开发者通过代码控制UI的需求，又支持设计师直接在场景中放置和配置UI，适用于各种VR应用场景。
