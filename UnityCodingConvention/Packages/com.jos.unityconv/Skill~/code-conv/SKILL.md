---
name: code-conv
description: Deepfine Unity 프로젝트 공용 코드 컨벤션과 설계 규칙. 기능(화면) 코드를 XXXManager / XXXUI / XXXData / XXXService 구조("Manager가 받아서 UI에 전달, UI는 그리기만")로 새로 만들거나 기존 코드를 정리할 때 사용. "OO 화면 소스 정리", "샘플 데이터 분리", "새 팝업/위젯/화면 만들어줘", "이슈 기반 UI 개발", "코드 컨벤션 맞춰줘" 같은 요청에 사용.
argument-hint: (요청 내용, 예: 로그인 화면 적용 / 새 상세 팝업 개발 / 프로젝트 설정 만들기)
---

# code-conv: 공용 코드 컨벤션과 설계 규칙

## 이번 요청

$ARGUMENTS

- 위 요청의 범위(화면, 단계)만 작업한다. 범위가 비어 있거나 모호하면 작업 전에 대상 화면과 단계를 확인한다.
- 요청이 이 문서의 규칙과 충돌하면 진행 전에 충돌 내용을 알리고 확인받는다.
- **세션은 화면(작업 단위) 하나마다 새로 시작한다.** 대화가 길면 호출마다 그 전체를 다시 읽어 토큰이 크게 늘어난다.
  - 이 호출 전에 다른 작업 대화가 길게 쌓여 있으면, 작업 전에 "새 세션(또는 `/clear`)에서 다시 호출하면 토큰을 줄일 수 있다"고 한 줄 알린다. 사용자가 그대로 진행하라면 진행한다.
  - 화면 하나를 마치면 다음 화면은 새 세션에서 하라고 안내한다.

## 0. 시작 전 (프로젝트 설정 확인)

이 문서는 어느 프로젝트에서나 같은 규칙이다. 프로젝트마다 다른 내용은 **프로젝트 CLAUDE.md의 `code-conv 프로젝트 설정` 섹션**에 둔다.

1. CLAUDE.md에서 `code-conv 프로젝트 설정`을 찾는다. 있으면 그 값을 따르고, 이 문서와 다르면 프로젝트 설정이 우선한다.
2. 없으면 작업 전에 프로젝트를 조사해 아래 항목으로 초안을 만들고, 사용자 확인 후 CLAUDE.md에 추가한다.
   - 작업 씬, Manager 루트 오브젝트
   - 폴더: Manager / UI / Service / Controller / Network(DataSource) / Data / Defs(enum) / 샘플 JSON / DI 바인더
   - DI: 씬마다 기본 바인더 (Sample / Api 중 지금 쓰는 것)
   - 팝업 여는 방법 (기본은 패키지의 `PopupService`, 프로젝트 전용 팝업 관리자가 있으면 그 클래스)
   - 서버 통신 클래스와 응답 래퍼 (기본은 패키지의 `APIService`, `APIResponse`), 취소 토큰 지원 여부. `APIService`면 `Resources/APIConfig` 에셋 위치
   - 공용 유틸, 입력, 태그 규칙 (값 없음 표시, 입력 시스템, 상태 Tag 등)
   - 수정 금지 폴더 (서드파티, 다른 팀 소유)
   - 기준 예시 (이미 이 구조로 만든 화면)
   - 이번 정리에서 제외할 것 (다른 씬이 쓰는 타입 등)
3. **Base 코드**: `com.jos.unityconv` 패키지가 제공한다. 프로젝트에 복사하거나 새로 만들지 않는다.
   - 설치 확인: `Packages/manifest.json`의 `com.jos.unityconv`, 없으면 `Packages/com.jos.unityconv/package.json`(Embedded) 또는 `Packages/packages-lock.json`. 모두 없으면 작업 전에 설치를 안내한다. 예전 이름 `com.jos.code-conv`가 있으면 교체를 안내한다.
   - 버전 확인: 이 스킬은 패키지 **v2.9.0** 기준이다. 프로젝트 패키지 버전(`package.json`의 version, git 주소의 `#vX.Y.Z`)이 더 낮으면 사용자에게 알리고, 아래 Base API 요약 대신 그 패키지의 `Skill~/code-conv/SKILL.md`(Embedded 폴더 또는 `Library/PackageCache/com.jos.unityconv*/`)를 기준으로 삼는다.
   - Bases: `using UnityConv.Bases;` (`ManagerBase`, `UIBase`, `UIBase<TData>`, `PopupService`)
   - Network: `using UnityConv.Network;` (`APIService`, `APIStore`, `APIResponse<T>`). `APIService`는 static이고 처음 쓸 때 `Resources/APIConfig`(APIStore 에셋)를 읽는다. 서버(Dev / Release 등)는 APIStore의 Servers 목록과 Selected Server로 고르고, 코드에서 서버 주소를 분기하지 않는다. 씬에 오브젝트를 두지 않는다 (예전 `NetworkManager`가 씬에 있으면 APIStore를 `Resources/APIConfig`로 옮기고 지우도록 안내).
   - DI: `using UnityConv.DI;` (`[Inject]`, `SceneInstaller`, `SceneBinder`)
   - UI: `using UnityConv.UI;` (`ButtonToggle`, `RoundImage`, ToggleGroup 확장)
   - 팝업은 `UIBase.Show()` 기본값으로 `PopupService.Push(this)`가 연다. 닫기는 `PopupService.Close(this)`, 뒤로 가기는 `PopupService.Back()`. `PopupService`는 static 전역이라 씬에 오브젝트를 두지 않는다.
   - **팝업 스택 정책**: 새 팝업을 Push하면 이전 팝업은 닫힘이 아니라 **가려짐**이다.
     - 가려진 팝업은 진행 중 요청과 화면 상태(스크롤, 필터, 선택)를 유지한다. 다시 위로 오면 재조회, 리셋 없이 그대로 보인다 (`OnClosed` / `OnOpened`는 호출됨).
     - Close / Back / CloseAll로 스택에서 빠질 때만 닫힘이다 (요청 취소, 다음 Initialize 때 리셋).
     - 가려진 팝업에 Initialize하면 앞으로 꺼내지 않고 데이터만 적용한다. 돌아올 때 새 데이터가 필요하면 Manager가 명시적으로 다시 조회한다.
   - 프로젝트에 이미 같은 이름의 base 클래스(전역 네임스페이스)가 있으면 섞어 쓰지 않는다. 화면 단위로 패키지 클래스로 옮길지 사용자에게 확인한다.
   - 패키지 코드(`Packages/com.jos.unityconv`, `Library/PackageCache`)는 수정하지 않는다. 기능이 부족하면 사용자에게 알린다.
   - **패키지 소스는 읽지 않는다.** 사용법은 2절의 "Base API 요약"으로 충분하다. 요약에 없는 동작이 꼭 필요할 때만 해당 메서드 부분만 읽는다.
