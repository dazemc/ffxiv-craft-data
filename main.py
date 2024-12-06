import pandas as pd
import numpy as np
import re

df = pd.read_csv('./recipe/Recipe.csv')
df_level_table = pd.read_csv('./recipe/RecipeLevelTable.csv')

# print(df_level_table.iloc[0])
df_level_table.columns = df_level_table.iloc[0]
df_level_table = df_level_table[2:]
# print(df_level_table.iloc[6])

df.columns = df.iloc[0]
df = df[1:]
craft_types = list(df["CraftType"].unique())
craft_types.pop(0)
recipes_df = {}
for craft in craft_types:
    recipes_df[craft] = df[df["CraftType"] == craft]

# print(craft_types)

export_recipes = {
    "Woodworking": [],
    "Smithing": [],
    "Armorcraft": [],
    "Leatherworking": [],
    "Clothcraft": [],
    "Goldsmithing": [],
    "Cooking": [],
    "Alchemy": [],
}

for craft_type in recipes_df:
    for i in range(len(recipes_df[craft_type])):
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
            export_recipes[craft_type].append({
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

print(export_recipes["Alchemy"])



