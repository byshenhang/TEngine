using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MikeNspired.VRConsoleLog
{
    public class VRConsoleLogToggle : MonoBehaviour
    {
        [SerializeField] private Toggle toggleComponent;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image background, icon;

        [Header("Disabled Colors")] [SerializeField]
        private Color disabledTextColor, disabledBackgroundColor, disabledIconColor;

        private Color _originalTextColor, _originalBackgroundColor, _originalIconColor;
        public Toggle ToggleComponent => toggleComponent;
        private bool isInitialized;

        private void Initialize()
        {
            if (isInitialized) return;

            _originalTextColor = countText.color;
            _originalBackgroundColor = background.color;
            _originalIconColor = icon.color;
            isInitialized = true;
        }

        public void SetTextValue(int count)
        {
            if (countText != null) countText.text = count.ToString();
        }

        public void SetToOriginalColor()
        {
            if (!isInitialized) Initialize();

            SetTextColor(_originalTextColor);
            SetBackgroundColor(_originalBackgroundColor);
            SetIconColor(_originalIconColor);
        }

        public void SetToDisabledColor()
        {
            if (!isInitialized) Initialize();

            SetTextColor(disabledTextColor);
            SetBackgroundColor(disabledBackgroundColor);
            SetIconColor(disabledIconColor);
        }

        private void SetTextColor(Color color)
        {
            if (countText != null) countText.color = color;
        }

        private void SetBackgroundColor(Color color)
        {
            if (background != null) background.color = color;
        }

        private void SetIconColor(Color color)
        {
            if (icon != null) icon.color = color;
        }

        public void SetState(bool state)
        {
            if (!isInitialized) Initialize();
            if (state)
                SetToOriginalColor();
            else
                SetToDisabledColor();
        }
    }
}