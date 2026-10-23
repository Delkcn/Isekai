# PROJECT_STATE

更新：2026-10-24。当前版本 **0.7.13-preview**，项目目录名仍为 `IsekaijouchoPet-v0.6.0`。本文记录当前实现和用户已确认的约束，不将历史尝试视为待执行指令。

## 0.7.13 当前：互动闭眼右侧睫毛

- CoveredEyeRig 在已闭合的右眼可见眼缘补上细小深棕色弧线和短外睫毛，使用原画局部坐标、边缘抗锯齿和刘海裁切。SoftRig 在头部运动采样坐标中绘制，与头部绑定；随闭眼程度渐显，仅用于摸头/挥手等互动，不影响待机、普通眨眼及冻结睡眠。
- 6 项针对性检查通过，并查看实际面部放大预览：220/300/400 尺寸睫毛可见，无新增暖红色像素；眼部局部变化、原映射及睡眠样本保持。挥剑最高点停顿 0.1 秒、剑角度、命中时序和箭尖尾迹均不改。
- 预览 qa/v0713/review.html，发布 dist/IsekaijouchoPet-v0.7.13-preview.zip。全身原有发尾/衣摆细小边缘残留不在本次修改范围。

## 0.7.12 历史：互动闭眼外眼角残留

- 修正 CoveredEyeRig 的互动闭眼注册范围：原图 x852–860 的暖红色外眼角笔触位于前一版 x864–876 眼裂范围之外，现在独立按列收拢；保留两区域之间及上方的刘海。原素材不改，待机、普通眨眼、睡眠继续使用原映射。
- 0.7.11 的最高点 0.10 s 停顿凝剑、0.7.5 人物姿势/剑角度、命中 0.60 s、0.33 s 箭尖尾迹保持不变。
- 6 项针对性检查通过；220/300/400 尺寸摸头/挥手外眼角明显暖红像素从 4–25 个降至 0–1 个，剩余至多一个混色边缘像素，不宣称全部颜色像素绝对归零。已查看实际面部放大与全身截图，未运行全量回归。
- qa/v0712/review.html 提供 0.7.11/0.7.12 闭眼前后对照，左右挥剑预览沿用未改的 0.7.11 渲染。发布 dist/IsekaijouchoPet-v0.7.12-preview.zip。

## 0.7.11 历史：最高点停顿凝剑与互动右眼闭合

- 保持 0.7.5 的姿势、握持和剑角度。SwordMotion 将 0.37–0.47 s 映射至同一最高姿势，独立的显剑透明度与蓝色星芒在这 0.10 s 内推进至完全显现，再沿原曲线挥剑。主斩 0.47–0.63 s，HitFrame 0.60 s，总长 1.13 s，消散顺延至 0.88–1.13 s。刀风继续采样真实剑尖。
- 摸头/挥手的刘海下右眼：原 CoveredEyeRig 只检测红色虹膜，未覆盖全部眼白与眼缘。新增依据当前 v0610 正面素材注册的完整眼裂边界，仅由 SoftRig 在互动闭眼时使用，保持上方刘海。普通眨眼与睡眠继续走原映射；未编辑 PNG 素材或睡眠曲线。
- 6 项针对性检查通过（InteractionCheck）：原姿势时间映射、停顿与完整显剑、30/60/120fps 单次命中时刻、220/300/400尺寸的摸头/挥手右眼、映射单调与刘海、待机/普通眨眼及两张睡眠样本逐像素对照。未运行完整回归。极小尺寸保留至多一个抗锯齿色边像素，检查不等于宣称所有美术细节已验收。
- 已看左右停顿凝剑/斩击/消散和摸头/挥手面部放大预览。qa/v0711/review.html 提供左右播放、关闭特效及面部前后对照；其余动作与 0.33 s 箭尖尾迹保持。旧发尾/衣摆少量边缘残留仍在。
- 发布 dist/IsekaijouchoPet-v0.7.11-preview.zip，EXE + 散资源，未重建 character.pack。

## 0.7.10 历史：完整恢复 0.7.5 挥剑，最高姿势才显剑

