using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Utsuru.UI
{
    /// <summary>
    /// Manages password visibility toggle between hidden (Eye-Slash) and visible (Eye).
    /// Preserves input text without clearing or modifying it.
    /// </summary>
    public class PasswordVisibilityToggle : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("The TMP Input Field whose password visibility is toggled.")]
        [SerializeField] private TMP_InputField passwordInputField;

        [Tooltip("The Button component triggering the toggle.")]
        [SerializeField] private Button toggleButton;

        [Tooltip("The Image component displaying the eye icon.")]
        [SerializeField] private Image targetIconImage;

        [Header("Sprites")]
        [Tooltip("Sprite displayed when password is visible (Eye).")]
        [SerializeField] private Sprite eyeVisibleSprite;

        [Tooltip("Sprite displayed when password is hidden (Eye-Slash).")]
        [SerializeField] private Sprite eyeHiddenSprite;

        [Header("State")]
        [SerializeField] private bool isPasswordVisible = false;

        public bool IsPasswordVisible => isPasswordVisible;

        private void Awake()
        {
            if (toggleButton == null)
            {
                toggleButton = GetComponent<Button>();
            }

            if (targetIconImage == null)
            {
                targetIconImage = GetComponent<Image>();
            }

            // Ensure sprites are cached if not assigned
            if (eyeVisibleSprite == null)
            {
#if UNITY_EDITOR
                eyeVisibleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Login-Page/Eye.png");
#endif
            }
            if (eyeHiddenSprite == null)
            {
#if UNITY_EDITOR
                eyeHiddenSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Login-Page/Eye-Slash.png");
#endif
            }

            // Guard against duplicate listener registrations:
            // If the button already has a persistent listener configured in the inspector,
            // do NOT add another runtime listener.
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(TogglePasswordVisibility);

                bool hasPersistentListener = false;
                for (int i = 0; i < toggleButton.onClick.GetPersistentEventCount(); i++)
                {
                    if (toggleButton.onClick.GetPersistentTarget(i) == (Object)this &&
                        toggleButton.onClick.GetPersistentMethodName(i) == nameof(TogglePasswordVisibility))
                    {
                        hasPersistentListener = true;
                        break;
                    }
                }

                if (!hasPersistentListener)
                {
                    toggleButton.onClick.AddListener(TogglePasswordVisibility);
                }
            }
        }

        private void OnEnable()
        {
            // Reset to default hidden state whenever the panel or object becomes active
            SetPasswordVisibility(false);
        }

        /// <summary>
        /// Toggles password visibility state.
        /// </summary>
        public void TogglePasswordVisibility()
        {
            SetPasswordVisibility(!isPasswordVisible);
        }

        /// <summary>
        /// Sets the password visibility state explicitly.
        /// </summary>
        /// <param name="visible">True to show plain text, False to mask as password.</param>
        public void SetPasswordVisibility(bool visible)
        {
            isPasswordVisible = visible;

            if (passwordInputField != null)
            {
                // Update both contentType and inputType without clearing or modifying text
                passwordInputField.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
                passwordInputField.inputType = visible ? TMP_InputField.InputType.Standard : TMP_InputField.InputType.Password;
                passwordInputField.ForceLabelUpdate();
            }

            if (targetIconImage != null)
            {
                // Hidden = Eye-Slash, Visible = Eye
                Sprite targetSprite = visible ? eyeVisibleSprite : eyeHiddenSprite;
                if (targetSprite != null)
                {
                    targetIconImage.sprite = targetSprite;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (toggleButton == null)
            {
                toggleButton = GetComponent<Button>();
            }

            if (targetIconImage == null)
            {
                targetIconImage = GetComponent<Image>();
            }

            if (eyeVisibleSprite == null)
            {
                eyeVisibleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Login-Page/Eye.png");
            }

            if (eyeHiddenSprite == null)
            {
                eyeHiddenSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Login-Page/Eye-Slash.png");
            }

            if (passwordInputField == null)
            {
                passwordInputField = GetComponentInParent<TMP_InputField>();
            }
        }
#endif
    }
}
