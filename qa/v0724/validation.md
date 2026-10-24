# 0.7.24 — 跑步速度 150%

运行代码仅修改 src/Configuration/AnimationParameters.cs 的 runSpeed：132 → 198 px/s，基准人物尺寸 300 px；内部参数允许上限调整到 270，其余 23 项值保持。普通跑步、施法跑位、跑动搬运均通过现有 RunGait.SpeedAt300 读取新值。步频继续跟随距离，不改变步幅 / 骨长 / 美术轮廓；普通走路仍为 36 px/s，加速与刹停曲线不改。

测试文件更新：tests/FixedParametersCheck.cs 对原冻结快照只应用本次授权的 1.5 倍速度；tests/RunCheck.cs 更新目标速度；tests/CastingCheck.cs 新增前后版速度对照并生成新版完整过程预览。VERSION、README.md、PROJECT_STATE.md、Release.ps1 同步到 0.7.24-preview。

验证：
- 构建成功。
- CastingCheck speed：4 组聚焦检查通过。双向 220 / 300 / 400 px 稳态速度均为 0.7.23 的 150%，走路仍为 36 px/s；落地脚点跟随距离。上下方目标在 30 / 60 / 120 fps 均能自动跑位、刹停、转向施法并命中一次；边缘、取消与目标丢失继续通过。
- FixedParametersCheck：11 项通过，剩余 23 个固定参数不变，未修改用户参数文件。
- JourneyCheck：上下方两个完整过程均只命中一次；已查看新速度下的原生连续取样。无特效五姿势沿用未修改的旧版渲染，页面已说明。
- 没有重跑无关全量回归或访问真实桌面目标；仅改速度入口，其余动作、素材与特效源代码不变。

本版仍只在当前屏幕横向寻找站位。窗口 / 文件变化取消、无空间提示、45 秒等待上限不变。混合 DPI 多屏尚待硬件验收，原有细小美术边缘保持。
