#!powershell.exe -ExecutionPolicy Bypass -File

# Edit these lists to specify files that should be included in the mod output.
#
# MakeModFolder.ps1 produces:
#   Output/mods/<mod>/client       client DLLs, sent to players
#   Output/mods/<mod>/server       server DLLs, never sent to players
#   Output/mods/<mod>/manifest.json  shared by both sides

$clientProject = "WukongMp.PvP"
$serverProject = "WukongMp.PvP.Serverside"

# Copied from the client build folder (WukongMp.PvP/bin/<Configuration>/netstandard2.0) into the client folder
$clientBuildFiles = @(
    "WukongMp.PvP.dll",
    "WukongMp.Pvp.Common.dll"
)

# Copied from the "Content" folder into the mod folder root
$manifestFiles = @("manifest.json")

# Copied from the "Content" folder into the client folder
$clientContentFiles = @(
    # Add any non-code client files here, e.g. save files or .paks.
    "ArchiveSaveFile.0.sav", # endgame arena save
    "ArchiveSaveFile.1.sav", # new character save
    "ArchiveSaveFile.2.sav"  # matchmaking shared save
)

# Copied from the server build folder (WukongMp.PvP.Serverside/bin/<Configuration>/net10.0) into the server folder.
$serverBuildFiles = @(
    "WukongMp.PvP.Serverside.dll",
    "WukongMp.Pvp.Common.dll"
)

# Copied from the "Content" folder into the server folder. Never sent to players.
$serverContentFiles = @(
    "config.json"
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
