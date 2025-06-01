# VR RPG 战斗系统框架文档

## 1. 系统概述

战斗系统是VR RPG游戏的核心交互模块，负责管理战斗实体、技能释放、伤害计算、状态效果和投射物等战斗要素。该框架采用高度模块化设计，通过CombatModule作为中央门面，整合各个子系统并提供统一接口。

## 2. 系统架构

### 核心组件

```
CombatModule (中央门面)
├── EntityManager - 实体管理
├── SkillManager - 技能管理
├── CombatStateManager - 战斗状态管理
├── StatusEffectManager - 状态效果管理
├── ProjectileManager - 投射物管理
└── MockDataProvider - 模拟数据提供
```

### 设计原则

- **高内聚低耦合**：每个管理器负责独立的功能域
- **易扩展性**：支持添加新的实体类型、技能、状态效果和投射物
- **统一接口**：所有外部调用通过CombatModule进行
- **生命周期管理**：所有组件遵循统一的初始化-更新-清理流程

## 3. 主要子系统

### 3.1 实体系统

- **Entity**: 所有战斗单位的基类，包含属性、状态和位置信息
- **EntityManager**: 管理所有实体的创建、更新和销毁

### 3.2 技能系统

- **Skill**: 定义技能行为和效果
- **SkillManager**: 管理技能的创建、执行和冷却

### 3.3 状态效果系统

- **StatusEffect**: 状态效果基类，定义效果类型、持续时间和堆叠策略
- **AttributeModifierEffect**: 属性修改类效果（增益/减益）
- **DamageOverTimeEffect**: 持续伤害类效果（DOT）
- **StatusEffectManager**: 管理实体上的所有状态效果

### 3.4 投射物系统

- **Projectile**: 投射物基类，定义位置、方向、速度和碰撞检测
- **HomingProjectile**: 追踪型投射物，可跟踪目标
- **BeamProjectile**: 光束类投射物，即时生效
- **ProjectileManager**: 创建和管理不同类型的投射物

### 3.5 战斗状态管理

- **CombatStateManager**: 管理战斗的不同阶段（准备、激活、暂停、结束）

## 4. 主要接口

### 4.1 实体管理接口

```csharp
CreateEntity(EntityType type, Vector3 position)
GetEntity(int entityId)
RemoveEntity(int entityId)
GetEntitiesInRange(Vector3 position, float range)
```

### 4.2 技能系统接口

```csharp
AddSkillToEntity(Entity entity, int skillId)
GetEntitySkills(Entity entity)
GetEntitySkillsByType(Entity entity, SkillType type)
GetSkillConfig(int skillId)
```

### 4.3 状态效果接口

```csharp
AddAttributeModifier(Entity source, Entity target, AttributeType attributeType, float value, float duration)
AddDamageOverTime(Entity source, Entity target, float damagePerTick, float duration, float tickInterval, DamageType damageType)
GetEntityStatusEffects(Entity entity)
GetEntityStatusEffectsByType(Entity entity, StatusEffectType type)
RemoveStatusEffect(Entity entity, int effectId)
RemoveAllStatusEffects(Entity entity)
```

### 4.4 投射物接口

```csharp
CreateBullet(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType, float speed, float lifetime)
CreateArrow(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType, float speed, float lifetime)
CreateMagicProjectile(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType, float speed, float lifetime, float effectRadius)
CreateHomingProjectile(Entity source, Entity target, Vector3 position, float damage, DamageType damageType, float speed, float lifetime, float turnRate)
CreateBeam(Entity source, Vector3 position, Vector3 direction, float damage, DamageType damageType, float lifetime, float maxDistance)
GetActiveProjectiles()
ClearAllProjectiles()
```

## 5. 数据流

1. 战斗初始化：`CombatModule.Initialize()` 调用各管理器初始化
2. 战斗更新：`CombatModule.OnUpdate()` 更新各子系统
3. 实体动作：玩家输入或AI决策 → 技能执行 → 创建投射物/应用状态效果 → 伤害计算
4. 战斗结束：`CombatModule.Shutdown()` 清理所有资源

## 6. 后续扩展方向

### 6.1 战斗反馈系统
- 浮动伤害数字和视觉反馈
- VR控制器震动反馈
- 音效和视觉特效系统
- 命中和受击动画整合

