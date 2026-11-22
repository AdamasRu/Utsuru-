using System;
using UnityEngine;
using Utsuru.Authentication.Models;
using Utsuru.Authentication.Security;
using Utsuru.Authentication.Storage;

namespace Utsuru.Authentication
{
    public enum AccountLoginMode
    {
        None,
        Registered,
        Guest
    }

    /// <summary>
    /// Central manager for the active local player session.
    /// Exposes player identity, persists across scene loads, and bridges in-memory
    /// authentication state with secure local disk storage.
    /// Does NOT perform any network calls or background token refreshes.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [Header("Runtime State")]
        [SerializeField] private AccountLoginMode currentLoginMode = AccountLoginMode.None;
        [SerializeField] private string authUserId;
        [SerializeField] private string localProfileId;
        [SerializeField] private string playerEmail;
        [SerializeField] private string playerUsername;

        public AccountLoginMode CurrentLoginMode => currentLoginMode;
        public bool IsLoggedIn => currentLoginMode != AccountLoginMode.None;
        public bool IsRegistered => currentLoginMode == AccountLoginMode.Registered;
        public bool IsGuest => currentLoginMode == AccountLoginMode.Guest;

        public string AuthUserId => authUserId;
        public string LocalProfileId => localProfileId;
        public string Email => playerEmail;
        public string Username => playerUsername;