4. Unity MCP를 쓰면 프로젝트 CLAUDE.md의 인스턴스 고정 규칙을 먼저 따른다.
5. 작업 전에 `git status --short`를 기록한다. 결과 보고 때 사용자의 기존 변경과 이번 변경을 구분한다.

## 1. 설계 개요

MVP의 Passive View에 가까운 구조. View는 그리기만, Manager가 데이터를 받아 View에 넣는다.

```
Network                       Manager 계층                         UI 계층
IXxxDataSource                화면 Manager (초기화, 화면 공유 기능)
 ├ SampleSource ─[Inject]──▶  └ 데이터 Manager ──Initialize(this, data)──▶ XXXUI (SetData로 그림)
 └ ApiSource   (SceneBinder)    ◀──────── 요청 (조건 변경, 다른 팝업 열기, 닫힘 알림) ─┘
          ▲
XXXService (static, DI 등록 안 함, 2개 이상 Manager가 참조)
```

허용 관계 (그 밖은 금지):
- UI → 자기 Manager: 사용자 요청 (조건 변경, 다른 팝업 열기, 닫힘 알림은 UIBase가 처리).
- Manager → 연결 UI: `[SerializeField]`로 참조하고 `Initialize(this, data)`로 데이터 전달.
- Manager → DataSource / Service: 조회, 공용 기능. DataSource, Service는 Manager, UI를 모른다.
- Manager → Controller: 화면 상태에 따른 제어를 public 메서드로 호출. Controller는 Manager, UI를 모르고, 알릴 일(클릭 등)은 이벤트로 연다.
- UI는 DataSource, 업무 Service를 직접 부르지 않는다. 예외: UI 인프라 Service(`PopupService.Close(this)` / `Back()`)는 UI가 직접 쓴다.
- 데이터 Manager끼리는 직접 참조하지 않고 화면 Manager를 거친다.

## 2. 역할 규칙

### XXXData (서버 데이터)
- 서버 응답과 같은 형태, snake_case 필드, 데이터 하나당 클래스 하나. 프로젝트 설정의 Data 폴더에 둔다.
- 이름: 목록 `XxxListData` (안에 `XxxData` 목록 + 부가 정보), 항목 `XxxData`. 목록 항목과 상세가 다르면 `XxxDetailData`, 집계는 `XxxSummaryData`.
- 응답 `data`가 배열이면 `XxxData[]`로 받고 ListData는 만들지 않는다 (`APIResponse<XxxData[]>`는 JsonUtility로 읽힌다).
  - 래퍼 없이 맨 바깥이 배열인 응답은 JsonUtility가 읽지 못한다. ApiSource에서 `{"items":` + 응답 + `}`로 감싸 내부 래퍼 클래스로 파싱한 뒤 배열을 반환한다.
  - 샘플 JSON은 서버와 같은 형식으로 만든다.
- 계산, 판단은 Data 안의 `GetXxx()` 메서드 (예: `GetOeeRate()`, `GetStatusTag()`). 이름에 무엇을 얻는지 쓴다 (`GetData` 금지). 기존 get 접근자 속성은 유지해도 된다.
- Color, 인스펙터 문구 등 UI 요소는 넣지 않는다. enum은 Defs에 둔다.

### XXXUI (View)
- `UIBase<TData>` 상속. 받은 Data로 그리기만 한다. DataSource, 업무 Service를 호출하지 않는다 (PopupService는 예외, 1절).
- `Initialize(manager, data)`
  - 닫혀 있으면: 열기(`Show()`) → 화면 상태 리셋(`ResetView`, 스크롤, 필터 등) → SetData
  - 열려 있으면: SetData만 (상태 유지)
  - "처음"은 앱 최초가 아니라 **닫혀 있는 상태** 기준. 열림 상태는 UIBase가 직접 보관.
  - 위젯(항상 표시 카드)은 `protected override bool IsPopup => false;` (리셋 없이 SetData만).
