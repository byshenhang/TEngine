    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    namespace MikeNspired.VRConsoleLog
    {
        public class VRConsoleLogger : MonoBehaviour
        {
            [Header("Prefab & UI References")]
            [SerializeField]
            private LogMessage logMessagePrefab;
            [SerializeField] private RectTransform logMessageParent;
            [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;
            [SerializeField] private ScrollRect scrollRect;
            [SerializeField] private CanvasGroup canvasGroup;
            [SerializeField] private Canvas canvas;

            [Header("Configuration")]
            [SerializeField] private bool keyWordFilter;
            [SerializeField] private string[] keyWords;
            [SerializeField] private string[] keyWordsIgnore;
            [SerializeField] private TimeType timeType;
            [SerializeField] private int maxMessageCount = 20;
            [SerializeField] private float fontSize = 36;
            [SerializeField] private float autoScrollAtDistancePercentage = 10;
            [SerializeField] private bool startDisabled = false;

            [Header("Colors")]
            [SerializeField] private Color logColor = Color.white;
            [SerializeField] private Color warningColor = Color.yellow;
            [SerializeField] private Color errorColor = Color.red;
            [SerializeField] private Color backgroundColor1;
            [SerializeField] private Color backgroundColor2;

            [Header("Visibility Controls")]
            [SerializeField] private bool logStackTrace;
            [SerializeField] private bool showLog = true;
            [SerializeField] private bool showWarning = true;
            [SerializeField] private bool showError = true;
            [SerializeField] private bool reverseArrangement = true;

            private int _logCount, _warningCount, _errorCount, _visibleCounter;
            private LogPool _logPool;

            public event UnityAction<LogType, int> OnLogSet;

            public RectTransform LogMessageParent => logMessageParent;
            public LogMessage LogMessagePrefab => logMessagePrefab;
            public int MaxMessageCount => maxMessageCount;
            
            private void OnEnable()
            {
                _logPool = new LogPool(this);
                _logPool.InitializeLogMessagePool(RebuildUI);
                CheckForLogManager();
                Clear();
                if (LogManager.Instance) LogManager.Instance.OnLogAdded += LogAdded;
                ReloadLogs();
                OnReverseArrangementChanged();
                UpdateMessageDisplay();
                UpdateMessages();
                OnFontSizeChanged();
                UpdateLogVisibility();
                UpdateScrollPosition();
                if(startDisabled) ToggleState();
            }

            private static void CheckForLogManager()
            {
                if (LogManager.Instance) return;

                // LogManager doesn't exist, create a new LogManager GameObject
                GameObject logManagerGameObject = new GameObject("LogManager");
                logManagerGameObject.AddComponent<LogManager>();
            }

            private void OnDisable()
            {
                if (LogManager.Instance) LogManager.Instance.OnLogAdded -= LogAdded;
            }

            private void LogAdded(LogData data)
            {
                if (!ShouldLogBeDisplayed(data)) return;
                HandleNewLog(data);
                StartCoroutine(CheckToAutoScroll());
            }
            
            private bool ShouldLogBeDisplayed(LogData data)
            {
                if (!keyWordFilter) return true;

                // First, check if the message should be ignored based on the ignore list.
                if (keyWordsIgnore != null && keyWordsIgnore.Length > 0)
                    foreach (var ignoreKeyword in keyWordsIgnore)
                        if (!string.IsNullOrEmpty(ignoreKeyword) && data.Message.IndexOf(ignoreKeyword, StringComparison.OrdinalIgnoreCase) != -1)
                            return false; // Ignore this message because it contains an ignore keyword.

                // If keywords are not defined or empty, return true since no filtering criteria are set
                if (keyWords == null || keyWords.Length == 0) return true;

                // Next, check if the message contains any of the keywords to include.
                foreach (var keyword in keyWords)
                    if (!string.IsNullOrEmpty(keyword) && data.Message.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) != -1)
                        return true; // Include this message because it contains a keyword.

                return false; // Return false if no keywords are found and it's not ignored.
            }
            
            private void OnReverseArrangementChanged()
            {
                verticalLayoutGroup.reverseArrangement = reverseArrangement;
                UpdateScrollPosition();
            }

            private void UpdateScrollPosition()
            {
                scrollRect.verticalNormalizedPosition = reverseArrangement ? 1f : 0f;
                RebuildUI();
            }

            public void ToggleState()
            {
                // Determine the target alpha based on the current state
                float targetAlpha = canvasGroup.alpha > .5f ? 0 : 1;
                canvas.enabled = Mathf.Approximately(targetAlpha, 1f);
                StopAllCoroutines();
                StartCoroutine(FadeCanvasGroup(canvasGroup, targetAlpha, .15f));
            }

            public void Show()
            {
                canvas.enabled = true;
                StopAllCoroutines();
                StartCoroutine(FadeCanvasGroup(canvasGroup, 1, .15f));
            }

            public void Hide()
            {
                canvas.enabled = false;
                StopAllCoroutines();
                StartCoroutine(FadeCanvasGroup(canvasGroup, 0, .15f));
            }
            
            private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
            {
                var startAlpha = cg.alpha;
                var time = 0f;

                while (time < duration)
                {
                    time += Time.deltaTime;
                    cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                    yield return null;
                }

                //Disable Canvas to prevent clicks or ray blocking when invisible
                cg.alpha = targetAlpha; 
            }
            
            private void OnFontSizeChanged() => SetLogMessagesFontSize(fontSize);
            
            private void ReloadLogs()
            {
                if (LogManager.Instance == null || LogManager.Instance.Logs.Count == 0)
                    return;

                _logPool.InitializeLogMessagePool(RebuildUI);  // Ensure pool is initialized before use

                var start = Mathf.Max(0, LogManager.Instance.Logs.Count - maxMessageCount);

                for (var i = start; i < LogManager.Instance.Logs.Count; i++)
                {
                    if (!ShouldLogBeDisplayed(LogManager.Instance.Logs[i])) return;
                    HandleNewLog(LogManager.Instance.Logs[i]);
                }
            }

            private void HandleNewLog(LogData logData)
            {
                if (_logPool._activeMessageCount >= maxMessageCount)
                {
                    LogMessage oldestMessage = logMessageParent.GetChild(0).GetComponent<LogMessage>();
                    _logPool.ReturnLogMessageToPool(oldestMessage);
                }

                var logMessage = _logPool.GetLogMessageFromPool();

                if (logData.Type is LogType.Exception or LogType.Assert)
                    logData.Type = LogType.Error;

                var isVisible = IsTypeVisible(logData.Type);
                
                logMessage.SetMessageData(logData.Message, logData.StackTrace, logData.Type, logData.GameTime, logData.TimeStamp.ToString("HH:mm:ss"));
                logMessage.SetDisplayData(fontSize, GetColor(logData.Type), GetBackgroundColor(_visibleCounter));
                logMessage.UpdateMessage(timeType, logStackTrace);
                
                logMessage.gameObject.SetActive(isVisible);  // Set visibility once all conditions checked

                if (isVisible)
                    _visibleCounter++;
                
                IncrementCount(logData.Type);
            }

            public void Clear()
            {
                _logPool._logMessagePool.Clear();
                foreach (Transform child in logMessageParent)
                {
                    var message = child.GetComponent<LogMessage>();
                    _logPool.ReturnLogMessageToPool(message);
                }
                
                // Reset counters and notify any listeners.
                _logCount = 0;
                _warningCount = 0;
                _errorCount = 0;
                _logPool._activeMessageCount = 0;
                _visibleCounter = 0;
                OnLogSet?.Invoke(LogType.Log, 0);
                OnLogSet?.Invoke(LogType.Warning, 0);
                OnLogSet?.Invoke(LogType.Error, 0);
            }

            private IEnumerator CheckToAutoScroll()
            {
                
                // Reset velocity to stop any ongoing scrolling
                scrollRect.velocity = Vector2.zero;

                // Calculate dynamic thresholds as a percentage of the viewport's height
                var dynamicThreshold = scrollRect.viewport.rect.height * autoScrollAtDistancePercentage * 0.01f;

                var contentHeight = scrollRect.content.rect.height;
                var viewportHeight = scrollRect.viewport.rect.height;

                // Calculate positions for bottom and top checks
                var contentBottomToViewportTop = scrollRect.content.anchoredPosition.y - contentHeight;
                var contentTopToViewportTop = scrollRect.content.anchoredPosition.y;
                var viewportBottomEdge = -viewportHeight;
                var viewportTopEdge = 0f; // Top edge is always 0 since anchoredPosition.y is measured from the top

                yield return RebuildUICoroutine();

                // When not in reverse arrangement, auto-scroll down to the bottom if adding content near the bottom
                if (!reverseArrangement && contentBottomToViewportTop > viewportBottomEdge - dynamicThreshold)
                    scrollRect.verticalNormalizedPosition = 0f; // Scroll to bottom

                // When in reverse arrangement, auto-scroll up to the top if adding content near the top
                if (reverseArrangement && contentTopToViewportTop < viewportTopEdge + dynamicThreshold)
                    scrollRect.verticalNormalizedPosition = 1f; // Scroll to top
            }

            private void RebuildUI() => StartCoroutine(RebuildUICoroutine());

            private IEnumerator RebuildUICoroutine()
            {
                yield return null;  // Waits until the next frame
                LayoutRebuilder.ForceRebuildLayoutImmediate(logMessageParent);
            }
            
            private Color GetBackgroundColor(int counter) => counter % 2 == 0 ? backgroundColor1 : backgroundColor2;

            private void IncrementCount(LogType type)
            {
                if (type is LogType.Log)
                {
                    _logCount = Mathf.Min(999, _logCount + 1);
                    OnLogSet?.Invoke(LogType.Log, _logCount);
                }
                else if (type is LogType.Warning)
                {
                    _warningCount = Mathf.Min(999, _warningCount + 1);
                    OnLogSet?.Invoke(LogType.Warning, _warningCount);
                }
                else if (type is LogType.Error)
                {
                    _errorCount = Mathf.Min(999, _errorCount + 1);
                    OnLogSet?.Invoke(LogType.Error, _errorCount);
                }
            }
            
            public void ToggleShowMessageType(LogType messageType)
            {
                switch (messageType)
                {
                    case LogType.Log:
                        showLog = !showLog;
                        break;
                    case LogType.Warning:
                        showWarning = !showWarning;
                        break;
                    case LogType.Error or LogType.Exception or LogType.Assert:
                        showError = !showError;
                        break;
                }

                UpdateLogVisibility();
            }

            public bool GetShowMessageType(LogType messageType)
            {
                return messageType switch
                {
                    LogType.Log => showLog,
                    LogType.Warning => showWarning,
                    LogType.Error or LogType.Exception or LogType.Assert => showError,
                    _ => false
                };
            }

            private void UpdateMessages()
            {
                foreach (Transform child in logMessageParent)
                    child.GetComponent<LogMessage>().UpdateMessage(timeType, logStackTrace);
                RebuildUI();
            }

            private void UpdateMessageDisplay()
            {
                _visibleCounter = 0;
                foreach (Transform child in logMessageParent)
                {
                    child.TryGetComponent(out LogMessage logMessage);
                    logMessage.SetDisplayData(fontSize, GetColor(logMessage.LogType), GetBackgroundColor(_visibleCounter++));
                }
            }
            
            private void UpdateLogVisibility()
            {
                _visibleCounter = 0;
                var visibleCount = 0;
                foreach (Transform child in logMessageParent)
                {
                    child.TryGetComponent(out LogMessage logMessage);
                    var childLogType = logMessage.LogType;
                    var isVisible = IsTypeVisible(childLogType) && logMessage.isActive;
                    child.gameObject.SetActive(isVisible);

                    if (!isVisible) continue;

                    logMessage.SetDisplayData(fontSize, GetColor(logMessage.LogType), GetBackgroundColor(visibleCount++));
                    _visibleCounter++;
                }

                RebuildUI();
            }

            public bool IsTypeVisible(LogType type)
            {
                return type switch
                {
                    LogType.Log => showLog,
                    LogType.Warning => showWarning,
                    LogType.Error or LogType.Exception or LogType.Assert => showError,
                    _ => false
                };
            }

            private Color GetColor(LogType type)
            {
                return type switch
                {
                    LogType.Error or LogType.Exception or LogType.Assert => errorColor,
                    LogType.Warning => warningColor,
                    LogType.Log => logColor,
                    _ => logColor
                };
            }

            private void SetLogMessagesFontSize(float newSize)
            {
                foreach (Transform child in logMessageParent)
                {
                    var logMessage = child.GetComponent<LogMessage>();
                    if (logMessage != null && logMessage.Text != null) logMessage.Text.fontSize = newSize;
                }
            }
            
            public void AdjustPoolSizeProxy() => _logPool.AdjustPoolSize();

            public enum TimeType
            {
                SystemTime,
                GameTime,
                NoTime
            }
        }
    }
    