### 6.2 AI系统增强
- 复杂敌人决策树
- 群体AI协同行为
- 难度自适应系统
- 基于玩家行为的学习系统

### 6.3 技能系统扩展
- 技能组合与连招系统
- 技能进化和升级路线
- 技能定制和点数分配
- 技能触发事件系统

### 6.4 VR交互增强
- 环境物体战斗交互
- 双手协同操作机制
- 高级手势识别
- 语音指令系统

### 6.5 战斗经济系统
- 能量/魔法值资源管理
- 冷却缩减机制
- 连击奖励系统
- 战斗资源获取机制

### 6.6 数据分析与记录
- 战斗数据收集和分析
- 玩家表现评分系统
- 战斗成就系统
- 战斗回放功能

### 6.7 配置数据优化
- 从外部文件加载配置
- 实时热更新支持
- 战斗参数调试工具
- 数据验证与错误处理

### 6.8 网络功能
- 多人战斗同步
- 网络延迟补偿
- 权威服务器架构
- 反作弊系统

## 7. 使用示例

```csharp
// 初始化战斗模块
CombatModule combatModule = new CombatModule();
combatModule.Initialize();

// 创建玩家和敌人
Entity player = combatModule.CreateEntity(EntityType.Player, new Vector3(0, 0, 0));
Entity enemy = combatModule.CreateEntity(EntityType.Enemy, new Vector3(5, 0, 0));

// 为玩家添加技能
Skill fireball = combatModule.AddSkillToEntity(player, 101); // 火球术ID

// 玩家使用技能创建投射物
Projectile fireballProj = combatModule.CreateMagicProjectile(
    player,                 // 源实体
    player.Transform.position,  // 起始位置
    (enemy.Transform.position - player.Transform.position).normalized, // 方向
    25f,                    // 伤害值
    DamageType.Fire,        // 伤害类型
    12f,                    // 速度
    5f,                     // 生命周期
    2f                      // 爆炸半径
);

// 添加灼烧状态效果
StatusEffect burnEffect = combatModule.AddDamageOverTime(
    player,                 // 源实体
    enemy,                  // 目标实体
    5f,                     // 每跳伤害
    8f,                     // 持续时间
    1f,                     // 伤害间隔
    DamageType.Fire         // 伤害类型
);

// 更新战斗（每帧调用）
combatModule.OnUpdate();

// 战斗结束后清理
combatModule.Shutdown();
```

## 8. 技能类型总结

战斗系统支持多种技能类型，以下是完整的技能类型分类：

### 8.1 基础伤害类型

- **直接伤害技能**：立即对目标造成伤害，无持续效果
- **持续伤害技能(DoT)**：在一段时间内周期性造成伤害（如中毒、燃烧）

### 8.2 投射物类型

- **子弹型**：高速直线移动的投射物，通常有物理伤害
- **箭矢型**：具有轻微抛物线，射程较远的物理投射物
- **魔法投射物**：可能具有特殊视觉效果和爆炸半径的元素类投射物
- **追踪型**：能自动追踪目标的智能投射物
- **光束型**：即时生效的线性攻击，如激光、闪电等
- **爆炸型**：到达目标位置或碰撞后产生范围伤害

### 8.3 状态效果类型

- **属性增益(Buff)**：提升目标属性或能力的正面效果
- **属性减益(Debuff)**：降低目标属性或能力的负面效果
- **持续伤害效果**：随时间造成伤害的效果（如灼烧、毒素）
- **控制效果**：限制目标行动能力的效果（如眩晕、减速、禁锢）

### 8.4 区域效果技能

- **范围伤害**：对区域内所有实体造成伤害
- **持续区域**：创建持续存在的效果区域（如火场、治疗光环）
- **动态区域**：可移动或扩展的区域效果（如旋转的风暴）

### 8.5 位移类技能

- **闪现/瞬移**：瞬间移动到指定位置
- **冲锋/突进**：快速移动到目标位置并可能造成效果
- **击退/拉近**：改变目标位置的技能

### 8.6 变形/形态转换技能

- **临时变形**：改变角色形态获得新能力
- **武器强化**：临时增强武器属性或外观
- **元素注入**：改变攻击的元素类型

### 8.7 召唤类技能

