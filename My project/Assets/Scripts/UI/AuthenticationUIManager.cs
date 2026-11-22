using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using TMPro;
using Utsuru.Authentication;

namespace Utsuru.UI
{
    /// <summary>
    /// Centralized UI manager for the local prototype AuthenticationScene.
    /// Manages navigation between all five authentication panels, validates local inputs,
    /// coordinates dynamic email display, and provides temporary prototype feedback.
    /// Does not connect to or simulate an authentication backend.
    /// </summary>
    public class AuthenticationUIManager : MonoBehaviour
    {
        [Header("Authentication Backend")]
        [Tooltip("Core manager handling Supabase Authentication REST API calls.")]
        [SerializeField] private AuthManager authManager;

        [Header("Authentication Panels")]
        [Tooltip("The Login panel GameObject.")]
        [SerializeField] private GameObject loginPanel;

        [Tooltip("The Sign Up panel GameObject.")]
        [SerializeField] private GameObject signUpPanel;

        [Tooltip("The Email Sent verification panel GameObject.")]
        [SerializeField] private GameObject emailSentPanel;

        [Tooltip("The Forgot Password panel GameObject.")]
        [SerializeField] private GameObject forgotPasswordPanel;

        [Tooltip("The Reset Password panel GameObject.")]
        [SerializeField] private GameObject resetPasswordPanel;

        [Header("Verification Email Helper")]
        [SerializeField] private VerificationEmailUI verificationEmailUI;

