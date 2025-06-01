using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗实体管理器 - 负责管理战斗实体的创建、查询和销毁
    /// </summary>
    public class CombatEntityManager : IDisposable
    {
        // 实体字典 <实体ID, 实体>
        private Dictionary<string, CombatEntityBase> _entities = new Dictionary<string, CombatEntityBase>();
        
        // 玩家实体ID列表
        private List<string> _playerEntityIds = new List<string>();
        
        // 敌人实体ID列表
        private List<string> _enemyEntityIds = new List<string>();
        
        // 是否已初始化
        private bool _initialized = false;
        
        /// <summary>
        /// 初始化实体管理器
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                Log.Warning("战斗实体管理器已经初始化");
                return;
            }
            
            _entities.Clear();
            _playerEntityIds.Clear();
            _enemyEntityIds.Clear();
            
            _initialized = true;
            Log.Info("战斗实体管理器初始化完成");
        }
        
        /// <summary>
        /// 更新所有实体
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_initialized) return;
            
            // 更新所有实体
            foreach (var entity in _entities.Values)
            {
                if (entity.IsActive)
                {
                    entity.Update(deltaTime);
                }
            }
            
            // 检查战斗结束条件
            CheckCombatEndCondition();
        }
        
        /// <summary>
        /// 检查战斗结束条件
        /// </summary>
        private void CheckCombatEndCondition()
        {
            // 检查是否处于战斗中状态
            if (GameModule.Combat.StateManager.CurrentStateType != CombatStateType.InCombat)
            {
                return;
            }
            
            // 检查玩家方是否全部死亡
            bool allPlayersDead = true;
            foreach (var entityId in _playerEntityIds)
            {
                var entity = GetEntity(entityId);
                if (entity != null && entity.IsAlive)
                {
                    allPlayersDead = false;
                    break;
                }
            }
            
            if (allPlayersDead)
            {
                // 玩家失败
                GameModule.Combat.EndCombat(false);
                return;
            }
            
            // 检查敌人方是否全部死亡
            bool allEnemiesDead = true;
            foreach (var entityId in _enemyEntityIds)
            {
                var entity = GetEntity(entityId);
                if (entity != null && entity.IsAlive)
                {
                    allEnemiesDead = false;
                    break;
                }
            }
            
            if (allEnemiesDead)
            {
                // 玩家胜利
                GameModule.Combat.EndCombat(true);
                return;
            }
        }
        
        /// <summary>
        /// 添加实体
        /// </summary>
        public bool AddEntity(CombatEntityBase entity)
        {
            if (!_initialized || entity == null)
            {
                Log.Error("添加实体失败：管理器未初始化或实体为空");
                return false;
            }
            
            if (string.IsNullOrEmpty(entity.EntityId))
            {
                Log.Error("添加实体失败：实体ID为空");
                return false;
            }
            
            if (_entities.ContainsKey(entity.EntityId))
            {
                Log.Warning($"实体 {entity.EntityId} 已存在，将被覆盖");
            }
            
            _entities[entity.EntityId] = entity;
            
            // 根据实体阵营分类
            if (entity.Faction == CombatFaction.Player)
            {
                if (!_playerEntityIds.Contains(entity.EntityId))
                {
                    _playerEntityIds.Add(entity.EntityId);
                }
            }
            else if (entity.Faction == CombatFaction.Enemy)
            {
                if (!_enemyEntityIds.Contains(entity.EntityId))
                {
                    _enemyEntityIds.Add(entity.EntityId);
                }
            }
            
            Log.Info($"添加实体 {entity.EntityId} 成功，阵营: {entity.Faction}");
            return true;
        }
        
        /// <summary>
        /// 移除实体
        /// </summary>
        public bool RemoveEntity(string entityId)
        {
            if (!_initialized || string.IsNullOrEmpty(entityId))
            {
                Log.Error("移除实体失败：管理器未初始化或实体ID为空");
                return false;
            }
            
            if (!_entities.TryGetValue(entityId, out var entity))
            {
                Log.Warning($"实体 {entityId} 不存在，无法移除");
                return false;
            }
            
            // 从阵营列表中移除
            if (entity.Faction == CombatFaction.Player)
            {
                _playerEntityIds.Remove(entityId);
            }
            else if (entity.Faction == CombatFaction.Enemy)
            {
                _enemyEntityIds.Remove(entityId);
            }
            
            // 从实体字典中移除
            _entities.Remove(entityId);
            
            Log.Info($"移除实体 {entityId} 成功");
            return true;
        }
        
        /// <summary>
        /// 获取实体
        /// </summary>
        public CombatEntityBase GetEntity(string entityId)
        {
            if (!_initialized || string.IsNullOrEmpty(entityId))
            {
                return null;
            }
            
            if (_entities.TryGetValue(entityId, out var entity))
            {
                return entity;
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取特定阵营的所有实体
        /// </summary>
        public List<CombatEntityBase> GetEntitiesByFaction(CombatFaction faction)
        {
            List<CombatEntityBase> result = new List<CombatEntityBase>();
            
            if (!_initialized) return result;
            
            // 根据阵营获取实体ID列表
            List<string> entityIds = faction == CombatFaction.Player ? _playerEntityIds : _enemyEntityIds;
            
            foreach (var entityId in entityIds)
            {
                var entity = GetEntity(entityId);
                if (entity != null && entity.IsActive)
                {
                    result.Add(entity);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取所有AI控制的实体
        /// </summary>
        public List<CombatEntityBase> GetAIEntities()
        {
            List<CombatEntityBase> result = new List<CombatEntityBase>();
            
            if (!_initialized) return result;
            
            // 遍历敌人实体（默认所有敌人都是AI控制）
            foreach (var entityId in _enemyEntityIds)
            {
                var entity = GetEntity(entityId);
                if (entity != null && entity.IsActive && entity.IsAlive)
                {
                    result.Add(entity);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 设置战斗参与者
        /// </summary>
        public void SetCombatParticipants(List<string> playerEntityIds, List<string> enemyEntityIds)
        {
            if (!_initialized)
            {
                Log.Error("设置战斗参与者失败：管理器未初始化");
                return;
            }
            
            // 将所有实体设置为非战斗状态
            foreach (var entity in _entities.Values)
            {
                entity.SetInCombat(false);
            }
            
            // 清空当前战斗参与者列表
            _playerEntityIds.Clear();
            _enemyEntityIds.Clear();
            
            // 设置新的战斗参与者
            if (playerEntityIds != null)
            {
                foreach (var entityId in playerEntityIds)
                {
                    var entity = GetEntity(entityId);
                    if (entity != null)
                    {
                        _playerEntityIds.Add(entityId);
                        entity.SetInCombat(true);
                    }
                }
            }
            
            if (enemyEntityIds != null)
            {
                foreach (var entityId in enemyEntityIds)
                {
                    var entity = GetEntity(entityId);
                    if (entity != null)
                    {
                        _enemyEntityIds.Add(entityId);
                        entity.SetInCombat(true);
                    }
                }
            }
            
            Log.Info($"设置战斗参与者，玩家数量: {_playerEntityIds.Count}，敌人数量: {_enemyEntityIds.Count}");
        }
        
        /// <summary>
        /// 创建玩家实体
        /// </summary>
        public CombatEntityBase CreatePlayerEntity(string entityId, string name, Dictionary<AttributeType, float> attributes)
        {
            if (!_initialized || string.IsNullOrEmpty(entityId))
            {
                Log.Error("创建玩家实体失败：管理器未初始化或ID为空");
                return null;
            }
            
            // 创建玩家实体
            var playerEntity = new CombatEntityPlayer(entityId, name, attributes);
            
            // 添加到管理器
            AddEntity(playerEntity);
            
            Log.Info($"创建玩家实体 {entityId} 成功");
            return playerEntity;
        }
        
        /// <summary>
        /// 创建敌人实体
        /// </summary>
        public CombatEntityBase CreateEnemyEntity(string entityId, string name, string entityType, Dictionary<AttributeType, float> attributes)
        {
            if (!_initialized || string.IsNullOrEmpty(entityId))
            {
                Log.Error("创建敌人实体失败：管理器未初始化或ID为空");
                return null;
            }
            
            // 创建敌人实体
            var enemyEntity = new CombatEntityEnemy(entityId, name, entityType, attributes);
            
            // 添加到管理器
            AddEntity(enemyEntity);
            
            Log.Info($"创建敌人实体 {entityId} 成功，类型: {entityType}");
            return enemyEntity;
        }
        
        /// <summary>
        /// 找到最近的敌人实体
        /// </summary>
        public CombatEntityBase FindNearestEnemy(CombatEntityBase sourceEntity, float maxDistance = float.MaxValue)
        {
            if (!_initialized || sourceEntity == null)
            {
                return null;
            }
            
            CombatEntityBase nearestEntity = null;
            float nearestDistance = maxDistance;
            
            // 确定目标阵营
            List<string> targetEntityIds = sourceEntity.Faction == CombatFaction.Player ? _enemyEntityIds : _playerEntityIds;
            
            foreach (var entityId in targetEntityIds)
            {
                var entity = GetEntity(entityId);
                if (entity != null && entity.IsActive && entity.IsAlive)
                {
                    float distance = Vector3.Distance(sourceEntity.Position, entity.Position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEntity = entity;
                    }
                }
            }
            
            return nearestEntity;
        }
        
        /// <summary>
        /// 清理所有战斗实体
        /// </summary>
        public void ClearCombatEntities()
        {
            if (!_initialized) return;
            
            // 将所有实体设置为非战斗状态
            foreach (var entity in _entities.Values)
            {
                entity.SetInCombat(false);
            }
            
            // 清空战斗参与者列表
            _playerEntityIds.Clear();
            _enemyEntityIds.Clear();
            
            Log.Info("清理所有战斗实体");
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_initialized) return;
            
            // 清空实体数据
            _entities.Clear();
            _playerEntityIds.Clear();
            _enemyEntityIds.Clear();
            
            _initialized = false;
            
            Log.Info("战斗实体管理器已释放资源");
        }
    }
    
    /// <summary>
    /// 战斗阵营类型
    /// </summary>
    public enum CombatFaction
    {
        Neutral = 0,  // 中立
        Player = 1,   // 玩家
        Enemy = 2,    // 敌人
        Ally = 3      // 盟友
    }
    
    /// <summary>
    /// 属性类型
    /// </summary>
    public enum AttributeType
    {
        MaxHealth,      // 最大生命值
        CurrentHealth,  // 当前生命值
        MaxMana,        // 最大法力值
        CurrentMana,    // 当前法力值
        Attack,         // 攻击力
        Defense,        // 防御力
        MagicAttack,    // 魔法攻击
        MagicDefense,   // 魔法防御
        Speed,          // 速度
        CritRate,       // 暴击率
        CritDamage,     // 暴击伤害
        HitRate,        // 命中率
        DodgeRate       // 闪避率
    }
}
