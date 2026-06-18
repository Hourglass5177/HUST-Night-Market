# 玩家相机滚轮缩放

## 本次实现

- 新增 `PlayerCameraController`，相机在玩家移动时持续以棋子为画面中心。
- 鼠标滚轮向上拉近、向下拉远，默认限制在初始镜头距离的 `0.40～1.25` 倍：近景可以更靠近棋子，远景不会退出过远。
- 镜头跟随和缩放均使用平滑过渡。
- 初始化时将水平观察方向吸附到最近的世界坐标轴，避免地图道路和地块在画面中倾斜。
- 鼠标位于 `ScrollRect` 或 `Scrollbar` 上时，滚轮只操作 UI，不改变相机缩放；普通 UI 不会阻断地图缩放。
- `PrototypeBootstrap` 初始化棋子后会自动给 `MainCamera` 安装并绑定控制器，无须修改现有测试场景。

## 修改文件

- `Assets/_Project/Scripts/Map/PlayerCameraController.cs`
- `Assets/Sandbox/PrototypeBootstrap.cs`

## Unity 验证

1. 打开 `Assets/Sandbox/TestScenes/S02_Game_HUST_test1.unity` 或 `S03_Game_square_test1.unity`。
2. 进入 Play Mode，确认镜头以玩家棋子为中心。
3. 滚动鼠标滚轮，确认镜头平滑拉近和拉远，并且不会无限缩放。
4. 投骰并移动棋子，确认相机持续跟随。

## 可调参数

控制器由运行时自动添加，默认参数已适配当前测试场景。若以后改为在场景中手工挂载，可在 Inspector 调整缩放比例、滚轮灵敏度、缩放缓动和跟随缓动。
