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
    public class CharacterFactory : MonoBehaviour
    {
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private Transform poolContainer;
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private int maxPoolSize = 100;
        
        // 对象池
        private Stack<GameObject> characterPool = new Stack<GameObject>();
        
        // 已使用的字符对象
        private HashSet<GameObject> activeCharacters = new HashSet<GameObject>();
        
        /// <summary>
        /// 初始化字符工厂
        /// </summary>
        public async UniTask Initialize()
        {
            if (poolContainer == null)
            {
                poolContainer = new GameObject("CharacterPool").transform;
                poolContainer.SetParent(transform);
            }
            
            // 预先创建对象到池中
            for (int i = 0; i < initialPoolSize; i++)
            {
                var character = CreateCharacterObject();
                characterPool.Push(character);
                
                // 每创建几个对象让出一帧，避免卡顿
                if (i % 5 == 0)
                    await UniTask.Yield();
            }
            
            Debug.Log($"[字符工厂] 初始化完成，池容量：{characterPool.Count}");
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
                
            // 重置字符对象状态
            ResetCharacter(character);
            
            // 如果池已满，直接销毁
            if (characterPool.Count >= maxPoolSize)
            {
                Destroy(character);
                Debug.Log($"[字符工厂] 池已满，销毁对象，当前池容量：{characterPool.Count}");
            }
            else
            {
                // 否则放回池中
                character.SetActive(false);
                character.transform.SetParent(poolContainer);
                characterPool.Push(character);
            }
            
            activeCharacters.Remove(character);
        }
        
        /// <summary>
        /// 创建一个新的字符对象
        /// </summary>
        private GameObject CreateCharacterObject()
        {
            GameObject obj;
            
            if (characterPrefab != null)
            {
                obj = Instantiate(characterPrefab, poolContainer);
            }
            else
            {
                // 如果没有预制体，创建一个带TextMeshPro的对象
                obj = new GameObject("Character");
                obj.transform.SetParent(poolContainer);
                
                var textComponent = obj.AddComponent<TextMeshProUGUI>();
                textComponent.alignment = TextAlignmentOptions.Center;
                textComponent.fontSize = 36;
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
        /// 清空对象池
        /// </summary>
        public void ClearPool()
        {
            while (characterPool.Count > 0)
            {
                var obj = characterPool.Pop();
                Destroy(obj);
            }
            
            Debug.Log("[字符工厂] 对象池已清空");
        }
        
        private void OnDestroy()
        {
            ClearPool();
        }
    }
}
