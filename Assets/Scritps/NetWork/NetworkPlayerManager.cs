using Fusion;
using UnityEngine;
using System.Collections;

public class NetworkPlayerManager : NetworkBehaviour
{
    [Networked] public int SelectedCharacterType { get; set; }
    [Networked] public NetworkBool HasSpawnedCharacter { get; set; }

    private static NetworkPlayerManager localInstance;
    private bool hasRequestedSpawn = false;
    private bool isCoroutineRunning = false; // ✅ เพิ่มตัวนี้

    private int rpcCount = 0;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            localInstance = this;

            // ✅ เช็คว่า coroutine รันแล้วหรือยัง
            if (!Runner.IsServer && !isCoroutineRunning)
            {
                isCoroutineRunning = true;
                StartCoroutine(SendCharacterSelectionDelayed());
            }
        }
    }

    private IEnumerator SendCharacterSelectionDelayed()
    {
        yield return null;

        // ✅ Double check
        if (hasRequestedSpawn)
        {
            Debug.LogWarning("Already requested spawn!");
            isCoroutineRunning = false;
            yield break;
        }

        hasRequestedSpawn = true;
        isCoroutineRunning = false; // ✅ Reset flag

        // Uncomment when ready
        // PlayerSelectionData.CharacterType selectedCharacter = PlayerSelectionData.GetSelectedCharacter();
        // RPC_RequestCharacterSpawn((int)selectedCharacter);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacterSpawn(int characterType)
    {
        rpcCount++;

        // ✅ เพิ่มการป้องกันการเรียกซ้ำ
        if (rpcCount > 10)
        {
            Debug.LogError($"⚠️ RPC called too many times! Stopping to prevent loop.");
            return;
        }

        Debug.Log($"[RPC] Request #{rpcCount} from {Object.InputAuthority}");

        if (HasSpawnedCharacter)
        {
            Debug.LogWarning($"[SERVER] Player {Object.InputAuthority} already spawned!");
            return;
        }

        SelectedCharacterType = characterType;
        HasSpawnedCharacter = true;

        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.SpawnCharacterForPlayer(Object.InputAuthority, (PlayerSelectionData.CharacterType)characterType);
        }
        else
        {
            Debug.LogError("[SERVER] PlayerSpawner not found!");
        }
    }
    public override void FixedUpdateNetwork()
    {
        // ⚠️ มีโอกาส trigger การ spawn ซ้ำๆ
       
    }
    /* public static PlayerSelectionData.CharacterType GetLocalPlayerCharacter()
     {
         if (localInstance != null)
         {
             return (PlayerSelectionData.CharacterType)localInstance.SelectedCharacterType;
         }
         return PlayerSelectionData.GetSelectedCharacter();
     }*/


}