        [Header("Login Panel Controls")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button playAsGuestButton;
        [SerializeField] private Button signUpNavigationButton;
        [SerializeField] private Button forgotPasswordNavigationButton;
        [FormerlySerializedAs("loginUsernameInput")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Toggle rememberMeToggle;
        [SerializeField] private Button rememberMeTextButton;
        [Tooltip("UI Alert box displayed when email or password is incorrect.")]
        [SerializeField] private GameObject incorrectCredentialsAlertBox;

        [Header("Sign Up Panel Controls")]
        [SerializeField] private Button signUpSubmitButton;
        [SerializeField] private Button signUpBackToLoginButton;
        [SerializeField] private TMP_InputField signUpUsernameInput;
        [SerializeField] private TMP_InputField signUpEmailInput;
        [SerializeField] private TMP_InputField signUpPasswordInput;

        [Header("Email Sent Panel Controls")]
        [SerializeField] private Button emailSentBackToLoginButton;
        [SerializeField] private Button resendVerificationEmailButton;

        [Header("Forgot Password Panel Controls")]
        [SerializeField] private Button forgotPasswordBackButton;
        [SerializeField] private Button forgotPasswordSubmitButton;
        [SerializeField] private TMP_InputField forgotPasswordEmailInput;

        [Header("Reset Password Panel Controls")]
        [SerializeField] private Button resetPasswordBackButton;
        [SerializeField] private Button resetPasswordSubmitButton;
        [SerializeField] private TMP_InputField resetPasswordCodeInput;
        [SerializeField] private TMP_InputField resetPasswordNewInput;
        [SerializeField] private TMP_InputField resetPasswordConfirmInput;

        [Header("Prototype Notification / Toast UI")]
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private CanvasGroup toastCanvasGroup;

        private Coroutine toastCoroutine;
        private TMP_Text loginButtonText;
        private string originalLoginButtonText = "Log In";
        private TMP_Text signUpSubmitButtonText;
        private string originalSignUpSubmitText = "Sign Up";
        private TMP_Text resendVerificationButtonText;
        private string originalResendVerificationText = "Resend Verification Email";
        private TMP_Text forgotPasswordSubmitButtonText;
        private string originalForgotPasswordSubmitText = "Continue";
        private TMP_Text resetPasswordSubmitButtonText;
        private string originalResetPasswordSubmitText = "Continue";
        private float resendCooldownRemaining = 0f;
        private Coroutine resendCooldownCoroutine;
        private string recoveryTargetAccount = string.Empty;

        private void Awake()
        {
            // Phase A4: Check if a remembered account exists and bypass login UI immediately
            if (StartupManager.CheckAndPerformStartupBypass(gameObject))
            {
                return;
            }

            AutoAssignReferences();
            CacheButtonTexts();
            SetupButtonListeners();
            EnsureToastUI();
            HideIncorrectCredentialsAlert();
            HideToast();
        }

        private void CacheButtonTexts()
        {
            if (loginButton != null)
            {
                loginButtonText = loginButton.GetComponentInChildren<TMP_Text>();
                if (loginButtonText != null)
                {
                    originalLoginButtonText = loginButtonText.text;
                }
            }

            if (signUpSubmitButton != null)
            {
                signUpSubmitButtonText = signUpSubmitButton.GetComponentInChildren<TMP_Text>();
                if (signUpSubmitButtonText != null)
                {
                    originalSignUpSubmitText = signUpSubmitButtonText.text;
                }
            }

            if (resendVerificationEmailButton != null)
            {
                resendVerificationButtonText = resendVerificationEmailButton.GetComponentInChildren<TMP_Text>();
                if (resendVerificationButtonText != null)
                {
                    originalResendVerificationText = resendVerificationButtonText.text;
                }
            }

            if (forgotPasswordSubmitButton != null)
            {
                forgotPasswordSubmitButtonText = forgotPasswordSubmitButton.GetComponentInChildren<TMP_Text>();
                if (forgotPasswordSubmitButtonText != null)
                {
                    originalForgotPasswordSubmitText = forgotPasswordSubmitButtonText.text;
                }
            }

            if (resetPasswordSubmitButton != null)
            {
                resetPasswordSubmitButtonText = resetPasswordSubmitButton.GetComponentInChildren<TMP_Text>();
                if (resetPasswordSubmitButtonText != null)
                {
                    originalResetPasswordSubmitText = resetPasswordSubmitButtonText.text;
                }
            }
        }

        private void Start()
        {
            // LoginPanel is the initial active panel
            ShowLoginPanel();
        }

        #region Panel Navigation

        /// <summary>
        /// Activates the specified panel and hides all other authentication panels.
        /// </summary>
        /// <param name="activePanel">The GameObject of the panel to show.</param>
        public void ShowPanel(GameObject activePanel)
        {
            if (loginPanel != null) loginPanel.SetActive(loginPanel == activePanel);
            if (signUpPanel != null) signUpPanel.SetActive(signUpPanel == activePanel);
            if (emailSentPanel != null) emailSentPanel.SetActive(emailSentPanel == activePanel);
            if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(forgotPasswordPanel == activePanel);
            if (resetPasswordPanel != null) resetPasswordPanel.SetActive(resetPasswordPanel == activePanel);
        }

        public void ShowLoginPanel()
        {
            ShowPanel(loginPanel);
            HideIncorrectCredentialsAlert();
            HideToast();
        }

        public void ShowSignUpPanel()
        {
            ShowPanel(signUpPanel);
        }

        public void ShowEmailSentPanel()
        {
            ShowPanel(emailSentPanel);
        }

        public void ShowForgotPasswordPanel()
        {
            ShowPanel(forgotPasswordPanel);
            HideIncorrectCredentialsAlert();
            HideToast();
        }

        public void ShowResetPasswordPanel()
        {
            ShowPanel(resetPasswordPanel);
            HideIncorrectCredentialsAlert();
            HideToast();
        }

        #endregion

        #region Button Event Listeners Setup

        private void SetupButtonListeners()
        {
            // Login Panel
            SafeAddListener(loginButton, OnLoginButtonClicked);
            SafeAddListener(playAsGuestButton, OnPlayAsGuestButtonClicked);
            SafeAddListener(signUpNavigationButton, ShowSignUpPanel);
            SafeAddListener(forgotPasswordNavigationButton, ShowForgotPasswordPanel);
            SafeAddListener(rememberMeTextButton, ToggleRememberMe);

            if (loginEmailInput != null)
            {
                loginEmailInput.onValueChanged.RemoveListener(OnLoginInputChanged);
                loginEmailInput.onValueChanged.AddListener(OnLoginInputChanged);
            }

            if (loginPasswordInput != null)
            {
                loginPasswordInput.onValueChanged.RemoveListener(OnLoginInputChanged);
                loginPasswordInput.onValueChanged.AddListener(OnLoginInputChanged);
            }

            // Sign Up Panel
            SafeAddListener(signUpBackToLoginButton, ShowLoginPanel);
            SafeAddListener(signUpSubmitButton, OnSignUpSubmitClicked);

            // Email Sent Panel
            SafeAddListener(emailSentBackToLoginButton, ShowLoginPanel);
            SafeAddListener(resendVerificationEmailButton, OnResendVerificationClicked);

            // Forgot Password Panel
            SafeAddListener(forgotPasswordBackButton, ShowLoginPanel);
            SafeAddListener(forgotPasswordSubmitButton, OnForgotPasswordSubmitClicked);

            // Reset Password Panel
            SafeAddListener(resetPasswordBackButton, ShowLoginPanel);
            SafeAddListener(resetPasswordSubmitButton, OnResetPasswordSubmitClicked);
        }

        private void SafeAddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;

            string targetMethodName = action.Method.Name;
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == (UnityEngine.Object)this &&
                    button.onClick.GetPersistentMethodName(i) == targetMethodName)
                {
                    return; // Already registered in Inspector! Do not duplicate.
                }
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        #endregion

        #region Panel Actions & Validation

        private void SetLoginLoadingState(bool isLoading)
        {
            if (loginButton != null)
            {
                loginButton.interactable = !isLoading;
            }

            if (loginButtonText != null)
            {
                loginButtonText.text = isLoading ? "Logging In..." : originalLoginButtonText;
            }
        }

        public void OnLoginButtonClicked()
        {
            string emailOrUser = loginEmailInput != null ? loginEmailInput.text.Trim() : string.Empty;
            string password = loginPasswordInput != null ? loginPasswordInput.text : string.Empty;

            // 1. Client-side credential validation:
            // If empty, immediately show IncorrectEmailPassword-Box
            if (string.IsNullOrEmpty(emailOrUser) || string.IsNullOrEmpty(password))
            {
                ShowIncorrectCredentialsAlert();
                return;
            }

            // 2. Update UI loading state
            SetLoginLoadingState(true);
            HideIncorrectCredentialsAlert();
            HideToast();

            try
            {
                var mgr = LocalAccountManager.EnsureInstance();
                if (mgr.Login(emailOrUser, password, out var account, out var error))
                {
                    bool rememberMe = rememberMeToggle != null && rememberMeToggle.isOn;
                    SessionManager.EnsureInstance().InitializeFromLocalAccount(account, rememberMe);

                    if (loginPasswordInput != null)
                    {
                        loginPasswordInput.text = string.Empty;
                    }
                    HideIncorrectCredentialsAlert();
                    HideToast();

                    Debug.Log($"[AuthenticationUIManager] Local login successful as '{account.username}' (Role: {account.role}). Navigating to HomeScreen...");
                    SceneManager.LoadScene("HomeScreen");
                }
                else
                {
                    ShowIncorrectCredentialsAlert();
                }
            }
            catch (Exception ex)
            {
                HideIncorrectCredentialsAlert();
                ShowTemporaryMessage($"Login error: {ex.Message}");
            }
            finally
            {
                SetLoginLoadingState(false);
            }
        }

        private bool IsCredentialError(string errorCode, int statusCode)
        {
            if (errorCode == "invalid_credentials" ||
                errorCode == "invalid_grant" ||
                errorCode == "invalid_email_format" ||
                errorCode == "empty_email" ||
                errorCode == "empty_password" ||
                errorCode == "user_not_found")
            {
                return true;
            }

            if (statusCode == 400 && errorCode != "email_not_confirmed")
            {
                return true;
            }

            return false;
        }

        public void ShowIncorrectCredentialsAlert()
        {
            HideToast();
            if (incorrectCredentialsAlertBox != null)
            {
                incorrectCredentialsAlertBox.SetActive(true);
            }
        }

        public void HideIncorrectCredentialsAlert()
        {
            if (incorrectCredentialsAlertBox != null)
            {
                incorrectCredentialsAlertBox.SetActive(false);
            }
        }

        public void HideToast()
        {
            if (toastCoroutine != null)
            {
                StopCoroutine(toastCoroutine);
                toastCoroutine = null;
            }
            if (toastRoot != null)
            {
                toastRoot.SetActive(false);
            }
        }

        public void OnPlayAsGuestButtonClicked()
        {
            Debug.Log("[AuthenticationUIManager] Starting game in Guest mode...");

            if (playAsGuestButton != null)
            {
                playAsGuestButton.interactable = false;
            }

            // Set up guest session in SessionManager
            SessionManager.EnsureInstance().SetGuestSession("Guest");

            // Transition directly to HomeScreen
            SceneManager.LoadScene("HomeScreen");
        }

        public void ToggleRememberMe()
        {
            if (rememberMeToggle != null)
            {
                rememberMeToggle.isOn = !rememberMeToggle.isOn;
            }
        }

        private void OnLoginInputChanged(string _)
        {
            HideIncorrectCredentialsAlert();
        }

        public void OnSignUpSubmitClicked()
        {
            string email = signUpEmailInput != null ? signUpEmailInput.text.Trim() : string.Empty;
            string username = signUpUsernameInput != null ? signUpUsernameInput.text.Trim() : string.Empty;
            string password = signUpPasswordInput != null ? signUpPasswordInput.text : string.Empty;

            // Pertahankan bentuk email asli sesuai input field (jangan di-split)
            if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(email))
            {
                username = email;
            }

            if (string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(username))
            {
                email = username;
            }

            if (string.IsNullOrEmpty(email))
            {
                ShowTemporaryMessage("Silakan masukkan email.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowTemporaryMessage("Silakan masukkan password.");
                return;
            }

            if (password.Length < 6)
            {
                ShowTemporaryMessage("Password minimal harus 6 karakter.");
                return;
            }

            SetSignUpLoadingState(true);

            try
            {
                var mgr = LocalAccountManager.EnsureInstance();
                if (mgr.SignUp(username, email, password, out string error))
                {
                    if (signUpPasswordInput != null)
                    {
                        signUpPasswordInput.text = string.Empty;
                    }

                    ShowLoginPanel();
                    if (loginEmailInput != null)
                    {
                        loginEmailInput.text = !string.IsNullOrEmpty(email) ? email : username;
                    }
                    ShowTemporaryMessage("Akun berhasil dibuat! Silakan login.");
                }
                else
                {
                    ShowTemporaryMessage(error);
                }
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage($"Registration error: {ex.Message}");
            }
            finally
            {
                SetSignUpLoadingState(false);
            }
        }

        public void OnResendVerificationClicked()
        {
            ShowTemporaryMessage("Mode offline lokal: Verifikasi email tidak diperlukan.");
        }

        private AuthManager EnsureAuthManager()
        {
            if (authManager == null)
            {
                authManager = AuthManager.Instance;
                if (authManager == null)
                {
                    authManager = FindFirstObjectByType<AuthManager>();
                    if (authManager == null)
                    {
                        authManager = gameObject.AddComponent<AuthManager>();
                    }
                }
            }
            return authManager;
        }

        private void SetSignUpLoadingState(bool isLoading)
        {
            if (signUpSubmitButton != null)
            {
                signUpSubmitButton.interactable = !isLoading;
            }

            if (signUpSubmitButtonText != null)
            {
                signUpSubmitButtonText.text = isLoading ? "Signing Up..." : originalSignUpSubmitText;
            }
        }

        private void SetResendLoadingState(bool isLoading)
        {
            if (resendVerificationEmailButton != null)
            {
                resendVerificationEmailButton.interactable = !isLoading && resendCooldownRemaining <= 0f;
            }

            if (resendVerificationButtonText != null)
            {
                if (isLoading)
                {
                    resendVerificationButtonText.text = "Sending...";
                }
                else if (resendCooldownRemaining <= 0f)
                {
                    resendVerificationButtonText.text = originalResendVerificationText;
                }
            }
        }

        private void StartResendCooldown(float durationSeconds)
        {
            if (resendCooldownCoroutine != null)
            {
                StopCoroutine(resendCooldownCoroutine);
            }
            resendCooldownCoroutine = StartCoroutine(ResendCooldownRoutine(durationSeconds));
        }

        private IEnumerator ResendCooldownRoutine(float durationSeconds)
        {
            resendCooldownRemaining = durationSeconds;
            if (resendVerificationEmailButton != null)
            {
                resendVerificationEmailButton.interactable = false;
            }

            while (resendCooldownRemaining > 0f)
            {
                if (resendVerificationButtonText != null)
                {
                    resendVerificationButtonText.text = $"Resend in {Mathf.CeilToInt(resendCooldownRemaining)}s";
                }
                yield return new WaitForSecondsRealtime(1f);
                resendCooldownRemaining -= 1f;
            }

            resendCooldownRemaining = 0f;
            if (resendVerificationEmailButton != null)
            {
                resendVerificationEmailButton.interactable = true;
            }
            if (resendVerificationButtonText != null)
            {
                resendVerificationButtonText.text = originalResendVerificationText;
            }
            resendCooldownCoroutine = null;
        }

        private void SetForgotPasswordLoadingState(bool isLoading)
        {
            if (forgotPasswordSubmitButton != null)
            {
                forgotPasswordSubmitButton.interactable = !isLoading;
            }

            if (forgotPasswordSubmitButtonText != null)
            {
                forgotPasswordSubmitButtonText.text = isLoading ? "Sending..." : originalForgotPasswordSubmitText;
            }
        }

        private void SetResetPasswordLoadingState(bool isLoading)
        {
            if (resetPasswordSubmitButton != null)
            {
                resetPasswordSubmitButton.interactable = !isLoading;
            }

            if (resetPasswordSubmitButtonText != null)
            {
                resetPasswordSubmitButtonText.text = isLoading ? "Updating..." : originalResetPasswordSubmitText;
            }
        }

        public void OnForgotPasswordSubmitClicked()
        {
            string emailOrUser = forgotPasswordEmailInput != null ? forgotPasswordEmailInput.text.Trim() : string.Empty;

            if (string.IsNullOrEmpty(emailOrUser))
            {
                ShowTemporaryMessage("Silakan masukkan username atau email.");
                return;
            }

            SetForgotPasswordLoadingState(true);

            try
            {
                var mgr = LocalAccountManager.EnsureInstance();
                if (mgr.AccountExists(emailOrUser))
                {
                    recoveryTargetAccount = emailOrUser;
                    ShowTemporaryMessage("Akun ditemukan! Silakan masukkan password baru.");
                    ShowResetPasswordPanel();
                }
                else
                {
                    ShowTemporaryMessage("Akun / Email tidak ditemukan di database lokal.");
                }
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage($"Recovery error: {ex.Message}");
            }
            finally
            {
                SetForgotPasswordLoadingState(false);
            }
        }

        public void OnResetPasswordSubmitClicked()
        {
            string target = recoveryTargetAccount;
            if (string.IsNullOrEmpty(target) && forgotPasswordEmailInput != null)
            {
                target = forgotPasswordEmailInput.text.Trim();
            }
            if (string.IsNullOrEmpty(target) && loginEmailInput != null)
            {
                target = loginEmailInput.text.Trim();
            }

            if (string.IsNullOrEmpty(target))
            {
                ShowTemporaryMessage("Silakan tentukan akun di menu Lupa Password terlebih dahulu.");
                ShowForgotPasswordPanel();
                return;
            }

            string newPassword = resetPasswordNewInput != null ? resetPasswordNewInput.text : string.Empty;
            string confirmPassword = resetPasswordConfirmInput != null ? resetPasswordConfirmInput.text : string.Empty;

            if (string.IsNullOrEmpty(newPassword))
            {
                ShowTemporaryMessage("Silakan masukkan password baru.");
                return;
            }

            if (newPassword.Length < 6)
            {
                ShowTemporaryMessage("Password minimal harus 6 karakter.");
                return;
            }

            if (string.IsNullOrEmpty(confirmPassword))
            {
                ShowTemporaryMessage("Silakan konfirmasi password baru.");
                return;
            }

            if (newPassword != confirmPassword)
            {
                ShowTemporaryMessage("Konfirmasi password tidak cocok.");
                return;
            }

            SetResetPasswordLoadingState(true);

            try
            {
                var mgr = LocalAccountManager.EnsureInstance();
                if (mgr.ResetPassword(target, newPassword, out string error))
                {
                    if (resetPasswordCodeInput != null) resetPasswordCodeInput.text = string.Empty;
                    if (resetPasswordNewInput != null) resetPasswordNewInput.text = string.Empty;
                    if (resetPasswordConfirmInput != null) resetPasswordConfirmInput.text = string.Empty;

                    if (loginEmailInput != null)
                    {
                        loginEmailInput.text = target;
                    }
                    if (loginPasswordInput != null)
                    {
                        loginPasswordInput.text = string.Empty;
                    }

                    ShowLoginPanel();
                    ShowTemporaryMessage("Password berhasil direset! Silakan login.");
                }
                else
                {
                    ShowTemporaryMessage(error);
                }
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage($"Reset error: {ex.Message}");
            }
            finally
            {
                SetResetPasswordLoadingState(false);
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            // Simple robust regex check for user@domain.tld
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        #endregion

        #region Temporary Feedback / Toast

        /// <summary>
        /// Displays a non-intrusive floating prototype toast message in the UI and logs to console.
        /// </summary>
        /// <param name="message">The status or prototype feedback message.</param>
        public void ShowTemporaryMessage(string message)
        {
            Debug.Log($"[Authentication UI]: {message}");

            if (toastText != null)
            {
                toastText.text = message;
            }

            if (toastRoot != null && toastCanvasGroup != null)
            {
                if (toastCoroutine != null)
                {
                    StopCoroutine(toastCoroutine);
                }
                toastCoroutine = StartCoroutine(AnimateToastCoroutine());
            }
        }

        private IEnumerator AnimateToastCoroutine()
        {
            toastRoot.SetActive(true);
            toastCanvasGroup.alpha = 0f;

            // Fade In
            float duration = 0.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            toastCanvasGroup.alpha = 1f;

            // Hold display
            yield return new WaitForSecondsRealtime(2.5f);

            // Fade Out
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                yield return null;
            }
            toastCanvasGroup.alpha = 0f;
            toastRoot.SetActive(false);
            toastCoroutine = null;
        }

        private void EnsureToastUI()
        {
            if (toastRoot != null) return;

            // Create a runtime toast overlay under Canvas if not explicitly assigned in inspector
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
#if UNITY_2023_1_OR_NEWER
                canvas = FindFirstObjectByType<Canvas>();
#else
                canvas = FindObjectOfType<Canvas>();
#endif
            }

            if (canvas == null) return;

            GameObject toastGO = new GameObject("PrototypeToastNotification", typeof(RectTransform), typeof(CanvasGroup));
            toastGO.transform.SetParent(canvas.transform, false);

            RectTransform rt = toastGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.08f);
            rt.anchorMax = new Vector2(0.5f, 0.08f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 64f);

            Image bg = toastGO.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.12f, 0.08f, 0.92f); // Matches theme palette
            bg.raycastTarget = false;

