# XR交互组件示例

本目录包含了使用XRIModule交互组件的实用示例，展示了如何在VR应用中创建丰富的交互体验。

## 示例概述

### 1. 控制面板示例 (ControlPanelExample.cs)

展示如何创建一个交互式VR控制面板，包含：
- 电源按钮（使用ButtonPreset）
- 音量旋钮（使用KnobPreset）
- 温度滑块（使用SliderValueExtractor和SingleAxisConstraint）

该示例演示了如何：
- 应用预设到交互物体
- 使用值提取器获取控件当前值
- 响应控件变化更新UI和功能

### 2. 工业机械模拟 (IndustrialMachineExample.cs)

展示如何创建一个工业设备的VR训练模拟，包含：
- 紧急停止按钮
- 三档位主控制拉杆
- 可旋转的压力阀门
- 只读温度计表盘

该示例演示了如何：
- 实现不同类型的交互约束
- 在交互中添加触觉和音频反馈
- 创建系统状态和警报响应

### 3. 自定义交互预设 (CustomPresetsExample.cs)

展示如何创建和应用自定义交互预设，包含：
- 自定义旋转拨盘预设
- 带有刻度和阻尼感的旋钮
- 交互事件系统

该示例演示了如何：
- 创建自定义交互预设类
- 使用辅助组件实现复杂交互效果
- 通过事件系统连接交互与游戏逻辑

## 使用方法

1. **添加到场景**：将示例脚本添加到带有XR交互组件的场景中
2. **配置引用**：在Inspector中设置相关引用（交互物体、UI元素等）
3. **确保依赖**：场景中需要有XRISceneInteractionLogic组件

## 核心组件参考

### 交互约束 (XRInteractionConstraints.cs)

限制交互物体的移动或旋转范围：
- `SingleAxisConstraint` - 限制在单一轴上移动
- `RotationAxisConstraint` - 限制围绕特定轴旋转
- `BoundsConstraint` - 限制在特定区域内移动
- `DistanceConstraint` - 限制与指定点的距离

### 值提取器 (XRInteractionValueExtractors.cs)

从交互物体中提取当前值：
- `LeverValueExtractor` - 从拉杆位置提取值
- `KnobValueExtractor` - 从旋钮旋转角度提取值
- `SliderValueExtractor` - 从滑块位置提取值
- `ButtonValueExtractor` - 从按钮按压深度提取值

### 交互预设 (XRInteractionPresets.cs)

预定义的交互行为配置：
- `ButtonPreset` - 标准按钮行为
- `LeverPreset` - 标准拉杆行为
- `KnobPreset` - 标准旋钮行为
- `RotaryDialPreset` - 自定义拨盘行为（见CustomPresetsExample）

## 提示与最佳实践

1. **设计阶段**：
   - 根据交互需求，为每个交互对象选择适当的预设
   - 为每个交互对象设置合理的约束参数
   - 考虑用户反馈方式（视觉、触觉、音频）

2. **实现阶段**：
   - 使用预设快速实现常见交互对象
   - 对于复杂交互，组合使用约束和值提取器
   - 使用事件系统连接交互与游戏逻辑

3. **优化阶段**：
   - 添加触觉反馈增强用户体验
   - 调整约束参数使交互感觉自然
   - 为高频使用的交互添加自定义预设
