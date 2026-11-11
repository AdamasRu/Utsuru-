using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utsuru.Capture;

namespace Utsuru.DictionaryUI
{
    /// <summary>
    /// Menangani satu kartu kosakata di halaman buku Tab Kosakata.
    /// Mendukung status Discovered (terbuka) dan Locked (terkunci/siluet gembok).
    /// </summary>
    public class DictionaryVocabCardUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private Button cardButton;

        [Header("State Discovered")]
        [SerializeField] private GameObject discoveredGroup;
        [SerializeField] private TMP_Text kanjiLabel;
        [SerializeField] private TMP_Text romajiLabel;
        [SerializeField] private TMP_Text meaningLabel;
        [SerializeField] private TMP_Text categoryBadge;

        [Header("State Locked")]
        [SerializeField] private GameObject lockedGroup;
        [SerializeField] private TMP_Text lockedHintLabel;

        private ObjectInfo currentInfo;
        private bool isDiscovered;
        private Action<ObjectInfo> onClickCallback;

        private void Awake()
        {
            if (rootObject == null) rootObject = gameObject;
            if (cardButton != null) cardButton.onClick.AddListener(HandleClick);
        }

        public void Bind(ObjectInfo info, bool discovered, Action<ObjectInfo> onClick)
        {
            currentInfo = info;
            isDiscovered = discovered;
            onClickCallback = onClick;

            if (info == null)
            {
                if (rootObject != null) rootObject.SetActive(false);
                return;
            }

            if (rootObject != null) rootObject.SetActive(true);

            if (discovered)
            {
                if (discoveredGroup != null) discoveredGroup.SetActive(true);
                if (lockedGroup != null) lockedGroup.SetActive(false);

                var vocab = info.vocabulary;
                if (kanjiLabel != null) kanjiLabel.text = vocab != null ? vocab.DisplayJapanese : info.objectName;
                if (romajiLabel != null) romajiLabel.text = vocab != null ? vocab.romaji : "-";
                if (meaningLabel != null) meaningLabel.text = vocab != null ? vocab.translation : "-";
                if (categoryBadge != null)
                {
                    categoryBadge.text = vocab != null && !string.IsNullOrEmpty(vocab.category) ? vocab.category : "Umum";
                }
            }
            else
            {
                if (discoveredGroup != null) discoveredGroup.SetActive(false);
                if (lockedGroup != null) lockedGroup.SetActive(true);

                if (lockedHintLabel != null)
                {
                    lockedHintLabel.text = "🔒 Belum Ditemukan\n(Potret objek di kota)";
                }
            }
        }

        private void HandleClick()
        {
            if (isDiscovered && currentInfo != null)
            {
                onClickCallback?.Invoke(currentInfo);
            }
        }
    }
}

