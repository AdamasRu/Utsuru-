using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Utsuru.Authentication.Models
{
    /// <summary>
    /// Payload sent to Supabase Auth POST /auth/v1/signup.
    /// </summary>
    [JsonObject]
    public class SignUpRequest
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public SignUpData Data { get; set; }
    }

    /// <summary>
    /// User metadata stored in Supabase Auth user_metadata.
    /// Stores the player's chosen username without requiring a separate profiles table yet.
    /// </summary>
    [JsonObject]
    public class SignUpData
    {
        [JsonProperty("username")]
        public string Username { get; set; }
    }

    /// <summary>
    /// Response payload from Supabase Auth POST /auth/v1/signup.
    /// Handles both unconfirmed flow (returns SupabaseUser properties) and
    /// auto-confirmed flow (returns session tokens and nested user).
    /// </summary>
    [JsonObject]
    public class SignUpResponse : SupabaseUser
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("user")]
        public SupabaseUser NestedUser { get; set; }

        /// <summary>
        /// Resolved user object regardless of whether Supabase returned root user or nested user.
        /// </summary>
        public SupabaseUser ResolvedUser => NestedUser ?? this;

        /// <summary>
        /// True if email confirmation is required (no access token was issued).
        /// </summary>
        public bool IsConfirmationRequired => string.IsNullOrEmpty(AccessToken);
    }

    /// <summary>
    /// Payload sent to Supabase Auth POST /auth/v1/resend.
    /// </summary>
    [JsonObject]
    public class ResendVerificationRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "signup";

        [JsonProperty("email")]
        public string Email { get; set; }
    }

    /// <summary>
    /// Payload sent to Supabase Auth POST /auth/v1/recover.
    /// Initiates email password recovery.
    /// </summary>
    [JsonObject]
    public class PasswordRecoveryRequest
    {
        [JsonProperty("email")]
        public string Email { get; set; }
    }

    /// <summary>
    /// Payload sent to Supabase Auth POST /auth/v1/verify.
    /// Used for verifying OTP recovery code or token.
    /// </summary>
    [JsonObject]
    public class VerifyOtpRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "recovery";

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }

    /// <summary>
    /// Payload sent to Supabase Auth PUT /auth/v1/user.
    /// Updates the authenticated user's password.
    /// </summary>
    [JsonObject]
    public class UpdateUserPasswordRequest
    {
        [JsonProperty("password")]
        public string Password { get; set; }
    }

    /// <summary>
    /// Payload sent to Supabase Auth POST /auth/v1/token?grant_type=password.
    /// </summary>
    [JsonObject]
    public class LoginRequest
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    /// <summary>
    /// Response payload returned by Supabase Auth upon successful login (POST /auth/v1/token?grant_type=password).
    /// Contains JWT access token, refresh token, expiry, and user data.
    /// </summary>
    [JsonObject]
    public class SupabaseLoginResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("expires_at")]
        public long ExpiresAt { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("user")]
        public SupabaseUser User { get; set; }
    }

    /// <summary>
    /// Supabase user object returned upon successful registration, login, or query.
    /// </summary>
    [JsonObject]
    public class SupabaseUser
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("aud")]
        public string Aud { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("email_confirmed_at")]
        public string EmailConfirmedAt { get; set; }

        [JsonProperty("confirmed_at")]
        public string ConfirmedAt { get; set; }

        [JsonProperty("last_sign_in_at")]
        public string LastSignInAt { get; set; }

        [JsonProperty("user_metadata")]
        public Dictionary<string, object> UserMetadata { get; set; }

        [JsonProperty("identities")]
        public List<SupabaseUserIdentity> Identities { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonProperty("is_anonymous")]
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// Gets the username from user_metadata if available.
        /// </summary>
        public string GetUsername()
        {
            if (UserMetadata != null && UserMetadata.TryGetValue("username", out object val) && val != null)
            {
                return val.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Indicates whether the user's email has been confirmed.
        /// </summary>
        public bool IsEmailConfirmed => !string.IsNullOrEmpty(EmailConfirmedAt) || !string.IsNullOrEmpty(ConfirmedAt);
    }

    /// <summary>
    /// Linked identity record for a Supabase user.
    /// </summary>
    [JsonObject]
    public class SupabaseUserIdentity
    {
        [JsonProperty("identity_id")]
        public string IdentityId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("provider")]
        public string Provider { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("last_sign_in_at")]
        public string LastSignInAt { get; set; }
    }

    /// <summary>
    /// In-memory runtime authentication session model.
    /// Exists strictly in application memory during Phase A3.
    /// Persists across scene transitions (e.g. LoginPage -> HomeScreen).
    /// Never stored to PlayerPrefs, plain text, or disk.
    /// Tokens are strictly excluded from ToString() and logs.
    /// </summary>
    public class AuthSession
    {
        public string UserId { get; }
        public string Email { get; }
        public string Username { get; }
        public string AccessToken { get; }
        public string RefreshToken { get; }
        public int ExpiresIn { get; }
        public string TokenType { get; }
        public DateTime AuthenticatedAt { get; }

        public AuthSession(string userId, string email, string username, string accessToken, string refreshToken, int expiresIn, string tokenType, DateTime authenticatedAt)
        {
            UserId = userId ?? string.Empty;
            Email = email ?? string.Empty;
            Username = username ?? string.Empty;
            AccessToken = accessToken ?? string.Empty;
            RefreshToken = refreshToken ?? string.Empty;
            ExpiresIn = expiresIn;
            TokenType = tokenType ?? "bearer";
            AuthenticatedAt = authenticatedAt;
        }

        public AuthSession(string userId, string email, string accessToken, string refreshToken, int expiresIn, string tokenType, DateTime authenticatedAt)
            : this(userId, email, string.Empty, accessToken, refreshToken, expiresIn, tokenType, authenticatedAt)
        {
        }

        /// <summary>
        /// Gets whether the current in-memory access token is expired based on AuthenticatedAt and ExpiresIn.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= AuthenticatedAt.AddSeconds(ExpiresIn);

        /// <summary>
        /// Gets the calculated expiration time in UTC.
        /// </summary>
        public DateTime ExpiresAtUtc => AuthenticatedAt.AddSeconds(ExpiresIn);

        /// <summary>
        /// Explicitly sanitizes string representation so tokens are never exposed in logs or UI inspector.
        /// </summary>
        public override string ToString()
        {
            return $"[AuthSession: UserId={UserId}, Email={Email}, TokenType={TokenType}, ExpiresIn={ExpiresIn}s, AuthenticatedAt={AuthenticatedAt:o}]";
        }
    }

    /// <summary>
    /// Result wrapper returned by AuthManager operations to provide safe, clean outcome data to the UI.
    /// </summary>
    public class AuthResult
    {
        public bool IsSuccess { get; }
        public int StatusCode { get; }
        public string ErrorMessage { get; }
        public string ErrorCode { get; }
        public SupabaseUser User { get; }
        public AuthSession Session { get; }
        public bool IsEmailConfirmationRequired { get; }
        public bool EmailConfirmationRequired => IsEmailConfirmationRequired;

        public AuthResult(
            bool isSuccess,
            int statusCode,
            string errorMessage,
            string errorCode,
            SupabaseUser user,
            AuthSession session,
            bool isEmailConfirmationRequired = false)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
            User = user;
            Session = session;
            IsEmailConfirmationRequired = isEmailConfirmationRequired;
        }

        public static AuthResult Success(SupabaseUser user, bool emailConfirmationRequired = true) =>
            new AuthResult(true, 200, null, null, user, null, emailConfirmationRequired);

        public static AuthResult Success(SupabaseUser user, AuthSession session, bool emailConfirmationRequired = false) =>
            new AuthResult(true, 200, null, null, user, session, emailConfirmationRequired);

        public static AuthResult Failure(int statusCode, string errorMessage, string errorCode = null) =>
            new AuthResult(false, statusCode, errorMessage, errorCode, null, null, false);
    }
}
