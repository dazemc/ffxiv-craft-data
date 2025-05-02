import asyncio

import json
import mmap
import os, sys
import struct

from io import StringIO

import pandas as pd


FFXIV_FILEPATH: str | None = None


IMPORT_DIR: str = "./import"
IMPORT_RECIPE_DIR_KO: str = IMPORT_DIR + "/ko/recipedb"
IMPORT_BUFFS_DIR_KO: str = IMPORT_DIR + "/ko/buffs"

CRAFTDATA_FILEPATH: str = "./CraftData/Release/net7.0/win-x64/publish/"

if not os.path.exists(CRAFTDATA_FILEPATH + "config.json"):
    FFXIV_FILEPATH = input(
        "FFXIV file path has not been set, input the filepath and press enter: "
    ).replace("\\", "/")
    if len(FFXIV_FILEPATH) < 6:
        FFXIV_FILEPATH = None
    else:
        print(
            f"FFXIV filepath set to : {FFXIV_FILEPATH}\nYou can change/delet this at ./CraftData/Release/net7.0/win-x64/publish/config.json"
        )
else:
    print(
        "FFXIV filepath is already set, you can change/delete this at ./CraftData/Release/net7.0/win-x64/publish/config.json"
    )
if FFXIV_FILEPATH == None:
    print("Error reading filepath")
    sys.exit()


CURRENT_FILEPATH: str = os.path.abspath(__file__).replace("main.py", "")


CRAFTDATA_CONFIG: dict = {
    "GameDirectory": FFXIV_FILEPATH,
}


def set_cwd(filepath: str = "") -> None:
    os.chdir(os.path.dirname(CURRENT_FILEPATH + filepath))


async def run_exe_waiting_input() -> asyncio.subprocess.Process:
    try:
        process: asyncio.subprocess.Process = await asyncio.create_subprocess_exec(
            "./CraftData.exe",
            stdout=asyncio.subprocess.PIPE,
        )
        return process

    except Exception as e:
        print(f"An error occurred: {e}")


def read_shared_memory() -> tuple:  # <StringIO>
    shared_memory_name: str = "Global\\CraftData"
    memory_size: int = 0
    memory_offset: int = 8
    memory_size += memory_offset
    buffer_size = 0
    next_buffer_size = 0

    try:
        with mmap.mmap(
            -1, memory_size, tagname=shared_memory_name, access=mmap.ACCESS_READ
        ) as mm:
            buffer_size = struct.unpack_from("i", mm, 0)[0]

            next_buffer_size = struct.unpack_from("i", mm, 4)[0]

            memory_size += buffer_size + next_buffer_size

        with mmap.mmap(-1, memory_size, tagname=shared_memory_name) as mm:
            data: str = (
                mm[memory_offset:buffer_size].decode("utf-8").replace("\x00", "")
            )
            data_item: str = (
                mm[buffer_size:memory_size].decode("utf-8").replace("\x00", "")
            )

            csv_item_buffer: StringIO = StringIO(data_item)

            csv_buffer: StringIO = StringIO(data)

            return csv_buffer, csv_item_buffer

    except Exception as e:
        print(f"Error: {e}")


def set_ffxiv_config(config: dict) -> None:
    if FFXIV_FILEPATH != None:
        if not os.path.exists(FFXIV_FILEPATH + "config.json"):
            with open("config.json", "w") as file:
                json.dump(
                    config,
                    file,
                    indent=2,
                    sort_keys=True,
                    ensure_ascii=False,
                )


async def terminate_signal(process) -> bool | None:
    async for line in process.stdout:
        if "terminate" in line.decode("utf-8").strip():
            return False
        return True


async def create_csv() -> StringIO:
    process_task: asyncio.Task = asyncio.create_task(run_exe_waiting_input())

    process: asyncio.Task = await process_task

    while await terminate_signal(process):
        await asyncio.sleep(0.1)

    csv_data: tuple = read_shared_memory()

    csv: StringIO = csv_data[0]

    csv_item: StringIO = csv_data[1]
    if process:
        process.terminate()

    await process.wait()

    return csv, csv_item


def read_csv(csv: StringIO) -> pd.DataFrame:
    try:
        df: pd.DataFrame = pd.read_csv(csv)

        df.dropna(inplace=True)
        return df

    except Exception as e:
        print(f"Error reading csv: {e}")


def separate_crafters(crafts: list, df: pd.DataFrame) -> dict:
    recipes_df: dict = {}
    for crafter in crafts:
        recipes_df[crafter] = df[df.CraftType == crafter]
    return recipes_df


