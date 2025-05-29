using TEngine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GameLogic
{
    /// <summary>
    /// 示例：实验室场景交互逻辑，使用新的XRIModule直接注册事件处理
    /// </summary>
    public class LabXRIInteractionLogic : XRISceneInteractionLogic
    {
        // 特定场景物体引用
        [SerializeField] private GameObject _labMachine;
        [SerializeField] private GameObject _doorLock;
        [SerializeField] private GameObject _alarmLight;
        
        // 场景状态
        private bool _isExperimentRunning = false;
        private bool _isAlarmActive = false;
        
        protected override void RegisterXRInteractions()
        {
            // 设置初始状态
            if (_doorLock != null) {
                _doorLock.SetActive(true);
            }
            
            if (_alarmLight != null) {
                _alarmLight.SetActive(false);
            }
            
            // 1. 为紧急按钮注册事件处理
            var emergencyButton = GameObject.FindGameObjectWithTag("EmergencyButton")?.GetComponent<XRBaseInteractable>();
            if (emergencyButton != null)
            {
                RegisterInteractable(
                    emergencyButton,
                    selectHandler: OnEmergencyButtonSelected,
                    activateHandler: OnEmergencyButtonActivated
                );
            }
            
            // 2. 为实验按钮注册事件处理
            var experimentButton = GameObject.FindGameObjectWithTag("ExperimentButton")?.GetComponent<XRBaseInteractable>();
            if (experimentButton != null)
            {
                RegisterInteractable(
                    experimentButton,
                    selectHandler: OnExperimentButtonSelected,
                    activateHandler: OnExperimentButtonActivated
                );
            }
            
            // 3. 为门控制拉杆注册事件处理
            var doorLeverObjects = GameObject.FindGameObjectsWithTag("DoorControl");
            foreach (var obj in doorLeverObjects)
            {
                var leverInteractable = obj.GetComponent<XRBaseInteractable>();
                if (leverInteractable != null)
                {
                    RegisterInteractable(
                        leverInteractable,
                        selectHandler: OnDoorLeverSelected,
                        deselectHandler: OnDoorLeverReleased
                    );
                }
            }
            
            // 4. 使用标签批量注册物体
            RegisterInteractablesByTag("DangerousItem", 
                hoverEnterHandler: OnDangerousItemHoverEnter,
                hoverExitHandler: OnDangerousItemHoverExit
            );
            
            Log.Info("实验室场景交互逻辑已注册");
        }
        
        #region 紧急按钮事件处理
        
        private void OnEmergencyButtonSelected(SelectEnterEventArgs args)
        {
            // 当按钮被选中（例如手柄射线选中）
            Log.Info("紧急按钮被选中");
        }
        
        private void OnEmergencyButtonActivated(ActivateEventArgs args)
        {
            // 当按钮被激活（例如扳机按下）
            Log.Info("紧急按钮被按下");
            TriggerEmergencyMode();
        }
        
        #endregion
        
        #region 实验按钮事件处理
        
        private void OnExperimentButtonSelected(SelectEnterEventArgs args)
        {
            // 当按钮被选中
            Log.Info("实验按钮被选中");
        }
        
        private void OnExperimentButtonActivated(ActivateEventArgs args)
        {
            // 当按钮被激活
            Log.Info("实验按钮被按下");
            ToggleExperiment();
        }
        
        #endregion
        
        #region 门控拉杆事件处理
        
        private void OnDoorLeverSelected(SelectEnterEventArgs args)
        {
            // 当拉杆被选中（抓取）
            Log.Info("门控拉杆被抓取");
        }
        
        private void OnDoorLeverReleased(SelectExitEventArgs args)
        {
            // 当拉杆被释放
            // 在实际应用中，您可能需要检查拉杆的位置或旋转来确定值
            // 这里简化为检查y轴位置是否高于阈值
            
            var lever = args.interactableObject.transform;
            float leverValue = Mathf.Clamp01((lever.localPosition.y + 0.5f) / 1.0f); // 假设拉杆在-0.5到0.5范围内移动
            
            Log.Info($"门控拉杆被释放，当前值: {leverValue}");
            
            // 如果拉杆值超过阈值，解锁门
            if (leverValue > 0.9f)
            {
                UnlockDoor();
            }
        }
        
        #endregion
        
        #region 危险物品悬停处理
        
        private void OnDangerousItemHoverEnter(HoverEnterEventArgs args)
        {
            // 当交互器悬停在危险物品上时
            var itemName = args.interactableObject.transform.name;
            Log.Info($"注意：正在接近危险物品 {itemName}");
            
            // 可以在这里添加高亮效果或显示警告UI
        }
        
        private void OnDangerousItemHoverExit(HoverExitEventArgs args)
        {
            // 当交互器离开危险物品时
            Log.Info("已离开危险区域");
            
            // 移除高亮或警告UI
        }
        
        #endregion
        
        #region 场景状态控制方法
        
        private void TriggerEmergencyMode()
        {
            _isAlarmActive = true;
            
            if (_alarmLight != null) {
                _alarmLight.SetActive(true);
            }
            
            // 播放警报音效
            //GameModule.Audio.PlaySound("alarm_sound", true);
            
            // 设置紧急状态下的交互规则
            _sceneManager.SetGroupActive("DangerousItems", false);
            
            Log.Info("实验室紧急模式已启动");
        }
        
        private void ToggleExperiment()
        {
            _isExperimentRunning = !_isExperimentRunning;
            
            if (_isExperimentRunning)
            {
                // 启动实验
                if (_labMachine != null) {
                    var animator = _labMachine.GetComponent<Animator>();
                    if (animator != null) {
                        animator.SetTrigger("StartExperiment");
                    }
                }
                
                // 锁定安全区域的门
                if (_doorLock != null) {
                    _doorLock.SetActive(true);
                }
                
                Log.Info("实验已启动");
            }
            else
            {
                // 停止实验
                if (_labMachine != null) {
                    var animator = _labMachine.GetComponent<Animator>();
                    if (animator != null) {
                        animator.SetTrigger("StopExperiment");
                    }
                }
                
                Log.Info("实验已停止");
            }
        }
        
        private void UnlockDoor()
        {
            if (!_isExperimentRunning || _isAlarmActive)
            {
                // 只在实验未运行或紧急情况下才能解锁
                if (_doorLock != null) {
                    _doorLock.SetActive(false);
                }
                
                // 播放解锁音效
                //GameModule.Audio.PlaySound("door_unlock");
                
                Log.Info("门已解锁");
            }
        }
        
        #endregion
    }
}
