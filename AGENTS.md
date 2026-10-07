# 明路·地下：制作与验收约定

## 产品方向

- 玩家是地区组织负责人，安排人事、任务与长期联络；具体行动由成员执行。
- 只有敌方机构任职内线产出内部情报，按联络链逐夜送达。普通成员与民间任务不凭空奖励机构情报。
- 主画面以城区地图为中心；业务使用按钮和弹窗，避免重新堆成信息墙。
- 目标为约50元的Steam单机游戏。目标售价不是质量验收结论，禁止将内部测试通过写成已经达到商业发行标准。

## 每轮兼任PM

1. 改动前查看 `Docs/Minglu/制作验收看板.html`，选择真实玩家问题，核实当前启用的首脑模式代码。
2. 实现、运行适合改动的检查、查看实际渲染；仅有代码或文案不算可用功能。
3. 更新看板的状态、证据与剩余缺口。区分自动测试、人工试玩、平台审核；不编造完成率、游玩时长或玩家胜率。
4. 检查新局、边界、失败恢复、旧存档与随机结果兼容。旧局保留原通关合同。
5. 按阻断/数据损失 → 上手和整局节奏 → 内容与表现排序后续工作。没有新的需求时，不擅自建立周期自动化或对外发布。

## 固定交付

- 当前项目：`D:\UnityProjects\Operator911`；直接修改，用Git记录历史，不创建版本包目录。
- Windows构建：`Builds/MingluCity/MingluCity.exe`；发布构建不启用Development。
- 安装包：`Builds/Installer/MingluSetup.exe`；当前本机安装：`D:\Games\MingluCity`。
- 玩家数据固定使用 `DefaultCompany/Operator911` 目录，由 `MingluStorage` 维护；改显示名称不可让旧存档消失。
- 自动UI检查使用 `--minglu-capture` 的隔离存档，禁止用测试覆盖真实进度。
- `Minglu.EditorTools.PresentationVerification.BatchDeliver` 验证并构建。
- `Tools/Release/Review-Quality.ps1 -RequirePass` 检查当前构建的规则/UI/安装证据，报告写入 `outputs/release-current/quality-status.json`。人工商业验收仍单列。
- 每次实际交付更新教程并核对安装目录文件、已有用户存档哈希；本地提交不等于远程推送。
