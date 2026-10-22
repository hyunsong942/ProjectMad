# 매드 사이언티스트의 연구소 — Development Log

## 개발 기준과 범위

- 개발 기준: 사용자가 2026-10-21에 전달한 「Unity MCP 기반 3D 액션 게임 프로토타입 개발 지시서」.
- 최종 목표: Varco AI 에셋을 활용한 3인칭 스팀펑크 액션 게임 1스테이지, 플레이 시간 약 3~5분, 2일 개발.
- 현재 승인 범위: **Phase 1까지 승인·완료; Phase 2는 확인 대기**. 각 Phase 종료 후 사용자 확인을 받고 다음 Phase로 진행한다.
- 플레이어 이동, 카메라 추적, 회피는 Phase 1에서 구현했다. 전투, 적 AI, HUD, 스테이지 진행은 아직 구현하지 않았다.
- 점프 기능/입력/애니메이션을 추가하지 않는다. 향후 접지용 중력만 사용한다.
- 기존 씬/스크립트/설정 보존, 신규 작업은 Assets/Game 아래에 배치한다.
- 임시 Primitive는 기능 검증 수단이며 최종 모델로 간주하지 않는다.
- AI 작업과 사용자 작업, 실제 검증과 예정 작업을 구분한다.

## 2026-10-21 (Asia/Seoul) — Phase 0: 프로젝트 분석 및 초기 설정

### 사용자 요청

Unity MCP 연결 및 프로젝트 환경 분석, 기존 구조 확인, 개발 폴더 생성, 기존 씬을 보존한 신규 씬 생성, 3D 모델 확인, 리소스 목록과 개발 기록 작성. 완료 후 한국어 보고 및 다음 단계 확인 대기.

### 확인한 환경

| 항목 | 확인 결과 |
|---|---|
| 프로젝트 | ProjectMad |
| 프로젝트 경로 | C:/Users/mbc/Desktop/unity/ProjectMad |
| Unity | 6000.3.23f1 |
| Editor / 빌드 타깃 | WindowsEditor / StandaloneWindows64 |
| MCP | MCP for Unity 10.3.0, HTTP, ProjectMad@7286522e6862542f |
| 렌더 파이프라인 | Universal Render Pipeline 17.3.0 |
| 기본 렌더 설정 | Assets/Settings/PC_RPAsset.asset (GraphicsSettings 참조), PC/Mobile 품질별 RP 설정 존재 |
| Input System | com.unity.inputsystem 1.20.0, activeInputHandler=1 (새 Input System) |
| 기존 입력 에셋 | Assets/InputSystem_Actions.inputactions, Player/UI 액션 맵 |
| Cinemachine | 설치 목록에 없음 |
| AI Navigation | 2.0.14 설치됨, 아직 사용하지 않음 |
| uGUI / Test Framework | 2.0.0 / 1.6.0 |
| 신규 패키지 설치 | 없음 |
| 초기 Console | 조회된 오류 0건, 경고 0건 |

### 기존 구조 및 스크립트 분석

- Assets/Scenes/SampleScene.unity: 작업 시작 시 유일한 씬. Main Camera, Directional Light, Global Volume, MCP_Test_Cube가 열린 씬에 존재했다.
- SampleScene은 시작부터 isDirty=true였다. MCP_Test_Cube는 이전 대화에서 생성한 미저장 오브젝트이며 이번 Phase에서 수정하지 않았다.
- Assets/Settings: PC/Mobile URP Asset/Renderer와 Volume Profile 등 템플릿 렌더 설정.
- Assets/TutorialInfo/Scripts/Readme.cs: 템플릿 안내용 ScriptableObject.
- Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs: 템플릿 안내 Inspector/레이아웃용 Editor 코드. 게임 플레이 코드가 아니다.
- Packages에는 실제 com.coplaydev.unity-mcp 외에 이름이 Git URL에서 유래한 Embedded 패키지 0.1.0이 있다. 해당 RuntimeExample/EditorExample은 예제 계산 코드이며 게임 로직이 아니다. 삭제하거나 변경하지 않았다.
- Assets 내 기존 C# 파일은 위 Readme 관련 2개. 게임용 플레이어/적/전투 스크립트는 없다.
- 기존 입력 에셋에는 Move, Look, Attack, Sprint, Jump 등이 있다. **Jump→Space는 기존 템플릿 바인딩**이며 이번 작업에서 추가하지 않았다.
- 기존 입력 에셋은 수정하지 않았다. Phase 1에서 Assets/Game/Input에 별도 입력을 구성하여 Space를 Dodge로 연결할 계획이다.
- 기존 Build Settings에는 SampleScene만 등록되어 있다. 이번 Phase에서 빌드 설정은 변경하지 않았다.
- 프로젝트 및 조사한 상위 경로에서 AGENTS.md를 발견하지 못했다.

