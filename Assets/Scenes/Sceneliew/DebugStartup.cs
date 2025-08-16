using UnityEngine;

public class DebugStartup : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("=== GAME AWAKE START ===");
        Debug.Log("Platform: " + Application.platform);
        Debug.Log("Unity Version: " + Application.unityVersion);
        Debug.Log("Device Model: " + SystemInfo.deviceModel);
        Debug.Log("Graphics Memory: " + SystemInfo.graphicsMemorySize);
        Debug.Log("=== GAME AWAKE END ===");
    }

    void Start()
    {
        Debug.Log("=== GAME START ===");
    }
}