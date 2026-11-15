using System;
using UnityEngine;

namespace Utsuru.Authentication.Configuration
{
    /// <summary>
    /// ScriptableObject configuration holding client-safe Supabase project settings.
    /// Stores the Project URL and modern Publishable Key only.
    /// Never store service_role keys, secret keys, or database credentials here.
    /// </summary>
    [CreateAssetMenu(fileName = "SupabaseProjectConfig", menuName = "UTSURU/Authentication/Supabase Project Config")]
    public class SupabaseProjectConfig : ScriptableObject
    {
        [Header("Supabase Project Configuration")]
        [Tooltip("The HTTPS URL of your Supabase project (e.g., https://your-project-ref.supabase.co).")]
        [SerializeField] private string projectUrl = "";

        [Tooltip("The modern publishable key starting with 'sb_publishable_'. Do NOT use secret or service_role keys.")]
        [SerializeField] private string publishableKey = "";

        [Header("Optional Auth Redirect Configuration")]
        [Tooltip("Optional redirect URL passed to Supabase during signup and verification (redirect_to). Leave empty during development to let Supabase use its configured Site URL.")]
        [SerializeField] private string emailRedirectUrl = "";

        /// <summary>
        /// Gets the sanitized project URL without trailing slash.
        /// </summary>
        public string ProjectUrl => SanitizeUrl(projectUrl);

        /// <summary>
        /// Gets the trimmed publishable key.
        /// </summary>
        public string PublishableKey => publishableKey?.Trim() ?? string.Empty;

        /// <summary>
        /// Gets the optional email confirmation redirect URL.
        /// </summary>
        public string EmailRedirectUrl => emailRedirectUrl?.Trim() ?? string.Empty;

        /// <summary>
        /// Indicates whether a valid absolute redirect URL is configured.
        /// </summary>
        public bool HasValidRedirectUrl =>
            !string.IsNullOrWhiteSpace(emailRedirectUrl) &&
            Uri.TryCreate(emailRedirectUrl.Trim(), UriKind.Absolute, out _);

        /// <summary>
        /// Validates the configuration values according to UTSURU security and formatting requirements.
        /// </summary>
        /// <param name="errorMessage">Detailed explanation if validation fails; null if valid.</param>
        /// <returns>True if configuration is valid; otherwise false.</returns>
        public bool IsValidConfig(out string errorMessage)
        {
            errorMessage = null;

            // 1. Validate Project URL
            if (string.IsNullOrWhiteSpace(projectUrl))
            {
                errorMessage = "Project URL is empty. Please enter your Supabase project URL.";
                return false;
            }

            string cleanUrl = SanitizeUrl(projectUrl);
            if (!Uri.TryCreate(cleanUrl, UriKind.Absolute, out Uri uriResult) || uriResult.Scheme != Uri.UriSchemeHttps)
            {
                errorMessage = "Project URL must be a valid absolute HTTPS URL (e.g., https://your-ref.supabase.co).";
                return false;
            }

            if (!uriResult.Host.EndsWith("supabase.co", StringComparison.OrdinalIgnoreCase) &&
                !uriResult.Host.EndsWith("supabase.in", StringComparison.OrdinalIgnoreCase) &&
                !uriResult.Host.Contains("supabase"))
            {
                errorMessage = "Project URL host does not appear to be a valid Supabase project domain.";
                return false;
            }

            // 2. Validate Publishable Key presence
            if (string.IsNullOrWhiteSpace(publishableKey))
            {
                errorMessage = "Publishable Key is empty. Please enter your Supabase Publishable Key.";
                return false;
            }

            string cleanKey = publishableKey.Trim();

            // 3. Reject secret or service_role keys immediately
            if (cleanKey.StartsWith("sb_secret_", StringComparison.OrdinalIgnoreCase) ||
                cleanKey.IndexOf("service_role", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                errorMessage = "REJECTED: Secret or service_role key detected. Only publishable keys (sb_publishable_...) are allowed in client applications.";
                return false;
            }

            // 4. Validate expected publishable key format
            if (!cleanKey.StartsWith("sb_publishable_", StringComparison.Ordinal))
            {
                errorMessage = "Publishable Key format invalid. Expected key starting with 'sb_publishable_'.";
                return false;
            }

            if (cleanKey.Length < 25)
            {
                errorMessage = "Publishable Key appears too short to be a valid key.";
                return false;
            }

            // 5. Optional Email Redirect URL validation (only if provided)
            if (!string.IsNullOrWhiteSpace(emailRedirectUrl))
            {
                string cleanRedirect = emailRedirectUrl.Trim();
                if (!Uri.TryCreate(cleanRedirect, UriKind.Absolute, out _))
                {
                    errorMessage = "Email Redirect URL must be a valid absolute URI (e.g., https://your-game.com/auth or custom scheme like utsuru://auth-callback).";
                    return false;
                }
            }

            return true;
        }

        private static string SanitizeUrl(string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) return string.Empty;
            return rawUrl.Trim().TrimEnd('/');
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Only validate if user has entered values
            if (!string.IsNullOrEmpty(projectUrl) || !string.IsNullOrEmpty(publishableKey))
            {
                if (!IsValidConfig(out string error))
                {
                    // Safe warning: does not reveal key contents
                    Debug.LogWarning($"[SupabaseProjectConfig] Configuration warning in '{name}': {error}", this);
                }
            }
        }
#endif
    }
}
