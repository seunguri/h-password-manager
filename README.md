# 계정 관리 프로그램 (Password Manager)

윈도우 10 이상에서 사용할 수 있는 사내 계정 관리 프로그램입니다.\
<img width="1356" height="693" alt="캡처" src="https://github.com/user-attachments/assets/84c6f1c2-4c75-4598-8df4-4638f21bd8b6" />


## 기능

- 계정 정보 관리 (서비스명, 아이디, 비밀번호, 마지막 비밀번호 변경일, 비고, 태그)
- CRUD 기능 (생성, 읽기, 수정, 삭제)
- INI 파일에 계정 정보 저장
- 검색 기능 (서비스명, 아이디, 태그, 비고 검색)
- 단축키 지원
  - `Ctrl+N`: 새 계정 추가
  - `Enter`: 검색 후 첫 번째 항목 선택
  - `Esc`: 창 최소화
  - `Ctrl+Shitf+P`: 창 열기
- 시스템 트레이 지원 (창 닫기 시 트레이로 최소화)

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

**실행 파일 공유:** WPF 앱은 exe와 함께 여러 네이티브 DLL이 필요하므로, **publish 폴더 전체**를 압축해서 공유하세요. (`PasswordProtector.pdb`는 디버그용이라 제외해도 됩니다.)해

## 데이터 저장 위치

계정 정보는 다음 위치의 INI 파일에 저장됩니다:
`%APPDATA%\PasswordProtector\accounts.ini`

## 사용 방법

1. 프로그램 실행
2. "새 계정" 버튼 클릭 또는 `Ctrl+N`으로 계정 추가
3. 검색창에서 계정 검색
4. 계정 항목을 드래그하여 순서 변경
5. 수정/삭제 버튼으로 계정 관리
6. 창 닫기 시 시스템 트레이로 최소화 (더블클릭으로 다시 열기)