            GameObject textGO = new GameObject("ToastText", typeof(RectTransform));
            textGO.transform.SetParent(toastGO.transform, false);

            RectTransform textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.offsetMin = new Vector2(16f, 8f);
            textRt.offsetMax = new Vector2(-16f, -8f);

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20f;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            // Try to assign font asset if available
            TMP_FontAsset font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().Length > 0 ?
                Resources.FindObjectsOfTypeAll<TMP_FontAsset>()[0] : null;
            if (font != null)
            {
                tmp.font = font;
            }

            toastRoot = toastGO;
            toastText = tmp;
            toastCanvasGroup = toastGO.GetComponent<CanvasGroup>();
            toastCanvasGroup.blocksRaycasts = false;
            toastCanvasGroup.interactable = false;
            toastRoot.SetActive(false);
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoAssignReferences();
        }

        public void AutoAssignReferences()
        {
            Transform canvasTransform = transform;
            if (canvasTransform.GetComponent<Canvas>() == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null) canvasTransform = canvas.transform;
            }

            // Find Panels
            if (loginPanel == null)
            {
                var t = canvasTransform.Find("LoginPanel");
                if (t != null) loginPanel = t.gameObject;
            }
            if (signUpPanel == null)
            {
                var t = canvasTransform.Find("SignUpPanel");
                if (t != null) signUpPanel = t.gameObject;
            }
            if (emailSentPanel == null)
            {
                var t = canvasTransform.Find("EmailSentPanel");
                if (t != null) emailSentPanel = t.gameObject;
            }
            if (forgotPasswordPanel == null)
            {
                var t = canvasTransform.Find("ForgotPasswordPanel");
                if (t != null) forgotPasswordPanel = t.gameObject;
            }
            if (resetPasswordPanel == null)
            {
                var t = canvasTransform.Find("ResetPasswordPanel");
                if (t != null) resetPasswordPanel = t.gameObject;
            }

