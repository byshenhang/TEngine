using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MikeNspired.VRConsoleLog
{
    public class VRConsoleLoggerButtons : MonoBehaviour
    {
        [SerializeField] private VRConsoleLogger consoleLogger;

        [Header("Log Toggles")] 
        [SerializeField] private VRConsoleLogToggle logToggleComponent;
        [SerializeField] private VRConsoleLogToggle warningToggleComponent;
        [SerializeField] private VRConsoleLogToggle errorToggleComponent;
        [SerializeField] private Button clearButton;

        // Delegate references
        private UnityAction<bool> logToggleAction;
        private UnityAction<bool> warningToggleAction;
        private UnityAction<bool> errorToggleAction;

        private void Awake()
        {
            logToggleComponent.ToggleComponent.isOn = consoleLogger.GetShowMessageType(LogType.Log);
            warningToggleComponent.ToggleComponent.isOn = consoleLogger.GetShowMessageType(LogType.Warning);
            errorToggleComponent.ToggleComponent.isOn = consoleLogger.GetShowMessageType(LogType.Error);
            
            consoleLogger.OnLogSet += HandleLogAdded;
            clearButton.onClick.AddListener(Clear);
            
            // Initialize delegate instances
            logToggleAction = _ => ToggleMessageType(LogType.Log, logToggleComponent);
            warningToggleAction = _ => ToggleMessageType(LogType.Warning, warningToggleComponent);
            errorToggleAction = _ => ToggleMessageType(LogType.Error, errorToggleComponent);

            UpdateLogButtonVisibilityAtStart();
        }

        private void OnEnable()
        {
            logToggleComponent.ToggleComponent.onValueChanged.AddListener(logToggleAction);
            warningToggleComponent.ToggleComponent.onValueChanged.AddListener(warningToggleAction);
            errorToggleComponent.ToggleComponent.onValueChanged.AddListener(errorToggleAction);
        }

        private void OnDisable()
        {
            logToggleComponent.ToggleComponent.onValueChanged.RemoveListener(logToggleAction);
            warningToggleComponent.ToggleComponent.onValueChanged.RemoveListener(warningToggleAction);
            errorToggleComponent.ToggleComponent.onValueChanged.RemoveListener(errorToggleAction);
        }

        private void Clear()
        {
            consoleLogger.Clear();
            LogManager.Instance.Clear();
        }
        
        private void UpdateLogButtonVisibilityAtStart()
        {
            SetStartingColor(LogType.Log, logToggleComponent);
            SetStartingColor(LogType.Warning, warningToggleComponent);
            SetStartingColor(LogType.Error, errorToggleComponent);

            void SetStartingColor(LogType logType, VRConsoleLogToggle vrConsoleLogToggle)
            {
                if (consoleLogger.IsTypeVisible(logType))
                    vrConsoleLogToggle.SetToOriginalColor();
                else
                    vrConsoleLogToggle.SetToDisabledColor();
            }
        }

        private void HandleLogAdded(LogType type, int count)
        {
            switch (type)
            {
                case LogType.Log:
                    logToggleComponent.SetTextValue(count);
                    break;
                case LogType.Warning:
                    warningToggleComponent.SetTextValue(count);
                    break;
                case LogType.Error:
                    errorToggleComponent.SetTextValue(count);
                    break;
            }
        }

        private void ToggleMessageType(LogType logType, VRConsoleLogToggle toggle)
        {
            consoleLogger.ToggleShowMessageType(logType);
            toggle.SetState(consoleLogger.GetShowMessageType(logType));
        }
    }
}
