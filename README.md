# Ghost Shift

세로 화면용 원터치 2D 생존 게임 프로토타입입니다. 화면을 탭하면 플레이어가 반대 레인으로 이동하고, 남겨진 잔상이 잠시 장애물을 막습니다.

## 조작

- 첫 탭: 시작
- 플레이 중 탭: 반대 레인으로 이동
- 게임 오버 후 탭: 다시 시작

## 빌드

Unity **2022.3.62f3** + Android Build Support + Personal 라이선스를 사용합니다.
2022.3.74f1은 Enterprise/Industry용 Extended LTS이므로 Personal에서 실행되지 않습니다.

```bash
./build.sh test
./build.sh linux
./build.sh android
./build.sh webgl
```

산출물은 `Builds/Android/GhostShift.apk`와 `Builds/WebGL/`입니다. `Library`, `Builds`, APK/AAB는 Git에 포함하지 않습니다.
배포 페이지와 웹 플레이 주소는 <https://hsvai.github.io/GhostShift/> 및 <https://hsvai.github.io/GhostShift/play/>입니다.

첫 배포본은 Android 6.0 이상 / ARM64 / OpenGL ES 3 기기용 **디버그 키로 서명한 테스트 APK**입니다.
스토어 배포 전에는 별도 릴리스 키 보관, 스토어 정책 확인, 실기기 테스트가 필요합니다.
SDK/JDK 11/NDK r23b는 해당 Unity 에디터의 AndroidPlayer 폴더를 사용합니다.
SDK의 플랫폼·빌드 도구는 기존 `/home/ghtnql/Android/Sdk`를 링크로 재사용하고,
명령줄 도구만 Java 11 호환 버전으로 분리했습니다. 기존 SDK의 최신 도구는 변경하지 않았습니다.

## 게임 규칙

- 생존 시간 1초당 10점, 잔상이 장애물을 잡으면 25점 추가.
- 잔상은 0.65초 유지되며 한 번만 방어합니다. 레인마다 하나만 존재합니다.
- 시간이 지날수록 장애물 속도와 빈도가 증가합니다.
- 최고 점수와 효과음 설정은 기기에 저장됩니다. 계정·광고·백엔드는 없습니다.
- Android 뒤로가기 또는 데스크톱 Escape 키를 2초 안에 두 번 누르면 앱을 종료합니다.
- 홈 화면 전환처럼 앱이 백그라운드로 이동하면 현재 플레이를 일시정지하고 복귀 시 탭으로 재개합니다.
- 상단 SFX ON/OFF로 효과음을 켜거나 끕니다. 데스크톱에서는 Space도 탭으로 동작합니다.

## 검증

`./build.sh test`는 보너스 보존과 빠른 장애물의 연속 충돌 판정을 포함한 규칙 검증 10개를 실행합니다.
Linux 빌드 후 다음 명령으로 실제 플레이어에서 시작/레인 이동/일시정지/재개/종료/재시작을 확인합니다.

```bash
DISPLAY=:99 ./Builds/Linux/GhostShift.x86_64 -screen-fullscreen 0 \
  -screen-width 450 -screen-height 800 -force-glcore --ghostshift-smoke \
  -logFile "$PWD/Logs/player-smoke.log"
```

스크린샷은 `Builds/QA`에 기록됩니다. Android 실기기에서의 터치감과 화면 확인은 별도로 필요합니다.
