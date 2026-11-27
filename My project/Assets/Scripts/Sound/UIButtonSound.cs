using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Utsuru.Sound
{
    /// <summary>
    /// Komponen modular mandiri untuk tombol UI.
    /// Pasang pada GameObject tombol mana saja di Inspector untuk memutar SFX saat diklik.
    /// Tidak mengubah / merusak skrip logika UI apapun.
    /// </summary>
    public class UIButtonSound : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Jenis suara yang ingin dimainkan saat tombol ini diklik.")]
        [SerializeField] private UISoundType soundType = UISoundType.TabSwitch;

        [Tooltip("Klip audio kustom (opsional). Jika diisi, klip ini yang akan diputar dan mengabaikan soundType di atas.")]
        [SerializeField] private AudioClip customClip;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                // Gunakan onClick listener bawaan Button agar otomatis menghormati state interactable (tidak berbunyi jika disabled)
                _button.onClick.AddListener(TriggerSound);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Jika objek TIDAK punya komponen Button (misal Image biasa yang bisa diklik), gunakan pointer click langsung
            if (_button == null)
            {
                TriggerSound();
            }
        }

        private void TriggerSound()
        {
            if (customClip != null)
            {
                DictionarySoundManager.Instance?.PlayClipDirectly(customClip);
            }
            else
            {
                DictionarySoundManager.Play(soundType);
            }
        }
    }
}
