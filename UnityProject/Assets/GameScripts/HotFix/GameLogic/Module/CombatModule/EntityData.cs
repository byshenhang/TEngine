using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u5b9eu4f53u6570u636e - u7528u4e8eu521bu5efau548cu521du59cbu5316u5b9eu4f53
    /// </summary>
    public class EntityData
    {
        /// <summary>
        /// u5b9eu4f53u540du79f0
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// u57fau7840u5c5eu6027u503c
        /// </summary>
        public Dictionary<AttributeType, float> BaseAttributes { get; set; } = new Dictionary<AttributeType, float>();
        
        /// <summary>
        /// u5b9eu4f53u4f4du7f6e
        /// </summary>
        public Vector3 Position { get; set; }
        
        /// <summary>
        /// u5b9eu4f53u65cbu8f6c
        /// </summary>
        public Quaternion Rotation { get; set; }
        
        /// <summary>
        /// u9884u5236u4f53u8defu5f84
        /// </summary>
        public string PrefabPath { get; set; }
        
        /// <summary>
        /// u9ed8u8ba4u6784u9020u51fdu6570
        /// </summary>
        public EntityData()
        {
            Name = "Unknown";
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            PrefabPath = string.Empty;
            
            // u8bbeu7f6eu9ed8u8ba4u5c5eu6027
            BaseAttributes[AttributeType.Health] = 100f;
            BaseAttributes[AttributeType.MaxHealth] = 100f;
            BaseAttributes[AttributeType.Attack] = 10f;
            BaseAttributes[AttributeType.Defense] = 5f;
            BaseAttributes[AttributeType.Speed] = 3f;
        }
        
        /// <summary>
        /// u8bbeu7f6eu5c5eu6027u503c
        /// </summary>
        public void SetAttribute(AttributeType type, float value)
        {
            BaseAttributes[type] = value;
        }
        
        /// <summary>
        /// u83b7u53d6u5c5eu6027u503c
        /// </summary>
        public float GetAttribute(AttributeType type)
        {
            if (BaseAttributes.TryGetValue(type, out float value))
            {
                return value;
            }
            
            return 0f;
        }
    }
}
