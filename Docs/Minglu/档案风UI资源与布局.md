# 档案风 UI 资源与布局

参考《This Is the Police》的调度式信息分区、底部人员列、克制配色和独立事件窗口；为“明路·地下”重新绘制资源，没有抽取或打包参考游戏的贴图。官方参考：https://www.thqnordicmobile.com/en/games/this-is-the-police/

## 布局

- 1600×900 设计坐标，按窗口同比缩放。
- 顶部：局数、资源和系统操作；中部上沿：带图标的六类事务页签。
- 左栏：上级目标、三条优先事项、当前指挥提示。点击事项直接定位任务。
- 中栏：城区地图或任务、链条、人事、报告、经营内容。
- 右栏：选中成员档案、执行方案与授权。人员选择不改变正在比较的任务。
- 底部：最多八名成员的值班卡，展示头像、身份等级、当前职责和体力；最下方是反馈、下一步入口和推进按钮。
- 新局默认展示城区态势；左侧提示引导建链，不自动授权任何行动。

## 资源

运行资源：`Assets/Minglu/Resources/Minglu/UI/Noir/`。

- 原创主菜单背景 `title_city.png`，内置 imagegen 生成，已复制进入项目。
- 18 个透明线性图标：任务、链条、人事、地图、报告、联盟、情报、钱币、风险、时钟、保存、设置、箭头、关闭、确认、锁、夜晚、总部。
- 7 个可九宫格拉伸的资源：按钮默认、悬停、按下、禁用，通用面板、卡片、弹窗；12 像素边界，切角避免拉伸变形。
- 可编辑 SVG 在 `Tools/UI/Sources/`，生成脚本 `Tools/UI/build_noir_ui.py`。字体仍使用微软雅黑运行时文本，中文不会烘焙进图片。
- 共用控件接入使首页、配置、设置、档案、任务、链条、人事、地图、报告、经营、事件、指南、结局和外观编辑使用同一套表面与按钮状态。
- 色彩：炭黑背景、灰黑面板、暖纸色标题、朱红主操作、低饱和灰绿状态。按钮状态既有亮度反馈，也有选中边框；状态仍保留文字说明。

人物、城市地图和既有外观部件保持原资源；本轮是 UI 系统与主菜单插画重制，不更换已调整的人体比例。

## 背景生成提示词

内置 imagegen；新图生成，无输入图片。

“Create an original 16:9 landscape background illustration for a historical Chinese underground intelligence management strategy game main menu. NO TEXT, NO LETTERS, NO UI, NO LOGO. Restrained editorial screen-print / geometric noir illustration, angular paper-cut silhouettes, flat broad shapes, muted warm charcoal black, desaturated slate, parchment ivory and small vermilion red accent. Wide old Chinese riverside city skyline at night, roofs and watchtower in the left half, a tiny warm window and a single red signal ribbon, foreground shadow of sealed envelopes on a desk at lower left. Right half mostly plain deep charcoal negative space for menu controls. Sophisticated flat graphic art, dry ink texture very subtle, not photoreal, not 3D, not ornate fantasy. Inspired by cinematic noir strategy-game graphic design but entirely original composition. Save as game-ready background.”

## 验收

复用真实按钮命中的界面自动化检查：新局、任务、人员对比、链条岗位、外观编辑、事件、报告、存读档；另外检查新版菜单、地图布局和长文本截图。规则和存档格式不因视觉改版而改变。
