using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utsuru.Authentication.Configuration;
using Utsuru.Authentication.Models;
using Utsuru.Authentication.Networking;

namespace Utsuru.Authentication
{
    /// <summary>
    /// Core manager for Supabase Authentication operations in UTSURU.
    /// Handles registration, login, and email verification via standard HTTPS REST endpoints.
    /// Strictly adheres to UTSURU architecture: offline-first, no automatic startup requests,
    /// no background polling, and never stores passwords locally.
    /// In Phase A3, sessions exist strictly in application memory.
    /// </summary>
    [AddComponentMenu("UTSURU/Authentication/Auth Manager")]
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("ScriptableObject containing the Supabase Project URL and Publishable Key.")]
        [SerializeField] private SupabaseProjectConfig projectConfig;

        [Header("Diagnostics")]
        [Tooltip("Enable sanitized debug logs in console.")]
        [SerializeField] private bool enableDebugLogging = true;

        private SupabaseAuthRestClient restClient;
        private string lastRegisteredEmail = string.Empty;
        private string lastRecoveryEmail = string.Empty;
        private AuthSession currentSession;

        /// <summary>
        /// Gets the most recently registered or logged-in email address in this session.
        /// </summary>
        public string LastRegisteredEmail => lastRegisteredEmail;

        /// <summary>
        /// Gets the email address for which password recovery was most recently requested.
        /// </summary>
        public string LastRecoveryEmail => lastRecoveryEmail;

        /// <summary>
        /// Gets the current in-memory runtime authentication session.
        /// Exists strictly in application memory; never persisted to disk or PlayerPrefs in Phase A3.
        /// </summary>
        public AuthSession CurrentSession => currentSession;

        /// <summary>
        /// Global decoupled accessor for active session in memory across scenes.
        /// </summary>
        public static AuthSession ActiveSession { get; private set; }

        /// <summary>
        /// Indicates whether an active, non-expired session is held in memory.
        /// </summary>
        public bool IsAuthenticated => currentSession != null && !currentSession.IsExpired;

        /// <summary>
        /// Clears the active in-memory authentication session.
        /// </summary>
        public void ClearSession()
        {
            currentSession = null;
            ActiveSession = null;
            LogDebug("In-memory authentication session cleared.");
        }

        public SupabaseProjectConfig ProjectConfig
        {
            get => projectConfig;
            set
            {
                projectConfig = value;
                restClient = null; // Reinitialize client on next call
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureInitialized();
        }

        /// <summary>
        /// Ensures configuration and REST client are initialized.
        /// </summary>
        public void EnsureInitialized()
        {
            if (restClient != null) return;

            if (projectConfig == null)
            {
                // Attempt to load from default asset location or Resources
#if UNITY_EDITOR
                projectConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<SupabaseProjectConfig>(
                    "Assets/Settings/Authentication/SupabaseProjectConfig.asset");
#endif
                if (projectConfig == null)
                {
                    projectConfig = Resources.Load<SupabaseProjectConfig>("SupabaseProjectConfig");
                }
            }

            if (projectConfig != null)
            {
                restClient = new SupabaseAuthRestClient(projectConfig, enableDebugLogging);
            }
            else
            {
                Debug.LogWarning("[AuthManager] SupabaseProjectConfig is not assigned. Please assign it in the Inspector.");
            }
        }

        /// <summary>
        /// Authenticates an existing user account using Supabase Auth Password Grant
        /// (POST /auth/v1/token?grant_type=password).
        /// Requires email confirmation to be completed; unconfirmed accounts will be rejected.
        /// Stores the resulting access token and user metadata in an in-memory runtime session.
        /// Never logs password, access token, or refresh token.
        /// </summary>
        public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            string cleanEmail = (email ?? string.Empty).Trim();
            string rawPassword = password ?? string.Empty;

            // 1. Local validation
            if (string.IsNullOrEmpty(cleanEmail))
            {
                return AuthResult.Failure(400, "Please enter your email address.", "empty_email");
            }

            if (!IsValidEmail(cleanEmail))
            {
                return AuthResult.Failure(400, "Please enter a valid email address.", "invalid_email_format");
            }

            if (string.IsNullOrEmpty(rawPassword))
            {
                return AuthResult.Failure(400, "Please enter your password.", "empty_password");
            }

            if (restClient == null || projectConfig == null)
            {
                return AuthResult.Failure(0, "Authentication client is not initialized. Please verify configuration.", "client_uninitialized");
            }

            if (!projectConfig.IsValidConfig(out string configError))
            {
                return AuthResult.Failure(0, $"Configuration error: {configError}", "config_error");
            }

            LogDebug($"Attempting login for email: {cleanEmail}...");

            var requestPayload = new LoginRequest
            {
                Email = cleanEmail,
                Password = rawPassword
            };

            // 2. Dispatch POST /auth/v1/token?grant_type=password
            var response = await restClient.SendPostAsync<LoginRequest, SupabaseLoginResponse>(
                "/auth/v1/token?grant_type=password",
                requestPayload,
                cancellationToken);

            if (response.IsSuccess && response.Data != null)
            {
                var data = response.Data;
                if (data.User == null || string.IsNullOrEmpty(data.AccessToken))
                {
                    LogWarning("Login response succeeded but user payload or access token is missing.");
                    return AuthResult.Failure(response.StatusCode, "Invalid authentication response received from server.", "invalid_payload");
                }

                // Verify email confirmation status
                if (!data.User.IsEmailConfirmed)
                {
                    LogWarning($"Login rejected for user UUID {data.User.Id}: email is not confirmed.");
                    return AuthResult.Failure(400, "Please verify your email address before logging in.", "email_not_confirmed");
                }

                // Create secure in-memory runtime session
                var session = new AuthSession(
                    userId: data.User.Id,
                    email: data.User.Email,
                    username: data.User.GetUsername(),
                    accessToken: data.AccessToken,
                    refreshToken: data.RefreshToken,
                    expiresIn: data.ExpiresIn,
                    tokenType: data.TokenType,
                    authenticatedAt: DateTime.UtcNow
                );

                currentSession = session;
                ActiveSession = session;
                lastRegisteredEmail = data.User.Email;

                LogDebug($"Login successful for user UUID {data.User.Id} ({data.User.Email}). Runtime session established in memory.");
                return AuthResult.Success(data.User, session, false);
            }

            // 3. Error handling
            string errorCode = response.ErrorCode;
            string rawError = response.ErrorMessage ?? string.Empty;

            if (errorCode == "email_not_confirmed" || rawError.IndexOf("Email not confirmed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                LogWarning($"Login rejected: Email not confirmed for {cleanEmail}.");
                return AuthResult.Failure(response.StatusCode, "Please verify your email address before logging in.", "email_not_confirmed");
            }

            if (errorCode == "invalid_credentials" || rawError.IndexOf("Invalid login credentials", StringComparison.OrdinalIgnoreCase) >= 0 || response.StatusCode == 400)
            {
                // Generic message to avoid email enumeration
                return AuthResult.Failure(response.StatusCode, "Email or password is incorrect.", "invalid_credentials");
            }

            if (response.StatusCode == 429 || rawError.IndexOf("rate limit", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AuthResult.Failure(429, "Too many login attempts. Please wait a moment and try again.", "rate_limited");
            }

            if (response.StatusCode == 0 || errorCode == "network_error" || errorCode == "timeout")
            {
                return AuthResult.Failure(0, "Unable to connect. Check your internet connection.", "network_error");
            }

            string safeError = CleanErrorMessage(rawError, response.StatusCode);
            return AuthResult.Failure(response.StatusCode, safeError, errorCode);
        }

        /// <summary>
        /// Registers a new user account with Supabase Auth (POST /auth/v1/signup).
        /// Stores the chosen username in Auth user_metadata.
        /// Does NOT create a session or log the player in, because email confirmation is required.
        /// </summary>
        public async Task<AuthResult> SignUpAsync(string email, string password, string username, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            // 1. Local validation
            if (string.IsNullOrWhiteSpace(username))
            {
                return AuthResult.Failure(400, "Please enter your username.", "empty_username");
            }

            string cleanUsername = username.Trim();
            if (cleanUsername.Length < 3)
            {
                return AuthResult.Failure(400, "Username must be at least 3 characters long.", "username_too_short");
            }

            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email.Trim()))
            {
                return AuthResult.Failure(400, "Please enter a valid email address.", "invalid_email");
            }

            string cleanEmail = email.Trim();

            if (string.IsNullOrEmpty(password))
            {
                return AuthResult.Failure(400, "Please enter a password.", "empty_password");
            }

            if (password.Length < 6)
            {
                return AuthResult.Failure(400, "Password must be at least 6 characters long.", "password_too_short");
            }

            if (restClient == null || projectConfig == null)
            {
                return AuthResult.Failure(0, "Authentication client is not initialized. Please verify configuration.", "client_uninitialized");
            }

            if (!projectConfig.IsValidConfig(out string configError))
            {
                return AuthResult.Failure(0, $"Configuration error: {configError}", "config_error");
            }

            // 2. Build endpoint with optional redirect_to parameter
            string endpoint = "/auth/v1/signup";
            if (projectConfig.HasValidRedirectUrl)
            {
                endpoint += $"?redirect_to={Uri.EscapeDataString(projectConfig.EmailRedirectUrl)}";
                LogDebug($"Using custom email confirmation redirect: {projectConfig.EmailRedirectUrl}");
            }
            else
            {
                LogDebug("No custom redirect URL configured. Supabase will use default Site URL.");
            }

            // 3. Build payload
            var requestPayload = new SignUpRequest
            {
                Email = cleanEmail,
                Password = password,
                Data = new SignUpData
                {
                    Username = cleanUsername
                }
            };

            LogDebug($"Submitting registration request for user: '{cleanUsername}'...");

            // 4. Dispatch request
            var response = await restClient.SendPostAsync<SignUpRequest, SupabaseUser>(endpoint, requestPayload, cancellationToken);

            if (response.IsSuccess)
            {
                var user = response.Data;

                // Check for Supabase email enumeration protection:
                // When email confirmation is enabled and user already exists, Supabase may return 200 with empty identities list.
                if (user != null && user.Identities != null && user.Identities.Count == 0)
                {
                    LogWarning("Registration returned user with empty identities (email already registered).");
                    return AuthResult.Failure(400, "An account with this email already exists. Please log in or use forgot password.", "user_already_exists");
                }

                // Cache registered email for verification display and resend flow
                lastRegisteredEmail = cleanEmail;

                LogDebug("Registration successful. Email confirmation is required.");
                return AuthResult.Success(user, emailConfirmationRequired: true);
            }

            // 5. Handle errors
            string safeError = CleanErrorMessage(response.ErrorMessage, response.StatusCode);
            LogWarning($"Registration failed (HTTP {response.StatusCode}): {safeError}");
            return AuthResult.Failure(response.StatusCode, safeError, response.ErrorCode);
        }

        /// <summary>
        /// Resends the signup confirmation email via Supabase Auth (POST /auth/v1/resend).
        /// </summary>
        public async Task<AuthResult> ResendVerificationEmailAsync(string email = null, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            string targetEmail = !string.IsNullOrWhiteSpace(email) ? email.Trim() : lastRegisteredEmail;

            if (string.IsNullOrWhiteSpace(targetEmail) || !IsValidEmail(targetEmail))
            {
                return AuthResult.Failure(400, "No valid email address found to resend verification link.", "invalid_email");
            }

            if (restClient == null || projectConfig == null)
            {
                return AuthResult.Failure(0, "Authentication client is not initialized.", "client_uninitialized");
            }

            string endpoint = "/auth/v1/resend";
            if (projectConfig.HasValidRedirectUrl)
            {
                endpoint += $"?redirect_to={Uri.EscapeDataString(projectConfig.EmailRedirectUrl)}";
            }

            var requestPayload = new ResendVerificationRequest
            {
                Type = "signup",
                Email = targetEmail
            };

            LogDebug("Sending resend verification request...");

            var response = await restClient.SendPostAsync(endpoint, requestPayload, cancellationToken);

            if (response.IsSuccess)
            {
                lastRegisteredEmail = targetEmail;
                LogDebug("Verification email resent successfully.");
                return AuthResult.Success(null, emailConfirmationRequired: true);
            }

            string safeError = CleanErrorMessage(response.ErrorMessage, response.StatusCode);
            LogWarning($"Resend verification failed (HTTP {response.StatusCode}): {safeError}");
            return AuthResult.Failure(response.StatusCode, safeError, response.ErrorCode);
        }

        /// <summary>
        /// Sends a password recovery email via Supabase Auth (POST /auth/v1/recover).
        /// Generates an email containing an OTP code or recovery link.
        /// </summary>
        public async Task<AuthResult> SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email.Trim()))
            {
                return AuthResult.Failure(400, "Please enter a valid email address.", "invalid_email");
            }

            string cleanEmail = email.Trim();

            if (restClient == null || projectConfig == null)
            {
                return AuthResult.Failure(0, "Authentication client is not initialized.", "client_uninitialized");
            }

            string endpoint = "/auth/v1/recover";
            if (projectConfig.HasValidRedirectUrl)
            {
                endpoint += $"?redirect_to={Uri.EscapeDataString(projectConfig.EmailRedirectUrl)}";
            }

            var requestPayload = new PasswordRecoveryRequest
            {
                Email = cleanEmail
            };

            LogDebug($"Sending password recovery request for {cleanEmail}...");

            var response = await restClient.SendPostAsync(endpoint, requestPayload, cancellationToken);

            if (response.IsSuccess)
            {
                lastRecoveryEmail = cleanEmail;
                LogDebug("Password recovery request dispatched successfully.");
                return AuthResult.Success(null, emailConfirmationRequired: false);
            }

            if (response.StatusCode == 429)
            {
                return AuthResult.Failure(429, "Too many recovery attempts. Please wait a moment before trying again.", "rate_limited");
            }

            if (response.StatusCode == 0 || response.ErrorCode == "network_error" || response.ErrorCode == "timeout")
            {
                return AuthResult.Failure(0, "Unable to connect. Check your internet connection.", "network_error");
            }

            string safeError = CleanErrorMessage(response.ErrorMessage, response.StatusCode);
            LogWarning($"Password recovery failed (HTTP {response.StatusCode}): {safeError}");
            return AuthResult.Failure(response.StatusCode, safeError, response.ErrorCode);
        }

        /// <summary>
        /// Verifies a recovery OTP code (POST /auth/v1/verify) and updates the user's password (PUT /auth/v1/user).
        /// </summary>
        public async Task<AuthResult> ResetPasswordWithCodeAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            string targetEmail = !string.IsNullOrWhiteSpace(email) ? email.Trim() : lastRecoveryEmail;
            if (string.IsNullOrWhiteSpace(targetEmail) || !IsValidEmail(targetEmail))
            {
                return AuthResult.Failure(400, "No valid email address found for password reset.", "invalid_email");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return AuthResult.Failure(400, "Please enter your reset code.", "empty_code");
            }

            string cleanCode = code.Trim();

            if (string.IsNullOrEmpty(newPassword))
            {
                return AuthResult.Failure(400, "Please enter a new password.", "empty_password");
            }

            if (newPassword.Length < 6)
            {
                return AuthResult.Failure(400, "Password must be at least 6 characters long.", "password_too_short");
            }

            if (restClient == null || projectConfig == null)
            {
                return AuthResult.Failure(0, "Authentication client is not initialized.", "client_uninitialized");
            }

            LogDebug($"Verifying recovery code for {targetEmail}...");

            // Step 1: Verify recovery OTP token
            var verifyPayload = new VerifyOtpRequest
            {
                Type = "recovery",
                Email = targetEmail,
                Token = cleanCode
            };

            var verifyResponse = await restClient.SendPostAsync<VerifyOtpRequest, SupabaseLoginResponse>(
                "/auth/v1/verify",
                verifyPayload,
                cancellationToken);

            if (!verifyResponse.IsSuccess || verifyResponse.Data == null || string.IsNullOrEmpty(verifyResponse.Data.AccessToken))
            {
                string rawError = verifyResponse.ErrorMessage ?? string.Empty;
                LogWarning($"Verify OTP failed (HTTP {verifyResponse.StatusCode}): {rawError}");

                if (verifyResponse.StatusCode == 429)
                {
                    return AuthResult.Failure(429, "Too many attempts. Please wait a moment and try again.", "rate_limited");
                }

                if (verifyResponse.StatusCode == 0)
                {
                    return AuthResult.Failure(0, "Unable to connect. Check your internet connection.", "network_error");
                }

                return AuthResult.Failure(verifyResponse.StatusCode, "Invalid or expired reset code. Please check your email or request a new code.", "invalid_reset_code");
            }

            string tempAccessToken = verifyResponse.Data.AccessToken;
            LogDebug("Recovery token verified successfully. Updating password...");

            // Step 2: Update password using the verified session Bearer token
            var updatePayload = new UpdateUserPasswordRequest
            {
                Password = newPassword
            };

            var updateResponse = await restClient.SendPutAsync<UpdateUserPasswordRequest, SupabaseUser>(
                "/auth/v1/user",
                updatePayload,
                tempAccessToken,
                cancellationToken);

            if (updateResponse.IsSuccess && updateResponse.Data != null)
            {
                LogDebug("Password updated successfully via Supabase Auth.");
                return AuthResult.Success(updateResponse.Data, emailConfirmationRequired: false);
            }

            string updateError = CleanErrorMessage(updateResponse.ErrorMessage, updateResponse.StatusCode);
            LogWarning($"Password update failed (HTTP {updateResponse.StatusCode}): {updateError}");
            return AuthResult.Failure(updateResponse.StatusCode, updateError, updateResponse.ErrorCode);
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private static string CleanErrorMessage(string rawMessage, int statusCode)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                return $"Request failed (HTTP {statusCode}).";
            }

            if (rawMessage.IndexOf("user already registered", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawMessage.IndexOf("user_already_exists", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "An account with this email already exists. Please log in or use forgot password.";
            }

            if (statusCode == 429 || rawMessage.IndexOf("rate limit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawMessage.IndexOf("once every", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Too many requests. Please wait a minute before requesting another verification email.";
            }

            if (rawMessage.IndexOf("Password should be at least", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Password must be at least 6 characters long.";
            }

            return rawMessage;
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[AuthManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[AuthManager] {message}");
        }
    }
}
