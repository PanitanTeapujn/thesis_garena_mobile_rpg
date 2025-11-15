using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// 🎮 GameStateManager - จัดการสถานะเกม (ชนะ/แพ้)
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private bool gameEnded = false;
    [SerializeField] private int deadPlayerCount = 0;
    [SerializeField] private int totalPlayerCount = 0;

    [Header("Win Condition")]
    [SerializeField] private bool hasEnemySpawner = true;

    private NetworkRunner runner;
    private HashSet<PlayerRef> deadPlayers = new HashSet<PlayerRef>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        runner = FindObjectOfType<NetworkRunner>();

        if (runner != null)
        {
            totalPlayerCount = runner.ActivePlayers.Count();
            Debug.Log($"[GameState] Total players: {totalPlayerCount}");
        }

        // ตรวจสอบว่ามี EnemySpawner หรือไม่
        CheckForEnemySpawner();
    }

    /// <summary>
    /// ✅ ตรวจสอบว่ามี EnemySpawner หรือไม่
    /// </summary>
    private void CheckForEnemySpawner()
    {
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        hasEnemySpawner = (spawner != null);

        Debug.Log($"[GameState] Has EnemySpawner: {hasEnemySpawner}");
    }

    /// <summary>
    /// ✅ เรียกเมื่อมีผู้เล่นตาย
    /// </summary>
    public void OnPlayerDied(PlayerRef player)
    {
        if (gameEnded) return;

        Debug.Log($"[GameState] ========================================");
        Debug.Log($"[GameState] Player {player} died");
        Debug.Log($"[GameState] ========================================");

        if (deadPlayers.Contains(player))
        {
            Debug.LogWarning($"[GameState] Player {player} already marked as dead");
            return;
        }

        deadPlayers.Add(player);
        deadPlayerCount++;

        Debug.Log($"[GameState] Dead players: {deadPlayerCount}/{totalPlayerCount}");

        // ตรวจสอบเงื่อนไขแพ้
        CheckLoseCondition();
    }

    /// <summary>
    /// ✅ ตรวจสอบเงื่อนไขแพ้
    /// </summary>
    private void CheckLoseCondition()
    {
        // ✅ ถ้าทุกคนตายหมด → แพ้
        if (deadPlayerCount >= totalPlayerCount)
        {
            Debug.Log("[GameState] 💀 ALL PLAYERS DEAD - GAME OVER!");
            EndGame(false); // แพ้
        }
        else
        {
            Debug.Log($"[GameState] {totalPlayerCount - deadPlayerCount} player(s) still alive");
        }
    }

    /// <summary>
    /// ✅ เรียกเมื่อศัตรูตายหมด (จาก EnemySpawner)
    /// </summary>
    public void OnAllEnemiesDefeated()
    {
        if (gameEnded) return;

        Debug.Log("[GameState] ========================================");
        Debug.Log("[GameState] 🏆 ALL ENEMIES DEFEATED - VICTORY!");
        Debug.Log("[GameState] ========================================");

        EndGame(true); // ชนะ
    }

    /// <summary>
    /// ✅ จบเกม - ชนะหรือแพ้
    /// </summary>
    private void EndGame(bool victory)
    {
        if (gameEnded) return;

        gameEnded = true;

        Debug.Log($"[GameState] ========================================");
        Debug.Log($"[GameState] GAME ENDED - Victory: {victory}");
        Debug.Log($"[GameState] ========================================");

        if (victory)
        {
            // ✅ ชนะ - ทุกคน (รวมผี) ไป StageCompleteUI
            ShowStageComplete();
        }
        else
        {
            // ✅ แพ้ - ทุกคนไป LoseScene
            GoToLoseScene();
        }
    }

    /// <summary>
    /// ✅ แสดง StageCompleteUI (ชนะ)
    /// </summary>
    private void ShowStageComplete()
    {
        Debug.Log("[GameState] 🎉 Showing Stage Complete UI...");

        StageCompleteUI stageCompleteUI = FindObjectOfType<StageCompleteUI>();

        if (stageCompleteUI != null)
        {
            string stageName = SceneManager.GetActiveScene().name;
            stageCompleteUI.ShowStageComplete(stageName);
            Debug.Log("[GameState] ✅ Stage Complete UI shown");
        }
        else
        {
            Debug.LogError("[GameState] ❌ StageCompleteUI not found!");
        }

        // ปิด SpectatorManager (ถ้ามี)
        SpectatorManager spectator = FindObjectOfType<SpectatorManager>();
        if (spectator != null)
        {
            spectator.ExitSpectatorMode();
        }
    }

    /// <summary>
    /// ✅ ไป LoseScene (แพ้)
    /// </summary>
    private void GoToLoseScene()
    {
        Debug.Log("[GameState] 💀 Going to Lose Scene...");

        // ปิด SpectatorManager (ถ้ามี)
        SpectatorManager spectator = FindObjectOfType<SpectatorManager>();
        if (spectator != null)
        {
            spectator.ExitSpectatorMode();
        }

        // โหลด LoseScene
        SceneManager.LoadScene("LoseScene");
    }

    /// <summary>
    /// ✅ Debug - ดูสถานะเกม
    /// </summary>
    [ContextMenu("Debug Game State")]
    public void DebugGameState()
    {
        Debug.Log($"[GameState] ========================================");
        Debug.Log($"[GameState] Game Ended: {gameEnded}");
        Debug.Log($"[GameState] Dead Players: {deadPlayerCount}/{totalPlayerCount}");
        Debug.Log($"[GameState] Has Enemy Spawner: {hasEnemySpawner}");
        Debug.Log($"[GameState] ========================================");
    }
}