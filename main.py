import pandas as pd
import re
import json
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
os.chdir(script_dir)

df = pd.read_csv("./recipe/Recipe.csv")
df_de = pd.read_csv("./recipe/RecipeDE.csv")
df_fr = pd.read_csv("./recipe/RecipeFR.csv")
df_ja = pd.read_csv("./recipe/RecipeJA.csv")

df_level_table = pd.read_csv("./recipe/RecipeLevelTable.csv")
df_level_table.columns = df_level_table.iloc[0]
df_level_table = df_level_table[2:]

df.columns = df.iloc[0]
df = df[1:]
df_de.columns = df_de.iloc[0]
df_de = df_de[1:]
df_fr.columns = df_fr.iloc[0]
df_fr = df_fr[1:]
df_ja.columns = df_ja.iloc[0]
df_ja = df_ja[1:]


craft_types = list(df["CraftType"].unique())[1:]
craft_types_de = list(df_de["CraftType"].unique())[1:]
craft_types_fr = list(df_fr["CraftType"].unique())[1:]
craft_types_ja = list(df_ja["CraftType"].unique())[1:]


recipes_df = {}
recipes_df_de = {}
recipes_df_fr = {}
recipes_df_ja = {}
for i in range(len(craft_types)):
    recipes_df[craft_types[i]] = df[df["CraftType"] == craft_types[i]]
    recipes_df_de[craft_types_de[i]] = df_de[df_de["CraftType"] == craft_types_de[i]]
    recipes_df_fr[craft_types_fr[i]] = df_fr[df_fr["CraftType"] == craft_types_fr[i]]
    recipes_df_ja[craft_types_ja[i]] = df_ja[df_ja["CraftType"] == craft_types_ja[i]]


export_recipes = {
    "Carpenter": [],
    "Blacksmith": [],
    "Armorer": [],
    "Leatherworker": [],
    "Weaver": [],
    "Goldsmith": [],
    "Culinarian": [],
    "Alchemist": [],
}


for j, craft_type in enumerate(recipes_df):
    for i in range(len(recipes_df[craft_type])):
        match craft_type:
            case "Alchemy":
                craft_key = "Alchemist"
            case "Armorcraft":
                craft_key = "Armorer"
            case "Smithing":
                craft_key = "Blacksmith"
            case "Woodworking":
                craft_key = "Carpenter"
            case "Cooking":
                craft_key = "Culinarian"
            case "Goldsmithing":
                craft_key = "Goldsmith"
            case "Clothcraft":
                craft_key = "Weaver"
            case "Leatherworking":
                craft_key = "Leatherworker"
        current_recipe = recipes_df[craft_type].iloc[i]
        name = current_recipe["Item{Result}"]
        name_de = recipes_df_de[craft_types_de[j]].iloc[i]["Item{Result}"]
        name_de = str(name_de).replace("<SoftHyphen/>", "\u00AD")
        name_fr = recipes_df_fr[craft_types_fr[j]].iloc[i]["Item{Result}"]
        name_fr = str(name_fr).replace("<SoftHyphen/>", "\u00AD")
        name_ja = recipes_df_ja[craft_types_ja[j]].iloc[i]["Item{Result}"]
        level_table_num = [
            num for num in re.findall(r"\d+", current_recipe["RecipeLevelTable"])
        ][0]
        current_recipe["RecipeLevelTable"] = df_level_table.iloc[
            int(level_table_num)
        ]  # append level table
        current_level_table = current_recipe["RecipeLevelTable"]
        difficulty = int(current_level_table["Difficulty"])
        durability = int(current_level_table["Durability"])
        base_level = current_level_table["ClassJobLevel"]
        level = current_level_table["#"]
        quality = int(current_level_table["Quality"])
        difficulty_factor = int(current_recipe["DifficultyFactor"]) / 100
        quality_factor = int(current_recipe["QualityFactor"]) / 100
        durability_factor = int(current_recipe["DurabilityFactor"]) / 100
        difficulty *= difficulty_factor
        durability *= durability_factor
        quality *= quality_factor
        difficulty = int(difficulty // 1)
        durability = int(durability // 1)
        quality = int(quality // 1)
        progress_divider = current_level_table["ProgressDivider"]
        progress_modifier = current_level_table["ProgressModifier"]
        quality_divider = current_level_table["QualityDivider"]
        quality_modifier = current_level_table["QualityModifier"]
        suggested_craft = current_level_table["SuggestedCraftsmanship"]
        stars = current_level_table["Stars"]
        if type(name) == str:
            export_recipes[craft_key].append(
                {
                    "baseLevel": int(base_level),
                    "difficulty": difficulty,
                    "durability": durability,
                    "level": int(level),
                    "maxQuality": quality,
                    "name": {
                        "de": name_de,
                        "en": name,
                        "fr": name_fr,
                        "ja": name_ja,
                    },
                    "progressDivider": int(progress_divider),
                    "progressModifier": int(progress_modifier),
                    "qualityDivider": int(quality_divider),
                    "qualityModifier": int(quality_modifier),
                    "suggestedCraftsmanship": int(suggested_craft),
                    "stars": int(stars),
                }
            )

path = "../../data/recipedb/"
if not os.path.isdir(path):
    path = "export"
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
