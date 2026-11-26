using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CutsceneTransitionUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text transitionText; // Kosongkan jika tidak pakai teks biasa

    [Header("Text Settings")]
    [Tooltip("Teks yang tampil saat transisi dari Siang ke Malam")]
    [SerializeField] private string toNightText = "Malam Tiba...";

    [Tooltip("Teks yang tampil saat transisi dari Malam ke Siang")]
    [SerializeField] private string toDayText = "Pagi Telah Tiba!";

    [Header("Timing Settings")]
    [Tooltip("Kecepatan layar menghitam dan membuka (detik)")]
    [SerializeField] private float fadeDuration = 0.8f;

    [Tooltip("Lama layar tertutup penuh sebelum membuka kembali (detik)")]
    [SerializeField] private float holdDuration = 1.0f;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Pastikan transisi tidak menutupi layar saat game baru mulai
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public IEnumerator PlayTransitionRoutine(bool targetIsNight, Action onScreenCovered)
    {
        // Pastikan panel transisi aktif agar visual fade & teksnya tampil di layar
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // 1. Ubah teks transisi sesuai konfigurasi Inspector
        if (transitionText != null)
        {
            transitionText.text = targetIsNight ? toNightText : toDayText;
            if (!transitionText.gameObject.activeSelf)
                transitionText.gameObject.SetActive(true);
        }

        // Kunci klik mouse agar tidak menembus UI
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
        }

        // 2. FADE IN (Layar perlahan menghitam)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = fadeDuration > 0f ? Mathf.Clamp01(timer / fadeDuration) : 1f;
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        // 3. TITIK PUNCAK (Layar tertutup 100%):
        // Eksekusi perubahan lampu & skybox di balik layar!
        onScreenCovered?.Invoke();

        // 4. HOLD (Tahan layar tertutup sebentar)
        yield return new WaitForSeconds(holdDuration);

        // 5. FADE OUT (Layar perlahan terbuka kembali)
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = fadeDuration > 0f ? 1f - Mathf.Clamp01(timer / fadeDuration) : 0f;
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }
}