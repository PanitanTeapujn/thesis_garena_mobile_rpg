using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion;


public class QuestLobby : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    [Header("Button")]

    private GameObject questPanel;
    [SerializeField]
     private Button closeQuestButton;
    void Start()
    {
        closeQuestButton.onClick.AddListener(CloseQuestPanel);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void CloseQuestPanel()
    {
        questPanel.SetActive(false);
    }
}
