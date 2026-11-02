# UnityConv (`com.jos.unityconv`)

Unity 프로젝트에서 **Manager / UI / Data / Service** 구조로 화면 코드를 만들기 위한 패키지입니다.
설치 한 번으로 아래 네 가지가 같이 들어옵니다.

| 구성 | 내용 |
|---|---|
| **Base 코드** | `ManagerBase`, `UIBase<TData>`, `PopupService` (`UnityConv.Bases`) |
| **Network 코드** | `APIService`, `APIStore`, `APIResponse<T>`, `NetworkManager` (`UnityConv.Network`) |
| **UI 컴포넌트** | `ButtonToggle`(버튼 + 토글), `RoundImage`(둥근 모서리 이미지) (`UnityConv.UI`) |
| **Claude Code 스킬** | `/code-conv` 명령어. 이 구조로 코드를 새로 만들거나 정리할 때 사용 |

설치하면 [Unity-MCP](https://github.com/CoplayDev/unity-mcp)를 같이 설치할지도 묻습니다. Claude Code가 Unity 에디터를 직접 다룰 때 필요합니다.

> **v1(`com.jos.code-conv`)이나 `com.jos.uguitemplate`를 쓰고 있다면** 아래 [v1에서 옮기기](#v1에서-옮기기)를 먼저 보세요.

---

## 설치

파일을 직접 내려받을 필요는 없습니다. Unity Package Manager가 아래 git 주소로 GitHub에서 패키지를 받아 옵니다.

```
https://github.com/Jos5565/Claude-Unity-CodingConventionPackage.git?path=/UnityCodingConvention/Packages/com.jos.unityconv#v2.2.0
```

### 준비
- **git 설치**: Unity가 git 명령으로 패키지를 받습니다. 터미널에서 `git --version`이 실행되는지 확인하세요. git을 새로 설치했다면 Unity Hub와 에디터를 다시 시작해야 합니다.
- **Unity 6 (6000.0) 이상**: UI 컴포넌트가 `com.unity.ugui` 2.0.0을 씁니다. 이 의존성은 자동으로 설치됩니다.
- **macOS, Windows 모두 지원**합니다. Windows는 [Git for Windows](https://git-scm.com/download/win)를 설치하세요.

### 1. 패키지 추가 (둘 중 하나)

**방법 A. Package Manager 화면에서 (가장 쉬움)**
1. Unity에서 **Window > Package Manager**를 엽니다.
2. 왼쪽 위 **+ > Install package from git URL...** 을 고릅니다.
3. 위 주소를 붙여 넣고 **Install**을 누릅니다.

**방법 B. `Packages/manifest.json`에 직접 추가 (팀 프로젝트에 권장)**
`dependencies`에 한 줄을 추가합니다.

```json
{
  "dependencies": {
    "com.jos.unityconv": "https://github.com/Jos5565/Claude-Unity-CodingConventionPackage.git?path=/UnityCodingConvention/Packages/com.jos.unityconv#v2.2.0",
    ...
  }
}
```

방법 A로 설치해도 `manifest.json`에 같은 줄이 들어갑니다. 이 파일을 프로젝트 git에 커밋하면, 다른 팀원은 **pull만 받고 Unity를 열면 자동으로 설치**됩니다.

### 2. Unity 열기
처음 열면 자동으로 두 가지가 진행됩니다.

1. **스킬 설치**
   - 패키지 안의 스킬이 **개인 스킬 폴더**에 복사됩니다. 이 PC에서 여는 모든 Claude Code 세션에서 보입니다.

     | OS | 설치 위치 |
     |---|---|
     | macOS | `~/.claude/skills/code-conv/` |
     | Windows | `%USERPROFILE%\.claude\skills\code-conv\` (예: `C:\Users\<이름>\.claude\skills\code-conv\`) |

   - `CLAUDE_CONFIG_DIR` 환경 변수를 쓰고 있으면 그 폴더의 `skills/code-conv/`에 설치됩니다. Unity Hub를 실행할 때 이 변수가 잡혀 있어야 합니다(시스템 환경 변수로 설정).
   - Console에 `[UnityConv] /code-conv 스킬 설치` 로그가 찍힙니다. 설치된 폴더는 **Tools > UnityConv > Open Skill Folder**로 열 수 있습니다.
2. **Unity-MCP 설치 확인**
   - 설치돼 있지 않으면 대화상자가 뜹니다. **설치**, **다시 묻지 않음**, **나중에** 중 하나를 고릅니다.
   - 나중에 설치하려면 **Tools > UnityConv > Install Unity-MCP**를 씁니다.

### 3. Claude Code에서 사용
Claude Code를 엽니다. 개인 스킬이라 **어느 폴더에서 열어도** `/code-conv`가 보입니다. 이미 열려 있던 세션에도 바로 반영됩니다.

> 작업할 Unity 프로젝트 폴더(또는 그 위 git 루트)에서 여는 것을 권장합니다. 스킬이 그 프로젝트의 `CLAUDE.md`(`code-conv 프로젝트 설정`)를 읽기 때문입니다.

```
/code-conv 프로젝트 설정 만들기
/code-conv 로그인 화면 정리
/code-conv 새 상세 팝업 개발
```

처음에는 `/code-conv 프로젝트 설정 만들기`를 실행합니다. 프로젝트를 조사해서 `CLAUDE.md`에 넣을 설정 초안(씬, 폴더, 수정 금지 폴더 등)을 보여 주고, 확인을 받은 뒤 추가합니다.

---

## v1에서 옮기기

v2에서 패키지 이름이 `com.jos.code-conv`에서 `com.jos.unityconv`로 바뀌었습니다. 예전에 따로 배포하던 UI 라이브러리(`com.jos.uguitemplate`)도 이 패키지에 들어왔습니다.

1. `manifest.json`에서 예전 줄을 **지웁니다**.
   - `"com.jos.code-conv": ...`
   - `"com.jos.uguitemplate": ...` (쓰고 있었다면)
2. 위 [설치](#설치)의 새 줄을 추가합니다.
3. `com.jos.uguitemplate`를 쓰던 코드는 `using UGUICUSTOM;`을 `using UnityConv.UI;`로 바꿉니다.
   - 두 패키지를 같이 설치하면 같은 이름의 클래스가 두 개가 되어 컴파일 에러가 납니다. 반드시 예전 패키지를 먼저 지우세요.
   - `ButtonToggle`은 v2에서 이벤트 이름이 `OnClick` → `onClick`, `IsOn` → `onValueChanged`로 바뀌었습니다. 씬과 프리팹에 저장된 연결은 그대로 유지됩니다. 코드에서 직접 부르던 곳만 바꾸면 됩니다.
4. 스킬은 새 패키지가 개인 스킬 폴더에 다시 설치합니다. v2.0 이하가 프로젝트에 설치했던 `.claude/skills/code-conv/`는 자동으로 지워집니다. 따로 할 일은 없습니다.

---

## 업데이트

주소 끝의 `#v2.2.0`은 버전 태그입니다. 태그로 고정해야 팀원 모두가 같은 버전을 받습니다.

1. [Tags](https://github.com/Jos5565/Claude-Unity-CodingConventionPackage/tags)에서 새 버전을 확인합니다.
2. `manifest.json`의 주소 끝을 바꿉니다. 예: `#v2.2.0` → `#v2.3.0`
3. Unity로 돌아오면 새 버전을 받고, 스킬도 새 버전으로 다시 설치됩니다.

> 태그 없이 `#main`을 쓰면 받을 때마다 최신 코드가 들어옵니다. 팀원마다 버전이 달라질 수 있어 권장하지 않습니다.

## 문제 해결

| 증상 | 확인할 것 |
|---|---|
| `No 'git' executable was found` | git이 설치돼 있는지 확인하고, 설치한 뒤 Unity Hub와 에디터를 다시 시작합니다 |
| `Unable to find package` / 404 | 주소의 `?path=`와 `#태그`가 정확한지 확인합니다 |
| `/code-conv`가 안 보임 | 1) Console에 `[UnityConv] /code-conv 스킬 설치` 로그가 있는지, **Tools > UnityConv > Open Skill Folder**로 폴더가 열리는지 확인합니다. 2) 개인 스킬 폴더(`skills`)가 세션 중에 처음 생겼다면 세션 안에서 `/reload-skills`를 실행하거나 새로 엽니다. 3) 그래도 안 되면 **Tools > UnityConv > Reinstall Skill**을 실행합니다 |
| `더 높은 버전 ... 이미 설치돼 있어 건너뜁니다` 로그 | 다른 프로젝트가 더 높은 버전의 스킬을 설치한 상태입니다. 개인 폴더는 PC에 하나라 낮은 버전으로 덮어쓰지 않습니다. 이 프로젝트 버전으로 바꾸려면 **Reinstall Skill**을 씁니다 |
| Windows에서 스킬 설치 실패 | Console의 `[UnityConv] 스킬 설치 실패` 메시지를 확인합니다. 백신이나 OneDrive가 `%USERPROFILE%\.claude` 폴더를 잠그고 있지 않은지 확인합니다 |
| `UnityConv` 네임스페이스를 못 찾음 | 코드가 자체 `.asmdef` 안에 있다면 References에 `UnityConv`(Bases, Network)나 `UnityConv.UI`를 추가합니다 |
| 같은 이름의 클래스가 두 개라는 에러 (`ButtonToggle` 등) | `com.jos.uguitemplate`가 아직 설치돼 있습니다. 지우세요 |
| 샘플 플레이 중 `InvalidOperationException ... UnityEngine.Input class` | 아래 [샘플](#샘플)의 입력 설정 안내를 보세요 |

---

## 스킬 파일 관리

- 개인 스킬 폴더의 `code-conv/`는 **패키지가 만드는 폴더**입니다. 직접 수정하지 마세요.
  - 패키지 버전이나 내용이 바뀌면 통째로 다시 설치됩니다. 더 높은 버전이 이미 있으면 덮어쓰지 않습니다.
  - 프로젝트마다 다른 내용은 프로젝트 `CLAUDE.md`의 `code-conv 프로젝트 설정` 섹션에 둡니다.
- 프로젝트 git에는 아무것도 생기지 않습니다. 팀원은 각자 Unity를 열 때 자기 PC에 설치됩니다.
- 다시 설치하려면 **Tools > UnityConv > Reinstall Skill**, 폴더를 열려면 **Tools > UnityConv > Open Skill Folder**를 씁니다.
- 직접 만든 `code-conv` 폴더가 있으면 지우지 않고 `.claude/code-conv.backup-날짜/`로 옮깁니다. 이전 이름인 `df-clean-code` 폴더가 남아 있으면 Console에 삭제 안내가 나옵니다.

---

## 코드 사용 예

```csharp
using UnityConv.Bases;

// 데이터 Manager: 조회 → (취소, 빈 결과면 버림) → UI 초기화
public class RobotManager : ManagerBase
{
    [SerializeField] private RobotDetailPopupUI ui_robotDetail;
    private IRobotDataSource _source;

    public void Initialize(ScreenManager screen, IRobotDataSource source)
    {
        _source = source;
        BindViews(ui_robotDetail);
    }

    public void OpenRobotDetail(string robotName)
    {
        Request(ui_robotDetail, token => _source.GetRobotDetailAsync(robotName, token), detail => ui_robotDetail.Initialize(this, detail));
    }
}

// UI: 받은 데이터로 그리기만, 요청은 자기 Manager에
public class RobotDetailPopupUI : UIBase<RobotDetailData>
{
    private RobotManager Manager => Owner as RobotManager;

    protected override void ResetView() { /* 스크롤, 접힘 초기화 */ }
    public override void SetData(RobotDetailData detail) { /* 그리기 */ }
    public void OnClickClose() => PopupService.Close(this);
}
```

- `ManagerBase.Request`
  - 같은 UI에 새 요청이 오거나, UI가 닫히거나, Manager가 꺼지면 이전 요청을 자동으로 취소합니다.
  - 늦게 온 응답은 버립니다.
- `UIBase.Initialize`
  - 닫혀 있으면 `PopupService.Push`로 열고 `ResetView`를 부른 뒤 `SetData`를 합니다.
  - 이미 열려 있으면 `SetData`만 합니다.
- `PopupService`
  - 씬 오브젝트가 필요 없는 static 전역 팝업 스택입니다. `Push`, `Close`, `Back`, `CloseAll`, `Get<T>()`를 제공합니다.

### Network
- 씬에 `NetworkManager`를 두고 `APIStore` 에셋(서버 주소, API 이름과 경로 목록)을 연결합니다.
- 그다음 `APIService.GetAsync<T>("apiName", keys, token)`처럼 API 이름으로 호출합니다.
- 응답 형식은 `{ success, code, message, data }`(`APIResponse<T>`)를 기준으로 합니다.
- 서버가 보낸 쿠키(Set-Cookie)는 보관했다가 이후 요청에 붙입니다.
- v2.2.0부터 액세스 토큰(`APIService.AccessToken`, Authorization 헤더, 토큰 재발급)은 지원하지 않습니다. 필요하면 프로젝트에서 직접 헤더를 붙이세요.
- 파일 응답의 확장자를 판별하려면 `APIService.ExtensionDetector`에 판별 함수를 등록합니다. 판별 함수의 예시는 샘플에 있습니다.

### UI 컴포넌트
메뉴 **GameObject > UI (Canvas) > Button Toggle / Round Image**로 만듭니다. Canvas가 없으면 같이 만듭니다.

**ButtonToggle**: 하나의 컴포넌트로 버튼과 토글을 모두 씁니다. 인스펙터 위쪽 툴바에서 모드를 고릅니다.

| 모드 | 이벤트 | 설명 |
|---|---|---|
| Button | `onClick` | 일반 버튼 |
| Toggle | `onValueChanged(bool)` | `isOn`, `group`(ToggleGroup)을 씁니다. 같은 그룹에서는 하나만 켜집니다. 그룹의 `Allow Switch Off`를 끄면 마지막 하나는 꺼지지 않습니다 |

```csharp
using UnityConv.UI;

toggle_tab.onValueChanged.AddListener(isOn => { if (isOn && Manager != null) Manager.RequestTab(TabType.Week); });
toggle_tab.SetIsOnWithoutNotify(true);              // 이벤트 없이 값만 변경 (그래픽은 갱신)
ButtonToggle selected = group_tab.GetFirstActiveButtonToggle();
```

- On/Off 모습은 `Off/On Color`와 `Off/On Sprite`로 정합니다. `On Sprite`가 비어 있으면 색만 바뀝니다.
- 컴포넌트를 추가할 때 현재 모습이 Off 상태로 저장됩니다.
- Toggle에서 Button으로 바꾸면 Off 모습으로 돌아가고, 그룹 처리에서 빠집니다.

**RoundImage**: 모서리 반경을 지정할 수 있는 Image입니다. 내부에서 [Unity UI Rounded Corners](https://github.com/kirevdokimov/Unity-UI-Rounded-Corners)(MIT, `Third Party Notices.md` 참고) 셰이더를 씁니다.
- 셰이더를 `Shader.Find`로 불러옵니다. 빌드에서 둥근 모서리가 안 보이면 **Project Settings > Graphics > Always Included Shaders**에 `UI/RoundedCorners/RoundedCorners`를 추가하세요.

### 샘플
**Package Manager > UnityConv > Samples**에서 필요한 샘플만 **Import**합니다.

| 샘플 | 파일 | 내용 |
|---|---|---|
| Network Sample | `Network Sample.unity`, `NetworkManager.prefab` | NetworkManager 배치 예시 |
| | `APIStore.asset` | 샘플 서버 주소와 API 목록 |
| | `FileExtensionDecoder.cs` | 파일 확장자 판별 예시. 임포트하면 `APIService.ExtensionDetector`에 자동 등록됩니다 |
| UGUI Sample | `Sample Scene.unity` | 같은 그룹의 ButtonToggle 4개. 이벤트가 `ButtonToggleSampleLog`에 연결돼 있어 콘솔로 확인할 수 있습니다 |
| | `Round Image Demo.unity`, `Round Image.prefab`, `Button Toggle.prefab` | RoundImage, ButtonToggle 예시 |

- 샘플 씬의 라이트는 URP 컴포넌트를 씁니다. URP가 아닌 프로젝트에서는 Missing Script 경고가 뜰 수 있습니다.
- UGUI Sample의 EventSystem은 **Input System**(`InputSystemUIInputModule`)을 씁니다.
  - Input System 패키지가 없거나 예전 Input만 쓰는 프로젝트에서는, 씬의 EventSystem에서 입력 모듈을 `StandaloneInputModule`로 바꾸세요.
  - 반대로 Input System만 쓰는 프로젝트에서 `StandaloneInputModule`을 쓰면 플레이 중 `InvalidOperationException`이 납니다.

---

## 주의

- **asmdef**: 패키지 어셈블리는 `UnityConv`(Bases, Network)와 `UnityConv.UI`(UI 컴포넌트)입니다. 기본 프로젝트 코드(Assembly-CSharp)는 자동으로 참조합니다. 프로젝트 코드가 자체 `.asmdef`를 쓰면 그 asmdef의 References에 직접 추가하세요.
- **이름 충돌**: 프로젝트에 이미 전역 네임스페이스의 `ManagerBase`, `UIBase`가 있어도 컴파일 에러는 나지 않습니다. 다만 섞어 쓰면 헷갈리니 화면 단위로 패키지 클래스로 옮기는 것을 권장합니다.
- **Unity-MCP 버전**: 설치 도구는 `v9.7.3` 태그로 고정해서 설치합니다. 다른 버전을 쓰려면 `manifest.json`에서 직접 바꾸면 됩니다.
- **지원 버전**: Unity 6 (6000.0) 이상. 개발과 테스트는 Unity 6000.3에서 했습니다.

---

## 이 저장소 구조 (패키지 개발용)

```
Claude-Unity-CodingConventionPackage/
 ├ README.md
 └ UnityCodingConvention/                     개발, 테스트용 Unity 프로젝트
     └ Packages/com.jos.unityconv/            ← 배포되는 패키지
         ├ package.json
         ├ Third Party Notices.md
         ├ Runtime/   UnityConv.asmdef         Bases/, Network/
         │            UI/ (UnityConv.UI.asmdef) ButtonToggle, RoundImage
         │            ThirdParty/UiRoundedCorners/
         ├ Editor/    UnityConv.Editor.asmdef  SkillInstaller.cs
         │            UI/ (UnityConv.UI.Editor.asmdef) 인스펙터, 메뉴
         │            ThirdParty/UiRoundedCorners/
         ├ Skill~/code-conv/SKILL.md          스킬 원본 (Unity가 임포트하지 않는 폴더)
         └ Samples~/NetworkSample/, UGUISample/  샘플 (Package Manager에서 Import)
```

### 릴리스 방법
1. `package.json`의 `version`을 올립니다.
2. Unity로 열어 새 파일의 `.meta`가 생겼는지 확인합니다. git URL로 설치된 패키지는 `.meta`가 없는 파일을 무시합니다.
   - 다른 프로젝트에서 가져온 파일은 `.meta`를 같이 복사하지 마세요. GUID가 원래 프로젝트와 겹치면, 설치한 쪽에서 `GUID ... conflicts with` 오류가 나고 그 파일이 무시됩니다.
3. 커밋하고 `vX.Y.Z` 태그를 붙여 푸시합니다.
