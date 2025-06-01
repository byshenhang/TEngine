using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗模块 - 负责管理VR RPG战斗系统
    /// </summary>
    public class CombatModule : IModule
    {
        // 单例访问器
        public static CombatModule Instance { get; private set; }
        
        // 战斗状态管理器
        private CombatStateManager _stateManager;
        
        // 技能管理器
        private SkillManager _skillManager;
        
        // 行为树管理器
        private BehaviorTreeManager _behaviorTreeManager;
        
        // 战斗配置管理器
        private CombatConfigManager _configManager;
        
        // VR交互处理器
        private CombatInteractionHandler _interactionHandler;
        
        // 战斗实体管理器
        private CombatEntityManager _entityManager;
        
        // 是否已初始化
        private bool _initialized = false;
        
        /// <summary>
        /// 获取技能管理器实例
        /// </summary>
        public SkillManager SkillManager => _skillManager;
        
        /// <summary>
        /// 获取行为树管理器实例
        /// </summary>
        public BehaviorTreeManager BehaviorTreeManager => _behaviorTreeManager;
        
        /// <summary>
        /// 获取战斗状态管理器实例
        /// </summary>
        public CombatStateManager StateManager => _stateManager;
        
        /// <summary>
        /// 获取交互处理器实例
        /// </summary>
        public CombatInteractionHandler InteractionHandler => _interactionHandler;
        
        /// <summary>
        /// 获取战斗实体管理器实例
        /// </summary>
        public CombatEntityManager EntityManager => _entityManager;
        
        /// <summary>
        /// 初始化模块
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                Log.Warning("战斗模块已经初始化");
                return;
            }
            
            Instance = this;
            
            // 创建并初始化各个管理器
            _stateManager = new CombatStateManager();
            _skillManager = new SkillManager();
            _behaviorTreeManager = new BehaviorTreeManager();
            _configManager = new CombatConfigManager();
            _interactionHandler = new CombatInteractionHandler();
            _entityManager = new CombatEntityManager();
            
            // 按顺序初始化各个管理器
            _configManager.Initialize();
            _entityManager.Initialize();
            _skillManager.Initialize();
            _behaviorTreeManager.Initialize();
            _stateManager.Initialize();
            
            // 最后初始化交互处理器，因为它依赖于其他管理器
            string playerEntityId = CreatePlayerEntity();
            _interactionHandler.Initialize(playerEntityId);
            
            // 注册到更新循环
            GameEntry.Update.Subscribe(UpdateCombatModule);
            
            _initialized = true;
            Log.Info("战斗模块初始化完成");
        }
        
        /// <summary>
        /// 创建玩家实体并返回实体ID
        /// </summary>
        private string CreatePlayerEntity()
        {
            // 实际实现中，应根据玩家配置数据创建玩家实体
            // 这里简化实现，假设玩家ID为"player_001"
            return "player_001";
        }
        
        /// <summary>
        /// 更新战斗模块
        /// </summary>
        private void UpdateCombatModule(float deltaTime)
        {
            if (!_initialized) return;
            
            // 更新战斗状态
            _stateManager.Update(deltaTime);
            
            // 更新实体
            _entityManager.Update(deltaTime);
            
            // 更新交互处理
            _interactionHandler.Update();
            
            // 更新行为树（AI决策）
            UpdateBehaviorTrees(deltaTime);
        }
        
        /// <summary>
        /// 更新所有AI实体的行为树
        /// </summary>
        private void UpdateBehaviorTrees(float deltaTime)
        {
            // 获取所有AI控制的实体
            var aiEntities = _entityManager.GetAIEntities();
            
            foreach (var entity in aiEntities)
            {
                // 更新实体的行为树
                _behaviorTreeManager.UpdateEntityBehaviorTree(entity.EntityId, entity);
            }
        }
        
        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartCombat(string combatId, List<string> playerEntityIds, List<string> enemyEntityIds)
        {
            if (!_initialized)
            {
                Log.Error("战斗模块未初始化，无法开始战斗");
                return;
            }
            
            // 进入战斗准备状态
            _stateManager.EnterState(CombatStateType.Preparing);
            
            // 设置战斗参与者
            _entityManager.SetCombatParticipants(playerEntityIds, enemyEntityIds);
            
            // 为AI实体分配行为树
            AssignBehaviorTreesToEnemies(enemyEntityIds);
            
            // 进入战斗中状态
            _stateManager.EnterState(CombatStateType.InCombat);
            
            Log.Info($"开始战斗 {combatId}，玩家数量: {playerEntityIds.Count}，敌人数量: {enemyEntityIds.Count}");
        }
        
        /// <summary>
        /// 为敌人分配行为树
        /// </summary>
        private void AssignBehaviorTreesToEnemies(List<string> enemyEntityIds)
        {
            foreach (var entityId in enemyEntityIds)
            {
                // 获取实体
                var entity = _entityManager.GetEntity(entityId);
                if (entity == null) continue;
                
                // 根据实体类型分配适当的行为树
                string behaviorTreeTemplateId = GetBehaviorTreeTemplateForEntity(entity);
                _behaviorTreeManager.AssignBehaviorTree(entityId, behaviorTreeTemplateId);
                
                Log.Info($"为实体 {entityId} 分配行为树 {behaviorTreeTemplateId}");
            }
        }
        
        /// <summary>
        /// 根据实体类型获取合适的行为树模板ID
        /// </summary>
        private string GetBehaviorTreeTemplateForEntity(CombatEntityBase entity)
        {
            // 实际实现中，应根据实体类型、等级、角色等信息决定使用什么行为树
            // 这里简化实现
            if (entity.EntityType.Contains("boss"))
            {
                return "boss_basic";
            }
            else if (entity.EntityType.Contains("ranged"))
            {
                return "ranged_enemy_basic";
            }
            else
            {
                return "melee_enemy_basic";
            }
        }
        
        /// <summary>
        /// 结束战斗
        /// </summary>
        public void EndCombat(bool victory)
        {
            if (!_initialized)
            {
                Log.Error("战斗模块未初始化，无法结束战斗");
                return;
            }
            
            // 设置战斗结果
            _stateManager.SetCombatResult(victory);
            
            // 进入战斗结束状态
            _stateManager.EnterState(CombatStateType.Ending);
            
            // 清理战斗资源
            _entityManager.ClearCombatEntities();
            
            Log.Info($"结束战斗，胜利: {victory}");
        }
        
        /// <summary>
        /// 使用技能
        /// </summary>
        public async UniTask<bool> UseSkill(string casterEntityId, string skillId, string targetEntityId = null)
        {
            if (!_initialized)
            {
                Log.Error("战斗模块未初始化，无法使用技能");
                return false;
            }
            
            // 检查战斗状态是否允许使用技能
            if (_stateManager.CurrentStateType != CombatStateType.InCombat)
            {
                Log.Warning("当前战斗状态不允许使用技能");
                return false;
            }
            
            // 获取施法者实体
            var casterEntity = _entityManager.GetEntity(casterEntityId);
            if (casterEntity == null)
            {
                Log.Error($"施法者实体 {casterEntityId} 不存在");
                return false;
            }
            
            // 获取目标实体（如果有）
            CombatEntityBase targetEntity = null;
            if (!string.IsNullOrEmpty(targetEntityId))
            {
                targetEntity = _entityManager.GetEntity(targetEntityId);
                if (targetEntity == null)
                {
                    Log.Warning($"目标实体 {targetEntityId} 不存在");
                    // 某些技能可能不需要目标，所以这里不直接返回false
                }
            }
            
            // 使用技能
            bool success = await _skillManager.UseSkill(casterEntityId, skillId, targetEntityId);
            
            if (success)
            {
                Log.Info($"实体 {casterEntityId} 成功使用技能 {skillId} 对目标 {targetEntityId}");
            }
            else
            {
                Log.Warning($"实体 {casterEntityId} 使用技能 {skillId} 失败");
            }
            
            return success;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Shutdown()
        {
            if (!_initialized) return;
            
            // 取消注册更新循环
            GameEntry.Update.Unsubscribe(UpdateCombatModule);
            
            // 按顺序释放各个管理器资源
            _interactionHandler?.Dispose();
            _stateManager?.Dispose();
            _behaviorTreeManager?.Dispose();
            _skillManager?.Dispose();
            _entityManager?.Dispose();
            _configManager?.Dispose();
            
            _initialized = false;
            Instance = null;
            
            Log.Info("战斗模块已释放资源");
        }
    }
}
