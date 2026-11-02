using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

namespace UnityConv.Network
{
    /// <summary>
    /// 서버 쿠키(Set-Cookie, 예: 세션 쿠키)를 보관하고, 이후 요청에 Cookie 헤더로 붙입니다.
    /// </summary>
    public static class CookieStore
    {
        private static readonly Dictionary<string, string> cookies = new Dictionary<string, string>();

        // "a=1; Path=/, b=2; Expires=Fri, 23 Oct 2026 ..." 처럼 합쳐진 헤더를 쿠키 단위로 분리
        private static readonly Regex CookieSplitter = new Regex(@",(?=\s*[^;,=\s]+=)");

        public static string Get(string name) => cookies.TryGetValue(name, out string value) ? value : null;

        public static void Set(string name, string value)
        {
            if (string.IsNullOrEmpty(value)) cookies.Remove(name);
            else cookies[name] = value;
        }

        public static void Clear() => cookies.Clear();

        /// <summary>보관 중인 쿠키를 요청의 Cookie 헤더로 설정합니다.</summary>
        public static void Apply(UnityWebRequest request)
        {
            if (cookies.Count == 0) return;
            request.SetRequestHeader("Cookie", string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}")));
        }

        /// <summary>응답의 Set-Cookie 헤더를 읽어 보관합니다.</summary>
        public static void Capture(UnityWebRequest request)
        {
            string header = request.GetResponseHeader("Set-Cookie");
            if (string.IsNullOrEmpty(header)) return;

            foreach (string cookie in CookieSplitter.Split(header))
            {
                string pair = cookie.Split(';')[0].Trim();
                int eq = pair.IndexOf('=');
                if (eq <= 0) continue;

                string name = pair.Substring(0, eq).Trim();
                string value = pair.Substring(eq + 1).Trim();
                bool expired = Regex.IsMatch(cookie, @"Max-Age=0\b", RegexOptions.IgnoreCase);
                Set(name, expired ? null : value);
            }
        }
    }
}