- 最新要求覆盖 0.7.6 正手绑定及 0.7.9 的 135° 设置。仅恢复挥剑相关的 SlashSkeleton、SwordRig 武器轴与 PaintedSwordArt 手掌表面至 qa/v0706/baseline 中已核实 VERSION 为 0.7.5-preview 的源码；人物姿势和剑角度均与该版一致。其余动作、全局发长、散素材加载及 0.33 s 箭尖尾迹保留当前版。
- SwordMotion 的动作曲线、五阶段节奏及 0.50 s HitFrame 不改；剑透明度单独控制，抬手期间为零，在最高关键姿势 0.37 s 才开始显剑，用 0.025 s 快速凝聚。蓝色星芒同步移至该时刻；挥斩刀风仍采样真实剑尖，消散时机仍为 0.78–1.03 s，不增加停顿。
- 6 项针对性检查通过：左右 200 Hz 非透明度姿态字段对照；显剑边界与抬手阶段无特效；十个关键姿势的肩肘腕/握点/剑尖；剑角度及按新透明度绘制的 0.7.5 全画面逐像素对照；刚性握持；实际刀风像素贴合恢复后的剑尖弧线。未重复完整回归。
- 已查看左右关键姿势、握持放大及显剑/刀风/消散截图，提供 qa/v0710/review.html 和左右可播放 HTML，支持关闭特效、骨骼线与慢速播放。既有左向远侧手遮挡、少量发尾/衣摆边缘残留保留，不以本次对照通过代替美术验收。
- 发布：dist/IsekaijouchoPet-v0.7.10-preview.zip，使用 EXE + 12 个散资源文件，不重建 character.pack。

## 0.7.9 历史：135° 握持与特效跟随

- SlashSkeleton 将主体阶段腕掌偏转设为 −45°，使前臂方向（肘→腕）与剑刃方向（握点→剑尖）的夹角为 135°；进入/复位随 Amount 平滑过渡。保留 0.7.5 肩肘腕节点轨迹与 0.7.6 正手绑定，手掌与剑一起旋转。
- 蓝色凝聚/消散星芒与刀风继续从当前骨骼握点和实际剑尖轨迹取样。箭尖尾迹仍为 0.33 s，其他动作和素材不变。
- 6 项针对性检查全部通过，包含左右实际刀风像素与新剑尖弧线对齐。已查看左右特效、关键姿势和握持放大，未重复完整回归。记录为 qa/v0709/FocusedCheck.txt，预览为 qa/v0709/review.html。
- 已知起剑剑刃经过脸前、左向远侧手遮挡及少量旧发尾/衣摆边缘残留仍存在，本轮只调整指定夹角。
- 发布包 dist/IsekaijouchoPet-v0.7.9-preview.zip 使用 EXE + 12 个散资源文件，不重建 character.pack。

## 0.7.8 历史：散文件、短尾迹与握持夹角

- 用户要求减少检查，随后明确要求打包。当前交付 ZIP 内的 EXE + assets；原 character.pack 的 12 个条目已核对/展开，原包移动到 qa/v0708/baseline/character.pack 备份。AssetStore 优先读取 assets，兼容旧包作为缺少散文件时的后备。已提供 dist/IsekaijouchoPet-v0.7.8-preview.zip，未重建 character.pack。
- MagicArrow 每段箭尖尾迹总寿命为 0.33 s：保持 0.22 s 后以 0.11 s 淡出。发射点仍为箭尖，箭速、爆炸和其他特效不变。
- SlashSkeleton 的手腕朝向在当前动作上额外偏转 −10°，限制范围为 −35° 到 +30°，随 Amount 平滑进入/退出；剑与前臂夹角增大 10°。肩、肘、腕节点位置保持，手掌与剑一起转动，沿用正手绑定。SwordMotion 的 0.7.5 原曲线不改。
- 本轮仅 6 项针对性检查通过：散文件加载、0.33 s 生命周期、两向箭尖连接、十关键姿势的肩肘腕位置与 0.7.7 一致、角度增加、刚性握持。另实际查看左右五姿势、握持放大与尾迹三时刻；未重复全量回归。
- 本地运行 IsekaijouchoPet.exe；预览 qa/v0708/review.html。已知起剑时剑刃经过脸前、远侧手遮挡及旧发尾/衣摆边缘残留仍保留。本轮未处理这些既有问题。

## 0.7.7 历史：恢复手臂与箭尖尾迹

