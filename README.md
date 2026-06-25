# Claude Code for Unity

Unity 에디터 안에서 Anthropic의 [Claude Code CLI](https://docs.anthropic.com/en/docs/claude-code)를 채팅 UI 형태로 사용하는 패키지. 코드 읽기·수정, 씬/프로젝트 컨텍스트 첨부, 콘솔 에러 자동 분석, Unity Profiler 데이터 해석까지 한 창에서 처리합니다.

---

## 요구사항

- **Unity**: 2022.3 LTS 이상
- **플랫폼**: Windows 우선 지원 (macOS는 미검증)
- **Claude Code CLI**: 사용자 PC에 설치돼 있어야 함
  ```
  npm install -g @anthropic-ai/claude-code
  ```
  또는 [Claude Code 공식 설치 가이드](https://docs.anthropic.com/en/docs/claude-code/quickstart) 참고
- **인증**: Anthropic 계정 (OAuth 로그인) 또는 API Key

---

## 설치

### Git URL (권장)
Unity > `Window` > `Package Manager` > `+` > `Add package from git URL`:
```
https://github.com/YeongJunSeong/claude-code-unitypackage.git
```

### .tgz tarball
사내 공유나 오프라인 배포가 필요한 경우:
1. 패키지 개발 프로젝트에서 `Tools` > `Claude Code Package` > `Export as UPM tarball (.tgz)` 실행
2. 배포받은 .tgz 파일을 `Package Manager` > `+` > `Add package from tarball`로 import

### .unitypackage (legacy)
권장하지 않지만 호환성이 필요할 때 `Tools` > `Claude Code Package` > `Export as .unitypackage` 사용.

---

## 첫 실행

1. `Window` > `Claude Code` 메뉴로 채팅창 열기
2. 우측 상단 계정 버튼 → 로그인
   - **OAuth**: 브라우저로 Claude 계정 로그인 (권장)
   - **API Key**: console.anthropic.com에서 발급한 키 입력
3. 모델 선택 (Sonnet / Opus 등 드롭다운)
4. 입력창에 프롬프트 작성 → Enter

---

## 주요 기능

### 채팅 UI
- 스트리밍 응답 (실시간 텍스트 + 응답 경과 시간 표시)
- 마크다운 렌더링, 코드 블록 신택스 하이라이팅 (C#, JSON, YAML, Shell, Shader)
- 메시지별 Regenerate / Edit and Resend
- 응답 복사, 마지막 응답 클립보드 복사
- 컨텍스트 사용량 인디케이터

### 첨부 시스템

| 방식 | 설명 |
|---|---|
| **+ 버튼** | 파일/폴더, 선택된 GameObject, 콘솔 에러, 슬래시 명령 메뉴 |
| **`@` 태그** | 입력 중 `@`로 자동완성 — Selection / ActiveScene / ConsoleErrors / ProjectStructure / Scene GameObjects / Assets |
| **드래그 앤 드롭** | Project/Hierarchy에서 채팅창으로 드래그 |
| **이미지 붙여넣기** | `Ctrl+V`로 클립보드 이미지 직접 첨부 (멀티모달) |
| **우클릭 메뉴** | Hierarchy `GameObject > Ask Claude > ...`, Project `Assets > Ask Claude > ...` |

### 슬래시 명령

| 명령 | 동작 |
|---|---|
| `/clear`, `/new` | 현재 세션 초기화 |
| `/sessions` | 히스토리 사이드바 토글 |
| `/login`, `/logout` | 인증 |
| `/copy` | 마지막 응답 복사 |
| `/model <name>` | 모델 변경 |
| `/settings` | 프로젝트 설정 열기 |
| `/help` | 명령어 목록 |

### CLAUDE.md 통합
프로젝트 루트의 `CLAUDE.md` 파일은 Claude가 매 세션 자동으로 읽어들이는 프로젝트 컨텍스트. 코드 컨벤션, 빌드 명령, 도메인 지식 등을 적어두면 응답 품질이 크게 향상됩니다.

채팅창 헤더의 **CLAUDE.md** 드롭다운에서:
- **Create** — Claude가 프로젝트를 분석해 새 CLAUDE.md 작성
- **Read** — 현재 CLAUDE.md 내용 보기
- **Update** — 기존 CLAUDE.md를 분석 후 갱신

### Permission Mode (권한 모드)
헤더 드롭다운에서 선택:

| 모드 | 동작 |
|---|---|
| **Permission Request** (기본) | Claude가 파일 수정 등 도구 호출 시마다 승인 팝업 |
| **Accept Edits** | 도구 호출 자동 승인 (빠른 진행, 위험성 있음) |
| **Plan Mode** | 읽기 전용 — 계획만 제안, 실제 수정은 안 함 |

승인 팝업에서 **Once / Session / Always** 선택 가능. Always는 해당 도구를 영구 허용 목록에 추가.

### DiffView
Claude가 `Write` / `Edit` / `MultiEdit` 도구로 코드를 수정할 때, 승인 팝업에 변경 사항이 diff 형태로 표시. 컨텍스트 라인 ±3 + `...` 생략으로 핵심 변경만 보임.

### Console 에러 통합
Unity 콘솔에 에러가 발생하면 채팅창 헤더에 빨간 뱃지로 카운트 표시. 클릭하면 최근 에러 목록 드롭다운:
- 에러별 **Fix with Claude** 버튼 → 해당 에러를 새 세션으로 분석/수정
- `Tools` > `Claude Code` > `Fix Latest Console Error` 메뉴로도 가능

### Profiler 분석
`Tools` > `Claude Code` > `Analyze Profiler Data` (단축키 `Ctrl+Shift+P`)
- Unity Profiler가 이미 캡처한 데이터를 자동으로 읽어 채팅에 전달
- Claude가 한국어로 평가 + 의심 병목/스파이크/메모리 이슈 설명
- EditorLoop는 "에디터 오버헤드 — 실제 빌드엔 없음"으로 자동 안내

### 세션 히스토리
- 모든 대화가 자동 저장 (`<ProjectRoot>/ClaudeCodeHistory/`)
- 우측 상단 **History** 버튼으로 사이드바 토글, 과거 세션 클릭으로 복원
- 도메인 리로드 중에도 스트리밍 응답 유지 (LockReloadAssemblies)

### 패키지 자기수정 차단
이 패키지가 **UPM(읽기 전용)으로 설치된 프로젝트**에서는, Claude가 패키지 자신의
코드(`Library/PackageCache/com.dnsoft.claudecode/...`)를 건드리지 못하도록 보호합니다.

- **차단**: 패키지 파일의 수정/생성(Edit·Write·MultiEdit·NotebookEdit) 및
  Bash를 통한 복사·이동·삭제(`cp`/`mv`/`rm`/`sed` 등) — 권한 팝업조차 뜨지 않고 자동 거부
- **허용**: 패키지 코드 **읽기**(Read) — Claude가 패키지 동작을 참고/설명할 수 있음
- **개발 모드 예외**: 패키지를 직접 개발하는 embedded/local 설치(`Packages/`에 소스가 있는 경우)에서는
  차단하지 않음 — 패키지 개발이 막히지 않도록

> 패키지 코드를 수정하려면 원본 저장소에서 변경하거나, embedded/local 패키지로 전환해야 합니다.

---

## MCP 도구 (고급)

패키지가 내장 MCP 서버를 자동 실행하여 Claude가 Unity 에디터를 직접 조작할 수 있게 합니다.

| 도구 | 용도 |
|---|---|
| `unity_scene_query` | 현재 씬의 GameObject 계층 조회 |
| `unity_scene_manipulate` | GameObject 생성/이동/삭제, 컴포넌트 조작 |
| `unity_asset_search` | Assets 검색 |
| `unity_profile_start` / `stop` / `status` | 포커스 프로파일링 (자체 ProfilerRecorder 기반) |
| `permission_prompt` | 사용자에게 권한 요청 |

---

## 설정

`Edit` > `Project Settings` > `Claude Code`:
- CLI 경로 확인 (자동 탐지, 환경변수 `CLAUDE_CLI_PATH`로 오버라이드 가능)
- 모델 변경
- 인증 정보
- Permission Mode 기본값

---

## 알려진 한계

- Editor에서만 동작 (런타임 빌드에는 포함되지 않음 — Editor asmdef)
- macOS / Linux는 미검증
- 클립보드 이미지 붙여넣기는 Windows 전용 (PowerShell + System.Windows.Forms)
- Profiler 분석 중 EditorLoop가 항상 큰 비중을 차지함 (에디터 오버헤드라 정상)
- 한 번에 하나의 활성 세션만 지원 (다중 채팅창 미지원)

---

## 문제 해결

### "Claude Code CLI not found"
- `npm install -g @anthropic-ai/claude-code` 재실행
- 또는 `Project Settings > Claude Code`에서 CLI 절대 경로 확인
- 환경변수 `CLAUDE_CLI_PATH` 설정 가능

### OAuth 로그인 시 붙여넣기 안 됨
- Claude CLI v2.1.105+ 회귀 버그. 다음으로 다운그레이드:
  ```
  claude install --force 2.1.104
  ```

### OAuth 로그인 시 브라우저 "사이트 연결 불가 / ERR_UNSAFE_PORT"
일부 CLI 버전은 OAuth 콜백을 `localhost:6667`처럼 브라우저가 차단하는 포트로 받습니다.
승인 후 `http://localhost:6667/callback?code=...` 페이지에서 ERR_UNSAFE_PORT가 뜨면:

- **해결 1 (권장): API Key 방식 사용** — `Project Settings > Claude Code > Authentication`에서
  Method를 `ApiKey`로 바꾸고 console.anthropic.com에서 발급한 키 입력. 콜백 포트 문제를 완전히 회피.
- **해결 2: CLI 버전 변경** — `claude install --force <안정버전>` 후 재로그인.
  (최신 CLI는 보통 호스팅 콜백을 써서 이 문제가 없습니다.)
- **해결 3 (고급): 콜백을 수동 전달** — 브라우저 주소창의 전체 URL을 복사한 뒤,
  새 PowerShell 창에서 실행:
  ```powershell
  Invoke-WebRequest -UseBasicParsing "http://localhost:6667/callback?code=...&state=..."
  ```
  (CLI가 콜백을 기다리는 동안 실행해야 함. 200 OK가 나오면 로그인 완료.)

### 패키지 import 시 컴파일 에러
- Unity 2022.3 미만 버전 → 업그레이드 필요
- 다른 어시스턴트 패키지와 어셈블리 충돌 가능 → asmdef references 확인

### 도메인 리로드로 응답 끊김
- 스트리밍 중 자동으로 `LockReloadAssemblies` 적용됨. 그래도 끊기면 응답 완료 후 컴파일 재시도
- 진행 중 Ctrl+R 등으로 강제 리로드하면 세션 손실 가능

---

## 디렉토리 구조

```
com.dnsoft.claudecode/
├── package.json
├── README.md (이 파일)
├── CHANGELOG.md
├── LICENSE
├── Documentation~/      Unity가 import 시 무시하는 문서 폴더
└── Editor/
    ├── ClaudeCode.Editor.asmdef
    ├── Core/            CLI 프로세스, 세션, 모델 관리
    ├── UI/              채팅창, 다이얼로그, VectorIcons, 마크다운 렌더러
    ├── MCP/             내장 MCP HTTP 서버 + 도구
    ├── Context/         프로젝트/씬/콘솔 컨텍스트 수집
    ├── Approval/        Permission Mode, 권한 캐시
    └── History/         세션 직렬화/저장
```

---

## 라이선스

본 저장소의 `LICENSE` 파일 참고.

---

## 변경 이력

`CHANGELOG.md` 참고.