- **临时盟友**：召唤NPC战斗单位协助战斗
- **幻象/假身**：创建分散敌人注意力的虚假目标
- **陷阱/守卫**：放置静态的攻击性或防御性实体
- **元素召唤物**：召唤元素生物（如火元素、冰元素）

### 8.8 控制类技能

- **眩晕/定身**：临时禁用目标行动能力
- **减速/冰冻**：降低目标移动或攻击速度
- **恐惧/混乱**：让敌人失去控制或逃跑
- **吸引/嘲讽**：强制敌人将注意力集中在施法者身上

### 8.9 反射/反制技能

- **伤害反射**：将受到的伤害反弹给攻击者
- **技能反制**：打断或阻止敌人技能释放
- **法术吸收**：将敌人的魔法攻击转化为自身资源

### 8.10 连锁/触发类技能

- **连锁闪电**：从一个目标跳跃到另一个目标
- **条件触发**：满足特定条件时自动释放的技能
- **组合技**：与其他技能组合使用产生特殊效果

### 8.11 环境交互技能

- **元素反应**：利用环境中的元素产生反应
- **地形改变**：创造或改变地形（如障碍物、冰桥）
- **引力操控**：改变区域引力效果（如浮空、重力井）

### 8.12 护盾/防御类技能

- **吸收护盾**：吸收一定量伤害的临时护盾
- **反伤护盾**：受到攻击时对攻击者造成伤害
- **选择性护盾**：只对特定类型伤害有效的护盾

### 8.13 增强/武器类技能

- **武器注魔**：临时给武器添加特殊效果
- **双持/多武器**：临时使用额外武器或改变武器形态
- **特殊攻击模式**：改变基础攻击方式（如散射、穿透）

## 9. 注意事项

1. 所有外部系统应只与CombatModule交互，不直接访问子系统
2. 添加新功能时应保持模块化设计，确保可测试性
3. 实体和效果的性能优化对VR体验至关重要
4. 战斗参数应通过配置系统管理，便于平衡性调整

## 9. 实施建议

### 9.1 近期优先级（1-3个月）

1. **完善视觉反馈系统**
   - 实现伤害数字显示系统（浮动文本）
   - 添加基础命中特效和受击特效
   - 集成VR控制器震动反馈

2. **增强状态效果系统**
   - 实现更多类型的状态效果（眩晕、减速、增强等）
   - 添加状态效果的视觉指示器
   - 实现状态效果之间的交互逻辑（如湿润+电击=加成伤害）

3. **优化配置数据系统**
   - 将MockDataProvider替换为基于JSON的配置系统
   - 设计技能和效果的数据表格式
   - 实现配置数据的校验和错误处理

### 9.2 中期目标（3-6个月）

1. **战斗AI增强**
   - 改进敌人的战术决策系统
   - 实现基于玩家行为的适应性AI
   - 添加协同攻击的敌群行为

2. **技能系统扩展**
   - 实现技能组合和连招系统
   - 设计技能升级和进化机制
   - 添加环境互动型技能

3. **资源管理系统**
   - 实现能量/魔法值系统
   - 设计资源消耗和恢复机制
   - 添加战斗中资源获取的游戏玩法

### 9.3 长期规划（6个月以上）

1. **网络同步框架**
   - 设计多人战斗的同步架构
   - 实现网络状态预测和回滚
   - 添加反作弊机制

2. **数据分析与平衡**
   - 建立战斗数据收集和分析系统
   - 设计自动平衡测试工具
   - 实现战斗回放功能

3. **高级VR交互**
   - 实现复杂手势识别系统
   - 设计双手协同技能机制
   - 添加语音控制系统

### 9.4 技术实施注意事项

1. **性能优化**
   - 投射物和状态效果应使用对象池管理
   - 视觉效果应考虑VR性能开销
   - 大量实体时使用空间分区优化碰撞检测

2. **可测试性**
   - 为关键战斗逻辑编写单元测试
   - 设计战斗场景的自动化测试
   - 创建战斗平衡性测试工具

3. **架构整合**
   - 确保战斗系统与其他游戏系统（如物品、进度、任务）的无缝整合
   - 设计清晰的事件系统用于跨系统通信
   - 保持代码的可维护性和可读性

---

通过以上架构和实施建议，战斗系统可持续扩展以满足游戏设计需求，同时保持代码质量和性能表现。后续扩展应当遵循现有的模块化设计原则，通过扩展而非修改现有代码来添加新功能。
