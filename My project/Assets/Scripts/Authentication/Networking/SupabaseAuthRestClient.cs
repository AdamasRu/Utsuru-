using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Utsuru.Authentication.Configuration;

namespace Utsuru.Authentication.Networking
{
    /// <summary>
    /// Base response model for Supabase REST requests.
    /// </summary>
    public class SupabaseRestResponse
    {
        public bool IsSuccess { get; }
        public int StatusCode { get; }
        public string RawJson { get; }
        public string ErrorMessage { get; }
        public string ErrorCode { get; }

        public SupabaseRestResponse(bool isSuccess, int statusCode, string rawJson, string errorMessage, string errorCode = null)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            RawJson = rawJson;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }

        public static SupabaseRestResponse Success(int statusCode, string rawJson) =>
            new SupabaseRestResponse(true, statusCode, rawJson, null, null);

        public static SupabaseRestResponse Failure(int statusCode, string errorMessage, string rawJson = null, string errorCode = null) =>
            new SupabaseRestResponse(false, statusCode, rawJson, errorMessage, errorCode);
    }

    /// <summary>
    /// Strongly-typed response model for Supabase REST requests.
    /// </summary>
    /// <typeparam name="T">Deserialized payload type.</typeparam>
    public class SupabaseRestResponse<T> : SupabaseRestResponse
    {
        public T Data { get; }

        public SupabaseRestResponse(bool isSuccess, int statusCode, T data, string rawJson, string errorMessage, string errorCode = null)
            : base(isSuccess, statusCode, rawJson, errorMessage, errorCode)
        {
            Data = data;
        }

        public static SupabaseRestResponse<T> Success(int statusCode, T data, string rawJson) =>
            new SupabaseRestResponse<T>(true, statusCode, data, rawJson, null, null);

        public new static SupabaseRestResponse<T> Failure(int statusCode, string errorMessage, string rawJson = null, string errorCode = null) =>
            new SupabaseRestResponse<T>(false, statusCode, default, rawJson, errorMessage, errorCode);
    }

    /// <summary>
    /// Reusable REST client for Supabase Authentication endpoints.
    /// Communicates over standard HTTPS using HttpClient and Newtonsoft.Json.
    /// Follows UTSURU offline-first architecture: no background polling, no realtime sockets.
    /// </summary>
    public class SupabaseAuthRestClient
    {
        // Reusable static HttpClient instance to prevent socket exhaustion
        private static readonly HttpClient SharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        private readonly SupabaseProjectConfig config;
        private readonly bool enableDebugLogging;

        /// <summary>
        /// Initializes a new instance of the SupabaseAuthRestClient.
        /// </summary>
        /// <param name="config">The SupabaseProjectConfig ScriptableObject containing Project URL and Publishable Key.</param>
        /// <param name="enableDebugLogging">Whether to log sanitized diagnostic information.</param>
        public SupabaseAuthRestClient(SupabaseProjectConfig config, bool enableDebugLogging = false)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.enableDebugLogging = enableDebugLogging;
        }

        /// <summary>
        /// Sends an asynchronous GET request to a Supabase endpoint.
        /// </summary>
        /// <param name="endpoint">The relative endpoint path (e.g., "/auth/v1/settings").</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A SupabaseRestResponse containing status code and raw response body.</returns>
        public async Task<SupabaseRestResponse> SendGetAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            return await SendRequestInternalAsync<object>(HttpMethod.Get, endpoint, null, false, null, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous GET request and deserializes the JSON response into type T.
        /// </summary>
        /// <typeparam name="T">The expected response data type.</typeparam>
        /// <param name="endpoint">The relative endpoint path.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A SupabaseRestResponse&lt;T&gt; with the deserialized payload.</returns>
        public async Task<SupabaseRestResponse<T>> SendGetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        {
            var raw = await SendRequestInternalAsync<T>(HttpMethod.Get, endpoint, null, true, null, cancellationToken);
            return raw;
        }

        /// <summary>
        /// Sends an asynchronous POST request with a JSON payload to a Supabase endpoint.
        /// </summary>
        /// <typeparam name="TRequest">The request payload type.</typeparam>
        /// <param name="endpoint">The relative endpoint path.</param>
        /// <param name="payload">The request object to serialize into JSON.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A SupabaseRestResponse containing status code and raw response body.</returns>
        public async Task<SupabaseRestResponse> SendPostAsync<TRequest>(string endpoint, TRequest payload, CancellationToken cancellationToken = default)
        {
            return await SendRequestInternalAsync<object>(HttpMethod.Post, endpoint, payload, false, null, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous POST request with a JSON payload and deserializes the JSON response.
        /// </summary>
        /// <typeparam name="TRequest">The request payload type.</typeparam>
        /// <typeparam name="TResponse">The expected response data type.</typeparam>
        /// <param name="endpoint">The relative endpoint path.</param>
        /// <param name="payload">The request object to serialize into JSON.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A SupabaseRestResponse&lt;TResponse&gt; with the deserialized payload.</returns>
        public async Task<SupabaseRestResponse<TResponse>> SendPostAsync<TRequest, TResponse>(string endpoint, TRequest payload, CancellationToken cancellationToken = default)
        {
            return await SendRequestInternalAsync<TResponse>(HttpMethod.Post, endpoint, payload, true, null, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with a JSON payload and Bearer token, then deserializes the JSON response.
        /// Used for authenticated user profile/password updates (PUT /auth/v1/user).
        /// </summary>
        public async Task<SupabaseRestResponse<TResponse>> SendPutAsync<TRequest, TResponse>(
            string endpoint,
            TRequest payload,
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            return await SendRequestInternalAsync<TResponse>(HttpMethod.Put, endpoint, payload, true, bearerToken, cancellationToken);
        }

        /// <summary>
        /// Sends an asynchronous PUT request with a JSON payload and Bearer token.
        /// </summary>
        public async Task<SupabaseRestResponse> SendPutAsync<TRequest>(
            string endpoint,
            TRequest payload,
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            return await SendRequestInternalAsync<object>(HttpMethod.Put, endpoint, payload, false, bearerToken, cancellationToken);
        }

        /// <summary>
        /// Core request dispatch and error-handling pipeline.
        /// </summary>
        private async Task<SupabaseRestResponse<TResult>> SendRequestInternalAsync<TResult>(
            HttpMethod method,
            string endpoint,
            object payload,
            bool deserializePayload,
            string bearerToken,
            CancellationToken cancellationToken)
        {
            // Validate configuration before dispatching
            string configError = null;
            if (config == null || !config.IsValidConfig(out configError))
            {
                string safeConfigError = configError ?? "SupabaseProjectConfig is missing or invalid.";
                LogWarning($"Configuration error: {safeConfigError}");
                return SupabaseRestResponse<TResult>.Failure(0, safeConfigError);
            }

            // Build request URL
            string baseUrl = config.ProjectUrl;
            string cleanEndpoint = (endpoint ?? string.Empty).TrimStart('/');
            string fullUrl = $"{baseUrl}/{cleanEndpoint}";

            if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out Uri requestUri))
            {
                string invalidUrlError = "Failed to construct valid absolute URI from Project URL and endpoint.";
                LogWarning(invalidUrlError);
                return SupabaseRestResponse<TResult>.Failure(0, invalidUrlError);
            }

            try
            {
                using (var request = new HttpRequestMessage(method, requestUri))
                {
                    // Publishable Key is sent strictly via the 'apikey' header.
                    // Never send the Publishable Key as a Bearer access token.
                    request.Headers.Add("apikey", config.PublishableKey);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    if (!string.IsNullOrEmpty(bearerToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                    }

                    // Serialize payload if provided
                    if (payload != null && (method == HttpMethod.Post || method == HttpMethod.Put))
                    {
                        string jsonContent = JsonConvert.SerializeObject(payload);
                        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    }

                    LogDebug($"Sending {method} request to {requestUri.AbsolutePath}");

                    using (var response = await SharedHttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                    {
                        int statusCode = (int)response.StatusCode;
                        string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        LogDebug($"Received response HTTP {statusCode} for {requestUri.AbsolutePath}");

                        if (response.IsSuccessStatusCode)
                        {
                            if (deserializePayload && !string.IsNullOrWhiteSpace(responseBody))
                            {
                                try
                                {
                                    TResult deserialized = JsonConvert.DeserializeObject<TResult>(responseBody);
                                    return SupabaseRestResponse<TResult>.Success(statusCode, deserialized, responseBody);
                                }
                                catch (JsonException jsonEx)
                                {
                                    string parseError = "Failed to parse successful JSON response from server.";
                                    LogWarning($"{parseError} Error: {jsonEx.Message}");
                                    return SupabaseRestResponse<TResult>.Failure(statusCode, parseError, responseBody);
                                }
                            }

                            return SupabaseRestResponse<TResult>.Success(statusCode, default, responseBody);
                        }

                        // Parse standard Supabase error response if available
                        var (parsedErrorMessage, parsedErrorCode) = ExtractSupabaseError(responseBody, statusCode, response.ReasonPhrase);
                        LogWarning($"HTTP {statusCode} on {requestUri.AbsolutePath}: {parsedErrorMessage} (ErrorCode: {parsedErrorCode ?? "none"})");
                        return SupabaseRestResponse<TResult>.Failure(statusCode, parsedErrorMessage, responseBody, parsedErrorCode);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogWarning($"Request to {requestUri.AbsolutePath} was cancelled.");
                    return SupabaseRestResponse<TResult>.Failure(0, "Request was cancelled.", null, "cancelled");
                }

                LogWarning($"Request to {requestUri.AbsolutePath} timed out.");
                return SupabaseRestResponse<TResult>.Failure(0, "Request timed out. Please check your internet connection.", null, "timeout");
            }
            catch (HttpRequestException httpEx)
            {
                // Network or DNS failure
                LogWarning($"Network error connecting to {requestUri.Host}: {httpEx.Message}");
                return SupabaseRestResponse<TResult>.Failure(0, $"Network error: Unable to connect to Supabase server ({requestUri.Host}). Please check your connection.", null, "network_error");
            }
            catch (Exception ex)
            {
                // General unexpected exception
                LogWarning($"Unexpected error during request: {ex.Message}");
                return SupabaseRestResponse<TResult>.Failure(0, $"Unexpected request error: {ex.Message}", null, "unexpected_error");
            }
        }

        /// <summary>
        /// Extracts user-friendly error messages and error codes from standard Supabase error response JSON.
        /// Supabase Auth returns {"msg": "..."} or {"error_description": "..."} or {"message": "..."}
        /// along with optional {"error_code": "..."} or {"error": "..."}.
        /// </summary>
        private static (string message, string errorCode) ExtractSupabaseError(string responseBody, int statusCode, string fallbackReason)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return ($"Server returned HTTP {statusCode} ({fallbackReason ?? "Error"}).", null);
            }

            try
            {
                var errorObj = JsonConvert.DeserializeObject<SupabaseErrorPayload>(responseBody);
                string message = errorObj?.GetFormattedMessage();
                string code = errorObj?.ErrorCode ?? errorObj?.Error;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return (message, code);
                }
                return ($"Server returned HTTP {statusCode} ({fallbackReason ?? "Error"}).", code);
            }
            catch
            {
                // Non-JSON response (e.g. cloudflare HTML error page)
            }

            return ($"Server returned HTTP {statusCode} ({fallbackReason ?? "Error"}).", null);
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[SupabaseAuthRestClient] {message}");
            }
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning($"[SupabaseAuthRestClient] {message}");
        }

        [JsonObject]
        private class SupabaseErrorPayload
        {
            [JsonProperty("code")]
            public int? Code { get; set; }

            [JsonProperty("error_code")]
            public string ErrorCode { get; set; }

            [JsonProperty("msg")]
            public string Msg { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("error_description")]
            public string ErrorDescription { get; set; }

            public string GetFormattedMessage()
            {
                if (!string.IsNullOrWhiteSpace(Msg)) return Msg;
                if (!string.IsNullOrWhiteSpace(Message)) return Message;
                if (!string.IsNullOrWhiteSpace(ErrorDescription)) return ErrorDescription;
                if (!string.IsNullOrWhiteSpace(Error)) return Error;
                return null;
            }
        }
    }
}
