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

    void Start()
    {
        // ✅ แน่ใจว่า Time.timeScale เป็น 1
        Time.timeScale = 1f;

        BackToLobby.onClick.AddListener(BackToLobbys);
        AudioManager.instance.PlaySFX(29, 1f);

        Debug.Log("[LoseScene] Scene loaded - Time.timeScale = " + Time.timeScale);
    }

    void BackToLobbys()
    {
        Debug.Log("[LoseScene] ========================================");
        Debug.Log("[LoseScene] Back to Lobby button clicked!");
        Debug.Log("[LoseScene] ========================================");

        StartCoroutine(BackToLobbyWithCleanup());
    }

    private IEnumerator BackToLobbyWithCleanup()
    {
        // ✅ 1. Cleanup network components อย่างถูกต้อง
        CleanupNetworkComponents();

        // ✅ 2. รอ 1 frame ให้ cleanup เสร็จ
        yield return null;

        // ✅ 3. แน่ใจว่า timeScale เป็น 1
        Time.timeScale = 1f;

        // ✅ 4. โหลด Lobby scene
        Debug.Log("[LoseScene] Loading Lobby scene...");
        SceneManager.LoadScene("Lobby");
    }

    private void CleanupNetworkComponents()
    {
        Debug.Log("========================================");
        Debug.Log("[LoseScene] 🧹 Starting network cleanup...");
        Debug.Log("========================================");

        // ✅ 1. Cleanup PlayerSpawner ก่อน
        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            Debug.Log("[LoseScene] Calling PlayerSpawner.CleanupOnGameExit()");
            spawner.CleanupOnGameExit();
        }

        // ✅ 2. Shutdown และ DESTROY NetworkRunner
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("[LoseScene] Found NetworkRunner - shutting down...");

            // Remove all callbacks ก่อน shutdown
            var allCallbacks = FindObjectsOfType<MonoBehaviour>();
            foreach (var obj in allCallbacks)
            {
                if (obj is INetworkRunnerCallbacks)
                {
                    runner.RemoveCallbacks(obj as INetworkRunnerCallbacks);
                    Debug.Log($"[LoseScene] Removed callback: {obj.GetType().Name}");
                }
            }

            // Shutdown
            if (runner.IsRunning)
            {
                runner.Shutdown();
                Debug.Log("[LoseScene] NetworkRunner shutdown complete");
            }

            // ✅ CRITICAL: ทำลาย GameObject ด้วย
            Destroy(runner.gameObject);
            Debug.Log("[LoseScene] ✅ NetworkRunner GameObject DESTROYED");
        }
        else
        {
            Debug.Log("[LoseScene] No NetworkRunner found (already cleaned?)");
        }

        // ✅ 3. ทำลาย SingleInputController
        SingleInputController inputController = FindObjectOfType<SingleInputController>();
        if (inputController != null)
        {
            Debug.Log("[LoseScene] Destroying SingleInputController");
            Destroy(inputController.gameObject);
        }

        // ✅ 4. ลบ NetworkObjects ทั้งหมด
        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        Debug.Log($"[LoseScene] Found {networkObjects.Length} NetworkObjects to destroy");

        foreach (var obj in networkObjects)
        {
            if (obj != null && obj.gameObject != null)
            {
                Debug.Log($"[LoseScene] Destroying NetworkObject: {obj.gameObject.name}");
                Destroy(obj.gameObject);
            }
        }

        // ✅ 5. ลบ CombatUIManager (ถ้ามี DontDestroyOnLoad)
        CombatUIManager combatUI = FindObjectOfType<CombatUIManager>();
        if (combatUI != null)
        {
            Debug.Log("[LoseScene] Destroying CombatUIManager");
            Destroy(combatUI.gameObject);
        }

        // ✅ 6. ลบ NetworkRunnerHandler (ถ้ามี)
        NetworkRunnerHandler runnerHandler = FindObjectOfType<NetworkRunnerHandler>();
        if (runnerHandler != null)
        {
            Debug.Log("[LoseScene] Destroying NetworkRunnerHandler");
            Destroy(runnerHandler.gameObject);
        }

        // ✅ 7. Reset Time Scale ให้แน่ใจ
        Time.timeScale = 1f;

        Debug.Log("========================================");
        Debug.Log("[LoseScene] ✅ Network cleanup completed!");
        Debug.Log("========================================");
    }

    private void OnDestroy()
    {
        // ✅ แน่ใจว่า timeScale กลับเป็น 1 เมื่อ destroy
        Time.timeScale = 1f;
        Debug.Log("[LoseScene] OnDestroy: Time.timeScale reset to 1");
    }
}