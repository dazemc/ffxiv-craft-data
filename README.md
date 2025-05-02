# FFXIV Craft Data Processor

This repository contains a Python script (`main.py`) that processes crafting data for *Final Fantasy XIV* (FFXIV). It communicates with a C# executable (`CraftData.exe`) via **shared memory IPC** to generate CSV data for recipes and buffs, processes it with `pandas`, and exports JSON files. The script supports Korean translations using JSON files in the `import/ko/` directory.

## Features
- Configures `CraftData.exe` using the FFXIV game directory path.
- Processes crafting recipes and buffs into structured JSON files.
- Supports Korean translations via JSON files in `import/ko/recipedb/` and `import/ko/buffs/`.
- Outputs data to `export/recipedb/` and `export/buffs/` (or `import/ko/` for Korean data, or `../../data/` if present).
- Uses **shared memory IPC** to exchange data between `main.py` and `CraftData.exe`.

## Shared Memory IPC
The Python script (`main.py`) communicates with `CraftData.exe` (located in `CraftData/Release/net7.0/win-x64/publish/`) using **shared memory inter-process communication (IPC)**. The shared memory region (`Global\\CraftData`) includes a header with two 4-byte integers:
- **First integer**: Length of the recipe CSV data buffer.
- **Second integer**: Length of the item (buff) CSV data buffer.
`CraftData.exe` writes CSV data to this region, which `main.py` reads, decodes as UTF-8, and processes with `pandas`.

## CraftData C# Source
`CraftData.exe` is a C# program that uses the **SaintCoinach** library to extract FFXIV game data and generate CSV files for recipes and buffs. The source code is available in the repository (or a linked location) and includes:
- **Data Extraction**: Reads game data (recipes, items, buffs) from the FFXIV directory using SaintCoinach.
- **CSV Generation**: Creates CSV strings for recipes and buffs, including multilingual names (English, Japanese, German, French, Korean).
- **Shared Memory**: Writes CSV data to the `Global\\CraftData` shared memory region with a header indicating buffer lengths.
- **Configuration**: Reads the FFXIV game directory from `config.json`.

## SaintCoinach Dependency
The project relies on **SaintCoinach** (in `./src/SaintCoinach`), a library for extracting FFXIV game data. The SaintCoinach source files are **soft-linked** to the `CraftData` source, so updates to SaintCoinach propagate to `CraftData`.

- **Important**: SaintCoinach must be updated after every FFXIV patch to ensure compatibility with the latest game data.
- **Update Process**:
  - Run the provided `update.ps1` PowerShell script to update SaintCoinach and build CraftData.exe:
    ```bash
    ./update.ps1
    ```
  - Alternatively, reference `update.ps1` for manual commands, typically:
    ```bash
    cd src/SaintCoinach
    git pull origin main
    cd ../..
    # Rebuild CraftData
    ```
  - Recompile `CraftData.exe` as it is self contained.

## Prerequisites
- **Python** with `uv` (a fast Python package manager).
- **Git LFS** to pull large files (e.g., `CraftData.exe`).
- All required files are included in the repository:
  - `main.py`
  - `CraftData.exe` (in `CraftData/Release/net7.0/win-x64/publish/`)
  - `import/ko/recipedb/` (with JSON files like `Carpenter.json`, `Blacksmith.json`)
  - `import/ko/buffs/` (with JSON files like `Meal.json`, `Medicine.json`)
  - `CraftData/Release/net7.0/win-x64/publish/` (for `config.json`)
  - `src/SaintCoinach/` (SaintCoinach source, soft-linked to `CraftData`)

## Setup and Run

Follow these steps to set up and run the script using `uv`.

### 1. Install `uv`
Install `uv` to manage Python and dependencies:

```bash
# Windows (PowerShell)
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

# Linux/macOS
curl -LsSf https://astral.sh/uv/install.sh | sh
```

Verify installation:
```bash
uv --version
```

