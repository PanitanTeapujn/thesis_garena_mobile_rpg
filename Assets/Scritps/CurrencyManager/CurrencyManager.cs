using UnityEngine;
using Fusion;
using System;

/// <summary>
/// จัดการระบบเงินและเพชรของผู้เล่น
/// เงินและเพชรจะใช้ร่วมกันทุกตัวละคร (เหมือน Shared Inventory)
/// </summary>
public class CurrencyManager : NetworkBehaviour
{
    #region Events
    public static event Action<long, long> OnGoldChanged;  // (oldAmount, newAmount)
    public static event Action<int, int> OnGemsChanged;    // (oldAmount, newAmount)
    public static event Action<CurrencyType, long, TransactionType> OnCurrencyTransaction;
    #endregion

    #region Network Properties
    [Networked] public long NetworkedGold { get; set; } = 1000;
    [Networked] public int NetworkedGems { get; set; } = 50;
    [Networked] public bool IsInitialized { get; set; } = false;
    #endregion

    #region Private Variables
    private bool hasTriedFirebaseLoad = false;
    private Character character;

    // Cache for quick access
    private long cachedGold = 1000;
    private int cachedGems = 50;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        character = GetComponent<Character>();
    }

    protected virtual void Start()
    {
        // Delayed initialization
        Invoke("TryInitialize", 1f);
    }

    public override void Spawned()
    {
        base.Spawned();

        // Quick initialization for network
        if (HasStateAuthority && !IsInitialized)
        {
            InitializeCurrencySystem();
        }
    }
    #endregion

    #region Initialization
    private void TryInitialize()
    {
        if (HasInputAuthority && !hasTriedFirebaseLoad)
        {
            hasTriedFirebaseLoad = true;
            TryLoadFromFirebase();
        }
    }

    private void TryLoadFromFirebase()
    {
        // ไม่ใช้ coroutine - check แบบง่ายๆ
        if (PersistentPlayerData.Instance.HasValidData() &&
            PersistentPlayerData.Instance.ShouldLoadCurrencyFromFirebase())
        {
            ApplyFirebaseData();
        }
        else
        {
            // ลองรอ 2 วินาที แล้วใช้ default
            Invoke("FallbackToDefault", 2f);
        }
    }

    private void ApplyFirebaseData()
    {
        if (PersistentPlayerData.Instance.multiCharacterData?.sharedCurrency != null)
        {
            var currencyData = PersistentPlayerData.Instance.multiCharacterData.sharedCurrency;

            if (HasInputAuthority)
            {
                RPC_ApplyFirebaseCurrency(currencyData.gold, currencyData.gems);
                Debug.Log($"✅ Applied Firebase currency data: Gold={currencyData.gold}, Gems={currencyData.gems}");
            }
        }
    }

    private void FallbackToDefault()
    {
        if (!IsInitialized && HasInputAuthority)
        {
            Debug.Log("Using fallback default currency");
            InitializeCurrencySystem();
        }
    }

    private void InitializeCurrencySystem()
    {
        if (IsInitialized) return;

        // Initialize with default values
        NetworkedGold = 1000;
        NetworkedGems = 50;

        cachedGold = NetworkedGold;
        cachedGems = NetworkedGems;

        IsInitialized = true;

        Debug.Log($"✅ Initialized currency system - Gold: {NetworkedGold}, Gems: {NetworkedGems}");

        // Fire events
        OnGoldChanged?.Invoke(0, NetworkedGold);
        OnGemsChanged?.Invoke(0, NetworkedGems);
    }
    #endregion

    #region Network RPCs
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ApplyFirebaseCurrency(long gold, int gems)
    {
        NetworkedGold = gold;
        NetworkedGems = gems;

        cachedGold = gold;
        cachedGems = gems;

        IsInitialized = true;

        // Broadcast to all clients
        RPC_BroadcastCurrency(gold, gems);

        Debug.Log($"✅ Applied Firebase currency: Gold={gold}, Gems={gems}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastCurrency(long gold, int gems)
    {
        long oldGold = cachedGold;
        int oldGems = cachedGems;

        NetworkedGold = gold;
        NetworkedGems = gems;

        cachedGold = gold;
        cachedGems = gems;

        IsInitialized = true;

        // Fire events
        OnGoldChanged?.Invoke(oldGold, gold);
        OnGemsChanged?.Invoke(oldGems, gems);

        Debug.Log($"✅ Currency synced: Gold={gold}, Gems={gems}");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestCurrencyChange(CurrencyType currencyType, long amount, TransactionType transactionType)
    {
        if (transactionType == TransactionType.Earn)
        {
            AddCurrencyInternal(currencyType, amount);
        }
        else if (transactionType == TransactionType.Spend)
        {
            SpendCurrencyInternal(currencyType, amount);
        }
    }
    #endregion

    #region Public Currency Methods
    /// <summary>
    /// เพิ่มเงิน
    /// </summary>
    public bool AddGold(long amount, bool saveImmediately = true)
    {
        if (amount <= 0) return false;

        if (HasStateAuthority)
        {
            return AddCurrencyInternal(CurrencyType.Gold, amount, saveImmediately);
        }
        else if (HasInputAuthority)
        {
            RPC_RequestCurrencyChange(CurrencyType.Gold, amount, TransactionType.Earn);
            return true; // จะได้รับผลลัพธ์จาก RPC
        }

        return false;
    }

    /// <summary>
    /// เพิ่มเพชร
    /// </summary>
    public bool AddGems(int amount, bool saveImmediately = true)
    {
        if (amount <= 0) return false;

        if (HasStateAuthority)
        {
            return AddCurrencyInternal(CurrencyType.Gems, amount, saveImmediately);
        }
        else if (HasInputAuthority)
        {
            RPC_RequestCurrencyChange(CurrencyType.Gems, amount, TransactionType.Earn);
            return true;
        }

        return false;
    }

    /// <summary>
    /// ใช้เงิน
    /// </summary>
    public bool SpendGold(long amount, bool saveImmediately = true)
    {
        if (amount <= 0 || !HasEnoughGold(amount)) return false;

        if (HasStateAuthority)
        {
            return SpendCurrencyInternal(CurrencyType.Gold, amount, saveImmediately);
        }
        else if (HasInputAuthority)
        {
            RPC_RequestCurrencyChange(CurrencyType.Gold, amount, TransactionType.Spend);
            return true;
        }

        return false;
    }

    /// <summary>
    /// ใช้เพชร
    /// </summary>
    public bool SpendGems(int amount, bool saveImmediately = true)
    {
        if (amount <= 0 || !HasEnoughGems(amount)) return false;

        if (HasStateAuthority)
        {
            return SpendCurrencyInternal(CurrencyType.Gems, amount, saveImmediately);
        }
        else if (HasInputAuthority)
        {
            RPC_RequestCurrencyChange(CurrencyType.Gems, amount, TransactionType.Spend);
            return true;
        }

        return false;
    }

    /// <summary>
    /// ตรวจสอบว่ามีเงินเพียงพอ
    /// </summary>
    public bool HasEnoughGold(long amount)
    {
        return GetCurrentGold() >= amount;
    }

    /// <summary>
    /// ตรวจสอบว่ามีเพชรเพียงพอ
    /// </summary>
    public bool HasEnoughGems(int amount)
    {
        return GetCurrentGems() >= amount;
    }

    /// <summary>
    /// ดึงจำนวนเงินปัจจุบัน
    /// </summary>
    public long GetCurrentGold()
    {
        return IsInitialized ? NetworkedGold : cachedGold;
    }

    /// <summary>
    /// ดึงจำนวนเพชรปัจจุบัน
    /// </summary>
    public int GetCurrentGems()
    {
        return IsInitialized ? NetworkedGems : cachedGems;
    }
    #endregion

    #region Internal Currency Operations
    private bool AddCurrencyInternal(CurrencyType currencyType, long amount, bool saveImmediately = true)
    {
        if (amount <= 0) return false;

        long oldValue = 0;
        long newValue = 0;
        bool success = false;

        if (currencyType == CurrencyType.Gold)
        {
            oldValue = NetworkedGold;
            long newGold = NetworkedGold + amount;

            // Check max limit
            if (newGold > 999999999) newGold = 999999999;

            NetworkedGold = newGold;
            cachedGold = newGold;
            newValue = newGold;
            success = true;

            OnGoldChanged?.Invoke(oldValue, newValue);
        }
        else if (currencyType == CurrencyType.Gems)
        {
            oldValue = NetworkedGems;
            int newGems = NetworkedGems + (int)amount;

            // Check max limit
            if (newGems > 999999) newGems = 999999;

            NetworkedGems = newGems;
            cachedGems = newGems;
            newValue = newGems;
            success = true;

            OnGemsChanged?.Invoke((int)oldValue, (int)newValue);
        }

        if (success)
        {
            OnCurrencyTransaction?.Invoke(currencyType, amount, TransactionType.Earn);

            // Broadcast to all clients
            RPC_BroadcastCurrency(NetworkedGold, NetworkedGems);

            if (saveImmediately)
            {
                QuickSaveCurrency();
            }

            Debug.Log($"💰 Added {amount} {currencyType}: {oldValue} → {newValue}");
        }

        return success;
    }

    private bool SpendCurrencyInternal(CurrencyType currencyType, long amount, bool saveImmediately = true)
    {
        if (amount <= 0) return false;

        long oldValue = 0;
        long newValue = 0;
        bool success = false;

        if (currencyType == CurrencyType.Gold)
        {
            if (NetworkedGold < amount) return false;

            oldValue = NetworkedGold;
            NetworkedGold -= amount;
            cachedGold = NetworkedGold;
            newValue = NetworkedGold;
            success = true;

            OnGoldChanged?.Invoke(oldValue, newValue);
        }
        else if (currencyType == CurrencyType.Gems)
        {
            if (NetworkedGems < amount) return false;

            oldValue = NetworkedGems;
            NetworkedGems -= (int)amount;
            cachedGems = NetworkedGems;
            newValue = NetworkedGems;
            success = true;

            OnGemsChanged?.Invoke((int)oldValue, (int)newValue);
        }

        if (success)
        {
            OnCurrencyTransaction?.Invoke(currencyType, amount, TransactionType.Spend);

            // Broadcast to all clients
            RPC_BroadcastCurrency(NetworkedGold, NetworkedGems);

            if (saveImmediately)
            {
                QuickSaveCurrency();
            }

            Debug.Log($"💸 Spent {amount} {currencyType}: {oldValue} → {newValue}");
        }

        return success;
    }
    #endregion

    #region Save/Load
    private void QuickSaveCurrency()
    {
        if (!HasInputAuthority) return;

        try
        {
            if (PersistentPlayerData.Instance.multiCharacterData?.sharedCurrency != null)
            {
                var currencyData = PersistentPlayerData.Instance.multiCharacterData.sharedCurrency;

                // Update currency data
                currencyData.gold = NetworkedGold;
                currencyData.gems = NetworkedGems;
                currencyData.UpdateDebugInfo();

                // Save to Firebase
                PersistentPlayerData.Instance.SaveCurrencyData();

                Debug.Log($"💾 Currency saved: Gold={NetworkedGold}, Gems={NetworkedGems}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error saving currency: {e.Message}");
        }
    }

    public void ForceSaveCurrency()
    {
        QuickSaveCurrency();
    }

    public void ForceLoadCurrency()
    {
        TryLoadFromFirebase();
    }
    #endregion

    #region Static Helper Methods
    /// <summary>
    /// หา CurrencyManager ในฉาก
    /// </summary>
    public static CurrencyManager FindCurrencyManager()
    {
        return FindObjectOfType<CurrencyManager>();
    }

    /// <summary>
    /// เพิ่มเงินผ่าน static method
    /// </summary>
    public static bool AddGoldStatic(long amount)
    {
        var manager = FindCurrencyManager();
        return manager?.AddGold(amount) ?? false;
    }

    /// <summary>
    /// เพิ่มเพชรผ่าน static method
    /// </summary>
    public static bool AddGemsStatic(int amount)
    {
        var manager = FindCurrencyManager();
        return manager?.AddGems(amount) ?? false;
    }

    /// <summary>
    /// ใช้เงินผ่าน static method
    /// </summary>
    public static bool SpendGoldStatic(long amount)
    {
        var manager = FindCurrencyManager();
        return manager?.SpendGold(amount) ?? false;
    }

    /// <summary>
    /// ใช้เพชรผ่าน static method
    /// </summary>
    public static bool SpendGemsStatic(int amount)
    {
        var manager = FindCurrencyManager();
        return manager?.SpendGems(amount) ?? false;
    }
    #endregion

    #region Debug Methods
  
    #endregion
}