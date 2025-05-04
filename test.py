import json

DUPE_CHECK = True

# with open("./export/recipedb/Leatherworker.json", "r") as file:
#     data = json.load(file)


def dupe_check(input_file):
    with open(input_file, "r", encoding="utf-8") as read_file:
        data = json.load(read_file)
    recipe_names = []
    duplicate_recipes = []
    for recipe in data:
        name = recipe["name"]["en"]
        for dupe_name in recipe_names:
            if dupe_name == name:
                print(recipe)
                duplicate_recipes.append(dict(recipe))
        recipe_names.append(name)
    if len(duplicate_recipes) > 0:
        with open("duplicate.json", "w", encoding="utf-8") as file:
            print("creating duplicate.json...")
            json.dump(duplicate_recipes, file, indent=4)


if DUPE_CHECK:
    dupe_check("./export/recipedb/Leatherworker.json")
