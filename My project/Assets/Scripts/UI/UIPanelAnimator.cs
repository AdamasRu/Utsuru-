using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Utsuru.UI
{
    /// <summary>
    /// Smoothly animates UI panel switches (fade + gentle slide/scale settle) and attaches
    /// subtle hover/click spring feedback to navigation buttons.
    /// </summary>
    public static class UIPanelAnimator
    {
        private const float DefaultFadeOutDuration = 0.12f;
        private const float DefaultFadeInDuration = 0.22f;
        private const float SlideDistance = 28f;

        /// <summary>
        /// Moves the shared scenic "Background" image onto the Canvas root (index 0) and disables
        /// duplicate per-panel "Background" children so cross-panel transitions never flash the
        /// empty camera background underneath.
        /// </summary>
        public static void EnsureSharedBackground(Canvas canvas, IList<GameObject> panels)
        {
            if (canvas == null || panels == null || panels.Count == 0) return;

            Transform existingShared = canvas.transform.Find("SharedBackground");
            Image templateImage = null;
            RectTransform templateRt = null;

            for (int i = 0; i < panels.Count; i++)
            {
                GameObject p = panels[i];
                if (p == null) continue;
                Transform bg = p.transform.Find("Background");
                if (bg == null) continue;

                if (templateImage == null)
                {
                    Image img = bg.GetComponent<Image>();
                    RectTransform rt = bg.GetComponent<RectTransform>();
                    if (img != null && img.sprite != null && rt != null)
                    {
                        templateImage = img;
                        templateRt = rt;
                    }
                }
            }

            if (existingShared == null && templateImage != null)
            {
                GameObject sharedGo = new GameObject("SharedBackground", typeof(RectTransform), typeof(Image));
                sharedGo.transform.SetParent(canvas.transform, false);
                sharedGo.transform.SetSiblingIndex(0);

                RectTransform sharedRt = sharedGo.GetComponent<RectTransform>();
                sharedRt.anchorMin = templateRt.anchorMin;
                sharedRt.anchorMax = templateRt.anchorMax;
                sharedRt.pivot = templateRt.pivot;
                sharedRt.anchoredPosition = templateRt.anchoredPosition;
                sharedRt.sizeDelta = templateRt.sizeDelta;
                sharedRt.localScale = templateRt.localScale;
                sharedRt.localRotation = templateRt.localRotation;

                Image sharedImg = sharedGo.GetComponent<Image>();
                sharedImg.sprite = templateImage.sprite;
                sharedImg.color = templateImage.color;
                sharedImg.type = templateImage.type;
                sharedImg.preserveAspect = templateImage.preserveAspect;
                sharedImg.raycastTarget = false;
            }
            else if (existingShared != null)
            {
                existingShared.SetSiblingIndex(0);
            }

            if (existingShared != null || templateImage != null)
            {
                for (int i = 0; i < panels.Count; i++)
                {
                    GameObject p = panels[i];
                    if (p == null) continue;
                    Transform bg = p.transform.Find("Background");
                    if (bg != null)
                    {
                        bg.gameObject.SetActive(false);
                    }

                    // Unity UI Panels carry a default semi-transparent white Image (alpha ~0.392)
                    // that was previously hidden by child "Background"; disable it so it doesn't
                    // wash out SharedBackground or flash white during transitions.
                    Image rootPanelImg = p.GetComponent<Image>();
                    if (rootPanelImg != null)
                    {
                        rootPanelImg.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Ensures <paramref name="panel"/> has a CanvasGroup component.
        /// </summary>
        public static CanvasGroup EnsureCanvasGroup(GameObject panel)
        {
            if (panel == null) return null;
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = panel.AddComponent<CanvasGroup>();
            }
            return cg;
        }

        /// <summary>
        /// Resets panel CanvasGroup and RectTransform transform properties to clean resting values.
        /// Keeps CanvasGroup.interactable = true so buttons never flash their faded DisabledColor.
        /// </summary>
        public static void ResetPanelVisuals(GameObject panel, float alpha = 1f)
        {
            if (panel == null) return;
            CanvasGroup cg = EnsureCanvasGroup(panel);
            cg.alpha = alpha;
            cg.interactable = true;
            cg.blocksRaycasts = alpha > 0.5f;

            RectTransform rt = panel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// Smoothly transitions from <paramref name="fromPanel"/> to <paramref name="toPanel"/>,
        /// deactivating any other panels in <paramref name="allPanels"/> immediately.
        /// Invokes <paramref name="beforeReveal"/> right before <paramref name="toPanel"/> fades in
        /// (ideal for updating sakura overlay placement cleanly).
        /// </summary>
        public static IEnumerator SwitchPanelRoutine(
            GameObject fromPanel,
            GameObject toPanel,
            IList<GameObject> allPanels,
            int slideDirection = 1,
            Action<GameObject> beforeReveal = null)
        {
            // Hide unrelated panels cleanly
            if (allPanels != null)
            {
                for (int i = 0; i < allPanels.Count; i++)
                {
                    GameObject p = allPanels[i];
                    if (p != null && p != fromPanel && p != toPanel && p.activeSelf)
                    {
                        ResetPanelVisuals(p, 1f);
                        p.SetActive(false);
                    }
                }
            }

            // Fade out outgoing panel (never set interactable = false so buttons stay crisp, not DisabledColor-faded)
            if (fromPanel != null && fromPanel != toPanel && fromPanel.activeSelf)
            {
                CanvasGroup fromCg = EnsureCanvasGroup(fromPanel);
                RectTransform fromRt = fromPanel.GetComponent<RectTransform>();
                fromCg.interactable = true;
                fromCg.blocksRaycasts = false;

                float startAlpha = fromCg.alpha;
                Vector2 startPos = fromRt != null ? fromRt.anchoredPosition : Vector2.zero;
                Vector2 endPos = new Vector2(-SlideDistance * 0.5f * slideDirection, 0f);

                float elapsed = 0f;
                while (elapsed < DefaultFadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / DefaultFadeOutDuration);
                    float eased = t * t;
                    fromCg.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                    if (fromRt != null)
                    {
                        fromRt.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                    }
                    yield return null;
                }

                ResetPanelVisuals(fromPanel, 1f);
                fromPanel.SetActive(false);
            }

            if (toPanel == null) yield break;

            // Prepare incoming panel (keep interactable = true so buttons remain fully opaque during transition)
            toPanel.SetActive(true);
            beforeReveal?.Invoke(toPanel);

            CanvasGroup toCg = EnsureCanvasGroup(toPanel);
            RectTransform toRt = toPanel.GetComponent<RectTransform>();

            toCg.alpha = 0f;
            toCg.interactable = true;
            toCg.blocksRaycasts = true;

            Vector2 enterStartPos = new Vector2(SlideDistance * slideDirection, 0f);
            Vector3 enterStartScale = new Vector3(0.985f, 0.985f, 1f);
            if (toRt != null)
            {
                toRt.anchoredPosition = enterStartPos;
                toRt.localScale = enterStartScale;
            }

            float inElapsed = 0f;
            while (inElapsed < DefaultFadeInDuration)
            {
                inElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(inElapsed / DefaultFadeInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic

                toCg.alpha = eased;
                if (toRt != null)
                {
                    toRt.anchoredPosition = Vector2.Lerp(enterStartPos, Vector2.zero, eased);
                    toRt.localScale = Vector3.Lerp(enterStartScale, Vector3.one, eased);
                }
                yield return null;
            }

            ResetPanelVisuals(toPanel, 1f);
        }

        /// <summary>
        /// Plays a gentle fade-in entrance on a panel without fading anything out first.
        /// </summary>
        public static IEnumerator PlayPanelEntranceRoutine(GameObject panel, float duration = 0.30f)
        {
            if (panel == null) yield break;

            panel.SetActive(true);
            CanvasGroup cg = EnsureCanvasGroup(panel);
            RectTransform rt = panel.GetComponent<RectTransform>();

            cg.alpha = 0f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            Vector3 startScale = new Vector3(0.98f, 0.98f, 1f);
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = startScale;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                cg.alpha = eased;
                if (rt != null)
                {
                    rt.localScale = Vector3.Lerp(startScale, Vector3.one, eased);
                }
                yield return null;
            }

            ResetPanelVisuals(panel, 1f);
        }

        /// <summary>
        /// Plays a crisp fade-out + gentle scale-down before deactivating a panel (e.g. closing Pause menu).
        /// </summary>
        public static IEnumerator PlayPanelExitRoutine(GameObject panel, float duration = 0.16f)
        {
            if (panel == null || !panel.activeSelf) yield break;

            CanvasGroup cg = EnsureCanvasGroup(panel);
            RectTransform rt = panel.GetComponent<RectTransform>();
            cg.interactable = true;
            cg.blocksRaycasts = false;

            float startAlpha = cg.alpha;
            Vector3 startScale = rt != null ? rt.localScale : Vector3.one;
            Vector3 endScale = new Vector3(0.975f, 0.975f, 1f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t;
                cg.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                if (rt != null)
                {
                    rt.localScale = Vector3.Lerp(startScale, endScale, eased);
                }
                yield return null;
            }

            ResetPanelVisuals(panel, 1f);
            panel.SetActive(false);
        }

        /// <summary>
        /// Attaches tactile hover/press scale animation to all Buttons inside <paramref name="root"/>.
        /// </summary>
        public static void AttachButtonFeedbackInChildren(GameObject root)
        {
            if (root == null) return;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i];
                if (btn == null) continue;
                if (btn.GetComponent<UIButtonFeedback>() == null)
                {
                    btn.gameObject.AddComponent<UIButtonFeedback>();
                }
            }
        }
    }

    /// <summary>
    /// Adds subtle, responsive hover lift and click squish feel to a UI Button.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.045f;
        [SerializeField] private float pressScale = 0.955f;
        [SerializeField] private float speed = 16f;

        private Button button;
        private Vector3 baseScale;
        private bool isHovered;
        private bool isPressed;
        private bool baseCaptured;

        private void Awake()
        {
            CaptureBaseScale();
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            CaptureBaseScale();
            isHovered = false;
            isPressed = false;
            transform.localScale = baseScale;
        }

        private void OnDisable()
        {
            isHovered = false;
            isPressed = false;
            if (baseCaptured)
            {
                transform.localScale = baseScale;
            }
        }

        private void CaptureBaseScale()
        {
            if (!baseCaptured)
            {
                baseScale = transform.localScale;
                if (baseScale.sqrMagnitude < 0.001f) baseScale = Vector3.one;
                baseCaptured = true;
            }
        }

        private void Update()
        {
            float targetMul = 1f;
            bool canInteract = button == null || button.IsInteractable();
            if (canInteract)
            {
                if (isPressed) targetMul = pressScale;
                else if (isHovered) targetMul = hoverScale;
            }

            Vector3 target = baseScale * targetMul;
            if ((transform.localScale - target).sqrMagnitude > 0.00001f)
            {
                float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
                transform.localScale = Vector3.Lerp(transform.localScale, target, t);
            }
            else if (transform.localScale != target)
            {
                transform.localScale = target;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
        public void OnPointerExit(PointerEventData eventData) { isHovered = false; isPressed = false; }
        public void OnPointerDown(PointerEventData eventData) => isPressed = true;
        public void OnPointerUp(PointerEventData eventData) => isPressed = false;
    }
}