### 사용 가능한 에셋

- MCP의 Assets 범위 Model 검색 결과: **0개**.
- 파일 조사에서 FBX/OBJ/GLB/GLTF/Blend 등 모델 파일, 사용자 제작 Prefab/Material 파일을 발견하지 못했다.
- 사용 가능한 것은 Unity Primitive와 템플릿 렌더 설정, URP 안내 이미지 등이다.
- 사용자는 Varco 과학자 모델을 이미 제작했다고 설명했다. **현재 Unity 프로젝트에는 임포트되지 않았으며 모델 실물/품질/리깅 여부를 AI가 직접 검사하지 않았다.**
- Phase 1~3은 Capsule/Cube 등으로 진행 가능하다. 외부 모델 부재는 기능 개발을 막지 않는다.

### AI가 수행한 작업

1. Unity MCP 인스턴스, 프로젝트 경로, 버전, 현재 씬/Editor 상태를 조회했다.
2. manifest, 설치 패키지 목록, Graphics/Quality/Input 설정, 기존 씬/스크립트/모델을 조사했다.
3. 변경 전 Assets/Packages/ProjectSettings의 기존 파일 100개 SHA-256을 기록하고 작업 후 비교했다.
4. manage_asset(create_folder)를 batch_execute로 실행해 아래 17개 폴더를 생성했다.
5. 기존 SampleScene의 미저장 내용을 유지하기 위해 MCP execute_code에서 EditorSceneManager.NewScene(DefaultGameObjects, Additive)로 별도 씬을 생성하고 신규 경로에만 저장했다.
6. 신규 씬 활성 상태와 Hierarchy, 누락 스크립트/깨진 프리팹, Console/컴파일 상태를 확인했다.
7. 이 DevelopmentLog.md를 프로젝트 루트에 새 파일로 작성했다. 기존 파일 덮어쓰기를 막기 위해 CreateNew를 사용했다.

### 생성 파일 / 폴더

신규 씬: Assets/Game/Scenes/MadScientistLab_Prototype.unity

신규 문서: DevelopmentLog.md

개발 폴더:
- Assets/Game
- Assets/Game/Scenes
- Assets/Game/Scripts
- Assets/Game/Scripts/Player
- Assets/Game/Scripts/Enemy
- Assets/Game/Scripts/Combat
- Assets/Game/Scripts/Managers
- Assets/Game/Scripts/UI
- Assets/Game/Prefabs
- Assets/Game/Prefabs/Player
- Assets/Game/Prefabs/Enemies
- Assets/Game/Prefabs/Environment
- Assets/Game/Materials
- Assets/Game/Models
- Assets/Game/Effects
- Assets/Game/UI
- Assets/Game/Input

- 각 폴더와 씬의 .meta는 Unity가 생성했다.
- ProjectSettings/SceneTemplateSettings.json도 씬 생성 과정에서 Unity가 새로 생성했다. 기존 설정 파일을 덮어쓴 것이 아니다.
- 생성/수정한 C# 스크립트: **없음**. execute_code의 C#은 Editor에서 메모리 컴파일했으며 .cs 파일을 만들지 않았다.
- 신규 모델, 머티리얼, 프리팹, 입력 액션 파일: 아직 없음.

### Unity 오브젝트와 씬 상태

| 씬 | 오브젝트 / 상태 |
|---|---|
| SampleScene | 기존 4개 루트 그대로 로드됨; 미저장 상태 유지; 이번 Phase에서 저장/닫기/삭제하지 않음 |
| MadScientistLab_Prototype | Main Camera와 Directional Light만 생성; 저장 완료; 활성 씬 |
| 새 Main Camera | Transform, Camera, AudioListener |
| 새 Directional Light | Transform, Light, UniversalAdditionalLightData |

신규 씬은 기존 씬과 Additive로 함께 열려 있다. 이는 기존 미저장 변경을 보존하기 위한 상태다. Phase 1의 Play Mode 테스트 전에 SampleScene의 미저장 Cube 보존 방식을 사용자와 정하고, 개발 씬만 실행하여 중복 Camera/AudioListener를 피해야 한다. 이번 Phase에서는 Play Mode에 들어가지 않았다.

### 실제 검증 결과