- **UI는 자신을 Initialize한 Manager만 안다** (`private XxxManager Manager => Owner as XxxManager;`). 조건 변경(탭, 월, 하위 선택)이나 다른 팝업 열기는 그 Manager에 요청한다. 선택한 조건은 UI가 들고 요청할 때 넘긴다.
- Awake/Start에서 데이터 관련 초기화 금지. 표시 문구, 포맷(퍼센트, string.Format)은 UI 인스펙터 string으로.
- 자식은 OnEnable/OnDisable을 직접 만들지 않고 `OnOpened()` / `OnClosed()` 오버라이드에서 리스너 구독, 해제 (닫힘 시 Manager 알림은 UIBase가 처리).
- `PopupService` 스택 없이 켜는 팝업(알림 아래 모달 등)은 `Show()`만 오버라이드한다 (같은 프레임에 켤 것).
- 열 때 데이터 외 조건(기간, 위치, 진입 경로 등)이 필요하면 `SetRange` / `SetBay` 같은 설정 메서드를 두고 Manager가 **Initialize 전에** 호출한다.
- 카드 탭처럼 UI가 고른 조건으로 Manager가 다시 조회해야 하면 UI가 `SelectedRange` 같은 읽기 속성을 노출한다.
- 팝업 안 조건 변경 결과는 Data(서버 응답 형태)를 SetData로 받고, 조건 표시는 UI가 들고 있는 선택 조건으로 갱신한다. 화면용 Data를 따로 만들지 않는다.

### XXXManager
- `ManagerBase` 상속. **MonoSingleton 사용 금지.**
- **기존 MonoSingleton Manager(`XxxManager.Instance`)는 DI로 정리한다.**
  - 같은 씬에만 있으면: 그 Manager를 `SceneInstaller`의 `m_SceneObjects`에 등록하고, 쓰는 Manager는 `Instance` 대신 `[Inject] private XxxManager xxxManager;`로 받는다. `Instance`를 쓰는 곳이 없어지면 싱글톤 상속, `Instance`, `DontDestroyOnLoad`를 지운다.
  - 이번 범위 밖 코드가 `Instance`를 쓰면 싱글톤은 남기고 범위 안만 `[Inject]`로 바꾼다. 남은 사용처는 결과 보고에 적는다.
  - UI가 `Instance`를 쓰면 자기 Manager를 거치게 바꾼다 (UI는 `[Inject]` 안 씀).
  - 여러 씬에서 쓰는 싱글톤(`DontDestroyOnLoad`)은 씬 단위 DI로 옮길 수 없다. 앱 전체 상태면 static Service로 옮길지 사용자에게 확인하고, 아니면 그대로 두고 보고한다.
- **화면 Manager**: 하위 데이터 Manager를 `[SerializeField]`로 들고 `Awake`에서 순서대로 `Initialize(this)` (Awake 초기화는 화면 Manager만). 화면 공통 상태, 주기 갱신, 하위 Manager 간 화면 이동. DataSource는 만들지 않는다.
- **데이터 Manager**: 화면 Manager의 자식 GameObject 컴포넌트, 이름도 `XxxManager` (Controller로 부르지 않음). 해당 데이터의 조회와 UI 전달. 연결 UI는 `[SerializeField]`로 들고 `BindViews`로 자신을 알린다 (Inspector에서 확인). DataSource는 `[Inject] private IXxxDataSource dataSource;`로 받는다.
- **최초 조회**: 데이터 Manager는 화면 Manager가 부르는 `Initialize(screen)`에서 `BindViews` 다음에, 처음부터 보이는 위젯의 조회를 직접 시작한다. `HandleUIOpened`에 최초 조회를 맡기지 않는다 (UI의 OnEnable이 Manager 초기화보다 먼저 실행되면 호출되지 않음).
- 화면 Manager 없이 Manager 하나뿐인 간단한 화면은 그 Manager가 두 역할을 한다 (Awake에서 `BindViews`, Start에서 최초 조회. DI 샘플이 이 경우).
- 데이터 Manager끼리는 서로 참조하지 않는다. 다른 Manager 소관 UI는 **화면 Manager를 거쳐** 연다 (화면 이동은 순환하기 쉬움).
- 화면 하나에 데이터 Manager가 많으면 공통 부모(`Screen`, 공통 갱신 메서드)를 둔다.
- **Manager 기능이 커지면 나눈다.** Manager에는 데이터 조회와 UI 전달만 남긴다.
  - 새 데이터의 조회와 UI 전달 → 데이터 Manager
  - 오브젝트 동작, 연출, 카메라, 3D 상호작용(씬 오브젝트 필요) → Controller (그 Manager의 자식, 아래 XXXController)
  - 씬 오브젝트 없이 2개 이상 Manager가 쓰는 기능, 상태 → Service (static)
