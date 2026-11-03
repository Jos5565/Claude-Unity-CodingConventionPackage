# UnityConv (`com.jos.unityconv`)

Manager / UI / Data / Service 구조의 공용 base 코드, Network, UI 컴포넌트와 Claude Code 스킬(`/code-conv`)을 함께 배포하는 패키지입니다.

- `UnityConv.Bases`: `ManagerBase`, `UIBase<TData>`, `PopupService`
- `UnityConv.Network`: `APIService`(static, `Resources/APIConfig`를 읽음), `APIStore`, `APIResponse<T>`
- `UnityConv.DI`: `SceneInstaller`, `SceneBinder`, `[Inject]` (씬 단위, Manager에 DataSource 주입)
- `UnityConv.UI`: `ButtonToggle`, `RoundImage`
- Unity를 열면 스킬이 개인 스킬 폴더(macOS `~/.claude/skills/code-conv/`, Windows `%USERPROFILE%\.claude\skills\code-conv\`)에 자동으로 설치됩니다. Claude Code를 어느 폴더에서 열어도 `/code-conv`가 보입니다. Unity-MCP 설치 여부도 묻습니다.
- 업데이트는 **Tools > UnityConv > Check for Updates** (Package Manager의 Update 버튼은 git 패키지에서 새 버전을 받지 않음).

설치와 사용법: https://github.com/Jos5565/Claude-Unity-CodingConventionPackage
서드파티 라이선스: `Third Party Notices.md`
