# Claude Code Unity Package

Unity 에디터 안에서 **Claude Code CLI**를 AI 어시스턴트로 사용할 수 있게 해주는 에디터 확장 패키지.

## 주요 기능

- **채팅 UI** — 스트리밍 응답, 마크다운 렌더링(코드 블록/리스트/굵게/링크), 타이핑 인디케이터
- **인증** — CLI OAuth 로그인 (Unity 내부 다이얼로그) + API 키 직접 입력
- **권한 시스템** — Claude가 도구 사용 시 Unity 다이얼로그로 Allow/Deny (MCP 기반)
- **모델 선택** — Opus / Sonnet / Haiku + 구체적 버전 지정 가능
- **세션 히스토리** — 좌측 사이드바, 클릭으로 이전 대화 복원
- **첨부** — 파일/폴더/GameObject/콘솔 에러 첨부, **이미지 Ctrl+V 붙여넣기 + 썸네일**
- **컨텍스트 자동 수집** — 활성 씬, 선택된 오브젝트, 콘솔 에러, 프로젝트 구조 자동 첨부
- **도메인 리로드 대응** — 스크립트 컴파일 시 세션 자동 복구
- **에러 처리** — 카테고리별 분류, Retry 버튼, 상세 정보 펼치기

## 필수 조건

- **Unity 2022.3 LTS 이상**
- **Claude Code CLI** (`claude.exe`) 설치 — [claude.com/code](https://claude.com/code)
- **OS** — Windows 정식 지원 / macOS, Linux best-effort

## 설치

### Git URL (권장)
Unity Package Manager → `+` → "Add package from git URL" →
```
https://github.com/<your-repo>/com.dnsoft.claudecode.git
```

### 로컬 .tgz 파일
Unity Package Manager → `+` → "Add package from tarball" → `.tgz` 파일 선택

### 로컬 폴더
`Packages/com.dnsoft.claudecode/` 폴더를 프로젝트에 복사 → Unity가 자동 인식

## 사용법

1. **Window > Claude Code** 메뉴로 채팅 창 오픈 (단축키: `Ctrl+Shift+K`)
2. 우상단 **계정 버튼 클릭 → Sign in** (최초 1회)
3. 터미널에서 `/login` 입력 → 브라우저 인증 → 코드 paste
4. Unity 채팅창에 메시지 입력 후 Enter

## 설정

**Edit > Project Settings > Claude Code**:
- **Model** — 사용할 Claude 모델
- **Authentication** — CLI 로그인 vs API 키
- **Approval Mode**:
  - `Manual Approve` (기본) — 모든 도구 사용 시 Unity 다이얼로그
  - `Auto Approve` — 자동 승인 (위험)
  - `CLI Delegate` — CLI 기본 동작에 위임

## 라이선스

MIT
