using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace UnityConv.Editor
{
    /// <summary>
    /// 에디터가 열릴 때 패키지의 Skill~ 을 개인 스킬 폴더(~/.claude/skills)로 설치 (내용이 바뀐 경우만), Unity-MCP 설치 확인
    /// </summary>
    [InitializeOnLoad]
    internal static class SkillInstaller
    {
        private const string LogTag = "[UnityConv]";
        private const string SkillName = "code-conv";
        private const string LegacySkillName = "df-clean-code";
        private const string StampFileName = ".installed";

        private const string McpPackageName = "com.coplaydev.unity-mcp";
        private const string McpPackageUrl = "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v9.7.3";
        private const string McpCheckedKey = "UnityConv.McpChecked"; // 에디터 세션당 한 번만 확인

        private static AddRequest _mcpRequest;

        private static string McpNeverAskKey => $"UnityConv.McpNeverAsk.{Application.dataPath}";

        static SkillInstaller()
        {
            EditorApplication.delayCall += Run;
        }

        [MenuItem("Tools/UnityConv/Reinstall Skill")]
        private static void ReinstallSkill()
        {
            InstallSkill(true);
        }

        [MenuItem("Tools/UnityConv/Open Skill Folder")]
        private static void OpenSkillFolder()
        {
            string target = Path.Combine(GetClaudeConfigDir(), "skills", SkillName);
            if (!Directory.Exists(target))
            {
                EditorUtility.DisplayDialog("UnityConv", $"스킬이 아직 설치되지 않았습니다.\n{target}\n\nTools > UnityConv > Reinstall Skill로 설치하세요.", "확인");
                return;
            }
            EditorUtility.RevealInFinder(Path.Combine(target, "SKILL.md"));
        }

        [MenuItem("Tools/UnityConv/Install Unity-MCP")]
        private static void InstallMcpMenu()
        {
            if (IsPackageInstalled(McpPackageName))
            {
                EditorUtility.DisplayDialog("UnityConv", "Unity-MCP가 이미 설치돼 있습니다.", "확인");
                return;
            }
            InstallMcp();
        }

        private static void Run()
        {
            try
            {
                InstallSkill(false);
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogTag} 스킬 설치 실패: {e.Message}");
            }
            CheckMcp();
        }

        #region Skill

        /// <summary>
        /// 개인 스킬 폴더(~/.claude/skills)에 설치, Claude Code를 어느 폴더에서 열어도 보임 (macOS / Windows 공통)
        /// </summary>
        private static void InstallSkill(bool force)
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(SkillInstaller).Assembly);
            if (package == null)
            {
                Debug.LogWarning($"{LogTag} 패키지 정보를 찾지 못해 스킬을 설치하지 않았습니다.");
                return;
            }

            string source = Path.Combine(package.resolvedPath, "Skill~", SkillName);
            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"{LogTag} 스킬 원본이 없습니다: {source}");
                return;
            }

            string claudeDir = GetClaudeConfigDir();
            string skillsDir = Path.Combine(claudeDir, "skills");
            string target = Path.Combine(skillsDir, SkillName);
            string stampPath = Path.Combine(target, StampFileName);
            string stamp = $"{package.version}\n{ComputeHash(source)}";

            RemoveProjectSkill();

            if (File.Exists(stampPath))
            {
                string installed = File.ReadAllText(stampPath);
                if (!force && installed == stamp)
                {
                    return;
                }

                // 개인 폴더는 PC에 하나라 다른 프로젝트가 설치한 더 높은 버전은 낮추지 않음 (메뉴 Reinstall은 강제)
                string installedVersion = installed.Split('\n')[0];
                if (!force && CompareVersion(installedVersion, package.version) > 0)
                {
                    Debug.Log($"{LogTag} 더 높은 버전(v{installedVersion})의 /{SkillName} 스킬이 이미 설치돼 있어 건너뜁니다: {target}");
                    return;
                }
            }

            if (Directory.Exists(target))
            {
                if (File.Exists(stampPath))
                {
                    DeleteDirectory(target);
                }
                else
                {
                    // 패키지가 설치하지 않은 폴더 (직접 만든 스킬), skills 밖으로 옮겨 보관
                    string backup = Path.Combine(claudeDir, $"{SkillName}.backup-{DateTime.Now:yyyyMMddHHmmss}");
                    Directory.Move(target, backup);
                    Debug.LogWarning($"{LogTag} 기존 {SkillName} 스킬 폴더를 백업으로 옮겼습니다: {backup}");
                }
            }

            CopyDirectory(source, target);
            File.WriteAllText(stampPath, stamp);
            Debug.Log($"{LogTag} /{SkillName} 스킬 설치 (v{package.version}): {target}");

            string legacy = Path.Combine(skillsDir, LegacySkillName);
            if (Directory.Exists(legacy))
            {
                Debug.LogWarning($"{LogTag} 이전 스킬 폴더가 남아 있습니다. /{LegacySkillName} 명령어가 같이 보이니 삭제하세요: {legacy}");
            }
        }

        /// <summary>
        /// Claude Code 설정 폴더, CLAUDE_CONFIG_DIR이 있으면 그 경로, 없으면 사용자 폴더의 .claude (Windows: %USERPROFILE%\.claude)
        /// </summary>
        private static string GetClaudeConfigDir()
        {
            string configDir = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
            if (!string.IsNullOrEmpty(configDir))
            {
                return configDir;
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");
        }

        /// <summary>
        /// v2.0 이하가 프로젝트(git 루트)에 설치한 스킬 정리, 패키지가 설치한 것(.installed 있음)만 삭제
        /// </summary>
        private static void RemoveProjectSkill()
        {
            string projectSkill = Path.Combine(FindGitRoot(), ".claude", "skills", SkillName);
            if (!File.Exists(Path.Combine(projectSkill, StampFileName)))
            {
                return;
            }

            DeleteDirectory(projectSkill);
            Debug.Log($"{LogTag} 프로젝트에 설치됐던 이전 방식의 스킬을 정리했습니다 (이제 개인 스킬 폴더에 설치): {projectSkill}");
        }

        /// <summary>
        /// 버전 비교 (a > b 이면 양수), 형식이 다르면 0
        /// </summary>
        private static int CompareVersion(string a, string b)
        {
            if (Version.TryParse(a.Split('-')[0], out Version va) && Version.TryParse(b.Split('-')[0], out Version vb))
            {
                return va.CompareTo(vb);
            }
            return 0;
        }

        /// <summary>
        /// 읽기 전용 속성을 풀고 삭제 (Windows에서 패키지 캐시 파일을 복사하면 읽기 전용이 될 수 있음)
        /// </summary>
        private static void DeleteDirectory(string path)
        {
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(path, true);
        }

        /// <summary>
        /// Unity 프로젝트에서 위로 올라가며 찾은 git 루트, 없으면 Unity 프로젝트 루트
        /// </summary>
        private static string FindGitRoot()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            DirectoryInfo dir = new DirectoryInfo(projectRoot);
            while (dir != null)
            {
                string git = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(git) || File.Exists(git))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return projectRoot;
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                if (Path.GetFileName(file) == ".DS_Store")
                {
                    continue;
                }
                string copied = Path.Combine(target, Path.GetFileName(file));
                File.Copy(file, copied, true);
                File.SetAttributes(copied, FileAttributes.Normal);
            }
            foreach (string dir in Directory.GetDirectories(source))
            {
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
            }
        }

        /// <summary>
        /// 스킬 원본 내용 해시 (같은 버전에서 원본을 고쳐도 다시 설치)
        /// </summary>
        private static string ComputeHash(string source)
        {
            string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories)
                .Where(file => Path.GetFileName(file) != ".DS_Store")
                .OrderBy(file => file, StringComparer.Ordinal)
                .ToArray();

            using (SHA256 sha = SHA256.Create())
            {
                foreach (string file in files)
                {
                    byte[] path = Encoding.UTF8.GetBytes(file.Substring(source.Length).Replace('\\', '/'));
                    sha.TransformBlock(path, 0, path.Length, null, 0);
                    byte[] content = File.ReadAllBytes(file);
                    sha.TransformBlock(content, 0, content.Length, null, 0);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", string.Empty);
            }
        }

        #endregion

        #region Unity-MCP

        private static void CheckMcp()
        {
            if (Application.isBatchMode || SessionState.GetBool(McpCheckedKey, false))
            {
                return;
            }
            SessionState.SetBool(McpCheckedKey, true);

            if (EditorPrefs.GetBool(McpNeverAskKey, false) || IsPackageInstalled(McpPackageName))
            {
                return;
            }

            int choice = EditorUtility.DisplayDialogComplex(
                "UnityConv",
                "Unity-MCP(com.coplaydev.unity-mcp)가 설치돼 있지 않습니다.\nClaude Code가 Unity 에디터를 다룰 수 있도록 같이 설치할까요?\n\n나중에 Tools > UnityConv > Install Unity-MCP 로도 설치할 수 있습니다.",
                "설치",
                "다시 묻지 않음",
                "나중에");

            if (choice == 0)
            {
                InstallMcp();
            }
            else if (choice == 1)
            {
                EditorPrefs.SetBool(McpNeverAskKey, true);
            }
        }

        private static void InstallMcp()
        {
            if (_mcpRequest != null)
            {
                return;
            }

            Debug.Log($"{LogTag} Unity-MCP 설치 중: {McpPackageUrl}");
            _mcpRequest = Client.Add(McpPackageUrl);
            EditorApplication.update += WaitMcpInstall;
        }

        private static void WaitMcpInstall()
        {
            if (!_mcpRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= WaitMcpInstall;
            if (_mcpRequest.Status == StatusCode.Success)
            {
                Debug.Log($"{LogTag} Unity-MCP 설치 완료 ({_mcpRequest.Result.version})");
            }
            else
            {
                Debug.LogError($"{LogTag} Unity-MCP 설치 실패: {_mcpRequest.Error?.message}");
            }
            _mcpRequest = null;
        }

        private static bool IsPackageInstalled(string packageName)
        {
            return PackageInfo.GetAllRegisteredPackages().Any(package => package.name == packageName);
        }

        #endregion
    }
}