| 검증 | 실제 결과 |
|---|---|
| Unity MCP 조회 | 인스턴스/씬/에셋/패키지/Console 조회 성공 |
| 신규 폴더 | 17/17 생성 성공 |
| 신규 씬 | 디스크 저장됨, isLoaded=true, isDirty=false, 활성 씬으로 조회됨 |
| Hierarchy | 신규 씬 루트 2개: Main Camera, Directional Light |
| Scene validate(auto_repair=false) | totalIssues=0, missingScripts=0, brokenPrefabs=0 |
| 기존 파일 무결성 | 초기 기존 파일 100개 SHA-256 모두 동일; 삭제/변경 0개 |
| SampleScene 무결성 | 기존 파일 SHA-256 동일, 열린 씬 루트 4개 및 미저장 상태 유지 |
| 컴파일 | is_compiling=false, is_domain_reload_pending=false, 최근 컴파일 완료 상태 |
| 최종 Console 조회 | Error 0건, MCP WebSocket Warning 1건 |
| 도구 준비 상태 | ready_for_tools=true, blocking_reasons=[] |
| 플레이 테스트 / 자동 게임 테스트 | 미실시 — Phase 0에 게임 시스템 없음 |
| 빌드 | 미실시 — Phase 5 예정 |

기존 SampleScene 파일 SHA-256:
`07FCE8B04804DDCCB3026DB5B43D8DC41BA138E49D50D36A5C24877081EACDCA`

위 무결성 검증 범위는 Assets/Packages/ProjectSettings의 작업 시작 당시 존재한 100개 파일이다. Library/Temp/Logs/UserSettings 등 Editor 내부 캐시/세션 파일은 검증 범위에 포함하지 않았다.

### 발생한 오류와 대응

1. **씬 생성 후 추가 활성화 호출의 실패 반환**
   - execute_code에서 신규 씬 저장 후 SetActiveScene 반환값 검사로 "Could not activate the new scene." 예외가 반환되었다.
   - 신규 씬이 이미 생성/저장된 상태일 수 있으므로 생성 명령을 재시도하지 않았다.
   - manage_scene(get_loaded_scenes/get_active/get_hierarchy)로 실제 상태를 재조회했다.
   - 신규 씬은 이미 로드·활성·저장된 상태이고 기존 SampleScene도 유지됨을 확인했다.
   - 정확한 false 반환 내부 원인은 확정하지 않았다. 도구 오류를 숨기거나 씬 생성 전체 실패로 오인하지 않고 결과 상태로 확인했다.

2. **MCP 연결 계층 경고 1건**
   - Console: "[WebSocket] Unexpected receive error: WebSocket is not initialised"
   - 게임 스크립트 컴파일 오류가 아닌 MCP 패키지 경고로 기록했다.
   - 이후 MCP 조회/검증은 성공하고 Editor ready_for_tools=true를 확인했다.
   - 경고를 지우거나 MCP/패키지 설정을 임의 수정하지 않았다. 반복되면 연결 상태를 다시 점검한다.

3. **표준 씬 생성 도구의 보존 위험을 사전 확인**
   - 설치된 manage_scene(create) 구현은 NewSceneMode.Single을 사용하며 additive 옵션은 load 경로에서만 적용되는 것을 소스에서 확인했다.
   - 기존 미저장 씬을 닫는 동작을 피하기 위해 Unity MCP execute_code의 Additive 생성 기능을 사용했다.
   - Unity API는 unity_reflect로 NewScene/SaveScene/SetActiveScene 시그니처를 확인했다.

### 사용한 Unity MCP 도구

- read_mcp_resource: editor/state, instances, project/info 조회. custom-tools 목록은 같은 대화의 선행 연결 점검에서 확인했다.
- manage_scene: get_loaded_scenes, get_active, get_hierarchy, validate(auto_repair=false).
- manage_asset: search(filter_type=Model), create_folder.
- manage_packages: list_packages, status.
- batch_execute: 17개 개발 폴더 순차 생성.
- unity_reflect: EditorSceneManager.NewScene/SaveScene, SceneManager.SetActiveScene 검증.
- execute_code: 기존 씬을 유지하는 Additive 신규 씬 생성/저장, DevelopmentLog.md 신규 작성.
- read_console: 오류/경고 조회. Console clear는 실행하지 않았다.
- 보조 로컬 읽기: 첨부 지시서, 프로젝트 파일/패키지 구현 읽기와 파일 해시 비교. 게임 씬/오브젝트는 Unity MCP로 조작했다.

### 사용자가 직접 수행한 작업

- 개발 지시서와 Phase 0 한정 범위를 제공했다.
- 실행 중인 Unity Editor와 MCP 연결 환경을 준비했다.
- Varco 과학자 모델을 이미 제작했다고 설명했다. 제작 과정 및 원본 파일은 이번 세션에서 확인하지 못했다.
- 이번 Phase에서 사용자에 의한 임포트/코드 작성/게임 테스트 완료는 보고받지 않았으며 수행했다고 기록하지 않는다.

## 리소스 제작 요청

