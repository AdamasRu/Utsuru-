using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utsuru.Authentication.Models;
using Utsuru.Authentication.Storage;

namespace Utsuru.Authentication
{
    /// <summary>
    /// Evaluates local account status upon application boot in the LoginPage scene (Scene 0).
    /// If a valid remembered-account marker exists on disk, bypasses the authentication UI
    /// and immediately loads the HomeScreen scene without any network calls.
    /// Strictly LOCAL-FIRST, CLOUD-ON-DEMAND, and works 100% OFFLINE.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class StartupManager : MonoBehaviour
    {
        [Header("Target Scenes")]
        [Tooltip("The home screen scene loaded when an account is remembered.")]
        [SerializeField] private string homeScreenSceneName = "HomeScreen";

        [Header("UI Prevention")]
        [Tooltip("Optional Canvas or root panels to disable immediately if bypassing login.")]
        [SerializeField] private GameObject authCanvasRoot;

        private void Awake()
        {
            CheckAndPerformStartupBypass(authCanvasRoot != null ? authCanvasRoot : gameObject, homeScreenSceneName);
        }

        /// <summary>
        /// Static bootstrap check that can be called early from AuthenticationUIManager or scene entry.
        /// Returns true if a remembered account was found and scene transition to HomeScreen was initiated.
        /// </summary>
        public static bool CheckAndPerformStartupBypass(GameObject canvasToHide = null, string targetSceneName = "HomeScreen")
        {
            try
            {
                // 1. Check if local_profile.json exists and has an active login marker
                if (!LocalAccountStorage.HasRememberedAccount())
                {
                    Debug.Log("[StartupManager] No remembered account found on disk. Proceeding with standard login UI.");
                    return false;
                }

                LocalProfileData profile = LocalAccountStorage.LoadProfile();
                if (profile == null || !profile.is_logged_in || string.IsNullOrWhiteSpace(profile.auth_user_id))
                {
                    Debug.Log("[StartupManager] Local profile does not have an active login session.");
                    return false;
                }

                // 2. Valid remembered account found!
                Debug.Log($"[StartupManager] Valid remembered account detected for '{profile.username}' (Auth UUID: {profile.auth_user_id}). Bypassing login UI.");

                // 3. Immediately hide UI to eliminate any frame flicker
                if (canvasToHide != null)
                {
                    canvasToHide.SetActive(false);
                }

                // 4. Initialize SessionManager strictly offline from local profile
                SessionManager sessionMgr = SessionManager.EnsureInstance();
                sessionMgr.InitializeFromLocalProfile(profile);

                // 5. Navigate directly to HomeScreen without touching network
                Debug.Log($"[StartupManager] Direct offline navigation to scene '{targetSceneName}'...");
                SceneManager.LoadScene(targetSceneName);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StartupManager] Error during startup bypass evaluation: {ex.Message}");
                return false;
            }
        }
    }
}
