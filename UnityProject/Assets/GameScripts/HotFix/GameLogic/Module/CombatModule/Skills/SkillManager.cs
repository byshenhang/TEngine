using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u6280u80fdu7ba1u7406u5668 - u7ba1u7406u6240u6709u5b9eu4f53u7684u6280u80fdu6e38u620fu901au7528u6280u80fd
    /// </summary>
    public class SkillManager
    {
        private Dictionary<int, SkillConfig> _skillConfigs = new Dictionary<int, SkillConfig>();
        private Dictionary<Entity, List<Skill>> _entitySkills = new Dictionary<Entity, List<Skill>>();
        
        /// <summary>
        /// u521du59cbu5316u6280u80fdu7ba1u7406u5668
        /// </summary>
        public void Initialize()
        {
            // u52a0u8f7du6280u80fdu914du7f6e
            LoadSkillConfigs();
            Log.Info($"[SkillManager] u521du59cbu5316u5b8cu6210uff0cu52a0u8f7du4e86 {_skillConfigs.Count} u4e2au6280u80fdu914du7f6e");
        }
        
        /// <summary>
        /// u66f4u65b0u6240u6709u6280u80fd
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach (var entitySkillList in _entitySkills.Values)
            {
                foreach (var skill in entitySkillList)
                {
                    skill.Update(deltaTime);
                }
            }
        }
        
        /// <summary>
        /// u6e05u7406u6280u80fdu7ba1u7406u5668
        /// </summary>
        public void Clear()
        {
            _entitySkills.Clear();
        }
        
        /// <summary>
        /// u79fbu9664u5b9eu4f53u7684u6240u6709u6280u80fd
        /// </summary>
        public void RemoveEntitySkills(Entity entity)
        {
            if (_entitySkills.ContainsKey(entity))
            {
                _entitySkills.Remove(entity);
            }
        }
        
        /// <summary>
        /// u7ed9u5b9eu4f53u6dfbu52a0u6280u80fd
        /// </summary>
        public Skill AddSkillToEntity(Entity entity, int skillId)
        {
            if (!_skillConfigs.TryGetValue(skillId, out SkillConfig config))
            {
                Log.Error($"[SkillManager] u65e0u6cd5u627eu5230u6280u80fdID: {skillId}");
                return null;
            }
            
            // u521bu5efau76f8u5e94u7c7bu578bu7684u6280u80fd
            Skill newSkill = CreateSkillInstance(config);
            
            if (newSkill != null)
            {
                // u521du59cbu5316u6280u80fd
                newSkill.Init(config, entity);
                
                // u5c06u6280u80fdu6dfbu52a0u5230u5b9eu4f53u7684u6280u80fdu5217u8868
                if (!_entitySkills.TryGetValue(entity, out List<Skill> skills))
                {
                    skills = new List<Skill>();
                    _entitySkills[entity] = skills;
                }
                
                skills.Add(newSkill);
                Log.Info($"[SkillManager] u5b9eu4f53 {entity.Name} u6dfbu52a0u4e86u6280u80fd {config.Name}");
                
                return newSkill;
            }
            
            return null;
        }
        
        /// <summary>
        /// u83b7u53d6u5b9eu4f53u7684u6240u6709u6280u80fd
        /// </summary>
        public List<Skill> GetEntitySkills(Entity entity)
        {
            if (_entitySkills.TryGetValue(entity, out List<Skill> skills))
            {
                return skills;
            }
            
            return new List<Skill>();
        }
        
        /// <summary>
        /// u83b7u53d6u5b9eu4f53u7684u6307u5b9au7c7bu578bu6280u80fd
        /// </summary>
        public List<Skill> GetEntitySkillsByType(Entity entity, SkillType type)
        {
            List<Skill> result = new List<Skill>();
            
            if (_entitySkills.TryGetValue(entity, out List<Skill> skills))
            {
                foreach (var skill in skills)
                {
                    if (skill.Config.Type == type)
                    {
                        result.Add(skill);
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// u52a0u8f7du6280u80fdu914du7f6e
        /// </summary>
        private void LoadSkillConfigs()
        {
            // u4eceu914du7f6eu8868u4e2du52a0u8f7du6280u80fdu914du7f6e
            // u8fd9u91ccu5148u4f7fu7528u6a21u62dfu6570u636eu8fdeujoin
            _skillConfigs = MockDataProvider.Instance.GetAllSkillConfigs();
        }
        
        /// <summary>
        /// u6839u636eu6280u80fdu7c7bu578bu521bu5efau76f8u5e94u7684u6280u80fdu5b9eu4f8b
        /// </summary>
        private Skill CreateSkillInstance(SkillConfig config)
        {
            switch (config.Type)
            {
                case SkillType.MeleeAttack:
                    return new MeleeAttackSkill();
                    
                case SkillType.RangedAttack:
                    return new RangedAttackSkill();
                    
                case SkillType.AreaEffect:
                    return new AreaEffectSkill();
                    
                case SkillType.Buff:
                    return new BuffSkill();
                    
                case SkillType.Debuff:
                    return new DebuffSkill();
                    
                // u5176u4ed6u6280u80fdu7c7bu578bu5b9eu73b0...
                    
                default:
                    Log.Error($"[SkillManager] u4e0du652fu6301u7684u6280u80fdu7c7bu578b: {config.Type}");
                    return null;
            }
        }
    }
}
