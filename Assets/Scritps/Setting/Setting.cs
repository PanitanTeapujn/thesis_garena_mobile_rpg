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
    

    void Start()
    {
        SetupButton();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void SetupButton()
    {
        settingButton.onClick.AddListener(OpenSettingPanel);

        closeButton.onClick.AddListener(CloseSettingPanel);
        resumeButton.onClick.AddListener(CloseSettingPanel);

    }
    private void OpenSettingPanel()
    {
        settingPanel.SetActive(true);
        Time.timeScale = 0;
    }
    private void CloseSettingPanel()
    {
        settingPanel.SetActive(false);
        Time.timeScale = 1;
    }

}
