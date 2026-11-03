using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityConv.Network
{
    [CreateAssetMenu(fileName = "APIStore", menuName = "UnityConv/Network/APIStore")]
    public class APIStore : ScriptableObject
    {
        [Tooltip("접속할 서버 목록 (예: Dev / Release), API 경로는 모든 서버에 공통")]
        public List<APIServer> servers = new List<APIServer>();
        [Tooltip("기본으로 쓸 서버 (servers의 순서)")]
        public int selectedServer;
        public List<APIUrl> apiUrls;

        // v2.4.0 이하의 ServerUrl 하나짜리 에셋 호환
        [SerializeField, HideInInspector, FormerlySerializedAs("ServerUrl")] private string legacyServerUrl;

        [NonSerialized] private int _runtimeServer = -1;

        /// <summary>APIService가 읽는 Resources 경로 (Resources/APIConfig)</summary>
        public const string ResourceName = "APIConfig";

        /// <summary>
        /// 지금 쓰는 서버 주소 (SelectServer로 바꾼 서버, 없으면 selectedServer)
        /// </summary>
        public string ServerUrl
        {
            get
            {
                if (servers == null || servers.Count == 0)
                    return legacyServerUrl;
                return servers[CurrentServerIndex].Url;
            }
        }

        public string ServerName => servers != null && servers.Count > 0 ? servers[CurrentServerIndex].Name : null;

        private int CurrentServerIndex => Mathf.Clamp(_runtimeServer >= 0 ? _runtimeServer : selectedServer, 0, servers.Count - 1);

        /// <summary>
        /// 실행 중 서버 전환 (에셋 값은 바꾸지 않음), 이름이 없으면 false
        /// </summary>
        public bool SelectServer(string serverName)
        {
            int index = servers != null ? servers.FindIndex(server => server.Name == serverName) : -1;
            if (index < 0)
            {
                Debug.LogError($"[APIStore] 등록되지 않은 서버 이름: {serverName}", this);
                return false;
            }

            _runtimeServer = index;
            return true;
        }

        public string DownloadUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri absoluteUri) &&
                (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
                return absoluteUri.AbsoluteUri;

            StringBuilder sb = new StringBuilder();
            sb.Append(ServerUrl);
            if (!string.IsNullOrEmpty(url) && !url.StartsWith("/"))
                sb.Append('/');
            sb.Append(url);
            return sb.ToString();
        }

        /// <summary>
        /// 등록된 이름의 URL, 없으면 null
        /// </summary>
        public string GetUrl(string name)
        {
            string api = FindApi(name);
            if (api == null)
                return null;

            StringBuilder sb = new StringBuilder();
            sb.Append(ServerUrl);
            sb.Append(api);
            return sb.ToString();
        }

        /// <summary>
        /// 등록된 이름의 URL에 키를 채워 반환 (예: /api/assets/{0}/files.json), 없으면 null
        /// </summary>
        public string GetUrl(string name, object[] format)
        {
            string api = FindApi(name);
            if (api == null)
                return null;

            string form = string.Format(api, format);
            StringBuilder sb = new StringBuilder();
            sb.Append(ServerUrl);
            sb.Append(form);
            return sb.ToString();
        }

        private string FindApi(string name)
        {
            if (apiUrls != null)
            {
                foreach (APIUrl apiUrl in apiUrls)
                {
                    if (apiUrl.Name == name)
                        return apiUrl.Url;
                }
            }

            Debug.LogError($"[APIStore] 등록되지 않은 API 이름: {name}", this);
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 예전 ServerUrl을 서버 목록으로 옮김
            if ((servers == null || servers.Count == 0) && !string.IsNullOrEmpty(legacyServerUrl))
            {
                servers = new List<APIServer> { new APIServer { Name = "Default", Url = legacyServerUrl } };
                legacyServerUrl = null;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    [Serializable]
    public struct APIServer
    {
        public string Name;
        public string Url;
    }

    [Serializable]
    public struct APIUrl
    {
        public string Name;
        public string Url;
    }

    [Serializable]
    public class APIResponse<T>
    {
        public bool success;
        public string code;
        public string message;
        public T data;
    }

    [Serializable]
    public class APIFileResponse
    {
        public byte[] bytes;
        /// <summary>APIService.ExtensionDetector로 판별한 파일 확장자 (예: ".png"), 등록이 없으면 null</summary>
        public string extension;
    }

    [Serializable]
    public class UploadFileResponse
    {
        public int id;
        public string fileType;
        public string fileOriginalName;
        public string fileUploadName;
        public string fileUploadPath;
        public string fileUrl;
        public string fileMimeType;
        public float fileSize;
        public int sortingNumber;
        public string createdAt;
    }
}
