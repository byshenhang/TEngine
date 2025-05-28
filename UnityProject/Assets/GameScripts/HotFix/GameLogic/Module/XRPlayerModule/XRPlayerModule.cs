using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using Unity.XR.CoreUtils;
using UnityEngine;

#if UNITY_EDITOR || ENABLE_XR
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
#endif

namespace GameLogic
{
    /// <summary>
    /// XR���ģ�� - ����XR Origin (XR Rig)����ؽ�������
    /// </summary>
    public sealed class XRPlayerModule : Singleton<XRPlayerModule>, IUpdate
    {
        // XR Rig����
        private Transform _xrRig;
        /// <summary> ��ȡXR Rig Transform </summary>
        public Transform XRRig => _xrRig;

#if UNITY_EDITOR || ENABLE_XR
        // XR Origin�������
        private XROrigin _xrOrigin;
        /// <summary> ��ȡXR Origin��� </summary>
        public XROrigin XROrigin => _xrOrigin;

        // XR Camera����
        private Camera _xrCamera;
        /// <summary> ��ȡXR��� </summary>
        public Camera XRCamera => _xrCamera;

        // �ֲ�����������
        private XRController _leftHandController;
        private XRController _rightHandController;
        /// <summary> ���ֿ����� </summary>
        public XRController LeftHandController => _leftHandController;
        /// <summary> ���ֿ����� </summary>
        public XRController RightHandController => _rightHandController;
#endif

        // �����¼��ֵ�
        private Dictionary<XRInteractionEventType, List<Action<object, object>>> _interactionEvents =
            new Dictionary<XRInteractionEventType, List<Action<object, object>>>();

        /// <summary> ģ���ʼ�� </summary>
        protected override void OnInit()
        {
            Log.Info("XRPlayerModule ��ʼ����...");

            // ����XR������
            FindXRComponents();

            // ע��Ӧ���˳��ص�
            Application.quitting += OnApplicationQuit;

            Log.Info("XRPlayerModule ��ʼ�����");
        }

        /// <summary> ���Ҳ�����XR��� </summary>
        private void FindXRComponents()
        {
#if UNITY_EDITOR || ENABLE_XR
            // ����XR Origin
            _xrOrigin = GameObject.FindObjectOfType<XROrigin>();
            if (_xrOrigin != null)
            {
                _xrRig = _xrOrigin.transform;
                _xrCamera = _xrOrigin.Camera;
                Log.Info($"�ҵ� XR Origin: {_xrOrigin.name}");
            }
            else
            {
                Log.Warning("������δ�ҵ� XR Origin");
                // ʹ���������Ϊ����
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _xrRig = mainCamera.transform.parent != null ? mainCamera.transform.parent : mainCamera.transform;
                    _xrCamera = mainCamera;
                    Log.Info("ʹ���������ΪXR Camera");
                }
                else
                {
                    Log.Error("δ�ҵ�����XR����� - ���ش���");
                }
            }

            // ���ҿ�����
            var controllers = GameObject.FindObjectsOfType<XRController>();
            foreach (var controller in controllers)
            {
                if (controller.name.Contains("Left") || controller.controllerNode == XRNode.LeftHand)
                {
                    _leftHandController = controller;
                    Log.Info($"�ҵ����ֿ�����: {controller.name}");
                }
                else if (controller.name.Contains("Right") || controller.controllerNode == XRNode.RightHand)
                {
                    _rightHandController = controller;
                    Log.Info($"�ҵ����ֿ�����: {controller.name}");
                }
            }
#else
            // ��XRģʽ��ʹ�������
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _xrRig = mainCamera.transform.parent != null ? mainCamera.transform.parent : mainCamera.transform;
                Log.Info("��XRģʽ��ʹ�������");
            }
#endif
        }

        /// <summary> ÿ֡���� </summary>
        public void OnUpdate()
        {
#if UNITY_EDITOR || ENABLE_XR
            // ���¿���������
            UpdateControllerInput();
#endif
        }