        public LocalProfileData CurrentProfile { get; private set; }
        public AuthSession ActiveAuthSession { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Ensures a singleton instance exists in the scene.
        /// </summary>
        public static SessionManager EnsureInstance()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<SessionManager>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("SessionManager");
                    Instance = go.AddComponent<SessionManager>();
                }
            }
            return Instance;
        }

        /// <summary>
        /// Initializes the player context purely from local offline profile metadata.
        /// Strictly offline: no Supabase, no network calls.
        /// </summary>
        public void InitializeFromLocalProfile(LocalProfileData profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("[SessionManager] Cannot initialize from null profile.");
                return;
            }

            CurrentProfile = profile;
            currentLoginMode = profile.login_mode == "Guest" ? AccountLoginMode.Guest : AccountLoginMode.Registered;
            authUserId = profile.auth_user_id;
            localProfileId = profile.local_profile_id;
            playerEmail = profile.email;
            playerUsername = profile.username;

            if (LocalAccountManager.EnsureInstance() != null && !string.IsNullOrEmpty(profile.username))
            {
                LocalAccountManager.Instance.SetActiveAccount(profile.username);
            }

            Debug.Log($"[SessionManager] Initialized local session (Mode: {currentLoginMode}, User: {playerUsername}, ProfileId: {localProfileId})");
        }

        /// <summary>
        /// Initializes the player context from a LocalAccountRecord (100% offline local CRUD).
        /// </summary>
        public void InitializeFromLocalAccount(LocalAccountRecord account, bool rememberMe)
        {
            if (account == null)
            {
                Debug.LogWarning("[SessionManager] Cannot initialize from null local account.");
                return;
            }

            currentLoginMode = AccountLoginMode.Registered;
            authUserId = account.accountId;
            playerEmail = account.email;
            playerUsername = account.username;
            localProfileId = account.accountId;

            LocalProfileData profile = new LocalProfileData
            {
                login_mode = "Registered",
                is_logged_in = rememberMe,
                auth_user_id = account.accountId,
                local_profile_id = account.accountId,
                email = account.email,
                username = account.username,
                last_login_timestamp = DateTime.UtcNow.ToString("o")
            };

            CurrentProfile = profile;

            if (rememberMe)
            {
                LocalAccountStorage.SaveProfile(profile);
            }
            else
            {
                LocalAccountStorage.ClearRememberedAccount();
                SecureCredentialStorage.ClearCredentials();
            }

            if (LocalAccountManager.EnsureInstance() != null)
            {
                LocalAccountManager.Instance.SetActiveAccount(account.username);
            }

            Debug.Log($"[SessionManager] Local account session initialized for '{account.username}' (RememberMe: {rememberMe})");
        }

        /// <summary>
        /// Connects a successful in-memory authentication session to local storage.
        /// If rememberMe is true, persists profile marker and encrypted credentials to disk.
        /// </summary>
        public void SaveRegisteredSession(AuthSession session, bool rememberMe)
        {
            if (session == null || string.IsNullOrEmpty(session.UserId))
            {
                Debug.LogError("[SessionManager] Cannot save null or invalid authentication session.");
                return;
            }

            ActiveAuthSession = session;
            currentLoginMode = AccountLoginMode.Registered;
            authUserId = session.UserId;
            playerEmail = session.Email;
            playerUsername = session.Username;

            // Retain existing local_profile_id if it matches this auth_user_id, otherwise create new
            LocalProfileData existing = LocalAccountStorage.LoadProfile();
            if (existing != null && existing.auth_user_id == session.UserId && !string.IsNullOrEmpty(existing.local_profile_id))
            {
                localProfileId = existing.local_profile_id;
            }
            else
            {
                localProfileId = Guid.NewGuid().ToString();
            }

            LocalProfileData profile = new LocalProfileData
            {
                login_mode = "Registered",
                is_logged_in = rememberMe, // Only true if Remember Me was selected
                auth_user_id = session.UserId,
                local_profile_id = localProfileId,
                email = session.Email,
                username = session.Username,
                last_login_timestamp = DateTime.UtcNow.ToString("o")
            };

            CurrentProfile = profile;

            if (rememberMe)
            {
                // 1. Save unencrypted account profile metadata
                LocalAccountStorage.SaveProfile(profile);

                // 2. Save encrypted credentials using Windows DPAPI
                SecureCredentialStorage.SaveCredentials(
                    session.UserId,
                    session.AccessToken,
                    session.RefreshToken,
                    session.TokenType,
                    session.ExpiresIn
                );

                Debug.Log("[SessionManager] Registered account successfully remembered on disk.");
            }
            else
            {
                // If Remember Me was unchecked, clear any previous persistent markers
                LocalAccountStorage.ClearRememberedAccount();
                SecureCredentialStorage.ClearCredentials();
                Debug.Log("[SessionManager] Login completed without persisting credentials (Remember Me unchecked).");
            }
        }

        /// <summary>
        /// Sets the current player context to Guest mode.
        /// Preserves existing stable local_profile_id if previous session was Guest.
        /// Ensures Registered credentials are not leaked and saves Guest local profile marker with is_logged_in = false.
        /// </summary>
        public void SetGuestSession(string guestName = "Guest")
        {
            ActiveAuthSession = null;
            currentLoginMode = AccountLoginMode.Guest;
            authUserId = null;
            playerEmail = null;
            playerUsername = guestName;

            // Retain existing guest profile ID if present, otherwise generate stable GUID
            LocalProfileData existing = LocalAccountStorage.LoadProfile();
            if (existing != null && existing.login_mode == "Guest" && !string.IsNullOrEmpty(existing.local_profile_id))
            {
                localProfileId = existing.local_profile_id;
            }
            else if (string.IsNullOrEmpty(localProfileId))
            {
                localProfileId = Guid.NewGuid().ToString();
            }

            LocalProfileData guestProfile = new LocalProfileData
            {
                login_mode = "Guest",
                is_logged_in = false, // Do not auto-bypass login on cold boot
                auth_user_id = null,
                local_profile_id = localProfileId,
                email = null,
                username = guestName,
                last_login_timestamp = DateTime.UtcNow.ToString("o")
            };

            CurrentProfile = guestProfile;
            LocalAccountStorage.SaveProfile(guestProfile);

            // Clean any credentials from DPAPI vault so guest has clean slate
            SecureCredentialStorage.ClearCredentials();

            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.ClearSession();
            }

            Debug.Log($"[SessionManager] Guest session initialized (ProfileId: {localProfileId})");
        }

        /// <summary>
        /// Explicit logout: clears persistent markers, erases DPAPI vault, and clears in-memory state.
        /// Does NOT delete local player progress, dictionary data, points, or photos.
        /// </summary>
        public void Logout()
        {
            Debug.Log("[SessionManager] Executing explicit logout...");

            // 1. Clear persistent local marker
            LocalAccountStorage.ClearRememberedAccount();

            // 2. Securely erase DPAPI encrypted vault
            SecureCredentialStorage.ClearCredentials();

            // 3. Clear in-memory session
            ActiveAuthSession = null;
            CurrentProfile = null;
            currentLoginMode = AccountLoginMode.None;
            authUserId = null;
            playerEmail = null;
            playerUsername = null;

            // 4. Synchronize with AuthManager and LocalAccountManager
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.ClearSession();
            }
            if (LocalAccountManager.Instance != null)
            {
                LocalAccountManager.Instance.Logout();
            }

            Debug.Log("[SessionManager] Logout complete. User returned to logged-out state.");
        }
    }
}
