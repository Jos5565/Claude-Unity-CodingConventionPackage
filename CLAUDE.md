# 이 저장소

`com.jos.unityconv` Unity 패키지를 개발하는 저장소다 (v1 이름은 `com.jos.code-conv`). 배포 대상은 `UnityCodingConvention/Packages/com.jos.unityconv/` 하나이고, 나머지 Unity 프로젝트는 테스트용이다.

- 스킬 원본은 `Packages/com.jos.unityconv/Skill~/code-conv/SKILL.md`. 개인 스킬 폴더(`~/.claude/skills/code-conv/`, Windows `%USERPROFILE%\.claude\skills\code-conv\`)는 SkillInstaller가 만든 복사본이라 수정하지 않는다 (v2.1.0부터 프로젝트가 아닌 개인 폴더에 설치).
- SKILL.md의 "패키지 코드는 수정하지 않는다" 규칙은 패키지를 쓰는 프로젝트용이다. 이 저장소에서는 패키지 코드를 고친다.
- 새 파일을 만들면 Unity로 열어 `.meta`를 생성한 뒤 커밋한다 (git 설치 패키지는 `.meta` 없는 파일을 무시).
- 다른 프로젝트 파일을 가져올 때 `.meta`를 복사하지 않는다 (GUID가 겹치면 설치 프로젝트에서 패키지 파일이 무시됨). 이미 들어왔으면 GUID를 새로 만들고 샘플, asmdef 참조도 같이 바꾼다.
- 런타임 코드는 `UnityConv.Bases` / `UnityConv.Network` / `UnityConv.DI` / `UnityConv.UI` 네임스페이스, 스킬 3절 코드 컨벤션을 따른다. 에디터 코드는 `UnityConv.UI.EditorTools`처럼 `Editor`가 아닌 이름을 쓴다 (`UnityEditor.Editor`와 충돌).
- `Runtime/ThirdParty/UiRoundedCorners`는 서드파티(MIT) 원본이라 고치지 않는다.
- 파일 삭제, 이동은 Unity가 켜져 있으면 에셋 DB에 남을 수 있다. MCP로 확인하거나 Unity를 재시작한다.
