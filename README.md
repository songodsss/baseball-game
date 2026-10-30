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
  - `StoryEvents` — 성장 스토리 이벤트 16종(코치의 특별 지도, 레전드의 방문, 슬럼프, 팬레터, 광고 제의, 재활 등). 시즌 길이와 상관없이 시즌당 8번 안팎, 고른 답에 따라 경험치·피로·보유금·사기가 바뀐다. 사기는 경기 컨디션에 더해지고 하루 1씩 원래대로 돌아온다
  - `Legends` — 2026년 이전 가상 리그의 레전드 5명(미스터 홈런 장태산, 안타 제조기 민경호 등). 커리어마다 한 번씩 "레전드의 방문" 이벤트로 찾아와 자기 주특기를 가르쳐 준다. 내가 키운 선수는 등장하지 않는다
  - 연도 — 선수마다 2026년부터 시작한다(`Career.Year`). 다음 선수가 앞 선수의 20년 뒤로 이어지지 않는다
  - `Facilities` — 개인 시설 3종(트레이너실: 피로 회복, 재활 센터: 부상 확률·기간, 컨디셔닝 센터: 노쇠화), 각 3단계. 장비를 다 산 뒤 연봉을 쓸 곳. 능력치를 직접 올리지는 않는다
  - `Awards` — 시즌 타이틀: 타격왕·출루율 1위(규정 타석 경기당 3.1), 홈런왕, 타점왕, 최다안타, MVP(OPS + 팀 승률 조금), 신인왕(내 선수 첫 시즌). `League.Batting`이 리그 전체 타자 기록을 모은다
  - `Postseason` — 정규시즌 상위 4팀 가을야구: 준결승 1-4위, 2-3위 3전 2선승, 챔피언 결정전 5전 3선승. 진출하면 연봉 비율로 보너스
  - `Achievements` — 커리어 업적 18종(첫 안타, 100홈런, 3할 타자, MVP, 우승 반지, 억대 연봉, 철인 등). 은퇴 선수도 간직해서 내 팀 전체 수집 현황이 된다
  - 구단 고유 색 — `League.TeamColors`. 선수 등록 때 입단 팀을 고르면 그 팀 유니폼을 입는다. 경기 화면에서 내 선수는 머리 위 금색 표시로 구분
  - `Stats` — 타율, 출루율, 장타율, OPS, 통산 기록. `League.GamesBehind`는 승차
  - `Career.Sign` / `SeasonSummary.Offers` — FA(자유계약). 8시즌을 뛰고 나면(그리고 계약이 끝날 때마다) 내 구단과 다른 3개 구단이 연봉·기간을 제안한다. 다른 구단이 더 많이 주고, 나이가 어릴수록 계약이 길다(30세 이하 4년, 34세 이하 2년, 그 뒤 1년). 계약 기간에는 연봉이 고정된다
  - `SaveGame` — 내 팀과 진행 중인 커리어(리그, 순위 포함)를 저장/불러오기. 버전 번호가 붙은 바이너리라 Unity에 JSON 라이브러리가 필요 없고, 나중 버전도 옛 저장을 읽을 수 있다
- `src/BaseballSim.Sandbox` — 시즌을 통째로 돌려 리그 기록과 능력치 영향을 확인하는 콘솔 프로그램
- `src/BaseballSim.Tests` — 엔진 테스트 (xUnit)

- `unity/` — Unity 프로젝트 (Unity 6). 엔진 폴더를 로컬 패키지로 불러온다 (`unity/Packages/manifest.json`)
  - `Assets/Scripts/GrowthScreen.cs` — 육성 화면 프로토타입. 씬 설정 없이 Play만 누르면 화면이 코드로 만들어진다. 처음 켜거나 은퇴하면 선수 이름과 시즌 길이를 정하는 등록 화면이 뜬다
  - `Assets/Scripts/RecordsScreen.cs` — 기록 화면: 리그 순위(승률, 승차), 내 이번 시즌·시즌별·통산 기록, 내 팀 은퇴 선수
  - `Assets/Scripts/MatchScreen.cs` — 경기 화면. 육성 화면의 "오늘 경기 보기"로 연다. 점수판, 아웃·주자 표시, 중계, 일시정지 / 속도 x1·x2·x4 / 내 타석으로 / 결과 보기
  - `Assets/Scripts/MatchField.cs` — 경기 화면의 3D 구장. 기본 도형(캡슐 선수, 공, 베이스, 펜스)으로 투구, 타구, 수비수 쫓아가기, 주루를 연출. 내 선수는 금색. 아트 작업 때 3D 애니풍 캐릭터 모델로 교체 예정
  - `Assets/Scripts/SaveStore.cs` — 폰 내부 저장소에 저장 파일을 쓴다. 하루 진행, 장비 구입, 시즌 길이 변경, 은퇴 때마다, 그리고 앱을 내리거나 끌 때 자동 저장
  - `Assets/Scripts/Monetization.cs` — 보상형 광고(`IRewardedAds`)와 인앱 결제(`IStore`) 연결 지점. 지금은 테스트용이라 바로 지급하고 실제 청구는 없다. 광고는 하루 3번, 한 번에 피로 30 회복. 결제로 팔 상품은 아직 정하지 않았다. 실제 SDK(AdMob/Unity Ads, Unity IAP)는 이 인터페이스 뒤에 붙인다
  - 육성 화면 위쪽 "설정"(경기 기본 속도, 도움말 다시 보기, 데이터 초기화). 행동 버튼 위에 오늘 상대 표시
  - `Assets/Scripts/SafeArea.cs` — 노치·둥근 모서리를 피해 화면 배치
  - `Assets/Editor/AndroidBuild.cs` — 메뉴 Baseball → 안드로이드 설정 적용 / 안드로이드 APK 빌드. 세로 고정, 패키지명 `com.songodsss.baseballgame`, IL2CPP ARM64, 빈 씬 자동 생성
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

## 폰에 설치 (안드로이드)

1. Unity Hub에서 Unity 6 설치 시 Android Build Support 모듈 포함
2. 프로젝트를 열고 메뉴 Baseball → 안드로이드 APK 빌드. `unity/Builds/Android/baseball.apk`가 생긴다
3. 폰에서 개발자 옵션 → USB 디버깅을 켜고 연결해 설치하거나, APK 파일을 폰으로 옮겨 설치

## 실행 (엔진만)

```
cd src
dotnet run --project BaseballSim.Sandbox -- 42          # 리그 기록, 능력치 영향, 샘플 경기 (42 = 랜덤 시드)
dotnet run --project BaseballSim.Sandbox -- career 42   # 자동으로 선수 2명의 커리어를 끝까지 플레이. 끝에 144, 72, 36 중 시즌 길이를 붙일 수 있음 (기본 36)
dotnet test BaseballSim.Tests                           # 엔진 테스트
```

능력치는 1~100, 50이 리그 평균이다. 밸런스는 `PlateAppearanceModel` 맨 위의 기본 확률 상수로 조정한다.
