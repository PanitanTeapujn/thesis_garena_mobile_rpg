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

    [Header("💎 Currency Display")]
    public TextMeshProUGUI currentGoldText;    // แสดงเงินปัจจุบัน
    public TextMeshProUGUI currentGemsText;    // แสดงเพชรปัจจุบัน
    // Start is called before the first frame update
    void Start()
    {
        closeEnchanceLobby.onClick.AddListener(CloseEnchanceLobby);
        upgradeStatButton.onClick.AddListener(OpenUpgradeStatLobby);
        UpdateCurrencyDisplay();
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
    public void UpdateCurrencyDisplay()
    {
        try
        {
            var currencyManager = CurrencyManager.FindCurrencyManager();

            if (currencyManager != null)
            {
                if (currentGoldText != null)
                {
                    currentGoldText.text = $"{currencyManager.GetCurrentGold():N0}";
                }

                if (currentGemsText != null)
                {
                    currentGemsText.text = $"{currencyManager.GetCurrentGems():N0}";
                }
            }
            else
            {
                if (currentGoldText != null)
                {
                    currentGoldText.text = "";
                }

                if (currentGemsText != null)
                {
                    currentGemsText.text = "";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error updating currency display: {e.Message}");
        }
    }
}
