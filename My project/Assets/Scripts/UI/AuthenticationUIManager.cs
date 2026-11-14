using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        [SerializeField] private TMP_InputField loginUsernameInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Toggle rememberMeToggle;
        [SerializeField] private Button rememberMeTextButton;

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

        private void Awake()
        {
            AutoAssignReferences();
            SetupButtonListeners();
            EnsureToastUI();
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
        }

        public void ShowResetPasswordPanel()
        {
            ShowPanel(resetPasswordPanel);
        }

        #endregion

        #region Button Event Listeners Setup

        private void SetupButtonListeners()
        {
            // Login Panel
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(OnLoginButtonClicked);
                loginButton.onClick.AddListener(OnLoginButtonClicked);
            }

            if (playAsGuestButton != null)
            {
                playAsGuestButton.onClick.RemoveListener(OnPlayAsGuestButtonClicked);
                playAsGuestButton.onClick.AddListener(OnPlayAsGuestButtonClicked);
            }

            if (signUpNavigationButton != null)
            {
                signUpNavigationButton.onClick.RemoveListener(ShowSignUpPanel);
                signUpNavigationButton.onClick.AddListener(ShowSignUpPanel);
            }

            if (forgotPasswordNavigationButton != null)
            {
                forgotPasswordNavigationButton.onClick.RemoveListener(ShowForgotPasswordPanel);
                forgotPasswordNavigationButton.onClick.AddListener(ShowForgotPasswordPanel);
            }

            if (rememberMeTextButton != null)
            {
                rememberMeTextButton.onClick.RemoveListener(ToggleRememberMe);
                rememberMeTextButton.onClick.AddListener(ToggleRememberMe);
            }

            // Sign Up Panel
            if (signUpBackToLoginButton != null)
            {
                signUpBackToLoginButton.onClick.RemoveListener(ShowLoginPanel);
                signUpBackToLoginButton.onClick.AddListener(ShowLoginPanel);
            }

            if (signUpSubmitButton != null)
            {
                signUpSubmitButton.onClick.RemoveListener(OnSignUpSubmitClicked);
                signUpSubmitButton.onClick.AddListener(OnSignUpSubmitClicked);
            }

            // Email Sent Panel
            if (emailSentBackToLoginButton != null)
            {
                emailSentBackToLoginButton.onClick.RemoveListener(ShowLoginPanel);
                emailSentBackToLoginButton.onClick.AddListener(ShowLoginPanel);
            }

            if (resendVerificationEmailButton != null)
            {
                resendVerificationEmailButton.onClick.RemoveListener(OnResendVerificationClicked);
                resendVerificationEmailButton.onClick.AddListener(OnResendVerificationClicked);
            }

            // Forgot Password Panel
            if (forgotPasswordBackButton != null)
            {
                forgotPasswordBackButton.onClick.RemoveListener(ShowLoginPanel);
                forgotPasswordBackButton.onClick.AddListener(ShowLoginPanel);
            }

            if (forgotPasswordSubmitButton != null)
            {
                forgotPasswordSubmitButton.onClick.RemoveListener(OnForgotPasswordSubmitClicked);
                forgotPasswordSubmitButton.onClick.AddListener(OnForgotPasswordSubmitClicked);
            }

            // Reset Password Panel
            if (resetPasswordBackButton != null)
            {
                resetPasswordBackButton.onClick.RemoveListener(ShowLoginPanel);
                resetPasswordBackButton.onClick.AddListener(ShowLoginPanel);
            }

            if (resetPasswordSubmitButton != null)
            {
                resetPasswordSubmitButton.onClick.RemoveListener(OnResetPasswordSubmitClicked);
                resetPasswordSubmitButton.onClick.AddListener(OnResetPasswordSubmitClicked);
            }
        }

        #endregion

        #region Panel Actions & Validation

        public void OnLoginButtonClicked()
        {
            // Prototype only: authentication backend is not connected
            ShowTemporaryMessage("Authentication backend is not connected yet. (UI Prototype)");
        }

        public void OnPlayAsGuestButtonClicked()
        {
            ShowTemporaryMessage("Guest login is not connected yet. (UI Prototype)");
        }

        public void ToggleRememberMe()
        {
            if (rememberMeToggle != null)
            {
                rememberMeToggle.isOn = !rememberMeToggle.isOn;
            }
        }

        public void OnSignUpSubmitClicked()
        {
            string email = signUpEmailInput != null ? signUpEmailInput.text.Trim() : string.Empty;
            string username = signUpUsernameInput != null ? signUpUsernameInput.text.Trim() : string.Empty;
            string password = signUpPasswordInput != null ? signUpPasswordInput.text : string.Empty;

            // Validate inputs
            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            {
                ShowTemporaryMessage("Please fill in all required fields.");
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                ShowTemporaryMessage("Please enter your username.");
                return;
            }

            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                ShowTemporaryMessage("Please enter a valid email address.");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowTemporaryMessage("Please enter your password.");
                return;
            }

            // Pass email to dynamic verification display
            if (verificationEmailUI != null)
            {
                verificationEmailUI.SetEmail(email);
            }

            // Open EmailSentPanel for UI demonstration
            ShowEmailSentPanel();
        }

        public void OnResendVerificationClicked()
        {
            // Backend email delivery is not implemented
            ShowTemporaryMessage("Backend email delivery is not implemented yet. (UI Preview)");
        }

        public void OnForgotPasswordSubmitClicked()
        {
            // For UI navigation testing only: open ResetPasswordPanel without claiming email sent
            ShowResetPasswordPanel();
        }

        public void OnResetPasswordSubmitClicked()
        {
            string resetCode = resetPasswordCodeInput != null ? resetPasswordCodeInput.text.Trim() : string.Empty;
            string newPassword = resetPasswordNewInput != null ? resetPasswordNewInput.text : string.Empty;
            string confirmPassword = resetPasswordConfirmInput != null ? resetPasswordConfirmInput.text : string.Empty;

            if (string.IsNullOrEmpty(resetCode))
            {
                ShowTemporaryMessage("Please enter your reset code.");
                return;
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                ShowTemporaryMessage("Please enter a new password.");
                return;
            }

            if (string.IsNullOrEmpty(confirmPassword))
            {
                ShowTemporaryMessage("Please confirm your new password.");
                return;
            }

            if (newPassword != confirmPassword)
            {
                ShowTemporaryMessage("Passwords do not match. Please re-enter.");
                return;
            }

            // Validated locally: show prototype message, do not modify actual password
            ShowTemporaryMessage("Password reset validated locally. Backend is not connected.");
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
                if (loginUsernameInput == null)
                {
                    var t = loginPanel.transform.Find("Username-Input");
                    if (t != null) loginUsernameInput = t.GetComponent<TMP_InputField>();
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
