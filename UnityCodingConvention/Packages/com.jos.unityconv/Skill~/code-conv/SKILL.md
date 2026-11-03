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
   - 폴더: Manager / UI / Service / Network(DataSource) / Data / Defs(enum) / 샘플 JSON / DI 바인더
   - DI: 씬마다 기본 바인더 (Sample / Api 중 지금 쓰는 것)
   - 팝업 여는 방법 (기본은 패키지의 `PopupService`, 프로젝트 전용 팝업 관리자가 있으면 그 클래스)
   - 서버 통신 클래스와 응답 래퍼 (기본은 패키지의 `APIService`, `APIResponse`), 취소 토큰 지원 여부. `APIService`면 `Resources/APIConfig` 에셋 위치
   - 공용 유틸, 입력, 태그 규칙 (값 없음 표시, 입력 시스템, 상태 Tag 등)
   - 수정 금지 폴더 (서드파티, 다른 팀 소유)
   - 기준 예시 (이미 이 구조로 만든 화면)
   - 이번 정리에서 제외할 것 (다른 씬이 쓰는 타입 등)
3. **Base 코드**: `com.jos.unityconv` 패키지가 제공한다. 프로젝트에 복사하거나 새로 만들지 않는다.
   - `Packages/manifest.json`에 `com.jos.unityconv`가 없으면 작업 전에 사용자에게 설치를 안내한다. 예전 이름 `com.jos.code-conv`가 있으면 교체를 안내한다.
   - Bases: `using UnityConv.Bases;` (`ManagerBase`, `UIBase`, `UIBase<TData>`, `PopupService`)
   - Network: `using UnityConv.Network;` (`APIService`, `APIStore`, `APIResponse<T>`). `APIService`는 static이고 처음 쓸 때 `Resources/APIConfig`(APIStore 에셋)를 읽는다. 서버(Dev / Release 등)는 APIStore의 Servers 목록과 Selected Server로 고르고, 코드에서 서버 주소를 분기하지 않는다. 씬에 오브젝트를 두지 않는다 (예전 `NetworkManager`가 씬에 있으면 APIStore를 `Resources/APIConfig`로 옮기고 지우도록 안내).
   - DI: `using UnityConv.DI;` (`[Inject]`, `SceneInstaller`, `SceneBinder`)
   - UI: `using UnityConv.UI;` (`ButtonToggle`, `RoundImage`, ToggleGroup 확장)
   - 팝업은 `UIBase.Show()` 기본값으로 `PopupService.Push(this)`가 연다. 닫기는 `PopupService.Close(this)`, 뒤로 가기는 `PopupService.Back()`. `PopupService`는 static 전역이라 씬에 오브젝트를 두지 않는다.
   - 프로젝트에 이미 같은 이름의 base 클래스(전역 네임스페이스)가 있으면 섞어 쓰지 않는다. 화면 단위로 패키지 클래스로 옮길지 사용자에게 확인한다.
   - 패키지 코드(`Packages/com.jos.unityconv`, `Library/PackageCache`)는 수정하지 않는다. 기능이 부족하면 사용자에게 알린다.
   - **패키지 소스는 읽지 않는다.** 사용법은 2절의 "Base API 요약"으로 충분하다. 요약에 없는 동작이 꼭 필요할 때만 해당 메서드 부분만 읽는다.
4. Unity MCP를 쓰면 프로젝트 CLAUDE.md의 인스턴스 고정 규칙을 먼저 따른다.

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

의존 방향: **UI → Manager → Service / DataSource**. 역방향 금지.

## 2. 역할 규칙

### XXXData (서버 데이터)
- 서버 응답과 같은 형태, snake_case 필드, 데이터 하나당 클래스 하나. 프로젝트 설정의 Data 폴더에 둔다.
- 이름: 목록 `XxxListData` (안에 `XxxData` 목록 + 부가 정보), 항목 `XxxData`. 목록 항목과 상세가 다르면 `XxxDetailData`, 집계는 `XxxSummaryData`.
- 서버가 배열을 바로 주면 `XxxData[]`로 받고 ListData는 만들지 않는다.
- 계산, 판단은 Data 안의 `GetXxx()` 메서드 (예: `GetOeeRate()`, `GetStatusTag()`). 이름에 무엇을 얻는지 쓴다 (`GetData` 금지). 기존 get 접근자 속성은 유지해도 된다.
- Color, 인스펙터 문구 등 UI 요소는 넣지 않는다. enum은 Defs에 둔다.

