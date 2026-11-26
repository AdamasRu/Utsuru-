using System;
using System.Threading.Tasks;
using UnityEngine;
using Utsuru.Authentication.Configuration;
using Utsuru.Authentication.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utsuru.Authentication.Testing
{
    /// <summary>
    /// Developer-only manual connection testing component for Supabase.
    /// Strictly manual: will NEVER execute automatically in Awake, Start, or scene load.
    /// Exposes a context menu action and Inspector button to test the public GET /auth/v1/settings endpoint.
    /// </summary>
    [AddComponentMenu("UTSURU/Authentication/Supabase Connection Tester")]
    public class SupabaseConnectionTester : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The SupabaseProjectConfig ScriptableObject asset containing the Project URL and Publishable Key.")]
        [SerializeField] private SupabaseProjectConfig projectConfig;

        [Header("Test Results (Read-Only)")]
        [Tooltip("Indicates whether a manual test has been executed.")]
        [SerializeField] private bool testCompleted;

        [Tooltip("Indicates whether the last manual test succeeded with HTTP 200.")]
        [SerializeField] private bool testSuccessful;

        [Tooltip("The HTTP status code returned by the last test.")]
        [SerializeField] private int lastHttpStatusCode;

        [Tooltip("Safe summary of the last test result.")]
        [TextArea(3, 6)]
        [SerializeField] private string lastResultSummary = "No test executed yet. Right-click component or click 'Test Supabase Connection' below.";

        /// <summary>
        /// Gets whether a manual test has been executed.
        /// </summary>
        public bool TestCompleted => testCompleted;

        /// <summary>
        /// Gets whether the last manual test succeeded.
        /// </summary>
        public bool TestSuccessful => testSuccessful;

        /// <summary>
        /// Gets the HTTP status code returned by the last test.
        /// </summary>
        public int LastHttpStatusCode => lastHttpStatusCode;

        /// <summary>
        /// Gets the safe summary of the last test result.
        /// </summary>
        public string LastResultSummary => lastResultSummary;

        // Guard against duplicate concurrent test triggers
        private bool isTestRunning;

        // Explicit empty Awake and Start ensuring NO automatic execution at runtime or scene loading
        private void Awake()
        {
            // Strictly manual execution only: do not initiate Supabase connection on Awake
        }

        private void Start()
        {
            // Strictly manual execution only: do not initiate Supabase connection on Start
        }

        /// <summary>
        /// Explicit developer-only trigger for testing the Supabase connection.
        /// Can be called from the Inspector Context Menu or custom Editor button.
        /// </summary>
        [ContextMenu("Test Supabase Connection")]
        public void RunConnectionTest()
        {
            if (isTestRunning)
            {
                Debug.LogWarning("[SupabaseConnectionTester] A connection test is already in progress. Please wait.", this);
                return;
            }

            RunConnectionTestAsync();
        }

        private async void RunConnectionTestAsync()
        {
            isTestRunning = true;
            testCompleted = false;
            testSuccessful = false;
            lastHttpStatusCode = 0;
            lastResultSummary = "Connecting to Supabase Auth endpoint (GET /auth/v1/settings)...";

            Debug.Log("[SupabaseConnectionTester] Initiating manual Supabase connection test...", this);

            // Step 1: Validate configuration presence
            if (projectConfig == null)
            {
                string missingConfigMsg = "Test Aborted: SupabaseProjectConfig asset is not assigned in the Inspector.";
                RecordResult(false, 0, missingConfigMsg);
                Debug.LogError($"[SupabaseConnectionTester] {missingConfigMsg}", this);
                isTestRunning = false;
                return;
            }

            // Step 2: Validate configuration values
            if (!projectConfig.IsValidConfig(out string configError))
            {
                string invalidConfigMsg = $"Test Aborted: Configuration validation failed: {configError}";
                RecordResult(false, 0, invalidConfigMsg);
                Debug.LogError($"[SupabaseConnectionTester] {invalidConfigMsg}", this);
                isTestRunning = false;
                return;
            }

            // Step 3: Dispatch manual test request to GET /auth/v1/settings
            try
            {
                var restClient = new SupabaseAuthRestClient(projectConfig, enableDebugLogging: true);
                var response = await restClient.SendGetAsync("/auth/v1/settings");

                lastHttpStatusCode = response.StatusCode;

                if (response.StatusCode == 200)
                {
                    string successMsg = $"Connection successful! HTTP 200 OK. Supabase Auth settings endpoint responded correctly for project '{projectConfig.ProjectUrl}'.";
                    RecordResult(true, 200, successMsg);
                    Debug.Log($"<color=#00AA00><b>[SupabaseConnectionTester] {successMsg}</b></color>", this);
                }
                else if (response.StatusCode == 401 || response.StatusCode == 403)
                {
                    string authErrorMsg = $"Authentication/Configuration Error (HTTP {response.StatusCode}): The server rejected the request. Please verify that your Publishable Key is correct and active. (Credentials are never printed for security).";
                    RecordResult(false, response.StatusCode, authErrorMsg);
                    Debug.LogError($"[SupabaseConnectionTester] {authErrorMsg}", this);
                }
                else if (response.StatusCode == 0)
                {
                    string networkErrorMsg = $"Network / DNS Connection Failure: {response.ErrorMessage}";
                    RecordResult(false, 0, networkErrorMsg);
                    Debug.LogError($"[SupabaseConnectionTester] {networkErrorMsg}", this);
                }
                else
                {
                    string generalErrorMsg = $"Server returned HTTP {response.StatusCode}: {response.ErrorMessage}";
                    RecordResult(false, response.StatusCode, generalErrorMsg);
                    Debug.LogWarning($"[SupabaseConnectionTester] {generalErrorMsg}", this);
                }
            }
            catch (Exception ex)
            {
                string exceptionMsg = $"Unexpected test exception: {ex.Message}";
                RecordResult(false, 0, exceptionMsg);
                Debug.LogError($"[SupabaseConnectionTester] {exceptionMsg}", this);
            }
            finally
            {
                isTestRunning = false;
            }
        }

        private void RecordResult(bool success, int statusCode, string summary)
        {
            testCompleted = true;
            testSuccessful = success;
            lastHttpStatusCode = statusCode;
            lastResultSummary = summary;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom Inspector editor providing a direct clickable button for running the connection test.
    /// </summary>
    [CustomEditor(typeof(SupabaseConnectionTester))]
    public class SupabaseConnectionTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12);

            var tester = (SupabaseConnectionTester)target;

            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
            if (GUILayout.Button("Test Supabase Connection (GET /auth/v1/settings)", GUILayout.Height(32)))
            {
                tester.RunConnectionTest();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.HelpBox(
                "This test connects to GET /auth/v1/settings using the configured Project URL and Publishable Key.\n" +
                "It strictly runs on-demand and will never execute automatically at game startup.",
                MessageType.Info);
        }
    }
#endif
}
