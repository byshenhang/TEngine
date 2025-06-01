using System;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// u52a8u4f5cu8282u70b9 - u6267u884cu5177u4f53u884cu4e3au7684u53f6u5b50u8282u70b9
    /// </summary>
    public abstract class ActionNode : BehaviorTreeNode
    {
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public ActionNode(string name, string description = "") 
            : base(name, description)
        {
        }
    }
    
    /// <summary>
    /// u7b80u5355u52a8u4f5cu8282u70b9 - u4f7fu7528u59cbu7ec8u6210u529fu6216u5931u8d25u7684u52a8u4f5c
    /// </summary>
    public class SimpleActionNode : ActionNode
    {
        // u52a8u4f5cu59cbu7ec8u8fd4u56deu6210u529fu8fd8u662fu5931u8d25
        private bool _alwaysSucceed;
        
        // u52a8u4f5cu51fdu6570
        private Action<CombatEntityBase> _action;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public SimpleActionNode(string name, Action<CombatEntityBase> action, bool alwaysSucceed = true, string description = "") 
            : base(name, description)
        {
            _action = action;
            _alwaysSucceed = alwaysSucceed;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            // u6267u884cu52a8u4f5c
            _action?.Invoke(entity);
            
            // u6839u636eu8bbeu7f6eu8fd4u56deu6210u529fu6216u5931u8d25
            _status = _alwaysSucceed ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
            return _status;
        }
    }
    
    /// <summary>
    /// u7b49u5f85u8282u70b9 - u7b49u5f85u6307u5b9au65f6u95f4
    /// </summary>
    public class WaitNode : ActionNode
    {
        // u7b49u5f85u65f6u95f4uff08u79d2uff09
        private float _waitTime;
        
        // u5f53u524du7b49u5f85u65f6u95f4
        private float _currentWaitTime;
        
        // u662fu5426u4f7fu7528u968fu673au7b49u5f85u65f6u95f4
        private bool _useRandomTime;
        
        // u968fu673au7b49u5f85u65f6u95f4u8303u56f4
        private float _minWaitTime;
        private float _maxWaitTime;
        
        /// <summary>
        /// u6784u9020u51fdu6570 - u56fau5b9au7b49u5f85u65f6u95f4
        /// </summary>
        public WaitNode(string name, float waitTime, string description = "") 
            : base(name, description)
        {
            _waitTime = waitTime;
            _useRandomTime = false;
            _currentWaitTime = 0f;
        }
        
        /// <summary>
        /// u6784u9020u51fdu6570 - u968fu673au7b49u5f85u65f6u95f4
        /// </summary>
        public WaitNode(string name, float minWaitTime, float maxWaitTime, string description = "") 
            : base(name, description)
        {
            _minWaitTime = minWaitTime;
            _maxWaitTime = maxWaitTime;
            _useRandomTime = true;
            _currentWaitTime = 0f;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            // u9996u6b21u6267u884c
            if (_status != BehaviorNodeStatus.Running)
            {
                // u968fu673au7b49u5f85u65f6u95f4
                if (_useRandomTime)
                {
                    _waitTime = UnityEngine.Random.Range(_minWaitTime, _maxWaitTime);
                }
                
                _currentWaitTime = 0f;
                _status = BehaviorNodeStatus.Running;
            }
            
            // u589eu52a0u5df2u7b49u5f85u65f6u95f4
            _currentWaitTime += Time.deltaTime;
            
            // u68c0u67e5u662fu5426u7b49u5f85u7ed3u675f
            if (_currentWaitTime >= _waitTime)
            {
                _status = BehaviorNodeStatus.Success;
            }
            
            return _status;
        }
        
        public override void Reset()
        {
            base.Reset();
            _currentWaitTime = 0f;
        }
    }
    
    /// <summary>
    /// u653bu51fbu8282u70b9 - u4f7fu7528u5b9eu4f53u7684u6307u5b9au6280u80fdu653bu51fbu76eeu6807
    /// </summary>
    public class AttackNode : ActionNode
    {
        // u6280u80fdID
        private string _skillId;
        
        // u76eeu6807u9009u62e9u5668u51fdu6570
        private Func<CombatEntityBase, CombatEntityBase> _targetSelector;
        
        // u662fu5426u6b63u5728u653bu51fb
        private bool _isAttacking;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public AttackNode(string name, string skillId, Func<CombatEntityBase, CombatEntityBase> targetSelector, string description = "") 
            : base(name, description)
        {
            _skillId = skillId;
            _targetSelector = targetSelector;
            _isAttacking = false;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            if (_status != BehaviorNodeStatus.Running)
            {
                // u9996u6b21u6267u884c
                _isAttacking = false;
            }
            
            // u5982u679cu6ca1u6709u5728u653bu51fbu4e14u4e0du662fu6b63u5728u4f7fu7528u6280u80fd
            if (!_isAttacking && !entity.IsActing)
            {
                // u9009u62e9u76eeu6807
                var target = _targetSelector?.Invoke(entity);
                if (target == null || !target.IsAlive)
                {
                    // u6ca1u6709u6709u6548u76eeu6807uff0cu653bu51fbu5931u8d25
                    _status = BehaviorNodeStatus.Failure;
                    return _status;
                }
                
                // u68c0u67e5u662fu5426u6709u6307u5b9au6280u80fd
                if (!entity.HasSkill(_skillId))
                {
                    // u6ca1u6709u6307u5b9au6280u80fduff0cu653bu51fbu5931u8d25
                    _status = BehaviorNodeStatus.Failure;
                    return _status;
                }
                
                // u4f7fu7528u6280u80fdu653bu51fbu76eeu6807
                entity.UseSkill(_skillId, target);
                _isAttacking = true;
                _status = BehaviorNodeStatus.Running;
            }
            // u5982u679cu6b63u5728u653bu51fbuff0cu7b49u5f85u653bu51fbu5b8cu6210
            else if (_isAttacking)
            {
                // u68c0u67e5u662fu5426u5b8cu6210u6280u80fdu65bdu653e
                if (!entity.IsActing && entity.CurrentSkillId == null)
                {
                    // u653bu51fbu5b8cu6210uff0cu8fd4u56deu6210u529f
                    _isAttacking = false;
                    _status = BehaviorNodeStatus.Success;
                }
                else
                {
                    // u4ecdu5728u653bu51fbu4e2d
                    _status = BehaviorNodeStatus.Running;
                }
            }
            
            return _status;
        }
        
        public override void Reset()
        {
            base.Reset();
            _isAttacking = false;
        }
    }
    
    /// <summary>
    /// u79fbu52a8u8282u70b9 - u79fbu52a8u5230u6307u5b9au76eeu6807u9644u8fd1
    /// </summary>
    public class MoveToTargetNode : ActionNode
    {
        // u76eeu6807u9009u62e9u5668u51fdu6570
        private Func<CombatEntityBase, CombatEntityBase> _targetSelector;
        
        // u8fbeu5230u76eeu6807u7684u8dddu79bbu9608u503c
        private float _stoppingDistance;
        
        // u79fbu52a8u8d85u65f6u65f6u95f4
        private float _timeoutDuration;
        
        // u5f53u524du8d85u65f6u8ba1u65f6u5668
        private float _timeoutTimer;
        
        // u5f53u524du76eeu6807
        private CombatEntityBase _currentTarget;
        
        /// <summary>
        /// u6784u9020u51fdu6570
        /// </summary>
        public MoveToTargetNode(string name, Func<CombatEntityBase, CombatEntityBase> targetSelector, 
            float stoppingDistance = 1.5f, float timeoutDuration = 5f, string description = "") 
            : base(name, description)
        {
            _targetSelector = targetSelector;
            _stoppingDistance = stoppingDistance;
            _timeoutDuration = timeoutDuration;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            if (_status != BehaviorNodeStatus.Running)
            {
                // u9996u6b21u6267u884cuff0cu521du59cbu5316
                _timeoutTimer = 0f;
                _currentTarget = _targetSelector?.Invoke(entity);
                
                if (_currentTarget == null || !_currentTarget.IsAlive)
                {
                    // u6ca1u6709u6709u6548u76eeu6807uff0cu79fbu52a8u5931u8d25
                    _status = BehaviorNodeStatus.Failure;
                    return _status;
                }
                
                _status = BehaviorNodeStatus.Running;
            }
            
            // u589eu52a0u8d85u65f6u8ba1u65f6u5668
            _timeoutTimer += Time.deltaTime;
            
            // u68c0u67e5u662fu5426u8d85u65f6
            if (_timeoutTimer >= _timeoutDuration)
            {
                Log.Warning($"{entity.Name} u79fbu52a8u5230u76eeu6807 {_currentTarget.Name} u8d85u65f6");
                _status = BehaviorNodeStatus.Failure;
                return _status;
            }
            
            // u68c0u67e5u76eeu6807u662fu5426u4ecdu7136u6709u6548
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                _status = BehaviorNodeStatus.Failure;
                return _status;
            }
            
            // u83b7u53d6u5b9eu4f53u548cu76eeu6807u7684u4f4du7f6e
            Vector3 entityPosition = Vector3.zero;
            Vector3 targetPosition = Vector3.zero;
            
            if (entity.GameObject != null)
            {
                entityPosition = entity.GameObject.transform.position;
            }
            
            if (_currentTarget.GameObject != null)
            {
                targetPosition = _currentTarget.GameObject.transform.position;
            }
            
            // u8ba1u7b97u4e0eu76eeu6807u7684u8dddu79bb
            float distanceToTarget = Vector3.Distance(entityPosition, targetPosition);
            
            // u68c0u67e5u662fu5426u5df2u8fbeu5230u76eeu6807
            if (distanceToTarget <= _stoppingDistance)
            {
                _status = BehaviorNodeStatus.Success;
                return _status;
            }
            
            // u5b9eu9645u79fbu52a8u903bu8f91
            MoveToPosition(entity, targetPosition);
            
            // u4ecdu5728u79fbu52a8u4e2d
            _status = BehaviorNodeStatus.Running;
            return _status;
        }
        
        /// <summary>
        /// u79fbu52a8u5230u6307u5b9au4f4du7f6e
        /// </summary>
        private void MoveToPosition(CombatEntityBase entity, Vector3 targetPosition)
        {
            // u5b9eu9645u5b9eu73b0u4e2du5e94u8c03u7528u5bfcu822au6216u79fbu52a8u7ec4u4ef6
            // u8fd9u91ccu53eau662fu6a21u62dfu79fbu52a8u903bu8f91
            
            if (entity.GameObject != null)
            {
                // u83b7u53d6u79fbu52a8u901fu5ea6
                float moveSpeed = entity.Attributes.GetAttributeValue(AttributeType.MoveSpeed);
                
                // u8ba1u7b97u79fbu52a8u65b9u5411
                Vector3 currentPosition = entity.GameObject.transform.position;
                Vector3 direction = (targetPosition - currentPosition).normalized;
                
                // u79fbu52a8u5b9eu4f53
                // entity.GameObject.transform.position += direction * moveSpeed * Time.deltaTime;
                
                // u65cbu8f6cu9762u5411u76eeu6807
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    // entity.GameObject.transform.rotation = Quaternion.Slerp(
                    //    entity.GameObject.transform.rotation, targetRotation, 10f * Time.deltaTime);
                }
                
                Log.Info($"{entity.Name} u6b63u5728u79fbu52a8u5230 {targetPosition}uff0cu901fu5ea6: {moveSpeed}");
            }
        }
        
        public override void Reset()
        {
            base.Reset();
            _timeoutTimer = 0f;
            _currentTarget = null;
        }
    }
}
