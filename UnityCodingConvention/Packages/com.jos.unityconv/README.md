# UnityConv (`com.jos.unityconv`)

Manager / UI / Data / Service 구조의 공용 base 코드, Network, UI 컴포넌트와 Claude Code 스킬(`/code-conv`)을 함께 배포하는 패키지입니다.

- `UnityConv.Bases`: `ManagerBase`, `UIBase<TData>`, `PopupService`
- `UnityConv.Network`: `APIService`, `APIStore`, `APIResponse<T>`, `NetworkManager`
- `UnityConv.UI`: `ButtonToggle`, `RoundImage`
- Unity를 열면 스킬이 git 루트의 `.claude/skills/code-conv/`에 자동으로 설치됩니다. Unity-MCP 설치 여부도 묻습니다.

설치와 사용법: https://github.com/Jos5565/Claude-Unity-CodingConventionPackage
서드파티 라이선스: `Third Party Notices.md`
