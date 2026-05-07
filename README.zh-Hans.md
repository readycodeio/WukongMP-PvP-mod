# WukongMP PvP 模组

![version](https://img.shields.io/badge/version-0.2.1-green)

<img src="https://flagcdn.com/gb.svg" width="18" alt="Chinese"/> [English](README.md)

WukongMP 官方 PvP 模组,由 [ReadyM 团队](https://www.ready.mp)开发。

本仓库的结构与 [WukongMP 模组模板](https://github.com/readycodeio/wukongmp-mod-template)类似,因此你可以参考该模板的文档,获取通用的模组开发说明。

有关如何使用 SDK 以及参与本模组开发的详细信息,请查阅 [WukongMP SDK 文档](https://docs.ready.mp)。

## 注意事项

在服务端脚本支持实现之前,本 PvP 模组使用 [PvP API](https://docs.ready.mp/wukong-mp/api-reference/WukongMp.Sdk.Api/WukongMp.Sdk.Api.IWukongPvpApi) 和 [Cheats API](https://docs.ready.mp/wukong-mp/api-reference/WukongMp.Sdk.Api/WukongMp.Sdk.Api.IWukongCheatsApi)。

这些 API 属于临时方案,源于 PvP 功能与 SDK 之间的紧耦合,特别是自定义数据组件被定义在 SDK 中而非模组本身。

待服务端脚本支持实现后,本 PvP 模组将重构为使用服务端脚本,这些 API 也将被移除。

## 参与贡献

关于本模组的讨论可前往官方 [ReadyM 论坛](https://forum.ready.mp)。
你也可以加入 [WukongMP Discord 服务器](https://discord.com/invite/wukongmp),进行实时交流与获取支持。

如果你想参与本模组的开发,请按照以下步骤操作:

1. Fork 本仓库,并为你的新功能或 Bug 修复创建一个新分支。
2. 进行修改,并确保有完善的文档说明和充分的测试。
3. 向本仓库的主分支提交 Pull Request,描述你的改动以及背后的动机。
4. ReadyM 团队会审核你的 Pull Request,并提供反馈,若符合项目标准则会合并。

感谢你的贡献!