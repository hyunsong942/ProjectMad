# 매드 사이언티스트의 연구소 — Development Log

## 개발 기준과 범위

- 개발 기준: 사용자가 2026-10-21에 전달한 「Unity MCP 기반 3D 액션 게임 프로토타입 개발 지시서」.
- 최종 목표: Varco AI 에셋을 활용한 3인칭 스팀펑크 액션 게임 1스테이지, 플레이 시간 약 3~5분, 2일 개발.
- 현재 승인 범위: **Phase 3 완료; Phase 4 플레이어 모델·애니메이션·클릭 3타 콤보 적용 완료**. 각 Phase 종료 후 사용자 확인을 받고 다음 Phase로 진행한다.
- Phase 1 조작, Phase 2 전투, Phase 3 연구소 스테이지/경비 3기/출구/승패/HUD/재시작을 구현했다. Phase 4의 과학자 플레이어/골렘 경비 모델·텍스처를 적용했다. 최신 사용자 정정에 따라 과학자는 직접 조작하는 플레이어이다. 현재 과학자는 V2 외형의 스킨 메시와 제공된 Humanoid 애니메이션을 사용한다. 플레이어 Idle/Walk/Dodge/클릭 3타 콤보를 연결했다. 적 애니메이션과 나머지 연출, Phase 5 플레이 타임 조정 및 빌드는 남아 있다.
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


## 2026-10-21 (Asia/Seoul) — Phase 2: 전투 시스템 완료

### 사용자 요청과 범위

- Phase 2 진행 질문에 사용자가 "해주면 고맙지?"라고 답하여 진행 승인.
- AI가 플레이어 근접 공격, 공통 체력/피해, 적 1기 FSM, 피격/사망, 회피 무적 판정과 실제 Play Mode 검증을 수행했다.
- Phase 3의 적 3기 배치, 스테이지 목표, 출구 잠금/개방, HUD, 게임 오버/클리어 UI, 재시작 버튼은 아직 구현하지 않았다.
- 외부 모델과 새 패키지를 추가하지 않았다. 경비 로봇은 임시 Primitive 모델이다.

### 보호 조치

- 수정 전 Phase 1 런타임 스크립트 4개, 입력 에셋, 개발 씬, DevelopmentLog.md를 DevelopmentBackups/Phase2Start에 7개 파일로 백업했다.
- 백업은 Assets 바깥의 프로젝트 내부에 있어 중복 스크립트로 컴파일되지 않는다.
- 단계 시작 시 Assets/Packages/ProjectSettings의 기존 파일 143개 SHA-256을 기록했다.
- 작업 후 기존 파일 변경은 아래 5개뿐이다: 개발 씬, LabControls.inputactions, LabPlayerInput.cs, LabPlayerDodge.cs, LabPlayerMotor.cs.
- 나머지 기존 138개 파일은 해시가 동일하다. SampleScene, Readme 스크립트, 기존 InputSystem_Actions.inputactions, Packages, ProjectSettings는 이번 단계에서 변경되지 않았다.
- 프로젝트 밖 파일을 수정하지 않았다. 런타임 테스트 배치/수치 변경은 저장하지 않고 Play Mode 종료로 복원했다.

### 완성한 기능 / 설정

| 항목 | 구현 및 초기값 |
|---|---|
| 기본 공격 입력 | 마우스 왼쪽 버튼, 누를 때 1회 |
| 플레이어 공격 | 피해 15, 범위 2.2m, 전방 각도 100도, 쿨다운 0.55초 |
| 중복 피해 방지 | 한 공격의 Collider 목록을 LabHealth 단위 HashSet으로 중복 제거 |
| 벽 너머 공격 차단 | 공격자/대상 Collider를 제외한 시야 Raycast로 장애물 검사 |
| 체력 | 플레이어 100 / 경비 로봇 30 |
| 중앙 피해 처리 | LabHealth.TryTakeDamage에서 사망, 같은 진영/자기 자신, 비정상 피해량, 회피 무적을 검사 |
| 회피 무적 | 시작 후 0.18초, Inspector 수정 가능; 회피 시간 이내로 제한 |
| 회피 이후 피격 | 무적이 끝나면 회피가 아직 진행 중이어도 정상 피격 |
| 플레이어 사망 | 체력 0, 이동/공격/회피/입력/CharacterController 중단, 커서 해제 |
| 적 FSM | Idle / Chase / Attack / Dead |
| 적 이동 | CharacterController, 속도 2.5m/s, 감지 8m, 추적 포기 11m |
| 적 공격 | 범위 1.8m, 각도 120도, 피해 10, 공격 시작 간 쿨다운 1.5초 |
| 공격 예고 | 0.45초 경고 원 표시 후 타격; 예고 시작 방향을 고정하고 타격 시 거리/각도/시야를 재확인 |
| 적 사망 | 이동/공격/진행 중 예고 중단, Collider 비활성화, 어두운 색으로 표시 |
| 사망 통지 | LabHealth.Died 이벤트를 한 번만 발생; Phase 3 스테이지 시스템이 구독 가능 |
| 전투 피드백 | 플레이어 공격 호, 적 공격 예고 원, 피격 색상 변화, 사망 색상 |
| Gizmo | 플레이어 공격 범위/각도, 적 감지/공격 범위 표시 |

- 모든 핵심 수치는 Inspector에서 조절할 수 있다.
- 피해 적용은 공통 LabHealth 한 곳을 통한다. Player/Enemy 진영을 구분한다.
- 입력 업데이트 → 플레이어 이동/회피 → 플레이어 공격 → 적 AI 순서로 실행하여 같은 프레임 회피 입력을 적 타격 전에 처리한다.
- 커서 재잠금을 위한 클릭은 공격으로 소비하지 않는다. 다음 클릭부터 공격한다.
- 적은 평면에서 직선 추적하며 벽과 충돌한다. 장애물 우회/NavMesh 경로 탐색은 이번 범위에 포함하지 않았다.
- 초기 적 위치는 (0, 0.05, 4), 플레이어는 (0, 0.05, -6). 시작 거리가 감지 범위 밖이므로 접근하면 전투가 시작된다.
- 회피 이동 3m / 0.3초 / 종료 후 회복 0.8초는 Phase 1 값을 유지했다. 점프는 없다.

### 생성/수정 스크립트

신규 런타임 스크립트 5개:
- Assets/Game/Scripts/Combat/LabHealth.cs — 진영, 최대/현재 체력, 피해/무적 검사, 피해/사망 이벤트.
- Assets/Game/Scripts/Combat/LabMeleeQuery.cs — 범위/각도/시야 검사 공통 유틸리티.
- Assets/Game/Scripts/Player/LabPlayerCombat.cs — 공격 입력 처리, 중복 피해 방지, 공격 쿨다운, 플레이어 사망 시 조작 정리.
- Assets/Game/Scripts/Enemy/LabEnemyGuard.cs — 경비 로봇 FSM, 추적, 예고/공격, 사망 정리.
- Assets/Game/Scripts/Combat/LabCombatFeedback.cs — MaterialPropertyBlock 피격/사망 색상, 공격 호/예고 표시.

기존 개발 스크립트 수정:
- LabPlayerInput.cs — Attack 액션 읽기/초기화 추가.
- LabPlayerDodge.cs — 무적 시간과 IsInvulnerable 추가, 비활성 상태에서 회피 시작 방지.
- LabPlayerMotor.cs — 실행 순서 -100 지정. 이동/접지 알고리즘은 유지.

신규 검증 스크립트:
- Assets/Game/Tests/Phase2PlayModeChecks.cs — UNITY_EDITOR 조건부 실제 Play Mode 자동 입력/검증. 씬에 저장하지 않고 빌드에서 제외.
- Phase 1 테스트 소스 및 LabThirdPersonCamera.cs는 변경하지 않았다.

### 기타 파일과 오브젝트

- 수정: Assets/Game/Input/LabControls.inputactions — Attack 1개와 왼쪽 버튼 바인딩 추가.
- 수정: Assets/Game/Scenes/MadScientistLab_Prototype.unity — 아래 전투 오브젝트/컴포넌트 추가 및 저장.
- 신규: Assets/Game/Prefabs/Enemies/GuardRobot.prefab — 후속 스테이지에서 재사용 가능한 적 프리팹.
- 신규 머티리얼: Lab_GuardCopper, Lab_GuardEyes, Lab_AttackWarning, Lab_Swing.
- Player에 LabHealth, LabCombatFeedback, LabPlayerCombat 추가.
- Player/AttackArc: 공격 때만 나타나는 LineRenderer.
- GuardRobot_01: CharacterController, LabHealth, LabCombatFeedback, LabEnemyGuard.
- GuardRobot_01/Visual_Robot_Replaceable: 몸통/머리/팔/다리/눈의 Primitive 조합. 이후 모델 교체 가능.
- GuardRobot_01/AttackTelegraph: 공격 예고 원 LineRenderer.
- 적 프리팹은 씬 대상 참조 없이 저장한 뒤, 씬 인스턴스의 target에 Player를 연결했다. 복제 시 대상이 비어 있으면 Start에서 플레이어를 찾는다.
- 신규 Phase2TestResults.json: 최종 38개 검증 결과.
- 신규 Captures/Phase2/Phase2_CombatPrototype.png: 실제 Play Mode 화면 1280x720.
- 첫 37개 검증 기록도 DevelopmentBackups/Phase2Start/Phase2TestResults_FirstRun.json에 보존했다.
- 자동 검증용 오브젝트와 가상 입력 장치는 종료 시 제거했다.

### 실제 테스트 결과

최종 결과: **38 passed / 0 failed**, complete=true.
기록 시각: 2026-10-21T13:28:53.1681755+09:00.

실제 Editor Play Mode에서 가상 Keyboard/Mouse 입력을 Input System으로 전달했다.
피해 판정 경계 테스트 일부는 actor 배치, 체력, AI 활성 상태를 테스트에서 직접 준비했다.
플레이어 사망 처리 단독 검증에서는 적 피해를 일시적으로 100으로 올려 시간을 줄였다.
**별도의 최종 통합 검증은 모든 기본 전투 수치를 유지한 상태**에서 플레이어 두 번 공격과 적 한 번 공격을 주고받아 적 사망/플레이어 HP 90을 확인했다.
이러한 테스트 전용 상태 변경은 Play Mode 종료 후 저장되지 않았다.

