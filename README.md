# Unity Code Convention (`com.jos.code-conv`)

Unity 프로젝트에서 **Manager / UI / Data / Service** 구조로 화면 코드를 만들기 위한 패키지입니다.
설치 한 번으로 아래 세 가지가 같이 들어옵니다.

| 구성 | 내용 |
|---|---|
| **Base 코드** | `ManagerBase`, `UIBase<TData>`, `PopupService` (`UnityConv.Bases`) |
| **Network 코드** | `APIService`, `APIStore`, `APIResponse<T>`, `NetworkManager` (`UnityConv.Network`) |
| **Claude Code 스킬** | `/code-conv` 명령어. 이 구조로 코드를 새로 만들거나 정리할 때 사용 |

설치하면 [Unity-MCP](https://github.com/CoplayDev/unity-mcp)를 같이 설치할지도 묻습니다. Claude Code가 Unity 에디터를 직접 다룰 때 필요합니다.

---

## 설치

파일을 직접 내려받을 필요는 없습니다. Unity Package Manager가 아래 git 주소로 GitHub에서 패키지를 받아 옵니다.

```
https://github.com/Jos5565/Claude-Unity-CodingConventionPackage.git?path=/UnityCodingConvention/Packages/com.jos.code-conv#v1.0.0
```

### 준비
- **git 설치**: Unity가 git 명령으로 패키지를 받습니다. 터미널에서 `git --version`이 실행되는지 확인하세요. git을 새로 설치했다면 Unity Hub와 에디터를 다시 시작해야 합니다.
- **Unity 2022.3 이상**

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
    "com.jos.code-conv": "https://github.com/Jos5565/Claude-Unity-CodingConventionPackage.git?path=/UnityCodingConvention/Packages/com.jos.code-conv#v1.0.0",
    ...
  }
}
```

방법 A로 설치해도 `manifest.json`에 같은 줄이 들어갑니다. 이 파일을 프로젝트 git에 커밋하면, 다른 팀원은 **pull만 받고 Unity를 열면 자동으로 설치**됩니다.

### 2. Unity 열기
처음 열면 자동으로 두 가지가 진행됩니다.

1. **스킬 설치**
   - 패키지 안의 스킬이 프로젝트 git 루트의 `.claude/skills/code-conv/`에 복사됩니다. git 루트가 없으면 Unity 프로젝트 루트에 복사됩니다.
   - Console에 `[UnityConv] /code-conv 스킬 설치` 로그가 찍힙니다.
2. **Unity-MCP 설치 확인**
   - 설치돼 있지 않으면 대화상자가 뜹니다. **설치**, **다시 묻지 않음**, **나중에** 중 하나를 고릅니다.
   - 나중에 설치하려면 **Tools > UnityConv > Install Unity-MCP**를 씁니다.

### 3. Claude Code에서 사용
git 루트에서 Claude Code를 **새로** 엽니다. 스킬 목록은 세션을 시작할 때 읽기 때문입니다.

```
/code-conv 프로젝트 설정 만들기
/code-conv 로그인 화면 정리
/code-conv 새 상세 팝업 개발
```

처음에는 `/code-conv 프로젝트 설정 만들기`를 실행합니다. 프로젝트를 조사해서 `CLAUDE.md`에 넣을 설정 초안(씬, 폴더, 수정 금지 폴더 등)을 보여 주고, 확인을 받은 뒤 추가합니다.

---

## 업데이트

주소 끝의 `#v1.0.0`은 버전 태그입니다. 태그로 고정해야 팀원 모두가 같은 버전을 받습니다.

