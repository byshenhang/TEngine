using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LyricFX.Factory
{
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
            
            character.SetActive(true);
            activeCharacters.Add(character);
            
            return character;
        }
        
        /// <summary>
        /// 回收一个字符对象
        /// </summary>
        public void ReleaseCharacter(GameObject character)
        {
            if (character == null || !activeCharacters.Contains(character))
                return;
                
            activeCharacters.Remove(character);
            
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
            // 重置字符对象状态
            ResetCharacter(character);
            
            // 如果池已满，直接销毁
            if (characterPool.Count >= maxPoolSize)
            {
               GameObject.Destroy(character);
                Debug.Log($"[字符工厂] 池已满，销毁对象，当前池容量：{characterPool.Count}");
            }
            else
            {
                // 否则放回池中
                character.SetActive(false);
                character.transform.SetParent(poolContainer);
                characterPool.Push(character);
                // 记录性能监控
                LyricFX.Utils.PerformanceMonitor.Instance.RecordCharacterRecycle();
            }
        }
        
        /// <summary>
        /// 异步处理待回收的字符对象
        /// </summary>
        private async UniTask ProcessPendingRecycleAsync()
        {
            // 避免重复处理
            if (pendingRecycleQueue.Count == 0) return;
            
            var processCount = 0;
            const int maxProcessPerFrame = 3; // 每帧最多处理3个对象
            
            while (pendingRecycleQueue.Count > 0 && processCount < maxProcessPerFrame)
            {
                var character = pendingRecycleQueue.Dequeue();
                RecycleCharacterImmediate(character);
                processCount++;
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
            GameObject obj;
            
            if (characterPrefab != null)
            {
                obj = GameObject.Instantiate(characterPrefab, poolContainer);
            }
            else
            {
                // 如果没有预制体，创建一个带TextMeshPro的对象
                obj = new GameObject("Character");
                obj.transform.SetParent(poolContainer);
                
                // 添加RectTransform组件并设置基本属性
                var rectTransform = obj.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(50, 50); // 设置字符大小
                rectTransform.anchorMin = new Vector2(0, 0.5f); // 左中锚点
                rectTransform.anchorMax = new Vector2(0, 0.5f); // 左中锚点
                rectTransform.pivot = new Vector2(0, 0.5f); // 左中心为轴点
                
                var textComponent = obj.AddComponent<TextMeshProUGUI>();
                textComponent.alignment = TextAlignmentOptions.Center;
                textComponent.fontSize = 36;
                
                // 设置TextMeshPro的RectTransform填充整个字符对象
                var textRect = textComponent.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
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
                // 重置变换
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
            
            if (characterPool.Count >= targetSize)
            {
                Debug.Log($"[字符工厂] 对象池已足够，当前: {characterPool.Count}, 目标: {targetSize}");
                return;
            }
            
            int createCount = targetSize - characterPool.Count;
            Debug.Log($"[字符工厂] 开始预热对象池，需创建 {createCount} 个对象");
            
            for (int i = 0; i < createCount; i++)
            {
                var character = CreateCharacterObject();
                characterPool.Push(character);
                
                // 每创建5个对象让出一帧，避免卡顿
                if (i % 5 == 0)
                    await UniTask.Yield();
            }
            
            Debug.Log($"[字符工厂] 对象池预热完成，当前容量: {characterPool.Count}");
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
        /// 获取对象池状态信息
        /// </summary>
        /// <returns>状态信息字符串</returns>
        public string GetPoolStatus()
        {
            return $"池容量: {characterPool.Count}/{maxPoolSize}, 活动对象: {activeCharacters.Count}, 待回收: {pendingRecycleQueue.Count}";
        }
        
        /// <summary>
        /// 清空对象池
        /// </summary>
        public void ClearPool()
        {
            // 先处理待回收队列
            while (pendingRecycleQueue.Count > 0)
            {
                var character = pendingRecycleQueue.Dequeue();
                GameObject.Destroy(character);
            }
            
            // 清空对象池
            while (characterPool.Count > 0)
            {
                var obj = characterPool.Pop();
                GameObject.Destroy(obj);
            }
            
            Debug.Log("[字符工厂] 对象池已清空");
        }
        
        private void OnDestroy()
        {
            ClearPool();
        }
    }
}
