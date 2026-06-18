# 关卡背景音乐

## 本次实现

- 使用 `Spring (It's A Big World Outside).mp3` 作为关卡背景音乐。
- 进入游戏关卡后自动播放，启用循环和二维音频，默认音量为 `0.4`。
- 主菜单不创建播放器，因此不会播放背景音乐。
- 离开关卡时播放器随场景销毁，音乐自动停止。
- MP3 使用 Streaming 方式读取，减少长音乐的运行时内存占用。

## 修改文件

- `Assets/_Project/Scripts/Audio/LevelBgmConfig.cs`
- `Assets/_Project/Scripts/Audio/LevelBgmPlayer.cs`
- `Assets/_Project/Resources/Audio/LevelBgmConfig.asset`
- `Assets/Sandbox/PrototypeBootstrap.cs`

## Unity 验证

1. 进入主菜单，确认没有背景音乐。
2. 开始游戏进入任意关卡，确认音乐自动播放。
3. 等待音乐结束或拖动播放进度测试，确认能够循环。
4. 返回主菜单，确认音乐停止。
