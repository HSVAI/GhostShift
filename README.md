# Ghost Shift

세로 화면용 원터치 2D 생존 게임 프로토타입입니다. 화면을 탭하면 플레이어가 반대 레인으로 이동하고, 남겨진 잔상이 잠시 장애물을 막습니다.

## 조작

- 첫 탭: 시작
- 플레이 중 탭: 반대 레인으로 이동
- 게임 오버 후 탭: 다시 시작

## 빌드

Unity 2022.3.74f1 + Android Build Support가 설치된 Linux 환경에서 실행합니다.

```bash
/opt/Unity/Editor/Unity -batchmode -nographics -quit \
  -projectPath /home/ghtnql/GhostShift \
  -executeMethod GhostShift.Editor.BuildGame.BuildAndroid
```

산출물은 `Builds/Android/GhostShift.apk`입니다. `Library`, `Builds`, APK/AAB는 Git에 포함하지 않습니다.
