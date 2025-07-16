using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion;

public class ShopLooby : MonoBehaviour
{
    public Button closeShop;
    [Header("Panel")]
    public GameObject shopPanel;
    // Start is called before the first frame update
    void Start()
    {
        closeShop.onClick.AddListener(CloseShop);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void CloseShop()
    {
        shopPanel.SetActive(false);
    }

}
