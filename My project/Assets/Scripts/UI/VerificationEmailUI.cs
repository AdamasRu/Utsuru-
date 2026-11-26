using UnityEngine;
using TMPro;

namespace Utsuru.UI
{
    /// <summary>
    /// Handles dynamic email display across SignUpPanel and EmailSentPanel.
    /// </summary>
    public class VerificationEmailUI : MonoBehaviour
    {
        [Header("Target UI References")]
        [Tooltip("The TMP InputField for email entry in SignUpPanel.")]
        [SerializeField] private TMP_InputField signUpEmailInput;

        [Tooltip("The TMP_Text component in EmailSentPanel displaying the destination email.")]
        [SerializeField] private TMP_Text emailDisplayText;

        [Header("Settings")]
        [Tooltip("Fallback email shown when no email was entered.")]
        [SerializeField] private string fallbackEmail = "user@example.com";

        public TMP_InputField SignUpEmailInput => signUpEmailInput;
        public TMP_Text EmailDisplayText => emailDisplayText;

        /// <summary>
        /// Reads the email from the SignUp input field and applies it to the EmailSent display.
        /// </summary>
        public void SyncEmailFromSignUp()
        {
            if (signUpEmailInput != null)
            {
                SetEmail(signUpEmailInput.text);
            }
        }

        /// <summary>
        /// Updates the displayed email text dynamically.
        /// </summary>
        /// <param name="email">Email address to display.</param>
        public void SetEmail(string email)
        {
            if (emailDisplayText != null)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    emailDisplayText.text = fallbackEmail;
                }
                else
                {
                    emailDisplayText.text = email.Trim();
                }
            }
        }

        /// <summary>
        /// Gets the current text entered into the SignUp email input field.
        /// </summary>
        public string GetEnteredSignUpEmail()
        {
            return signUpEmailInput != null ? signUpEmailInput.text.Trim() : string.Empty;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoAssignReferences();
        }

        public void AutoAssignReferences()
        {
            if (signUpEmailInput == null)
            {
                var inputGo = GameObject.Find("Canvas/SignUpPanel/Email-Input");
                if (inputGo != null)
                {
                    signUpEmailInput = inputGo.GetComponent<TMP_InputField>();
                }
            }

            if (emailDisplayText == null)
            {
                var textGo = GameObject.Find("Canvas/EmailSentPanel/youremail.com");
                if (textGo != null)
                {
                    emailDisplayText = textGo.GetComponent<TMP_Text>();
                }
            }
        }
#endif
    }
}

