using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace GameLogic
{
    /// <summary>
    /// 玩家战斗实体
    /// </summary>
    public class CombatEntityPlayer : CombatEntityBase
    {
        // VR控制器引用
        private Transform _leftController;
        private Transform _rightController;
        
        // VR控制器数据
        private Vector3 _leftControllerPosition;
        private Quaternion _leftControllerRotation;
        private Vector3 _rightControllerPosition;
        private Quaternion _rightControllerRotation;
        
        // VR控制器速度（用于手势检测）
        private Vector3 _leftControllerVelocity;
        private Vector3 _rightControllerVelocity;
        private Vector3 _prevLeftControllerPosition;
        private Vector3 _prevRightControllerPosition;

        // 是否处于格挡状态
        private bool _isBlocking = false;
        
        // 格挡姿势检测参数
        private float _blockingAngleThreshold = 30f; // 双手之间的角度阈值
        private float _blockingDistanceThreshold = 0.3f; // 双手之间的距离阈值
        private float _blockingHeightThreshold = 1.3f; // 相对地面的高度阈值
        
        // 格挡减伤系数
        private float _blockDamageReduction = 0.5f;
        
        // 手势相关参数
        private bool _isLeftSwinging = false;
        private bool _isRightSwinging = false;
        private float _swingVelocityThreshold = 1.5f; // 挥动速度阈值
        private float _swingCooldown = 0.5f; // 挥动冷却时间
        private float _leftSwingCooldownTimer = 0f;
        private float _rightSwingCooldownTimer = 0f;
        
        /// <summary>
        /// 是否处于格挡状态
        /// </summary>
        public bool IsBlocking => _isBlocking;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public CombatEntityPlayer(string entityId, string name, Dictionary<AttributeType, float> attributes) 
            : base(entityId, name, attributes)
        {
            // 设置为玩家阵营
            Faction = CombatFaction.Player;
            EntityType = "player";
            
            // 初始化VR控制器数据
            _leftControllerPosition = Vector3.zero;
            _leftControllerRotation = Quaternion.identity;
            _rightControllerPosition = Vector3.zero;
            _rightControllerRotation = Quaternion.identity;
            _prevLeftControllerPosition = Vector3.zero;
            _prevRightControllerPosition = Vector3.zero;
            
            // 添加玩家默认技能
            AddDefaultSkills();
            
            // 注册VR输入事件（通过XRIModule）
            RegisterVRInputEvents();
        }
        
        /// <summary>
        /// 注册VR输入事件
        /// </summary>
        private void RegisterVRInputEvents()
        {
            // 注册XRI模块的输入事件，例如：握把扳机、按钮等
            if (XRIModule.Instance != null)
            {
                // 注册左右手控制器的主要按钮事件
                XRIModule.Instance.RegisterTriggerEvent(XRNode.LeftHand, OnLeftTriggerPressed, OnLeftTriggerReleased);
                XRIModule.Instance.RegisterTriggerEvent(XRNode.RightHand, OnRightTriggerPressed, OnRightTriggerReleased);
                
                // 注册握把(Grip)按钮事件
                XRIModule.Instance.RegisterGripEvent(XRNode.LeftHand, OnLeftGripPressed, OnLeftGripReleased);
                XRIModule.Instance.RegisterGripEvent(XRNode.RightHand, OnRightGripPressed, OnRightGripReleased);
                
                // 注册其他按钮事件，如需要
                XRIModule.Instance.RegisterPrimaryButtonEvent(XRNode.LeftHand, OnLeftPrimaryButtonPressed, null);
                XRIModule.Instance.RegisterPrimaryButtonEvent(XRNode.RightHand, OnRightPrimaryButtonPressed, null);
                
                Log.Info($"玩家实体 {EntityId} 已注册VR输入事件");
            }
            else
            {
                Log.Error("XRIModule实例不可用，无法注册VR输入事件");
            }
        }
        
        /// <summary>
        /// 添加默认技能
        /// </summary>
        private void AddDefaultSkills()
        {
            // 基础攻击技能
            AddSkill("basic_attack");
            
            // 基础治疗技能
            AddSkill("basic_heal");
            
            // 可以根据玩家类型添加更多技能
        }
        
        /// <summary>
        /// 更新玩家实体
        /// </summary>
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            if (!_isActive || !IsAlive) return;
            
            // 更新VR控制器数据和速度
            UpdateControllerData(deltaTime);
            
            // 检测格挡状态
            CheckBlockingState();
            
            // 更新手势冷却时间
            UpdateGestureCooldowns(deltaTime);
            
            // 检测手势动作
            DetectGestures();
        }
        
        /// <summary>
        /// 更新VR控制器数据
        /// </summary>
        private void UpdateControllerData(float deltaTime)
        {
            if (_leftController == null || _rightController == null) return;
            
            // 保存上一帧位置用于计算速度
            _prevLeftControllerPosition = _leftControllerPosition;
            _prevRightControllerPosition = _rightControllerPosition;
            
            // 更新当前控制器位置和旋转
            _leftControllerPosition = _leftController.position;
            _leftControllerRotation = _leftController.rotation;
            _rightControllerPosition = _rightController.position;
            _rightControllerRotation = _rightController.rotation;
            
            // 计算控制器速度
            _leftControllerVelocity = (_leftControllerPosition - _prevLeftControllerPosition) / deltaTime;
            _rightControllerVelocity = (_rightControllerPosition - _prevRightControllerPosition) / deltaTime;
            
            // 更新玩家实体位置和旋转（基于头显位置或其他）
            if (XRIModule.Instance != null)
            {
                Transform headTransform = XRIModule.Instance.GetHeadTransform();
                if (headTransform != null)
                {
                    Vector3 playerPosition = headTransform.position;
                    playerPosition.y = 0; // 确保玩家在地面上
                    SetPosition(playerPosition);
                    
                    // 保持Y轴旋转，忽略其他轴的旋转
                    Vector3 forward = headTransform.forward;
                    forward.y = 0;
                    if (forward.magnitude > 0.01f)
                    {
                        SetRotation(Quaternion.LookRotation(forward));
                    }
                }
            }
        }
        
        /// <summary>
        /// 检测格挡状态
        /// </summary>
        private void CheckBlockingState()
        {
            if (_leftController == null || _rightController == null) return;
            
            // 检测双手是否在适合格挡的位置
            // 1. 检查双手之间的距离是否在合适范围内
            float handsDistance = Vector3.Distance(_leftControllerPosition, _rightControllerPosition);
            
            // 2. 检查双手是否在适当高度（胸部高度）
            float averageHeight = (_leftControllerPosition.y + _rightControllerPosition.y) / 2;
            bool correctHeight = averageHeight > _blockingHeightThreshold;
            
            // 3. 检查双手是否形成了横向防御姿势（双手大致水平）
            Vector3 handDirection = (_rightControllerPosition - _leftControllerPosition).normalized;
            float angleFromHorizontal = Vector3.Angle(handDirection, Vector3.right);
            bool correctAngle = angleFromHorizontal < _blockingAngleThreshold;
            
            // 满足所有条件时认为玩家在格挡
            _isBlocking = handsDistance < _blockingDistanceThreshold && correctHeight && correctAngle;
            
            // 当状态变化时记录日志
            if (_isBlocking && !_isBlocking) // 刚开始格挡
            {
                Log.Info($"玩家 {EntityId} 开始格挡");
            }
            else if (!_isBlocking && _isBlocking) // 格挡结束
            {
                Log.Info($"玩家 {EntityId} 结束格挡");
            }
        }
        
        /// <summary>
        /// 设置VR控制器引用
        /// </summary>
        public void SetControllers(Transform leftController, Transform rightController)
        {
            _leftController = leftController;
            _rightController = rightController;
            
            // 初始化控制器位置和旋转
            if (_leftController != null)
            {
                _leftControllerPosition = _leftController.position;
                _leftControllerRotation = _leftController.rotation;
                _prevLeftControllerPosition = _leftControllerPosition;
            }
            
            if (_rightController != null)
            {
                _rightControllerPosition = _rightController.position;
                _rightControllerRotation = _rightController.rotation;
                _prevRightControllerPosition = _rightControllerPosition;
            }
            
            Log.Info($"为玩家实体 {EntityId} 设置VR控制器引用");
        }
        
        /// <summary>
        /// 受到伤害时考虑格挡减伤
        /// </summary>
        public override void TakeDamage(float damage, CombatEntityBase attacker, bool isCritical = false)
        {
            if (!_isActive || !IsAlive) return;
            
            // 如果处于格挡状态，减少伤害
            if (_isBlocking)
            {
                damage *= (1 - _blockDamageReduction);
                Log.Info($"玩家 {EntityId} 成功格挡，伤害减少 {_blockDamageReduction * 100}%");
                
                // 触发格挡反馈（如震动等）
                TriggerHapticFeedback(0.6f, 0.3f);
            }
            
            // 调用基类方法处理伤害
            base.TakeDamage(damage, attacker, isCritical);
        }
        
        /// <summary>
        /// 玩家死亡处理
        /// </summary>
        protected override void OnDeath(CombatEntityBase killer)
        {
            base.OnDeath(killer);
            
            // 玩家死亡可能需要特殊处理，例如显示死亡界面、触发重生机制等
            Log.Warning($"玩家 {EntityId} 死亡！");
            
            // 触发长时间强烈震动反馈
            TriggerHapticFeedback(1.0f, 0.7f);
            
            // 通知游戏模块玩家死亡
            GameModule.Combat.OnPlayerDeath(EntityId);
        }
        #region VR输入事件处理
        
        /// <summary>
        /// 更新手势冷却时间
        /// </summary>
        private void UpdateGestureCooldowns(float deltaTime)
        {
            // 更新左手挥动冷却
            if (_leftSwingCooldownTimer > 0)
            {
                _leftSwingCooldownTimer -= deltaTime;
                if (_leftSwingCooldownTimer <= 0)
                {
                    _leftSwingCooldownTimer = 0;
                    _isLeftSwinging = false;
                }
            }
            
            // 更新右手挥动冷却
            if (_rightSwingCooldownTimer > 0)
            {
                _rightSwingCooldownTimer -= deltaTime;
                if (_rightSwingCooldownTimer <= 0)
                {
                    _rightSwingCooldownTimer = 0;
                    _isRightSwinging = false;
                }
            }
        }
        
        /// <summary>
        /// 检测手势动作
        /// </summary>
        private void DetectGestures()
        {
            // 检测左手挥动手势
            if (!_isLeftSwinging && _leftControllerVelocity.magnitude > _swingVelocityThreshold)
            {
                _isLeftSwinging = true;
                _leftSwingCooldownTimer = _swingCooldown;
                OnLeftSwingDetected();
            }
            
            // 检测右手挥动手势
            if (!_isRightSwinging && _rightControllerVelocity.magnitude > _swingVelocityThreshold)
            {
                _isRightSwinging = true;
                _rightSwingCooldownTimer = _swingCooldown;
                OnRightSwingDetected();
            }
        }
        
        /// <summary>
        /// 左手挥动手势检测到
        /// </summary>
        private void OnLeftSwingDetected()
        {
            Log.Info($"玩家 {EntityId} 左手挥动手势检测到");
            
            // 执行基础攻击技能
            if (IsInCombat)
            {
                // 获取最近的敌人作为目标
                CombatEntityBase target = GameModule.Combat.EntityManager.FindNearestEnemy(this, 5f);
                if (target != null)
                {
                    UseSkill("basic_attack", target.EntityId).Forget();
                    TriggerHapticFeedback(0.4f, 0.1f, true);
                }
            }
        }
        
        /// <summary>
        /// 右手挥动手势检测到
        /// </summary>
        private void OnRightSwingDetected()
        {
            Log.Info($"玩家 {EntityId} 右手挥动手势检测到");
            
            // 执行特殊技能或打开技能轮盘等
            if (IsInCombat)
            {
                // 示例：打开技能轮盘UI
                GameModule.Combat.InteractionHandler.ShowSkillWheel();
                TriggerHapticFeedback(0.2f, 0.1f, false);
            }
        }
        
        /// <summary>
        /// 左手扳机按下
        /// </summary>
        private void OnLeftTriggerPressed()
        {
            Log.Info($"玩家 {EntityId} 左手扳机按下");
            
            // 示例：选择目标或执行特定动作
            GameModule.Combat.InteractionHandler.SelectTarget(_leftControllerPosition, _leftControllerRotation);
            TriggerHapticFeedback(0.2f, 0.05f, true);
        }
        
        /// <summary>
        /// 左手扳机释放
        /// </summary>
        private void OnLeftTriggerReleased()
        {
            Log.Info($"玩家 {EntityId} 左手扳机释放");
        }
        
        /// <summary>
        /// 右手扳机按下
        /// </summary>
        private void OnRightTriggerPressed()
        {
            Log.Info($"玩家 {EntityId} 右手扳机按下");
            
            // 示例：确认技能选择或使用当前选择的技能
            GameModule.Combat.InteractionHandler.ConfirmSkillSelection();
            TriggerHapticFeedback(0.2f, 0.05f, false);
        }
        
        /// <summary>
        /// 右手扳机释放
        /// </summary>
        private void OnRightTriggerReleased()
        {
            Log.Info($"玩家 {EntityId} 右手扳机释放");
        }
        
        /// <summary>
        /// 左手握把按下
        /// </summary>
        private void OnLeftGripPressed()
        {
            Log.Info($"玩家 {EntityId} 左手握把按下");
            
            // 示例：启动格挡姿势辅助
            TriggerHapticFeedback(0.1f, 0.05f, true);
        }
        
        /// <summary>
        /// 左手握把释放
        /// </summary>
        private void OnLeftGripReleased()
        {
            Log.Info($"玩家 {EntityId} 左手握把释放");
        }
        
        /// <summary>
        /// 右手握把按下
        /// </summary>
        private void OnRightGripPressed()
        {
            Log.Info($"玩家 {EntityId} 右手握把按下");
            
            // 示例：启动格挡姿势辅助
            TriggerHapticFeedback(0.1f, 0.05f, false);
        }
        
        /// <summary>
        /// 右手握把释放
        /// </summary>
        private void OnRightGripReleased()
        {
            Log.Info($"玩家 {EntityId} 右手握把释放");
        }
        
        /// <summary>
        /// 左手主按钮按下
        /// </summary>
        private void OnLeftPrimaryButtonPressed()
        {
            Log.Info($"玩家 {EntityId} 左手主按钮按下");
            
            // 示例：使用治疗技能
            UseSkill("basic_heal", EntityId).Forget();
            TriggerHapticFeedback(0.3f, 0.1f, true);
        }
        
        /// <summary>
        /// 右手主按钮按下
        /// </summary>
        private void OnRightPrimaryButtonPressed()
        {
            Log.Info($"玩家 {EntityId} 右手主按钮按下");
            
            // 示例：切换武器或显示背包
            GameModule.Combat.InteractionHandler.ToggleInventory();
            TriggerHapticFeedback(0.3f, 0.1f, false);
        }
        
        /// <summary>
        /// 触发震动反馈
        /// </summary>
        /// <param name="amplitude">震动强度 0-1</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="isLeft">是否左手控制器</param>
        private void TriggerHapticFeedback(float amplitude, float duration, bool isLeft = true)
        {
            if (XRIModule.Instance != null)
            {
                XRNode node = isLeft ? XRNode.LeftHand : XRNode.RightHand;
                XRIModule.Instance.SendHapticImpulse(node, amplitude, duration);
            }
        }
        
        #endregion
    }
}