### 2. Install Git LFS
The repository uses **Git LFS** to store large files (e.g., `CraftData.exe`). Install Git LFS to pull these files:

```bash
# Windows (PowerShell)
winget install Git.Git
git lfs install

# Linux (Debian/Ubuntu)
sudo apt-get install git-lfs
git-lfs install

# macOS (Homebrew)
brew install git-lfs
git-lfs install
```

### 3. Clone the Repository
Clone the project, ensuring Git LFS pulls large files:

```bash
git clone https://github.com/dazemc/ffxiv-craft-data.git
cd ffxiv-craft-data
git submodule update --init --recursive
git lfs pull
```

### 4. Update SaintCoinach (Post-Patch)
If setting up after an FFXIV patch, update the SaintCoinach source:

```bash
./update.ps1
```

- This pulls the latest SaintCoinach data and rebuilds `CraftData` if needed.
- Check `update.ps1` for manual commands (e.g., `git pull` in `src/SaintCoinach`).

### 5. Initialize the Project
Initialize a `uv` project to create `pyproject.toml`:

```bash
uv init
```

### 6. Run the Script
Execute `main.py` using `uv run`:

```bash
uv run python main.py
```

- **What happens**:
  - If `config.json` is missing in `CraftData/Release/net7.0/win-x64/publish/`, you’ll be prompted to enter the FFXIV game directory path (e.g., `C:/Program Files/FFXIV`).
  - `main.py` runs `CraftData.exe`, reads CSV data via shared memory IPC, and generates JSON files in `export/recipedb/` and `export/buffs/` (or `import/ko/` for Korean data, or `../../data/` if present).
  - Korean JSON files in `import/ko/recipedb/` and `import/ko/buffs/` are merged into the output.

- **Input Tips**:
  - Example: `G:\SteamLibrary\steamapps\common\FINAL FANTASY XIV Online` slash orientation does not matter.

### Alternative: Manual Virtual Environment
If you prefer to manage the virtual environment manually:

```bash
uv venv
.\.venv\Scripts\activate  # Windows
source .venv/bin/activate  # Linux/macOS
uv sync
python main.py
```

## Troubleshooting

- **ModuleNotFoundError: No module named 'pandas'**:
  - Ensure you ran `uv add pandas` and use `uv run python main.py`.
  - Or, sync the virtual environment:
    ```bash
    uv sync
    ```

- **Error: CraftData.exe not found**:
  - Verify `CraftData.exe` is in `CraftData/Release/net7.0/win-x64/publish/`.
  - Ensure you ran `git lfs pull` to download large files.
  - Check Git LFS setup or contact the maintainer.

- **Error reading filepath**:
  - The FFXIV filepath must be valid and at least 6 characters long.
  - Delete `config.json` to reset:
    ```bash
    del CraftData\Release\net7.0\win-x64\publish\config.json
    ```

- **SaintCoinach Outdated**:
  - If data is incorrect or `CraftData.exe` fails, update SaintCoinach:
    ```bash
    ./update.ps1
    ```
  - Recompile `CraftData.exe` if SaintCoinach changes significantly.

- **Path Issues**:
  - Run the script from the project directory:
    ```bash
    cd path/to/ffxiv-craft-data
    uv run python main.py
    ```
  - Ensure write permissions for `export/` and `config.json`.

- **Python Version**:
  - `uv` uses a compatible Python version (e.g., 3.11 or 3.13). If issues occur, try Python 3.11:
    ```bash
    uv python install 3.11
    uv run --python 3.11 python main.py
    ```

- **Windows-Specific**:
  - `CraftData.exe` is Windows-only.

## Contributing
- Report issues or suggest improvements via [GitHub Issues](https://github.com/dazemc/ffxiv-craft-data/issues).
- Submit pull requests with enhancements (e.g., additional language support, improved error handling).

## License
[MIT License](LICENSE)

## Contact
For questions, open an issue or contact [dazemc](https://github.com/dazemc).
