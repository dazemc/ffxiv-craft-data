import pandas as pd
import json
import os
import asyncio
import mmap
import struct
from io import StringIO

CRAFTDATA_FILEPATH: str = "/CraftData/Debug/net7.0/"
CRAFTDATA_CONFIG: str = CRAFTDATA_FILEPATH + "config.txt"
FFXIV_FILEPATH: str = "G:/SteamLibrary/steamapps/common/FINAL FANTASY XIV Online"
# CSV_FILEPATH: str = "./CraftData/Debug/net7.0/export/Recipe.csv"
CURRENT_FILEPATH: str = os.path.abspath(__file__).replace("main.py", "")


def set_cwd(filepath: str = "") -> None:
    os.chdir(os.path.dirname(CURRENT_FILEPATH + filepath))


async def run_exe_waiting_input() -> asyncio.subprocess.Process:
    try:
        process: asyncio.subprocess.Process = await asyncio.create_subprocess_exec(
            "./CraftData.exe",
            # stdout=asyncio.subprocess.PIPE,
        )
        return process

    except Exception as e:
        print(f"An error occurred: {e}")


def read_shared_memory() -> StringIO:
    shared_memory_name = "Global\\CraftData"
    memory_size = 5000192

    try:
        with mmap.mmap(-1, memory_size, tagname=shared_memory_name) as mm:
            data_length: int = struct.unpack("i", mm[:4])[0]
            data: str = mm[4 : 4 + data_length].decode("utf-8")
            csv_buffer: StringIO = StringIO(data)
            return csv_buffer
    except Exception as e:
        print(f"Error: {e}")


def set_ffxiv_filepath(filepath: str) -> None:
    with open("config.txt", "w") as file:
        file.writelines(filepath)


async def create_csv() -> StringIO:
    process_task: asyncio.Task = asyncio.create_task(run_exe_waiting_input())
    process: asyncio.Task = await process_task
    await asyncio.sleep(8)  # TODO: Dynamic await
    csv: StringIO = read_shared_memory()
    if process:
        process.terminate()
    await process.wait()
    return csv


def read_csv(csv: StringIO) -> pd.DataFrame:
    try:
        df: pd.DataFrame = pd.read_csv(csv)
        df.dropna(inplace=True)
        return df
    except Exception as e:
        print(f"Error reading csv: {e}")


def seperate_crafters(crafts: list, df: pd.DataFrame) -> dict:
    recipes_df: dict = {}
    for crafter in crafts:
        recipes_df[crafter] = df[df.CraftType == crafter]
    return recipes_df


def create_export(recipes_df: dict) -> dict:
    export_recipes: dict = {
        "Carpenter": [],
        "Blacksmith": [],
        "Armorer": [],
        "Leatherworker": [],
        "Weaver": [],
        "Goldsmith": [],
        "Culinarian": [],
        "Alchemist": [],
    }

    for craft_type in recipes_df:
        for i in range(len(recipes_df[craft_type])):
            match craft_type:
                case "Alchemy":
                    craft_key: str = "Alchemist"
                case "Armorcraft":
                    craft_key: str = "Armorer"
                case "Smithing":
                    craft_key: str = "Blacksmith"
                case "Woodworking":
                    craft_key: str = "Carpenter"
                case "Cooking":
                    craft_key: str = "Culinarian"
                case "Goldsmithing":
                    craft_key: str = "Goldsmith"
                case "Clothcraft":
                    craft_key: str = "Weaver"
                case "Leatherworking":
                    craft_key: str = "Leatherworker"
            current_recipe: dict = recipes_df[craft_type].iloc[i]
            name: str = current_recipe.Name
            name_de: str = str(current_recipe.NameDE).replace("<SoftHyphen/>", "\u00AD")
            name_fr: str = str(current_recipe.NameFR).replace("<SoftHyphen/>", "\u00AD")
            name_ja: str = current_recipe.NameJA
            difficulty: int = int(current_recipe.Difficulty)
            durability: int = int(current_recipe.Durability)
            base_level: int = int(current_recipe.ClassJobLevel)
            level: int = int(current_recipe.Level)
            quality: int = int(current_recipe.Quality)
            difficulty_factor: int = int(current_recipe.DifficultyFactor) / 100
            quality_factor: int = int(current_recipe.QualityFactor) / 100
            durability_factor: int = int(current_recipe.DurabilityFactor) / 100
            difficulty *= difficulty_factor
            durability *= durability_factor
            quality *= quality_factor
            difficulty: int = int(difficulty // 1)
            durability: int = int(durability // 1)
            quality: int = int(quality // 1)
            progress_divider: int = int(current_recipe.ProgressDivider)
            progress_modifier: int = int(current_recipe.ProgressModifier)
            quality_divider: int = int(current_recipe.QualityDivider)
            quality_modifier: int = int(current_recipe.QualityModifier)
            suggested_craft: int = int(current_recipe.SuggestedCrafsmanship)
            stars: int = int(current_recipe.Stars)
            export_recipes[craft_key].append(
                {
                    "baseLevel": base_level,
                    "difficulty": difficulty,
                    "durability": durability,
                    "level": level,
                    "maxQuality": quality,
                    "name": {
                        "de": name_de,
                        "en": name,
                        "fr": name_fr,
                        "ja": name_ja,
                    },
                    "progressDivider": progress_divider,
                    "progressModifier": progress_modifier,
                    "qualityDivider": quality_divider,
                    "qualityModifier": quality_modifier,
                    "suggestedCraftsmanship": suggested_craft,
                    "stars": stars,
                }
            )
    return export_recipes


def export(export_recipes: dict) -> None:
    path: str = "../../data/recipedb/"
    if not os.path.isdir(path):
        path: str = "export"
        if not os.path.isdir(path):
            os.mkdir(path)
    for crafter in export_recipes:
        with open(f"./{path}/{crafter}.json", "w", encoding="utf-8") as json_file:
            json.dump(
                export_recipes[crafter],
                json_file,
                indent=2,
                sort_keys=True,
                ensure_ascii=False,
            )


async def main() -> None:
    set_cwd(CRAFTDATA_FILEPATH)
    set_ffxiv_filepath(FFXIV_FILEPATH)
    csv: StringIO = await create_csv()
    set_cwd()
    df: pd.DataFrame = read_csv(csv)
    craft_types: list = list(df.CraftType.unique())
    recipes_df: dict = seperate_crafters(crafts=craft_types, df=df)
    export_recipes: dict = create_export(recipes_df=recipes_df)
    export(export_recipes=export_recipes)


if __name__ == "__main__":
    asyncio.run(main())