아래 수치와 폴리곤 수는 이 프로토타입을 위한 권장 제작 예산이다. 모든 모델은 Unity 1유닛=1m 기준을 권장한다.
**Phase 0~1에 당장 필요한 외부 리소스는 없다.** 필수 제출 모델 준비와 이후 시각 개선 리소스를 구분한다.

### [리소스 제작 요청] 필수 제출용 — 기존 Varco 과학자

**리소스 이름:** Varco 과학자 NPC (기존 제작 모델 전달 우선)
**사용 목적:** Varco 활용 요구사항 충족 및 연구 공간의 정적인 NPC.
**외형 및 디자인 요구사항:** 스팀펑크 과학자, 고글/작업복/황동 장치; 기존 모델 디자인 우선.
**권장 크기 및 비율:** 높이 약 1.7~1.9m, 바닥 중심 피벗, 적용된 스케일.
**필요한 파츠 분리 여부:** 필수 아님; 몸과 소품이 합쳐져 있어도 가능.
**리깅 필요 여부:** 없음.
**필요한 애니메이션:** 없음.
**권장 파일 형식:** FBX 또는 OBJ+MTL, 텍스처 PNG/TGA 별도 포함. GLB만 있으면 Blender에서 FBX/OBJ로 변환하는 방식 우선.
**권장 폴리곤 수:** 약 10,000~40,000 triangles 권장; 실제 파일을 확인한 뒤 조정.
**Unity 적용 위치:** Assets/Game/Models, 개발 씬의 과학자 연구 공간.
**필요 시점:** Phase 4 시작 전; 최종 제출 전 필수.
**임시 오브젝트 사용 가능 여부:** 가능. 임시 NPC는 최종 Varco 리소스로 인정하지 않는다.

### [리소스 제작 요청] 후속 시각 적용 — 경비 로봇 1종

**리소스 이름:** 스팀펑크 경비 로봇
**사용 목적:** 동일 모델을 적 3기에 재사용.
**외형 및 디자인 요구사항:** 황동/흑철 몸체, 발광 눈, 실루엣이 분명한 근접 경비 로봇.
**권장 크기 및 비율:** 높이 약 1.5~1.8m, 폭 약 0.7~1m, 바닥 중심 피벗.
**필요한 파츠 분리 여부:** 몸통/머리/팔 분리 권장, 필수 아님.
**리깅 필요 여부:** 초기에는 불필요; 애니메이션 적용 시에만 필요.
**필요한 애니메이션:** 초기 없음; 시간이 남으면 Idle/Walk/Attack/Death.
**권장 파일 형식:** FBX 또는 OBJ+MTL와 텍스처.
**권장 폴리곤 수:** 약 5,000~20,000 triangles/1기.
**Unity 적용 위치:** Assets/Game/Models, 향후 경비 로봇 Prefab의 Visual 자식.
**필요 시점:** Phase 4; Phase 2~3 전투 검증 완료 뒤.
**임시 오브젝트 사용 가능 여부:** 가능. Capsule/Cube로 AI와 전투부터 검증.

### [리소스 제작 요청] 후속 시각 적용 — 연구소 소형 환경 세트

**리소스 이름:** 증기 보일러/배관/기계식 출구
**사용 목적:** 연구 공간과 탈출 구역의 스팀펑크 분위기.
**외형 및 디자인 요구사항:** 황동 배관, 흑철 프레임, 큰 밸브, 시인성 있는 출구.
**권장 크기 및 비율:** 보일러 높이 2~3m, 문 개구부 폭 약 2.5~3m·높이 3m, 회피 통로를 막지 않는 크기.
**필요한 파츠 분리 여부:** 출구 문짝과 프레임 분리 권장; 배관은 간단한 모듈 가능.
**리깅 필요 여부:** 없음.
**필요한 애니메이션:** 없음. 문 이동/색상 변경은 Unity에서 구현 가능.
**권장 파일 형식:** FBX 또는 OBJ+MTL와 텍스처.
**권장 폴리곤 수:** 소품당 약 1,000~10,000 triangles.
**Unity 적용 위치:** Assets/Game/Models 및 향후 Environment Prefab.
**필요 시점:** Phase 4; 최소 게임 완성 이후.
**임시 오브젝트 사용 가능 여부:** 가능. 바닥/벽/배관/출구는 Primitive와 기본 머티리얼로 먼저 제작.

### [리소스 제작 요청] 선택 개선 — 플레이어 모델

