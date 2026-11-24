using UnityEngine;

namespace Utsuru.UI
{
    /// <summary>
    /// Ensures that the Sakura Falling Petals particle animation appears on all screens
    /// (HomeScreen, Options tabs, Credits, and all Login panels) exactly between the
    /// Background image (sibling index 0) and the UI elements (sibling index 2+).
    /// Prevents duplicate emitters and maintains continuous smooth falling without reset.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SakuraSceneIntegrator : MonoBehaviour
    {
        private void Awake()
        {
            IntegrateSakuraInScene();
        }

        private void Start()
        {
            IntegrateSakuraInScene();
        }

        public static void IntegrateSakuraInScene()
        {
            // 1. Check HomeScreen scene panels
            if (HomeScreenUIManager.Instance != null)
            {
                var hm = HomeScreenUIManager.Instance;
                EnsureEmitterForPanel(GetPanelTransform(hm, "homeScreenPanel"));
                EnsureEmitterForPanel(GetPanelTransform(hm, "creditsPanel"));
                EnsureEmitterForPanel(GetPanelTransform(hm, "settingsOptionsPanel"));
            }

            // 2. Check LoginPage scene panels
            var authManager = Object.FindAnyObjectByType<AuthenticationUIManager>();
            if (authManager != null)
            {
                EnsureEmitterForPanel(GetPanelTransform(authManager, "loginPanel"));
                EnsureEmitterForPanel(GetPanelTransform(authManager, "signUpPanel"));
                EnsureEmitterForPanel(GetPanelTransform(authManager, "emailSentPanel"));
                EnsureEmitterForPanel(GetPanelTransform(authManager, "forgotPasswordPanel"));
                EnsureEmitterForPanel(GetPanelTransform(authManager, "resetPasswordPanel"));
            }
        }

        private static Transform GetPanelTransform(object target, string fieldName)
        {
            if (target == null) return null;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                var go = field.GetValue(target) as GameObject;
                if (go != null) return go.transform;
            }
            return null;
        }

        public static SakuraPetalEmitter EnsureEmitterForPanel(Transform panelTransform)
        {
            if (panelTransform == null) return null;

            // Check if emitter already exists under this panel
            var existing = panelTransform.GetComponentInChildren<SakuraPetalEmitter>(true);
            if (existing != null)
            {
                // Ensure sibling index is 1 (right after Background at 0)
                if (existing.transform.GetSiblingIndex() != 1)
                {
                    existing.transform.SetSiblingIndex(1);
                }
                return existing;
            }

            // Create emitter GameObject
            var emitterObj = new GameObject("SakuraPetalEmitter", typeof(RectTransform), typeof(SakuraPetalEmitter));
            emitterObj.transform.SetParent(panelTransform, false);

            var rt = emitterObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            // Place at sibling index 1 (between Background at 0 and UI elements at 2+)
            if (panelTransform.childCount > 1)
            {
                emitterObj.transform.SetSiblingIndex(1);
            }

            var emitter = emitterObj.GetComponent<SakuraPetalEmitter>();
            emitter.InitializePetals();
            return emitter;
        }
    }
}
