using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 🎥 SpectatorManager - จัดการกล้องและ UI สำหรับผู้เล่นที่ตาย
/// </summary>
public class SpectatorManager : MonoBehaviour
{
    [Header("Spectator UI")]
    public GameObject spectatorUI;
    public TextMeshProUGUI spectatorText;
    public TextMeshProUGUI aliveCountText;
    public TextMeshProUGUI currentTargetText;

    [Header("Camera Settings")]
    public float cameraDistance = 15f;
    public float cameraHeight = 10f;
    public float cameraFollowSpeed = 5f;

    private Hero deadHero;
    private Hero currentTarget;
    private Camera spectatorCamera;
    private List<Hero> aliveHeroes = new List<Hero>();
    private int currentTargetIndex = 0;

    private bool isSpectating = false;

    /// <summary>
    /// ✅ เข้า Spectator Mode
    /// </summary>
    public void EnterSpectatorMode(Hero hero)
    {
        Debug.Log($"[SpectatorManager] ========================================");
        Debug.Log($"[SpectatorManager] Entering Spectator Mode");
        Debug.Log($"  - Dead Hero: {hero.CharacterName}");
        Debug.Log($"[SpectatorManager] ========================================");

        deadHero = hero;
        isSpectating = true;

        // Setup camera
        SetupSpectatorCamera();

        // หา heroes ที่ยังมีชีวิตอยู่
        FindAliveHeroes();

        // เลือก target แรก
        if (aliveHeroes.Count > 0)
        {
            currentTarget = aliveHeroes[0];
            currentTargetIndex = 0;
            Debug.Log($"[SpectatorManager] ✅ Spectating: {currentTarget.CharacterName}");
        }
        else
        {
            Debug.LogWarning("[SpectatorManager] ⚠️ No alive heroes to spectate!");
        }

        // Setup UI
        SetupSpectatorUI();

        Debug.Log("[SpectatorManager] ✅ Spectator Mode activated");
    }

    /// <summary>
    /// ✅ Setup กล้อง Spectator
    /// </summary>
    private void SetupSpectatorCamera()
    {
        spectatorCamera = Camera.main;

        if (spectatorCamera == null)
        {
            Debug.LogError("[SpectatorManager] ❌ Main Camera not found!");
            return;
        }

        Debug.Log("[SpectatorManager] Camera setup for spectating");
    }

    /// <summary>
    /// ✅ หา heroes ที่ยังมีชีวิตอยู่
    /// </summary>
    private void FindAliveHeroes()
    {
        aliveHeroes.Clear();

        Hero[] allHeroes = FindObjectsOfType<Hero>();

        foreach (var hero in allHeroes)
        {
            // ✅ เช็คว่ายังมีชีวิต: CurrentHp > 0 หรือ NetworkedCurrentHp > 0
            if (hero != null && hero != deadHero && IsHeroAlive(hero))
            {
                aliveHeroes.Add(hero);
                Debug.Log($"[SpectatorManager] Found alive hero: {hero.CharacterName}");
            }
        }

        Debug.Log($"[SpectatorManager] Total alive heroes: {aliveHeroes.Count}");
    }
    private bool IsHeroAlive(Hero hero)
    {
        if (hero == null) return false;

        // เช็ค HP จาก network property หรือ local property
        int currentHp = hero.NetworkedCurrentHp > 0 ? hero.NetworkedCurrentHp : hero.CurrentHp;

        return currentHp > 0;
    }
    /// <summary>
    /// ✅ Setup Spectator UI
    /// </summary>
    private void SetupSpectatorUI()
    {
        // หา/สร้าง Spectator UI
        if (spectatorUI == null)
        {
            spectatorUI = CreateSpectatorUI();
        }

        spectatorUI.SetActive(true);

        UpdateSpectatorUI();
    }

