# WukongMP PvP mod

![version](https://img.shields.io/badge/version-0.2.1-green)

<img src="https://flagcdn.com/cn.svg" width="18" alt="Chinese"/> [中文版](README.zh-Hans.md)

The official PvP mod for WukongMP, developed by the [ReadyM team](https://www.ready.mp).

The structure of this repository is similar to
the [WukongMP mod template](https://github.com/readycodeio/wukongmp-mod-template), so you can refer to the template's
documentation for general mod development instructions.

Refer to the [WukongMP SDK documentation](https://docs.ready.mp) for detailed information on how to use the SDK and
contribute to this mod's development.

## Caveats

Until the server-side scripting support is implemented, the PvP mod uses the [PvP API](https://docs.ready.mp/wukong-mp/api-reference/WukongMp.Sdk.Api/WukongMp.Sdk.Api.IWukongPvpApi) and the [Cheats API](https://docs.ready.mp/wukong-mp/api-reference/WukongMp.Sdk.Api/WukongMp.Sdk.Api.IWukongCheatsApi).

These APIs are temporary solutions arising from the tight coupling of PvP features with the SDK,
in particular having custom data components defined in the SDK and not in the mod itself.

Once the server-side scripting support is implemented, the PvP mod will be refactored to use server-side scripts instead of these APIs, and the APIs will be removed.

## Contributing

The place for discussions about this mod is the official [ReadyM Forum](https://forum.ready.mp). 
You can also join the [WukongMP Discord server](https://discord.com/invite/wukongmp) for real-time discussions and support.

If you want to contribute to the development of this mod, please follow these steps:

1. Fork this repository and create a new branch for your feature or bug fix.
2. Make your changes and ensure that they are well-documented and tested.
3. Submit a pull request to the main branch of this repository, describing your changes and the motivation behind them.
4. The ReadyM team will review your pull request and provide feedback or merge it if it meets the project's standards.

Thank you for your contributions!