### XXXUI (View)
- `UIBase<TData>` 상속. 받은 Data로 그리기만 한다. DataSource, Service를 호출하지 않는다.
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
- 팝업 안 조건 변경 결과는 조회 조건을 담은 Data로 받아 SetData에서 조건 표시까지 갱신한다.

### XXXManager
- `ManagerBase` 상속. **MonoSingleton 사용 금지.**
- **기존 MonoSingleton Manager(`XxxManager.Instance`)는 DI로 정리한다.**
  - 같은 씬에만 있으면: 그 Manager를 `SceneInstaller`의 `m_SceneObjects`에 등록하고, 쓰는 Manager는 `Instance` 대신 `[Inject] private XxxManager _xxx;`로 받는다. `Instance`를 쓰는 곳이 없어지면 싱글톤 상속, `Instance`, `DontDestroyOnLoad`를 지운다.
  - 이번 범위 밖 코드가 `Instance`를 쓰면 싱글톤은 남기고 범위 안만 `[Inject]`로 바꾼다. 남은 사용처는 결과 보고에 적는다.
  - UI가 `Instance`를 쓰면 자기 Manager를 거치게 바꾼다 (UI는 `[Inject]` 안 씀).
  - 여러 씬에서 쓰는 싱글톤(`DontDestroyOnLoad`)은 씬 단위 DI로 옮길 수 없다. 앱 전체 상태면 static Service로 옮길지 사용자에게 확인하고, 아니면 그대로 두고 보고한다.
- **화면 Manager**: 하위 데이터 Manager를 `[SerializeField]`로 들고 `Awake`에서 순서대로 `Initialize(this)` (Awake 초기화는 화면 Manager만). 화면 공통 상태, 주기 갱신, 하위 Manager 간 화면 이동. DataSource는 만들지 않는다.
- **데이터 Manager**: 화면 Manager의 자식 GameObject 컴포넌트, 이름도 `XxxManager` (Controller로 부르지 않음). 해당 데이터의 조회와 UI 전달. 연결 UI는 `[SerializeField]`로 들고 `BindViews`로 자신을 알린다 (Inspector에서 확인). DataSource는 `[Inject] private IXxxDataSource _source;`로 받는다.
- 데이터 Manager끼리는 서로 참조하지 않는다. 다른 Manager 소관 UI는 **화면 Manager를 거쳐** 연다 (화면 이동은 순환하기 쉬움).
- 화면 하나에 데이터 Manager가 많으면 공통 부모(`Screen`, 공통 갱신 메서드)를 둔다.
- **DI**: `[Inject]`는 Manager만 쓴다 (UI에 쓰면 SceneInstaller가 주입하지 않고 에러). 주입 대상은 DataSource, 설정 에셋, 기존 싱글톤 Manager(위). Manager끼리 참조는 `[SerializeField]`, Service는 static이라 주입하지 않는다.
- 데이터가 다 와야 UI를 Initialize (여러 개면 `Task.WhenAll`).
- Manager가 Initialize하지 않고 열린 UI(공용 상세 버튼 등)는 UIBase가 `HandleUIOpened`로 알려주고, 필요한 Manager만 오버라이드해 조회.
- Manager가 Initialize하지 않는 그 밖의 진입점(3D 클릭 등)은 화면 Manager를 `[SerializeField]`로 참조.
- Action/이벤트로 UI에 뿌리지 않는다. 직접 Initialize.
- Unity 오브젝트 필드에 `?.` 쓰지 않는다 (Inspector 미연결 fake null 통과). `!= null`로 확인.

### ManagerBase / UIBase (공통 부모, 요청과 취소)
- ManagerBase: UI별 요청 토큰 관리. 같은 UI(+채널)의 새 요청이 오면 이전 요청 취소, UI가 닫히면 취소, Manager가 꺼지면 전체 취소.
- 늦게 온 응답, 조건이 바뀐 뒤의 응답, 닫힌 뒤의 응답은 모두 토큰 하나로 버린다 (UI별 조건 비교 코드 금지).
- 한 UI가 독립된 요청을 여러 개 하면 채널로 구분 (예: 목록 / 하위 창).

