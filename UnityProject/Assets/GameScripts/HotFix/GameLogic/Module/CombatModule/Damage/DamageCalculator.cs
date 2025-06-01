using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u4f24u5bb3u7c7bu578b
    /// </summary>
    public enum DamageType
    {
        Physical = 0,   // u7269u7406u4f24u5bb3
        Magical = 1,    // u9b54u6cd5u4f24u5bb3
        True = 2,       // u771fu5b9eu4f24u5bb3uff08u65e0u89c6u9632u5fa1uff09
        Fire = 3,       // u706bu7cfbu4f24u5bb3
        Ice = 4,        // u51b0u7cfbu4f24u5bb3
        Lightning = 5,   // u96f7u7cfbu4f24u5bb3
        Magic = 6,
        Energy = 7
    }
    
    /// <summary>
    /// u4f24u5bb3u6570u636e - u7528u4e8eu8bb0u5f55u4f24u5bb3u8be6u60c5
    /// </summary>
    public class DamageData
    {
        /// <summary>
        /// u539fu59cbu4f24u5bb3u503c
        /// </summary>
        public float OriginalValue { get; set; }
        
        /// <summary>
        /// u6700u7ec8u4f24u5bb3u503cuff08u7ecfu8fc7u8ba1u7b97u540euff09
        /// </summary>
        public float FinalValue { get; set; }
        
        /// <summary>
        /// u4f24u5bb3u7c7bu578b
        /// </summary>
        public DamageType Type { get; set; }
        
        /// <summary>
        /// u662fu5426u66b4u51fb
        /// </summary>
        public bool IsCritical { get; set; }
        
        /// <summary>
        /// u4f24u5bb3u6765u6e90
        /// </summary>
        public Entity Source { get; set; }
        
        /// <summary>
        /// u4f24u5bb3u76eeu6807
        /// </summary>
        public Entity Target { get; set; }
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public DamageData(float damage, DamageType type, Entity source, Entity target, bool isCritical = false)
        {
            OriginalValue = damage;
            FinalValue = damage;
            Type = type;
            Source = source;
            Target = target;
            IsCritical = isCritical;
        }
    }
    
    /// <summary>
    /// u4f24u5bb3u8ba1u7b97u5668 - u63d0u4f9bu4f24u5bb3u8ba1u7b97u548cu4f24u5bb3u5904u7406u76f8u5173u529fu80fd
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// u8ba1u7b97u4f24u5bb3u503c
        /// </summary>
        /// <param name="attacker">u653bu51fbu8005</param>
        /// <param name="defender">u9632u5fa1u8005</param>
        /// <param name="baseDamage">u57fau7840u4f24u5bb3</param>
        /// <param name="attackMultiplier">u653bu51fbu529bu7cfbu6570</param>
        /// <param name="damageType">u4f24u5bb3u7c7bu578b</param>
        /// <returns>u8ba1u7b97u540eu7684u6700u7ec8u4f24u5bb3u503c</returns>
        public static float CalculateDamage(
            Entity attacker, 
            Entity defender, 
            float baseDamage, 
            float attackMultiplier = 1.0f, 
            DamageType damageType = DamageType.Physical)
        {
            if (attacker == null || defender == null)
            {
                return 0f;
            }
            
            // u83b7u53d6u653bu51fbu8005u5c5eu6027
            float attackerAttack = attacker.Attributes.GetAttribute(AttributeType.Attack);
            float criticalRate = attacker.Attributes.GetAttribute(AttributeType.Critical);
            float criticalDamage = attacker.Attributes.GetAttribute(AttributeType.CritDamage);
            
            // u83b7u53d6u9632u5fa1u8005u5c5eu6027
            float defenderDefense = defender.Attributes.GetAttribute(AttributeType.Defense);
            
            // u521du59cbu4f24u5bb3u503c
            float damage = baseDamage + (attackerAttack * attackMultiplier);
            
            // u786eu5b9au662fu5426u66b4u51fb
            bool isCritical = Random.value <= (criticalRate / 100f);
            
            // u5982u679cu662fu66b4u51fbuff0cu5e94u7528u66b4u51fbu4f24u5bb3
            if (isCritical)
            {
                damage *= (1f + criticalDamage / 100f);
            }
            
            // u8ba1u7b97u9632u5fa1u51cfu514d
            float damageReduction = 0f;
            
            switch (damageType)
            {
                case DamageType.Physical:
                    // u7269u7406u4f24u5bb3u516cu5f0f: u51cfu4f24 = u9632u5fa1 / (u9632u5fa1 + 100)
                    damageReduction = defenderDefense / (defenderDefense + 100f);
                    break;
                    
                case DamageType.Magical:
                    // u9b54u6cd5u4f24u5bb3u516cu5f0f: u51cfu4f24 = u9b54u6297 / (u9b54u6297 + 100)
                    float magicResist = defenderDefense * 0.5f; // u7b80u5316u5b9eu73b0uff0cu9b54u6cd5u6297u6027u4e3au9632u5fa1u7684u4e00u534a
                    damageReduction = magicResist / (magicResist + 100f);
                    break;
                    
                case DamageType.True:
                    // u771fu5b9eu4f24u5bb3u4e0du53d7u9632u5fa1u5f71u54cd
                    damageReduction = 0f;
                    break;
                    
                // u5176u4ed6u5143u7d20u4f24u5bb3u53efu4ee5u6839u636eu9700u8981u6269u5c55
                default:
                    damageReduction = defenderDefense / (defenderDefense + 100f);
                    break;
            }
            
            // u5e94u7528u51cfu4f24
            damage *= (1f - damageReduction);
            
            // u786eu4fddu4f24u5bb3u4e0du4f1au4e3au8d1f
            damage = Mathf.Max(1f, damage);
            
            // u521bu5efau4f24u5bb3u6570u636eu5bf9u8c61uff08u53efu7528u4e8eu4e8bu4ef6u5206u53d1u6216u8bb0u5f55uff09
            DamageData damageData = new DamageData(baseDamage, damageType, attacker, defender, isCritical)
            {
                FinalValue = damage
            };
            
            // u5206u53d1u4f24u5bb3u4e8bu4ef6uff08u5982u679cu6709u4e8bu4ef6u7cfbu7edfuff09
            // EventManager.Dispatch("OnDamageDealt", damageData);
            
            // u8fd4u56deu6700u7ec8u4f24u5bb3u503c
            return damage;
        }
    }
}
