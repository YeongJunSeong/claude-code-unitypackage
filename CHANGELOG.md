# Changelog

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

### Fixed
- CliLogin(OAuth) 모드에서 PC에 설정된 `ANTHROPIC_API_KEY`/`ANTHROPIC_AUTH_TOKEN`
  환경변수가 OAuth 인증을 덮어써 401을 유발하는 문제 방지 (CLI 자식 프로세스 환경에서 제거)
- `PackageInfo` 모호성 컴파일 에러 (UnityEditor vs UnityEditor.PackageManager)
- 입력창 텍스트를 코드로 교체할 때 발생하던 `ArgumentOutOfRangeException` (커서 인덱스 클램프)

## [0.1.0] - 2026-05-15

### Added
- Initial project structure
