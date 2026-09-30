# 《白日升》UI 参考与本项目落地

本次沿用正在制作的按钮与指挥界面方向。依据官方介绍及截图进行视觉与信息组织研究；没有将截图推断为完整实机操作评测。

## 来源与观察

- [官方 Steam 页面](https://store.steampowered.com/app/1189100/Rise_Of_The_White_Sun/)：角色、派系和组织经营是重要表现内容。
- [部队命令面板](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/1189100/ss_1836262a7964f6f7a424006d09d7956815fdf9b5.1920x1080.jpg)：状态与命令分列，命令旁给出资源成本。
- [地方政策页面](https://cdn.akamai.steamstatic.com/steam/apps/1189100/ss_d7c255a0fb67c2f8cdc6a651df62663014535f84.1920x1080.jpg)：暖纸面板和按目的分类的行动入口。
- [人物池](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1189100/ss_628d384bd76c2fb5349514c963e50b4ef31dffdd.1920x1080.jpg)：头像提供入口，人物详情在旁侧阅读。

## 采用方式

| 参考点 | 我们的具体设计 |
| --- | --- |
| 纸质档案与指挥桌统一语汇 | 按钮更新为深红印签、牛皮纸签、深棕墨框三类，保留五态、九宫格和独立文字。 |
| 动作旁可见的成本 | 招募和有序转移按钮左侧显示动作、右侧独立显示费用；签发栏直接显示银两、情报、体力、精神和暴露代价。 |
| 地图与地点行动相联系 | 点城区留在地图，右侧展示当地情况和待办；明确点击任务入口后再进入任务签发页。 |
| 人物入口持续可见 | 延续底部成员条、右侧所选成员档案和固定右下推进入口。 |
| 少量清晰的操作类别 | 继续区分目录、文字链接、地图标记、普通命令和最终提交。 |

小尺寸中文可读性与命令含义优先于纹理。新按钮用细框和低对比印刷质感，不把所有界面入口变成同一块带边框的牌子。

本轮实现是界面与操作路径调整，数值、随机判定、组织玩法和存档结构沿用项目原有实现。

## 资产与维护

美术为内置 image_gen 生成的原创图集；没有提取或使用《白日升》的游戏贴图。资源继续写入固定 `PrintButtons` 目录，并由 `PrintButtonAssetTools` 无损切图导入。当前源图、提示词、裁切清单位于 `ArtSource/UI/PrintButtons/`，具体见[按钮说明](按钮美术资产说明.md)。

## 本轮验证

- `Logs/white-sun-ui-build.log`：切图、Sprite 导入检查和 Unity 构建成功。
- `Logs/white-sun-art-smoke.log`：15 张按钮的五态切换、焦点、单次点击与禁用拦截通过。
- `Logs/white-sun-design.log` 与 `Logs/white-sun-720.log`：1600×900、1280×720 的菜单、页面、弹窗、地图上下文、空区入口和存读档通过；选区不会改动回合、预算或任务负责人。
- `Logs/white-sun-studio.log`：外观工作室回归通过。
- 15 张切图的 RGBA 数据与图集对应区域逐像素一致。

截图：`outputs/white-sun-art/preview.png`、`outputs/white-sun-design/check.district-context.png`、`outputs/white-sun-design/check.dispatch.png`。