1. [Releases / Tags](https://github.com/Jos5565/Claude-Unity-CodingConventionPackage/tags)에서 새 버전을 확인합니다.
2. `manifest.json`의 주소 끝을 바꿉니다. 예: `#v1.0.0` → `#v1.1.0`
3. Unity로 돌아오면 새 버전을 받고, 스킬도 새 버전으로 다시 설치됩니다.

> 태그 없이 `#main`을 쓰면 받을 때마다 최신 코드가 들어옵니다. 팀원마다 버전이 달라질 수 있어 권장하지 않습니다.

## 문제 해결

| 증상 | 확인할 것 |
|---|---|
| `No 'git' executable was found` | git이 설치돼 있는지 확인하고, 설치한 뒤 Unity Hub와 에디터를 다시 시작합니다 |
| `Unable to find package` / 404 | 주소의 `?path=`와 `#태그`가 정확한지 확인합니다 |
| `/code-conv`가 안 보임 | Console에 `[UnityConv]` 설치 로그가 있는지 봅니다. Claude Code를 **git 루트에서 새로** 열었는지 확인합니다. 그래도 안 되면 **Tools > UnityConv > Reinstall Skill**을 실행합니다 |
| `UnityConv` 네임스페이스를 못 찾음 | 코드가 자체 `.asmdef` 안에 있다면 References에 `UnityConv`를 추가합니다 |

---

## 스킬 파일 관리

- `.claude/skills/code-conv/`는 **패키지가 만드는 폴더**입니다. 직접 수정하지 마세요.
  - 패키지 버전이나 내용이 바뀌면 통째로 다시 설치됩니다.
  - 프로젝트마다 다른 내용은 프로젝트 `CLAUDE.md`의 `code-conv 프로젝트 설정` 섹션에 둡니다.
- 폴더 안에 `.gitignore`(`*`)가 같이 생겨서 git에 올라가지 않습니다. 팀원은 각자 Unity를 열 때 설치됩니다.
- 다시 설치하려면 **Tools > UnityConv > Reinstall Skill**을 씁니다.
- 직접 복사해 둔 예전 `code-conv` 폴더가 있으면 `.claude/code-conv.backup-날짜/`로 옮겨집니다. 이전 이름인 `df-clean-code` 폴더가 남아 있으면 Console에 삭제 안내가 나옵니다.

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
- 액세스 토큰이 만료되면(401/403) `APIStore.TokenReissuePath`로 재발급을 요청한 뒤 한 번 재시도합니다. 경로를 비우면 재발급하지 않습니다.
- 파일 응답의 확장자를 판별하려면 `APIService.ExtensionDetector`에 판별 함수를 등록합니다. 판별 함수의 예시는 샘플에 있습니다.

### 샘플
**Package Manager > Unity Code Convention > Samples > Network Sample > Import**

| 파일 | 내용 |
|---|---|
| `Network Sample.unity`, `NetworkManager.prefab` | NetworkManager 배치 예시 |
| `APIStore.asset` | 샘플 서버 주소와 API 목록 |
| `FileExtensionDecoder.cs` | 파일 확장자 판별 예시. 임포트하면 `APIService.ExtensionDetector`에 자동 등록됩니다 |

샘플 씬의 라이트는 URP 컴포넌트를 씁니다. URP가 아닌 프로젝트에서는 Missing Script 경고가 뜰 수 있습니다.

---

## 주의

- **asmdef**: 패키지 어셈블리 이름은 `UnityConv`(런타임)입니다. 기본 프로젝트 코드(Assembly-CSharp)는 자동으로 참조합니다. 프로젝트 코드가 자체 `.asmdef`를 쓰면 그 asmdef의 References에 `UnityConv`를 추가하세요.
- **이름 충돌**: 프로젝트에 이미 전역 네임스페이스의 `ManagerBase`, `UIBase`가 있어도 컴파일 에러는 나지 않습니다. 다만 섞어 쓰면 헷갈리니 화면 단위로 패키지 클래스로 옮기는 것을 권장합니다.
- **Unity-MCP 버전**: 설치 도구는 `v9.7.3` 태그로 고정해서 설치합니다. 다른 버전을 쓰려면 `manifest.json`에서 직접 바꾸면 됩니다.
- **지원 버전**: Unity 2022.3 이상. 개발과 테스트는 Unity 6000.3에서 했습니다.

---

## 이 저장소 구조 (패키지 개발용)

```
Claude-Unity-CodingConventionPackage/
 ├ README.md
 └ UnityCodingConvention/                     개발, 테스트용 Unity 프로젝트
     └ Packages/com.jos.code-conv/            ← 배포되는 패키지
         ├ package.json
         ├ Runtime/   (UnityConv.asmdef)       Bases/, Network/
         ├ Editor/    (UnityConv.Editor.asmdef) SkillInstaller.cs
         ├ Skill~/code-conv/SKILL.md          스킬 원본 (Unity가 임포트하지 않는 폴더)
         └ Samples~/NetworkSample/            샘플 (Package Manager에서 Import)
```

### 릴리스 방법
1. `package.json`의 `version`을 올립니다.
2. Unity로 열어 새 파일의 `.meta`가 생겼는지 확인합니다. git URL로 설치된 패키지는 `.meta`가 없는 파일을 무시합니다.
3. 커밋하고 `vX.Y.Z` 태그를 붙여 푸시합니다.
