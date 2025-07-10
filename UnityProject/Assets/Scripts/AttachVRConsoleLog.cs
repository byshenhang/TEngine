using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// VR控制台附加组件，用于在VR环境中显示和控制控制台界面
/// VR Console attachment component for displaying and controlling console interface in VR environment
/// </summary>
public class AttachVRConsoleLog : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup.alpha = 0;
    }

    void Update()
    {
        var leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        leftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButtonPressed);
        if (Input.GetKeyDown(KeyCode.F1) || triggerButtonPressed)
        {
            canvasGroup.alpha = canvasGroup.alpha == 1 ? 0 : 1;
        }
    }
}