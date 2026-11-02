using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace UnityConv.Editor
{
    /// <summary>
    /// git 태그(vX.Y.Z)로 새 버전을 확인하고, 고르면 그 태그로 다시 설치 (manifest.json은 Unity가 변경)
    /// Package Manager의 Update는 git 패키지에서 같은 태그만 다시 받아 오므로 이 메뉴로 업데이트합니다.
    /// </summary>
    [InitializeOnLoad]
    internal static class PackageUpdater
    {
        private const string LogTag = "[UnityConv]";
        private const string DialogTitle = "UnityConv 업데이트";
        private const int GitTimeoutMs = 15000;

        private const string CheckedKey = "UnityConv.UpdateChecked"; // 에디터 세션당 한 번
        private const string LastCheckKey = "UnityConv.LastUpdateCheck"; // 하루 한 번
        private const string SkipVersionKey = "UnityConv.SkipVersion";

        private static bool _checking;
        private static AddRequest _addRequest;

        static PackageUpdater()
        {
            EditorApplication.delayCall += AutoCheck;
        }

        [MenuItem("Tools/UnityConv/Check for Updates")]
        private static void CheckMenu()
        {
            Check(true);
        }

        private static void AutoCheck()
        {
            if (Application.isBatchMode || SessionState.GetBool(CheckedKey, false))
            {
                return;
            }
            SessionState.SetBool(CheckedKey, true);

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (EditorPrefs.GetString(LastCheckKey, string.Empty) == today)
            {
                return;
            }
            EditorPrefs.SetString(LastCheckKey, today);

            Check(false);
        }

        /// <summary>
        /// manual이면 결과(최신, 실패)를 항상 대화상자로 알림, 자동 확인은 새 버전이 있을 때만 알림
        /// </summary>
        private static async void Check(bool manual)
        {
            if (_checking || _addRequest != null)
            {
                return;
            }

            PackageInfo package = PackageInfo.FindForAssembly(typeof(PackageUpdater).Assembly);
            if (package == null || package.source != PackageSource.Git)
            {
                if (manual)
                {
                    EditorUtility.DisplayDialog(DialogTitle, "git 주소로 설치한 패키지만 업데이트를 확인할 수 있습니다.", "확인");
                }
                return;
            }

            string installUrl = GetInstallUrl(package);
            string repoUrl = installUrl.Split('?', '#')[0];

            _checking = true;
            string latestTag;
            try
            {
                latestTag = await Task.Run(() => FindLatestTag(repoUrl));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{LogTag} 업데이트 확인 실패: {e.Message}");
                if (manual)
                {
                    EditorUtility.DisplayDialog(DialogTitle, $"업데이트를 확인하지 못했습니다.\n{e.Message}", "확인");
                }
                return;
            }
            finally
            {
                _checking = false;
            }

            string latestVersion = latestTag?.Substring(1);
            if (latestVersion == null || !IsNewer(latestVersion, package.version))
            {
                if (manual)
                {
                    EditorUtility.DisplayDialog(DialogTitle, $"최신 버전입니다 (v{package.version}).", "확인");
                }
                return;
            }

            if (!manual && EditorPrefs.GetString(SkipVersionKey, string.Empty) == latestVersion)
            {
                return;
            }

            int choice = EditorUtility.DisplayDialogComplex(
                DialogTitle,
                $"새 버전이 있습니다.\n\nv{package.version} → v{latestVersion}\n\n버전 목록: {repoUrl.Replace(".git", string.Empty)}/tags",
                "업데이트",
                "이 버전 건너뛰기",
                "나중에");

            if (choice == 0)
            {
                Install($"{installUrl.Split('#')[0]}#{latestTag}");
            }
            else if (choice == 1)
            {
                EditorPrefs.SetString(SkipVersionKey, latestVersion);
            }
        }

        /// <summary>
        /// packageId(name@url)에서 설치 주소, 예: https://github.com/.../X.git?path=/Packages/com.jos.unityconv#v2.2.0
        /// </summary>
        private static string GetInstallUrl(PackageInfo package)
        {
            return package.packageId.Substring(package.name.Length + 1);
        }

        /// <summary>
        /// git ls-remote로 가장 높은 vX.Y.Z 태그 (없으면 null), 백그라운드 스레드에서 실행
        /// </summary>
        private static string FindLatestTag(string repoUrl)
        {
            var startInfo = new ProcessStartInfo("git", $"ls-remote --tags --refs \"{repoUrl}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            // 로그인 정보가 없을 때 입력을 기다리며 멈추지 않도록
            startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";

            using (Process process = Process.Start(startInfo))
            {
                Task<string> output = process.StandardOutput.ReadToEndAsync();
                Task<string> error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(GitTimeoutMs))
                {
                    process.Kill();
                    throw new TimeoutException("git ls-remote 응답 시간 초과");
                }
                if (process.ExitCode != 0)
                {
                    throw new Exception(error.Result.Trim());
                }

                string latest = null;
                foreach (string line in output.Result.Split('\n'))
                {
                    int index = line.IndexOf("refs/tags/v", StringComparison.Ordinal);
                    if (index < 0)
                    {
                        continue;
                    }

                    string tag = line.Substring(index + "refs/tags/".Length).Trim();
                    if (Version.TryParse(tag.Substring(1), out _) && (latest == null || IsNewer(tag.Substring(1), latest.Substring(1))))
                    {
                        latest = tag;
                    }
                }
                return latest;
            }
        }

        /// <summary>
        /// a가 b보다 높은 버전이면 true, 형식이 다르면 false
        /// </summary>
        private static bool IsNewer(string a, string b)
        {
            return Version.TryParse(a, out Version va) && Version.TryParse(b.Split('-')[0], out Version vb) && va > vb;
        }

        private static void Install(string url)
        {
            Debug.Log($"{LogTag} 업데이트 설치 중: {url}");
            _addRequest = Client.Add(url);
            EditorApplication.update += WaitInstall;
        }

        private static void WaitInstall()
        {
            if (!_addRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= WaitInstall;
            if (_addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"{LogTag} v{_addRequest.Result.version}로 업데이트했습니다.");
            }
            else
            {
                Debug.LogError($"{LogTag} 업데이트 실패: {_addRequest.Error?.message}");
                EditorUtility.DisplayDialog(DialogTitle, $"업데이트하지 못했습니다.\n{_addRequest.Error?.message}", "확인");
            }
            _addRequest = null;
        }
    }
}
