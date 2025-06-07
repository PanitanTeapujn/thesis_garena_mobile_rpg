using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
public class LoseScene : MonoBehaviour
{
    [Header("Buttons")]
    public Button BackToLobby;
    // Start is called before the first frame update
    void Start()
    {
        BackToLobby.onClick.AddListener(BackToLobbys);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void BackToLobbys()
    {
       
        SceneManager.LoadScene("Lobby");
    }
}