**리소스 이름:** 스팀펑크 탐험가 플레이어
**사용 목적:** 초기 Capsule 외형 교체.
**외형 및 디자인 요구사항:** 뒤에서 읽기 쉬운 실루엣, 짧은 코트/작업복/금속 장비.
**권장 크기 및 비율:** 높이 약 1.75~1.85m, 발바닥 중심 피벗.
**필요한 파츠 분리 여부:** 무기/소품 별도 파츠 권장.
**리깅 필요 여부:** 정적 모델 교체에는 불필요; 캐릭터 애니메이션 적용 시 Humanoid 리깅 권장.
**필요한 애니메이션:** 선택: Idle/Walk/Attack/Dodge/Hit/Death. 점프 애니메이션 없음.
**권장 파일 형식:** FBX와 텍스처.
**권장 폴리곤 수:** 약 10,000~30,000 triangles.
**Unity 적용 위치:** Assets/Game/Models, 향후 Player Prefab의 Visual 자식.
**필요 시점:** Phase 4 이후 여유가 있을 때.
**임시 오브젝트 사용 가능 여부:** 가능. 초기 Capsule을 유지하여 기능 개발을 진행한다.

별도 무기 모델/특수 VFX 이미지/추가 적 종류는 현재 요청하지 않는다. 간단한 타격 표현과 색상 변화, 기본 UI는 Unity 기능으로 먼저 구현한다.

## 다음 단계 제안 — 사용자 확인 후 Phase 1

1. 기존 SampleScene의 미저장 Cube를 보존하는 저장 방식부터 정리하고, 개발 씬만 실행되는 테스트 환경을 준비.
2. 개발 씬에 Capsule Player와 CharacterController, 평면 바닥/벽 등 최소 충돌 테스트 환경 구성.
3. 별도 Input System 입력으로 WASD 이동, 마우스 시점, Esc 커서 해제, Space 회피 구현.
4. 카메라 기준 이동 및 이동 방향 회전, 접지용 중력 구현. 점프 기능 없음.
5. Cinemachine이 없으므로 신규 패키지를 설치하지 않고 간단한 3인칭 추적/회전 카메라 작성.
6. 이동·카메라·회피를 독립적인 소수의 스크립트로 구성. 회피 기본값: 거리 3m, 시간 0.3초, 쿨다운 0.8초.
7. 이동/카메라/회피/벽 충돌/쿨다운을 실제 Play Mode에서 확인하고 컴파일/Console 오류 점검.
8. Phase 1 결과를 기록·보고한 후 Phase 2 승인 대기. 무적/데미지/적 AI/전투는 이번 Phase 0에서 만들지 않았다.

2일 진행 제안(예정): 1일차 Phase 0~2 중심, 2일차 Phase 3의 완주/재시작을 먼저 확보한 뒤 Phase 4 모델·연출과 Phase 5 테스트/빌드/포트폴리오. 각 Phase의 사용자 확인 조건은 유지한다.

포트폴리오용으로 Phase 0 종료 시 Unity Hierarchy의 두 씬과 Project의 Game 폴더가 보이는 스크린샷을 사용자에게 요청할 예정이며, 현재 촬영/수령했다고 기록하지 않는다.

## 설치 패키지 스냅샷

manage_packages 결과 47개. 아래는 당시 설치 상태이며 신규 설치 내역이 아니다.

| 패키지 | 버전 | 소스 |
|---|---|---|
| com.coplaydev.unity-mcp | 10.3.0 | Git |
| com.unity.ai.navigation | 2.0.14 | Registry |
| com.unity.collab-proxy | 2.13.6 | Registry |
| com.unity.ide.rider | 3.0.40 | Registry |
| com.unity.ide.visualstudio | 2.0.26 | Registry |
| com.unity.inputsystem | 1.20.0 | Registry |
| com.unity.multiplayer.center | 1.0.1 | BuiltIn |
| com.unity.render-pipelines.universal | 17.3.0 | BuiltIn |
| com.unity.test-framework | 1.6.0 | BuiltIn |
| com.unity.timeline | 1.8.13 | Registry |
| com.unity.ugui | 2.0.0 | BuiltIn |
| com.unity.visualscripting | 1.9.12 | Registry |
| com.unity.modules.accessibility | 1.0.0 | BuiltIn |
| com.unity.modules.adaptiveperformance | 1.0.0 | BuiltIn |
| com.unity.modules.ai | 1.0.0 | BuiltIn |
| com.unity.modules.androidjni | 1.0.0 | BuiltIn |
| com.unity.modules.animation | 1.0.0 | BuiltIn |
| com.unity.modules.assetbundle | 1.0.0 | BuiltIn |
| com.unity.modules.audio | 1.0.0 | BuiltIn |
| com.unity.modules.cloth | 1.0.0 | BuiltIn |
| com.unity.modules.director | 1.0.0 | BuiltIn |
| com.unity.modules.imageconversion | 1.0.0 | BuiltIn |
| com.unity.modules.imgui | 1.0.0 | BuiltIn |
| com.unity.modules.jsonserialize | 1.0.0 | BuiltIn |
| com.unity.modules.particlesystem | 1.0.0 | BuiltIn |
| com.unity.modules.physics | 1.0.0 | BuiltIn |
| com.unity.modules.physics2d | 1.0.0 | BuiltIn |
| com.unity.modules.screencapture | 1.0.0 | BuiltIn |
| com.unity.modules.terrain | 1.0.0 | BuiltIn |
| com.unity.modules.terrainphysics | 1.0.0 | BuiltIn |
| com.unity.modules.tilemap | 1.0.0 | BuiltIn |
| com.unity.modules.ui | 1.0.0 | BuiltIn |
| com.unity.modules.uielements | 1.0.0 | BuiltIn |
| com.unity.modules.umbra | 1.0.0 | BuiltIn |
| com.unity.modules.unityanalytics | 1.0.0 | BuiltIn |
| com.unity.modules.unitywebrequest | 1.0.0 | BuiltIn |
| com.unity.modules.unitywebrequestassetbundle | 1.0.0 | BuiltIn |
| com.unity.modules.unitywebrequestaudio | 1.0.0 | BuiltIn |
| com.unity.modules.unitywebrequesttexture | 1.0.0 | BuiltIn |
| com.unity.modules.unitywebrequestwww | 1.0.0 | BuiltIn |
| com.unity.modules.vectorgraphics | 1.0.0 | BuiltIn |
| com.unity.modules.vehicles | 1.0.0 | BuiltIn |
| com.unity.modules.video | 1.0.0 | BuiltIn |
| com.unity.modules.vr | 1.0.0 | BuiltIn |
| com.unity.modules.wind | 1.0.0 | BuiltIn |
| com.unity.modules.xr | 1.0.0 | BuiltIn |
| com.hyunsongbong.httpsgithub.comcoplaydevunity-mcp.gitpathmcpforunitymain | 0.1.0 | Embedded |


