using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Modal dialog serbaguna untuk input teks (nama album) dan konfirmasi (hapus foto/album).
    /// </summary>
    public class DictionarySimpleDialog : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject dialogRoot;

        [Header("Komponen")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text confirmButtonText;

        private Action<string> onConfirmInput;
        private Action onConfirmAction;

        private void Awake()
        {
            if (dialogRoot == null) dialogRoot = gameObject;
            if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
            Hide();
        }

        public void ShowInput(string title, string defaultText, string confirmText, Action<string> onConfirm)
        {
            onConfirmInput = onConfirm;
            onConfirmAction = null;

            if (titleLabel != null) titleLabel.text = title;
            if (messageLabel != null) messageLabel.gameObject.SetActive(false);
            if (inputField != null)
            {
                inputField.gameObject.SetActive(true);
                inputField.text = defaultText ?? "";
                inputField.ActivateInputField();
            }
            if (confirmButtonText != null) confirmButtonText.text = confirmText ?? "OK";

            if (dialogRoot != null) dialogRoot.SetActive(true);
        }

        public void ShowConfirm(string title, string message, string confirmText, Action onConfirm)
        {
            onConfirmAction = onConfirm;
            onConfirmInput = null;

            if (titleLabel != null) titleLabel.text = title;
            if (messageLabel != null)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = message;
            }
            if (inputField != null) inputField.gameObject.SetActive(false);
            if (confirmButtonText != null) confirmButtonText.text = confirmText ?? "Ya";

            if (dialogRoot != null) dialogRoot.SetActive(true);
        }

        private void HandleConfirm()
        {
            if (onConfirmInput != null)
            {
                string text = inputField != null ? inputField.text : "";
                onConfirmInput.Invoke(text);
            }
            else if (onConfirmAction != null)
            {
                onConfirmAction.Invoke();
            }
            Close();
        }

        public void Close()
        {
            Hide();
        }

        public void Hide()
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
        }
    }
}

