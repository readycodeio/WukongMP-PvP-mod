#!powershell.exe -ExecutionPolicy Bypass -File

# Edit these lists to specify files that should be included in the mod folder.

# Copied from build folder (bin/Configuration/netstandard2.0)
$buildFiles = @(
    "WukongMp.PvP.dll"
)

# Copied from the "Content" folder to mod folder root
$contentFiles = @(
    # Add any non-code files here, e.g. save files or .paks.
    "manifest.json",
    "ArchiveSaveFile.0.sav", # endgame arena save
    "ArchiveSaveFile.1.sav"  # new character save
)

# Copied from build folder to mod folder root (only in Debug builds)
$debugBuildFiles = @(
    "WukongMp.PvP.pdb"
)
