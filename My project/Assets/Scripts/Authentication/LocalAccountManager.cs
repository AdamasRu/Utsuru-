using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Utsuru.Authentication
{
    [Serializable]
    public class LocalAccountRecord
    {
        public string accountId;
        public string username;
        public string email;
        public string passwordHash;
        public string role; // "admin" or "player"
        public string createdAt;
    }

    [Serializable]
    public class LocalAccountDatabase
    {
        public List<LocalAccountRecord> accounts = new List<LocalAccountRecord>();
    }

    /// <summary>
    /// Local Account Manager providing 100% offline CRUD operations for accounts.
    /// Replaces Supabase backend dependencies on this local-only branch.
    /// Accounts are stored in Application.persistentDataPath/Save/local_accounts.json.
    /// </summary>
    public class LocalAccountManager : MonoBehaviour
    {
        public static LocalAccountManager Instance { get; private set; }

        private const string SaveFolder = "Save";
        private const string DatabaseFileName = "local_accounts.json";

        private LocalAccountDatabase database = new LocalAccountDatabase();
        private LocalAccountRecord activeAccount = null;
        private bool isLoaded = false;

        public LocalAccountRecord ActiveAccount => activeAccount;
        public bool IsAdminLoggedIn => activeAccount != null && activeAccount.role == "admin";
        public bool IsLoggedIn => activeAccount != null;
        public string CurrentUsername => activeAccount != null ? activeAccount.username : "Guest";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                EnsureLoaded();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static LocalAccountManager EnsureInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<LocalAccountManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("LocalAccountManager");
                    Instance = go.AddComponent<LocalAccountManager>();
                }
            }
            Instance.EnsureLoaded();
            return Instance;
        }

        private string GetDatabaseFilePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFolder, DatabaseFileName);
        }

        public void EnsureLoaded()
        {
            if (isLoaded) return;

            string path = GetDatabaseFilePath();
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    database = JsonUtility.FromJson<LocalAccountDatabase>(json);
                    if (database == null) database = new LocalAccountDatabase();
                }
                else
                {
                    database = new LocalAccountDatabase();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalAccountManager] Gagal membaca '{path}': {ex.Message}. Membuat database baru.");
                database = new LocalAccountDatabase();
            }

            // Seed Akun Admin default jika belum ada
            EnsureDefaultAdminAccount();

            isLoaded = true;
        }

        private void EnsureDefaultAdminAccount()
        {
            bool hasAdmin = database.accounts.Exists(a => a.username.Equals("admin", StringComparison.OrdinalIgnoreCase));
            if (!hasAdmin)
            {
                var admin = new LocalAccountRecord
                {
                    accountId = "acc_admin",
                    username = "admin",
                    email = "admin@utsuru.local",
                    passwordHash = HashPassword("admin"),
                    role = "admin",
                    createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                database.accounts.Insert(0, admin);
                SaveDatabase();
                Debug.Log("[LocalAccountManager] Akun Admin default berhasil dibuat (User: admin, Pass: admin).");
            }
        }

        private void SaveDatabase()
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, SaveFolder);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonUtility.ToJson(database, true);
                File.WriteAllText(GetDatabaseFilePath(), json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalAccountManager] Gagal menyimpan database akun: {ex.Message}");
            }
        }

        // ==========================================
        // 1. CREATE: Registrasi Akun Baru (Sign Up)
        // ==========================================
        public bool SignUp(string username, string email, string password, out string errorMessage)
        {
            EnsureLoaded();
            errorMessage = string.Empty;

            username = username != null ? username.Trim() : string.Empty;
            email = email != null ? email.Trim() : string.Empty;
            password = password != null ? password : string.Empty;

            // Pertahankan bentuk email asli (jika username kosong, gunakan email)
            if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(email))
            {
                username = email;
            }
            if (string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(username))
            {
                email = username;
            }

            if (string.IsNullOrEmpty(username))
            {
                errorMessage = "Email / Username tidak boleh kosong.";
                return false;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                errorMessage = "Password minimal harus 6 karakter.";
                return false;
            }

            // Cek apakah akun / email sudah ada
            if (database.accounts.Exists(a =>
                a.username.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(email) && a.email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                a.username.Equals(email, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(username) && a.email.Equals(username, StringComparison.OrdinalIgnoreCase))))
            {
                errorMessage = "Akun atau Email ini sudah terdaftar. Silakan gunakan email lain atau login.";
                return false;
            }

            var newAccount = new LocalAccountRecord
            {
                accountId = "acc_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                username = username,
                email = !string.IsNullOrEmpty(email) ? email : username,
                passwordHash = HashPassword(password),
                role = "player",
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            database.accounts.Add(newAccount);
            SaveDatabase();

            Debug.Log($"[LocalAccountManager] Akun baru berhasil didaftarkan: {newAccount.username} ({newAccount.accountId})");
            return true;
        }

        // ==========================================
        // 2. READ & AUTH: Login
        // ==========================================
        public bool Login(string usernameOrEmail, string password, out LocalAccountRecord account, out string errorMessage)
        {
            EnsureLoaded();
            account = null;
            errorMessage = string.Empty;

            usernameOrEmail = usernameOrEmail != null ? usernameOrEmail.Trim() : string.Empty;
            password = password != null ? password : string.Empty;

            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
            {
                errorMessage = "Silakan masukkan username/email dan password.";
                return false;
            }

            string inputHash = HashPassword(password);

            // Cari berdasarkan username ATAU email
            var found = database.accounts.Find(a =>
                a.username.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) ||
                a.email.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase));

            if (found == null)
            {
                errorMessage = "Akun tidak ditemukan.";
                return false;
            }

            if (found.passwordHash != inputHash)
            {
                errorMessage = "Password salah.";
                return false;
            }

            activeAccount = found;
            account = found;
            Debug.Log($"[LocalAccountManager] Login berhasil sebagai: {found.username} (Role: {found.role})");
            return true;
        }

        // ==========================================
        // 3. READ: Ambil Semua Akun (Untuk Panel Admin)
        // ==========================================
        public IReadOnlyList<LocalAccountRecord> GetAllAccounts()
        {
            EnsureLoaded();
            return database.accounts;
        }

        public bool AccountExists(string usernameOrEmail)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(usernameOrEmail)) return false;
            return database.accounts.Exists(a =>
                a.username.Equals(usernameOrEmail.Trim(), StringComparison.OrdinalIgnoreCase) ||
                a.email.Equals(usernameOrEmail.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // ==========================================
        // 4. UPDATE: Reset / Ubah Password
        // ==========================================
        public bool ResetPassword(string usernameOrEmail, string newPassword, out string errorMessage)
        {
            EnsureLoaded();
            errorMessage = string.Empty;

            usernameOrEmail = usernameOrEmail != null ? usernameOrEmail.Trim() : string.Empty;
            newPassword = newPassword != null ? newPassword : string.Empty;

            if (string.IsNullOrEmpty(usernameOrEmail))
            {
                errorMessage = "Target akun tidak valid.";
                return false;
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                errorMessage = "Password baru minimal harus 6 karakter.";
                return false;
            }

            var account = database.accounts.Find(a =>
                a.username.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) ||
                a.email.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase));

            if (account == null)
            {
                errorMessage = "Akun tidak ditemukan.";
                return false;
            }

            account.passwordHash = HashPassword(newPassword);
            SaveDatabase();

            Debug.Log($"[LocalAccountManager] Password untuk akun '{account.username}' berhasil di-update!");
            return true;
        }

        // ==========================================
        // 5. DELETE: Hapus Akun (Dari Panel Admin)
        // ==========================================
        public bool DeleteAccount(string accountId, out string errorMessage)
        {
            EnsureLoaded();
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(accountId))
            {
                errorMessage = "ID akun tidak valid.";
                return false;
            }

            var account = database.accounts.Find(a => a.accountId == accountId);
            if (account == null)
            {
                errorMessage = "Akun tidak ditemukan.";
                return false;
            }

            // Proteksi: Akun admin tidak boleh dihapus
            if (account.role == "admin" || account.username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Akun Admin utama tidak dapat dihapus!";
                return false;
            }

            database.accounts.Remove(account);
            SaveDatabase();

            // Hapus file save dictionary terkait jika ada
            try
            {
                string dictSaveFile = Path.Combine(Application.persistentDataPath, SaveFolder, $"{account.username}_dictionary.json");
                if (File.Exists(dictSaveFile))
                {
                    File.Delete(dictSaveFile);
                    Debug.Log($"[LocalAccountManager] File save dictionary untuk '{account.username}' juga dihapus.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalAccountManager] Gagal menghapus save file dictionary: {ex.Message}");
            }

            Debug.Log($"[LocalAccountManager] Akun '{account.username}' ({accountId}) berhasil dihapus!");
            return true;
        }

        public void Logout()
        {
            activeAccount = null;
            Debug.Log("[LocalAccountManager] User logged out.");
        }

        public void SetActiveAccount(string usernameOrId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(usernameOrId))
            {
                activeAccount = null;
                return;
            }

            activeAccount = database.accounts.Find(a =>
                a.accountId.Equals(usernameOrId, StringComparison.OrdinalIgnoreCase) ||
                a.username.Equals(usernameOrId, StringComparison.OrdinalIgnoreCase) ||
                a.email.Equals(usernameOrId, StringComparison.OrdinalIgnoreCase));

            if (activeAccount != null)
            {
                Debug.Log($"[LocalAccountManager] Active account set to: {activeAccount.username} (Role: {activeAccount.role})");
            }
        }

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
