using Fusion;
using UnityEngine;
using System.Collections;

public class NetworkPlayerManager : NetworkBehaviour
{
    [Networked] public int SelectedCharacterType { get; set; }
    [Networked] public NetworkBool HasSpawnedCharacter { get; set; }

    private static NetworkPlayerManager localInstance;
    private bool hasRequestedSpawn = false;
    private int rpcCount = 0;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            localInstance = this;
            Debug.Log($"NetworkPlayerManager spawned for local player. IsServer: {Runner.IsServer}");

            // ถ้าเป็น Client ให้รอ 1 frame แล้วค่อยส่ง RPC
            if (!Runner.IsServer)
            {
                StartCoroutine(SendCharacterSelectionDelayed());
            }
        }
    }

    private IEnumerator SendCharacterSelectionDelayed()
    {
        // รอ 1 frame เพื่อให้ NetworkObject setup เสร็จ
        yield return null;
        if (hasRequestedSpawn)
        {
            Debug.LogWarning("Already requested spawn!");
            yield break; // ใช้ yield break แทน return
        }
        // ส่งข้อมูลตัวละครที่เลือกไปให้ Host
        hasRequestedSpawn = true; // ตั้ง flag ก่อนส่ง

        PlayerSelectionData.CharacterType selectedCharacter = PlayerSelectionData.GetSelectedCharacter();
        Debug.Log($"Client sending character selection to server: {selectedCharacter}");
        RPC_RequestCharacterSpawn((int)selectedCharacter);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestCharacterSpawn(int characterType)
    {
        rpcCount++;
        Debug.Log($"[RPC] Request #{rpcCount} from {Object.InputAuthority}");
        // RPC นี้จะรันบน Server/Host เท่านั้น
        //  Debug.Log($"[SERVER] Received character spawn request from player {Object.InputAuthority}: {(PlayerSelectionData.CharacterType)characterType}");

        // ตรวจสอบว่า spawn แล้วหรือยัง
        if (HasSpawnedCharacter)
        {
            Debug.LogWarning($"[SERVER] Player {Object.InputAuthority} already spawned!");
            return;
        }

        // บันทึกข้อมูลตัวละครที่ player เลือก
        SelectedCharacterType = characterType;
        HasSpawnedCharacter = true;

        // ส่งต่อไปยัง PlayerSpawner เพื่อ spawn ตัวละครที่ถูกต้อง
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
        // Debug: แสดงสถานะ
        if (HasInputAuthority && !hasRequestedSpawn && HasSpawnedCharacter)
        {
            hasRequestedSpawn = true;
            Debug.Log($"[CLIENT] Character spawn confirmed by server");
        }
    }

    public static PlayerSelectionData.CharacterType GetLocalPlayerCharacter()
    {
        if (localInstance != null)
        {
            return (PlayerSelectionData.CharacterType)localInstance.SelectedCharacterType;
        }
        return PlayerSelectionData.GetSelectedCharacter();
    }

    void OnGUI()
    {
        if (HasInputAuthority)
        {
            GUI.Label(new Rect(10, 190, 500, 20), $"[NPM] Selected: {(PlayerSelectionData.CharacterType)SelectedCharacterType}, Spawned: {HasSpawnedCharacter}");
        }
    }
}