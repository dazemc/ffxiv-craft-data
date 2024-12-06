import pandas as pd
import re
import json

df = pd.read_csv('./recipe/Recipe.csv')
df_level_table = pd.read_csv('./recipe/RecipeLevelTable.csv')

df_level_table.columns = df_level_table.iloc[0]
df_level_table = df_level_table[2:]

df.columns = df.iloc[0]
df = df[1:]
craft_types = list(df["CraftType"].unique())
craft_types.pop(0)
recipes_df = {}
for craft in craft_types:
    recipes_df[craft] = df[df["CraftType"] == craft]

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

for craft_type in recipes_df:
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
        level_table_num = [num for num in re.findall(r'\d+', current_recipe["RecipeLevelTable"])][0]
        current_recipe["RecipeLevelTable"] = df_level_table.iloc[int(level_table_num)]  # append level table
        current_level_table = current_recipe["RecipeLevelTable"]
        difficulty = current_level_table["Difficulty"]
        durability = current_level_table["Durability"]
        level = current_level_table["ClassJobLevel"]
        base_level = current_level_table["#"]
        quality = current_level_table["Quality"]
        progress_divider = current_level_table["ProgressDivider"]
        progress_modifier = current_level_table["ProgressModifier"]
        quality_divider = current_level_table["QualityDivider"]
        quality_modifier = current_level_table["QualityModifier"]
        suggested_craft = current_level_table["SuggestedCraftsmanship"]
        stars = current_level_table["Stars"]
        if (type(name) == str):
            export_recipes[craft_key].append({
                "baseLevel": base_level,
                "difficulty": difficulty,
                "durability": durability,
                "level": level,
                "maxQuality": quality,
                "name": {
                    "en": name,
                },
                "progressDivider": progress_divider,
                "progressModifier": progress_modifier,
                "qualityDivider": quality_divider,
                "qualityModifier": quality_modifier,
                "suggestedCraftsmanship": suggested_craft,
                "stars": stars,
            })



for crafter in export_recipes:
    with open(f"./export/{crafter}.json", "w") as json_file:
        json.dump(export_recipes[crafter], json_file, indent=2, sort_keys=True, ensure_ascii=False)