- 最新要求为 **0.7.5 手臂动作 + 0.7.6 正手握持**，覆盖上一版横斩手臂曲线。SwordMotion 原文件恢复至 0.7.5，肩肘腕与重心/时序完全一致；SwordRig、PaintedSwordArt 保留 0.7.6 正手绑定与握点。菜单使用“单手挥剑”。
- 箭尾迹改由箭尖历史位置产生，移除一个箭长的后移，发射到命中位置连续连接。箭速、各段 1 秒保留和 0.4 秒渐隐、螺旋及爆炸不变。
- 412 项通过：剑 99（含左右 200 Hz 全部姿态字段与 0.7.5 对照）、箭尖尾迹 16、其他动作逐像素对照 297。已查看左右五姿势/握持与箭尾迹渲染。正手使剑尖方向相对 0.7.5 反转，起剑时右向剑刃经过脸前区域；保留指定手臂路径供用户验收。
- 资源包与头发代码不变，旧发尾/衣摆少量边缘残留仍存在。预览为 `qa/v0707/sword-review.html`，验证为 `qa/v0707/validation.md`，发布包为 `dist/IsekaijouchoPet-v0.7.7-preview.zip`。

## 0.7.6 历史：正手横斩

- 最新反馈将动作从斜斩改为**正手横斩**，覆盖先前斜向下劈的要求。重设 SwordMotion 的肩肘腕关键姿势，主斩击以横向浅弧扫过，保持固定骨长、五阶段节奏及原站姿出入。
- 确认 0.7.5 的根因：武器轴仍是腕掌方向 +90°，只翻转握拳画面无法纠正反手。当前 SwordRig 改为 −90° 的正手绑定，PaintedSwordArt 使用近侧手背/远侧手心。新增有向腕掌—剑刃约束，防止再次仅靠画面翻转绕过错误绑定。
- 300 身高参数下主斩击剑尖横向跨度 89.72 px、纵向 13.64 px。左右 103 项剑动作检查通过；全局短发开启时，另 297 帧其他动作与 0.7.5 逐像素一致。已看左右五姿势、主斩击五采样与握持放大，完成一次轨迹视觉微调。
- 菜单改为“单手挥剑 · 横斩”。蓝色星芒、残影、0.50 s HitFrame、独立接口、箭速及原资源包保持。头发分层代码没有修改，0.7.5 的少量边缘残留继续列为已知问题；左向远侧持剑手在部分阶段被躯干遮挡。
- `qa/v0706/sword-review.html` 为本轮预览入口，`qa/v0706/validation.md` 记录修改与验证，发布包 `dist/IsekaijouchoPet-v0.7.6-preview.zip`。以下旧版握持验收描述属于历史记录，以本节和当前代码为准。

## 0.7.5 历史修正候选

- 按用户反馈修正剑与手的交角：手掌绘制同时绑定腕掌骨骼与剑刃轴，使剑刃从拇指侧伸出；近远侧使用对应手掌表面。仅修改 `PaintedSwordArt` 与 `SwordRig` 的手掌绘制，动作关键帧、剑尖路径及握点不变。
- `HairLength` 对十姿态及六个睡眠中间关键图细化轮廓，增加腿间与衣摆空隙，使用覆盖率与预乘 alpha；旧发丝位置实际变为透明，不涂背景色。全局发长要求延续，头部原像素保持；没有修改动作曲线和资源包。
- 本轮 459 项自动检查通过：剑骨骼与握持 103、发长/透明/入口/箭尾 48、冻结帧 297、固定参数 11。已实际查看左右五姿势、握持放大、十姿态及睡眠/其他动作，并用棋盘格/黑/白背景检查透明。
- **视觉仍有已知问题**：少量衣摆细浅边、睡姿脚边小残点及局部轮廓偏硬尚存。遵守最多两轮自主视觉修正的要求，保留可运行候选等待反馈；不能表述为已彻底清干净或视觉验收完成。
- 入口：`qa/v0705/hair-review.html`、左右 sword 预览、`qa/v0705/validation.md`；发布包 `dist/IsekaijouchoPet-v0.7.5-preview.zip`。本轮未删除资源，随包 removed-assets.csv 仅为 0.7.4 历史清理记录。以下 0.7.4 节为上一版状态。

## 0.7.4 历史变更与验收状态

