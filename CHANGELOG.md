# Changelog

## [0.2.4] - 2026-07-22

### Added
- 컴파일 에러 감지 + 자동 Fix 토스트: 컴파일러 에러(CS...)를 감지해 에러 뱃지에
  반영하고, 컴파일 실패 순간 채팅창에 "컴파일 에러 N개 — Fix with Claude" 토스트 표시.
  클릭 한 번으로 에러 목록 + 관련 파일이 첨부된 수정 세션 시작.
  (기존에는 런타임/콘솔 에러만 감지 가능했음)
- `Tools > Claude Code > Check for Updates` 메뉴: 업데이트 즉시 수동 확인

### Changed
- 새 버전 확인 주기: 하루 1회 → **에디터 세션당 1회** (Unity를 켤 때마다 1회 확인,
  도메인 리로드에는 재확인 안 함). 릴리즈 다음 재시작에 바로 알림이 뜨도록 개선.

## [0.2.3] - 2026-07-22

### Added
- 새 버전 알림: 하루 1회 GitHub의 최신 버전을 확인해, 새 버전이 있으면 채팅창
  상태바에 `NEW v<버전>` 뱃지 표시. 클릭하면 Package Manager가 해당 패키지가
  선택된 상태로 열려 바로 Update/재설치 가능. (embedded/local 개발 설치에서는 미동작)

## [0.2.2] - 2026-07-21

### Added
- Fable 5 모델을 모델 드롭다운에 추가 (`claude-fable-5`)
- 작업량(reasoning effort) 선택 기능: 입력줄 버튼 클릭 → 슬라이더 팝업에서
  낮음/중간/높음/매우 높음/최대 선택 (`--effort` CLI 플래그로 전달).
  "기본값 사용" 시 플래그 생략되어 CLI/계정 기본값 적용. 설정은 세션 간 유지.

## [0.2.1] - 2026-06-26

### Added
- 메시지별 토큰/비용 표시: 각 어시스턴트 응답 하단에 `in/out` 토큰과 비용을 표시
  (마우스 오버 시 input/cache write/cache read/output 상세). 히스토리에도 보존.

### Fixed
- Windows에서 npm으로 설치한 CLI(`...\npm\claude` 확장자 없는 셔임)를 실행하지 못하던 문제
  ("%1은(는) 올바른 Win32 응용 프로그램이 아닙니다"). `.cmd`/`.exe`를 우선 선택하고
  `.cmd`는 `cmd.exe`로 실행하도록 수정. → 다른 작업자 PC에서 로그인 감지/메시지 전송이 안 되던 문제 해결.

### Docs
- OAuth 로그인 시 `ERR_UNSAFE_PORT`(localhost 콜백 포트 차단) 문제해결 가이드 추가.

## [0.2.0] - 2026-06-25

### Added
- Opus 4.8 모델을 모델 드롭다운에 추가
- 패키지 자기수정 차단: UPM(읽기 전용)으로 설치된 경우 Claude가 패키지 자신의
  파일을 수정/복사/이동/삭제하지 못하도록 권한 단계에서 자동 거부 (`PackageSelfGuard`).
  Edit/Write/MultiEdit/NotebookEdit 및 Bash(cp/mv/rm/sed 등) 우회 모두 차단.
  읽기(Read)는 허용. 패키지를 개발 중인 embedded/local 설치에서는 차단 안 함.
- Unity Profiler 데이터 분석 메뉴 (`Tools > Claude Code > Analyze Profiler Data`, `Ctrl+Shift+P`)
- UPM tarball(.tgz) export 메뉴 (`Tools > Claude Code Package > Export as UPM tarball`)
- 패키지 README 문서

### Changed
- 마크다운 렌더링 시 연속된 텍스트(단락·제목·리스트)를 하나의 선택 가능한 요소로
  합쳐, 블록 경계에서 드래그 선택이 끊기던 문제 개선. 코드 블록은 별도 박스로 유지.

### Fixed
- CliLogin(OAuth) 모드에서 PC에 설정된 `ANTHROPIC_API_KEY`/`ANTHROPIC_AUTH_TOKEN`
  환경변수가 OAuth 인증을 덮어써 401을 유발하는 문제 방지 (CLI 자식 프로세스 환경에서 제거)
- `PackageInfo` 모호성 컴파일 에러 (UnityEditor vs UnityEditor.PackageManager)
- 입력창 텍스트를 코드로 교체할 때 발생하던 `ArgumentOutOfRangeException` (커서 인덱스 클램프)

## [0.1.0] - 2026-05-15

### Added
- Initial project structure
