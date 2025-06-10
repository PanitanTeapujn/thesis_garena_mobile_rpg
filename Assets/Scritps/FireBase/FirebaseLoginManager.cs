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
    private FirebaseAuth auth;
    private FirebaseUser user;
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

            // Quick setup - ไม่รอให้ Firebase load เสร็จ
            SetupPlayerDataQuick();

            // Start loading data in background
            PersistentPlayerData.Instance.LoadPlayerDataAsync();

            // ไปหน้าต่อไปเลย
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

            // Quick setup for new user
            SetupNewPlayerDataQuick();

            // Create Firebase data in background
            StartCoroutine(CreateFirebaseDataAsync());

            // ไปหน้าเลือกตัวละคร
            SceneManager.LoadScene("CharacterSelection");
        }
    }

    // ========== Quick Setup Methods (No Blocking) ==========
    private void SetupPlayerDataQuick()
    {
        // Setup basic PlayerPrefs immediately
        string playerName = nameInput.text.Trim();
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("PlayerId", user.UserId);

        // Set character selection from previous session if exists
        string savedCharacter = PlayerPrefs.GetString("LastCharacterSelected", "IronJuggernaut");
        if (Enum.TryParse<PlayerSelectionData.CharacterType>(savedCharacter, out var charType))
        {
            PlayerSelectionData.SaveCharacterSelection(charType);
        }

        Debug.Log($"✅ Quick setup completed for {playerName}");
    }

    private void SetupNewPlayerDataQuick()
    {
        string playerName = nameInput.text.Trim();
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("PlayerId", user.UserId);
        PlayerSelectionData.SaveCharacterSelection(PlayerSelectionData.CharacterType.IronJuggernaut);

        // Set basic default stats in PlayerPrefs
        PlayerPrefs.SetInt("PlayerLevel", 1);
        PlayerPrefs.SetInt("PlayerExp", 0);
        PlayerPrefs.SetInt("PlayerMaxHp", 100);
        PlayerPrefs.SetInt("PlayerMaxMana", 50);
        PlayerPrefs.SetInt("PlayerAttackDamage", 20);
        PlayerPrefs.SetInt("PlayerArmor", 5);
        PlayerPrefs.SetFloat("PlayerCritChance", 5f);
        PlayerPrefs.SetFloat("PlayerMoveSpeed", 5f);

        Debug.Log($"✅ New player quick setup completed for {playerName}");
    }

    // ========== Background Firebase Operations ==========
    private IEnumerator CreateFirebaseDataAsync()
    {
        PlayerProgressData newPlayerData = new PlayerProgressData();
        newPlayerData.playerName = nameInput.text.Trim();
        newPlayerData.lastCharacterSelected = "Assasins";
        newPlayerData.registrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        newPlayerData.lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Apply basic stats
        newPlayerData.currentLevel = 1;
        newPlayerData.currentExp = 0;
        newPlayerData.expToNextLevel = 100;
        newPlayerData.totalMaxHp = 100;
        newPlayerData.totalMaxMana = 50;
        newPlayerData.totalAttackDamage = 20;
        newPlayerData.totalArmor = 5;
        newPlayerData.totalCriticalChance = 5f;
        newPlayerData.totalCriticalMultiplier = 2f;
        newPlayerData.totalMoveSpeed = 5f;
        newPlayerData.totalAttackRange = 2f;
        newPlayerData.totalAttackCooldown = 1f;

        // Save to PersistentPlayerData
        PersistentPlayerData.Instance.currentPlayerData = newPlayerData;
        PersistentPlayerData.Instance.isDataLoaded = true;

        // Save to Firebase (background)
        string json = JsonUtility.ToJson(newPlayerData, true);
        var task = databaseReference.Child("players").Child(user.UserId).SetRawJsonValueAsync(json);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"Failed to create Firebase data: {task.Exception}");
        }
        else
        {
            Debug.Log($"✅ Firebase data created successfully for {newPlayerData.playerName}");
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
}