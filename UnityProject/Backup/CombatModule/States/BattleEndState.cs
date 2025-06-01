using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6218u6597u7ed3u675fu72b6u6001 - u5904u7406u6218u6597u7ed3u675fu903bu8f91
    /// </summary>
    public class BattleEndState : CombatStateBase
    {
        // u6218u6597u7ed3u7b97u662fu5426u5b8cu6210
        private bool _cleanupComplete = false;
        
        public override void OnEnter(CombatStateContext context)
        {
            base.OnEnter(context);
            
            // u91cdu7f6eu72b6u6001
            _cleanupComplete = false;
            
            // u7ed3u7b97u6218u6597
            FinalizeAndCleanupBattle();
        }
        
        public override void OnUpdate()
        {
            // u5982u679cu6e05u7406u5b8cu6210u5219u8fdbu5165u76f8u5e94u7ed3u679cu72b6u6001
            if (_cleanupComplete)
            {
                switch (_context.Result)
                {
                    case CombatResult.Victory:
                        TransitionTo(CombatStateType.Victory);
                        break;
                    case CombatResult.Defeat:
                        TransitionTo(CombatStateType.Defeat);
                        break;
                    default:
                        // u65e0u9700u8fdbu5165u5176u4ed6u72b6u6001
                        break;
                }
            }
        }
        
        /// <summary>
        /// u6700u7ec8u5904u7406u5e76u6e05u7406u6218u6597
        /// </summary>
        private void FinalizeAndCleanupBattle()
        {
            Log.Info($"u6b63u5728u7ed3u675fu6218u6597: {_context.CombatId}, u7ed3u679c: {_context.Result}");
            
            // u83b7u53d6u6218u6597u5b9eu4f53
            var players = _context.GetExtraData<List<CombatEntityBase>>("Players", new List<CombatEntityBase>());
            var enemies = _context.GetExtraData<List<CombatEntityBase>>("Enemies", new List<CombatEntityBase>());
            
            // u5904u7406u5956u52b1u548cu7ecfu9a8c
            if (_context.Result == CombatResult.Victory)
            {
                ProcessRewardsAndExperience(players, enemies);
            }
            
            // u6e05u7406u6240u6709u5b9eu4f53u72b6u6001
            foreach (var player in players)
            {
                player.CleanupAfterCombat(_context.CombatId);
                Log.Info($"u73a9u5bb6u5b9eu4f53u5df2u6e05u7406: {player.EntityId}");
            }
            
            foreach (var enemy in enemies)
            {
                enemy.CleanupAfterCombat(_context.CombatId);
                Log.Info($"u654cu4ebau5b9eu4f53u5df2u6e05u7406: {enemy.EntityId}");
            }
            
            // u64adu653eu6218u6597u7ed3u675fu6548u679c
            PlayBattleEndEffects();
            
            // u6807u8bbcu6e05u7406u5b8cu6210
            _cleanupComplete = true;
            Log.Info($"u6218u6597u7ed3u675fu5904u7406u5b8cu6210: {_context.CombatId}");
        }
        
        /// <summary>
        /// u5904u7406u5956u52b1u548cu7ecfu9a8c
        /// </summary>
        private void ProcessRewardsAndExperience(List<CombatEntityBase> players, List<CombatEntityBase> enemies)
        {
            Log.Info("u5904u7406u6218u6597u5956u52b1u548cu7ecfu9a8c");
            
            int totalExperience = 0;
            List<ItemDrop> rewards = new List<ItemDrop>();
            
            // u8ba1u7b97u7ecfu9a8cu548cu6536u96c6u5956u52b1
            foreach (var enemy in enemies)
            {
                if (enemy is EnemyCombatEntity enemyEntity)
                {
                    totalExperience += enemyEntity.ExperienceValue;
                    
                    // u8ba1u7b97u6389u843d
                    var drops = enemyEntity.CalculateDrops();
                    if (drops != null && drops.Count > 0)
                    {
                        rewards.AddRange(drops);
                    }
                }
            }
            
            // u5206u914du7ecfu9a8c
            int expPerPlayer = players.Count > 0 ? totalExperience / players.Count : 0;
            foreach (var player in players)
            {
                if (player is PlayerCombatEntity playerEntity)
                {
                    playerEntity.AddExperience(expPerPlayer);
                    Log.Info($"u73a9u5bb6 {player.EntityId} u83b7u5f97u7ecfu9a8c: {expPerPlayer}");
                }
            }
            
            // u6536u96c6u5956u52b1
            if (rewards.Count > 0)
            {
                // u5c06u5956u52b1u5b58u50a8u5230u4e0au4e0bu6587u4e2du4fbbu547du5177u6709
                _context.SetExtraData("Rewards", rewards);
                
                // u5206u53d1u5956u52b1u7ed9u73a9u5bb6 (u8fd9u91ccu53ebu8bb0u5f55u65e5u5fd7u4fe1u606f)
                foreach (var item in rewards)
                {
                    Log.Info($"u6218u5229u5956u52b1: {item.ItemId} x{item.Count}");
                }
            }
        }
        
        /// <summary>
        /// u64adu653eu6218u6597u7ed3u675fu7279u6548
        /// </summary>
        private void PlayBattleEndEffects()
        {
            // u6839u636eu6218u6597u7ed3u679cu64adu653eu4e0du540cu6548u679c
            string effectName = _context.Result == CombatResult.Victory ? "battle_victory" : "battle_defeat";
            Log.Info($"u64adu653eu6218u6597u7ed3u675fu7279u6548: {effectName}");
            
            // u97f3u6548
            // AudioModule.PlaySound(effectName);
            
            // u89c6u89c9u6548u679c
            // VFXModule.PlayEffect(effectName);
            
            // u53efu4ee5u8c03u7528XRu76f8u5173u6a21u5757u5b9eu73b0u7279u6548
            // GameModule.XRI.TriggerEffect(effectName);
        }
    }
    
    /// <summary>
    /// u7269u54c1u6389u843du7c7bu
    /// </summary>
    public class ItemDrop
    {
        /// <summary>
        /// u7269u54c1ID
        /// </summary>
        public string ItemId { get; set; }
        
        /// <summary>
        /// u6570u91cf
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// u7269u54c1u54c1u8d28
        /// </summary>
        public int Quality { get; set; }
        
        public ItemDrop(string itemId, int count = 1, int quality = 0)
        {
            ItemId = itemId;
            Count = count;
            Quality = quality;
        }
    }
}
