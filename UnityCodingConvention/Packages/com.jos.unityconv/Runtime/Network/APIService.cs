using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityConv.Network
{
    public class APIService
    {
        public static APIStore store;
        private const int MaxAttempts = 3;
        private const int RetryDelayMs = 700;
        private const int DefaultTimeoutSec = 10;
        private const int DownloadTimeoutSec = 180;

        /// <summary>로그인으로 받은 액세스 토큰. 모든 요청에 Authorization: Bearer 로 전송됩니다.</summary>
        public static string AccessToken { get; set; }
        private static Task<bool> reissueTask;

        /// <summary>파일 응답의 확장자 판별 (없으면 extension은 null), 프로젝트에서 등록합니다.</summary>
        public static Func<byte[], string> ExtensionDetector { get; set; }

        public static async Task<UploadFileResponse> PostFileAsync(string urlName, string fileName, byte[] fileData, object[] keys = null)
        {
            if (fileData == null || fileData.Length == 0)
            {
                Debug.LogError($"파일 업로드 실패: 업로드할 파일 데이터가 없습니다. ({fileName})");
                return null;
            }

            string url = BuildUrl(urlName, keys);
            if (url == null) return default;

            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(fileData),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            request.SetRequestHeader("Accept", "application/json");
            ApplyAuth(request);

            await request.SendWebRequest();
            CookieStore.Capture(request);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"파일 업로드 실패\n" +
                    $"URL: {url}\n" +
                    $"HTTP: {request.responseCode}\n" +
                    $"오류: {request.error}\n" +
                    $"응답: {request.downloadHandler.text}");
                return null;
            }

            APIResponse<UploadFileResponse> response = ParseJson<APIResponse<UploadFileResponse>>(request.downloadHandler.text);
            return response?.data;
        }

        /// <summary>
        /// multipart/form-data 텍스트 필드로 POST하고, 응답 바디 전체를 TResponse로 직접 파싱합니다. 실패 시 null.
        /// </summary>
        public static async Task<TResponse> PostMultipartDirectAsync<TResponse>(string urlName, Dictionary<string, string> fields, object[] keys = null)
            where TResponse : class
        {
            string url = BuildUrl(urlName, keys);
            if (url == null) return default;

            APIResult result = await SendWithRetryAsync(() =>
            {
                var sections = new List<IMultipartFormSection>();
                foreach (KeyValuePair<string, string> field in fields)
                    if (!string.IsNullOrEmpty(field.Value)) sections.Add(new MultipartFormDataSection(field.Key, field.Value)); // 빈 값은 섹션을 만들 수 없음
                return UnityWebRequest.Post(url, sections);
            }, DefaultTimeoutSec);

            if (!result.ok)
            {
                Debug.LogError($"[APIService] Multipart Post 실패 ({url}): {result.error}");
                return null;
            }

            TResponse parsed = ParseJson<TResponse>(result.text);
            if (parsed == null)
                Debug.LogError($"[APIService] Multipart Post 응답 파싱 실패 ({url}): {result.text}");
            return parsed;
        }

        public static async Task<bool> PostAsync<T>(string urlName, T formData, object[] keys = null, CancellationToken cancellationToken = default)
        {
            string url = BuildUrl(urlName, keys);
            if (url == null) return default;

            Debug.Log(url);
            var result = await PostJsonWithRetryAsync(url, JsonUtility.ToJson(formData), 30, cancellationToken);
            if (result.canceled)
                return false;

            if (!result.ok)
            {
                Debug.LogError($"[APIService] Post 실패: {result.error}");
                return false;
            }

            var parsed = ParseJson<APIResponse<T>>(result.text);
            if (parsed == null || !parsed.success)
            {
                Debug.LogError($"[APIService] Post 거부: {result.text}");
                return false;
            }

            return true;
        }


        /// <summary>요청 바디(TRequest)와 응답 data(TResponse)의 형식이 다를 때 사용합니다.</summary>
        public static async Task<TResponse> PostResponeAsync<TRequest, TResponse>(string urlName, TRequest formData, object[] keys = null)
        {
            APIResponse<TResponse> response = await PostResponseAsync<TRequest, TResponse>(urlName, formData, keys);
            if (response == null || !response.success)
                return default;

            return response.data;
        }

        public static Task<APIResponse<T>> PostResponseAsync<T>(string urlName, T formData, object[] keys = null)
            => PostResponseAsync<T, T>(urlName, formData, keys);

        public static async Task<APIResponse<TResponse>> PostResponseAsync<TRequest, TResponse>(string urlName, TRequest formData, object[] keys = null)
        {
            APIResult result = await PostFormAsync(urlName, formData, keys);
            if (result == null)
                return null;

            APIResponse<TResponse> parsed = ParseJson<APIResponse<TResponse>>(result.text);
            if (parsed == null)
            {
                Debug.LogError($"[APIService] Post 응답 파싱 실패: {result.text}");
                return null;
            }

            if (!parsed.success)
                Debug.LogWarning($"[APIService] Post 거부: {result.text}");

            return parsed;
        }
        /// <summary>
        /// 응답이 APIResponse로 감싸지지 않은 경우, 응답 바디 전체를 TResponse로 직접 파싱합니다.
        /// </summary>
        public static async Task<TResponse> PostDirectAsync<TRequest, TResponse>(string urlName, TRequest formData, object[] keys = null)
            where TResponse : class
        {
            APIResult result = await PostFormAsync(urlName, formData, keys);
            if (result == null)
                return null;

            if (!result.ok)
            {
                Debug.LogError($"[APIService] Post 실패: {result.error}");
                return null;
            }

            TResponse parsed = ParseJson<TResponse>(result.text);
            if (parsed == null)
                Debug.LogError($"[APIService] Post 응답 파싱 실패: {result.text}");

            return parsed;
        }

        private static async Task<APIResult> PostFormAsync<TRequest>(string urlName, TRequest formData, object[] keys)
        {
            string url = BuildUrl(urlName, keys);
            if (url == null) return default;

            Debug.Log(url);
            APIResult result = null;
            if (formData == null)
                result = await PostJsonWithRetryAsync(url, "", 30);
            else
                result = await PostJsonWithRetryAsync(url, JsonUtility.ToJson(formData), 30);

            if (!result.ok && string.IsNullOrWhiteSpace(result.text))
            {
                Debug.LogError($"[APIService] Post 실패: {result.error}");
                return null;
            }
            return result;
        }
        public static async Task<APIFileResponse> GetFileAsync(string urlName, string[] keys)
        {
            string url = BuildUrl(urlName, keys);
            if (url == null) return default;


            var get = await SendWithRetryAsync(() => UnityWebRequest.Get(url), DownloadTimeoutSec, binary: true);

            if (!get.ok)
            {
                Debug.LogError($"[APIService] GET File 실패 ({url}): {get.error}");
                return default;
            }

            byte[] bytes = get.bytes;

            byte[] unwrapped = await Task.Run(() => TryDecodeBufferJson(bytes));

            if (unwrapped != null)
            {
                Debug.LogWarning("[APIService] 서버가 Buffer-JSON 형식으로 응답 — 원본 바이트로 복원했습니다. (서버 수정 권장)");
                bytes = unwrapped;
            }
            APIFileResponse data = new APIFileResponse
            {
                bytes = bytes,
                extension = ExtensionDetector?.Invoke(bytes)
            };
            return data;
        }
        public static async Task<APIFileResponse> GetFileAsync(string urlName)
        {
            string url = BuildDownloadUrl(urlName);
            if (url == null) return default;

            var get = await SendWithRetryAsync(() => UnityWebRequest.Get(url), DownloadTimeoutSec, binary: true);

            if (!get.ok)
            {
                Debug.LogError($"[APIService] GET File 실패 ({url}): {get.error}");
                return default;
            }

            byte[] bytes = get.bytes;

            byte[] unwrapped = await Task.Run(() => TryDecodeBufferJson(bytes));

            if (unwrapped != null)
            {
                Debug.LogWarning("[APIService] 서버가 Buffer-JSON 형식으로 응답 — 원본 바이트로 복원했습니다. (서버 수정 권장)");
                bytes = unwrapped;
            }
            APIFileResponse data = new APIFileResponse
            {
                bytes = bytes,
                extension = ExtensionDetector?.Invoke(bytes)
            };
            return data;
        }

        public static async Task<AudioClip> DecodeAudioClipAsync(
            APIFileResponse fileResponse,
            string fileName,
            string mimeType)
        {
            if (fileResponse?.bytes == null || fileResponse.bytes.Length == 0)
                return null;

            string extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = fileResponse.extension;
            if (string.IsNullOrWhiteSpace(extension))
                extension = GetAudioExtension(mimeType);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".mp3";
            if (!extension.StartsWith("."))
                extension = $".{extension}";

            string temporaryPath = Path.Combine(
                Application.temporaryCachePath,
                $"server_audio_{Guid.NewGuid():N}{extension}");

            try
            {
                await File.WriteAllBytesAsync(temporaryPath, fileResponse.bytes);

                string uri = new Uri(temporaryPath).AbsoluteUri;
                using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                    uri,
                    GetAudioType(extension, mimeType));

                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"오디오 디코딩 실패: {request.error}");
                    return null;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null)
                    clip.name = Path.GetFileNameWithoutExtension(fileName);

                return clip;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        public static async Task<T> GetAsync<T>(string urlName, object[] keys = null, CancellationToken cancellationToken = default)
        {
            string url = BuildUrl(urlName, keys);
            if (url == null) return default;

            var get = await SendWithRetryAsync(() => UnityWebRequest.Get(url), DefaultTimeoutSec, false, cancellationToken);
            if (get.canceled)
                return default;

            if (!get.ok)
            {
                Debug.LogError($"[APIService] GET 실패 ({url}): {get.error}");
                return default;
            }

            if (string.IsNullOrWhiteSpace(get.text) || get.text.Trim() == "null")
                return default;

            Debug.Log(get.text);
            APIResponse<T> response = ParseJson<APIResponse<T>>(get.text);
            if (response == null || !response.success || response.data == null)
            {
                Debug.LogError($"[APIService] GET 응답 파싱 실패: {get.text}");
                return default;
            }
            T result = response.data;

            return result;
        }



        /// <summary>
        /// 응답이 APIResponse로 감싸지지 않은 GET 요청. 응답 바디 전체를 TResponse로 직접 파싱합니다.
        /// </summary>
        public static async Task<TResponse> GetDirectAsync<TResponse>(string urlName, object[] keys = null)
            where TResponse : class
        {
            string url = BuildUrl(urlName, keys);
            if (url == null) return default;

            var get = await SendWithRetryAsync(() => UnityWebRequest.Get(url), DefaultTimeoutSec);
            if (!get.ok)
            {
                Debug.LogError($"[APIService] GET 실패 ({url}): {get.error}");
                return null;
            }

            TResponse parsed = ParseJson<TResponse>(get.text);
            if (parsed == null)
                Debug.LogError($"[APIService] GET 응답 파싱 실패 ({url}): {get.text}");

            return parsed;
        }

        #region =========== SAFETY ===========

        /// <summary>취소되면 요청을 중단하고 canceled 결과를 반환합니다 (토큰 재발급은 취소하지 않음).</summary>
        public static async Task<APIResult> SendWithRetryAsync(Func<UnityWebRequest> factory, int timeoutSeconds, bool binary = false, CancellationToken cancellationToken = default)
        {
            APIResult result = await SendOnceAsync(factory, timeoutSeconds, binary, cancellationToken);

            // 액세스 토큰 만료 → refreshToken 쿠키로 재발급 후 1회 재시도
            if (!result.canceled && IsUnauthorized(result.statusCode) && !string.IsNullOrEmpty(AccessToken) && CanReissue() && await ReissueTokenAsync())
                result = await SendOnceAsync(factory, timeoutSeconds, binary, cancellationToken);

            return result;
        }

        private static async Task<APIResult> SendOnceAsync(Func<UnityWebRequest> factory, int timeoutSeconds, bool binary, CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return CanceledResult();

                using UnityWebRequest www = factory();
                www.timeout = timeoutSeconds;
                ApplyAuth(www);
                using (cancellationToken.Register(www.Abort))
                {
                    await www.SendWebRequest();
                }
                if (cancellationToken.IsCancellationRequested)
                    return CanceledResult();
                CookieStore.Capture(www);

                if (www.result == UnityWebRequest.Result.Success)
                {
                    return binary
                        ? new APIResult { ok = true, statusCode = www.responseCode, bytes = www.downloadHandler.data }
                        : new APIResult { ok = true, statusCode = www.responseCode, text = www.downloadHandler.text };
                }

                // 연결 실패(요청이 서버에 도달하지 못함)만 재시도 — 중복 처리 위험 없음
                if (www.result == UnityWebRequest.Result.ConnectionError && attempt < MaxAttempts)
                {
                    //  Debug.LogWarning($"[APIService] {label} 연결 실패 — 재시도 {attempt}/{MaxAttempts - 1}");
                    try
                    {
                        await Task.Delay(RetryDelayMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return CanceledResult();
                    }
                    continue;
                }

                string responseText = binary ? null : www.downloadHandler.text;
                string bodyText = string.IsNullOrEmpty(responseText) ? "" : $"\n응답: {responseText}";
                return new APIResult
                {
                    ok = false,
                    statusCode = www.responseCode,
                    text = responseText,
                    error = $"{www.error}{bodyText}"
                };
            }
            return new APIResult { ok = false, error = "재시도 횟수 초과" };
        }

        private static bool CanReissue() => store != null && !string.IsNullOrEmpty(store.TokenReissuePath);

        /// <summary>등록된 API 이름으로 URL 생성, APIStore가 없거나 이름이 없으면 null</summary>
        private static string BuildUrl(string urlName, object[] keys)
        {
            if (store == null)
            {
                Debug.LogError($"[APIService] APIStore가 설정되지 않았습니다. NetworkManager를 씬에 두거나 APIService.store를 지정하세요. ({urlName})");
                return null;
            }
            return keys != null ? store.GetUrl(urlName, keys) : store.GetUrl(urlName);
        }

        /// <summary>경로나 전체 URL로 다운로드 URL 생성, APIStore가 없으면 null</summary>
        private static string BuildDownloadUrl(string url)
        {
            if (store == null)
            {
                Debug.LogError($"[APIService] APIStore가 설정되지 않았습니다. NetworkManager를 씬에 두거나 APIService.store를 지정하세요. ({url})");
                return null;
            }
            return store.DownloadUrl(url);
        }

        private static APIResult CanceledResult() => new APIResult { ok = false, canceled = true, error = "요청 취소" };

        private static bool IsUnauthorized(long statusCode) => statusCode == 401 || statusCode == 403;

        private static void ApplyAuth(UnityWebRequest request)
        {
            CookieStore.Apply(request);
            if (!string.IsNullOrEmpty(AccessToken))
                request.SetRequestHeader("Authorization", $"Bearer {AccessToken}");
        }

        /// <summary>refreshToken 쿠키로 액세스 토큰을 재발급합니다. 동시에 여러 요청이 만료돼도 재발급은 한 번만 수행됩니다.</summary>
        public static Task<bool> ReissueTokenAsync()
        {
            return reissueTask ??= ReissueTokenInternalAsync();
        }

        private static async Task<bool> ReissueTokenInternalAsync()
        {
            try
            {
                string url = store.DownloadUrl(store.TokenReissuePath);
                APIResult result = await SendOnceAsync(() =>
                {
                    var www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
                    www.downloadHandler = new DownloadHandlerBuffer();
                    return www;
                }, DefaultTimeoutSec, binary: false);

                TokenReissueResponse parsed = result.ok ? ParseJson<TokenReissueResponse>(result.text) : null;
                if (parsed == null || string.IsNullOrEmpty(parsed.accessToken))
                {
                    Debug.LogError($"[APIService] 토큰 재발급 실패 — 다시 로그인하세요. ({result.statusCode})");
                    AccessToken = null;
                    return false;
                }

                AccessToken = parsed.accessToken;
                return true;
            }
            finally
            {
                reissueTask = null;
            }
        }

        [Serializable]
        private class TokenReissueResponse
        {
            public string accessToken;
            public string message;
        }
        public static Task<APIResult> PostJsonWithRetryAsync(string url, string jsonBody, int timeoutSeconds, CancellationToken cancellationToken = default)
        {
            byte[] body = Encoding.UTF8.GetBytes(jsonBody);
            return SendWithRetryAsync(() =>
            {
                var www = new UnityWebRequest(url, "POST");
                www.uploadHandler = new UploadHandlerRaw(body);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                return www;
            }, timeoutSeconds, false, cancellationToken);
        }
        #endregion
        /// <summary>JSON이 아닌 응답(HTML 에러 페이지 등)이면 예외 대신 null을 반환합니다.</summary>
        private static T ParseJson<T>(string text) where T : class
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string trimmed = text.TrimStart();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("[")) return null;

            try
            {
                return JsonUtility.FromJson<T>(text);
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"[APIService] JSON 파싱 오류: {e.Message}\n응답: {text}");
                return null;
            }
        }
        private static byte[] TryDecodeBufferJson(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 24 || bytes[0] != (byte)'{') return null;

            string head = Encoding.ASCII.GetString(bytes, 0, Math.Min(64, bytes.Length));
            if (!head.Contains("\"type\":\"Buffer\"") || !head.Contains("\"data\"")) return null;

            int start = Array.IndexOf(bytes, (byte)'[');
            if (start < 0) return null;

            int write = 0;
            int value = -1;
            for (int i = start + 1; i < bytes.Length; i++)
            {
                byte ch = bytes[i];
                if (ch >= (byte)'0' && ch <= (byte)'9')
                {
                    value = (value < 0 ? 0 : value * 10) + (ch - (byte)'0');
                }
                else if (ch == (byte)',' || ch == (byte)']')
                {
                    if (value >= 0) bytes[write++] = (byte)value;
                    value = -1;
                    if (ch == (byte)']') break;
                }
            }

            if (write == 0) return null;
            var result = new byte[write];
            Array.Copy(bytes, result, write);
            return result;
        }

        private static string GetAudioExtension(string mimeType)
        {
            return mimeType?.ToLowerInvariant() switch
            {
                "audio/wav" => ".wav",
                "audio/x-wav" => ".wav",
                "audio/mpeg" => ".mp3",
                "audio/ogg" => ".ogg",
                _ => null
            };
        }

        private static AudioType GetAudioType(string extension, string mimeType)
        {
            return extension?.ToLowerInvariant() switch
            {
                ".wav" => AudioType.WAV,
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                _ => mimeType?.ToLowerInvariant() switch
                {
                    "audio/wav" => AudioType.WAV,
                    "audio/x-wav" => AudioType.WAV,
                    "audio/mpeg" => AudioType.MPEG,
                    "audio/ogg" => AudioType.OGGVORBIS,
                    _ => AudioType.UNKNOWN
                }
            };
        }

    }
}
