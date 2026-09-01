#!powershell.exe -ExecutionPolicy Bypass -File

# Edit these lists to specify files that should be included in the mod output.
#
# MakeModFolder.ps1 produces:
#   Output/mods/WukongMp.PvP   the client mod folder, dropped into the game's Mods/ folder
#   Output/server_mods         loose files, dropped into the server's server_mods/ folder
#
# NOTE: the Common project is spelled "Pvp", not "PvP", so its assembly is WukongMp.Pvp.Common.dll.
# The casing matters on a case-sensitive filesystem, e.g. CI on Linux.

# Project folder names. The client mod folder in Output takes its name from $clientProject.
$clientProject = "WukongMp.PvP"
$serverProject = "WukongMp.PvP.Serverside"

# Copied from the client build folder (WukongMp.PvP/bin/<Configuration>/netstandard2.0)
# into the client mod folder
$clientBuildFiles = @(
    "WukongMp.PvP.dll",
    "WukongMp.Pvp.Common.dll"
)

# Copied from the "Content" folder into the client mod folder root
$contentFiles = @(
    # Add any non-code files here, e.g. save files or .paks.
    "manifest.json",
    "ArchiveSaveFile.0.sav", # endgame arena save
    "ArchiveSaveFile.1.sav", # new character save
    "ArchiveSaveFile.2.sav"  # matchmaking shared save
)

# Copied from the server build folder (WukongMp.PvP.Serverside/bin/<Configuration>/net10.0)
# into server_mods. Server mods have no folder of their own, every file sits next to
# the SDK's own server mods, so only ship what is yours.
$serverBuildFiles = @(
    "WukongMp.PvP.Serverside.dll",
    "WukongMp.Pvp.Common.dll"
)

# Copied only in Debug builds
$clientDebugBuildFiles = @(
    "WukongMp.PvP.pdb",
    "WukongMp.Pvp.Common.pdb"
)

$serverDebugBuildFiles = @(
    "WukongMp.PvP.Serverside.pdb",
    "WukongMp.Pvp.Common.pdb"
)
