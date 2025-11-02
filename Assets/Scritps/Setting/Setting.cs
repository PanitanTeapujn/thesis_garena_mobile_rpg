using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class Setting : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject soundPanel;

    [Header("Button")]
    [SerializeField] private Button settingButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button closesoundButton;

    [Header("Sound Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Volume Text (Optional)")]
    [SerializeField] private TextMeshProUGUI bgmVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;

    void Start()
    {
        SetupButton();
        SetupSliders();
    }

    private void SetupButton()
    {
        settingButton.onClick.AddListener(OpenSettingPanel);
        soundButton.onClick.AddListener(OpenSoundPanel);
        closeButton.onClick.AddListener(CloseSettingPanel);
        closesoundButton.onClick.AddListener(CloseSoundPanel);
        resumeButton.onClick.AddListener(CloseSettingPanel);
    }

    private void SetupSliders()
    {
        if (AudioManager.instance == null)
        {
            Debug.LogWarning("⚠️ AudioManager instance not found!");
            return;
        }

        // ตั้งค่า Slider Range (0 ถึง 1)
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.value = 0.7f; // ค่าเริ่มต้น
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = 1f; // ค่าเริ่มต้น
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // อัปเดต Text แสดงค่า Volume (ถ้ามี)
        UpdateVolumeText();
    }

    // ฟังก์ชันเมื่อ BGM Slider เปลี่ยนค่า
    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetBGMVolume(value);
            Debug.Log($"🎵 BGM Volume: {value * 100}%");
        }

        // อัปเดต Text (ถ้ามี)
        if (bgmVolumeText != null)
        {
            bgmVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    // ฟังก์ชันเมื่อ SFX Slider เปลี่ยนค่า
    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(value);
            Debug.Log($"🔊 SFX Volume: {value * 100}%");
        }

        // อัปเดต Text (ถ้ามี)
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void UpdateVolumeText()
    {
        if (bgmVolumeText != null)
        {
            bgmVolumeText.text = $"{Mathf.RoundToInt(bgmSlider.value * 100)}%";
        }

        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100)}%";
        }
    }

    private void OpenSettingPanel()
    {
        settingPanel.SetActive(true);
        Time.timeScale = 0;
    }

    private void OpenSoundPanel()
    {
        soundPanel.SetActive(true);
    }

    private void CloseSettingPanel()
    {
        settingPanel.SetActive(false);
        soundPanel.SetActive(false); // ปิด Sound Panel ด้วย
        Time.timeScale = 1;
    }

    private void CloseSoundPanel()
    {
        soundPanel.SetActive(false);
    }
}