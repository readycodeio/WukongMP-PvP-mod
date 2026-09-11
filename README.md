# WukongMP PvP mod

![SDK](https://img.shields.io/badge/SDK-0.4.1-green)

<img src="https://flagcdn.com/cn.svg" width="18" alt="Chinese"/> [中文版](README.zh-Hans.md)

The official PvP mod for WukongMP, developed by the [ReadyM team](https://www.ready.mp).

Requires the WukongMP SDK 0.4.0 or newer.

The structure of this repository is similar to
the [WukongMP mod template](https://github.com/readycodeio/wukongmp-mod-template), so you can refer to the template's
documentation for general mod development instructions.

Refer to the [WukongMP SDK documentation](https://docs.ready.mp) for detailed information on how to use the SDK and
contribute to this mod's development.

## Layout

| Project | Runs on | Holds |
|---------------------------|--------|-------------------------------------------------------------------|
| `WukongMp.PvP`            | client | game patches, UI, chat, commands, cheats, gameplay configuration  |
| `WukongMp.Pvp.Common`     | both   | networked components, RPC contracts, spawn data, shared constants |
| `WukongMp.PvP.Serverside` | server | game mode rules: round start and end, anti-stall, room config     |

The mod owns its own state. `PvpStateComponent` and `PvPComponent` are declared in
`WukongMp.Pvp.Common` and attached to the SDK's archetypes at load time, so nothing PvP specific
lives in the SDK.

Earlier releases had no server-side scripting, so PvP state had to be declared in the SDK and
reached through a temporary PvP API and Cheats API. Both are gone as of 0.4.0, along with the
coupling that made them necessary.

## Server-side configuration

The server half reads `config.json` from its own mod folder. It sets the starting arena, the number
of tournament rounds, the New Game Plus level applied to spawned enemies, and which abilities are
allowed. Every key is optional, and an unknown key fails the mod load rather than being ignored
silently.

The file is polled every five seconds. A change made while a match is running is applied once that
match ends, since settings like the arena and the round count cannot change under a live round.

## Contributing

The place for discussions about this mod is the official [ReadyM Forum](https://forum.ready.mp). 
You can also join the [WukongMP Discord server](https://discord.com/invite/wukongmp) for real-time discussions and support.

If you want to contribute to the development of this mod, please follow these steps:

1. Fork this repository and create a new branch for your feature or bug fix.
2. Make your changes and ensure that they are well-documented and tested.
3. Submit a pull request to the main branch of this repository, describing your changes and the motivation behind them.
4. The ReadyM team will review your pull request and provide feedback or merge it if it meets the project's standards.

Thank you for your contributions!
