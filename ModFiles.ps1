#!powershell.exe -ExecutionPolicy Bypass -File

# Edit these lists to specify files that should be included in the mod output.
#
# MakeModFolder.ps1 produces the client mod folder twice:
#   Output/mods/WukongMp.PvP   matches the co-op mod's layout, drop the whole mods folder into the game
#   Output/WukongMp.PvP        the same folder at the Output root, for the existing manual workflow
#
# There is no server_mods output yet.

# Project folder name. The mod folder in Output takes its name from this.
$clientProject = "WukongMp.PvP"

# Copied from build folder ($clientProject/bin/<Configuration>/netstandard2.0)
$buildFiles = @(
    "WukongMp.PvP.dll"
)

# Copied from the "Content" folder to mod folder root
$contentFiles = @(
    # Add any non-code files here, e.g. save files or .paks.
    "manifest.json",
    "ArchiveSaveFile.0.sav", # endgame arena save
    "ArchiveSaveFile.1.sav", # new character save
    "ArchiveSaveFile.2.sav"  # matchmaking shared save
)

# Copied from build folder to mod folder root (only in Debug builds)
$debugBuildFiles = @(
    "WukongMp.PvP.pdb"
)
