using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion;
public class EnchanceLobby : MonoBehaviour
{
    [Header("Button")]
    public Button closeEnchanceLobby;
    public Button upgradeStatButton;
    [Header("Panel")]
    public GameObject enchancePanel;
    public GameObject upgradeStatPanel;
    // Start is called before the first frame update
    void Start()
    {
        closeEnchanceLobby.onClick.AddListener(CloseEnchanceLobby);
        upgradeStatButton.onClick.AddListener(OpenUpgradeStatLobby);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CloseEnchanceLobby()
    {
        enchancePanel.SetActive(false);
        upgradeStatPanel.SetActive(true);

    }
    void OpenUpgradeStatLobby()
    {
        upgradeStatPanel.SetActive(true);
        CloseEnchanceLobby();
    }
}