**Base API 요약** (`UnityConv.Bases`, `UnityConv.DI`, 이 요약대로 쓰고 소스는 읽지 않는다)
- `ManagerBase : MonoBehaviour`
  - `protected void Request<T>(UIBase ui, Func<CancellationToken, Task<T>> fetch, Action<T> apply, string channel = null) where T : class`: 같은 (ui, channel)의 이전 요청 취소 → fetch → 취소됐거나 null이면 버림, 아니면 apply. fetch, apply 예외는 로그만 남김.
  - `protected void BindViews(params UIBase[] views)`: UI에 이 Manager를 연결 (null이면 경고).
  - `public virtual void HandleUIOpened(UIBase ui)`: Manager가 Initialize하지 않았는데 연결된 UI가 열렸을 때 호출됨.
  - `public void CancelRequests(UIBase ui)`: UI가 닫힐 때 UIBase가 호출 (직접 부를 일 거의 없음).
  - `OnDisable`, `OnDestroy`는 virtual이고 전체 요청을 취소한다. 오버라이드하면 `base.OnDisable()` / `base.OnDestroy()`를 호출한다.
- `UIBase : MonoBehaviour`
  - `protected ManagerBase Owner`, `public bool IsShown`
  - `protected virtual bool IsPopup => true` (위젯은 false), `protected virtual void Show()` (기본 `PopupService.Push(this)`), `protected virtual void ResetView()`
  - `protected virtual void OnOpened()` / `OnClosed()`: OnEnable / OnDisable 대신 쓴다 (UIBase가 OnEnable/OnDisable을 private으로 씀, 자식이 만들면 안 됨).
- `UIBase<TData> : UIBase` (TData는 class)
  - `public void Initialize(ManagerBase owner, TData data)`: 닫혀 있으면 Show + (닫힌 뒤 처음이면) ResetView, 그다음 SetData.
  - `public abstract void SetData(TData data)`
- `PopupService` (static): `Push(Component)`, `Close(Component)`, `Back()`, `CloseAll()`, `Get<T>()` (비활성 포함 씬 검색), `GetPrevious(Component)`, `LogEnabled`
- DI (`UnityConv.DI`)
  - `[Inject]` 필드: SceneInstaller가 다른 스크립트의 Awake 전에 채움 (비활성 오브젝트, 부모 클래스 private 필드 포함). 등록이 없으면 `[DI] ... 등록돼 있지 않습니다` 경고.
  - `SceneInstaller` (씬마다 하나): `m_Binder`(SceneBinder), `m_SceneObjects`(그대로 등록할 씬 컴포넌트). 컨테이너는 그 씬에서만, 씬이 내려가면 정리.
  - `SceneBinder` (ScriptableObject, 메뉴 Create > UnityConv > DI > Scene Binder): `m_Classes`(new로 만들 클래스의 MonoScript), `m_Assets`(ScriptableObject). 인터페이스로도 등록됨.
  - 실행 중 생성: `SceneInstaller.InstantiateAndInject(prefab, parent)` (Awake 전 주입), 이미 만든 오브젝트는 `SceneInstaller.Inject(gameObject)`.