#if UNITY_EDITOR || ENABLE_XR
        /// <summary> ����XR���������벢���������¼� </summary>
        private void UpdateControllerInput()
        {
            // ������������
            if (_leftHandController != null)
            {
                var device = _leftHandController.inputDevice;
                // �������ť
                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                {
                    TriggerInteractionEvent(XRInteractionEventType.TriggerPressed, _leftHandController, null);
                }
                // ���ץȡ��ť
                if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed) && gripPressed)
                {
                    TriggerInteractionEvent(XRInteractionEventType.GripPressed, _leftHandController, null);
                }
            }
            // �����������룬ͬ��
            if (_rightHandController != null)
            {
                var device = _rightHandController.inputDevice;
                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                {
                    TriggerInteractionEvent(XRInteractionEventType.TriggerPressed, _rightHandController, null);
                }
                if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed) && gripPressed)
                {
                    TriggerInteractionEvent(XRInteractionEventType.GripPressed, _rightHandController, null);
                }
            }
        }
#endif

        /// <summary> ������ҵ�ָ��λ�� </summary>
        /// <param name="position">Ŀ��λ��</param>
        /// <param name="rotation">Ŀ����ת����ѡ��</param>
        public void TeleportTo(Vector3 position, Quaternion? rotation = null)
        {
#if UNITY_EDITOR || ENABLE_XR
            var teleportProvider = GameObject.FindObjectOfType<TeleportationProvider>();
            if (teleportProvider != null && _xrRig != null)
            {
                var rotationValue = rotation.HasValue ? rotation.Value : _xrRig.rotation;
                teleportProvider.QueueTeleportRequest(new TeleportRequest()
                {
                    destinationPosition = position,
                    destinationRotation = rotationValue,
                    matchOrientation = MatchOrientation.TargetUp,
                });
                Log.Info($"�Ѵ�����ҵ�λ��: {position}");
                return;
            }
#endif
            // ��XR��δ�ҵ�TeleportProviderʱֱ���ƶ�
            if (_xrRig != null)
            {
                _xrRig.position = position;
                if (rotation.HasValue)
                    _xrRig.rotation = rotation.Value;
                Log.Info($"ֱ���ƶ���ҵ�λ��: {position}");
            }
        }

        /// <summary> ��ת��� </summary>
        /// <param name="degrees">��ת�Ƕȣ��ȣ�</param>
        public void RotatePlayer(float degrees)
        {
            if (_xrRig != null)
            {
                _xrRig.Rotate(Vector3.up, degrees);
                Log.Info($"�����ת {degrees} ��");
            }
        }

        /// <summary> ע�ύ���¼� </summary>
        /// <param name="eventType">�¼�����</param>
        /// <param name="callback">�ص�����</param>
        public void RegisterInteractionEvent(XRInteractionEventType eventType, Action<object, object> callback)
        {
            if (!_interactionEvents.ContainsKey(eventType))
                _interactionEvents[eventType] = new List<Action<object, object>>();
            _interactionEvents[eventType].Add(callback);
            Log.Info($"��ע�� {eventType} �����¼�");
        }

        /// <summary> ȡ��ע�ύ���¼� </summary>
        /// <param name="eventType">�¼�����</param>
        /// <param name="callback">�ص�����</param>
        public void UnregisterInteractionEvent(XRInteractionEventType eventType, Action<object, object> callback)
        {
            if (_interactionEvents.ContainsKey(eventType))
            {
                _interactionEvents[eventType].Remove(callback);
                Log.Info($"��ȡ�� {eventType} �����¼�");
            }
        }

        /// <summary> ���������¼� </summary>
        private void TriggerInteractionEvent(XRInteractionEventType eventType, object interactor, object interactable)
        {
            if (_interactionEvents.TryGetValue(eventType, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback(interactor, interactable);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"{eventType} �¼��ص�����: {ex.Message}");
                    }
                }
            }
        }

        /// <summary> Ӧ�ó����˳�ʱ������ </summary>
        private void OnApplicationQuit()
        {
            Log.Info("Ӧ���˳�������XRPlayerModule��Դ...");
            _interactionEvents.Clear();
            Application.quitting -= OnApplicationQuit;
            _xrRig = null;
#if UNITY_EDITOR || ENABLE_XR
            _xrOrigin = null;
            _xrCamera = null;
            _leftHandController = null;
            _rightHandController = null;
#endif
            Log.Info("�������");
        }

        /// <summary> ģ���ͷ� </summary>
        protected override void OnRelease()
        {
            _interactionEvents.Clear();
            Application.quitting -= OnApplicationQuit;
            Log.Info("XRPlayerModule ���ͷ�");
        }
    }
}
