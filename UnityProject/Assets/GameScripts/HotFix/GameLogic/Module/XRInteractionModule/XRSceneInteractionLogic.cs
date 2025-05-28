using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 自定义场景交互逻辑组件 - 继承此类实现特定场景的交互逻辑
    /// </summary>
    public abstract class XRSceneInteractionLogic : MonoBehaviour
    {
        // 引用场景交互管理器
        protected XRSceneInteractionManager _sceneManager;
        
        protected virtual void Awake()
        {
            // 获取场景交互管理器
            _sceneManager = GetComponent<XRSceneInteractionManager>();
            if (_sceneManager == null)
            {
                _sceneManager = FindObjectOfType<XRSceneInteractionManager>();
            }
            
            if (_sceneManager == null)
            {
                Log.Error("未找到场景交互管理器，请确保场景中有XRSceneInteractionManager组件");
                return;
            }
            
            // 注册自定义交互逻辑
            RegisterInteractionLogic();
        }
        
        /// <summary>
        /// 注册自定义交互逻辑
        /// </summary>
        protected abstract void RegisterInteractionLogic();
    }
    
    /// <summary>
    /// 示例：实验室场景交互逻辑
    /// </summary>
    public class LabSceneInteractionLogic : XRSceneInteractionLogic
    {
        // 特定场景物体引用
        [SerializeField] private GameObject _labMachine;
        [SerializeField] private GameObject _doorLock;
        [SerializeField] private GameObject _alarmLight;
        
        // 场景状态
        private bool _isExperimentRunning = false;
        private bool _isAlarmActive = false;
        
        protected override void RegisterInteractionLogic()
        {
            // 监听特定交互事件
            XRInteractionEventBus.Instance.Subscribe<XRButtonPressEvent>(OnButtonPressed, _sceneManager.SceneId);
            XRInteractionEventBus.Instance.Subscribe<XRLeverPullEvent>(OnLeverPulled, _sceneManager.SceneId);
            
            // 设置初始状态
            if (_doorLock != null) {
                _doorLock.SetActive(true);
            }
            
            if (_alarmLight != null) {
                _alarmLight.SetActive(false);
            }
            
            Log.Info("实验室场景交互逻辑已注册");
        }
        
        private void OnButtonPressed(XRButtonPressEvent evt)
        {
            // 检查是否是特定按钮
            if (evt.Target != null && evt.Target.CompareTag("EmergencyButton"))
            {
                // 触发紧急模式
                TriggerEmergencyMode();
            }
            else if (evt.Target != null && evt.Target.CompareTag("ExperimentButton"))
            {
                // 启动/停止实验
                ToggleExperiment();
            }
        }
        
        private void OnLeverPulled(XRLeverPullEvent evt)
        {
            // 检查是否是门锁控制杆
            if (evt.Target != null && evt.Target.name.Contains("DoorControl"))
            {
                // 根据拉杆值解锁门
                if (evt.PullValue > 0.9f)
                {
                    UnlockDoor();
                }
            }
        }
        
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
        
        private void OnDestroy()
        {
            // 取消订阅事件
            XRInteractionEventBus.Instance.Unsubscribe<XRButtonPressEvent>(OnButtonPressed, _sceneManager.SceneId);
            XRInteractionEventBus.Instance.Unsubscribe<XRLeverPullEvent>(OnLeverPulled, _sceneManager.SceneId);
        }
    }
    
    /// <summary>
    /// 示例：博物馆场景交互逻辑
    /// </summary>
    public class MuseumInteractionLogic : XRSceneInteractionLogic
    {
        // 展品列表
        [SerializeField] private List<MuseumExhibit> _exhibits = new List<MuseumExhibit>();
        
        protected override void RegisterInteractionLogic()
        {
            // 订阅展品交互事件
            XRInteractionEventBus.Instance.Subscribe<XRHoverEnterEvent>(OnExhibitHover, _sceneManager.SceneId);
            XRInteractionEventBus.Instance.Subscribe<XRTouchEvent>(OnExhibitTouch, _sceneManager.SceneId);
            
            // 创建展品交互组
            _sceneManager.CreateInteractionGroup("Exhibits", true);
            
            // 注册展品
            foreach (var exhibit in _exhibits)
            {
                if (exhibit != null)
                {
                    // 假设展品实现了IXRInteractable接口
                    var interactable = exhibit.GetComponent<IXRInteractable>();
                    if (interactable != null)
                    {
                        _sceneManager.RegisterInteractable(interactable);
                    }
                }
            }
            
            Log.Info("u535au7269u9986u573au666fu4ea4u4e92u903bu8f91u5df2u6ce8u518c");
        }
        
        private void OnExhibitHover(XRHoverEnterEvent evt)
        {
            if (evt.Target != null)
            {
                var exhibit = evt.Target.GetComponent<MuseumExhibit>();
                if (exhibit != null)
                {
                    // u663eu793au5c55u54c1u7684u7b80u4ecbu63d0u793a
                    ShowExhibitTooltip(exhibit);
                }
            }
        }
        
        private void OnExhibitTouch(XRTouchEvent evt)
        {
            if (evt.Target != null)
            {
                var exhibit = evt.Target.GetComponent<MuseumExhibit>();
                if (exhibit != null)
                {
                    // u663eu793au5c55u54c1u8be6u7ec6u4fe1u606fu5e76u64adu653eu8bf4u660eu97f3u9891
                    ShowExhibitDetails(exhibit);
                }
            }
        }
        
        private void ShowExhibitTooltip(MuseumExhibit exhibit)
        {
            // u5047u8bbeu6211u4eecu6709u4e00u4e2a3D UIu63d0u793au7cfbu7edf
            if (GameModule.UI3D != null)
            {
                // u8fd9u91ccu7684u5b9eu73b0u4f1au56feu4e8eu60a8u7684UI3Du6a21u5757u7684u5b9eu9645API
                // GameModule.UI3D.ShowTooltip(exhibit.transform.position + Vector3.up * 0.2f, exhibit.Title);
                Log.Info($"u663eu793au5c55u54c1u63d0u793a: {exhibit.Title}");
            }
        }
        
        private void ShowExhibitDetails(MuseumExhibit exhibit)
        {
            // u5728UIu4e2du663eu793au5c55u54c1u4fe1u606f
            // u8fd9u91ccu4f7fu7528u5047u8bbeu7684ExhibitInfoForm
            /* 
            GameModule.UI.OpenUIForm<ExhibitInfoForm>(new ExhibitInfoData { 
                Title = exhibit.Title,
                Description = exhibit.Description,
                Year = exhibit.Year
            });
            */
            
            // u64adu653eu5c55u54c1u97f3u9891
            if (!string.IsNullOrEmpty(exhibit.AudioClipName))
            {
                //GameModule.Audio.PlaySound(exhibit.AudioClipName);
            }
            
            Log.Info($"u663eu793au5c55u54c1u8be6u60c5: {exhibit.Title}");
        }
        
        private void OnDestroy()
        {
            // u53d6u6d88u4e8bu4ef6u8ba2u9605
            XRInteractionEventBus.Instance.Unsubscribe<XRHoverEnterEvent>(OnExhibitHover, _sceneManager.SceneId);
            XRInteractionEventBus.Instance.Unsubscribe<XRTouchEvent>(OnExhibitTouch, _sceneManager.SceneId);
        }
    }
    
    /// <summary>
    /// u5c55u54c1u7ec4u4ef6 - u535au7269u9986u6f14u793au7528
    /// </summary>
    public class MuseumExhibit : MonoBehaviour
    {
        [SerializeField] private string _title = "Unnamed Exhibit";
        [SerializeField] private string _description = "No description available.";
        [SerializeField] private int _year = 2000;
        [SerializeField] private string _audioClipName;
        
        public string Title => _title;
        public string Description => _description;
        public int Year => _year;
        public string AudioClipName => _audioClipName;
    }
}
