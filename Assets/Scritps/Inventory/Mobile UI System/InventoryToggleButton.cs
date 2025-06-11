using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryToggleButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("UI References")]
    public Button toggleButton;
    public GameObject inventoryUI;
    public InventoryUIManager inventoryUIManager;

    [Header("Button Settings")]
    public KeyCode toggleKey = KeyCode.B; // ปุ่ม B
    public KeyCode altToggleKey = KeyCode.I; // ปุ่ม I
    public KeyCode closeKey = KeyCode.Escape; // ปุ่ม ESC

    [Header("Visual Settings")]
    public Sprite openIcon;
    public Sprite closeIcon;
    public Color normalColor = Color.white;
    public Color activeColor = Color.yellow;
    public Color hoverColor = Color.cyan;

    [Header("Animation Settings")]
    public bool useScaleAnimation = true;
    public float scaleAmount = 1.2f;
    public float animationDuration = 0.1f;

    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip hoverSound;
    [Range(0f, 1f)]
    public float audioVolume = 0.7f;

    [Header("Mobile Settings")]
    public bool isDraggable = true;
    public float dragThreshold = 50f;
    public bool savePosition = true;

    [Header("Notification Settings")]
    public GameObject notificationBadge;
    public Text notificationText;

    // Private variables
    private bool isInventoryOpen = false;
    private Image buttonImage;
    private AudioSource audioSource;
    private Vector2 startTouchPosition;
    private bool isDragging = false;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private int newItemCount = 0;
    private bool isHovering = false;

    // Animation
    private Vector3 originalScale;
    private Coroutine currentAnimation;

    // Events
    public System.Action<bool> OnInventoryToggled;
    public System.Action OnButtonPressed;
    public System.Action OnButtonHover;

    #region Unity Lifecycle

    void Start()
    {
        InitializeComponents();
        SetupButton();
        LoadSavedPosition();
        SetInventoryState(false);
        UpdateNotificationBadge();
    }

    void Update()
    {
        HandleKeyboardInput();
        HandleMobileBackButton();
    }

    void OnDestroy()
    {
        SaveCurrentPosition();
        UnsubscribeFromEvents();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && savePosition)
        {
            SaveCurrentPosition();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && savePosition)
        {
            SaveCurrentPosition();
        }
    }

    #endregion

    #region Initialization

    void InitializeComponents()
    {
        // Setup components
        if (toggleButton == null)
            toggleButton = GetComponent<Button>();

        buttonImage = toggleButton?.GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (rectTransform != null)
            originalScale = rectTransform.localScale;

        // Setup audio
        SetupAudio();

        // Find InventoryUIManager if not assigned
        if (inventoryUIManager == null && inventoryUI != null)
        {
            inventoryUIManager = inventoryUI.GetComponent<InventoryUIManager>();
        }

        // Subscribe to inventory events
        SubscribeToEvents();
    }

    void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (openSound != null || closeSound != null || hoverSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = audioVolume;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    void SetupButton()
    {
        // Setup button click
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleInventory);

            // Add hover effects
            var trigger = toggleButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = toggleButton.gameObject.AddComponent<EventTrigger>();
            }

            // Hover enter
            var hoverEnter = new EventTrigger.Entry();
            hoverEnter.eventID = EventTriggerType.PointerEnter;
            hoverEnter.callback.AddListener((data) => { OnButtonHoverEnter(); });
            trigger.triggers.Add(hoverEnter);

            // Hover exit
            var hoverExit = new EventTrigger.Entry();
            hoverExit.eventID = EventTriggerType.PointerExit;
            hoverExit.callback.AddListener((data) => { OnButtonHoverExit(); });
            trigger.triggers.Add(hoverExit);
        }
    }

    void SubscribeToEvents()
    {
        if (inventoryUIManager != null)
        {
            var inventoryManager = inventoryUIManager.GetInventoryManager();
            if (inventoryManager != null)
            {
                inventoryManager.OnItemAdded += OnItemAdded;
            }
        }
    }

    void UnsubscribeFromEvents()
    {
        if (inventoryUIManager != null)
        {
            var inventoryManager = inventoryUIManager.GetInventoryManager();
            if (inventoryManager != null)
            {
                inventoryManager.OnItemAdded -= OnItemAdded;
            }
        }
    }

    #endregion

    #region Input Handling

    void HandleKeyboardInput()
    {
        // ตรวจสอบการกดปุ่ม Keyboard
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(altToggleKey))
        {
            ToggleInventory();
        }

        // ปุ่ม ESC ปิด Inventory
        if (Input.GetKeyDown(closeKey) && isInventoryOpen)
        {
            CloseInventory();
        }
    }

    void HandleMobileBackButton()
    {
        // Mobile back button (Android)
        if (Input.GetKeyDown(KeyCode.Escape) && Application.platform == RuntimePlatform.Android)
        {
            if (isInventoryOpen)
            {
                CloseInventory();
            }
        }
    }

    #endregion

    #region Core Functions

    public void ToggleInventory()
    {
        SetInventoryState(!isInventoryOpen);
        OnButtonPressed?.Invoke();
    }

    public void OpenInventory()
    {
        SetInventoryState(true);
    }

    public void CloseInventory()
    {
        SetInventoryState(false);
    }

    public void SetInventoryState(bool isOpen)
    {
        bool wasOpen = isInventoryOpen;
        isInventoryOpen = isOpen;

        // เปิด/ปิด UI
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(isOpen);
        }

        // อัปเดต UI Manager
        if (inventoryUIManager != null)
        {
            if (isOpen)
            {
                inventoryUIManager.RefreshInventoryUI();
            }
        }

        // เปลี่ยนสี/ไอคอนปุ่ม
        UpdateButtonVisual();

        // จัดการ Cursor และ Input
        HandleGameplayInput(isOpen);

        // Play sound และ animation
        PlayToggleSound(isOpen);
        PlayToggleAnimation();

        // Clear notification when opened
        if (isOpen)
        {
            ClearNotification();
        }

        // Fire event
        OnInventoryToggled?.Invoke(isOpen);

        Debug.Log($"Inventory {(isOpen ? "Opened" : "Closed")}");
    }

    #endregion

    #region Visual Updates

    private void UpdateButtonVisual()
    {
        if (buttonImage != null)
        {
            // เปลี่ยนสี
            Color targetColor = isInventoryOpen ? activeColor : normalColor;
            if (isHovering && !isInventoryOpen)
            {
                targetColor = hoverColor;
            }

            buttonImage.color = targetColor;

            // เปลี่ยนไอคอน (ถ้ามี)
            if (openIcon != null && closeIcon != null)
            {
                buttonImage.sprite = isInventoryOpen ? closeIcon : openIcon;
            }
        }
    }

    private void PlayToggleAnimation()
    {
        if (!useScaleAnimation || rectTransform == null) return;

        // Stop current animation
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation = StartCoroutine(ScaleAnimation());
    }

    private System.Collections.IEnumerator ScaleAnimation()
    {
        Vector3 targetScale = originalScale * scaleAmount;

        // Scale up
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            rectTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        rectTransform.localScale = originalScale;
        currentAnimation = null;
    }

    #endregion

    #region Audio

    private void PlayToggleSound(bool isOpen)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = isOpen ? openSound : closeSound;
        if (clipToPlay != null)
        {
            audioSource.volume = audioVolume;
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private void PlayHoverSound()
    {
        if (audioSource != null && hoverSound != null)
        {
            audioSource.volume = audioVolume * 0.5f; // Lower volume for hover
            audioSource.PlayOneShot(hoverSound);
        }
    }

    #endregion

    #region Input Control

    private void HandleGameplayInput(bool isUIOpen)
    {
        // ปิด/เปิด gameplay input เมื่อเปิด UI
        var inputController = FindObjectOfType<SingleInputController>();
        if (inputController != null)
        {
            inputController.enabled = !isUIOpen;
        }

        // จัดการ Cursor (สำหรับ PC)
#if !UNITY_ANDROID && !UNITY_IOS
        if (isUIOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif

        // จัดการ Time Scale (ถ้าต้องการหยุดเกม)
        // Time.timeScale = isUIOpen ? 0f : 1f;
    }

    #endregion

    #region Hover Effects

    private void OnButtonHoverEnter()
    {
        isHovering = true;
        UpdateButtonVisual();
        PlayHoverSound();
        OnButtonHover?.Invoke();
    }

    private void OnButtonHoverExit()
    {
        isHovering = false;
        UpdateButtonVisual();
    }

    #endregion

    #region Mobile Drag Support

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDraggable)
        {
            startTouchPosition = eventData.position;
            isDragging = false;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
        {
            // ถ้าไม่ได้ลาก ให้เปิดปิด Inventory
            ToggleInventory();
        }
        isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        Vector2 currentPosition = eventData.position;
        float distance = Vector2.Distance(startTouchPosition, currentPosition);

        if (distance > dragThreshold)
        {
            isDragging = true;

            // ลากปุ่มตามนิ้ว
            Vector2 localPoint;
            if (parentCanvas != null && rectTransform != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    currentPosition,
                    parentCanvas.worldCamera,
                    out localPoint
                );

                rectTransform.localPosition = localPoint;

                // ตรวจสอบขอบเขตหน้าจอ
                ClampToScreen();
            }
        }
    }

    private void ClampToScreen()
    {
        if (rectTransform == null || parentCanvas == null) return;

        Vector3 pos = rectTransform.localPosition;
        Vector2 canvasSize = (parentCanvas.transform as RectTransform).sizeDelta;
        Vector2 buttonSize = rectTransform.sizeDelta;

        // คำนวณขอบเขต
        float minX = -canvasSize.x / 2 + buttonSize.x / 2;
        float maxX = canvasSize.x / 2 - buttonSize.x / 2;
        float minY = -canvasSize.y / 2 + buttonSize.y / 2;
        float maxY = canvasSize.y / 2 - buttonSize.y / 2;

        // จำกัดตำแหน่ง
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        rectTransform.localPosition = pos;
    }

    #endregion

    #region Position Save/Load

    public void SaveCurrentPosition()
    {
        if (!savePosition || rectTransform == null) return;

        Vector3 pos = rectTransform.localPosition;
        PlayerPrefs.SetFloat("InventoryButton_X", pos.x);
        PlayerPrefs.SetFloat("InventoryButton_Y", pos.y);
        PlayerPrefs.Save();

        Debug.Log($"Inventory button position saved: {pos}");
    }

    public void LoadSavedPosition()
    {
        if (!savePosition || rectTransform == null) return;

        if (PlayerPrefs.HasKey("InventoryButton_X"))
        {
            Vector3 pos = rectTransform.localPosition;
            pos.x = PlayerPrefs.GetFloat("InventoryButton_X", pos.x);
            pos.y = PlayerPrefs.GetFloat("InventoryButton_Y", pos.y);
            rectTransform.localPosition = pos;
            ClampToScreen();

            Debug.Log($"Inventory button position loaded: {pos}");
        }
    }

    public void ResetPosition()
    {
        if (rectTransform != null)
        {
            // Reset to default position (top right)
            rectTransform.anchoredPosition = new Vector2(-80, -80);
            SaveCurrentPosition();
        }
    }

    #endregion

    #region Notification System

    private void OnItemAdded(InventoryItem item)
    {
        if (!isInventoryOpen)
        {
            newItemCount++;
            UpdateNotificationBadge();
        }
    }

    public void AddNotification(int count = 1)
    {
        newItemCount += count;
        UpdateNotificationBadge();
    }

    public void ClearNotification()
    {
        newItemCount = 0;
        UpdateNotificationBadge();
    }

    private void UpdateNotificationBadge()
    {
        if (notificationBadge != null)
        {
            bool hasNewItems = newItemCount > 0;
            notificationBadge.SetActive(hasNewItems);

            if (hasNewItems && notificationText != null)
            {
                notificationText.text = newItemCount > 99 ? "99+" : newItemCount.ToString();
            }
        }
    }

    #endregion

    #region Public Utility Methods

    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    public void SetDraggable(bool draggable)
    {
        isDraggable = draggable;
    }

    public void SetToggleKeys(KeyCode primary, KeyCode secondary, KeyCode close)
    {
        toggleKey = primary;
        altToggleKey = secondary;
        closeKey = close;
    }

    public void SetColors(Color normal, Color active, Color hover)
    {
        normalColor = normal;
        activeColor = active;
        hoverColor = hover;
        UpdateButtonVisual();
    }

    public void SetIcons(Sprite openSprite, Sprite closeSprite)
    {
        openIcon = openSprite;
        closeIcon = closeSprite;
        UpdateButtonVisual();
    }

    public void SetAudioClips(AudioClip open, AudioClip close, AudioClip hover)
    {
        openSound = open;
        closeSound = close;
        hoverSound = hover;
    }

    public void SetVolume(float volume)
    {
        audioVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = audioVolume;
        }
    }

    public void SetAnimationSettings(bool useAnimation, float scale, float duration)
    {
        useScaleAnimation = useAnimation;
        scaleAmount = scale;
        animationDuration = duration;
    }

    public Vector3 GetCurrentPosition()
    {
        return rectTransform != null ? rectTransform.localPosition : Vector3.zero;
    }

    public void SetPosition(Vector3 position)
    {
        if (rectTransform != null)
        {
            rectTransform.localPosition = position;
            ClampToScreen();
        }
    }

    public int GetNotificationCount()
    {
        return newItemCount;
    }

    #endregion

    #region Context Menu Testing

    [ContextMenu("Test Open")]
    void TestOpen()
    {
        OpenInventory();
    }

    [ContextMenu("Test Close")]
    void TestClose()
    {
        CloseInventory();
    }

    [ContextMenu("Test Toggle")]
    void TestToggle()
    {
        ToggleInventory();
    }

    [ContextMenu("Add Notification")]
    void TestAddNotification()
    {
        AddNotification(1);
    }

    [ContextMenu("Clear Notification")]
    void TestClearNotification()
    {
        ClearNotification();
    }

    [ContextMenu("Reset Position")]
    void TestResetPosition()
    {
        ResetPosition();
    }

    [ContextMenu("Save Position")]
    void TestSavePosition()
    {
        SaveCurrentPosition();
    }

    #endregion
}
