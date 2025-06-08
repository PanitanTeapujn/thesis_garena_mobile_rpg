// สร้างไฟล์ใหม่: WorldSpaceUI.cs
using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider healthBar;
    public Slider manaBar;
    public Text playerNameText;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, 0);

    private Hero targetHero;
    private Camera mainCamera;
    private bool isInitialized = false;

    public void Initialize(Hero hero)
    {
        targetHero = hero;
        mainCamera = Camera.main;

        if (playerNameText != null)
        {
            playerNameText.text = hero.CharacterName;
        }

        isInitialized = true;
        Debug.Log($"WorldSpaceUI initialized for {hero.CharacterName}");

        // อัปเดต UI ทันทีหลังจาก initialize
        UpdateBars();
    }

    private void Update()
    {
        if (!isInitialized || targetHero == null || mainCamera == null)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            return;
        }

        // ตรวจสอบว่า Hero ยังมีอยู่
        if (!targetHero.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        // Follow target position
        transform.position = targetHero.transform.position + offset;

        // Face camera
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                            mainCamera.transform.rotation * Vector3.up);
        }

        // Update bars
        UpdateBars();
    }



    private void UpdateBars()
    {
        if (targetHero == null) return;

        // รอให้ network state พร้อม
        if (!targetHero.IsNetworkStateReady) return;

        if (healthBar != null && targetHero.NetworkedMaxHp > 0)
        {
            float healthPercent = (float)targetHero.NetworkedCurrentHp / targetHero.NetworkedMaxHp;
            healthBar.value = Mathf.Clamp01(healthPercent);
        }

        if (manaBar != null && targetHero.NetworkedMaxMana > 0)
        {
            float manaPercent = (float)targetHero.NetworkedCurrentMana / targetHero.NetworkedMaxMana;
            manaBar.value = Mathf.Clamp01(manaPercent);
        }
    }
}