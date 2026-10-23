# 动作与程序启动接口

`ISwordSlashAction` 与模型提供商无关。Pet 在 UI 线程实现该接口，SwordMotion 提供可独立测试的动作状态；已有转身结束、站稳后才开始 1.13 秒动作。

```csharp
ISwordSlashAction action = pet;
action.HitFrame += (sender, hit) => {
    // 只通知 Agent Runtime；此处不删除、移动或改写文件。
    // hit.RequestId / hit.TargetPosition / hit.FacingDirection
};
action.SwordSlash(new PointF(screenX, screenY), +1);
```

HitFrame 在主斩击的 0.60 秒触发。帧间步长较大也只通知一次；暂停不推进，命中前取消不通知，新请求生成新的 RequestId。

启动程序是独立、显式的工具操作。可选的 SwordAgentRuntime 适配器在 RequestLaunch 被明确调用后登记路径，并只对匹配的命中请求调用启动工具；普通右键动作不会启动程序。

```csharp
var runtime = new SwordAgentRuntime(action, new ProgramLaunchController());
runtime.LaunchCompleted += (sender, result) => {
    // result.Success / result.Error
};
// 由调用方先取得要启动的应用及用户授权，并在 UI 线程调度。
runtime.RequestLaunch(absoluteExePath, new PointF(screenX, screenY), +1);
// 取消等待或结束生命周期：
runtime.CancelPendingLaunch();
runtime.Dispose();
```

ProgramLaunchController 不接收 shell 命令字符串，不做文件移动、删除或内容修改。它只接受现有的绝对 .exe 路径，并返回进程启动成功或错误。桌面图标识别、路径解析、接近目标与具体模型的工具协议留给外部 Runtime。

0.7.11：最高姿势在 0.37–0.47 秒停顿凝剑，命中通知和后续阶段整体顺延 0.10 秒。