            // Auth Manager
            if (authManager == null)
            {
                authManager = GetComponent<AuthManager>();
                if (authManager == null) authManager = GetComponentInParent<AuthManager>();
                if (authManager == null) authManager = GetComponentInChildren<AuthManager>(true);
            }

            // Verification Email UI
            if (verificationEmailUI == null)
            {
                verificationEmailUI = GetComponentInChildren<VerificationEmailUI>(true);
            }

            // Login Panel Controls
            if (loginPanel != null)
            {
                if (loginButton == null)
                {
                    var t = loginPanel.transform.Find("Login-Button");
                    if (t != null) loginButton = t.GetComponent<Button>();
                }
                if (playAsGuestButton == null)
                {
                    var t = loginPanel.transform.Find("PlayAsGuest-Button");
                    if (t != null) playAsGuestButton = t.GetComponent<Button>();
                }
                if (signUpNavigationButton == null)
                {
                    var t = loginPanel.transform.Find("Sign-Up");
                    if (t != null) signUpNavigationButton = t.GetComponent<Button>();
                }
                if (forgotPasswordNavigationButton == null)
                {
                    var t = loginPanel.transform.Find("Forgot Password");
                    if (t != null) forgotPasswordNavigationButton = t.GetComponent<Button>();
                }
                if (loginEmailInput == null)
                {
                    var t = loginPanel.transform.Find("Email-Input") ?? loginPanel.transform.Find("Username-Input");
                    if (t != null) loginEmailInput = t.GetComponent<TMP_InputField>();
                }
                if (loginPasswordInput == null)
                {
                    var t = loginPanel.transform.Find("Password-Input");
                    if (t != null) loginPasswordInput = t.GetComponent<TMP_InputField>();
                }
                if (rememberMeToggle == null)
                {
                    var t = loginPanel.transform.Find("RememberMeCheck");
                    if (t != null) rememberMeToggle = t.GetComponent<Toggle>();
                }
                if (rememberMeTextButton == null)
                {
                    var t = loginPanel.transform.Find("Remember-me");
                    if (t != null) rememberMeTextButton = t.GetComponent<Button>();
                }
                if (incorrectCredentialsAlertBox == null)
                {
                    var t = loginPanel.transform.Find("IncorrectEmailPassword-Box");
                    if (t != null) incorrectCredentialsAlertBox = t.gameObject;
                }
            }