| 검증 그룹 | 실제 확인 |
|---|---|
| 입력 | 왼쪽 클릭 공격, Space 회피, 새 입력에 Jump 없음 |
| 플레이어 공격 | 피해 15, 쿨다운, 두 번 타격으로 HP 30 적 처치 |
| 중복 판정 | 적 루트 CharacterController + 자식 Trigger가 동시에 검출되어도 15만 피해 |
| 공격 제약 | 후방/거리 밖/벽 너머 대상 제외, 회피 중 공격 금지 |
| 커서 | 재잠금 클릭은 공격하지 않고 다음 클릭부터 공격 |
| 중앙 피해 | 음수/NaN/무한대, 자기 자신/같은 진영, 죽은 공격자의 피해 거부 |
| 무적 | 시작 직후 피해 거부, 0.18초 이후에는 회피 중에도 피해 허용 |
| 적 AI | 감지 밖 Idle, 감지 후 Chase, 약 0.4초에 1.003m 추적, 멀어지면 Idle |
| 공격 예고 | 타격 전 원 표시, 타격 시 피해 10, 재공격 쿨다운 |
| 거리 회피 | 회피로 적 사거리 밖으로 이동해 피해 회피 |
| 무적 회피 | 벽에 막혀 이동 거리가 줄어도 적 사거리 안에서 무적 시간으로 실제 적 타격 차단 |
| 적 사망 | 이벤트 1회, 진행 중 공격 취소, Collider/이동/피해 중단 |
| 플레이어 사망 | 실제 AI 타격으로 HP 0, 이동/공격/회피/입력/Controller 중단 |
| 사망 중 회피 | 무적이 끝난 회피 도중 사망하면 회피 즉시 취소 |
| Phase 1 회귀 | 이동, 3m 회피, 마우스 카메라 회전 정상 |
| 기본 수치 통합 전투 | 플레이어 15 피해 공격 2회로 적 처치, 적 10 피해 공격 1회 후 플레이어 HP 90 |

- 초기 37개 검증 모두 통과. 기본 수치 통합 전투 검증 1개를 추가하여 최종 38개 모두 통과했다.
- 신규 코드 컴파일 오류 없음.
- Console 최종 조회: Error 0, Warning 0. Console clear는 실행하지 않았다.
- Scene validate: missingScripts=0, brokenPrefabs=0.
- 캡처 직접 확인: 플레이어와 임시 경비 로봇, 테스트 환경 정상 표시.
- 최종 상태: Play Mode 종료, 개발 씬 저장됨(isDirty=false), 루트 오브젝트 5개.
- 사용자 직접 조작 감각 검증, 배포 빌드 테스트는 아직 수행하지 않았다.

### 오류와 해결

- 이번 Phase 2에서 도구 실행/컴파일/런타임/자동 검증 오류는 발생하지 않았다.
- 실패한 테스트를 숨기거나 Console을 지우지 않았다.
- 피해 판정과 기본 수치 전투는 자동 검증으로 확인했지만, 전체 스테이지/승패 UI가 완성되었다고 기록하지 않는다.

### 사용한 Unity MCP 도구

- read_mcp_resource: Editor 상태 확인.
- unity_reflect: Physics.OverlapSphere/RaycastNonAlloc, LineRenderer, PrefabUtility API 확인.
- execute_code: 프로젝트 내부 백업, 신규 코드 및 기존 개발 코드 수정, 입력 액션 추가, 컴포넌트/씬/머티리얼/프리팹 구성, 검증 실행, 기록 갱신.
- manage_scene: 개발 씬 검사/상태 조회.
- manage_editor: Play Mode 진입/종료.
- read_console: 오류/경고 확인.
- manage_camera: 실제 Play Mode 화면 캡처.
- 보조 로컬 읽기: 현재 소스/입력 확인, 기존 파일 해시 비교, 결과 JSON 읽기.

### 사용자 작업 / 추가 리소스

- 사용자: Phase 2 진행 승인.
- AI: 구현, 프리팹/임시 모델 구성, 실제 Play Mode 자동 입력 검증, 캡처, 개발 기록 작성.
- 사용자의 새 모델 제공이나 수동 플레이 완료는 이번 단계에서 보고받지 않았다.
- Phase 3는 기존 Primitive와 경비 로봇 프리팹으로 진행할 수 있다. 지금 추가 외부 리소스는 필요 없다.
- Phase 4 전에 Varco 과학자 원본 모델/텍스처를 제공받아야 하며, 기존 리소스 제작 요청의 규격을 유지한다.

### 직접 플레이

1. MadScientistLab_Prototype 씬을 열고 Play 후 Game 창 클릭.
2. WASD 이동 / 마우스 시점 / 왼쪽 클릭 공격 / Space 회피 / Esc 커서 해제.
3. 앞쪽 경비 로봇에 접근하면 추적과 공격이 시작된다.
4. 공격 예고 원을 보고 타이밍에 맞춰 회피하거나 사거리 밖으로 이동한다.
5. 플레이어 또는 적이 죽은 뒤 다시 시험하려면 Unity Play를 종료하고 재실행한다.
6. 현재 게임 오버/재시작 UI는 없으며 Phase 3에서 구현한다. 체력과 상태는 Play Mode Inspector에서 확인할 수 있다.

### 다음 단계 — Phase 3 승인 대기

- 연구소 1스테이지의 시작/전투/연구/탈출 구역 구성.
- 경비 로봇 3기 배치 및 사망 이벤트 기반 남은 적 수 관리.
- 출구 잠금, 전원 처치 후 개방, 출구 진입 시 클리어.
- 플레이어 체력/남은 적/현재 목표 HUD.
- 게임 오버/클리어 메시지 및 재시작.
- 시작부터 승리, 패배 후 재시작까지 검증 후 보고.
- Phase 3는 아직 시작하지 않았다.


## 2026-10-21 (Asia/Seoul) — Phase 3: 스테이지 구성

### 사용자 요청과 작업 범위

- 사용자가 Phase 3 진행을 승인했다.
- 연구소 1스테이지, 경비 로봇 3기, 출구 잠금/개방, 승리/패배, 기본 HUD와 재시작을 구현했다.
- Phase 4 모델/연출 적용과 Phase 5 빌드는 이번 승인 범위에 포함하지 않는다.
- 변경 전에 Phase 2의 개발 씬, Build Settings, 개발 기록을 DevelopmentBackups/Phase3Start에 백업했다.
- 기존 개발 씬과 전투 코드 분석 후 작업했다. 모든 프로젝트 쓰기와 씬 조작은 Unity MCP execute_code로 수행했다. 로컬 셸은 소스/결과 읽기와 해시 비교에만 사용했다.

### 완성한 기능

| 기능 | 구현 |
|---|---|
| 스테이지 | 시작 구역 → 중앙 전투 공간 → 연구 공간 → 북쪽 탈출 구역 |
| 경비 로봇 | GuardRobot 프리팹 인스턴스 3기; 기존 기본 전투 수치 유지 |
| 진행 상태 | Running / Cleared / GameOver |
| 적 수 관리 | LabHealth.Died 구독, HashSet으로 중복 사망 집계 방지 |
| 출구 잠금 | 시작 시 문 Collider 활성화, 빨간 상태등 |
| 출구 개방 | 적 0기일 때 Collider 해제, 문 위로 이동, 고정된 문틀 상태등 초록색 |
| 승리 | 전원 처치 후 살아 있는 플레이어가 출구 Trigger에 들어가면 클리어 |
| 패배 | 플레이어 HP 0에서 GameOver, 조작과 모든 적 AI 중단 |
| 종료 화면 | 한국어 클리어/게임 오버 메시지, 커서 해제, 다시 시작 버튼 |
| 재시작 | 개발 씬을 비동기 재로드하여 체력/적/문/입력/이벤트 구독 초기화 |
| HUD | 체력 숫자와 막대, 남은 경비 수, 현재 목표, 출구 상태, 조작 안내 |
| 종료 상태 보호 | 승리 후 추가 피해 거부; 패배 후 출구 진입으로 승리 전환 불가 |

- 점프를 추가하지 않았다. Phase 1 이동/카메라/회피와 Phase 2 전투 런타임 코드는 변경하지 않았다.
- 경비 로봇은 기존 직선 추적 방식이다. 중앙 통로를 넓게 두고 큰 기계/연구 소품은 주로 가장자리에 배치하여 필수 경로의 우회 탐색이 필요하지 않도록 구성했다.
- 전체 외곽 바닥과 벽을 유지했다. 출구는 북쪽 외벽 안쪽에 있어 밖으로 떨어지지 않는다.
- UI는 설치된 uGUI + InputSystemUIInputModule을 사용한다. 패키지를 추가하지 않았다.
- 한국어 UI는 Windows의 Malgun Gothic 시스템 폰트를 동적으로 사용한다. 폰트 파일을 복사/배포하지 않았다. 다른 OS 배포 시 한국어 폰트 제공 방식을 별도로 확인해야 한다.
- 공유 동적 폰트 1개를 씬 재시작 동안 유지하며, 플레이 종료 시 해제한다. 새 결과 메시지 때문에 font atlas가 재구축되면 다음 프레임에 활성 Text의 글자 메시를 갱신한다.

### 생성/수정 스크립트

신규 런타임 3개:
- Assets/Game/Scripts/Managers/LabStageController.cs — 사망 이벤트, 적 수, 승패, 종료 시 조작 정리, 재시작.
- Assets/Game/Scripts/Managers/LabExitDoor.cs — 문 Collider/상태등/상승 연출, 출구 Trigger.
- Assets/Game/Scripts/UI/LabStageHUD.cs — 한국어 HUD와 결과 화면, 버튼 연결, 공유 폰트 수명 관리.

신규 검증:
- Assets/Game/Tests/Phase3PlayModeChecks.cs — UNITY_EDITOR 조건부 실제 Play Mode 입력/버튼/재시작 검증.
- 테스트 실행 오브젝트는 씬에 저장하지 않았다. 테스트 코드와 가상 입력 장치는 빌드 게임에 포함되지 않는다.

### 씬 / 오브젝트 / 파일