- 最新用户要求：**头发长度全局改为腰部稍下方**。这覆盖此前“保持发长”的约束，包括睡眠的外观；睡眠动画时序、骨骼、插值仍冻结。`HairLength` 在 `CharacterArt.Normalize` 与 `SleepArt.Key` 的公共加载阶段分离后发，使用现有 `SingleSkin` 调整静止绑定长度；正反朝向独立注册，头顶/刘海/脸部原像素保持。挥手补图经同一 Normalize 路径，不仅修改剑动作。
- **视觉未验收**：已检查五视图、十姿态、睡眠九采样与其他动作。部分睡眠中间姿态的发尾/衣摆仍有分层接缝；依据用户限制停止继续自主美术修正，保留可运行候选等待反馈。不要把编译或自动检查通过表述为该问题已经解决。查看 `qa/v0704/hair-review.html` 与 `hair-actions.png`。
- 剑仍使用现有二维肩肘腕链；关键姿势与武器握持方向调整为前上→相反侧下方的跨身斜线，不绕圈。蓝色星芒在动作开始时凝聚，在 0.78–1.03 s 消散；没有剑鞘，特效菜单可全部关闭。资源和骨骼提前初始化，站稳后在普通 locomotion 刹停分支之前进入动作。HitFrame 仍为 0.50 s。
- 箭后新增淡金色明亮尾迹，每段保留 1 s 后用 0.4 s 渐隐；生命周期和窗口范围包含尾迹，原速度 12.8 倍身高/秒、螺旋、三环与金色爆炸不变。
- 检查：剑骨骼/接口 97、发长头部像素/入口/尾迹 31、固定参数 11、射箭遮挡/时序 8。另 297 帧既有动作在仅关闭本次 HairLength 绑定时与 0.7.3 逐像素一致。合计 444；这不证明全局发长的视觉质量已经通过。
- 删除 32 张旧 PNG（49,961,299 字节），清单与 SHA-256 在 `qa/v0704/removed-assets.csv`。当前十一张 PNG 和 character.pack 字节保留；仍被旧检查工具引用的 v0519 素材暂留，旧发布包不删除。
- 当前发布与验证入口为 `dist/IsekaijouchoPet-v0.7.4-preview.zip`、`qa/v0704/validation.md`。下文 0.7.3 数值及验收记录为基线历史，冲突时以本节和当前代码为准。

## 当前架构

- **Windows / C# / .NET Framework 4.8 / WinForms**，使用 System.Drawing 软件绘制和透明分层窗口。`Pet` 管理交互与动作状态，`RenderFrame` 将状态传给 `PetRenderer`；`FrameClock` 以真实时间推进，目标 60 Hz。[主窗口][app] · [渲染入口][renderer] · [时钟][clock]
- 代码按 App（交互协调）、Animation（动作曲线/状态）、Rendering（原画与变形）、Platform/Desktop（桌面图标）、UI（菜单/气泡/聊天/特效）、Configuration、AI/Files 分层。
- **离线原画、运行时变形**：主图集含十个姿态，当前发布资源为十一张 PNG（人物图集、腿/睡眠/手臂补充素材、弓、弦、箭、低饱和蓝银直剑与右手握拳补图）。`character.pack` 是 ZIP 容器，保留 PNG 原始字节；优先从 assets 读取散文件，缺失时才使用旧 character.pack 后备；当前发行不含 pack。历史素材仍保留，不把每个播放帧写入 assets。[原画注册][art] · [加载][assets] · [打包清单][pack]
- 用户的 **24 项动画参数已写死**，正式程序不读取外部动画参数 JSON，调试页面入口已关闭；旧编辑器代码仅供内部工具使用。[固定参数][params] · [参数快照][snapshot]

## 骨骼与渲染系统

- 整体为 **2D/2.5D 骨骼、原画网格变形和程序插值的混合系统**，不是完整 3D 模型，也没有统一的全身骨骼编辑工程。
- **转身**：`TurnRig → SkeletalTurn → CharacterSkeleton`。九骨骼覆盖根、胸、头、两侧头发、双臂、双腿；五个方向原画通过加权蒙皮、视角校正及同一网格上的纹理插值连接。正面/侧面端点直接复用站立/行走画面；单脚转身叠加支撑脚固定、另一脚抬起和轻微侧倾。[转身入口][turn] · [转身网格][turnmesh] · [九骨骼][skeleton]
- **正面/走跑/手臂**：`SoftRig` 处理呼吸、眼口、头发等局部变形；`SideWalk + WalkArt` 驱动步相和分层腿部；`JointArm` 分离大臂、小臂、手和肘部连接，供挥手、搬运、射箭和单手挥剑使用。各动作有专用控制器，不共用一条全身动作链。[渲染入口][renderer] · [关节组件][joints]
- **睡眠固定为 0.6.34**：当前调用 `RestRig + PoseMesh + SleepArt`，使用九个关键姿态槽及局部混合，最后两槽复用正式睡姿，避免末尾换手臂；不是后来试验的纯骨骼抱膝方案。[睡眠实现][rest]
- **射箭**：人物由 `SideArcheryRig.cs` 中的 `ArcheryRig` 驱动，维持骨段长度、指尖搭弦和远近臂遮挡；弓/弦/箭各有独立透明 PNG，随动作变形或旋转。粒子、螺旋和波纹仍为程序绘制，放在独立穿透鼠标的特效窗口；左向弓箭及飞行特效与人物重叠部分在同一帧中合成（人物最后绘制），外部特效窗口裁空人物矩形区域，遮挡不依赖窗口层级。[射箭人物][archer] · [射箭时序][shot] · [特效窗口][overlay]