            // SignUp Panel Controls
            if (signUpPanel != null)
            {
                if (signUpBackToLoginButton == null)
                {
                    var t = signUpPanel.transform.Find("Login");
                    if (t != null) signUpBackToLoginButton = t.GetComponent<Button>();
                }
                if (signUpSubmitButton == null)
                {
                    var t = signUpPanel.transform.Find("Continue-Button");
                    if (t != null) signUpSubmitButton = t.GetComponent<Button>();
                }
                if (signUpUsernameInput == null)
                {
                    var t = signUpPanel.transform.Find("Password-Username");
                    if (t != null) signUpUsernameInput = t.GetComponent<TMP_InputField>();
                }
                if (signUpEmailInput == null)
                {
                    var t = signUpPanel.transform.Find("Email-Input");
                    if (t != null) signUpEmailInput = t.GetComponent<TMP_InputField>();
                }
                if (signUpPasswordInput == null)
                {
                    var t = signUpPanel.transform.Find("Password-Input");
                    if (t != null) signUpPasswordInput = t.GetComponent<TMP_InputField>();
                }
            }

            // Email Sent Panel Controls
            if (emailSentPanel != null)
            {
                if (emailSentBackToLoginButton == null)
                {
                    var t = emailSentPanel.transform.Find("GoBackToLoginButton");
                    if (t != null) emailSentBackToLoginButton = t.GetComponent<Button>();
                }
                if (resendVerificationEmailButton == null)
                {
                    var t = emailSentPanel.transform.Find("ResendVerification");
                    if (t != null) resendVerificationEmailButton = t.GetComponent<Button>();
                }
            }