- 수정: Assets/Game/Scenes/MadScientistLab_Prototype.unity.
- 수정: ProjectSettings/EditorBuildSettings.asset — 재시작용 개발 씬을 enabled 상태로 추가. 기존 SampleScene 항목과 순서는 유지했다. 실제 제출 빌드 시작 씬 선정은 Phase 5에서 처리한다.
- 기존 Phase1_TestArena를 Laboratory로 정리했다. 바닥/외곽벽/그리드를 유지하고 기둥/장애물 위치를 가장자리로 조정했다.
- 플레이어 시작 위치: (0, 0.05, -8).
- GuardRobot_01/02/03: (-4, 0.05, 1), (4, 0.05, 3), (0, 0.05, 7). 각 인스턴스는 기존 GuardRobot.prefab에 연결되어 있다.
- StageZones: StartZone, ResearchArea_PrimitivePlaceholder, MachineArea_Primitives.
- 연구 벤치/콘솔/기계 장치/배관/EXIT 및 RESEARCH 표지판을 Primitive와 기존 머티리얼로 구성했다.
- VarcoScientist_PlacementMarker는 모델을 배치할 자리 표시와 받침대뿐이다. 과학자 모델/NPC 완성을 의미하지 않는다.
- StageManager, ExitZone, LockedDoorPanel, 문틀, 출구 통로벽, 고정 상태등.
- StageHUD: HealthCard, ObjectiveCard, ControlsCard, EndPanel, ResultCard, RestartButton.
- EventSystem + InputSystemUIInputModule.
- 개발 씬 루트 11개.
- 신규 Phase3TestResults.json 및 Captures/Phase3/Phase3_InitialHUD-1.png(최종 시작 화면), Phase3_UnlockedHUD.png, Phase3_ClearScreen.png, Phase3_GameOverScreen.png.
- 백업과 첫 실패/중단 테스트 기록은 DevelopmentBackups/Phase3Start에 보존했다.

### 실제 검증 결과

최종 결과: **39 passed / 0 failed**, complete=true.
기록 시각: 2026-10-21T14:23:39.0478234+09:00.

- 적 수 3 → 2 → 1 → 0, 기본 공격 6회 후 플레이어 HP 70, 문 개방과 실제 출구 진입 클리어를 확인했다.
- 기본 적 공격 10회로 HP 0과 게임 오버를 확인했다.
- 최종 Play Mode Console: Error 0 / Warning 0.
- Play Mode 종료 후 최종 Console: Error 0 / MCP WebSocket 경고 1건. 게임 코드 오류는 없고 후속 MCP 조회/실행은 정상이다.
- Scene validate: missingScripts=0, brokenPrefabs=0, totalIssues=0.
- 최종 Edit Mode, 개발 씬 저장됨(isDirty=false), 루트 11개.