- **单手拔剑斜斩**：`SwordMotion → PetSword → SwordRig` 复用现有转身与动作互斥。`SlashSkeleton` 建立基于已注册关节的二维肩—肘—腕—手掌—武器链，限制肩/肘/腕范围；`SlashBodyRig` 复用 JointArm、SingleSkin 及现有方向独立的腿部蒙皮，双腿用两段 IK 固定脚底。五阶段为预备 0.15 s、起剑 0.22 s、主斩 0.16 s、收势 0.25 s、复位 0.25 s；不绕整圈，不使用逐行迈步变形。头发/服装有极小的延迟跟随，比例与原画不变。左右保持非对称衣袖与靴子，远侧右臂在人物后层。
- **动作与工具隔离**：`ISwordSlashAction.SwordSlash(PointF targetPosition, int facingDirection)` 与模型无关，在 UI 线程调度。0.50 s 发出一次 `HitFrame`，携带请求 ID、目标屏幕坐标与朝向。事件本身不操作桌面文件；`ProgramLaunchController` 处理显式绝对 .exe 路径并返回成功/失败，`SwordAgentRuntime` 为可选适配器。默认右键菜单只播放动作，不启动应用。目标寻路、图标识别与模型工具协议尚未接入。
## 人物设计约束（用户确认）

- ヰ世界情緒 / **Anemone I**，精细 Q 版，约 **三至四头身**；保持当前脸形、绘画质量和跨视角比例，不继续执行已撤回的脸部拉长/下巴改形尝试。
- 头发为很浅的暖白棕/浅棕色，不能变成银灰；以已批准的统一原画为准，保留长度、体积与轮廓一致性。
- 左右均指**角色自身**：右眼浅红，左眼保持原有青蓝色；两眼实际比例与眼位协调，保留右眼前刘海遮挡，不能为增加露出面积移动刘海或将左眼染红。
- 黑色服装配红花，**每朵花内有眼睛**；左右袖长、腿部袜靴长度不对称，左臂由长袖覆盖，不能镜像复制成对称服装。
- 气质略带疏离，但用户是熟人；笑容以居中的柔和微笑为主，摸头可闭眼、轻歪头，避免张嘴大笑与嘴部左右漂移。人物基准见[当前原画][atlas]。

## 已有动作与交互

| 动作 | 当前行为 |
| --- | --- |
| 待机、摸头、问好 | 呼吸、眨眼、视线跟随；摸头微笑/闭眼/歪头；挥手手心朝用户，大臂从静止角度向外打开，小臂与手腕联动，缓慢收手。 |
| 行走、小跑、转身 | 左右走跑；跑步轻前倾、微屈膝、对侧手脚同步及手腕惯性；普通/单脚转身随机出现，跑步有专用转身衔接。 |
| 上跃、下落 | 保留横向前移分量；既可独立触发，也用于上下接近/搬运图标。 |
| 图标搬运 | 左右随机走动或跑动推动；跑动接近与接取连续，保持握持手势，跑动距离可选 1–6 格；上下搬运使用跃/落动作。实时更新 Explorer 图标位置并确认，推动期间临时关闭网格吸附，结束/中断恢复。 |
| 单手拔剑斜斩 | 右键“单手拔剑 · 斜斩”选择左右；可关闭斩击轨迹。五阶段骨骼动作、固定握点与脚底。菜单暂停；其他动作、拖动、缩放、退出清理剑层。 |
| 睡眠、起身 | 保持 0.6.34 的动作、过渡与速度，后续不得主动调整。 |
| 魔法射箭 | 先完成转身，再同步抬手与聚光显弓，弓完整出现时指尖已搭弦，并立即开始凝箭（移除 1.05 秒空档）；左手持弓、右手拉至脸侧，双腿微开；白金星光聚合后弓以完整尺寸出现。25° 仰射，取消中段停顿，连续拉弦、末段减速拉满，满弓后移身高的 2.5%，瞄准 300 ms 后释放，随动后收手。 |
| 射箭特效、对话 | 暖白弓箭及白金粒子；旋转螺旋半径保持 0.6.44，螺旋及弓箭周围粒子发光强度增益为 0.6.44 的 2 倍（显示饱和封顶），从箭头至箭尾后一个箭长（总长 2L，为 0.6.45 的 2/3，保持螺距为六圈）；箭飞行速度为 0.6.46 的 2 倍（每秒 12.8 倍角色高度）；保留发射波纹，触边改为金色三重圆环及金色星光，三环在命中后 0/0.1/0.2 秒生成，各自持续 0.65 秒，最大半径为角色身高 75%（此前 3 倍）。另有对话气泡、聊天/文件助手、拖动和托盘菜单。 |

