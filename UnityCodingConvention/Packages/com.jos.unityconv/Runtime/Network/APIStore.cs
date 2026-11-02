using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityConv.Network
{
    [CreateAssetMenu(fileName = "APIStore", menuName = "UnityConv/Network/APIStore")]
    public class APIStore : ScriptableObject
    {
        public string ServerUrl;
        public List<APIUrl> apiUrls;

        public const string ResourceName = "APIConfig";

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