## 2026-10-21 (Asia/Seoul) — Phase 1: 플레이어 기본 조작 완료

### 승인과 실제 수행 범위

- 사용자: Phase 1 시작 질문에 "ㅇㅇ"로 승인.
- AI: 이동/카메라/회피와 최소 충돌 테스트 공간을 구현하고 실제 Unity Play Mode에서 자동 입력 검증을 수행했다.
- 전투, 무적 판정, 적 AI, 체력, HUD, 승패, 재시작, 모델 적용은 아직 구현하지 않았다.
- 새 패키지를 설치하지 않았다. 새 점프 액션/코드/애니메이션도 없다.

### 시작 상태와 기존 씬 보존

- Phase 1 첫 Editor 상태 조회 시각: 2026-10-21 13:03:41 KST경.
- 이번 단계 시작 시 SampleScene은 isDirty=false, rootCount=3이었다. Main Camera / Directional Light / Global Volume만 있었고 이전 MCP_Test_Cube는 존재하지 않았다.
- Phase 0 종료 당시의 미저장 상태와 달라졌으므로 과거 상태를 복원하거나 Cube를 다시 만들지 않았다.
- SampleScene 파일 마지막 수정 시각은 2026-10-21 13:02:04.720646 KST로, Phase 1 시작 전이다. 변경 주체/이유는 확인하지 못했다.
- clean 상태를 확인한 뒤 manage_scene(close_scene)으로 SampleScene을 Editor에서 닫았다. 파일 삭제/저장/덮어쓰기는 하지 않았다.
- 최종 파일 비교: Phase 0 기준 기존 100개 중 99개 SHA-256 동일. SampleScene만 위의 단계 시작 전 변경으로 다름.
- 현재 SampleScene SHA-256: 99B46968404725760412E3445401B15E30417543291337FF3E5EA0B744CA5759.
- 개발 씬만 열어 카메라와 AudioListener가 각 1개인 상태에서 테스트했다.

### 완성한 기능

- CharacterController 기반 WASD 이동, 카메라 yaw 기준 이동, 이동 방향으로 캐릭터 회전.
- 대각선 입력 정규화, 입력 해제 즉시 수평 이동 정지.
- 아래 방향 중력과 접지 유지. 점프 없음.
- 마우스 3인칭 회전/추적, pitch -20~65도 제한, Inspector 감도/거리 설정.
- 카메라 SphereCastNonAlloc 충돌 회피. 자기 플레이어의 Collider는 필터링하고 벽 앞에서 카메라 거리를 줄인다.
- Space 회피: 방향 입력이 있으면 입력 방향, 없으면 캐릭터 전방.
- 회피 거리 3m / 시간 0.3초 / **회피 종료 뒤 회복 쿨다운 0.8초**. 기본 시작 간 최소 간격은 약 1.1초.
- 회피 중 방향 변경/일반 이동/재발동 제한. 마지막 프레임 변위를 남은 회피 시간으로 제한한다.
- 모든 이동과 회피를 동일 CharacterController.Move 경로로 처리하여 벽/장애물 충돌을 유지한다.
- Esc: 커서 해제 및 조작 중단. Game 화면 왼쪽 클릭: 커서 재잠금. 포커스 상실 시 커서를 해제한다.
- 외형은 Visual_Capsule_Replaceable 자식으로 분리하여 이후 모델 교체 시 컨트롤러를 유지할 수 있다.