    /// <summary>
    /// ✅ สร้าง Spectator UI
    /// </summary>
    private GameObject CreateSpectatorUI()
    {
        Debug.Log("[SpectatorManager] Creating Spectator UI...");

        // สร้าง Canvas
        GameObject canvas = new GameObject("SpectatorUI");
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // สร้าง Panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.8f);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        // สร้าง Spectator Text
        GameObject textObj = new GameObject("SpectatorText");
        textObj.transform.SetParent(panel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(800, 100);

        spectatorText = textObj.AddComponent<TextMeshProUGUI>();
        spectatorText.text = "💀 YOU ARE DEAD - SPECTATING";
        spectatorText.fontSize = 36;
        spectatorText.alignment = TextAlignmentOptions.Center;
        spectatorText.color = Color.red;

        // สร้าง Alive Count Text
        GameObject aliveObj = new GameObject("AliveCountText");
        aliveObj.transform.SetParent(panel.transform, false);

        RectTransform aliveRect = aliveObj.AddComponent<RectTransform>();
        aliveRect.anchorMin = new Vector2(0.5f, 0.2f);
        aliveRect.anchorMax = new Vector2(0.5f, 0.2f);
        aliveRect.sizeDelta = new Vector2(400, 50);

        aliveCountText = aliveObj.AddComponent<TextMeshProUGUI>();
        aliveCountText.fontSize = 24;
        aliveCountText.alignment = TextAlignmentOptions.Center;
        aliveCountText.color = Color.white;

        // สร้าง Current Target Text
        GameObject targetObj = new GameObject("CurrentTargetText");
        targetObj.transform.SetParent(panel.transform, false);

        RectTransform targetRect = targetObj.AddComponent<RectTransform>();
        targetRect.anchorMin = new Vector2(0.5f, 0.6f);
        targetRect.anchorMax = new Vector2(0.5f, 0.6f);
        targetRect.sizeDelta = new Vector2(600, 40);

        currentTargetText = targetObj.AddComponent<TextMeshProUGUI>();
        currentTargetText.fontSize = 28;
        currentTargetText.alignment = TextAlignmentOptions.Center;
        currentTargetText.color = Color.yellow;

        Debug.Log("[SpectatorManager] ✅ Spectator UI created");

        return canvas;
    }

    /// <summary>
    /// ✅ อัพเดท Spectator UI
    /// </summary>
    private void UpdateSpectatorUI()
    {
        if (aliveCountText != null)
        {
            aliveCountText.text = $"👥 Alive Players: {aliveHeroes.Count}";
        }

        if (currentTargetText != null && currentTarget != null)
        {
            currentTargetText.text = $"🎥 Spectating: {currentTarget.CharacterName}";
        }
    }

    private void Update()
    {
        if (!isSpectating) return;

        // อัพเดทรายชื่อผู้เล่นที่ยังมีชีวิต
        UpdateAliveHeroesList();

        // ติดตามกล้อง
        FollowTarget();

        // สลับ target (กด Tab)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchTarget();
        }
    }

    /// <summary>
    /// ✅ อัพเดทรายชื่อผู้เล่นที่ยังมีชีวิต
    /// </summary>
    private void UpdateAliveHeroesList()
    {
        // เช็คทุก 1 วินาที
        if (Time.frameCount % 60 != 0) return;

        int previousCount = aliveHeroes.Count;
        FindAliveHeroes();

        if (aliveHeroes.Count != previousCount)
        {
            Debug.Log($"[SpectatorManager] Alive heroes updated: {aliveHeroes.Count}");
            UpdateSpectatorUI();

            // ถ้า target ปัจจุบันตายไปแล้ว
            if (currentTarget != null && !IsHeroAlive(currentTarget))
            {
                SwitchTarget();
            }
        }

        // ✅ ถ้าไม่มีคนเหลือเลย - รอ GameStateManager จัดการ
        if (aliveHeroes.Count == 0)
        {
            Debug.Log("[SpectatorManager] ⚠️ No alive heroes left!");
        }
    }
    /// <summary>
    /// ✅ ติดตามกล้อง
    /// </summary>
    private void FollowTarget()
    {
        if (currentTarget == null || spectatorCamera == null) return;

        Vector3 targetPosition = currentTarget.transform.position;
        Vector3 offset = new Vector3(0, cameraHeight, -cameraDistance);

        Vector3 desiredPosition = targetPosition + offset;

        spectatorCamera.transform.position = Vector3.Lerp(
            spectatorCamera.transform.position,
            desiredPosition,
            Time.deltaTime * cameraFollowSpeed
        );

        spectatorCamera.transform.LookAt(targetPosition + Vector3.up * 1.5f);
    }

    /// <summary>
    /// ✅ สลับ target
    /// </summary>
    private void SwitchTarget()
    {
        if (aliveHeroes.Count == 0) return;

        currentTargetIndex = (currentTargetIndex + 1) % aliveHeroes.Count;
        currentTarget = aliveHeroes[currentTargetIndex];

        Debug.Log($"[SpectatorManager] 🔄 Switched to: {currentTarget.CharacterName}");
        UpdateSpectatorUI();
    }

    /// <summary>
    /// ✅ ออกจาก Spectator Mode
    /// </summary>
    public void ExitSpectatorMode()
    {
        isSpectating = false;

        if (spectatorUI != null)
        {
            spectatorUI.SetActive(false);
        }

        Debug.Log("[SpectatorManager] ✅ Exited Spectator Mode");
    }
}