- **DI**: `[Inject]`는 Manager만 쓴다 (UI에 쓰면 SceneInstaller가 주입하지 않고 에러). 주입 대상은 DataSource, 설정 에셋, 기존 싱글톤 Manager(위). Manager끼리 참조는 `[SerializeField]`, Service는 static이라 주입하지 않는다.
- 데이터가 다 와야 UI를 Initialize (여러 개면 `Task.WhenAll`).
- Manager가 Initialize하지 않고 열린 UI(공용 상세 버튼 등)는 UIBase가 `HandleUIOpened`로 알려주고, 필요한 Manager만 오버라이드해 조회.
- Manager가 Initialize하지 않는 그 밖의 진입점(3D 클릭 등)은 Controller가 이벤트로 알리고, 그 Controller를 가진 Manager가 구독한다 (XXXController).
- Action/이벤트로 UI에 뿌리지 않는다. 직접 Initialize.
- Unity 오브젝트 필드에 `?.` 쓰지 않는다 (Inspector 미연결 fake null 통과). `!= null`로 확인.

### ManagerBase / UIBase (공통 부모, 요청과 취소)
- ManagerBase: (UI, 채널)별 요청 토큰 관리. 같은 (UI, 채널)의 새 요청이 오면 이전 요청 취소, UI가 닫히면 그 UI의 모든 채널 취소, Manager가 꺼지면 전체 취소. 가려진 팝업은 취소하지 않는다 (0절).
- 전제: 닫힘 취소는 UI에 Owner가 연결돼 있어야 동작한다. Request 전에 `BindViews`(또는 이전 Initialize)로 연결한다.
- 이 전제 안에서 늦게 온 응답, 조건이 바뀐 뒤의 응답, 닫힌 뒤의 응답은 토큰으로 버린다 (UI별 조건 비교 코드를 만들지 않는다).
- 토큰이 알 수 없는 경우는 Manager가 `CancelRequests(ui)`를 직접 부른다.
  - 아직 열리지 않은 팝업을 조회하는 중에 사용자가 진입을 취소했을 때 (다른 화면으로 이동 등).
  - 조건 변경으로 같은 UI의 다른 채널 결과까지 무효가 될 때.
- 한 UI가 독립된 요청을 여러 개 하면 채널로 구분 (예: 목록 / 하위 창).

**Base API 요약** (`UnityConv.Bases`, `UnityConv.DI`, 이 요약대로 쓰고 소스는 읽지 않는다)
- `ManagerBase : MonoBehaviour`
  - `protected void Request<T>(UIBase ui, Func<CancellationToken, Task<T>> fetch, Action<T> apply, string channel = null) where T : class`: 같은 (ui, channel)의 이전 요청 취소 → fetch → 취소됐거나 null이면 버림, 아니면 apply. fetch, apply 예외는 로그만 남김.
  - `protected void BindViews(params UIBase[] views)`: UI에 이 Manager를 연결 (null이면 경고).
  - `public virtual void HandleUIOpened(UIBase ui)`: Manager가 Initialize하지 않았는데 연결된 UI가 열렸을 때 호출됨.
  - `public void CancelRequests(UIBase ui)`: 그 UI의 모든 채널 취소. UI가 닫힐 때 UIBase가 호출하고, 위 두 경우에는 Manager가 직접 호출.
  - `OnDisable`, `OnDestroy`는 virtual이고 전체 요청을 취소한다. 오버라이드하면 `base.OnDisable()` / `base.OnDestroy()`를 호출한다.
- `UIBase : MonoBehaviour`
  - `protected ManagerBase Owner`, `public bool IsShown`, `public bool IsCovered` (위 팝업에 가려짐)
  - `protected virtual bool IsPopup => true` (위젯은 false), `protected virtual void Show()` (기본 `PopupService.Push(this)`), `protected virtual void ResetView()`
  - `protected virtual void OnOpened()` / `OnClosed()`: OnEnable / OnDisable 대신 쓴다 (UIBase가 OnEnable/OnDisable을 private으로 씀, 자식이 만들면 안 됨).
- `UIBase<TData> : UIBase` (TData는 class)
  - `public void Initialize(ManagerBase owner, TData data)`: 닫혀 있으면 Show + (닫힌 뒤 처음이면) ResetView, 가려져 있으면 꺼내지 않음, 그다음 SetData.
  - `public abstract void SetData(TData data)`
- `PopupService` (static): `Push(Component)` (이전 팝업은 가려짐), `Close(Component)`, `Back()`, `CloseAll()`, `IsCovered(GameObject)`, `Get<T>()` (비활성 포함 씬 검색), `GetPrevious(Component)`, `LogEnabled`
- DI (`UnityConv.DI`)
  - `[Inject]` 필드: SceneInstaller가 다른 스크립트의 Awake 전에 채움 (비활성 오브젝트, 부모 클래스 private 필드 포함). 등록이 없으면 `[DI] ... 등록돼 있지 않습니다` 경고.
  - `SceneInstaller` (씬마다 하나): `m_Binder`(SceneBinder), `m_SceneObjects`(그대로 등록할 씬 컴포넌트). 컨테이너는 그 씬에서만, 씬이 내려가면 정리.
  - `SceneBinder` (ScriptableObject, 메뉴 Create > UnityConv > DI > Scene Binder): `m_Classes`(new로 만들 클래스의 MonoScript), `m_Assets`(ScriptableObject). 인터페이스로도 등록됨.
    - `m_Classes`는 public 기본 생성자가 있는 일반 클래스만 (MonoBehaviour, ScriptableObject, abstract 불가).
    - 클래스 이름 문자열로만 만들어 코드 스트리핑에 지워질 수 있다. 등록하는 클래스에는 `[UnityEngine.Scripting.Preserve]`를 붙인다.
  - 실행 중 생성: `SceneInstaller.InstantiateAndInject(prefab, parent)` (Awake 전 주입), 이미 만든 오브젝트는 `SceneInstaller.Inject(gameObject)`.

