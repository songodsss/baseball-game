# 야구 선수 육성 게임

모바일 3D 애니풍 야구 선수 육성 게임 (Unity). 기획서: https://claude.ai/artifact/5Ld5EwwnNgBc5ysa6yVEKR

## 구성

- `src/BaseballSim.Engine` — 경기 시뮬레이션 엔진. Unity 없이 돌아가는 순수 C# (netstandard2.1, C# 9)이라 나중에 Unity 프로젝트 `Assets/` 아래에 그대로 복사해 쓴다.
  - `PlateAppearanceModel` — 투수와 타자 능력치로 한 타석의 결과 확률 계산
  - `GameSimulator` — 9회(최대 12회) 경기 진행, 주자, 투수 교체, 기록
  - `GameEvent` — 경기에서 일어난 일 목록. 3D 경기 화면은 이걸 받아서 연출만 한다
  - `Commentary` — 한국어 중계 문구
  - `MatchReplay` — 끝난 경기를 한 타석씩 넘기며 점수판(이닝별 득점, 안타)과 주자 이동(어느 루에서 어디로, 득점, 아웃)을 계산. 경기 화면은 이걸 받아 그리기만 한다
  - `RosterGenerator` — 가상 선수와 구단 생성
- `src/BaseballSim.Engine/Career` — 육성 루프
  - `Career` — 하루 단위 진행: 훈련 5종 또는 휴식을 고르면 리그 경기가 진행된다. 피로, 부상, 경기 경험치, 경기별 연봉 지급, 시즌 종료 시 나이와 연봉 재계약. 성장 속도 상수는 모두 이 파일 위쪽에 있다
  - `CareerPlayer` — 키우는 선수: 능력치, 잠재력(상한), 경험치, 피로, 보유금, 장비
  - `Equipment` — 슬롯 5개 × 4단계 장비. 인게임 연봉으로만 구매
  - `League` — 가상 10개 구단, 일정과 순위. 시즌 길이 144/72/36경기는 시즌 사이에 바꿀 수 있고, 짧은 시즌일수록 하루 성장과 보상이 커져 커리어 흐름은 같다
  - `Club` — 은퇴 선수가 들어가는 내 팀, 다음 선수 잠재력을 올려주는 명예 포인트
- `src/BaseballSim.Sandbox` — 시즌을 통째로 돌려 리그 기록과 능력치 영향을 확인하는 콘솔 프로그램
- `src/BaseballSim.Tests` — 엔진 테스트 (xUnit)

- `unity/` — Unity 프로젝트 (Unity 6). 엔진 폴더를 로컬 패키지로 불러온다 (`unity/Packages/manifest.json`)
  - `Assets/Scripts/GrowthScreen.cs` — 육성 화면 프로토타입. 씬 설정 없이 Play만 누르면 화면이 코드로 만들어진다
  - `Assets/Scripts/MatchScreen.cs` — 경기 화면. 육성 화면의 "오늘 경기 보기"로 연다. 점수판, 아웃·주자 표시, 중계, 일시정지 / 속도 x1·x2·x4 / 내 타석으로 / 결과 보기
  - `Assets/Scripts/MatchField.cs` — 경기 화면의 3D 구장. 기본 도형(캡슐 선수, 공, 베이스, 펜스)으로 투구, 타구, 수비수 쫓아가기, 주루를 연출. 내 선수는 금색. 아트 작업 때 3D 애니풍 캐릭터 모델로 교체 예정
  - `Assets/Scripts/UiKit.cs` — 코드로 UI를 만드는 도우미. 아트 작업 때 디자인된 프리팹으로 교체 예정

## Unity 설치 (처음 한 번)

1. https://unity.com/download 에서 Unity Hub 설치 후 로그인 (개인은 무료 Personal 라이선스)
2. Hub → Installs → Install Editor → Unity 6 (`unity/ProjectSettings/ProjectVersion.txt`의 6000.0 버전대 LTS 권장)
3. 설치 모듈에서 폰 테스트용으로 **Android Build Support**(OpenJDK, SDK & NDK 포함) 체크. 아이폰은 Mac + Xcode가 있어야 해서 나중에

## Unity에서 열기

1. Unity Hub → Add → Add project from disk → 저장소의 `unity` 폴더 선택
2. 에디터 버전을 물으면 설치한 Unity 6 버전 선택
3. 열리면 상단 Play 버튼. Game 창 해상도를 1080x1920(세로)으로 맞추면 폰처럼 보인다
4. 훈련을 하나 고르면 하루 경기가 진행된다. 아래 "오늘 경기 보기"를 누르면 그 경기를 3D로 다시 본다

## 실행 (엔진만)

```
cd src
dotnet run --project BaseballSim.Sandbox -- 42          # 리그 기록, 능력치 영향, 샘플 경기 (42 = 랜덤 시드)
dotnet run --project BaseballSim.Sandbox -- career 42   # 자동으로 선수 2명의 커리어를 끝까지 플레이. 끝에 144, 72, 36 중 시즌 길이를 붙일 수 있음 (기본 36)
dotnet test BaseballSim.Tests                           # 엔진 테스트
```

능력치는 1~100, 50이 리그 평균이다. 밸런스는 `PlateAppearanceModel` 맨 위의 기본 확률 상수로 조정한다.
