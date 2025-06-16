using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;

public class FirebaseLoginManager : MonoBehaviour
{
    [Header("Firebase")]
    public FirebaseAuth auth;
    public FirebaseUser user;
    private DatabaseReference databaseReference;

    [Header("Login UI")]
    public TMP_InputField nameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    public TextMeshProUGUI errorText;
    public GameObject loadingPanel;

    [System.Serializable]
    public class SimplePlayerData
    {
        public string playerName;
        public string password;
        public string lastCharacterSelected;
        public string registrationDate;
        public string lastLoginDate;
    }

    void Start()
    {
        InitializeFirebase();
        SetupUI();
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase initialized successfully");
            }
            else
            {
                Debug.LogError($"Could not resolve Firebase dependencies: {task.Result}");
                ShowError("Failed to initialize Firebase");
            }
        });
    }

    void SetupUI()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        passwordInput.onSubmit.AddListener(delegate { OnLoginButtonClicked(); });

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    void OnLoginButtonClicked()
    {
        string email = nameInput.text.Trim() + "@game.com";
        string password = passwordInput.text;

        if (ValidateInput())
        {
            StartCoroutine(LoginUser(email, password));
        }
    }

    void OnRegisterButtonClicked()
    {
        string email = nameInput.text.Trim() + "@game.com";
        string password = passwordInput.text;

        if (ValidateInput())
        {
            StartCoroutine(RegisterUser(email, password));
        }
    }

    bool ValidateInput()
    {
        string username = nameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username))
        {
            ShowError("Please enter username.");
            return false;
        }

        if (username.Length < 3 || username.Length > 16)
        {
            ShowError("Username must contain 3-16 characters.");
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowError("Please enter your password.");
            return false;
        }

        if (password.Length < 6)
        {
            ShowError("Password must contain at least 6 characters.");
            return false;
        }

        return true;
    }

    IEnumerator LoginUser(string email, string password)
    {
        ShowLoading(true);

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        ShowLoading(false);

        if (loginTask.Exception != null)
        {
            Debug.LogWarning($"Failed to login: {loginTask.Exception}");
            FirebaseException firebaseEx = loginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Login failed";
            switch (errorCode)
            {
                case AuthError.UserNotFound:
                    message = "This user was not found.";
                    break;
                case AuthError.WrongPassword:
                    message = "The password is incorrect.";
                    break;
            }
            ShowError(message);
        }
        else
        {
            user = loginTask.Result.User;
            Debug.Log($"Login successful: {user.Email}");

            UpdateLastLogin();

            // ✅ Setup ข้อมูลพื้นฐาน
            SetupPlayerDataQuick();

            // Start loading data in background
            PersistentPlayerData.Instance.LoadPlayerDataAsync();

            // ไปหน้าต่อไป
            SceneManager.LoadScene("Lobby");
        }
    }

    IEnumerator RegisterUser(string email, string password)
    {
        ShowLoading(true);

        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        ShowLoading(false);

        if (registerTask.Exception != null)
        {
            Debug.LogWarning($"Failed to register: {registerTask.Exception}");
            FirebaseException firebaseEx = registerTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Registration failed";
            switch (errorCode)
            {
                case AuthError.EmailAlreadyInUse:
                    message = "This username is already taken.";
                    break;
                case AuthError.WeakPassword:
                    message = "Password is not safe.";
                    break;
            }
            ShowError(message);
        }
        else
        {
            user = registerTask.Result.User;
            Debug.Log($"Registration successful: {user.Email}");

            // ✅ สร้างผู้เล่นใหม่ด้วย default Assassin stats ที่ถูกต้อง
            SetupNewPlayerWithDefaultAssassin();

            // Create Firebase data in background
            StartCoroutine(CreateFirebaseDataAsync());

            SceneManager.LoadScene("Lobby");
        }
    }

    // ✅ แก้ไข SetupNewPlayerWithDefaultAssassin ให้ครบถ้วน
    private void SetupNewPlayerWithDefaultAssassin()
    {
        string playerName = nameInput.text.Trim();
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("PlayerId", user.UserId);

        // Set default character เป็น Assassin
        PlayerSelectionData.SaveCharacterSelection(PlayerSelectionData.CharacterType.Assassin);
        PlayerPrefs.SetString("LastCharacterSelected", "Assassin");

        // ✅ โหลด Assassin stats จาก ScriptableObject
        CharacterStats assassinStats = Resources.Load<CharacterStats>("Characters/AssassinStats");

        if (assassinStats != null)
        {
            // ใช้ stats จาก ScriptableObject
            PlayerPrefs.SetInt("PlayerLevel", 1);
            PlayerPrefs.SetInt("PlayerExp", 0);
            PlayerPrefs.SetInt("PlayerExpToNext", 100);
            PlayerPrefs.SetInt("PlayerMaxHp", assassinStats.maxHp);
            PlayerPrefs.SetInt("PlayerMaxMana", assassinStats.maxMana);
            PlayerPrefs.SetInt("PlayerAttackDamage", assassinStats.attackDamage);
            PlayerPrefs.SetInt("PlayerArmor", assassinStats.arrmor);
            PlayerPrefs.SetFloat("PlayerCritChance", assassinStats.criticalChance);
            PlayerPrefs.SetFloat("PlayerMoveSpeed", assassinStats.moveSpeed);
            PlayerPrefs.SetFloat("PlayerHitRate", assassinStats.hitRate);
            PlayerPrefs.SetFloat("PlayerEvasionRate", assassinStats.evasionRate);
            PlayerPrefs.SetFloat("PlayerAttackSpeed", assassinStats.attackSpeed);

            Debug.Log($"✅ New player setup with ScriptableObject Assassin stats: HP={assassinStats.maxHp}, ATK={assassinStats.attackDamage}");
        }
        else
        {
            // Fallback ถ้าหา ScriptableObject ไม่เจอ
            PlayerPrefs.SetInt("PlayerLevel", 1);
            PlayerPrefs.SetInt("PlayerExp", 0);
            PlayerPrefs.SetInt("PlayerExpToNext", 100);
            PlayerPrefs.SetInt("PlayerMaxHp", 70);      // Assassin HP
            PlayerPrefs.SetInt("PlayerMaxMana", 40);    // Assassin Mana  
            PlayerPrefs.SetInt("PlayerAttackDamage", 35); // Assassin Attack
            PlayerPrefs.SetInt("PlayerArmor", 2);       // Assassin Armor
            PlayerPrefs.SetFloat("PlayerCritChance", 5f);
            PlayerPrefs.SetFloat("PlayerMoveSpeed", 6.5f); // Assassin Speed
            PlayerPrefs.SetFloat("PlayerHitRate", 85f);    // Assassin Hit Rate
            PlayerPrefs.SetFloat("PlayerEvasionRate", 12f); // Assassin Evasion
            PlayerPrefs.SetFloat("PlayerAttackSpeed", 1.3f); // Assassin Attack Speed

            Debug.LogWarning("⚠️ AssassinStats ScriptableObject not found, using fallback values");
        }

        PlayerPrefs.Save();
        Debug.Log($"✅ New player setup completed for {playerName}");
    }

    // ========== Quick Setup Methods (No Blocking) ==========
    private void SetupPlayerDataQuick()
    {
        // Setup basic PlayerPrefs immediately
        string playerName = nameInput.text.Trim();
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("PlayerId", user.UserId);

        // Set character selection from previous session if exists
        string savedCharacter = PlayerPrefs.GetString("LastCharacterSelected", "Assassin");
        if (Enum.TryParse<PlayerSelectionData.CharacterType>(savedCharacter, out var charType))
        {
            PlayerSelectionData.SaveCharacterSelection(charType);
        }

        Debug.Log($"✅ Quick setup completed for {playerName} with character {savedCharacter}");
    }

    // ========== Background Firebase Operations ==========
    private IEnumerator CreateFirebaseDataAsync()
    {
        // ✅ สร้าง MultiCharacterPlayerData ที่ถูกต้อง
        MultiCharacterPlayerData newPlayerData = new MultiCharacterPlayerData();
        newPlayerData.playerName = nameInput.text.Trim();
        newPlayerData.currentActiveCharacter = "Assassin";
        newPlayerData.registrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        newPlayerData.lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // ✅ ตรวจสอบว่า Assassin data ถูกสร้างแล้วและมี stats ถูกต้อง
        CharacterProgressData assassinData = newPlayerData.GetActiveCharacterData();
        if (assassinData != null)
        {
            Debug.Log($"✅ Assassin data created: Level {assassinData.currentLevel}, HP {assassinData.totalMaxHp}, ATK {assassinData.totalAttackDamage}");
        }
        else
        {
            Debug.LogError("❌ Failed to create Assassin data!");
        }

        // Save to PersistentPlayerData
        PersistentPlayerData.Instance.multiCharacterData = newPlayerData;
        PersistentPlayerData.Instance.isDataLoaded = true;

        // ✅ Force sync currentPlayerData
        PersistentPlayerData.Instance.Debug_ForceSyncCurrentPlayerData();

        // Save to Firebase (background)
        string json = JsonUtility.ToJson(newPlayerData, true);
        Debug.Log($"📝 Firebase JSON to save: {json.Substring(0, Mathf.Min(200, json.Length))}...");

        var task = databaseReference.Child("players").Child(user.UserId).SetRawJsonValueAsync(json);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"❌ Failed to create Firebase data: {task.Exception}");
        }
        else
        {
            Debug.Log($"✅ Firebase multi-character data created successfully for {newPlayerData.playerName}");
            newPlayerData.LogAllCharacters();
        }
    }

    void UpdateLastLogin()
    {
        if (user == null) return;

        var updates = new Dictionary<string, object>
        {
            { "lastLoginDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };

        databaseReference.Child("players").Child(user.UserId).UpdateChildrenAsync(updates);
    }

    // ========== UI Methods ==========
    void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            Invoke("HideError", 3f);
        }
    }

    void HideError()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    void ShowLoading(bool show)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(show);

        loginButton.interactable = !show;
        registerButton.interactable = !show;
        nameInput.interactable = !show;
        passwordInput.interactable = !show;
    }

    // ========== Debug Methods ==========
    [ContextMenu("Test Create New Player")]
    public void Debug_TestCreateNewPlayer()
    {
        if (string.IsNullOrEmpty(nameInput.text))
        {
            nameInput.text = "TestPlayer";
        }

        SetupNewPlayerWithDefaultAssassin();
        Debug.Log("🧪 Test player creation completed");
    }
}