```csharp
// 화면 Manager (Awake에서 하위 데이터 Manager 초기화, 데이터 Manager 간 화면 이동)
public class RobotScreenManager : ManagerBase
{
    [SerializeField] private RobotManager robotManager;
    [SerializeField] private WorkManager workManager;

    private void Awake()
    {
        robotManager.Initialize(this);
        workManager.Initialize(this);
    }

    public void OpenWorkStatus(string robotName) { workManager.OpenWorkStatus(robotName); }
}

// 데이터 Manager (Initialize에서 연결 + 최초 조회, 조회 → 취소/빈 결과면 버림 → 적용)
public class RobotManager : ManagerBase
{
    [SerializeField] private RobotSummaryUI robotSummaryUI;          // 처음부터 보이는 위젯
    [SerializeField] private RobotDetailPopupUI robotDetailPopupUI;
    [Inject] private IRobotDataSource dataSource;                    // Awake 전에 주입됨
    private RobotScreenManager screenManager;

    public void Initialize(RobotScreenManager screen)
    {
        screenManager = screen;
        BindViews(robotSummaryUI, robotDetailPopupUI);
        Request(robotSummaryUI, token => dataSource.GetRobotSummaryAsync(token), summary => robotSummaryUI.Initialize(this, summary));
    }

    public void OpenRobotDetail(string robotName)
    {
        if (string.IsNullOrEmpty(robotName)) return;
        Request(robotDetailPopupUI, token => dataSource.GetRobotDetailAsync(robotName, token), detail => robotDetailPopupUI.Initialize(this, detail));
    }

    public void OpenWorkStatus(string robotName) { screenManager.OpenWorkStatus(robotName); }
}

// UI (자기 Manager만 앎)
public class RobotDetailPopupUI : UIBase<RobotDetailData>
{
    [SerializeField] private ButtonToggle workStatusButton;
    private string robotName;
    private RobotManager Manager => Owner as RobotManager;

    protected override void OnOpened() { workStatusButton.onClick.AddListener(OnClickWorkStatusButton); }
    protected override void OnClosed() { workStatusButton.onClick.RemoveListener(OnClickWorkStatusButton); }
    protected override void ResetView() { SetInfoOpen(false); }
    public override void SetData(RobotDetailData detail) { robotName = detail.robot_name; ... }

    private void OnClickWorkStatusButton()
    {
        // Manager는 MonoBehaviour라 ?. 대신 != null (Unity fake null)
        if (Manager != null) Manager.OpenWorkStatus(robotName);
    }
}
```

### XXXService
- **2개 이상의 Manager가 참조해야 하는 기능이면 Service로 정의한다.** 앱 전체 기능, 상태(서버 통신, 팝업, 현재 라인, 로그인 사용자 등)도 Service.
- 한 Manager만 쓰면 그 Manager에 두고, 두 번째 Manager가 필요로 할 때 분리한다.
- 항상 `static class`. 씬 오브젝트, MonoBehaviour, DI 등록 없음. 비동기는 async/await, 설정은 Resources 에셋을 처음 쓸 때 읽는다 (예: `APIService` ← `Resources/APIConfig`).
- 프로젝트 설정의 Service 폴더에 둔다.
- Manager, UI를 모른다. 값 반환이나 이벤트 발행만. 분리해도 Manager의 public 메서드는 유지해 UI 영향 없음.

### DataSource (Network 계층)
- `IXxxDataSource` + `XxxSampleSource` (샘플 JSON, 서버와 같은 응답 래퍼 형식) + `XxxApiSource` (서버 통신 클래스).
- Sample 구현은 `<씬> Sample Binder`, Api 구현은 `<씬> Api Binder`의 Classes에 등록한다. 둘 다 같은 인터페이스라 SceneInstaller의 바인더만 바꾸면 전환된다 (코드의 `useSample` 분기, `new XxxSampleSource()` 금지).
- Manager만 사용. 실패 / 취소 시 null. 조건은 명시적 인자로 받고 `CancellationToken`을 마지막 인자로 받는다.
- 취소는 시그니처만으로 끝나지 않는다. 받은 토큰을 HTTP 요청과 재시도 대기까지 넘겨 실제 요청을 중단하고, 취소되면 null을 반환하고 실패 로그를 남기지 않는다. 패키지 `APIService`의 GET / POST / 파일 API는 모두 마지막 인자로 토큰을 받는다.
- 프로젝트 서버 통신 클래스에 취소 지원이 없으면 선택 인자 `CancellationToken cancellationToken = default`를 추가하고 위처럼 끝까지 넘긴다 (기존 호출 유지).
- 수정 금지 폴더라 토큰을 넘길 수 없으면 사용자에게 확인한 뒤 그대로 호출한다. 응답은 ManagerBase가 버리지만 네트워크 요청은 끝까지 진행되므로 결과 보고에 적는다.

