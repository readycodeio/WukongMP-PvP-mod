# WukongMP 模组模板

![version](https://img.shields.io/badge/版本-0.2.1-green)

<img src="https://flagcdn.com/gb.svg" width="18" alt="English"/> [English version](README.md)。

对于其他版本，请查看[标签](https://github.com/readycodeio/wukongmp-mod-template/tags)列表。

这是一个用于使用 WukongMP SDK 开发模组（Mod）的模板项目。

有关如何使用 SDK 和创建模组的详细信息，请参阅 [WukongMP SDK 文档](https://docs.ready.mp)。

## 快速上手

1. 克隆此仓库到您的本地机器。
2. 在您偏好的 C# IDE（例如 JetBrains Rider、Visual Studio）中打开解决方案（Solution）。
3. 构建（Build）解决方案，以确保所有依赖项（Dependencies）都已正确解析。
4. 通过修改 `Main.cs` 文件并添加您自己的代码，开始开发您的模组。
5. 根据模组功能的需要，引用 `Dependencies` 中的任何 DLL 文件。

## 仓库结构

- `ExampleMod/Mod.cs`: 模组的主要入口点，您可以在此处初始化和设置模组的功能。
- `ExampleMod/manifest.json`: 模组的清单文件，包含名称、版本和描述等元数据。
- `Dependencies`: WukongMP SDK 和原版游戏文件，供您在开发模组时引用。服务器二进制包中也包含相同的文件。

## 打包模组

1. 请务必使用正确的模组信息（例如名称、版本和描述）编辑 `manifest.json` 文件。
2. 编辑 `ModFiles.ps1` 以添加模组使用的任何额外文件。
3. 运行带有 `Release` 参数的 `MakeModFolder.ps1` 脚本，以创建一个需要上传到服务器的文件夹（默认名称：`ExampleMod`）。
4. 生成的文件夹可以在 `Output` 目录中找到。
5. 将构建好的模组文件夹复制到服务器的 `mods/` 目录并重启服务器。

## 调试

使用带有 `Debug` 参数的 `MakeModFolder.ps1` 脚本来创建模组的调试版本，其中包含用于调试目的的额外文件。

在调试模组之前，您需要在 WukongMP 中启用调试器。

### 启用调试器

若要在模组化的 WukongMP 中启用调试器，请按照以下步骤操作：

1. 导航至 `%APPDATA%\ReadyM.Launcher\DownloadCache\Loader`
2. 进入版本号最新的文件夹，例如 `0.7.457.1630`
3. 找到 `@APPDATA\CSharpLoader\b1cs.ini` 文件并编辑以下设置：

```ini
[Settings]
Develop=1       # 启用调试器
Console=1       # 显示控制台窗口
EnableJit=1     # 必需，请勿更改
```

下次启动游戏时，Mono 调试器服务器将在端口 `44446` 上启用。
您可以通过编辑同一文件夹中的 `debugger-agent.txt` 文件来更改调试器设置。

调试器代理（Debugger agent）的默认设置如下：

```txt
transport=dt_socket,loglevel=0,address=127.0.0.1:44446,server=y,suspend=n
```

### 从 JetBrains Rider 连接

1. 前往 `Run > Edit Configurations`。
2. 点击 `+` 按钮并选择 `Mono Remote`。
3. 将 `Name` 设置为类似 `WukongMP Debugger` 的名称。
4. 将 `Host` 设置为 `localhost`，并将 `Port` 设置为 `44446`（或您在 `debugger-agent.txt` 中配置的端口）。
5. 点击 `Apply` 然后点击 `OK`。

现在您可以启动游戏，然后在 JetBrains Rider 中运行 `WukongMP Debugger` 配置以连接到调试器。
您应该能在 Rider 中看到调试器控制台，并且可以在模组代码中设置断点进行调试。

> **注意**：调试器初次连接可能需要几秒钟，并显示消息 `Waiting for target to get ready`。一旦连接成功，状态将变为 `Target ready`。