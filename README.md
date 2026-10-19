# Snap-UnitySave

《召唤对决》的 Unity 项目存档与持续开发工程。项目包含召唤对决战斗场景、士兵与生成器逻辑、动画和 Timeline、模型与美术资源，以及用于复核战斗行为的编辑器工具、验收记录和录屏。

## 项目信息

- Unity：2022.3.62f3c1
- 主要战斗场景：`Assets/20261012-PVE守关敌万军/20261012-PVE收关敌万军.unity`
- 主要技术：URP、AI Navigation、Timeline、Cinemachine、TextMeshPro
- 大型媒体和美术源文件使用 Git LFS 管理。

## 工程内容

- **召唤对决与 PVE 战斗**：士兵生成、寻路、攻击、受击、击退、死亡和箭矢等战斗流程。
- **桥口近战控制**：蓝方盾兵有限推进与退守，红方近战排队、前排退路保护和延迟补位；移动优先级、碰撞限制及按伤害来源区分的闪白配置。
- **Unity 资源**：场景、Prefab、动画控制器、Timeline、NavMesh、材质、Shader、模型、贴图和 UI 图标。
- **开发与验收资料**：`Docs/` 中的配置说明、运行结果、JSON 验收记录和画面截图；`Assets/Editor/` 中的场景验证与断言工具。
- **过程录屏**：`Recordings/` 保存战斗与迭代过程视频。

## 打开项目

1. 安装 Git 和 Git LFS，并执行 `git lfs install`。
2. 克隆仓库：

   ```bash
   git clone https://github.com/yangui-debug/Snap-UnitySave.git
   ```

3. 使用 Unity Hub 选择与 `ProjectSettings/ProjectVersion.txt` 一致的 Unity 版本（`2022.3.62f3c1`）打开仓库目录，等待 Unity 导入资源。
4. 从 `Assets/20261012-PVE守关敌万军/` 打开主要 PVE 场景。

如果克隆后视频或其他 LFS 文件显示为文本指针，请确认 Git LFS 已安装，再运行 `git lfs pull`。

## 最近更新

### 2026-10-19｜PVE 桥口近战迭代与工程资源同步

- 更新主要 PVE 场景、NavMesh 和 Timeline 内容，补充战斗场景所需的角色、材质、Shader 与职业图标资源。
- 增加蓝盾兵有限推进/退守、红方近战排队与延迟补位等控制脚本及配置；受击移动优先级和碰撞约束保留原有伤害、寻路、生成与死亡流程。
- 增加弯曲箭头控制脚本及对应美术资源，更新若干士兵移动、生成、动画和受击逻辑。
- 增补编辑器断言与场景验证工具、配置说明、验收结果和截图，便于在 Unity 中继续调参与复核。
- 同步新增战斗迭代录屏；视频通过 Git LFS 保存。

桥口近战参数和接线方式见 [`Docs/新版桥口近战配置说明.md`](Docs/新版桥口近战配置说明.md)、[`Docs/右桥近战推拉说明.md`](Docs/右桥近战推拉说明.md)。验收记录见 `Docs/` 下对应 JSON 文件。自动化断言与短时场景采样用于辅助检查，不替代实际游玩中的视觉和手感验收。
