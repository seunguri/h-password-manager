# 계정 관리 프로그램 (Password Manager)

윈도우 10 이상에서 사용할 수 있는 사내 계정 관리 프로그램입니다.\
<img width="1356" height="693" alt="캡처" src="https://github.com/user-attachments/assets/84c6f1c2-4c75-4598-8df4-4638f21bd8b6" />


## 핵심 기능

- **계정 관리**: 서비스명, 아이디, 비밀번호, 비밀번호 변경일·만료, 비고, 태그 — 추가·조회·수정·삭제 및 목록 순서 변경
- **로컬 저장**: `%APPDATA%\PasswordProtector\accounts.ini`에 저장
- **비밀번호 보호**: INI에 넣기 전 **Windows DPAPI(CurrentUser)** 로 암호화합니다. 같은 PC·같은 Windows 사용자에서만 복호화되며, 다른 사용자나 다른 PC에서는 열람할 수 없습니다.
- **대시보드 새로고침**: 도구 모음의 새로고침으로 디스크의 계정·태그를 다시 읽어 옵니다. 현재 검색어와 태그 필터는 유지됩니다.
- **검색·필터**: 서비스명, 아이디, 태그, 비고로 검색하고 태그로 좁히기
- **단축키**
  - `Ctrl+N`: 새 계정 추가
  - `Enter`: 검색 후 첫 번째 항목 선택
  - `Esc`: 창 최소화
  - `Ctrl+Shift+P`: 창 열기
- **시스템 트레이**: 창을 닫으면 트레이로 최소화 (트레이 아이콘 더블클릭으로 복귀)

## 패키지 추가

```bash
dotnet add package System.Drawing.Common
```

## 빌드 방법

```bash
dotnet build
```

## 실행 파일 생성

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

실행 파일은 `bin/Release/net8.0-windows/win-x64/publish/` 폴더에 생성됩니다.

**실행 파일 공유:** WPF 앱은 exe와 함께 여러 네이티브 DLL이 필요하므로, **publish 폴더 전체**를 압축해서 공유하세요. (`PasswordProtector.pdb`는 디버그용이라 제외해도 됩니다.)

## 데이터 저장 위치

계정 정보는 다음 위치의 INI 파일에 저장됩니다:

`%APPDATA%\PasswordProtector\accounts.ini`

## 사용 방법

1. 프로그램 실행
2. "새 계정" 또는 `Ctrl+N`으로 계정 추가
3. 검색·태그로 항목 찾기
4. 수정·삭제 및 드래그로 순서 조정
5. 창 닫기 시 트레이로 최소화