```csharp
// 데이터 Manager (조회 → 취소/빈 결과면 버림 → 적용)
public void OpenRobotDetail(string robotName)
{
    if (string.IsNullOrEmpty(robotName)) return;
    Request(ui_robotDetail, token => _source.GetRobotDetailAsync(robotName, token), detail => ui_robotDetail.Initialize(this, detail));
}

// UI (자기 Manager만 앎)
public class RobotDetailPopupUI : UIBase<RobotDetailData>
{
    private RobotManager Manager => Owner as RobotManager;
    protected override void OnOpened() { /* 리스너 구독 */ }
    protected override void OnClosed() { /* 리스너 해제 */ }
    protected override void ResetView() { SetInfoOpen(false); }
    public override void SetData(RobotDetailData detail) { ... }
    private void OnClickWorkStatusButton()
    {
        // Manager는 MonoBehaviour라 ?. 대신 != null (Unity fake null)
        if (Manager != null) Manager.OpenWorkStatus(_robotName);
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
- 서버 통신 클래스에 취소 지원이 없으면 선택 인자 `CancellationToken cancellationToken = default`만 추가한다 (기존 호출 유지, 취소 시 null + 에러 로그 없음). 수정 금지 폴더면 사용자에게 먼저 확인.

### UI 컴포넌트 (패키지 제공)
- 버튼과 토글은 `ButtonToggle`을 쓴다. 모드(Button / Toggle)는 인스펙터 툴바로 정한다.
  - Button 모드: `onClick`. Toggle 모드: `onValueChanged(bool)`, `isOn`, `group`(ToggleGroup).
  - On/Off 모습은 `Off/On Color`, `Off/On Sprite`로 정한다. On Sprite가 비어 있으면 색만 바뀐다. 바꾸는 대상은 Target Graphic이다.
  - 값을 이벤트 없이 바꿀 때는 `SetIsOnWithoutNotify`. 그룹에서 켜진 토글 조회는 `group.ActiveButtonToggles()`, `group.AnyButtonTogglesOn()`.
  - XXXUI는 `OnOpened`에서 `onClick` / `onValueChanged`에 리스너를 등록하고 `OnClosed`에서 해제한다.
- 둥근 모서리 이미지는 `RoundImage`를 쓴다. 새로 만들 때는 메뉴 GameObject > UI (Canvas) > Button Toggle / Round Image.

## 3. 코드 컨벤션 (공통)
- 필드는 `[SerializeField] private` + 타입 접두사(`text_x`, `button_x`, `ui_x`, `manager_x`), `[Header]`로 묶는다.
- 메서드 호출은 위치 인자만 쓴다 (named argument 금지).
- 주석은 핵심만 한 줄. 항목 구분에 `·`를 쓰지 않고 `,` 또는 `/`를 쓴다.
- 이벤트 구독, 해제는 OnEnable / OnDisable (UIBase 자식은 OnOpened / OnClosed).
- 표시 문구는 인스펙터 string. 값 없음 표시, 입력 시스템, 상태 Tag는 프로젝트 설정의 공용 유틸을 쓴다.
- C# 관례대로 약어도 파스칼 (`OeeManager`).
- 편집 전 파일을 다시 읽고, 요청 범위 밖은 고치지 않는다.

## 4. 절차

### 기존 코드 정리
1. **조사**: 임시 데이터(`CreateSample*`, `useSample`, 하드코딩), 직접 조회, 조건 비교 코드, 싱글톤 `.Instance` 사용을 찾는다. UI를 위젯 / 팝업, 조건 변경이 있는 UI로 분류한다. 다른 Manager에 비슷한 코드가 있는지, 다른 씬이 참조하는 타입인지(`.meta` guid로 씬, 프리팹 검색 + 타입명 grep) 확인한다.
2. **Data**: 이름 규칙대로 Data 폴더로 옮기고, 공통 계산을 `GetXxx()`로.
3. **DataSource**: 인터페이스, Sample, Api, 샘플 JSON, API 경로 등록. 기존 `new XxxSampleSource()`, `useSample` 분기는 지우고 바인더로 옮긴다.
   - 바인더가 없으면 **자동으로 만든다**: Unity MCP로 프로젝트 설정의 바인더 폴더에 `<씬> Sample Binder.asset`, `<씬> Api Binder.asset`을 만들고 `m_Classes`에 구현 MonoScript를 넣는다 (SerializedObject로 설정 후 저장). MCP가 없으면 메뉴 Create > UnityConv > DI > Scene Binder로 만들도록 안내한다.
4. **Base**: 패키지 클래스를 쓴다 (0절). `using UnityConv.Bases;` / `using UnityConv.Network;` / `using UnityConv.DI;` 추가.
5. **UI**: UIBase 상속, Initialize 규칙, 요청은 Manager로, 조회, 조건 비교 코드 제거.
6. **Manager**: 화면 Manager + 데이터 Manager, `[Inject]`로 DataSource, Request로 조회와 Initialize. `.Instance`는 2절 규칙대로 `[Inject]`로 바꾼다.
7. **씬 연결**: Manager GameObject 추가, 참조 연결. 씬에 `SceneInstaller`가 없으면 **자동으로 만든다** (오브젝트 이름 `SceneInstaller`, 루트, 기본 바인더 연결). 이미 있으면 기존 바인더에 등록만 추가한다. 주입할 기존 싱글톤 Manager는 `m_SceneObjects`에 넣는다.
8. **검증**: 컴파일 에러 0, `[DI]` 경고 0, 플레이 모드에서 모든 UI 열기, 조건 변경, 빠른 연타, 응답 전 닫기, 화면 전환. 플레이 후 씬 dirty 아님, `git status`로 범위 밖 변경 없음.
   - 확인은 콘솔 로그와 텍스트 상태(오브젝트 활성 여부, 컴포넌트 값, `IsShown`)로 한다. **스크린샷은 사용자가 요청할 때만** 찍는다 (이미지 한 장이 코드 파일 수십 개만큼 토큰을 씀).
   - 씬, 프리팹 YAML은 통째로 읽지 않고 guid, 오브젝트 이름으로 grep해 필요한 블록만 본다.
9. 화면 단위로 단계 적용, 커밋은 사용자 요청 시.

### 신규 기능(새 팝업, 위젯, 화면) 개발
처음부터 이 구조로 만든다.
1. **소속 정하기**: 기존 데이터 Manager 소관이면 거기에 추가, 새 데이터 단위면 데이터 Manager를 새로 만들어 화면 Manager에 연결.
2. **Data**: 서버 응답 형식대로 `XxxData` / `XxxListData`, 계산은 `GetXxx()`.
3. **DataSource**: 인터페이스에 `GetXxxAsync(조건, CancellationToken)` 추가, Sample(샘플 JSON), Api 구현. 새 구현은 Sample / Api 바인더에 등록 (없으면 위 3단계처럼 생성).
4. **UI**: `UIBase<XxxData>` 상속, `SetData`, `OnOpened` / `OnClosed`, `ResetView`. 화면 이동, 조건 변경은 자기 Manager 메서드.
5. **Manager**: `[Inject] private IXxxDataSource _source;`, `OpenXxx` / `RequestXxx`에서 `Request(ui, fetch, apply)`, 연결 UI는 `[SerializeField]` + `BindViews`.
6. **오브젝트, 프리팹 이름**: 스크립트 이름과 같게 (5절).
7. **씬 연결, 검증**: 위 7, 8과 같음.

## 5. 이름 변경 주의
- 클래스, 파일 이름은 `.meta`를 같이 옮겨 guid 유지 (씬, 프리팹 연결 유지).
- `[SerializeField]` 필드 이름을 바꾸면 `[FormerlySerializedAs("old")]`를 붙인다.
- **이 스킬의 스크립트(XXXUI, XXXManager)가 붙은 오브젝트는 이름을 스크립트 이름과 같게** 한다. 같은 스크립트가 여럿이면 `RobotFloatLabelUI_AGV-01`처럼 뒤에 구분자. 그 밖의 하위 오브젝트는 프로젝트 접두사 규칙(`Text_` / `Image_` / `Group_` / `Button_` 등) 유지.
- 프리팹 루트 이름은 파일 이름을 따르므로 `AssetDatabase.RenameAsset`으로 프리팹 파일 이름을 바꾼다 (guid 유지). 상위 프리팹, 씬의 중첩 인스턴스 이름 오버라이드(`propertyPath: m_Name`)도 같이 고친다. 작업 씬이 아닌 다른 씬의 오버라이드는 건드리지 않는다.
- 씬에 Manager를 추가할 때 기존 참조 fileID를 알면 Unity 저장 대신 YAML 블록(GO, Transform, MB)을 직접 추가하고 씬을 다시 연다 (Unity 저장은 파일을 크게 재직렬화함).

## 6. 보류 (구현하지 말고 필요 시 사용자와 논의)
- 로딩, 에러 관제 (서버 통신 실패 정보 → 전역 상태 Service)
- 다른 씬이 쓰는 enum, 여러 씬에서 쓰는 싱글톤의 Service 전환 (사용자가 원할 때만)