### 생성/수정 파일

런타임 스크립트 4개(신규):
- Assets/Game/Scripts/Player/LabPlayerInput.cs — 별도 InputActionAsset 인스턴스, 입력 읽기, 커서/포커스 처리.
- Assets/Game/Scripts/Player/LabPlayerMotor.cs — 이동, 회전, 접지, CharacterController 충돌.
- Assets/Game/Scripts/Player/LabPlayerDodge.cs — 회피 방향/거리/지속시간/쿨다운.
- Assets/Game/Scripts/Player/LabThirdPersonCamera.cs — 3인칭 카메라, 마우스 회전, pitch 제한, 장애물 충돌.

기타 신규:
- Assets/Game/Input/LabControls.inputactions — Gameplay 맵: Move / Look / Dodge / ReleaseCursor / CaptureCursor.
- Assets/Game/Materials/Lab_Floor.mat
- Assets/Game/Materials/Lab_Iron.mat
- Assets/Game/Materials/Lab_Brass.mat
- Assets/Game/Materials/Lab_PlayerTeal.mat
- Assets/Game/Materials/Lab_Marking.mat
- Assets/Game/Tests/Phase1PlayModeChecks.cs — UNITY_EDITOR 조건부 검증 코드. 실제 Play Mode에서 가상 Keyboard/Mouse를 생성해 입력을 전달한다. 씬에는 저장하지 않고 빌드 코드에서 제외된다.
- Phase1TestResults.json — 26개 자동 검증 결과.
- Captures/Phase1/Phase1_PlayerControls.png — 실제 Play Mode 카메라 캡처, 1280x720.
- 위 Asset의 .meta 및 Tests 폴더 메타.

수정:
- Assets/Game/Scenes/MadScientistLab_Prototype.unity — Phase 0에서 만든 개발 씬에 Phase 1 구성 추가.
- DevelopmentLog.md — 현재 범위 갱신 및 이 기록 추가.

기존 Readme 스크립트, 기존 InputSystem_Actions.inputactions, 패키지/빌드/플레이어/렌더 설정은 수정하지 않았다.

### 생성한 Unity 오브젝트

- Player: CharacterController + LabPlayerInput + LabPlayerDodge + LabPlayerMotor.
- Player/Visual_Capsule_Replaceable: Capsule Mesh, Collider 제거.
- Player/ForwardIndicator: 캐릭터 전방 표시용 작은 Cube, Collider 없음.
- Phase1_TestArena/Ground: 24m x 24m 바닥.
- Wall_North / Wall_South / Wall_East / Wall_West: 높이 3.5m 외곽 벽.
- Dodge_Blocker, Pillar_A, Pillar_B: 충돌/회피 검증용 장애물.
- FloorMarkings 아래 22개 비충돌 격자 표식.
- 기존 개발 씬 Main Camera에 LabThirdPersonCamera 추가 및 Player 참조 연결.
- 개발 씬 Directional Light의 색/강도/그림자 설정.
- 자동 검증 중에만 Phase1_Verification_RuntimeOnly 생성. 검증 후 자동 제거됨.

### 실제 테스트 결과

검증 시각: 2026-10-21 13:14:16 KST. **26 passed / 0 failed**.
검증 방식: 실제 Editor Play Mode + Input System 가상 Keyboard/Mouse 이벤트 + 런타임 위치/상태 확인. 테스트 중의 기기 제한과 백그라운드 설정은 종료 시 복원했다.

| 검증 | 결과 |
|---|---|
| 개발 씬/카메라/AudioListener 각 1개 | 통과 |
| 새 입력 에셋 Jump 액션 없음 | 통과 |
| 무입력 접지 | root y=0.030m, 수평 이동 없음 |
| W 이동 속도 | 0.602초에 2.708m (설정 4.5m/s) |
| 키 해제 시 정지 | 수평 변위 0 |
| S/A/D 이동과 이동 방향 회전 | 통과 |
| 대각선 속도 정규화 | 0.601초에 2.703m |
| 카메라 yaw=90도에서 W | +X 방향 2.259m, Z 변위 0 |
| 마우스 카메라 회전 | 통과 |
| 상하 회전 제한 | 65도 / -20도 |
| Space 회피 시작 | 통과 |
| 무입력 전방 회피 | X 방향 3.000m, 종료 확인 |
| 쿨다운 중 재회피 제한 / 회복 후 허용 | 모두 통과 |
| 방향 회피 중 다른 이동 입력 제한 | 통과 |
| 회피 종료 후 일반 이동 복귀 | 통과 |
| 외곽 벽에 일반 이동/회피 | z=11.070m에서 정지 |
| 장애물 회피 충돌 | z=1.070m에서 정지 |
| 카메라 벽 충돌 | 벽 내부로 통과하지 않고 거리 축소 |
| Esc 조작 해제 / 클릭 재잠금 | 모두 통과 |
| 회피/이동 중 위로 튀지 않음 | 검사 전체 최대 root y=0.044m |

