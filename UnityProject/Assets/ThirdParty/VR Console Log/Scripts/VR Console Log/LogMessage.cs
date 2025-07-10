using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MikeNspired.VRConsoleLog
{
    public class LogMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private LogType logType;
        [SerializeField] private string condition, stackTrace, gameTime, systemTime;
        public LogType LogType => logType;
        public TextMeshProUGUI Text => text;
        public Button Button => button;
        public Image BackgroundImage => backgroundImage;
        public bool isActive, isShowingStackTrace;
        
        private void Awake()
        {
            // Set up the click listener for toggling stack trace
            var button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(ToggleStackTrace);
        }

        private void ToggleStackTrace()
        {
            isShowingStackTrace = !isShowingStackTrace;
            UpdateMessage(VRConsoleLogger.TimeType.SystemTime, isShowingStackTrace);
        }
        
        public void SetMessageData(string condition, string stackTrace, LogType logType, string gameTime, string systemTime)
        {
            this.condition = condition;
            this.stackTrace = stackTrace;
            this.logType = logType;
            this.gameTime = gameTime;
            this.systemTime = systemTime;
        }

        public void SetDisplayData(float fontSize, Color fontColor, Color backgroundColor)
        {
            text.fontSize = fontSize;
            text.color = fontColor;
            backgroundImage.color = backgroundColor;
        }

        public void UpdateMessage(VRConsoleLogger.TimeType timeType, bool logStackTrace)
        {
            isShowingStackTrace = logStackTrace;
            text.text = GetTime(timeType) + (!isShowingStackTrace ? condition?.Trim() : condition?.Trim() + " \n" + stackTrace?.Trim());
        }

        private string GetTime(VRConsoleLogger.TimeType timeType)
        {
            return timeType switch
            {
                VRConsoleLogger.TimeType.NoTime => null,
                VRConsoleLogger.TimeType.SystemTime => "[" + systemTime + "] ",
                _ => "[" + gameTime + "] "
            };
        }
    }
}