def separate_buffs(buffs: list, df: pd.DataFrame) -> dict:
    buffs_df: dict = {}

    for buff in buffs:
        buffs_df[buff] = df[df.Category == buff]

    return buffs_df


def merge_json(primary: dict, secondary: dict):
    missing_recipes = {}
    secondary_lookup = {
        recipe["key"]: recipe["name"].get("ko")
        for category in secondary.values()
        for recipe in category
        if "name" in recipe and "ko" in recipe["name"]
    }

    for category, recipes in primary.items():
        for recipe in recipes:
            key = recipe.get("key")
            if key in secondary_lookup:
                if secondary_lookup[key] is None:
                    print(
                        f"Warning: 'ko' name missing for key '{key}' in secondary JSON."
                    )
                else:
                    recipe["name"]["ko"] = secondary_lookup[key]
            else:
                if category not in missing_recipes:
                    missing_recipes[category] = [
                        {
                            "recipe": recipe["name"]["en"],
                            "key": recipe["key"],
                        }
                    ]
                else:
                    missing_recipes[category].append(
                        {
                            "recipe": recipe["name"]["en"],
                            "key": recipe["key"],
                        }
                    )

    if len(missing_recipes) > 0:
        with open(
            f"{IMPORT_DIR}/ko/missing_recipes.json", "w", encoding="utf-8"
        ) as file:
            json.dump(missing_recipes, file, ensure_ascii=False, indent=2)


def is_import_available() -> bool:
    return os.path.isdir(IMPORT_RECIPE_DIR_KO)


def get_ko_dict() -> dict:
    ko_json = {}

    json_files_list = os.listdir(IMPORT_RECIPE_DIR_KO)

    for crafter_file in json_files_list:
        with open(
            f"{IMPORT_RECIPE_DIR_KO}/{crafter_file}", "r", encoding="utf-8"
        ) as json_recipe:
            ko_json[crafter_file[:-5]] = json.load(json_recipe)

    return ko_json


