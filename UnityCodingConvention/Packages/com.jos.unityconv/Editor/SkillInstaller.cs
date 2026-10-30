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
    /// 에디터가 열릴 때 패키지의 Skill~ 을 git 루트의 .claude/skills 로 설치 (내용이 바뀐 경우만), Unity-MCP 설치 확인
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

            string root = FindClaudeRoot();
            string skillsDir = Path.Combine(root, ".claude", "skills");
            string target = Path.Combine(skillsDir, SkillName);
            string stampPath = Path.Combine(target, StampFileName);
            string stamp = $"{package.version}\n{ComputeHash(source)}";

            if (!force && File.Exists(stampPath) && File.ReadAllText(stampPath) == stamp)
            {
                return;
            }

            if (Directory.Exists(target))
            {
                if (File.Exists(stampPath))
                {
                    Directory.Delete(target, true);
                }
                else
                {
                    // 패키지가 설치하지 않은 폴더 (직접 복사한 이전 스킬), skills 밖으로 옮겨 명령어 중복 방지
                    string backup = Path.Combine(root, ".claude", $"{SkillName}.backup-{DateTime.Now:yyyyMMddHHmmss}");
                    Directory.Move(target, backup);
                    Debug.LogWarning($"{LogTag} 기존 {SkillName} 스킬 폴더를 백업으로 옮겼습니다: {backup}");
                }
            }

            CopyDirectory(source, target);
            File.WriteAllText(Path.Combine(target, ".gitignore"), "# com.jos.unityconv 패키지가 설치한 폴더, 직접 수정하지 말고 패키지에서 업데이트\n*\n");
            File.WriteAllText(stampPath, stamp);
            Debug.Log($"{LogTag} /{SkillName} 스킬 설치 (v{package.version}): {target}");

            string legacy = Path.Combine(skillsDir, LegacySkillName);
            if (Directory.Exists(legacy))
            {
                Debug.LogWarning($"{LogTag} 이전 스킬 폴더가 남아 있습니다. /{LegacySkillName} 명령어가 같이 보이니 삭제하세요: {legacy}");
            }
        }

        /// <summary>
        /// Claude Code를 여는 위치, Unity 프로젝트에서 위로 올라가며 git 루트를 찾고 없으면 Unity 프로젝트 루트
        /// </summary>
        private static string FindClaudeRoot()
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
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
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