动作入口见[主窗口][app]；射箭数值见[时序][shot]与[特效绘制][effects]，搬运见[搬运协调][carry]。

## 已知限制与待确认项

- **睡眠的旧过渡限制被明确接受并冻结**：仍有局部混合，手部遮挡变化时可能短暂叠影，不能宣称已实现无叠化的完整骨骼睡眠。`SkeletalRest`、`HugSleepRig` 等试验代码未启用。[睡眠实现][rest] · [已有边界记录][readme]
- **五视图 2.5D 有表达边界**：不同方向原画仍可能显露细节变化，不具备完整背面或任意视角的 3D 表面；新动作需检查衣袖、头发、肘部接缝与遮挡。[转身网格][turnmesh] · [已有边界记录][readme]
- **桌面环境依赖**：自动排列图标须由用户关闭；Explorer 超时、第三方桌面管理或外部改动位置会暂停搬运。正常结束/取消立即恢复网格吸附，强制终止后的恢复依赖下次启动。[桌面接口][desktop] · [网格恢复][grid]
- **验证边界**：0.7.3 本轮 370 项自动检查通过（骨骼/接口 73、冻结帧对照 297）；沿用原资源包，十一张 PNG 无改动。提供默认无特效、可切换骨骼线/握点/残影的左右连续预览和五姿势图。固定骨长、关节范围、剑尖连续、双脚固定、HitFrame 一次性通知和既有动作采样对照已验证；动作自然度、握持视觉、穿模及真实桌面表现仍需用户验收。60 Hz 是调度目标，软件绘制不保证所有机器恒定 60 FPS。[验证记录][qa]

## 后续接手

以当前代码、[参数快照][snapshot]及[0.7.3 发布包][release]为基线；保留已有素材和旧发布包。优先做局部动作/贴图修正，保持普通走跑和冻结的睡眠；修改人物原画或坐标注册时，需同步验证全部视角、表情、关节接缝和动作端点。本文件为状态记录。0.7.3 依据最新五阶段规范重做有限关节范围的拔剑斜斩与固定脚底 IK；保留蓝银剑蒙皮，轻量残影只在主斩期间出现。其他动作、箭速、睡眠与固定参数继续冻结。已做两轮视觉检查、一次命中时机修正；当前等待用户视觉验收，不自行扩展战斗系统。

[app]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/App/PetWindow.cs
[renderer]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/PetRenderer.cs
[clock]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Animation/FrameClock.cs
[art]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/CharacterArt.cs
[assets]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/AssetStore.cs
[pack]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/tools/PackAssets.cs
[params]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Configuration/AnimationParameters.cs
[snapshot]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/presets/user-2026-10-23-v0643.json
[turn]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/TurnRig.cs
[turnmesh]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/SkeletalTurn.cs
[skeleton]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/CharacterSkeleton.cs
[joints]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/RigParts.cs
[rest]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/RestRig.cs
[archer]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/SideArcheryRig.cs
[shot]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Animation/ArcheryMotion.cs
[overlay]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/UI/MagicOverlay.cs
[atlas]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/assets/v0610/anemone-i-unified-v10-eyes.png
[effects]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Rendering/ArcheryRig.cs
[carry]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/App/PetIconCarry.cs
[desktop]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Platform/DesktopIcons.cs
[grid]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/src/Platform/GridSnapLease.cs
[qa]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/qa/v0703/validation.md
[release]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/dist/IsekaijouchoPet-v0.7.3-preview.zip
[readme]: C:/Users/fuhua/Documents/ChatGPT/Iskjc/IsekaijouchoPet-v0.6.0/README.md
