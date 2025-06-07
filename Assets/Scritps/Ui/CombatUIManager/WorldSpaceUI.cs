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

    public void Initialize(Hero hero)
    {
        targetHero = hero;
        mainCamera = Camera.main;

        if (playerNameText != null)
        {
            playerNameText.text = hero.CharacterName;
        }
    }

    private void Update()
    {
        if (targetHero == null || mainCamera == null) return;

        // Follow target position
        transform.position = targetHero.transform.position + offset;

        // Face camera
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                        mainCamera.transform.rotation * Vector3.up);

        // Update bars
        UpdateBars();
    }

    private void UpdateBars()
    {
        if (healthBar != null)
        {
            float healthPercent = (float)targetHero.CurrentHp / targetHero.MaxHp;
            healthBar.value = healthPercent;
        }

        if (manaBar != null)
        {
            float manaPercent = (float)targetHero.CurrentMana / targetHero.MaxMana;
            manaBar.value = manaPercent;
        }
    }
}