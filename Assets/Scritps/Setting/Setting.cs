using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using Fusion;

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
    [SerializeField] private Button backToLobbyButton; // จะเป็น Logout ในหน้า Lobby

    [Header("Sound Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Volume Text (Optional)")]
    [SerializeField] private TextMeshProUGUI bgmVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;

    [Header("Scene Settings")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string startGameSceneName = "StartGame"; // ✅ เพิ่มชื่อ scene

    // ✅ เพิ่มตัวแปร pause state (ไม่ใช้ Time.timeScale)
    public static bool IsPaused { get; private set; } = false;

    void Start()
    {
        SetupButton();
        SetupSliders();
        CheckLobbyButton();
    }

    private void SetupButton()
    {
        settingButton.onClick.AddListener(OpenSettingPanel);
        soundButton.onClick.AddListener(OpenSoundPanel);
        closeButton.onClick.AddListener(CloseSettingPanel);
        closesoundButton.onClick.AddListener(CloseSoundPanel);
        resumeButton.onClick.AddListener(CloseSettingPanel);

        // ✅ ไม่ต้อง AddListener ที่นี่ เพราะจะเพิ่มใน CheckLobbyButton แทน
    }

    // ✅ แก้ไขฟังก์ชันนี้
    private void CheckLobbyButton()
    {
        if (backToLobbyButton != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;

            // ✅ ลบ listener เก่าก่อน (ป้องกันการเรียกซ้ำ)
            backToLobbyButton.onClick.RemoveAllListeners();

            if (currentScene.Equals(lobbySceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                // ✅ ในหน้า Lobby = แสดงปุ่ม Logout
                backToLobbyButton.gameObject.SetActive(true);
                backToLobbyButton.onClick.AddListener(LogoutToStartGame);

                // ✅ เปลี่ยนข้อความปุ่ม (ถ้ามี TextMeshProUGUI)
                TextMeshProUGUI buttonText = backToLobbyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "Logout";
                }

                Debug.Log("🚪 In Lobby scene - Logout button visible");
            }
            else
            {
                // ✅ ในหน้าอื่น = แสดงปุ่ม Back to Lobby
                backToLobbyButton.gameObject.SetActive(true);
                backToLobbyButton.onClick.AddListener(BackToLobbys);

                // ✅ เปลี่ยนข้อความปุ่มกลับ
                TextMeshProUGUI buttonText = backToLobbyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "Back to Lobby";
                }

                Debug.Log($"🎮 In {currentScene} - Back to Lobby button visible");
            }
        }
    }

    private void SetupSliders()
    {
        if (AudioManager.instance == null)
        {
            Debug.LogWarning("⚠️ AudioManager instance not found!");
            return;
        }

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.value = 0.7f;
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = 1f;
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        UpdateVolumeText();
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetBGMVolume(value);
            Debug.Log($"🎵 BGM Volume: {value * 100}%");
        }

        if (bgmVolumeText != null)
        {
            bgmVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(value);
            Debug.Log($"🔊 SFX Volume: {value * 100}%");
        }

        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void UpdateVolumeText()
    {
        if (bgmVolumeText != null && bgmSlider != null)
        {
            bgmVolumeText.text = $"{Mathf.RoundToInt(bgmSlider.value * 100)}%";
        }

        if (sfxVolumeText != null && sfxSlider != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100)}%";
        }
    }

    private void OpenSettingPanel()
    {
        settingPanel.SetActive(true);
        PauseGame();
    }

    private void OpenSoundPanel()
    {
        soundPanel.SetActive(true);
    }

    private void CloseSettingPanel()
    {
        settingPanel.SetActive(false);
        soundPanel.SetActive(false);
        ResumeGame();
    }

    private void CloseSoundPanel()
    {
        soundPanel.SetActive(false);
    }

    // ✅ ฟังก์ชันกลับไป Lobby (ใช้ในหน้าเกม)
    void BackToLobbys()
    {
        ResumeGame();
        CleanupNetworkComponents();
        Debug.Log("🏠 Loading Lobby Scene...");
        SceneManager.LoadScene(lobbySceneName);
    }

    // ✅ ฟังก์ชัน Logout ใหม่ (ใช้ในหน้า Lobby)
    void LogoutToStartGame()
    {
        ResumeGame();
        CleanupNetworkComponents();
        Debug.Log("🚪 Logging out to Start Game...");
        SceneManager.LoadScene(startGameSceneName);
    }

    // ========== ✅ Pause System (ไม่ใช้ Time.timeScale) ==========

    private void PauseGame()
    {
        IsPaused = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("⏸️ Game Paused");
    }

    private void ResumeGame()
    {
        IsPaused = false;
        Debug.Log("▶️ Game Resumed");
    }

    private void CleanupNetworkComponents()
    {
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("🔌 Shutting down NetworkRunner");
            runner.Shutdown();
        }

        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.CleanupOnGameExit();
        }

        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        foreach (var obj in networkObjects)
        {
            if (obj != null)
            {
                Destroy(obj.gameObject);
            }
        }
    }
}