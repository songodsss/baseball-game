# 야구 선수 육성 게임

모바일 3D 애니풍 야구 선수 육성 게임 (Unity). 기획서: https://claude.ai/artifact/5Ld5EwwnNgBc5ysa6yVEKR

## 구성

- `src/BaseballSim.Engine` — 경기 시뮬레이션 엔진. Unity 없이 돌아가는 순수 C# (netstandard2.1, C# 9)이라 나중에 Unity 프로젝트 `Assets/` 아래에 그대로 복사해 쓴다.
  - `PlateAppearanceModel` — 투수와 타자 능력치로 한 타석의 결과 확률 계산
  - `GameSimulator` — 9회(최대 12회) 경기 진행, 주자, 투수 교체, 기록
  - `GameEvent` — 경기에서 일어난 일 목록. 3D 경기 화면은 이걸 받아서 연출만 한다
  - `Commentary` — 한국어 중계 문구
  - `RosterGenerator` — 가상 선수와 구단 생성
- `src/BaseballSim.Engine/Career` — 육성 루프
  - `Career` — 하루 단위 진행: 훈련 5종 또는 휴식을 고르면 리그 경기가 진행된다. 피로, 부상, 경기 경험치, 경기별 연봉 지급, 시즌 종료 시 나이와 연봉 재계약. 성장 속도 상수는 모두 이 파일 위쪽에 있다
  - `CareerPlayer` — 키우는 선수: 능력치, 잠재력(상한), 경험치, 피로, 보유금, 장비
  - `Equipment` — 슬롯 5개 × 4단계 장비. 인게임 연봉으로만 구매
  - `League` — 가상 10개 구단, 144경기 일정과 순위
  - `Club` — 은퇴 선수가 들어가는 내 팀, 다음 선수 잠재력을 올려주는 명예 포인트
- `src/BaseballSim.Sandbox` — 시즌을 통째로 돌려 리그 기록과 능력치 영향을 확인하는 콘솔 프로그램

## 실행

```
cd src
dotnet run --project BaseballSim.Sandbox -- 42          # 리그 기록, 능력치 영향, 샘플 경기 (42 = 랜덤 시드)
dotnet run --project BaseballSim.Sandbox -- career 42   # 자동으로 선수 2명의 커리어를 끝까지 플레이 (시즌 36경기)
```

능력치는 1~100, 50이 리그 평균이다. 밸런스는 `PlateAppearanceModel` 맨 위의 기본 확률 상수로 조정한다.