- 신규 런타임/검증 스크립트 컴파일 오류: 0건.
- Scene validate(auto_repair=false): 누락 스크립트 0, 깨진 Prefab 0.
- Play Mode 테스트 후 Console Error/Warning 조회: 모두 0건. Console clear는 실행하지 않았다.
- 캡처를 직접 확인: Capsule, 바닥, 격자, 벽, 장애물이 정상 렌더링됨.
- 테스트를 마친 뒤 Play Mode 종료. 개발 씬 저장 완료(isDirty=false) 상태로 남김.
- 키보드/마우스를 사람이 직접 조작한 사용성 테스트는 아직 받지 않았다.
- 빌드 및 최종 스테이지 테스트는 Phase 5 범위이며 이번에 수행하지 않았다.

### 도구 조회 이슈 및 해결

- 초기 Input System 패키지 소스 조회에 잘못 추정한 캐시 경로를 사용해 파일 없음이 반환됨. rg --files로 실제 경로를 확인한 뒤 설치된 소스/API를 읽었다. 파일 변경 없음.
- manage_asset(get_info)의 Packages 경로가 Assets/Packages로 정규화되어 Shader 조회 실패. Unity execute_code에서 Shader.Find 결과와 AssetDatabase.GetAssetPath로 실제 설치된 URP Lit Shader를 확인한 뒤 머티리얼을 생성했다.
- 런타임/컴파일/자동 검증 실패는 없었고, 기능 코드 수정 재시도 없이 26개 검증이 통과했다.
- Phase 0에 기록된 MCP WebSocket 경고는 이번 단계 최종 조회에서는 나타나지 않았다.

### 사용한 도구

Unity MCP:
- read_mcp_resource — editor/state 확인.
- manage_scene — 로드 씬/활성 씬 조회, 저장된 SampleScene 닫기, 신규 씬 validate.
- unity_reflect — CharacterController.Move, InputAction, EditorSceneManager.SaveScene, Physics SphereCast API 확인.
- manage_asset — 설치 Shader 검색/조회.
- execute_code — 신규 소스/입력 작성, 씬 오브젝트/컴포넌트/머티리얼 구성과 참조 연결, 개발 씬 저장, 런타임 검증 시작, 개발 기록 갱신.
- manage_editor — Play Mode 진입/종료.
- read_console — 컴파일 및 런타임 오류/경고 조회.
- manage_camera — Play Mode 화면 캡처.

보조 로컬 도구: 파일/설치 패키지 소스 읽기, 새 코드의 Jump/구 Input API 검색, 기존 파일 SHA-256 비교, 검증 결과 읽기.

### 사용자 작업과 추가 리소스

- 사용자가 이번 단계 진행을 승인했다.
- AI가 구현과 자동 검증, 화면 캡처를 수행했다.
- 사용자 수동 플레이 결과는 아직 없다. 모델/리깅/텍스처를 새로 제공받지 않았다.
- Phase 2도 임시 적 모델로 진행할 수 있어 지금 추가 외부 리소스는 필요 없다.
- Varco 과학자 및 후속 리소스 요청은 앞의 리소스 제작 요청 항목을 유지한다.

### 직접 실행 / 조작

1. Assets/Game/Scenes/MadScientistLab_Prototype.unity를 열고 Unity Play 버튼.
2. Game 창을 클릭해 입력 포커스와 커서 잠금을 확보.
3. WASD 이동 / 마우스 시점 회전 / Space 회피.
4. Esc로 커서를 풀고, Game 창 왼쪽 클릭으로 다시 조작.
5. Player의 LabPlayerMotor / LabPlayerDodge, Main Camera의 LabThirdPersonCamera에서 수치를 조절할 수 있다.

### 다음 단계 — Phase 2 승인 대기

- 근접 공격 1개와 중복 피해 방지.
- 적 1기의 체력 / Idle-Chase-Attack-Dead FSM.
- 플레이어 피해/사망 처리.
- 중앙 피해 처리에서 회피 무적 구간 검사.
- 적 1기와 전투·처치·플레이어 사망·회피 검증 후 결과 보고.
- Phase 2는 아직 시작하지 않았다.
