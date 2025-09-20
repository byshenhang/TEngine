using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LyricFX.Factory
{
    /// <summary>
    /// 字符对象状态枚举
    /// </summary>
    public enum CharacterState
    {
        Available,      // 可用状态
        InUse,         // 使用中
        PendingRecycle, // 待回收
        Recycling      // 回收中
    }

    /// <summary>
    /// 字符工厂 - 负责创建和回收字符游戏对象
    /// </summary>
    public class CharacterFactory 
    {
        private GameObject characterPrefab;
        private Transform poolContainer;
        private int initialPoolSize = 20;
        private int maxPoolSize = 100;
        
        // 对象池
        private Stack<GameObject> characterPool = new Stack<GameObject>();
        
        // 已使用的字符对象
        private HashSet<GameObject> activeCharacters = new HashSet<GameObject>();
        
        // 字符对象状态管理
        private Dictionary<GameObject, CharacterState> characterStates = new Dictionary<GameObject, CharacterState>();
        private readonly object stateLock = new object();
        
        // 性能优化配置
        private bool enableAsyncRecycling = true;
        private Queue<GameObject> pendingRecycleQueue = new Queue<GameObject>();

        /// <summary>
        /// 初始化字符工厂
        /// </summary>
        public async UniTask Initialize(int poolSize = 50, int maxSize = 100)
        {
            initialPoolSize = poolSize;
            maxPoolSize = maxSize;

            Debug.Log($"[字符工厂] 初始化完成，池容量：{characterPool.Count}");
        }
        
        public async UniTask UpdateCharacter(GameObject characterObj, Transform container)
        {
            characterPrefab = characterObj;
            poolContainer = container;

            // 预先创建对象到池中
            for (int i = 0; i < initialPoolSize; i++)
            {
                var character = CreateCharacterObject();
                characterPool.Push(character);

                // 每创建几个对象让出一帧，避免卡顿
                if (i % 5 == 0)
                    await UniTask.Yield();
            }
        }

        /// <summary>
        /// 获取一个字符对象
        /// </summary>
        public GameObject GetCharacter()
        {
            GameObject character;
            
            lock (stateLock)
             {
                 if (characterPool.Count > 0)
                 {
                     character = characterPool.Pop();
                 }
                 else
                 {
                     character = CreateCharacterObject();
                     // 记录性能监控
                     LyricFX.Utils.PerformanceMonitor.Instance.RecordCharacterCreation();
                     Debug.Log($"[字符工厂] 池空，新建对象，当前活动对象：{activeCharacters.Count}");
                 }
                 
                 // 确保对象状态正确设置
                 character.SetActive(true);
                 activeCharacters.Add(character);
                 characterStates[character] = CharacterState.InUse;
             }
            
            return character;
        }
        
        /// <summary>
        /// 回收一个字符对象
        /// </summary>
        public void ReleaseCharacter(GameObject character)
        {
            if (character == null)
                return;
                
            lock (stateLock)
            {
                // 检查对象状态，避免重复回收
                if (!characterStates.ContainsKey(character) || 
                    characterStates[character] != CharacterState.InUse)
                {
                    Debug.LogWarning($"[字符工厂] 尝试回收非活动状态的对象: {characterStates.GetValueOrDefault(character, CharacterState.Available)}");
                    return;
                }
                
                activeCharacters.Remove(character);
                characterStates[character] = CharacterState.PendingRecycle;
            }
            
            if (enableAsyncRecycling)
            {
                // 异步回收，避免卡顿
                pendingRecycleQueue.Enqueue(character);
                _ = ProcessPendingRecycleAsync();
            }
            else
            {
                // 同步回收（保留原有逻辑）
                RecycleCharacterImmediate(character);
            }
        }
        
        /// <summary>
        /// 立即回收字符对象（同步版本）
        /// </summary>
        private void RecycleCharacterImmediate(GameObject character)
        {
            // 检查对象是否有效
            if (character == null)
            {
                Debug.LogWarning("[字符工厂] 尝试回收空的字符对象");
                return;
            }
            
            lock (stateLock)
            {
                // 检查对象状态
                if (!characterStates.ContainsKey(character) || 
                    characterStates[character] == CharacterState.Recycling)
                {
                    return; // 已经在回收中或不存在
                }
                
                characterStates[character] = CharacterState.Recycling;
            }
            
            // 重置字符对象状态
            ResetCharacter(character);
            
            lock (stateLock)
            {
                // 如果池已满，直接销毁
                if (characterPool.Count >= maxPoolSize)
                {
                    characterStates.Remove(character);
                    if (character != null) // 再次检查对象有效性
                    {
                        GameObject.Destroy(character);
                    }
                    Debug.Log($"[字符工厂] 池已满，销毁对象，当前池容量：{characterPool.Count}");
                }
                else
                {
                    // 否则放回池中
                    if (character != null && character.transform != null)
                    {
                        character.SetActive(false);
                        character.transform.SetParent(poolContainer);
                        characterPool.Push(character);
                        characterStates[character] = CharacterState.Available;
                        // 记录性能监控
                        LyricFX.Utils.PerformanceMonitor.Instance.RecordCharacterRecycle();
                    }
                    else
                    {
                        Debug.LogWarning("[字符工厂] 字符对象已被销毁，无法放回池中");
                        characterStates.Remove(character);
                    }
                }
            }
        }
        
        /// <summary>
        /// 异步处理待回收的字符对象
        /// </summary>
        private async UniTask ProcessPendingRecycleAsync()
        {
            // 避免重复处理
            if (pendingRecycleQueue.Count == 0) return;
            
            // 确保在主线程中执行
            await UniTask.SwitchToMainThread();
            
            var processCount = 0;
            const int maxProcessPerFrame = 2; // VR优化：每帧最多处理2个对象
            
            while (pendingRecycleQueue.Count > 0 && processCount < maxProcessPerFrame)
            {
                GameObject character = null;
                
                lock (stateLock)
                {
                    if (pendingRecycleQueue.Count > 0)
                    {
                        character = pendingRecycleQueue.Dequeue();
                        // 验证对象状态
                        if (!characterStates.ContainsKey(character) || 
                            characterStates[character] != CharacterState.PendingRecycle)
                        {
                            character = null; // 跳过无效对象
                        }
                    }
                }
                
                if (character != null)
                {
                    RecycleCharacterImmediate(character);
                    processCount++;
                }
            }
            
            // 如果还有待处理的对象，让出一帧后继续处理
            if (pendingRecycleQueue.Count > 0)
            {
                await UniTask.Yield();
                _ = ProcessPendingRecycleAsync();
            }
        }
        
        /// <summary>
        /// 创建一个新的字符对象
        /// </summary>
        private GameObject CreateCharacterObject()
        {
            GameObject obj = null;
            
            if (characterPrefab != null)
            {
                obj = GameObject.Instantiate(characterPrefab, poolContainer);
            }
            else
            {
                Debug.LogError("Miss characterPrefab Object");
                return null;
            }
            
            obj.SetActive(false);
            return obj;
        }
        
        /// <summary>
        /// 重置字符对象状态
        /// </summary>
        private void ResetCharacter(GameObject character)
        {
            try
            {
                // 检查对象是否有效
                if (character == null || character.transform == null)
                {
                    Debug.LogWarning("[字符工厂] 尝试重置无效的字符对象");
                    return;
                }
                
                // 重置Transform
                character.transform.localPosition = Vector3.zero;
                character.transform.localRotation = Quaternion.identity;
                character.transform.localScale = Vector3.one;
                
                // 重置文本
                var textComponent = character.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = "";
                    textComponent.color = Color.white; // 恢复默认颜色
                    textComponent.alpha = 1.0f;
                }
                
                // 可以添加更多特定组件的重置逻辑
            }
            catch (Exception ex)
            {
                Debug.LogError($"[字符工厂] 重置字符对象失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 根据歌词长度预热对象池
        /// </summary>
        /// <param name="estimatedCharCount">预估字符数量</param>
        /// <param name="multiplier">预热倍数</param>
        public async UniTask WarmupPool(int estimatedCharCount, float multiplier = 1.5f)
        {
            int targetSize = Mathf.Min((int)(estimatedCharCount * multiplier), maxPoolSize);
            
            int currentPoolSize;
            lock (stateLock)
            {
                currentPoolSize = characterPool.Count;
            }
            
            if (currentPoolSize >= targetSize)
            {
                Debug.Log($"[字符工厂] 对象池已足够，当前: {currentPoolSize}, 目标: {targetSize}");
                return;
            }
            
            int createCount = targetSize - currentPoolSize;
            Debug.Log($"[字符工厂] 开始预热对象池，需创建 {createCount} 个对象");
            
            for (int i = 0; i < createCount; i++)
            {
                var character = CreateCharacterObject();
                
                lock (stateLock)
                {
                    characterPool.Push(character);
                    characterStates[character] = CharacterState.Available;
                }
                
                // VR优化：每创建2个对象让出一帧，避免卡顿
                if (i % 2 == 0)
                    await UniTask.Yield();
            }
            
            lock (stateLock)
            {
                Debug.Log($"[字符工厂] 对象池预热完成，当前容量: {characterPool.Count}");
            }
        }
        
        /// <summary>
        /// 设置性能优化配置
        /// </summary>
        /// <param name="enableAsync">是否启用异步回收</param>
        public void SetPerformanceConfig(bool enableAsync)
        {
            enableAsyncRecycling = enableAsync;
            Debug.Log($"[字符工厂] 异步回收设置为: {enableAsyncRecycling}");
        }
        
        /// <summary>
        /// 强制清理所有对象（紧急情况使用）
        /// </summary>
        public void ForceCleanupAll()
        {
            lock (stateLock)
            {
                Debug.LogWarning("[字符工厂] 执行强制清理所有对象");
                
                // 清理所有状态记录的对象
                var allObjects = new List<GameObject>(characterStates.Keys);
                foreach (var obj in allObjects)
                {
                    if (obj != null)
                    {
                        GameObject.Destroy(obj);
                    }
                }
                
                // 清空所有集合
                characterPool.Clear();
                activeCharacters.Clear();
                pendingRecycleQueue.Clear();
                characterStates.Clear();
                
                Debug.Log("[字符工厂] 强制清理完成");
            }
        }
        
        /// <summary>
        /// 获取对象池状态信息
        /// </summary>
        /// <returns>状态信息字符串</returns>
        public string GetPoolStatus()
        {
            lock (stateLock)
            {
                var availableCount = 0;
                var inUseCount = 0;
                var pendingCount = 0;
                var recyclingCount = 0;
                
                foreach (var state in characterStates.Values)
                {
                    switch (state)
                    {
                        case CharacterState.Available: availableCount++; break;
                        case CharacterState.InUse: inUseCount++; break;
                        case CharacterState.PendingRecycle: pendingCount++; break;
                        case CharacterState.Recycling: recyclingCount++; break;
                    }
                }
                
                return $"池容量: {characterPool.Count}/{maxPoolSize}, 活动: {activeCharacters.Count}, " +
                       $"状态统计 - 可用: {availableCount}, 使用中: {inUseCount}, 待回收: {pendingCount}, 回收中: {recyclingCount}";
            }
        }
        
        /// <summary>
        /// 验证对象状态一致性（调试用）
        /// </summary>
        /// <returns>是否一致</returns>
        public bool ValidateStateConsistency()
        {
            lock (stateLock)
            {
                var issues = new List<string>();
                
                // 检查活动对象状态
                foreach (var activeChar in activeCharacters)
                {
                    if (!characterStates.ContainsKey(activeChar) || 
                        characterStates[activeChar] != CharacterState.InUse)
                    {
                        issues.Add($"活动对象状态不一致: {activeChar.name}");
                    }
                }
                
                // 检查池中对象状态
                foreach (var poolChar in characterPool)
                {
                    if (!characterStates.ContainsKey(poolChar) || 
                        characterStates[poolChar] != CharacterState.Available)
                    {
                        issues.Add($"池对象状态不一致: {poolChar.name}");
                    }
                }
                
                if (issues.Count > 0)
                {
                    Debug.LogWarning($"[字符工厂] 状态一致性检查发现问题:\n{string.Join("\n", issues)}");
                    return false;
                }
                
                return true;
            }
        }
        
        /// <summary>
        /// 清空对象池
        /// </summary>
        public void ClearPool()
        {
            lock (stateLock)
            {
                // 先处理待回收队列
                while (pendingRecycleQueue.Count > 0)
                {
                    var character = pendingRecycleQueue.Dequeue();
                    characterStates.Remove(character);
                    GameObject.Destroy(character);
                }
                
                // 清空对象池
                while (characterPool.Count > 0)
                {
                    var obj = characterPool.Pop();
                    characterStates.Remove(obj);
                    GameObject.Destroy(obj);
                }
                
                // 清空活动对象（如果有的话）
                foreach (var activeChar in activeCharacters)
                {
                    characterStates.Remove(activeChar);
                    GameObject.Destroy(activeChar);
                }
                
                activeCharacters.Clear();
                characterStates.Clear();
            }
            
            Debug.Log("[字符工厂] 对象池已清空");
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearPool();
            Debug.Log("[字符工厂] 已释放资源");
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
    }
}