### XXXController
Manager가 하던 일 중 **데이터 조회와 UI 전달이 아닌 기능**(오브젝트 동작, 연출, 카메라, 3D 상호작용 등)을 떼어낸 컴포넌트. 처음부터 독립적인 기능(LookAt, 회전, 하이라이트)도 Controller로 만든다.
- 데이터를 다루면 Manager, 다루지 않으면 Controller. 그래서 데이터 Manager는 Controller로 부르지 않는다.
- 이름은 기능 + Controller (`LookAtController`), 프로젝트 설정의 Controller 폴더.
- 기능 하나에 Controller 하나. 한 단위의 여러 기능을 한 클래스에 모으지 않는다.
- 동작 대상은 필드로 받는다 (`lookAtTransformList`). 자기 `transform`을 대상으로 쓰지 않는다.
- Controller 오브젝트는 자신을 제어하는 Manager의 자식으로 둔다 (아래 씬 구조). 어느 Manager도 제어하지 않으면 `Manager` 그룹 바로 아래.
- 대상이 프리팹이면 (실행 중 생성하거나 두 곳 이상에서 씀) 그 프리팹 루트가 자기 Controller를 갖는다. 바깥 Controller는 프리팹 안을 직접 참조하지 않고 그 Controller의 public 메서드를 호출한다.
- Unity 이벤트도 Controller에서 처리한다: 클릭은 `Physics.Raycast`로 맞은 Collider를 목록에서 찾고, 보임 여부는 `Renderer.isVisible`, Animator는 참조해서 제어. 충돌, Animator 이벤트처럼 대상 오브젝트에 스크립트가 꼭 있어야 할 때만 대상에 전달용 컴포넌트를 두고 결과 보고에 적는다.
- Manager, UI를 모른다. 화면 상태에 따라 제어할 때는 Manager → Controller public 메서드 한 방향. 클릭처럼 Manager에 알려야 하면 Controller가 `public event Action<T>`를 열고 Manager가 OnEnable / OnDisable에서 구독, 해제한다. 여러 Manager가 제어하면 `SceneInstaller`의 `m_SceneObjects`에 등록하고 `[Inject]`로 받는다.
- 프로젝트 스크립트에만 적용한다 (Collider, Animator, TMP 등은 원래 위치).
- 기존 코드 정리 시 Controller로 옮기는 것은 정리 범위 안에서만, 옮긴 목록은 결과 보고에 적는다.

### 씬 구조
- 루트 순서는 아래로 고정한다. 3D 콘텐츠 그룹(이름은 컨텍스트 기반)은 `Manager`와 `SceneInstaller` 사이에 둔다.
```
Directional Light
MainCamera                 (Unity 기본 이름 Main Camera를 바꿈, 태그 MainCamera 유지)
Canvas
EventSystem
Manager                    (정리용 그룹, 컴포넌트 없음)
├ RobotScreenManager       (화면 Manager)
│ ├ RobotManager           (데이터 Manager)
│ └ LookAtController       (이 Manager가 제어하는 Controller)
└ ...
Environment                (3D 콘텐츠 그룹, 예시)
SceneInstaller             (항상 맨 아래)
```
- **Canvas는 씬에 하나, 이름은 `Canvas`**. 새 UI는 이 Canvas 아래에 만든다. 메뉴로 UI를 만들다 Canvas가 새로 생기면 UI를 기존 Canvas로 옮기고 새 Canvas는 지운다.
  - 사용자가 Canvas를 따로 요청하면 요청한 이름으로, 이름 지정이 없으면 요청 내용에 맞게 짓는다 (`PopupCanvas`).
  - 3D 오브젝트 위 라벨처럼 World Space가 필요한 UI는 요청이 없어도 Canvas를 따로 만들고, 같은 방식으로 이름을 짓는다 (`RobotLabelCanvas`).
  - 기존 씬에 Canvas가 여러 개면 범위 밖 Canvas는 합치지 않고 결과 보고에 적는다 (정렬 순서, 스케일이 바뀜).
- 기존 씬은 정리 범위 안의 오브젝트만 이 구조로 옮기고, 옮긴 내용을 결과 보고에 적는다.

### UI 컴포넌트 (패키지 제공)
- 버튼과 토글은 `ButtonToggle`을 쓴다. 모드(Button / Toggle)는 인스펙터 툴바로 정한다.
  - Button 모드: `onClick`. Toggle 모드: `onValueChanged(bool)`, `isOn`, `group`(ToggleGroup).
  - On/Off 모습은 `Off/On Color`, `Off/On Sprite`로 정한다. On Sprite가 비어 있으면 색만 바뀐다. 바꾸는 대상은 Target Graphic이다.
  - 값을 이벤트 없이 바꿀 때는 `SetIsOnWithoutNotify`. 그룹에서 켜진 토글 조회는 `group.ActiveButtonToggles()`, `group.AnyButtonTogglesOn()`.
  - XXXUI는 `OnOpened`에서 `onClick` / `onValueChanged`에 리스너를 등록하고 `OnClosed`에서 해제한다.
- 둥근 모서리 이미지는 `RoundImage`를 쓴다 (Sprite는 Image 모드, Texture(RenderTexture 등)는 `sourceType = RawImage` + `texture`). 새로 만들 때는 메뉴 GameObject > UI (Canvas) > Button Toggle / Round Image.

