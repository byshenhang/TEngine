using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗开始状态 - 处理战斗初始化逻辑
    /// </summary>
    public class BattleStartState : CombatStateBase
    {
        // 战斗初始化是否完成
        private bool _initializationComplete = false;
        
        public override void OnEnter(CombatStateContext context)
        {
            base.OnEnter(context);
            
            // 重置状态
            _initializationComplete = false;
            
            // 初始化战斗
            InitializeBattle();
        }
        
        public override void OnUpdate()
        {
            // 如果初始化完成，进入活动战斗状态
            if (_initializationComplete)
            {
                TransitionTo(CombatStateType.BattleActive);
            }
        }
        
        /// <summary>
        /// 初始化战斗
        /// </summary>
        private void InitializeBattle()
        {
            Log.Info($"正在初始化战斗: {_context.CombatId}");
            
            // 获取战斗实体
            List<CombatEntityBase> players = new List<CombatEntityBase>();
            List<CombatEntityBase> enemies = new List<CombatEntityBase>();
            
            // 处理玩家实体
            foreach (var playerId in _context.PlayerIds)
            {
                var player = GameModule.Combat.GetCombatEntity(playerId);
                if (player != null)
                {                    
                    player.InitForCombat(_context.CombatId);
                    players.Add(player);
                    Log.Info($"玩家实体已准备战斗: {playerId}");
                }
                else
                {
                    Log.Warning($"无法找到玩家实体: {playerId}");
                }
            }
            
            // 处理敌人实体
            foreach (var enemyId in _context.EnemyIds)
            {
                var enemy = GameModule.Combat.GetCombatEntity(enemyId);
                if (enemy != null)
                {
                    enemy.InitForCombat(_context.CombatId);
                    enemies.Add(enemy);
                    Log.Info($"敌人实体已准备战斗: {enemyId}");
                }
                else
                {
                    Log.Warning($"无法找到敌人实体: {enemyId}");
                }
            }
            
            // 初始化回合计数
            _context.CurrentTurn = 1;
            
            // 设置战斗参与者列表到上下文
            _context.SetExtraData("Players", players);
            _context.SetExtraData("Enemies", enemies);
            
            // 播放战斗开始效果
            PlayBattleStartEffects();
            
            // 标记初始化完成
            _initializationComplete = true;
            Log.Info($"战斗初始化完成: {_context.CombatId}");
        }
        
        /// <summary>
        /// 播放战斗开始特效
        /// </summary>
        private void PlayBattleStartEffects()
        {
            // 在VR环境中播放战斗开始特效
            Log.Info("播放战斗开始特效");
            
            // 音效
            // AudioModule.PlaySound("battle_start");
            
            // 视觉效果
            // VFXModule.PlayEffect("battle_start_vfx");
            
            // 可以调用XR相关模块实现特效
            // GameModule.XRI.TriggerEffect("battle_start");
        }
    }
}