- 테스트는 실제 Editor Play Mode에서 가상 Keyboard/Mouse 상태를 Input System에 전달했다.
- 기본 수치 통합 전투: 시작 위치에서 WASD/왼쪽 버튼 입력만으로 적 3기를 처치하고, 출구까지 이동하여 클리어했다. 적 HP 30/공격 10/속도 2.5, 플레이어 공격 15를 유지했다.
- 문 잠금 경계 검사에는 테스트에서 플레이어 위치를 직접 배치했다. 동일 프레임 마지막 적 처치/플레이어 사망 검사는 중앙 피해 API로 상태를 준비했다. 이러한 테스트 전용 변경은 저장하지 않았다.
- 재시작은 Button.onClick 직접 호출이 아니라 실제 가상 마우스 좌표/버튼 입력 → InputSystemUIInputModule → uGUI 버튼 경로로 검증했다.
- 클리어 후 1회, 일반 패배 후 1회, 경계 조건 패배 후 1회로 총 3회 재로드하고 초기 상태와 폰트 재사용을 확인했다.
- 테스트 중에만 백그라운드 실행/입력 라우팅을 허용하고, 종료 시 원래 설정과 장치 상태를 정리했다.
- 실제 캡처에서 시작 HUD, HP 감소/초록 출구 표시, 클리어, 게임 오버 및 재시작 후 글자 표시를 확인했다.
- 초기 165개 Assets/Packages/ProjectSettings 파일과 최종 173개를 SHA-256으로 비교했다. 기존 파일 변경은 개발 씬과 EditorBuildSettings.asset 두 개뿐이며, 신규 파일 8개(C# 4개와 meta 4개), 삭제 0개였다.
- 기존 SampleScene, 이전 단계 런타임/테스트 소스, GuardRobot 프리팹, 입력 에셋, 패키지는 변경하지 않았다.
- 사용자 수동 조작 감각 평가와 배포 빌드는 수행하지 않았다.
- 자동 입력의 전투 완료 시간은 수 초 수준이다. 목표 3~5분 플레이 타임의 난이도/동선 조정은 아직 완료하지 않았으며 후속 검증에서 다룬다.

### 발생한 오류와 해결

1. 씬 구성용 MCP C# 코드에서 Object의 System.Object/UnityEngine.Object 이름 충돌로 실행 전 컴파일 실패. UnityEngine.Object로 명시한 뒤 재실행하여 해결했다. 이 실패에서는 씬 변경이 실행되지 않았다.
2. Play Mode 전환 직후 execute_code 1회가 success=false(상세 메시지 없음)를 반환했다. MCP로 상태와 테스트 오브젝트 부재를 확인한 뒤 같은 MCP 도구로 다시 실행했다. 파일 직접 수정 우회는 하지 않았다.
3. MCP 패키지 WebSocket is not initialised 경고가 일시적으로 조회됐다. 후속 상태 조회/실행은 정상 동작했고 최종 Console 검사를 별도로 수행했다. Console clear 도구는 사용하지 않았다.
4. 첫 자동 검증은 25 통과/10 실패. 에디터가 Game 창 포커스를 잃으면 가상 입력도 게임 액션에 전달되지 않아 탈출/버튼 검증이 연쇄 실패했다. 첫 보완 후 두 번째 검증도 입력 라우팅 문제로 중단했다.
5. 설치된 Input System 소스에서 editorInputBehaviorInPlayMode를 확인하고, 검증 중에만 AllDeviceInputAlwaysGoesToGameView를 적용하도록 수정했다. 플레이어 입력 포커스도 검증 실행 중에만 유지했다. 검증 종료 시 기존 설정을 복구한다. 이후 35개 검증이 모두 통과했다.
6. 캡처 검수에서 재시작 뒤 일부 HUD 글자가 빠지는 현상을 발견했다. LabStageHUD가 씬마다 동적 폰트를 생성/제거하던 방식에서 플레이 세션 동안 공유 폰트를 유지하도록 바꾸었다. 공유만으로는 결과 메시지가 추가될 때 기존 글자의 일부가 빠지는 현상이 남았다. 실제 화면에서 전체 Text의 FontTextureChanged 호출로 회복되는 것을 확인하고, Font.textureRebuilt/상태 갱신 후 다음 프레임에 활성 Text 메시를 갱신하도록 보완했다. 최종 자동 검증 39개 통과와 1920×1080 게임 오버/출구 개방 캡처에서 글자 전체 표시를 확인했다.
7. 출구 상태등이 문과 함께 위로 이동해 개방 후 보이지 않을 수 있어, 문틀에 고정하고 초록 표시 유지 검증을 추가했다.
8. 읽기용 MCP 진단 코드에서 InputControl.ReadValueAsObject 확장 메서드를 직접 찾지 못하는 실행 전 컴파일 실패가 1회 있었다. 제어 경로/액션 상태 조회로 바꾸어 확인했다. 게임 소스 오류는 아니었다.
9. 에디터 비활성 상태에서는 테스트 Start가 호출되기 전 플레이 루프가 멈출 수 있었다. 한 차례 MCP로 백그라운드 실행을 켠 뒤 원래 값으로 복구해 검증했고, 최종 테스트 소스는 Awake에서 기존 값을 저장하고 테스트 환경을 준비하도록 수정했다. 마지막 검증 후 runInBackground=false, backgroundBehavior=ResetAndDisableNonBackgroundDevices, editorInputBehavior=PointersAndKeyboardsRespectGameViewFocus, 가상 장치 0개로 복구된 것을 확인했다.

### 사용한 Unity MCP 도구

- read_mcp_resource: Editor 상태 확인.
- unity_reflect: 동적 폰트, UI Input Module, Image, 비동기 씬 로드 API 확인.
- execute_code: 프로젝트 내부 백업, C# 작성/수정, 씬/오브젝트/컴포넌트/UI 구성, Build Settings 등록, Play Mode 검증 실행, 기록 갱신.
- manage_scene: 씬 구조 검증과 최종 상태 확인.
- manage_editor: Play Mode 시작/종료.
- read_console: 컴파일/런타임 오류 및 경고 확인.
- manage_camera: 실제 Game View/HUD 캡처.
- 테스트 캡처는 MCP로 시작한 검증 스크립트의 ScreenCapture도 사용했다.
- 보조 도구: 로컬 읽기/해시 비교, 저장된 캡처 이미지 직접 확인.

### 사용자 작업 / 추가 리소스 / 다음 단계

- 사용자: Phase 3 진행 승인.
- AI: 스테이지/승패/HUD/재시작 구현, 자동 플레이 검증, 오류 수정, 화면 검수, 개발 기록.
- 사용자의 모델 전달 또는 수동 플레이 완료는 이번 단계에서 보고받지 않았다.
- Phase 3 기능에는 추가 외부 리소스가 필요하지 않았다.
- Phase 4 전 필수: 사용자가 제작한 Varco 과학자 모델(FBX/GLB 등)과 텍스처의 실제 파일 위치. 리깅 없는 정적 NPC로 사용한다.
- 로봇/환경 모델은 준비되는 대로 임시 시각 오브젝트를 교체할 수 있다. Primitive를 최종 제작 에셋으로 간주하지 않는다.
- 다음 단계는 Phase 4: 모델 적용, 머티리얼/조명/이펙트/UI 개선. 사용자 확인 전에는 진행하지 않는다.
- 이후 Phase 5에서 난이도/3~5분 플레이 타임, 실제 빌드, 시작 씬/종료, 제출용 기록을 검증한다.

### 직접 플레이

1. Assets/Game/Scenes/MadScientistLab_Prototype.unity를 열고 Unity Play 후 Game 창 클릭.
2. WASD 이동 / 마우스 시점 / 왼쪽 클릭 공격 / Space 회피 / Esc 커서 해제.
3. 경비 로봇 3기를 처치하면 북쪽 EXIT 상태등이 초록색으로 변하고 문이 열린다.
4. 출구 안으로 이동하면 클리어 화면이 표시된다.
5. 사망하면 게임 오버 화면이 표시되며, 두 결과 화면 모두 다시 시작 버튼으로 즉시 재도전할 수 있다.


## 2026-10-21 (Asia/Seoul) — Phase 4: 사용자 모델·텍스처 적용

### 사용자 요청 / 이번 범위

- 사용자가 애니메이션은 나중에 하기로 결정하고, 다음 FBX 두 개를 제공했다.
  - C:/Users/mbc/Downloads/Steampunk Gentleman Character-577950f548ab60f665d31aaf8728c152 (1).fbx
  - C:/Users/mbc/Downloads/Steampunk Bronze Golem-a051fa710a8284ff997f805d486d94d3.fbx
- 앞서 논의한 대로 Gentleman은 과학자 NPC, Bronze Golem은 경비 로봇 외형으로 적용했다.
- 이번 작업은 Phase 4의 모델/텍스처 적용 부분이다. 추가 조명·환경 모델·UI 연출과 Phase 5 빌드는 진행하지 않았다.
- 애니메이션 제작/적용/Animator Controller 추가는 하지 않았다. 두 모델은 원본 자세로 표시되며, 경비 이동은 기존 위치 이동을 사용한다.
- 플레이어 외형은 기존 Capsule이다. 이 단계에서 별도 플레이어 모델은 제공받지 않았다.

### 백업과 원본 보존

- 적용 전에 DevelopmentBackups/Phase4ModelsStart에 개발 씬, GuardRobot.prefab, DevelopmentLog.md를 백업했다.
- 모델 원본을 import_model_file로 Assets/Game/Models/Varco 아래에 복사해 가져왔다. Downloads 원본을 수정하지 않았다.
- 원본과 프로젝트 FBX 복사본의 SHA-256이 각각 일치한다.
  - Gentleman: 155AABB431DAD301A851A471DE90FA5FD4ACA9135273C78C3EE843C8B328EB22
  - Bronze Golem: 555F987C08E7103A4653AD89CE102574F018140BB58C2FC4766BDD637EB0749E
- Assets/Packages/ProjectSettings 173개 → 200개 파일 비교: 기존 변경은 개발 씬과 GuardRobot.prefab 두 개뿐, 신규 27개, 삭제 0개.
- 런타임/테스트 C# 소스, 기존 입력, 원본 SampleScene, 패키지와 프로젝트 설정은 변경하지 않았다.
- 경비 로봇의 기존 Primitive 외형은 프리팹 내부에서 비활성화하여 보존했다.

### 모델과 텍스처 확인 / 적용

- 두 FBX 모두 base_color.png와 normal.png를 내장하고 있었다. ModelImporter.ExtractTextures로 각 모델의 Textures 폴더에 추출했다.
- 별도 텍스처를 사용자에게 요청할 필요가 없었다.
- 색상 텍스처는 sRGB, normal.png는 Normal Map 유형으로 설정했다.
- 모델별 URP/Lit 머티리얼을 만들고 Base Map/Normal Map을 연결했다. 텍스처는 Unity 로드 기준 각각 2048×2048이다.
- Gentleman: Mesh 1개, 30,264 vertices / 40,000 triangles. 원본에 mixamorig 이름의 뼈대 Transform들이 있으나 이번 임포트는 Rig=None, Import Animation=false이며 움직이는 리그를 구성하지 않았다.
- Golem: Mesh 1개, 40,330 vertices / 40,000 triangles.
- 각 모델 높이를 약 1.8m로 맞췄다. Gentleman의 누운 축을 X=-90도로 보정했고, Golem의 FBX 기본 축 보정을 유지했다.
- 바닥 기준으로 위치를 맞췄으며, Golem은 중앙 발 영역의 최저점을 기준으로 약 1.36cm 더 내려 배치했다.
- 사용자 제공 원본 모델의 자세와 외형을 유지했다. 새 포즈/리깅/애니메이션을 만들었다고 기록하지 않는다.

### 생성 / 수정한 에셋과 오브젝트

신규 모델·텍스처·머티리얼:
- Assets/Game/Models/Varco/Scientist/Varco_SteampunkGentleman.fbx
- Assets/Game/Models/Varco/Scientist/Textures/base_color.png, normal.png
- Assets/Game/Models/Varco/Scientist/Materials/Varco_SteampunkGentleman_URP.mat
- Assets/Game/Models/Varco/Golem/Varco_BronzeGolem.fbx
- Assets/Game/Models/Varco/Golem/Textures/base_color.png, normal.png
- Assets/Game/Models/Varco/Golem/Materials/Varco_BronzeGolem_URP.mat

신규 프리팹:
- Assets/Game/Prefabs/Environment/VarcoScientist.prefab — 정적 모델 + CapsuleCollider.
- Assets/Game/Prefabs/Enemies/VarcoBronzeGolem_Visual.prefab — 기존 경비 캐릭터에 연결할 시각 모델. 추가 Collider/Animator 없음.

수정:
- Assets/Game/Prefabs/Enemies/GuardRobot.prefab — 기존 Visual_Robot_Replaceable 비활성화, Visual_Golem_Replaceable 추가, LabCombatFeedback.bodyRenderers를 실제 Golem Renderer로 교체.
- Assets/Game/Scenes/MadScientistLab_Prototype.unity — 기존 경비 3기 프리팹 인스턴스에 외형 반영, 연구 구역에 ScientistNPC_Varco 배치.
- 과학자 위치 (-9, 0.3, -1.7), Y 회전 125도. 0.3m 받침대 위에 발을 맞췄다.
- 기존 VarcoScientist_PlacementMarker를 VarcoScientist_ResearchStation으로 정리했다.
- 경비 루트/체력/AI/공격/회피/스테이지 참조와 수치는 유지했다.
- 스크립트 신규/수정: 없음.

기록:
- ModelImportManifest.json — 원본 해시, 적용 범위, 변경 파일, 회귀 검증 요약.
- Phase4ModelsValidation.json — 모델/텍스처/배치/효과 연결 8개 검사.
- Phase4ModelsTestResults.json — 기존 Play Mode 검증을 새 모델 씬에서 실행한 결과.
- Captures/Phase4Models/ImportedModels_PreviewEnabled.png — 두 모델 정면 확인.
- Captures/Phase4Models/Models_InGame.png — 경비 3기가 적용된 실제 게임 화면.
- Captures/Phase4Models/Scientist_InResearchArea.png — 연구 구역 과학자.
- Captures/Phase4Models/Models_UnlockedHUD.png, Models_ClearScreen.png, Models_GameOverScreen.png — 실제 회귀 플레이 결과.
- Phase 3 검증 코드를 수정하지 않고 재사용했다. 검증이 기록하는 기존 Phase 3 결과/캡처를 먼저 백업한 후, 새 결과를 Phase4Models 경로에 복사하고 Phase 3 원본 기록은 복원했다.

### 실제 테스트

- Play Mode 회귀: **39 passed / 0 failed**, complete=true.
- 기록 시각: 2026-10-21T14:52:04.3309509+09:00.
- 기본 수치 전투: WASD and mouse only, after restoring spawn; elapsed=5.582531s, HP=70, attacks=6.
- 새 Golem 외형으로 경비 3기 처치, 출구 개방/진입, 클리어, 기본 적 공격으로 패배, 실제 UI 클릭을 통한 재시작 3회 통과.
- 별도 모델 검사 **8 passed / 0 failed**:
  - 경비 3기의 시각 오브젝트/피격 Renderer 연결, 중복 Collider/Animator 없음.
  - 과학자의 발이 0.3m 받침대에 맞고 Collider가 있으며 Animator 없음.
  - 두 모델의 URP Base Map/Normal Map 연결과 Animation 비활성화.
  - 실제 Golem Mesh에 사망 색상 RGBA(0.13, 0.14, 0.15, 1) 적용.
  - 임시 카메라/프리뷰/검증 오브젝트 및 가상 입력 장치 제거.
- 마지막 사망 효과 검사는 Play Mode에서 중앙 피해 API로 적 1기를 처치하여 준비했다. 이 검사용 상태는 씬에 저장하지 않았다.
- 실제 캡처에서 텍스처, 방향, 크기, 과학자 발 위치, 경비 외형을 직접 확인했다.
- Scene validate: missingScripts=0, brokenPrefabs=0, totalIssues=0.
- 테스트 완료 후 Console: Error 0 / Warning 0.
- 최종 Edit Mode, 개발 씬 isDirty=false, 루트 11개.
- 3~5분 플레이 타임 조정, 사용자 수동 플레이, 배포 빌드는 이번 작업에서 수행하지 않았다.

### 오류와 해결

- 첫 모델 프리뷰 캡처는 비활성 임시 Camera를 지정하여 배경만 나왔다. URP에서 임시 Camera를 활성화한 후 두 모델이 정상 표시되는 것을 확인했고, 프리뷰와 카메라는 작업 후 제거했다.
- 읽기용 PowerShell 해시 조회에서 for 문 결과를 바로 파이프에 연결한 구문 오류 1회가 있었다. 결과를 변수에 모아 조회하도록 수정했다. 파일 쓰기는 없었다.
- 작업 초반 기존 MCP WebSocket is not initialised 경고 1건이 조회되었다. 모델 임포트/추출/배치/검증 도구 호출은 성공했다. 검증 완료 후 Console에서는 오류/경고가 조회되지 않았다.
- 모델/텍스처 임포트 오류, C# 컴파일 오류, 게임 런타임 오류는 발생하지 않았다.
- MCP 연결 실패를 파일 직접 수정으로 우회한 작업은 없다.

### 사용한 Unity MCP 도구

- import_model_file: 사용자 FBX 두 개 임포트.
- read_mcp_resource: Editor 상태와 custom-tools 확인.
- unity_reflect: ModelImporter.ExtractTextures, AssetImporter.AddRemap, PrefabUtility API 확인.
- execute_code: 백업, 내장 텍스처 추출, URP 머티리얼/프리팹/씬 구성, 모델 검사, 기존 Play Mode 검증 시작, 기록 작성.
- manage_scene: 최종 씬 상태와 누락 참조 검사.
- manage_editor: Play Mode 진입/종료.
- manage_camera: 모델 프리뷰/게임/과학자 배치 캡처.
- read_console: 임포트/컴파일/런타임 오류 조회.
- 보조 로컬 읽기: 파일 확인, 패키지 도구 소스 읽기, 결과 JSON 확인, SHA-256 비교.

### 사용자 / AI 역할과 다음 작업

- 사용자: 두 모델 제공, 애니메이션 작업 보류 결정.
- AI: 내장 텍스처 확인/추출, Unity 임포트, 머티리얼/프리팹 연결, 배치와 기존 기능 회귀 검증, 기록.
- AI가 3D 모델이나 텍스처를 제작한 것은 아니다.
- 이번 모델 적용에 추가 리소스는 필요하지 않다.
- 애니메이션은 사용자 결정대로 보류한다. 별도 승인 없이 리깅이나 애니메이션 작업을 진행하지 않는다.
- Phase 4의 환경/조명/추가 연출 및 Phase 5 난이도/플레이 타임/빌드 작업은 남아 있다.

## 2026-10-21 (Asia/Seoul) — 과학자 역할 정정: 플레이어 캐릭터

### 사용자 요청과 역할
- 사용자: “뭔가 잘못 전달 됬나봄 과학자가 캐릭터야”라고 정정했다. 과학자는 사용자가 직접 조작하는 플레이어이며, 애니메이션은 기존 요청대로 보류한다.
- AI: 앞선 모델 적용에서 Gentleman을 NPC로 해석한 오류를 수정했다. 모델·텍스처 제작은 사용자 제공 리소스이며 AI가 제작했다고 기록하지 않는다.
- 앞의 Phase 4 NPC 배치 기록과 당시 검증은 과거 이력이다. 현재 역할과 배치는 이 정정 기록을 따른다.

### 백업 및 변경
- 변경 전 개발 씬, DevelopmentLog.md, ModelImportManifest.json, Phase 3 결과/캡처를 DevelopmentBackups/PlayerScientistCorrection에 보관했다.
- 신규: Assets/Game/Prefabs/Player/VarcoScientist_Visual.prefab 및 meta. 기존 Scientist FBX, 내장 텍스처와 URP 머티리얼을 재사용한 시각 전용 프리팹이다.
- Player/Visual_Scientist_Replaceable에 연결했다. 발은 Player 원점, 높이는 1.8m이다. 추가 Collider/Animator/Animation은 없다.
- 기존 Player의 CharacterController, 이동/공격/회피/체력 스크립트와 카메라/스테이지 참조를 유지했다.
- Visual_Capsule_Replaceable과 ForwardIndicator는 비활성화하여 보관했다. AttackArc는 유지했다.
- LabCombatFeedback.bodyRenderers를 실제 과학자 모델 Renderer로 변경했다.
- 연구 구역의 잘못된 NPC 배치인 VarcoScientist_ResearchStation과 그 자식 ScientistNPC_Varco, NPC_PlaceholderBase를 제거했다. 다른 연구 구역 소품은 유지했다.
- Assets/Game/Prefabs/Environment/VarcoScientist.prefab은 미사용 과거 에셋으로 남겨두었다.
- Assets/Packages/ProjectSettings SHA-256 비교: 200개 → 202개. 기존 변경은 개발 씬 1개, 신규 플레이어 시각 프리팹과 meta 2개, 삭제 0개.
- C# 소스, 입력, 경비 프리팹, 원본 SampleScene, 패키지, 프로젝트 설정은 변경하지 않았다.

### 실제 검증
- 모델/연결 정적 검사: 8 passed / 0 failed. 활성 시각 모델, Primitive 비활성화, 단일 CharacterController, 애니메이션 없음, 피격 Renderer 연결, 높이/발 위치, NPC 제거, 골렘 3기 보존을 확인했다.
- 기존 Phase3PlayModeChecks 재사용: **39 passed / 0 failed**, complete=true.
- 완료 시각: 2026-10-21T15:08:52.6177172+09:00.
- 기본 수치 전투: WASD and mouse only, after restoring spawn; elapsed=5.601201s, HP=70, attacks=6.
- 과학자 플레이어로 경비 3기 처치, 출구 개방/진입, 승리, 기본 적 공격에 의한 패배, UI 클릭 재시작 3회가 통과했다.
- 실제 게임 캡처에서 과학자 뒷모습, 크기, 텍스처와 기본 3인칭 구도를 확인했다. 캐릭터는 원본 고정 자세를 사용한다.
- Scene validate: missingScripts=0, brokenPrefabs=0, totalIssues=0.
- 최종 Edit Mode, isDirty=false, 루트 11개, Player 입력 활성, NPC 0개, 임시 오브젝트 및 검증용 입력 장치 0개.
- 기존 Phase 3 결과와 캡처는 검증 후 백업본으로 복원했다.

### 검증 과정의 문제와 처리
- 첫 실행 전에 화면을 확인하면서 카메라 방향이 yaw=289.19986, pitch=32.36003으로 바뀌었다. 기존 검증은 기본 카메라 방향을 전제로 월드 축 이동을 입력하므로 검증 시작 조건에 맞지 않았다.
- 이 실행은 10 passed / 0 failed, complete=false 상태에서 중단하고 PlayerScientistTestAttempt1.json에 남겼다. 성공한 완전 검증으로 계산하지 않는다.
- 새 Play Mode에서 카메라를 초기 yaw=0, pitch=20으로 맞추고 재실행하여 39개를 모두 통과했다. 게임/테스트 소스 수정이나 전투 수치 완화는 하지 않았다.
- Play Mode 전환 직후 execute_code 1회가 success=false, 추가 메시지 없이 반환되었다. Editor 상태로 전환 중임을 확인하고 중복 실행 여부 확인 후 MCP로 재시도했다.
- 최종 Console에는 MCP WebSocket is not initialised 경고 1건, Error 0건이 있었다. 후속 MCP 호출은 성공했고 프로젝트 파일 직접 수정으로 연결 문제를 우회하지 않았다.
- 중단/재실행 후 남아 있던 Phase3TestKeyboard/Phase3TestMouse 2개를 이름으로 확인해 제거했다. 마지막 검증용 장치는 0개다.
- 기본 화면 캡처를 위해 런타임에서만 입력을 잠시 비활성화하고 카메라를 초기 방향으로 돌렸다. Play Mode 종료 후 저장 씬의 입력 활성 상태를 확인했다.

### 기록 및 도구
- PlayerScientistValidation.json: 모델 연결, 최종 상태, 변경 파일 검증.
- PlayerScientistTestResults.json: 완료한 39개 회귀 검증.
- Captures/PlayerScientist/PlayerScientist_DefaultView.png: 기본 3인칭 화면.
- Captures/PlayerScientist/PlayerScientist_UnlockedHUD.png, PlayerScientist_ClearScreen.png, PlayerScientist_GameOverScreen.png: 실제 회귀 플레이 결과.
- ModelImportManifest.json의 Gentleman 역할을 플레이어로 정정하고 이번 변경 이력을 추가했다.
- Unity MCP: execute_code(백업/프리팹/씬 연결/검증/기록), manage_editor(Play/Stop), manage_camera(화면 캡처), manage_scene(씬 검사), read_console(오류 조회), read_mcp_resource(Editor 상태).
- 보조 로컬 작업은 소스/결과 읽기와 SHA-256 비교뿐이며 프로젝트 쓰기는 Unity MCP를 사용했다.
- 애니메이션, 나머지 연출, Phase 5 플레이 타임 조정 및 빌드는 진행하지 않았다.

## 2026-10-21 (Asia/Seoul) — 과학자 플레이어 모델 V2 교체

### 사용자 요청 / 실제 적용
- 사용자: 새 Steampunk Gentleman Character-b22ccfb55b658305fe38768ba9525eaa (1) (1).fbx를 제공하고 “캐릭터가 조금 바뀜”이라고 알렸다.
- AI: 새 FBX와 내장 텍스처를 임포트하여 기존 플레이어의 시각 모델을 교체했다. 과학자=플레이어 역할과 애니메이션 보류 결정을 유지했다.
- 원본: C:\Users\mbc\Downloads\Steampunk Gentleman Character-b22ccfb55b658305fe38768ba9525eaa (1) (1).fbx
- 크기: 24453744 bytes. 원본 및 Unity 복사본 SHA-256: FC1DF814EA5F5A84A49AF2D6E39D381AC130317EACC9C24A62D51EBA4BF018B1.
- 사용자 원본 파일은 수정하지 않았다. 이전 FBX/텍스처/머티리얼/플레이어 시각 프리팹도 그대로 보관했다.
- 변경 전 개발 씬/기록과 기존 Phase 3 검증 결과·캡처를 DevelopmentBackups/ScientistModelV2에 백업했다.

### 변경 에셋
- 신규 모델: Assets/Game/Models/Varco/ScientistV2/Varco_SteampunkGentleman_V2.fbx.
- 내장 base_color.png / normal.png를 ScientistV2/Textures에 추출했다. 별도 텍스처 제공은 필요하지 않았다.
- ScientistV2/Materials/Varco_SteampunkGentleman_V2_URP.mat 생성. 기존 URP/Lit 설정을 재사용하고 새 색상/노멀 텍스처를 연결했다. Unity 로드 해상도는 각 2048×2048이다.
- 신규 플레이어 시각 프리팹: Assets/Game/Prefabs/Player/VarcoScientistV2_Visual.prefab.
- 새 모델은 MeshRenderer 1개, 29,683 vertices / 40,000 triangles이며 원본 고정 자세를 사용한다.
- X=-90도로 축을 보정하고 높이 1.8m, 발 위치 Player 원점에 맞췄다.
- import_model_file의 animation_type=none은 Rig=None으로 적용되었지만 importAnimation은 true였다. ModelImporter에서 importAnimation=false를 명시적으로 적용했다. Animator/Animation/추가 Collider는 없다.
- 개발 씬의 Player/Visual_Scientist_Replaceable을 새 프리팹으로 교체하고 LabCombatFeedback.bodyRenderers를 새 MeshRenderer로 연결했다.
- Player의 입력/이동/공격/회피/체력/CharacterController 및 카메라/스테이지 참조와 수치를 유지했다. 경비 3기의 골렘 모델도 유지했다.
- Assets/Packages/ProjectSettings 비교: 202개 → 215개, 기존 변경은 개발 씬 1개, 신규 13개(meta 포함), 삭제 0개. 기존 스크립트, 원본 SampleScene, 경비 프리팹, 패키지와 프로젝트 설정은 변경하지 않았다.

### 실제 검증과 결과
- 모델 연결/높이/바닥/Collider/애니메이션 보류/텍스처/피격 Renderer/구버전 보관/경비 상태를 묶은 정적 검사 8개 모두 통과.
- 기존 Phase3PlayModeChecks 재사용: **39 passed / 0 failed**, complete=true.
- 완료 시각: 2026-10-21T15:33:37.8428560+09:00.
- 기본 수치 전투: WASD and mouse only, after restoring spawn; elapsed=5.609532s, HP=70, attacks=6.
- 새 과학자로 경비 3기 처치, 출구 개방/진입, 승리/패배, 실제 UI 클릭 재시작 3회가 통과했다.
- 검증 시작 전 런타임 카메라 방향을 기본 yaw=0, pitch=20으로 설정하여 자동 입력의 시작 조건을 맞췄다. 테스트/게임 코드 변경은 없다.
- 실제 기본 게임 화면과 정면 프리뷰에서 얼굴/의상/텍스처/발 위치와 모델 방향을 확인했다.
- 프리뷰를 위한 카메라 각도/FOV 및 입력 비활성화는 런타임에서만 적용했다. 종료 후 입력/카메라 컨트롤러 활성, FOV=60으로 복원된 것을 확인했다.
- Scene validate: missingScripts=0, brokenPrefabs=0, totalIssues=0.
- 최종 Edit Mode, isDirty=false, 루트 11개, 테스트 러너 0개, 검증용 입력 장치 0개.
- 완료 직후 Console Error 0 / Warning 0. Play Mode 종료 후 MCP WebSocket is not initialised 경고 1건이 조회되었다. 게임/컴파일 오류는 없고 이번 작업의 MCP 도구 호출은 모두 성공했다.
- MCP 오류를 파일 직접 수정으로 우회하지 않았다.

### 기록 / 사용 도구
- ScientistModelV2Validation.json: 모델 연결, 원본 해시, 변경 파일, 최종 상태.
- ScientistModelV2TestResults.json: 완료한 39개 회귀 검증.
- Captures/ScientistModelV2/ScientistV2_DefaultView.png, ScientistV2_FrontPreview.png: 교체 확인 화면.
- 같은 폴더의 ScientistV2_UnlockedHUD.png, ScientistV2_ClearScreen.png, ScientistV2_GameOverScreen.png: 실제 회귀 플레이 결과.
- 기존 Phase 3 결과/캡처는 백업본으로 복원하고 SHA-256 일치 4/4를 확인했다. 이전 모델 적용의 검증 기록은 유지했다.
- ModelImportManifest.json에 activePlayerModel 및 V2 교체 이력을 추가했다.
- Unity MCP: import_model_file, execute_code, manage_editor, manage_camera, manage_scene, read_console, read_mcp_resource.
- 로컬 도구는 원본/결과 읽기와 해시 비교에만 사용했다. 프로젝트 쓰기는 Unity MCP로 수행했다.
- 사용자 역할: 새 모델 제공. AI 역할: 임포트/텍스처 연결/프리팹 교체/검증/기록. 모델·텍스처 자체를 AI가 제작한 것으로 기록하지 않는다.
- 애니메이션과 다른 Phase 작업은 추가 진행하지 않았다.

## 2026-10-21 (Asia/Seoul) — 플레이어 애니메이션 및 클릭 3타 콤보

### 사용자 요청 / 승인 범위
- 사용자가 Drunk Idle.fbx, Walking.fbx, dodge.fbx, Standing Melee Combo Attack Ver. 2.fbx를 제공했다.
- 합쳐진 공격 동작을 나누고 클릭 1회는 1타, 2회는 1→2타처럼 입력 횟수에 따라 이어지는 콤보를 요청했다.
- 이 요청에 따라 플레이어 애니메이션 보류를 해제하고 제공된 대기/걷기/회피/공격을 연결했다. 적 애니메이션, 사운드, 추가 환경 연출, Phase 5 빌드는 진행하지 않았다.
- 사용자: 모델 및 애니메이션 원본 제공. AI: Unity 임포트/클립 분리/리타기팅 연결/게임 로직 구현/검증. 원본 동작을 AI가 제작한 것으로 기록하지 않는다.

### 백업 / 원본 보존
- DevelopmentBackups/PlayerComboAnimation에 기존 개발 스크립트·meta, 개발 씬, DevelopmentLog.md, ModelImportManifest.json, Phase 3 결과/캡처 36개를 백업했다.
- 다운로드 원본 4개와 Unity에 복사한 FBX 4개의 SHA-256이 각각 일치한다. 상세 해시는 PlayerComboAnimationManifest.json에 기록했다.
- 이전 정적 과학자 모델/시각 프리팹, 골렘/경비 프리팹, 원본 SampleScene, 입력 에셋, 패키지와 프로젝트 설정은 보존했다.
- Assets/Packages/ProjectSettings: 215개 → 234개. 기존 수정 4개(개발 씬 + Player 코드 3개), 신규 19개(meta 포함), 삭제 0개.

### 리깅과 클립
- 네 파일 모두 유효한 Humanoid Avatar로 임포트되었다.
- Drunk Idle에는 과학자의 SkinnedMeshRenderer가 포함되어 있다. 29,683 정점 모두 스킨 가중치가 있고 52개 뼈대에 연결되어 있다. 정적 V2 과학자와 같은 외형/정점 수를 확인하고 기존 V2 URP 머티리얼과 텍스처를 재사용했다.
- Idle FBX globalScale=1.8720367, Y-up 좌표계를 사용한다. Idle 첫 프레임에서 발 최저점 -0.0226256m를 측정하여 모델을 +0.0226256m 올렸다. 해당 자세의 메시 높이는 약 1.777m이며 원래 약 1.8m 캐릭터 크기를 유지한다.
- Idle 0–222프레임, Walk 0–42프레임을 루프로 사용한다. Dodge는 10–60프레임을 사용한다.
- 공격 원본 0–126프레임(30fps, 4.2초)을 뼈대 위치 샘플과 실제 포즈 캡처로 조사했다. 단순한 길이 3등분이 아닌 타격/회복 동작 경계를 사용했다.

| 클립 | 원본 프레임 | 타격 원본 프레임 | 재생 배속 | 실제 길이 / 타격 시점 |
| --- | --- | --- | --- | --- |
| Attack1 | 8–40 | 29 | 1.6 | 약 0.667초 / 0.438초 |
| Attack2 | 40–68 | 59 | 1.6 | 약 0.583초 / 0.396초 |
| Attack3 | 68–112 | 79 | 1.6 | 약 0.917초 / 0.229초 |

- 앞뒤 불필요한 대기 구간은 잘랐으며 원본 FBX 바이트는 보존했다. 공격 클립은 루프하지 않는다.
- AnimatorController는 Idle/Walk/Dodge/Attack1/Attack2/Attack3 상태와 PlaybackSpeed 파라미터를 사용한다. 자동 다음 공격 전환은 없고 게임 로직이 클릭 입력에 따라 상태를 선택한다.
- Animator.applyRootMotion=false. Walk/Dodge/공격의 XZ 이동은 root motion으로 분리하여 무시하고 기존 CharacterController만 이동한다. 대기의 작은 흔들림은 포즈에 유지했다.
- API 의미 확인: [Unity ModelImporterClipAnimation.lockRootPositionXZ](https://docs.unity3d.com/ja/6000.0/ScriptReference/ModelImporterClipAnimation-lockRootPositionXZ.html). 프로젝트 동작은 별도 실제 입력/뼈대 위치 검사로 검증했다.
- 회피 거리 3m, 동작 시간 0.3초, 회복 0.8초, 무적 0.18초는 유지했다. 회피 클립은 기존 0.3초 동작에 맞춰 약 5.56배속으로 재생한다.

### 콤보 동작
- 좌클릭 1회: 1타만. 빠른 2회: 1→2타. 빠른 3회: 1→2→3타.
- 진행 중 후속 클릭을 최대 2개 저장한다. 연타해도 한 콤보에서 3타를 넘기지 않는다.
- 버튼을 누르고 유지하는 것은 추가 클릭으로 처리하지 않는다.
- 각 타격 종료 후 0.25초 동안 후속 입력을 허용한다. 시간이 지나면 다음 클릭은 1타로 돌아간다. 3타 종료 후 회복은 0.12초다.
- 피해는 각 클립의 실제 타격 지점에서 한 번 처리한다. 타격당 피해 15, 사거리 2.2m, 각도 100도는 유지했다.
- 타격 순간의 대상 위치/벽 가림을 검사하고, 같은 적의 Collider가 여러 개라도 한 번만 피해를 준다.
- 공격 중 이동/회전은 멈추되 중력과 충돌 처리는 유지한다. Space 회피는 공격보다 우선하며 진행/대기 콤보를 취소한다.
- 회피 중 클릭, Esc로 제어 해제, 사망/승패 종료 시 남은 타격을 실행하지 않는다.
- 화면 하단 조작 안내를 “좌클릭 1~3타 콤보”로 갱신했다.

### 코드 / 에셋 변경
- 수정: Assets/Game/Scripts/Player/LabPlayerCombat.cs — 즉시 공격/고정 쿨다운을 클릭 버퍼·3단계 공격·타격 시점 피해·취소로 교체.
- 수정: Assets/Game/Scripts/Player/LabPlayerMotor.cs — 공격 중 이동/회전 제한, 회피 우선 유지.
- 수정: Assets/Game/Scripts/Player/LabPlayerDodge.cs — 기존 동작을 바꾸지 않고 읽기 전용 Duration 속성 추가.
- 신규: Assets/Game/Scripts/Player/LabPlayerAnimation.cs — 이동/회피/공격 상태와 Animator 연결, 사망/종료 시 정지.
- 신규: Assets/Game/Tests/PlayerComboPlayModeChecks.cs — 실제 가상 키보드/마우스로 콤보·취소·피해 시점 검증.
- 신규: Assets/Game/Animations/Scientist/Source의 원본 4개 및 임포트 설정, Scientist.controller.
- 신규: Assets/Game/Prefabs/Player/VarcoScientist_Animated.prefab.
- 수정: Assets/Game/Scenes/MadScientistLab_Prototype.unity — 애니메이션 프리팹, 드라이버, 3개 공격 클립/타격 시점, 피격 Renderer, 조작 안내 연결.
- 기존 LabHealth, LabMeleeQuery, 적 AI, 스테이지/HUD 스크립트, 입력 코드 및 기존 Phase 1–3 검사 소스는 변경하지 않았다. 구형 Phase 2 검사는 당시 즉시 피해/공격 쿨다운을 가정하는 역사적 검사이며 이번에는 새 콤보 검사와 기존 Phase 3를 실행했다.

### 실제 검증
- 콤보 검사: **30 passed / 0 failed**, complete=true (2026-10-21T16:02:25.9631929+09:00).
- 검사 범위: Idle 뼈대 동작, Walk 입력/이동, 1/2/3회 클릭, 길게 누르기, 5회 빠른 클릭 상한, 시간 초과/짧은 유예, 타격 전 무피해/타격 시 1회 피해, 중복 Collider, 이동 제한, 회피/회피 중 클릭, 루트 이동 중복 방지, 벽 가림, 공격 준비 중 이탈한 대상, Esc/커서 재획득, 사망 시 예약 타격 취소.
- 기존 Phase 3: **39 passed / 0 failed**, complete=true (2026-10-21T16:05:34.5177399+09:00).
- 실제 기본 수치 전투: WASD and mouse only, after restoring spawn; elapsed=7.223408s, HP=60, attacks=6.
- 경비 3기 처치, 출구 개방/진입, 승리/패배, 실제 UI 버튼 재시작 3회 통과.
- 합계 **69개 통과 / 0개 실패**. 사용자 수동 플레이나 배포 빌드를 수행했다고 기록하지 않는다.
- Scene validate: missingScripts=0, brokenPrefabs=0, totalIssues=0.
- 플레이 검증 중 Console Error 0 / Warning 0. Play Mode 종료 후 기존과 같은 MCP WebSocket is not initialised 경고 1건이 조회되었다. 게임/컴파일 오류는 없었고 Unity MCP 호출은 모두 성공했다.
- 최종 Edit Mode, isDirty=false, 루트 11개, Player Collider는 CharacterController 1개, Humanoid Animator 1개, 모든 플레이어/카메라 제어 컴포넌트 활성.
- 임시 포즈 시트/카메라/테스트 표적/검증 러너/가상 입력 장치는 모두 제거했다.

### 실패와 수정 / 기록
- 첫 콤보 검사: 28 passed / 1 failed. 걷기 경로 1.5m 앞에 둔 테스트 표적 Collider가 이동을 막아 Walk 검사에서 Idle이 관측되었다. PlayerComboTestAttempt1.json에 그대로 보관했다.
- 표적을 걷기 경로 밖으로 옮기고 회피 중 클릭 검사 1개를 추가한 재실행에서 30개 모두 통과했다. 이 수정은 검사 배치 변경이며 게임 수치나 판정을 완화하지 않았다.
- 원본 파일 해시를 읽는 PowerShell에서 foreach 결과를 바로 파이프로 잇는 구문 오류 1회, rg 경로 와일드카드 오류 1회가 있었다. 읽기 명령을 고쳐 재조회했으며 파일 쓰기는 없었다.
- 첫 최종 화면 캡처는 Overlay HUD가 포함되지 않았다. Canvas 상태/텍스트가 정상임을 확인하고 런타임 Canvas 갱신 후 ScreenCapture로 HUD 포함 화면을 다시 캡처했다.
- Canvas의 갱신 후 dirty 표시가 남아 직렬화 값이 저장 씬과 같음을 확인하고 씬을 저장했다.
- PlayerComboTestResults.json: 통과한 30개 검사. PlayerComboStageTestResults.json: 통과한 스테이지 39개 검사.
- PlayerComboAnimationManifest.json: 원본 해시, 클립 범위, 타격 시점, 콤보 규칙, 최종 상태와 변경 파일.
- Captures/PlayerCombo: Attack_SourceFrames.png, Dodge_SourceFrames.png, Combo_Attack1/2/3.png, AnimatedPlayer_HUD.png, Combo_UnlockedHUD/ClearScreen/GameOverScreen.png.
- 기존 Phase 3 보고서/캡처는 검증 후 백업으로 복원하고 해시 일치 4/4를 확인했다.

### 사용 도구
- Unity MCP: read_mcp_resource, import_model_file, unity_reflect, execute_code, manage_editor, manage_camera, manage_scene, read_console.
- execute_code로 백업, 임포트 설정, 포즈 샘플링/클립 분리/프리팹/컨트롤러/씬/스크립트/검증/기록 쓰기를 수행했다.
- 보조 로컬 작업은 파일/코드 읽기, 해시 비교, 캡처 확인뿐이다. Unity MCP 연결 문제를 직접 파일 수정으로 우회하지 않았다.

## 2026-10-21 (Asia/Seoul) — 걷기→회피 전환 개선

- 사용자 피드백: “걷다가회피하는게 자연스럽진 않네? 움직임이”. AI 역할: 기존 원본 동작을 유지하면서 전환/이동/재생 시간을 조정하고 실제 입력 검증. 새로운 애니메이션 원본을 제작한 것이 아니다.
- Unity MCP로 현재 개발 씬과 코드/Animator를 확인하고 DevelopmentBackups/DodgeTransition에 변경 대상과 검증 기록 22개를 백업했다.
- 원인: 1.667초 회피 클립을 0.3초에 재생(약 5.56배), 즉시 10m/s 등속 이동, 0.035초 전환, 모든 Animator 상태가 PlaybackSpeed 하나를 공유해 전환 중 이전 동작의 속도까지 바뀜.
- 회피 동작을 0.75초(약 2.22배속)로 변경했다. 원본 10–60프레임 범위와 3m 거리는 유지한다.
- 원본 구르기의 전진 비율에 맞춘 누적 이동 곡선으로 밀어내기→구르기→일어서기를 연결한다. 시작 곡선의 기울기는 회피 방향으로 투영한 기존 이동 속도에 맞춘다. 위치 차분을 적분하여 프레임 간격이 달라도 총 이동 거리를 유지한다.
- 회피 진입 블렌드 0.09초, 이동 상태 블렌드 0.14초, 회피 후 걷기 속도 회복 약 0.12초, WalkSpeed 완충 0.06초. WalkSpeed/DodgeSpeed/AttackSpeed를 분리하고 Idle은 고정 1배속으로 둔다.
- 회피 마지막 이동 프레임까지 Dodge 상태를 유지한다. 시작 오프셋은 이동에서 이미 소비한 실제 상태 시간으로 맞춰 프레임 지연 시 포즈와 이동의 시간 차이를 보정한다.
- 무적은 기존 0.18초, 종료 후 쿨다운은 기존 0.8초다. 동작 시간이 길어져 회피 시작부터 다음 회피까지의 최소 간격은 1.1초에서 1.55초로 늘었다.
- CharacterController만 이동한다. FBX, 임포트 설정, 과학자 프리팹/머티리얼, 공격 판정/클릭 콤보 로직, 적, UI, 입력 에셋과 프로젝트 설정은 수정하지 않았다.
- 수정 파일: LabPlayerDodge.cs, LabPlayerMotor.cs, LabPlayerAnimation.cs, Scientist.controller, MadScientistLab_Prototype.unity, PlayerComboPlayModeChecks.cs. Assets/Packages/ProjectSettings 총 234개 유지, 변경 6개, 추가/삭제 0개.
- 검사 소스는 기존 30개 콤보 검사에 전환/시간 동기화/벽 충돌/무적/취소 13개를 추가했다. 변경된 회피 시간과 쿨다운을 기다리도록 검사 시간을 갱신했고 이번 보고서/캡처는 DodgeTransition 경로에 별도 저장한다.
- 최종 실제 Play Mode 검사: 전환·콤보 **43 passed / 0 failed**, 기존 Phase 3 **39 passed / 0 failed**. 3기 기본 경비 처치, 승패, 출구, 재시작 검사 통과. 전투 기록: elapsed=7.227256s, HP=60, attacks=6.
- 거리 적분 검사: 정지/걷기 출발 각각 30/60/120fps, dt=0.017/0.2초의 10조건 모두 3m 도착/역이동 없음. 총 **92개 통과 / 0개 실패**.
- 걷기→회피 측정: 걷기 4.499969m/s, 회피 첫 프레임 평균 3.561677m/s, 중간 최대 7.929829m/s, 마지막 0.005287129m/s, 회피 이동 3m. 애니메이션 진행률 최대 오차 0.001749158(0.75초 기준 약 1.31ms).
- 진입/회전 중/일어서기/걷기 복귀 캡처를 확인했다. 이는 자동 입력과 캡처 검증이며 사용자 수동 플레이의 체감을 대신했다고 기록하지 않는다.
- 첫 시도 41/1: 시작 프레임 이동에 비해 애니메이션이 늦어 최종 진행률 0.9415. 두 번째 42/1: 시작 오프셋에 원본 클립 초를 사용해 상태 배속이 중복 반영되어 최대 오차 0.1083. 실제 상태 초로 수정한 최종 43/0. 두 실패 보고서와 프레임 기록도 보존했다.
- Unity API 참고: [Animator.CrossFadeInFixedTime](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animator.CrossFadeInFixedTime.html). fixedTimeOffset의 상태 시간 의미를 확인했으며 배속과의 실제 관계는 프레임별 측정으로 검증했다.
- Play Mode Console Error 0 / Warning 0. 종료 후 MCP WebSocket is not initialised 경고 1건. 모든 MCP 호출은 성공했고 직접 파일 수정으로 우회하지 않았다.
- 기존 Phase 3 보고서/캡처 4개는 이번 결과를 별도 보관한 후 백업으로 복원했으며 해시 일치 4/4. 이전 콤보 보고서/캡처는 덮어쓰지 않았다.
- 최종 Edit Mode, 씬 저장/isDirty=false, Scene validate 문제 0, 루트 11개, 임시 테스트 오브젝트와 가상 입력 장치 0개.
- 상세 기록: DodgeTransitionManifest.json, DodgeTransitionTestResults.json, DodgeTransitionStageTestResults.json, DodgeDistanceChecks.json, DodgeTransitionSamples.json, Captures/DodgeTransition. PlayerComboAnimationManifest.json의 현재 회피 수치와 최신 검증 참조도 갱신했다.
- 사용 Unity MCP: read_mcp_resource, execute_code, manage_editor, manage_scene, read_console. execute_code로 백업/코드·Animator·씬 변경/런타임 검증/기록 쓰기를 수행했다. 보조 도구는 소스·해시 읽기, 캡처 보기, 공식 API 문서 확인에만 사용했다.

## 2026-10-21 (Asia/Seoul) — 회피 충전 게이지

- 사용자 요청: 회피 쿨타임을 글자 대신 스태미나 같은 게이지로 보여 주거나 연속 구르기를 허용해 달라는 피드백. 요청한 선택지 중 시각적 게이지를 구현했다.
- 사용자 역할: 플레이 피드백과 UI 방향 제시. AI 역할: HUD 구성, 실제 회피 상태 연결, 입력/충전/재시작 검증. 백업은 DevelopmentBackups/DodgeGauge(9개 파일 + 초기 해시 목록).
- 체력 바 아래에 황금색 수평 게이지를 추가했다. 글자/숫자 0개, 이미지 5개이며 마우스 입력을 가로채지 않는다. HealthCard 높이를 116에서 140으로 늘려 기존 요소와 겹치지 않게 했다.
- Space로 회피하면 비워지고 회피 시작부터 0.75초 동작 + 0.8초 회복 = 1.55초 동안 충전한다. 가득 차면 밝은 황금색과 오른쪽 끝 표시가 켜지며 0.25초 동안 짧게 밝아진다.
- LabPlayerDodge에 IsReady/RechargeProgress를 추가했다. TryStart와 UI가 같은 nextReadyTime과 회피 시작 시 저장한 rechargeDuration을 사용한다. HUD에 별도 타이머를 두지 않는다.
- 기존 이동 거리/회피 동작/무적/쿨다운 수치와 클릭 콤보는 유지한다. 이번 변경으로 연속 회피나 새로운 스태미나 자원 규칙을 추가하지 않았다.
- 신규: Assets/Game/Scripts/UI/LabDodgeGauge.cs 및 meta. 수정: Assets/Game/Scripts/Player/LabPlayerDodge.cs, Assets/Game/Scenes/MadScientistLab_Prototype.unity.
- 전용 검증은 Unity MCP execute_code에서 임시 Editor update 콜백과 가상 키보드/마우스로 수행했다. 실제 Space 입력, 소모, 회복, 조기 재사용 방지, 일시정지, 재사용, 사망 시 비활성 표시, 재시작 초기화 등 **11개 통과 / 0개 실패**.
- 중간 충전 시 실제 값과 HUD 값 모두 0.531031, Time.timeScale=0 일시정지 동안 동일 값 유지. 게이지가 충전된 후 새 Space 입력으로 두 번째 회피가 실행되었다.
- 충전 시간 기준은 회피 시작 후 1.55초다. 검사에서 키 입력을 큐에 넣기 직전부터 화면 표시를 관측하기까지 측정한 시간은 1.640259초였으며 입력/캡처 프레임 지연을 포함한다.
- 기존 스테이지 회귀 검사 **39개 통과 / 0개 실패**, 합계 **50개 통과**. 승패/경비 처치/출구/실제 UI 재시작 흐름을 확인했다.
- Ready, Consumed, Recharging, Recharged, Restarted 캡처를 Captures/DodgeGauge에 저장했고 완충/충전 중 화면을 직접 확인했다.
- Play Mode 및 종료 후 조회된 Console 오류/경고 0. Scene validate 문제 0. 최종 Edit Mode, 씬 저장/isDirty=false, 루트 11개, 임시 오브젝트/가상 장치 0, timeScale=1.
- 검증용 콜백/가상 장치/입력 설정/백그라운드 실행 설정은 종료 시 복원했다. 기존 Phase 3 보고서와 캡처 4개는 새 결과를 별도 보관한 뒤 백업으로 복원하고 해시 일치 4/4를 확인했다.
- 기록: DodgeGaugeManifest.json, DodgeGaugeTestResults.json, DodgeGaugeStageTestResults.json. 이전 회피/콤보 검증 자료는 보존했다.
- 사용 Unity MCP: execute_code, manage_editor, manage_scene, read_console. 프로젝트의 백업/스크립트/씬/UI/검증/기록 변경은 Unity MCP로 수행했다. 보조 도구는 파일 읽기와 캡처 확인에 사용했다.

## 2026-10-21 (Asia/Seoul) — 스팀펑크 건물·외곽 벽·바닥 타일

- 사용자 요청: 제공한 이미지를 바닥 타일로 깔고, 건물 모델을 배치하며 벽은 외곽에 둘 것.
- 사용자 역할: 건물 4종, 벽 1종, 바닥 이미지 원본 및 배치 방향 제공. AI 역할: Unity MCP 임포트, URP 재질/충돌 프리팹, 배치, 타일 메시, 실제 검증과 기록. 모델과 원본 텍스처를 AI가 새로 제작한 것은 아니다.
- 현재 개발 씬을 확인하고 DevelopmentBackups/SteampunkEnvironment에 변경 전 씬/meta, 개발 기록, 모델 목록, 기존 Phase 3 보고서/캡처 등 8개 파일과 Assets/Packages/ProjectSettings 236개 해시를 백업했다.
- FBX 5종의 내장 base_color.png/normal.png를 추출하고 URP/Lit 재질로 연결했다. 원본 FBX와 바닥 PNG 사본 해시 6/6 일치. 원본 파일 바이트는 바꾸지 않았다.
- 정적 환경으로 설정: animationType=None, importAnimation=false. 모델 임포트 도구가 importAnimation을 true로 남긴 것을 확인해 false로 명시 수정했다. 환경 모델에 Animator/행동 스크립트를 추가하지 않았다.
- Assets/Game/Prefabs/Environment/Steampunk에 재사용 프리팹 5개를 만들었다. 바닥에 발을 맞추고 XZ 중앙을 원점으로 정렬했으며, 회전된 메시 범위를 감싸는 BoxCollider 1개씩을 추가했다.
- Laboratory/Environment_Steampunk 아래 Buildings / PerimeterWalls / TiledFloor로 정리했다.
- 건물 4개는 높이 8m로 좌우 가장자리에 배치하고 입구 면을 중앙으로 향하게 했다. InventorHouse (-8.8,0,-3.5), ClockworkShop (8.8,0,-3.5), BrassWorkshop (-8.8,0,4.8), BoilerHouse (8.8,0,4.8).
- 외곽 벽은 총 24개다. 남/서/동 각 6개, 북쪽 4개와 출구 옆 2개. 기본 폭 4m/높이 약 2.724m. 북쪽 출구 앞은 5.6m 간격을 확보하고 기존 문 개방/트리거 동작을 유지했다.
- 기존 임시 바닥 격자/외곽 블록/기둥/연구·기계 구역 표식은 삭제하지 않고 비활성화했다. 출구 복도 벽의 기존 Renderer만 숨겼으며 Collider는 유지했다.
- 바닥은 3m 타일 9x9로 27x27m에 반복한다. 원래 Ground 충돌판도 27x27m로 늘려 외곽 벽 아래를 받친다. 타일 표면은 Y=0.004, 정점 324개/삼각형 162개이며 중복 바닥 Collider가 없다.
- 최초 캡처에서 이미지 가장자리 흰 여백이 반복 선으로 보였다. 원본 PNG는 보존하고 각 타일의 UV를 가장자리에서 4.5픽셀 안쪽으로 제한하는 메시로 수정했다. 최종 화면에서 흰 선 제거를 확인했다.
- 일반 배치/충돌/텍스처/스폰/출구/원본 보존 점검: SteampunkEnvironmentStaticChecks.json, **13 passed / 0 failed**.
- 실제 Play Mode 기존 스테이지 회귀 검사: SteampunkEnvironmentStageTestResults.json, **39 passed / 0 failed**, complete=true (2026-10-21T17:37:33.0401665+09:00).
- 전투 기록: WASD and mouse only, after restoring spawn; elapsed=7.231228s, HP=60, attacks=6. 경비 처치, 출구 개방/실제 이동 진입, 승패, 실제 UI 버튼 재시작 흐름 통과.
- 합계 **52개 점검 통과 / 0개 실패**. 사용자 수동 플레이 또는 배포 빌드를 수행한 것으로 기록하지 않는다.
- Play Mode Console Error 0 / Warning 0. 종료 후 MCP WebSocket is not initialised 경고 1건, 게임/컴파일 오류 0. 이후 MCP 호출은 성공했다. 직접 파일 수정으로 연결 문제를 우회하지 않았다.
- Scene validate: missingScripts=0, brokenPrefabs=0, totalIssues=0. 최종 Edit Mode, 씬 저장/isDirty=false, 루트 11개, timeScale=1. 임시 검사 러너/가상 입력 장치 0개이며 백그라운드 실행 설정을 false로 복원했다.
- 무결성: Assets/Packages/ProjectSettings 236→314개, 기존 수정은 MadScientistLab_Prototype.unity 1개, 신규 환경 에셋/meta 78개, 삭제 0개. 기존 게임 스크립트/Animator/캐릭터/적 프리팹/프로젝트 설정은 변경되지 않았다.
- 이번 스테이지 보고서/캡처를 별도 보관한 후 기존 Phase 3 증거 4개를 백업에서 복원하고 해시 일치 4/4를 확인했다.
- 기록: SteampunkEnvironmentManifest.json, SteampunkEnvironmentStaticChecks.json, SteampunkEnvironmentStageTestResults.json. ModelImportManifest.json에 환경 기록 참조를 추가했다.
- 최종 캡처: Captures/SteampunkEnvironment/Layout_Overview_Final.png, Environment_PlayerHUD.png, Player_View_Final.png, Environment_UnlockedHUD/ClearScreen/GameOverScreen.png. 초기 흰 선이 보이는 캡처도 이력으로 보존한다.
- 읽기/조회 단계에서 PowerShell foreach 파이프 구문 오류 및 Unity 값 직렬화 출력 문제가 있었으며 재조회로 해결했다. 프로젝트 파일에는 영향이 없다.
- 사용 Unity MCP: read_mcp_resource, import_model_file, execute_code, manage_scene, manage_camera, manage_editor, read_console. 프로젝트 백업/에셋/씬/검증/기록 쓰기는 Unity MCP에서 수행했다. 보조 로컬 도구는 파일 읽기·해시 확인·캡처 보기에만 사용했다.
