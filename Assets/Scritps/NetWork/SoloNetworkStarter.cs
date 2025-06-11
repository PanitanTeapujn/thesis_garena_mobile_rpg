using UnityEngine;
using Fusion;

// ✅ เพิ่ม Script นี้ใน PlayRoom1 เพื่อจัดการ Solo mode
public class SoloNetworkStarter : MonoBehaviour
{
    private async void Start()
    {
        // เช็คว่าเป็น Solo mode หรือไม่
        string gameMode = PlayerPrefs.GetString("GameMode", "");

        if (gameMode != "Solo")
        {
            Debug.Log("Not Solo mode, skipping SoloNetworkStarter");
            Destroy(gameObject);
            return;
        }

        Debug.Log("Solo mode detected - Setting up isolated network");

        // หา NetworkRunner ที่มีอยู่แล้ว
        NetworkRunner existingRunner = FindObjectOfType<NetworkRunner>();

        if (existingRunner != null)
        {
            Debug.Log("Found existing NetworkRunner - shutting down");
            existingRunner.Shutdown();
            Destroy(existingRunner.gameObject);

            // รอให้ shutdown เสร็จ
            await System.Threading.Tasks.Task.Delay(500);
        }

        // สร้าง NetworkRunner ใหม่สำหรับ Solo
        GameObject runnerGO = new GameObject("SoloNetworkRunner");
        NetworkRunner runner = runnerGO.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        // ใช้ RoomCode ที่สร้างใน LobbyManager
        string sessionName = PlayerPrefs.GetString("RoomCode", "DefaultSolo");

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host, // Solo เป็น Host เสมอ
            SessionName = sessionName,
            SceneManager = runnerGO.AddComponent<NetworkSceneManagerDefault>()
        };

        Debug.Log($"Starting Solo game with session: {sessionName}");

        try
        {
            await runner.StartGame(startGameArgs);
            Debug.Log($"Solo network started successfully! Session: {sessionName}");

            // ลงทะเบียน callbacks
            PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
            if (spawner != null)
            {
                runner.AddCallbacks(spawner);
                Debug.Log("PlayerSpawner callbacks registered");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error starting Solo network: {e.Message}");
        }
    }
}