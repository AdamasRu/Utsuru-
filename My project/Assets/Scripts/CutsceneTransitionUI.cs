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

    [Header("Timing Settings")]
    [Tooltip("Kecepatan layar menghitam dan membuka (detik)")]
    [SerializeField] private float fadeDuration = 0.8f;

    [Tooltip("Lama layar tertutup penuh sebelum membuka kembali (detik)")]
    [SerializeField] private float holdDuration = 1.0f;

    private void Awake()
    {
        // Pastikan transisi tidak menutupi layar saat game baru mulai
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public IEnumerator PlayTransitionRoutine(bool targetIsNight, Action onScreenCovered)
    {
        // 1. Ubah teks placeholder (nanti diganti animasi 2D)
        if (transitionText != null)
        {
            transitionText.text = targetIsNight ? "Malam Tiba..." : "Pagi Telah Tiba!";
        }

        // Kunci klik mouse agar tidak menembus UI
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        // 2. FADE IN (Layar perlahan menghitam)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

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
                canvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }
}