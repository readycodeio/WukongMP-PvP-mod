# WukongMP PvP 模组

![SDK](https://img.shields.io/badge/SDK-0.4.1-green)

<img src="https://flagcdn.com/gb.svg" width="18" alt="Chinese"/> [English](README.md)

WukongMP 官方 PvP 模组,由 [ReadyM 团队](https://www.ready.mp)开发。

需要 WukongMP SDK 0.4.0 或更高版本。

本仓库的结构与 [WukongMP 模组模板](https://github.com/readycodeio/wukongmp-mod-template)类似,因此你可以参考该模板的文档,获取通用的模组开发说明。

有关如何使用 SDK 以及参与本模组开发的详细信息,请查阅 [WukongMP SDK 文档](https://docs.ready.mp)。

## 项目结构

| 项目 | 运行端 | 内容 |
|---------------------------|--------|--------------------------------------------------|
| `WukongMp.PvP`            | 客户端 | 游戏补丁、UI、聊天、命令、作弊功能、玩法配置     |
| `WukongMp.Pvp.Common`     | 双端   | 联网组件、RPC 契约、出生点数据、共享常量         |
| `WukongMp.PvP.Serverside` | 服务端 | 玩法规则:回合开始与结束、反拖延、房间配置       |

本模组自行管理其状态。`PvpStateComponent` 与 `PvPComponent` 声明在 `WukongMp.Pvp.Common` 中,并在加载时附加到 SDK 的原型(Archetype)上,因此 SDK 中不再包含任何 PvP 专用内容。

早期版本没有服务端脚本支持,PvP 状态只能声明在 SDK 中,并通过临时的 PvP API 与 Cheats API 访问。自 0.4.0 起,这两个 API 以及导致其存在的紧耦合均已移除。

## 服务端配置

服务端部分会从自身的模组目录读取 `config.json`。该文件用于设置初始竞技场、锦标赛回合数、生成敌人的 New Game Plus 等级,以及允许使用的能力。所有键均为可选,但未知的键会导致模组加载失败,而不会被静默忽略。

该文件每五秒轮询一次。若在对局进行中修改,则会在该局结束后生效,因为竞技场和回合数等设置无法在回合进行中更改。

## 参与贡献

关于本模组的讨论可前往官方 [ReadyM 论坛](https://forum.ready.mp)。
你也可以加入 [WukongMP Discord 服务器](https://discord.com/invite/wukongmp),进行实时交流与获取支持。

如果你想参与本模组的开发,请按照以下步骤操作:

1. Fork 本仓库,并为你的新功能或 Bug 修复创建一个新分支。
2. 进行修改,并确保有完善的文档说明和充分的测试。
3. 向本仓库的主分支提交 Pull Request,描述你的改动以及背后的动机。
4. ReadyM 团队会审核你的 Pull Request,并提供反馈,若符合项目标准则会合并。

感谢你的贡献!