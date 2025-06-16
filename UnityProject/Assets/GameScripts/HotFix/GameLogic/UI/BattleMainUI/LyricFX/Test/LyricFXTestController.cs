using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LyricFX.Module;

namespace LyricFX.Test
{
    /// <summary>
    /// LyricFX框架的测试控制器，提供UI界面用于测试功能
    /// </summary>
    public class LyricFXTestController : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private LyricManager lyricManager;
        [SerializeField] private TextAsset[] testLyricFiles;
        
        [Header("UI控件")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Dropdown lyricFileDropdown;
        [SerializeField] private Slider timeSlider;
        
        private bool _isPlaying = false;
        private float _totalDuration = 60f; // 默认一分钟
        
        private void Start()
        {
            InitializeUI();
        }
        
        private void InitializeUI()
        {
            // 初始化歌词文件下拉菜单
            if (lyricFileDropdown != null && testLyricFiles != null && testLyricFiles.Length > 0)
            {
                lyricFileDropdown.ClearOptions();
                
                var options = new System.Collections.Generic.List<Dropdown.OptionData>();
                
                foreach (var lyricFile in testLyricFiles)
                {
                    options.Add(new Dropdown.OptionData(lyricFile.name));
                }
                
                lyricFileDropdown.AddOptions(options);
                lyricFileDropdown.onValueChanged.AddListener(OnLyricFileSelected);
            }
            
            // 初始化按钮
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayButtonClicked);
                
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
                
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetButtonClicked);
                
            // 初始化时间滑块
            if (timeSlider != null)
            {
                timeSlider.minValue = 0f;
                timeSlider.maxValue = _totalDuration;
                timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            }
        }
        
        private void OnLyricFileSelected(int index)
        {
            if (lyricManager != null && index >= 0 && index < testLyricFiles.Length)
            {
                lyricManager.SetLyricFile(testLyricFiles[index]);
                _isPlaying = false;
            }
        }
        
        private void OnPlayButtonClicked()
        {
            if (!_isPlaying && lyricManager != null)
            {
                _isPlaying = true;
                PlayLyricSequence().Forget();
            }
        }
        
        private void OnPauseButtonClicked()
        {
            _isPlaying = false;
        }
        
        private void OnResetButtonClicked()
        {
            _isPlaying = false;
            
            if (timeSlider != null)
                timeSlider.value = 0f;
                
            if (lyricManager != null)
                lyricManager.SeekTo(0f);
        }
        
        private void OnTimeSliderChanged(float value)
        {
            if (lyricManager != null && !_isPlaying)
            {
                lyricManager.SeekTo(value);
            }
        }
        
        private async UniTaskVoid PlayLyricSequence()
        {
            if (lyricManager != null)
            {
                try
                {
                    await lyricManager.PlaySequence();
                    
                    // 播放结束
                    _isPlaying = false;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"播放歌词序列失败: {ex.Message}");
                    _isPlaying = false;
                }
            }
        }
        
        private void Update()
        {
            // 更新时间滑块
            if (_isPlaying && timeSlider != null)
            {
                timeSlider.value += Time.deltaTime;
                
                if (timeSlider.value >= timeSlider.maxValue)
                {
                    _isPlaying = false;
                }
            }
        }
    }
}
