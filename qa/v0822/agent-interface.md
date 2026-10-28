# 0.8.22 接口与复现

`LeapFeathers.Draw(Graphics, width, size, ground, LeapPose)` 是 A2 上下跃动的纯绘图函数。沿用 `LeapPose.Petals` 作为透明度包络，`Time` 和 `Flare` 控制漂浮与扩散；字段名为历史兼容保留，不代表 A2 仍绘制花瓣。

`WalkArt.DrawLeap` 根据构造时的 CharacterForm 分派：A1 调用原 DrawPetals，A2 调用 LeapFeathers。特效在角色后方绘制。不要在这里推进动作或执行桌面交互。

衣装切换继续由 FormTransition 同时绘制同一时刻的 A1/A2，再混合整帧；因此特效与衣装一起过渡，动作不停止。

构建 App：`Build.ps1 -Target App`。
专项检查：`Build.ps1 -Target LeapFeatherCheck` 后运行 `LeapFeatherCheck.exe`。
画面对照使用提供的 0.8.21 基线程序。源码未依赖新增图片，也不需要图像生成服务。
