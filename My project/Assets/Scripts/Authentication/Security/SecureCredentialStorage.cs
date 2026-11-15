using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace Utsuru.Authentication.Security
{
    /// <summary>
    /// Encrypted credential vault payload before DPAPI encryption.
    /// Strictly kept in-memory and never written in plaintext to disk.
    /// </summary>
    [Serializable]
    public class VaultCredentialData
    {
        [JsonProperty("auth_user_id")]
        public string auth_user_id;

        [JsonProperty("access_token")]
        public string access_token;

        [JsonProperty("refresh_token")]
        public string refresh_token;

        [JsonProperty("token_type")]
        public string token_type;

        [JsonProperty("expires_in")]
        public long expires_in;

        [JsonProperty("saved_at_utc")]
        public string saved_at_utc;
    }

    /// <summary>
    /// Hardware/User-bound credential vault utilizing Windows DPAPI (Data Protection API).
    /// Stores encrypted authentication tokens separately from unencrypted profile metadata.
    /// Uses DataProtectionScope.CurrentUser so data can only be decrypted by the same Windows user account.
    /// Fully compatible with both .NET runtime and Unity Mono Windows Standalone builds.
    /// </summary>
    public static class SecureCredentialStorage
    {
        private const string SaveFolderName = "Save";
        private const string VaultFileName = "auth_vault.dat";
        private const string TempVaultFileName = "auth_vault.dat.tmp";

        private static string customSaveDirectory = null;

        private static void LogInfo(string msg) { try { Debug.Log(msg); } catch { } }
        private static void LogWarn(string msg) { try { Debug.LogWarning(msg); } catch { } }
        private static void LogErr(string msg) { try { Debug.LogError(msg); } catch { } }

        #region Native Windows DPAPI Interop Fallback for Unity Mono
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptProtectData(
            ref DATA_BLOB pDataIn,
            string szDataDescr,
            ref DATA_BLOB pOptionalEntropy,
            IntPtr pvReserved,
            IntPtr pPromptStruct,
            int dwFlags,
            ref DATA_BLOB pDataOut);

        [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptUnprotectData(
            ref DATA_BLOB pDataIn,
            StringBuilder ppszDataDescr,
            ref DATA_BLOB pOptionalEntropy,
            IntPtr pvReserved,
            IntPtr pPromptStruct,
            int dwFlags,
            ref DATA_BLOB pDataOut);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

        private static byte[] ProtectNative(byte[] data)
        {
            DATA_BLOB inBlob = new DATA_BLOB();
            DATA_BLOB outBlob = new DATA_BLOB();
            DATA_BLOB entropyBlob = new DATA_BLOB();

            IntPtr inPtr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, inPtr, data.Length);
            inBlob.cbData = data.Length;
            inBlob.pbData = inPtr;

            try
            {
                if (CryptProtectData(ref inBlob, "UTSURU_AUTH", ref entropyBlob, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref outBlob))
                {
                    byte[] cipher = new byte[outBlob.cbData];
                    Marshal.Copy(outBlob.pbData, cipher, 0, outBlob.cbData);
                    LocalFree(outBlob.pbData);
                    return cipher;
                }
                throw new CryptographicException($"CryptProtectData failed with Win32 error code {Marshal.GetLastWin32Error()}");
            }
            finally
            {
                Marshal.FreeHGlobal(inPtr);
            }
        }

        private static byte[] UnprotectNative(byte[] cipher)
        {
            DATA_BLOB inBlob = new DATA_BLOB();
            DATA_BLOB outBlob = new DATA_BLOB();
            DATA_BLOB entropyBlob = new DATA_BLOB();

            IntPtr inPtr = Marshal.AllocHGlobal(cipher.Length);
            Marshal.Copy(cipher, 0, inPtr, cipher.Length);
            inBlob.cbData = cipher.Length;
            inBlob.pbData = inPtr;

            try
            {
                if (CryptUnprotectData(ref inBlob, null, ref entropyBlob, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref outBlob))
                {
                    byte[] plain = new byte[outBlob.cbData];
                    Marshal.Copy(outBlob.pbData, plain, 0, outBlob.cbData);
                    LocalFree(outBlob.pbData);
                    return plain;
                }
                throw new CryptographicException($"CryptUnprotectData failed with Win32 error code {Marshal.GetLastWin32Error()}");
            }
            finally
            {
                Marshal.FreeHGlobal(inPtr);
            }
        }
        #endregion

        /// <summary>
        /// Allows test runners or unit tests to specify an explicit save directory.
        /// </summary>
        public static void SetCustomSaveDirectory(string path)
        {
            customSaveDirectory = path;
        }

        /// <summary>
        /// Gets the absolute directory path where the encrypted vault is stored.
        /// </summary>
        public static string GetSaveDirectory()
        {
            if (!string.IsNullOrEmpty(customSaveDirectory))
            {
                return customSaveDirectory;
            }

            try
            {
                return Path.Combine(Application.persistentDataPath, SaveFolderName);
            }
            catch
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData + "Low", "DefaultCompany", "My project", SaveFolderName);
            }
        }

        /// <summary>
        /// Gets the absolute path to the encrypted DPAPI vault file.
        /// </summary>
        public static string GetVaultFilePath()
        {
            return Path.Combine(GetSaveDirectory(), VaultFileName);
        }

        /// <summary>
        /// Checks if an encrypted vault file exists on disk.
        /// </summary>
        public static bool HasVault()
        {
            return File.Exists(GetVaultFilePath());
        }

        /// <summary>
        /// Encrypts data using Windows DPAPI (CurrentUser scope) via native Crypt32.dll.
        /// Native P/Invoke ensures full compatibility across Unity Editor, Mono Standalone, and IL2CPP without external NuGet dependencies.
        /// </summary>
        public static byte[] EncryptBytes(byte[] plaintext)
        {
            return ProtectNative(plaintext);
        }

        /// <summary>
        /// Decrypts data using Windows DPAPI (CurrentUser scope) via native Crypt32.dll.
        /// Native P/Invoke ensures full compatibility across Unity Editor, Mono Standalone, and IL2CPP without external NuGet dependencies.
        /// </summary>
        public static byte[] DecryptBytes(byte[] ciphertext)
        {
            return UnprotectNative(ciphertext);
        }

        /// <summary>
        /// Encrypts and saves tokens to disk using Windows DPAPI.
        /// </summary>
        public static bool SaveCredentials(string authUserId, string accessToken, string refreshToken, string tokenType = "bearer", long expiresIn = 3600)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                LogErr("[SecureCredentialStorage] Cannot store empty or null access token.");
                return false;
            }

            string saveDir = GetSaveDirectory();
            string vaultPath = GetVaultFilePath();
            string tempPath = Path.Combine(saveDir, TempVaultFileName);

            try
            {
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                VaultCredentialData vaultData = new VaultCredentialData
                {
                    auth_user_id = authUserId,
                    access_token = accessToken,
                    refresh_token = refreshToken,
                    token_type = tokenType,
                    expires_in = expiresIn,
                    saved_at_utc = DateTime.UtcNow.ToString("o")
                };

                string json = JsonConvert.SerializeObject(vaultData);
                byte[] plaintextBytes = Encoding.UTF8.GetBytes(json);

                // Encrypt using Windows DPAPI
                byte[] encryptedBytes = EncryptBytes(plaintextBytes);

                // Clear plaintext buffer from memory
                Array.Clear(plaintextBytes, 0, plaintextBytes.Length);

                // Atomic write
                File.WriteAllBytes(tempPath, encryptedBytes);
                if (File.Exists(vaultPath))
                {
                    File.Delete(vaultPath);
                }
                File.Move(tempPath, vaultPath);

                LogInfo("[SecureCredentialStorage] Credentials successfully encrypted and stored with Windows DPAPI.");
                return true;
            }
            catch (CryptographicException cryptoEx)
            {
                LogErr($"[SecureCredentialStorage] DPAPI encryption failed: {cryptoEx.Message}");
                CleanTemp(tempPath);
                return false;
            }
            catch (Exception ex)
            {
                LogErr($"[SecureCredentialStorage] Failed to save encrypted vault: {ex.Message}");
                CleanTemp(tempPath);
                return false;
            }
        }

        /// <summary>
        /// Loads and decrypts tokens from the DPAPI vault.
        /// Returns null safely if vault is missing, corrupted, or cannot be decrypted.
        /// </summary>
        public static VaultCredentialData LoadCredentials()
        {
            string vaultPath = GetVaultFilePath();
            if (!File.Exists(vaultPath))
            {
                return null;
            }

            try
            {
                byte[] encryptedBytes = File.ReadAllBytes(vaultPath);
                if (encryptedBytes == null || encryptedBytes.Length == 0)
                {
                    LogWarn("[SecureCredentialStorage] Vault file is empty.");
                    return null;
                }

                // Decrypt using Windows DPAPI
                byte[] decryptedBytes = DecryptBytes(encryptedBytes);

                string json = Encoding.UTF8.GetString(decryptedBytes);
                Array.Clear(decryptedBytes, 0, decryptedBytes.Length);

                VaultCredentialData data = JsonConvert.DeserializeObject<VaultCredentialData>(json);
                return data;
            }
            catch (CryptographicException cryptoEx)
            {
                // This occurs when vault is copied to another Windows machine/account or corrupted
                LogWarn($"[SecureCredentialStorage] DPAPI decryption failed (vault might belong to another Windows user or be corrupted): {cryptoEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                LogWarn($"[SecureCredentialStorage] Failed to read or decrypt vault: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Securely wipes and deletes the encrypted vault file upon explicit logout.
        /// </summary>
        public static bool ClearCredentials()
        {
            string vaultPath = GetVaultFilePath();
            try
            {
                if (File.Exists(vaultPath))
                {
                    // Overwrite with zeros before deletion
                    byte[] zeros = new byte[256];
                    File.WriteAllBytes(vaultPath, zeros);
                    File.Delete(vaultPath);
                }

                LogInfo("[SecureCredentialStorage] Encrypted vault wiped and deleted.");
                return true;
            }
            catch (Exception ex)
            {
                LogWarn($"[SecureCredentialStorage] Error clearing vault file: {ex.Message}");
                return false;
            }
        }

        private static void CleanTemp(string path)
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }
    }
}