## 3. 코드 컨벤션 (공통)
- **필드 이름**: private / protected / internal(`[SerializeField]`, `[Inject]` 포함)은 camelCase, public은 PascalCase. 접두사(`_`, `m_`, `text_`) 없음. `[SerializeField] private` + `[Header]`로 묶는다.
  - 용도 + 타입 순: `closeButton`, `closeButtonText`(CloseButton 안의 Text), `contentRect`, `robotInfoUI`, `robotManager`. TMP와 Text는 `Text`, RectTransform은 `Rect`.
  - GameObject는 오브젝트 이름 그대로, 묶음은 `Group`, 큰 화면 단위는 `Panel`로 끝낸다 (`emptyGroup`, `detailPanel`).
  - List와 배열은 `xxxList` (`closeButtonList`). Data 목록은 `robotDataList`(`List<RobotData>`), 목록 Data 클래스는 `robotListData`(`RobotListData`)로 구분한다.
  - bool은 `is` / `has` / `can` (`isOpen`). 매개변수, 지역 변수는 필드와 같은 이름을 쓰지 않는다.
  - 예외: Data 필드는 서버 응답과 같은 snake_case.
- 메서드, 속성, 이벤트, 상수(static readonly 포함)는 접근 수준과 관계없이 PascalCase.
- 메서드 호출은 위치 인자만 쓴다 (named argument 금지).
- 주석은 핵심만 한 줄. 항목 구분에 `·`를 쓰지 않고 `,` 또는 `/`를 쓴다.
- 이벤트 구독, 해제는 OnEnable / OnDisable (UIBase 자식은 OnOpened / OnClosed).
- 표시 문구는 인스펙터 string. 값 없음 표시, 입력 시스템, 상태 Tag는 프로젝트 설정의 공용 유틸을 쓴다.
- C# 관례대로 약어도 파스칼 (`OeeManager`).
- 편집 전 파일을 다시 읽고, 요청 범위 밖은 고치지 않는다. 단, 이름 변경에 따른 호출부 수정은 범위 밖이어도 한다 (5절).

## 4. 절차

### 기존 코드 정리
1. **조사**: 임시 데이터(`CreateSample*`, `useSample`, 하드코딩), 직접 조회, 조건 비교 코드, 싱글톤 `.Instance` 사용을 찾는다. UI를 위젯 / 팝업, 조건 변경이 있는 UI로 분류한다. 다른 Manager에 비슷한 코드가 있는지, 다른 씬이 참조하는 타입인지(`.meta` guid로 씬, 프리팹 검색 + 타입명 grep) 확인한다.
2. **Data**: 이름 규칙대로 Data 폴더로 옮기고, 공통 계산을 `GetXxx()`로.
3. **DataSource**: 인터페이스, Sample, Api, 샘플 JSON, API 경로 등록. 기존 `new XxxSampleSource()`, `useSample` 분기는 지우고 바인더로 옮긴다.
   - 바인더가 없으면 **자동으로 만든다**: Unity MCP로 프로젝트 설정의 바인더 폴더에 `<씬> Sample Binder.asset`, `<씬> Api Binder.asset`을 만들고 `m_Classes`에 구현 MonoScript를 넣는다 (SerializedObject로 설정 후 저장). MCP가 없으면 메뉴 Create > UnityConv > DI > Scene Binder로 만들도록 안내한다.
4. **Base**: 패키지 클래스를 쓴다 (0절). `using UnityConv.Bases;` / `using UnityConv.Network;` / `using UnityConv.DI;` 추가.
5. **UI**: UIBase 상속, Initialize 규칙, 요청은 Manager로, 조회, 조건 비교 코드 제거.
6. **Manager**: 화면 Manager + 데이터 Manager, `[Inject]`로 DataSource, Request로 조회와 Initialize. `.Instance`는 2절 규칙대로 `[Inject]`로 바꾼다.
7. **씬 연결**: Manager GameObject 추가, 참조 연결. 씬에 `SceneInstaller`가 없으면 **자동으로 만든다** (오브젝트 이름 `SceneInstaller`, 루트 맨 아래, 기본 바인더 연결). 오브젝트 위치는 2절 씬 구조를 따른다. 이미 있으면 기존 바인더에 등록만 추가한다. 주입할 기존 싱글톤 Manager는 `m_SceneObjects`에 넣는다.
8. **검증**: 컴파일 에러 0, `[DI]` 경고 0, 플레이 모드에서 모든 UI 열기, 조건 변경, 빠른 연타, 응답 전 닫기, 화면 전환. 플레이 후 씬 dirty 아님, `git status`로 범위 밖 변경 없음.
   - 확인은 콘솔 로그와 텍스트 상태(오브젝트 활성 여부, 컴포넌트 값, `IsShown`)로 한다. **스크린샷은 사용자가 요청할 때만** 찍는다 (이미지 한 장이 코드 파일 수십 개만큼 토큰을 씀). 단, 새 UI를 만들었으면 잘림, 겹침 확인용으로 한 장 찍는다.
   - 바인더나 DI 구현을 새로 만들었거나 바꿨으면 대상 플랫폼 Player 빌드를 한 번 실행해 Sample / Api 바인딩을 확인한다 (스트리핑). 못 하면 보고에 적는다.
   - MCP가 없어 씬 연결이나 플레이 검증을 못 하면 코드까지만 완료로 보고하고, 남은 연결 작업을 목록으로 적는다.
   - 씬, 프리팹 YAML은 통째로 읽지 않고 guid, 오브젝트 이름으로 grep해 필요한 블록만 본다.
