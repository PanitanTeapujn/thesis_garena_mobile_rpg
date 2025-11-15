using Fusion;
using UnityEngine;
using System.Collections;

public class NetworkPlayerManager : NetworkBehaviour
{
    [Networked] public int SelectedCharacterType { get; set; }
    [Networked] public NetworkBool HasSpawnedCharacter { get; set; }

    private static NetworkPlayerManager localInstance;
    private bool hasRequestedSpawn = false;
    private bool isCoroutineRunning = false;
    private bool rpcSent = false;

    public override void Spawned()
    {
        Debug.Log($"[NetworkPlayerManager] ========================================");
        Debug.Log($"[NetworkPlayerManager] Spawned:");
        Debug.Log($"  - HasInputAuthority: {HasInputAuthority}");
        Debug.Log($"  - HasStateAuthority: {HasStateAuthority}");
        Debug.Log($"  - Runner.IsServer: {Runner.IsServer}");
        Debug.Log($"  - Runner.IsClient: {Runner.IsClient}");
        Debug.Log($"  - Object.InputAuthority: {Object.InputAuthority}");
        Debug.Log($"[NetworkPlayerManager] ========================================");

        if (HasInputAuthority)
        {
            localInstance = this;

            // ✅ Client ที่ไม่ใช่ Host - ส่ง RPC ขอให้ spawn
            if (!Runner.IsServer && !isCoroutineRunning && !hasRequestedSpawn)
            {
                Debug.Log("[NetworkPlayerManager] 🌐 Client mode - requesting character spawn");
                isCoroutineRunning = true;
                StartCoroutine(SendCharacterSelectionDelayed());
            }
            // ✅ Host - ไม่ต้องส่ง RPC แต่ต้อง sync character type
            else if (Runner.IsServer)
            {
                Debug.Log("[NetworkPlayerManager] 🏠 Host mode - syncing character type");

                // ✅ Host ต้อง sync character type ของตัวเอง
                string activeCharacter = PersistentPlayerData.Instance?.GetCurrentActiveCharacter() ?? "Assassin";

                if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(activeCharacter, out var selectedCharacter))
                {
                    SelectedCharacterType = (int)selectedCharacter;
                    Debug.Log($"[NetworkPlayerManager] Host character type synced: {selectedCharacter}");
                }

                hasRequestedSpawn = true;
            }
        }
    }

    private IEnumerator SendCharacterSelectionDelayed()
    {
        Debug.Log("[NetworkPlayerManager] ⏳ Waiting for PersistentPlayerData...");

        // ✅ รอให้ PersistentPlayerData พร้อม
        float timeout = 5f;
        float elapsed = 0f;

        while (PersistentPlayerData.Instance == null || !PersistentPlayerData.Instance.isDataLoaded)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;

            if (elapsed >= timeout)
            {
                Debug.LogError("[NetworkPlayerManager] ❌ TIMEOUT waiting for PersistentPlayerData!");
                isCoroutineRunning = false;
                yield break;
            }
        }

        // ✅ Double check ว่ายังไม่เคย request
        if (hasRequestedSpawn || rpcSent)
        {
            Debug.LogWarning("[NetworkPlayerManager] ⚠️ Already requested spawn - aborting");
            isCoroutineRunning = false;
            yield break;
        }

        // ✅ ดึง character type ของ CLIENT นี้
        string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

        if (string.IsNullOrEmpty(activeCharacter))
        {
            Debug.LogError("[NetworkPlayerManager] ❌ No active character found!");
            activeCharacter = "Assassin"; // Fallback
        }

        // ✅ Parse enum
        PlayerSelectionData.CharacterType selectedCharacter;

        if (!System.Enum.TryParse<PlayerSelectionData.CharacterType>(activeCharacter, out selectedCharacter))
        {
            Debug.LogError($"[NetworkPlayerManager] ❌ Failed to parse '{activeCharacter}' - using Assassin");
            selectedCharacter = PlayerSelectionData.CharacterType.Assassin;
        }

        Debug.Log($"[NetworkPlayerManager] 📤 Sending RPC for character: {selectedCharacter}");

        // ✅ ส่ง RPC พร้อม character type ของ CLIENT
        hasRequestedSpawn = true;
        rpcSent = true;
        isCoroutineRunning = false;

        RPC_RequestCharacterSpawn((int)selectedCharacter);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacterSpawn(int characterType)
    {
        Debug.Log($"[NetworkPlayerManager] ========================================");
        Debug.Log($"[RPC] 📨 Received spawn request:");
        Debug.Log($"  - From: {Object.InputAuthority}");
        Debug.Log($"  - Character: {(PlayerSelectionData.CharacterType)characterType}");
        Debug.Log($"  - Already spawned: {HasSpawnedCharacter}");
        Debug.Log($"[NetworkPlayerManager] ========================================");

        // ✅ ป้องกัน duplicate spawn
        if (HasSpawnedCharacter)
        {
            Debug.LogWarning($"[RPC] ⚠️ Player {Object.InputAuthority} already has a character!");
            return;
        }

        // ✅ บันทึก character type ที่ได้รับจาก RPC
        SelectedCharacterType = characterType;
        HasSpawnedCharacter = true;

        // ✅ Find spawner และ spawn ด้วย character type ที่ถูกต้อง
        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();

        if (spawner != null)
        {
            Debug.Log($"[RPC] ✅ Spawning {(PlayerSelectionData.CharacterType)characterType} for {Object.InputAuthority}");

            // ✅ CRITICAL: ส่ง character type ที่ได้จาก RPC ไป
            spawner.SpawnCharacterForPlayer(
                Object.InputAuthority,
                (PlayerSelectionData.CharacterType)characterType
            );
        }
        else
        {
            Debug.LogError("[RPC] ❌ PlayerSpawner not found!");
            HasSpawnedCharacter = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // ✅ ลบการ spawn ซ้ำออก
    }

    public static PlayerSelectionData.CharacterType GetLocalPlayerCharacter()
    {
        if (localInstance != null)
        {
            return (PlayerSelectionData.CharacterType)localInstance.SelectedCharacterType;
        }

        // Fallback
        if (PersistentPlayerData.Instance != null)
        {
            string activeChar = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(activeChar, out var charType))
            {
                return charType;
            }
        }

        return PlayerSelectionData.CharacterType.Assassin;
    }

    // ✅ เพิ่ม method สำหรับดึง character type ของ player อื่น
    public static PlayerSelectionData.CharacterType GetPlayerCharacter(PlayerRef player)
    {
        // หา NetworkPlayerManager ของ player นั้น
        NetworkPlayerManager[] managers = FindObjectsOfType<NetworkPlayerManager>();

        foreach (var manager in managers)
        {
            if (manager.Object != null && manager.Object.InputAuthority == player)
            {
                Debug.Log($"[GetPlayerCharacter] Player {player} has character: {(PlayerSelectionData.CharacterType)manager.SelectedCharacterType}");
                return (PlayerSelectionData.CharacterType)manager.SelectedCharacterType;
            }
        }

        Debug.LogWarning($"[GetPlayerCharacter] No manager found for player {player} - using Assassin");
        return PlayerSelectionData.CharacterType.Assassin;
    }
}