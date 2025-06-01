using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 行为树节点状态
    /// </summary>
    public enum BehaviorNodeStatus
    {
        Inactive,   // 未激活
        Running,    // 运行中
        Success,    // 成功完成
        Failure     // 失败
    }
    
    /// <summary>
    /// 行为树节点基类 - 所有行为树节点的抽象基类
    /// </summary>
    public abstract class BehaviorTreeNode
    {
        // 节点名称
        protected string _name;
        
        // 节点状态
        protected BehaviorNodeStatus _status = BehaviorNodeStatus.Inactive;
        
        // 节点描述
        protected string _description;
        
        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// 节点状态
        /// </summary>
        public BehaviorNodeStatus Status => _status;
        
        /// <summary>
        /// 节点描述
        /// </summary>
        public string Description => _description;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public BehaviorTreeNode(string name, string description = "")
        {
            _name = name;
            _description = description;
        }
        
        /// <summary>
        /// 更新节点
        /// </summary>
        public abstract BehaviorNodeStatus Update(CombatEntityBase entity);
        
        /// <summary>
        /// 重置节点状态
        /// </summary>
        public virtual void Reset()
        {
            _status = BehaviorNodeStatus.Inactive;
        }
    }
    
    /// <summary>
    /// 复合节点 - 可以包含多个子节点的节点
    /// </summary>
    public abstract class CompositeNode : BehaviorTreeNode
    {
        // 子节点列表
        protected List<BehaviorTreeNode> _children = new List<BehaviorTreeNode>();
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public CompositeNode(string name, string description = "") 
            : base(name, description)
        {
        }
        
        /// <summary>
        /// 添加子节点
        /// </summary>
        public void AddChild(BehaviorTreeNode child)
        {
            if (child != null)
            {
                _children.Add(child);
            }
        }
        
        /// <summary>
        /// 移除子节点
        /// </summary>
        public bool RemoveChild(BehaviorTreeNode child)
        {
            return _children.Remove(child);
        }
        
        /// <summary>
        /// 获取所有子节点
        /// </summary>
        public List<BehaviorTreeNode> GetChildren()
        {
            return _children;
        }
        
        /// <summary>
        /// 重置节点状态，包括所有子节点
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            
            foreach (var child in _children)
            {
                child.Reset();
            }
        }
    }
    
    /// <summary>
    /// 选择节点 - 按顺序执行子节点，直到有一个成功为止
    /// </summary>
    public class SelectorNode : CompositeNode
    {
        // 当前正在执行的子节点索引
        private int _currentChildIndex = 0;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public SelectorNode(string name, string description = "") 
            : base(name, description)
        {
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            // 如果没有子节点，返回失败
            if (_children.Count == 0)
            {
                _status = BehaviorNodeStatus.Failure;
                return _status;
            }
            
            // 如果上次处于运行状态，从上次的索引继续
            if (_status != BehaviorNodeStatus.Running)
            {
                _currentChildIndex = 0;
            }
            
            // 执行子节点，直到找到一个成功的节点
            while (_currentChildIndex < _children.Count)
            {
                var child = _children[_currentChildIndex];
                var childStatus = child.Update(entity);
                
                // 如果子节点成功，选择节点成功
                if (childStatus == BehaviorNodeStatus.Success)
                {
                    _status = BehaviorNodeStatus.Success;
                    return _status;
                }
                // 如果子节点仍在运行，选择节点仍在运行
                else if (childStatus == BehaviorNodeStatus.Running)
                {
                    _status = BehaviorNodeStatus.Running;
                    return _status;
                }
                // 如果子节点失败，尝试下一个子节点
                else
                {
                    _currentChildIndex++;
                }
            }
            
            // 所有子节点都失败，选择节点失败
            _status = BehaviorNodeStatus.Failure;
            return _status;
        }
        
        public override void Reset()
        {
            base.Reset();
            _currentChildIndex = 0;
        }
    }
    
    /// <summary>
    /// 序列节点 - 按顺序执行子节点，直到所有子节点都成功或有一个失败
    /// </summary>
    public class SequenceNode : CompositeNode
    {
        // 当前正在执行的子节点索引
        private int _currentChildIndex = 0;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public SequenceNode(string name, string description = "") 
            : base(name, description)
        {
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            // 如果没有子节点，返回成功
            if (_children.Count == 0)
            {
                _status = BehaviorNodeStatus.Success;
                return _status;
            }
            
            // 如果上次处于运行状态，从上次的索引继续
            if (_status != BehaviorNodeStatus.Running)
            {
                _currentChildIndex = 0;
            }
            
            // 执行子节点，直到所有节点成功或有一个失败
            while (_currentChildIndex < _children.Count)
            {
                var child = _children[_currentChildIndex];
                var childStatus = child.Update(entity);
                
                // 如果子节点失败，序列节点失败
                if (childStatus == BehaviorNodeStatus.Failure)
                {
                    _status = BehaviorNodeStatus.Failure;
                    return _status;
                }
                // 如果子节点仍在运行，序列节点仍在运行
                else if (childStatus == BehaviorNodeStatus.Running)
                {
                    _status = BehaviorNodeStatus.Running;
                    return _status;
                }
                // 如果子节点成功，继续下一个子节点
                else
                {
                    _currentChildIndex++;
                }
            }
            
            // 所有子节点都成功，序列节点成功
            _status = BehaviorNodeStatus.Success;
            return _status;
        }
        
        public override void Reset()
        {
            base.Reset();
            _currentChildIndex = 0;
        }
    }
    
    /// <summary>
    /// 并行节点 - 同时执行所有子节点
    /// </summary>
    public class ParallelNode : CompositeNode
    {
        // 成功策略
        private ParallelPolicy _successPolicy;
        
        // 失败策略
        private ParallelPolicy _failurePolicy;
        
        /// <summary>
        /// 并行节点策略
        /// </summary>
        public enum ParallelPolicy
        {
            RequireOne,  // 只需要一个子节点满足条件
            RequireAll   // 需要所有子节点满足条件
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ParallelNode(string name, ParallelPolicy successPolicy = ParallelPolicy.RequireAll, 
            ParallelPolicy failurePolicy = ParallelPolicy.RequireOne, string description = "") 
            : base(name, description)
        {
            _successPolicy = successPolicy;
            _failurePolicy = failurePolicy;
        }
        
        public override BehaviorNodeStatus Update(CombatEntityBase entity)
        {
            int successCount = 0;
            int failureCount = 0;
            int runningCount = 0;
            
            // 遍历所有子节点
            foreach (var child in _children)
            {
                var childStatus = child.Update(entity);
                
                if (childStatus == BehaviorNodeStatus.Success)
                {
                    successCount++;
                    
                    // 如果成功策略是RequireOne，立即返回成功
                    if (_successPolicy == ParallelPolicy.RequireOne)
                    {
                        _status = BehaviorNodeStatus.Success;
                        return _status;
                    }
                }
                else if (childStatus == BehaviorNodeStatus.Failure)
                {
                    failureCount++;
                    
                    // 如果失败策略是RequireOne，立即返回失败
                    if (_failurePolicy == ParallelPolicy.RequireOne)
                    {
                        _status = BehaviorNodeStatus.Failure;
                        return _status;
                    }
                }
                else if (childStatus == BehaviorNodeStatus.Running)
                {
                    runningCount++;
                }
            }
            
            // 检查所有子节点的结果
            if (_failurePolicy == ParallelPolicy.RequireAll && failureCount == _children.Count)
            {
                _status = BehaviorNodeStatus.Failure;
                return _status;
            }
            
            if (_successPolicy == ParallelPolicy.RequireAll && successCount == _children.Count)
            {
                _status = BehaviorNodeStatus.Success;
                return _status;
            }
            
            // 如果仍有节点在运行，则并行节点仍在运行
            if (runningCount > 0)
            {
                _status = BehaviorNodeStatus.Running;
                return _status;
            }
            
            // 如果到这里，成功策略是RequireAll但不是所有节点都成功，返回失败
            _status = BehaviorNodeStatus.Failure;
            return _status;
        }
    }
}