9. **결과 보고**: 변경한 파일 / 실행한 검증과 결과 / 실행하지 못한 검증과 이유 / 남은 수동 작업(인스펙터 연결 등)을 나눠 적는다. 시작 때 기록한 `git status`와 비교해 이번 변경만 적는다.
10. 화면 단위로 단계 적용, 커밋은 사용자 요청 시.

### 신규 기능(새 팝업, 위젯, 화면) 개발
처음부터 이 구조로 만든다.
1. **소속 정하기**: 기존 데이터 Manager 소관이면 거기에 추가, 새 데이터 단위면 데이터 Manager를 새로 만들어 화면 Manager에 연결.
2. **Data**: 서버 응답 형식대로 `XxxData` / `XxxListData`, 계산은 `GetXxx()`.
3. **DataSource**: 인터페이스에 `GetXxxAsync(조건, CancellationToken)` 추가, Sample(샘플 JSON), Api 구현. 새 구현은 Sample / Api 바인더에 등록 (없으면 위 3단계처럼 생성).
4. **UI**: `UIBase<XxxData>` 상속, `SetData`, `OnOpened` / `OnClosed`, `ResetView`. 화면 이동, 조건 변경은 자기 Manager 메서드.
5. **Manager**: `[Inject] private IXxxDataSource dataSource;`, `OpenXxx` / `RequestXxx`에서 `Request(ui, fetch, apply)`, 연결 UI는 `[SerializeField]` + `BindViews`.
6. **오브젝트, 프리팹 이름**: 스크립트 이름, 연결된 필드 이름의 PascalCase와 같게 (5절).
7. **씬 연결, 검증**: 위 7, 8과 같음.

## 5. 이름 변경 주의
- 클래스, 파일 이름은 `.meta`를 같이 옮겨 guid 유지 (씬, 프리팹 연결 유지).
- 기존 코드를 정리할 때 필드 이름은 3절 규칙으로 바꾼다. 새 코드에는 아래 어트리뷰트를 쓰지 않는다.
  - **1차 정리**: 직렬화되는 필드(`[SerializeField]`, public 필드) 이름을 바꾸면 `[FormerlySerializedAs("old")]`를 붙인다 (범위 밖 씬, 프리팹의 연결 유지). public 멤버는 다른 파일의 호출부도 같이 바꾼다.
  - public 메서드 이름을 바꾸면 씬, 프리팹의 UnityEvent 연결(`m_MethodName: 옛이름`)도 grep해 같이 고친다 (이름 문자열로 연결돼 있어 컴파일 에러가 나지 않음).
  - **2차 정리** (사용자가 요청할 때): 그 스크립트를 쓰는 씬, 프리팹, 에셋을 `.meta` guid로 모두 찾아 다시 저장(`AssetDatabase.ForceReserializeAssets`)하고, 옛 필드 이름이 YAML에 남아 있지 않은지 grep으로 확인한 뒤 `FormerlySerializedAs`를 지운다. 하나라도 남아 있으면 지우지 않고 보고한다.
- **오브젝트 이름은 연결된 필드 이름의 PascalCase**로 한다 (`closeButtonText` → `CloseButtonText`, XXXUI / XXXManager가 붙은 오브젝트는 스크립트 이름). 필드가 없는 오브젝트도 같은 방식(`HeaderGroup`). 같은 스크립트가 여럿이면 `RobotFloatLabelUI_AGV-01`처럼 뒤에 구분자.
  - 기존 오브젝트 이름을 바꾸기 전에 Animator 클립 경로, `Find("...")` 문자열, 다른 씬의 이름 오버라이드를 검색한다. 걸리면 같이 고치거나 이름을 유지하고 보고한다.
- 프리팹 루트 이름은 파일 이름을 따르므로 `AssetDatabase.RenameAsset`으로 프리팹 파일 이름을 바꾼다 (guid 유지). 상위 프리팹, 씬의 중첩 인스턴스 이름 오버라이드(`propertyPath: m_Name`)도 같이 고친다. 작업 씬이 아닌 다른 씬의 오버라이드는 건드리지 않는다.
- 씬, 프리팹 편집은 Unity MCP(에디터 API)로 하고 저장한다. YAML 직접 편집은 Unity 저장이 파일을 크게 재직렬화할 때만, 아래를 모두 지킬 때 허용한다.
  - Unity에서 그 씬이 열려 있지 않거나, 열려 있으면 미저장 변경이 없다.
  - 새 fileID는 파일 안의 기존 ID와 겹치지 않는다.
  - GameObject의 컴포넌트 목록, Transform의 부모(`m_Father`)와 자식(`m_Children`), 참조 fileID가 서로 맞는다.
  - 프리팹 인스턴스(`PrefabInstance`) 안은 직접 편집하지 않는다.
  - 편집 후 씬을 다시 열어 Missing Script, 끊긴 참조가 없는지 확인한다.

## 6. 보류 (구현하지 말고 필요 시 사용자와 논의)
- 로딩, 에러 관제 (서버 통신 실패 정보 → 전역 상태 Service)
- 다른 씬이 쓰는 enum, 여러 씬에서 쓰는 싱글톤의 Service 전환 (사용자가 원할 때만)
- 저장, 삭제 같은 변경 작업의 공통 처리 (중복 실행 방지, 요청 중 닫힘, 성공 후 재조회). 지금 `Request`는 조회 전용이다.
- 실패, 빈 결과의 화면 처리 기준 (실패 시 기존 결과 유지 여부, 실패 안내, 재시도)
