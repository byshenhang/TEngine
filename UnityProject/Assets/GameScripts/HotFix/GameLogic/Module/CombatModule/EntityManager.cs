using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u5b9eu4f53u7ba1u7406u5668 - u7ba1u7406u6240u6709u6218u6597u5b9eu4f53u7684u751fu547du5468u671f
    /// </summary>
    public class EntityManager
    {
        private Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();
        private int _nextEntityId = 1;
        
        /// <summary>
        /// u521bu5efau5b9eu4f53
        /// </summary>
        public T CreateEntity<T>(EntityData data) where T : Entity, new()
        {
            T entity = new T();
            int entityId = _nextEntityId++;
            
            entity.Init(entityId, data);
            _entities.Add(entityId, entity);
            
            Log.Info($"[EntityManager] u521bu5efau5b9eu4f53 ID: {entityId}, u7c7bu578b: {typeof(T).Name}");
            
            return entity;
        }
        
        /// <summary>
        /// u83b7u53d6u5b9eu4f53
        /// </summary>
        public Entity GetEntity(int entityId)
        {
            if (_entities.TryGetValue(entityId, out Entity entity))
            {
                return entity;
            }
            
            return null;
        }
        
        /// <summary>
        /// u79fbu9664u5b9eu4f53
        /// </summary>
        public void RemoveEntity(int entityId)
        {
            if (_entities.TryGetValue(entityId, out Entity entity))
            {
                entity.OnDestroy();
                _entities.Remove(entityId);
                
                Log.Info($"[EntityManager] u79fbu9664u5b9eu4f53 ID: {entityId}");
            }
        }
        
        /// <summary>
        /// u66f4u65b0u6240u6709u6d3bu52a8u72b6u6001u7684u5b9eu4f53
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach (var entity in _entities.Values)
            {
                // u53eau66f4u65b0u5904u4e8eu6d3bu52a8u72b6u6001u7684u5b9eu4f53
                if (entity.IsActive)
                {
                    entity.OnUpdate(deltaTime);
                }
            }
        }
        
        /// <summary>
        /// u83b7u53d6u6240u6709u5b9eu4f53
        /// </summary>
        public List<Entity> GetAllEntities()
        {
            return new List<Entity>(_entities.Values);
        }
        
        /// <summary>
        /// u6e05u9664u6240u6709u5b9eu4f53
        /// </summary>
        public void Clear()
        {
            foreach (var entity in _entities.Values)
            {
                entity.OnDestroy();
            }
            
            _entities.Clear();
            Log.Info("[EntityManager] u6e05u9664u6240u6709u5b9eu4f53");
        }
        
        /// <summary>
        /// u83b7u53d6u5b9eu4f53u6570u91cf
        /// </summary>
        public int GetEntityCount()
        {
            return _entities.Count;
        }
    }
}
