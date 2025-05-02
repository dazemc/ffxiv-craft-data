$sourcePath = "./src/SaintcoinachSrc/SaintCoinach/bin/Release/net7.0"
$destPath = "./src/CraftDataSrc"

# Ensure the destination directory exists
if (-not (Test-Path $destPath)) {
    New-Item -Path $destPath -ItemType Directory -Force
}

# Get all files and directories recursively
$items = Get-ChildItem -Path $sourcePath -Recurse

# Iterate over each item (file or directory)
foreach ($item in $items) {
    # Calculate the relative path of the item
    $relativePath = $item.FullName.Substring((Get-Item $sourcePath).FullName.Length + 1)
    # Construct the destination path for the symbolic link
    $linkPath = Join-Path -Path $destPath -ChildPath $relativePath

    # Ensure the parent directory for the symbolic link exists
    $linkDir = Split-Path -Path $linkPath -Parent
    if (-not (Test-Path $linkDir)) {
        New-Item -Path $linkDir -ItemType Directory -Force
    }

    # Create a symbolic link for the item (file or directory)
    New-Item -Path $linkPath -ItemType SymbolicLink -Value $item.FullName -Force
}
git submodule update --init --recursive
git submodule foreach git pull origin master
cd ./src/SaintcoinachSrc/
dotnet build -c Release
cd ../CraftDataSrc/CraftData/CraftData
dotnet publish -c Release
cd ../../../
exit
