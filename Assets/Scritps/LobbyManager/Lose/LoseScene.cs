using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Fusion;
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
        CleanupNetworkComponents();

        SceneManager.LoadScene("Lobby");
    }
    private void CleanupNetworkComponents()
    {
        // Shutdown NetworkRunner
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("Shutting down NetworkRunner from LoseScene");
            runner.Shutdown();
        }

        // Cleanup PlayerSpawner
        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.CleanupOnGameExit();
        }

        // ลบ NetworkObjects ที่เหลืออยู่
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
