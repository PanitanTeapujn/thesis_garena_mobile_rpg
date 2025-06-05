/*using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections;

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

    // ข้อมูลผู้เล่นแบบง่าย
    [System.Serializable]
    public class SimplePlayerData
    {
        public string playerName;
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

        // ถ้ากด Enter ให้ Login
        passwordInput.onSubmit.AddListener(delegate { OnLoginButtonClicked(); });

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    void OnLoginButtonClicked()
    {
        string email = nameInput.text.Trim() + "@game.com"; // แปลง username เป็น email
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
            ShowError("กรุณาใส่ชื่อผู้ใช้");
            return false;
        }

        if (username.Length < 3 || username.Length > 16)
        {
            ShowError("ชื่อผู้ใช้ต้องมี 3-16 ตัวอักษร");
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowError("กรุณาใส่รหัสผ่าน");
            return false;
        }

        if (password.Length < 6)
        {
            ShowError("รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร");
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

            string message = "เข้าสู่ระบบล้มเหลว";
            switch (errorCode)
            {
                case AuthError.UserNotFound:
                    message = "ไม่พบผู้ใช้นี้";
                    break;
                case AuthError.WrongPassword:
                    message = "รหัสผ่านไม่ถูกต้อง";
                    break;
            }
            ShowError(message);
        }
        else
        {
            user = loginTask.Result.User;
            Debug.Log($"Login successful: {user.Email}");

            // อัพเดทเวลา login ล่าสุด
            UpdateLastLogin();

            // โหลดข้อมูลผู้เล่น
            yield return StartCoroutine(LoadPlayerData());

            // ไปหน้าเลือกตัวละคร
            SceneManager.LoadScene("CharacterSelection");
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

            string message = "ลงทะเบียนล้มเหลว";
            switch (errorCode)
            {
                case AuthError.EmailAlreadyInUse:
                    message = "ชื่อผู้ใช้นี้มีคนใช้แล้ว";
                    break;
                case AuthError.WeakPassword:
                    message = "รหัสผ่านไม่ปลอดภัย";
                    break;
            }
            ShowError(message);
        }
        else
        {
            user = registerTask.Result.User;
            Debug.Log($"Registration successful: {user.Email}");

            // สร้างข้อมูลเริ่มต้น
            yield return StartCoroutine(CreateInitialPlayerData());

            // ไปหน้าเลือกตัวละคร
            SceneManager.LoadScene("CharacterSelection");
        }
    }

    IEnumerator CreateInitialPlayerData()
    {
        SimplePlayerData newPlayerData = new SimplePlayerData
        {
            playerName = nameInput.text.Trim(),
            lastCharacterSelected = "IronJuggernaut", // ตัวละครเริ่มต้น
            registrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string json = JsonUtility.ToJson(newPlayerData);

        var task = databaseReference.Child("players").Child(user.UserId).SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"Failed to create player data: {task.Exception}");
        }
        else
        {
            Debug.Log("Player data created successfully");

            // บันทึกชื่อผู้เล่นไว้ใน PlayerPrefs
            PlayerPrefs.SetString("PlayerName", newPlayerData.playerName);
            PlayerPrefs.SetString("PlayerId", user.UserId);
        }
    }

    IEnumerator LoadPlayerData()
    {
        var task = databaseReference.Child("players").Child(user.UserId).GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"Failed to load player data: {task.Exception}");
        }
        else if (task.Result.Exists)
        {
            DataSnapshot snapshot = task.Result;
            string json = snapshot.GetRawJsonValue();
            SimplePlayerData playerData = JsonUtility.FromJson<SimplePlayerData>(json);

            // บันทึกข้อมูลใน PlayerPrefs
            PlayerPrefs.SetString("PlayerName", playerData.playerName);
            PlayerPrefs.SetString("PlayerId", user.UserId);

            // โหลดตัวละครที่เลือกล่าสุด
            if (!string.IsNullOrEmpty(playerData.lastCharacterSelected))
            {
                if (Enum.TryParse<PlayerSelectionData.CharacterType>(playerData.lastCharacterSelected, out var charType))
                {
                    PlayerSelectionData.SaveCharacterSelection(charType);
                }
            }
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
}*/