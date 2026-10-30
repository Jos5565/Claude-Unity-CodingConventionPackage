# 이 저장소

`com.jos.code-conv` Unity 패키지를 개발하는 저장소다. 배포 대상은 `UnityCodingConvention/Packages/com.jos.code-conv/` 하나이고, 나머지 Unity 프로젝트는 테스트용이다.

- 스킬 원본은 `Packages/com.jos.code-conv/Skill~/code-conv/SKILL.md`. 루트의 `.claude/skills/code-conv/`는 SkillInstaller가 만든 복사본이라 수정하지 않는다.
- SKILL.md의 "패키지 코드는 수정하지 않는다" 규칙은 패키지를 쓰는 프로젝트용이다. 이 저장소에서는 패키지 코드를 고친다.
- 새 파일을 만들면 Unity로 열어 `.meta`를 생성한 뒤 커밋한다 (git 설치 패키지는 `.meta` 없는 파일을 무시).
- 다른 프로젝트 파일을 가져올 때 `.meta`를 복사하지 않는다 (GUID가 겹치면 설치 프로젝트에서 패키지 파일이 무시됨). 이미 들어왔으면 GUID를 새로 만들고 샘플 참조도 같이 바꾼다.
- 런타임 코드는 `UnityConv.Bases` / `UnityConv.Network` 네임스페이스, 스킬 3절 코드 컨벤션을 따른다.