def create_export_recipe(recipes_df: dict) -> dict:
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
                case "Alchemy" | "연금":
                    craft_key: str = "Alchemist"

                case "Armorcraft" | "갑주제작":
                    craft_key: str = "Armorer"

                case "Smithing" | "대장일":
                    craft_key: str = "Blacksmith"

                case "Woodworking" | "목공":
                    craft_key: str = "Carpenter"

                case "Cooking" | "요리":
                    craft_key: str = "Culinarian"

                case "Goldsmithing" | "보석공예":
                    craft_key: str = "Goldsmith"

                case "Clothcraft" | "재봉":
                    craft_key: str = "Weaver"

                case "Leatherworking" | "가죽공예":
                    craft_key: str = "Leatherworker"

            current_recipe: dict = recipes_df[craft_type].iloc[i]

            recipe_key: int = int(current_recipe.Key)
            name: str = current_recipe.get("Name", "")
            name_de: str = str(current_recipe.get("NameDE", name)).replace(
                "<SoftHyphen/>", "\u00ad"
            )
            name_fr: str = str(current_recipe.get("NameFR", name)).replace(
                "<SoftHyphen/>", "\u00ad"
            )
            name_ja: str = current_recipe.get("NameJA", name)
            name_ko: str | bool = current_recipe.get(
                "NameKO", None
            )  # ko version < current version
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
            suggested_craft: int = int(current_recipe.SuggestedCraftsmanship)
            stars: int = int(current_recipe.Stars)

            export_recipes[craft_key].append(
                {
                    "key": recipe_key,
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
                        "ko": name_ko,
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


def create_export_buffs(df) -> dict:
    export_buffs = {"Meal": [], "Medicine": []}
    buff_key = ""

    for buff in df:
        for i in range(len(df[buff])):
            buff_key = buff

            if buff == "Meals":
                buff_key = "Meal"

            current_item = df[buff].iloc[i]

            buff_types = {}

            buff_types_hq = {}

            for j in range(3):
                match current_item[f"BuffType{j + 1}"]:
                    case "CP":
                        buff_types["cp_percent"] = int(
                            current_item[f"PercentageValue{j + 1}"]
                        )

                        buff_types["cp_value"] = int(current_item[f"MaxValue{j + 1}"])

                        buff_types_hq["cp_percent"] = int(
                            current_item[f"PercentageValueHQ{j + 1}"]
                        )

                        buff_types_hq["cp_value"] = int(
                            current_item[f"MaxValueHQ{j + 1}"]
                        )

                    case "Craftsmanship":
                        buff_types["craftsmanship_percent"] = int(
                            current_item[f"PercentageValue{j + 1}"]
                        )

                        buff_types["craftsmanship_value"] = int(
                            current_item[f"MaxValue{j + 1}"]
                        )

                        buff_types_hq["craftsmanship_percent"] = int(
                            current_item[f"PercentageValueHQ{j + 1}"]
                        )

                        buff_types_hq["craftsmanship_value"] = int(
                            current_item[f"MaxValueHQ{j + 1}"]
                        )

                    case "Control":
                        buff_types["control_percent"] = int(
                            current_item[f"PercentageValue{j + 1}"]
                        )

                        buff_types["control_value"] = int(
                            current_item[f"MaxValue{j + 1}"]
                        )

                        buff_types_hq["control_percent"] = int(
                            current_item[f"PercentageValueHQ{j + 1}"]
                        )

                        buff_types_hq["control_value"] = int(
                            current_item[f"MaxValueHQ{j + 1}"]
                        )

                    case "Nothing":
                        break

            for k in range(2):
                info = {
                    "hq": False if k == 0 else True,
                    "name": {
                        "de": (
                            current_item.get("NameDE", "")
                            if pd.notna(current_item.get("NameDE", ""))
                            else ""
                        ),
                        "en": (
                            current_item.get("Name", "")
                            if pd.notna(current_item.get("Name", ""))
                            else ""
                        ),
                        "fr": (
                            current_item.get("NameFR", "")
                            if pd.notna(current_item.get("NameFR", ""))
                            else ""
                        ),
                        "ja": (
                            current_item.get("NameJA", "")
                            if pd.notna(current_item.get("NameJA", ""))
                            else ""
                        ),
                    },
                }

                if k == 0:
                    buff_types.update(info)
                    export_buffs[buff_key].append(buff_types)
                else:
                    buff_types_hq.update(info)
                    export_buffs[buff_key].append(buff_types_hq)

    return export_buffs


def export(export_recipes: dict, export_buffs: dict) -> None:
    finish_message = ""
    path: str = "../../data/recipedb/"
    path_item: str = "../../data/buffs/"
    is_ko = export_recipes["Weaver"][0]["name"].get("ko", False)
    is_en = export_recipes["Weaver"][0]["name"].get("en", False)
    if is_ko and not is_en:
        finish_message = "Korean data exported to import/ko/"
        path: str = f"{IMPORT_RECIPE_DIR_KO}"
        path_item: str = f"{IMPORT_BUFFS_DIR_KO}"
    elif not os.path.isdir(path):
        finish_message = "Data exported to export/"
        path: str = "./export/recipedb/"
        path_item: str = "./export/buffs/"
    else:
        finish_message = "Data exported to ffxiv-craft/data"
        path: str = "../../data/recipedb/"
        path_item: str = "../../data/buffs/"

    os.makedirs(path, exist_ok=True)
    os.makedirs(path_item, exist_ok=True)

    for crafter in export_recipes:
        with open(f"./{path}/{crafter}.json", "w", encoding="utf-8") as json_file:
            json.dump(
                export_recipes[crafter],
                json_file,
                indent=2,
                sort_keys=True,
                ensure_ascii=False,
            )

    for buff in export_buffs:
        with open(f"./{path_item}/{buff}.json", "w", encoding="utf-8") as json_file:
            json.dump(
                export_buffs[buff],
                json_file,
                indent=2,
                sort_keys=True,
                ensure_ascii=False,
            )
    if finish_message == "":
        print("Error writing data files")
        return
    print(finish_message)


async def main() -> None:
    set_cwd(CRAFTDATA_FILEPATH)
    set_ffxiv_config(CRAFTDATA_CONFIG)
    csv: tuple = await create_csv()
    csv_recipe: StringIO = csv[0]
    csv_item: StringIO = csv[1]
    set_cwd()
    df_recipe: pd.DataFrame = read_csv(csv_recipe)
    df_item: pd.DataFrame = read_csv(csv_item)
    craft_types: list = list(df_recipe.CraftType.unique())
    buff_types: list = list(df_item.Category.unique())
    ordered_buff: dict = separate_buffs(buffs=buff_types, df=df_item)
    ordered_recipe: dict = separate_crafters(crafts=craft_types, df=df_recipe)
    export_buffs: dict = create_export_buffs(df=ordered_buff)
    export_recipes: dict = create_export_recipe(recipes_df=ordered_recipe)
    if is_import_available():
        ko_recipes = get_ko_dict()
        ordered_recipe = merge_json(export_recipes, ko_recipes)
    export(export_recipes=export_recipes, export_buffs=export_buffs)


if __name__ == "__main__":
    asyncio.run(main())