            // Forgot Password Panel Controls
            if (forgotPasswordPanel != null)
            {
                if (forgotPasswordBackButton == null)
                {
                    var t = forgotPasswordPanel.transform.Find("GoBackToLogin");
                    if (t != null) forgotPasswordBackButton = t.GetComponent<Button>();
                }
                if (forgotPasswordSubmitButton == null)
                {
                    var t = forgotPasswordPanel.transform.Find("Continue-Button");
                    if (t != null) forgotPasswordSubmitButton = t.GetComponent<Button>();
                }
                if (forgotPasswordEmailInput == null)
                {
                    var t = forgotPasswordPanel.transform.Find("EnterEmail-Input");
                    if (t != null) forgotPasswordEmailInput = t.GetComponent<TMP_InputField>();
                }
            }

            // Reset Password Panel Controls
            if (resetPasswordPanel != null)
            {
                if (resetPasswordBackButton == null)
                {
                    var t = resetPasswordPanel.transform.Find("GoBackToLogin");
                    if (t != null) resetPasswordBackButton = t.GetComponent<Button>();
                }
                if (resetPasswordSubmitButton == null)
                {
                    var t = resetPasswordPanel.transform.Find("Continue-Button");
                    if (t != null) resetPasswordSubmitButton = t.GetComponent<Button>();
                }
                if (resetPasswordCodeInput == null)
                {
                    var t = resetPasswordPanel.transform.Find("ResetCode-Input");
                    if (t != null) resetPasswordCodeInput = t.GetComponent<TMP_InputField>();
                }
                if (resetPasswordNewInput == null)
                {
                    var t = resetPasswordPanel.transform.Find("NewPassword-Input");
                    if (t != null) resetPasswordNewInput = t.GetComponent<TMP_InputField>();
                }
                if (resetPasswordConfirmInput == null)
                {
                    var t = resetPasswordPanel.transform.Find("ConfirmPassword-Input");
                    if (t != null) resetPasswordConfirmInput = t.GetComponent<TMP_InputField>();
                }
            }
        }
#endif
    }
}
